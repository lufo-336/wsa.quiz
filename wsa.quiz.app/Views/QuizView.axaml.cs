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
/// menu pausa modale unificato (Annulla / Abbandona / Salva e esci) — anche il
/// bottone "Pausa" del header apre la stessa modale (step 6).
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
                if (!_sessione.InViewMode && _sessione.InAttesaRisposta)
                {
                    int idx = e.Key - Key.A;
                    if (idx < _sessione.Risposte.Count)
                        _sessione.RispondiA(idx);
                }
                e.Handled = true;
                return;

            case Key.Up:
                if (!_sessione.InViewMode) _sessione.HighlightSu();
                e.Handled = true;
                return;

            case Key.Down:
                if (!_sessione.InViewMode) _sessione.HighlightGiu();
                e.Handled = true;
                return;

            case Key.Left:
                _sessione.VaiAPassataPrecedente();
                e.Handled = true;
                return;

            case Key.Right:
                _sessione.VaiAPassataSuccessiva();
                e.Handled = true;
                return;

            case Key.Enter:
                if (!_sessione.InViewMode)
                {
                    if (!_sessione.ConfermaHighlight() && _sessione.RispostaInviata)
                        _sessione.Avanza();
                }
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

    private void OnTornaACorrenteClick(object? sender, RoutedEventArgs e)
    {
        _sessione.TornaACorrente();
        Focus();
    }

    /// <summary>
    /// Modale pausa unificata (step 6). Tre uscite possibili veicolate da una
    /// variabile locale <c>azione</c>: <c>"annulla"</c> (default + ESC), <c>"abbandona"</c>,
    /// <c>"salva"</c>. Layout: Abbandona (danger) a sinistra, Annulla + Salva e esci
    /// (accent) a destra. Default focus su Annulla.
    /// </summary>
    private async void ApriMenuPausa()
    {
        var w = new Window
        {
            Title = "Quiz in pausa",
            Width = 480,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        string azione = "annulla";

        var contenuto = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 12 };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Quiz in pausa. Cosa vuoi fare?",
            FontSize = 14,
            FontWeight = global::Avalonia.Media.FontWeight.SemiBold
        });
        contenuto.Children.Add(new TextBlock
        {
            Text = "Annulla riprende il quiz. Abbandona lo chiude e lo registra in cronologia come abbandonato. Salva e esci lo mette nei Sospesi: lo riprenderai dalla relativa tab.",
            FontSize = 12,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Foreground = global::Avalonia.Media.Brushes.DimGray
        });

        // Pulsantiera: Abbandona (sx, danger) | spazio | Annulla, Salva e esci (dx, accent)
        var pulsantiera = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new global::Avalonia.Thickness(0, 4, 0, 0)
        };

        var abbandonaBtn = new Button { Content = "Abbandona", Padding = new global::Avalonia.Thickness(14, 5) };
        abbandonaBtn.Classes.Add("danger");
        Grid.SetColumn(abbandonaBtn, 0);

        var destra = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        Grid.SetColumn(destra, 2);

        var annullaBtn = new Button { Content = "Annulla", Padding = new global::Avalonia.Thickness(14, 5) };
        var salvaBtn = new Button { Content = "Salva e esci", Padding = new global::Avalonia.Thickness(14, 5) };
        salvaBtn.Classes.Add("accent");

        abbandonaBtn.Click += (_, _) => { azione = "abbandona"; w.Close(); };
        annullaBtn.Click += (_, _) => { azione = "annulla"; w.Close(); };
        salvaBtn.Click += (_, _) => { azione = "salva"; w.Close(); };

        destra.Children.Add(annullaBtn);
        destra.Children.Add(salvaBtn);

        pulsantiera.Children.Add(abbandonaBtn);
        pulsantiera.Children.Add(destra);

        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        // Step 8: gestione tastiera estesa nella modale pausa.
        // ESC = Annulla; frecce sinistra/destra muovono il focus fra i 3 bottoni
        // in modo ciclico (ordine visivo: Abbandona, Annulla, Salva); Ctrl+Tab
        // bloccato per non cambiare tab con la modale aperta.
        var bottoniInOrdine = new Button[] { abbandonaBtn, annullaBtn, salvaBtn };
        w.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape)
            {
                ke.Handled = true;
                azione = "annulla";
                w.Close();
                return;
            }
            if (ke.Key == Key.Tab && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                // Blocca Ctrl+Tab dentro la modale: niente cambio tab globale.
                ke.Handled = true;
                return;
            }
            if (ke.Key == Key.Left || ke.Key == Key.Right)
            {
                int idx = System.Array.IndexOf(bottoniInOrdine,
                    w.FocusManager?.GetFocusedElement() as Button);
                if (idx < 0) idx = 1; // default su Annulla
                int delta = ke.Key == Key.Right ? +1 : -1;
                int nuovo = (idx + delta + bottoniInOrdine.Length) % bottoniInOrdine.Length;
                bottoniInOrdine[nuovo].Focus();
                ke.Handled = true;
            }
        };

        // Focus di default su Annulla quando la finestra si apre
        w.Opened += (_, _) => annullaBtn.Focus();

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null) await w.ShowDialog(owner);
        else w.Show();

        // Riprendo focus per le scorciatoie del Quiz
        Focus();

        switch (azione)
        {
            case "abbandona":
                _sessione.Abbandona();
                return;

            case "salva":
                try
                {
                    var pausa = _sessione.EsportaPausa();
                    _storage.SalvaPausa(pausa);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SalvaPausa failed: {ex}");
                    return;
                }
                QuizMessoInPausa?.Invoke(this, EventArgs.Empty);
                return;

            default: // "annulla"
                return;
        }
    }

    private void OnSessioneConclusa(object? sender, EventArgs e)
    {
        QuizConcluso?.Invoke(this, _sessione);
    }
}
