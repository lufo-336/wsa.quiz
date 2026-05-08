using System;
using System.Collections.Generic;
using Avalonia.Controls;
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
        var quizView = new QuizView(sessione);
        quizView.QuizConcluso += OnQuizConcluso;
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

        var riep = new RiepilogoView(sessione.Risultato);
        riep.TornaAllaHome += (_, _) => RitornaAllaHome();
        HomeArea.Content = riep;
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
