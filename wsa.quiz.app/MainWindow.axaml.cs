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
/// il TabControl con le tre sezioni e la transizione fra Home / Quiz / Riepilogo
/// all'interno del tab "Home".
/// </summary>
public partial class MainWindow : Window
{
    private StorageService? _storage;
    private List<Materia> _materie = new();
    private List<Domanda> _tutteDomande = new();

    private HomeView? _homeView;

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

            _homeView = new HomeView();
            _homeView.Inizializza(_materie, _tutteDomande);
            _homeView.QuizAvviato += OnQuizAvviato;
            HomeArea.Content = _homeView;

            // Tab placeholder per ora (popolati negli step 3+)
            CronologiaArea.Content = new PlaceholderView(
                "Cronologia",
                "La sezione Cronologia verra' popolata nello step 3. La cronologia salvata dal console e dall'app GUI condivide gia' lo stesso file.");
            SospesiArea.Content = new PlaceholderView(
                "Sospesi",
                "La sezione Sospesi verra' popolata nello step 3. Lo storage e' gia' condiviso col console.");
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

    // ------------------------------------------------------------------ NAVIGAZIONE

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
