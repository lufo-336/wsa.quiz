using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Schermata di esecuzione del quiz. Riceve nel costruttore una <see cref="SessioneQuiz"/>
/// gia' configurata (ma non ancora avviata) e la usa come DataContext.
/// La sessione viene avviata in <c>OnAttachedToVisualTree</c>, cosi' il cronometro
/// parte solo quando la view e' effettivamente visibile.
///
/// Tastiera (step 4): A/B/C/D selezionano la risposta, Invio avanza, ESC apre il
/// menu pausa modale (Riprendi / Salva e esci / Annulla).
/// </summary>
public partial class QuizView : UserControl
{
    /// <summary>Sollevato quando la sessione termina (regolarmente o per abbandono).</summary>
    public event EventHandler<SessioneQuiz>? QuizConcluso;

    /// <summary>
    /// Sollevato quando l'utente ha scelto "Salva e esci" dal menu pausa: la
    /// pausa e' gia' stata persistita, la <c>MainWindow</c> deve solo navigare.
    /// </summary>
    public event EventHandler? QuizMessoInPausa;

    private readonly SessioneQuiz _sessione;
    private readonly StorageService _storage;
    private bool _avviata;

    public QuizView(SessioneQuiz sessione, StorageService storage)
    {
        InitializeComponent();
        _sessione = sessione;
        _storage = storage;
        DataContext = sessione;
        _sessione.Concluso += OnSessioneConclusa;
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_avviata)
        {
            _sessione.Avvia();
            _avviata = true;
        }
        // Focus sulla view per ricevere la tastiera (A/B/C/D, Invio, ESC).
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.A: case Key.B: case Key.C: case Key.D:
                if (_sessione.InAttesaRisposta)
                {
                    int idx = e.Key - Key.A;
                    if (idx < _sessione.Risposte.Count)
                        _sessione.RispondiA(idx);
                }
                e.Handled = true;
                return;

            case Key.Enter:
                if (_sessione.RispostaInviata) _sessione.Avanza();
                e.Handled = true;
                return;

            case Key.Escape:
                e.Handled = true;
                ApriMenuPausa();
                return;
        }

        base.OnKeyDown(e);
    }

    private void OnRispostaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RispostaItem r })
            _sessione.RispondiA(r.Indice);
    }

    private void OnAvanzaClick(object? sender, RoutedEventArgs e)
    {
        _sessione.Avanza();
    }

    private void OnPausaClick(object? sender, RoutedEventArgs e)
    {
        ApriMenuPausa();
    }

    /// <summary>
    /// Menu pausa: Riprendi (chiude e basta), Salva e esci (persiste pausa
    /// + solleva evento), Annulla (chiude e basta). ESC dentro la modale = Annulla.
    /// </summary>
    private async void ApriMenuPausa()
    {
        var w = new Window
        {
            Title = "Quiz in pausa",
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        bool salva = false;
        var contenuto = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 14 };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Quiz in pausa. Cosa vuoi fare?",
            FontSize = 14,
            FontWeight = global::Avalonia.Media.FontWeight.SemiBold
        });
        contenuto.Children.Add(new TextBlock
        {
            Text = "Salvando, la sessione resta nei Sospesi e potrai riprenderla dopo dalla relativa tab.",
            FontSize = 12,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Foreground = global::Avalonia.Media.Brushes.DimGray
        });
        var pulsantiera = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        var annullaBtn = new Button { Content = "Annulla", Padding = new global::Avalonia.Thickness(14, 5) };
        var salvaBtn = new Button { Content = "Salva e esci", Padding = new global::Avalonia.Thickness(14, 5) };
        var riprendiBtn = new Button { Content = "Riprendi" };
        riprendiBtn.Classes.Add("accent");
        riprendiBtn.Padding = new global::Avalonia.Thickness(14, 5);

        annullaBtn.Click += (_, _) => w.Close();
        riprendiBtn.Click += (_, _) => w.Close();
        salvaBtn.Click += (_, _) => { salva = true; w.Close(); };

        pulsantiera.Children.Add(annullaBtn);
        pulsantiera.Children.Add(salvaBtn);
        pulsantiera.Children.Add(riprendiBtn);
        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        // ESC dentro la modale = Annulla (chiude la finestra senza azione)
        w.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape) { ke.Handled = true; w.Close(); }
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null) await w.ShowDialog(owner);
        else w.Show();

        // Riprendo focus per le scorciatoie
        Focus();

        if (!salva) return;

        try
        {
            var pausa = _sessione.EsportaPausa();
            _storage.SalvaPausa(pausa);
        }
        catch (Exception ex)
        {
            // Se il salvataggio fallisce, segnalo e riprendo la sessione (cronometro fermo).
            // Una soluzione piu' raffinata mostrerebbe un dialog; per ora lascio la sessione
            // viva cosi' l'utente puo' riprovare con Salva e esci, e il timer ripartira'
            // alla prossima interazione che lo richiede. Tracciamento minimo.
            System.Diagnostics.Debug.WriteLine($"SalvaPausa failed: {ex}");
            return;
        }

        QuizMessoInPausa?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessioneConclusa(object? sender, EventArgs e)
    {
        QuizConcluso?.Invoke(this, _sessione);
    }
}
