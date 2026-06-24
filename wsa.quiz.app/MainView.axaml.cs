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
public partial class MainView : UserControl
{
    private StorageService? _storage;
    private List<Materia> _materie = new();
    private List<Domanda> _tutteDomande = new();
    private Dictionary<string, Domanda> _mappaDomande = new();

    private HomeView? _homeView;
    private CronologiaView? _cronologiaView;
    private SospesiView? _sospesiView;
    private StatisticheView? _statisticheView;

    public MainView()
    {
        InitializeComponent();
        ConfiguraNavigazione();
        Caricamento();
    }

    // ------------------------------------------------------------------ NAVIGAZIONE (touch vs desktop)

    /// <summary>
    /// Desktop: tab in alto + footer diagnostico (invariato). Touch: nasconde la
    /// striscia di tab e il footer, mostra la bottom navigation che pilota lo
    /// stesso TabControl. Il TabControl resta l'unico motore dei contenuti in
    /// entrambi i casi (vedi Tabs.SelectedIndex usato altrove).
    /// </summary>
    private void ConfiguraNavigazione()
    {
        // Mantiene evidenziata la voce attiva anche su cambi programmatici
        // (es. AvviaQuizView forza SelectedIndex = 0).
        Tabs.SelectionChanged += (_, _) => AggiornaNav();

        if (!AppEnv.TouchMode) return;

        Tabs.Classes.Add("touchnav");   // nasconde la striscia di tab (vedi MainView.axaml)
        FooterBar.IsVisible = false;    // footer diagnostico solo su desktop
        BottomNav.IsVisible = true;
        AggiornaNav();
    }

    private void OnNavClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tag } && int.TryParse(tag, out int indice))
            Tabs.SelectedIndex = indice;
    }

    /// <summary>Evidenzia la voce della bottom nav corrispondente alla tab attiva.</summary>
    private void AggiornaNav()
    {
        if (!AppEnv.TouchMode) return;
        var labels = new[] { NavHomeLabel, NavCronologiaLabel, NavSospesiLabel, NavStatisticheLabel };
        for (int i = 0; i < labels.Length; i++)
        {
            bool attiva = i == Tabs.SelectedIndex;
            labels[i].FontWeight = attiva ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal;
            labels[i].Opacity = attiva ? 1.0 : 0.6;
        }
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
                case 3: (StatisticheArea.Content as Control)?.Focus(); break;
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    // ------------------------------------------------------------------ BOOT

    private void Caricamento()
    {
        string cartellaUtente = StorageService.CartellaUtenteDefault();

        // Scelta della sorgente dati read-only:
        // - desktop/CLI: i JSON sono copiati nel bin → file system (identico a prima).
        // - Android: nessun file accanto all'eseguibile → risorse embedded (avares://).
        string cartellaDati = AppContext.BaseDirectory;
        IFonteDati fonte;
        string origine;
        if (File.Exists(Path.Combine(cartellaDati, "materie.json")))
        {
            fonte = new FileSystemFonteDati(cartellaDati);
            origine = $"file system: {cartellaDati}";
        }
        else
        {
            fonte = new AvaloniaResourceFonteDati();
            origine = "risorse embedded (avares://)";
        }
        FooterText.Text = $"Dati read-only: {origine}    ·    Cartella utente: {cartellaUtente}";

        try
        {
            _storage = new StorageService(fonte, cartellaUtente);
            _materie = _storage.CaricaMaterie();
            _tutteDomande = _storage.CaricaTutteLeDomande(_materie);
            _mappaDomande = _tutteDomande.ToDictionary(d => d.Id, d => d);

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

            // ---------- Tab Statistiche ----------
            _statisticheView = new StatisticheView();
            _statisticheView.Inizializza(_storage);
            StatisticheArea.Content = _statisticheView;
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
                _statisticheView?.Ricarica();
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
            var sessione = SessioneQuiz.RiprendiDa(pausa, _mappaDomande);
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
