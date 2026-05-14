using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Wsa.Quiz.App.Views;

public enum PausaDialogResult
{
    Annulla,
    Abbandona,
    Salva
}

/// <summary>
/// Modale pausa unificata (step 6). Tre uscite: Annulla (default + ESC),
/// Abbandona, Salva e esci. Frecce sx/dx muovono il focus ciclicamente fra i
/// 3 bottoni; Ctrl+Tab e' bloccato per non cambiare tab con la modale aperta.
/// </summary>
public partial class PausaDialog : Window
{
    public PausaDialogResult Risultato { get; private set; } = PausaDialogResult.Annulla;

    public PausaDialog()
    {
        InitializeComponent();

        var bottoniInOrdine = new Button[] { AbbandonaBtn, AnnullaBtn, SalvaBtn };
        KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape)
            {
                ke.Handled = true;
                Risultato = PausaDialogResult.Annulla;
                Close();
                return;
            }
            if (ke.Key == Key.Tab && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ke.Handled = true;
                return;
            }
            if (ke.Key == Key.Left || ke.Key == Key.Right)
            {
                int idx = Array.IndexOf(bottoniInOrdine,
                    FocusManager?.GetFocusedElement() as Button);
                if (idx < 0) idx = 1; // default su Annulla
                int delta = ke.Key == Key.Right ? +1 : -1;
                int nuovo = (idx + delta + bottoniInOrdine.Length) % bottoniInOrdine.Length;
                bottoniInOrdine[nuovo].Focus();
                ke.Handled = true;
            }
        };

        Opened += (_, _) => AnnullaBtn.Focus();
    }

    private void OnAbbandonaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Risultato = PausaDialogResult.Abbandona;
        Close();
    }

    private void OnAnnullaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Risultato = PausaDialogResult.Annulla;
        Close();
    }

    private void OnSalvaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Risultato = PausaDialogResult.Salva;
        Close();
    }
}
