using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Wsa.Quiz.App.State;
using Wsa.Quiz.App.Views;
using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App;

/// <summary>
/// Shell principale dell'app. Orchestra il caricamento dati al boot,
/// il TabControl con le tre sezioni (Home / Cronologia / Sospesi)
/// e la transizione fra Home / Quiz / Riepilogo all'interno del tab Home.
/// </summary>
public partial class MainWindow : Window
{
    private StorageService? _storage;
    private List<Materia> _materie = new();
    private List<Domanda> _tutteDomande = new();

    private HomeView? _homeView;
    private CronologiaView? _cronologiaView;
    private SospesiView? _sospesiView;

    public MainWindow()
    {
        InitializeComponent();
        Caricamento();
    }

    // ------------------------------------------------------------------ TASTIERA GLOBALE

    /// <summary>
    /// Step 8: Ctrl+Tab / Ctrl+Shift+Tab cambiano tab principale in modo ciclico.
    /// Funziona ovunque nell'app, anche dentro al quiz (il quiz resta vivo sotto
    /// e si ritrova quando si torna sulla tab Home).
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            int n = Tabs.ItemCount;
            if (n <= 0) { base.OnKeyDown(e); return; }
            int delta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : +1;
            Tabs.SelectedIndex = (Tabs.SelectedIndex + delta + n) % n;
            // Sposto il focus nella view appena attivata cosi' le scorciatoie locali
            // partono immediate senza dover cliccare.
            switch (Tabs.SelectedIndex)
            {
                case 0: (HomeArea.Content as Control)?.Focus(); break;
                case 1: (CronologiaArea.Content as Control)?.Focus(); break;
                case 2: (SospesiArea.Content as Control)?.Focus(); break;
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    // ------------------------------------------------------------------ BOOT

    private void Caricamento()
    {
        string cartellaDati = AppContext.BaseDirectory;
        string cartellaUtente = StorageService.CartellaUtenteDefault();
        FooterText.Text = $"Dati read-only: {cartellaDati}    ·    Cartella utente: {cartellaUtente}";

        try
        {
            _storage = new StorageService(cartellaDati, cartellaUtente);
            _materie = _storage.CaricaMaterie();
            _tutteDomande = _storage.CaricaTutteLeDomande(_materie);

            StatoHeaderText.Text = $"{_materie.Count} materie · {_tutteDomande.Count} domande";

            // ---------- Tab Home ----------
            _homeView = new HomeView();
            _homeView.Inizializza(_materie, _tutteDomande);
            _homeView.QuizAvviato += OnQuizAvviato;
            HomeArea.Content = _homeView;

            // ---------- Tab Cronologia ----------
            _cronologiaView = new CronologiaView();
            _cronologiaView.Inizializza(_storage);
            CronologiaArea.Content = _cronologiaView;

            // ---------- Tab Sospesi ----------
            _sospesiView = new SospesiView();
            _sospesiView.Inizializza(_storage);
            _sospesiView.RiprendiRichiesto += OnRiprendiSospesa;
            SospesiArea.Content = _sospesiView;
        }
        catch (Exception ex)
        {
            ErrorePanel.IsVisible = true;
            ErroreText.Text = ex.Message;
            HomeArea.Content = new PlaceholderView(
                "Impossibile avviare",
                "Verifica la presenza di materie.json e della cartella domande/ accanto all'eseguibile.");
        }
    }

    // ------------------------------------------------------------------ NAVIGAZIONE TAB HOME

    private void OnQuizAvviato(object? sender, SessioneQuiz sessione)
    {
        AvviaQuizView(sessione);
    }

    private void AvviaQuizView(SessioneQuiz sessione)
    {
        if (_storage == null) return;
        var quizView = new QuizView(sessione, _storage);
        quizView.QuizConcluso += OnQuizConcluso;
        quizView.QuizMessoInPausa += OnQuizMessoInPausa;
        HomeArea.Content = quizView;
        Tabs.SelectedIndex = 0; // resta sulla tab Home, ma sostituisce il contenuto
    }

    private void OnQuizConcluso(object? sender, SessioneQuiz sessione)
    {
        // Salvataggio cronologia (solo se c'e' almeno una risposta data)
        if (_storage != null && sessione.Risultato.TotaleDomande > 0)
        {
            try
            {
                _storage.SalvaRisultato(sessione.Risultato);
                // La nuova sessione dev'essere visibile la prossima volta che si apre il tab.
                _cronologiaView?.Ricarica();
            }
            catch (Exception ex)
            {
                ErrorePanel.IsVisible = true;
                ErroreText.Text = $"Impossibile salvare la cronologia: {ex.Message}";
            }
        }

        // Se la sessione era stata ripresa da una pausa, rimuovo la pausa originale.
        // Stesso comportamento del console (vedi Program.EseguiSessione*: EliminaPausa(stato.SessioneId)).
        if (_storage != null && sessione.IdSessionePausa is { } idPausa)
        {
            try
            {
                _storage.EliminaPausa(idPausa);
                _sospesiView?.Ricarica();
            }
            catch (Exception ex)
            {
                ErrorePanel.IsVisible = true;
                ErroreText.Text = $"Impossibile eliminare la pausa originale: {ex.Message}";
            }
        }

        var riep = new RiepilogoView(sessione.Risultato);
        riep.TornaAllaHome += (_, _) => RitornaAllaHome();
        HomeArea.Content = riep;
    }

    private void OnQuizMessoInPausa(object? sender, EventArgs e)
    {
        // La pausa e' gia' stata salvata dal QuizView. Torno alla Home e ricarico i sospesi.
        RitornaAllaHome();
        _sospesiView?.Ricarica();
    }

    private void OnRiprendiSospesa(object? sender, SessionePausa pausa)
    {
        if (_storage == null) return;
        try
        {
            var mappa = new Dictionary<string, Domanda>();
            foreach (var d in _tutteDomande) mappa[d.Id] = d;
            var sessione = SessioneQuiz.RiprendiDa(pausa, mappa);
            ErrorePanel.IsVisible = false;
            AvviaQuizView(sessione);
        }
        catch (InvalidOperationException ex)
        {
            ErrorePanel.IsVisible = true;
            ErroreText.Text = $"{ex.Message} La pausa e' stata rimossa.";
            try { _storage.EliminaPausa(pausa.SessioneId); }
            catch { /* best effort: la card rimane finche' non si preme Aggiorna */ }
            _sospesiView?.Ricarica();
        }
    }

    private void RitornaAllaHome()
    {
        ErrorePanel.IsVisible = false;
        if (_homeView == null)
        {
            _homeView = new HomeView();
            _homeView.Inizializza(_materie, _tutteDomande);
            _homeView.QuizAvviato += OnQuizAvviato;
        }
        HomeArea.Content = _homeView;
    }
}
