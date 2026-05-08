using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Schermata di esecuzione del quiz. Riceve nel costruttore una <see cref="SessioneQuiz"/>
/// gia' configurata (ma non ancora avviata) e la usa come DataContext.
/// La sessione viene avviata in <c>OnAttachedToVisualTree</c>, cosi' il cronometro
/// parte solo quando la view e' effettivamente visibile.
/// </summary>
public partial class QuizView : UserControl
{
    /// <summary>Sollevato quando la sessione termina (regolarmente o per abbandono).</summary>
    public event EventHandler<SessioneQuiz>? QuizConcluso;

    private readonly SessioneQuiz _sessione;
    private bool _avviata;

    public QuizView(SessioneQuiz sessione)
    {
        InitializeComponent();
        _sessione = sessione;
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

    private void OnAbbandonaClick(object? sender, RoutedEventArgs e)
    {
        // Conferma rapida via finestra modale
        ChiediConfermaAbbandono();
    }

    private async void ChiediConfermaAbbandono()
    {
        var w = new Window
        {
            Title = "Abbandona quiz",
            Width = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };
        bool conferma = false;
        var contenuto = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 14 };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Sei sicuro di voler abbandonare il quiz? Le risposte gia' date verranno comunque salvate in cronologia.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });
        var pulsantiera = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        var siBtn = new Button { Content = "Abbandona", Padding = new global::Avalonia.Thickness(14, 5) };
        var noBtn = new Button { Content = "Continua", Padding = new global::Avalonia.Thickness(14, 5) };
        siBtn.Click += (_, _) => { conferma = true; w.Close(); };
        noBtn.Click += (_, _) => w.Close();
        pulsantiera.Children.Add(noBtn);
        pulsantiera.Children.Add(siBtn);
        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        if (conferma) _sessione.Abbandona();
    }

    private void OnSessioneConclusa(object? sender, EventArgs e)
    {
        QuizConcluso?.Invoke(this, _sessione);
    }
}
