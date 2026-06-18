using Avalonia.Input;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Dialog modale riutilizzabile di conferma, reso come overlay in-page (M3).
/// Due uscite: true (conferma) o false (annulla). ESC chiude come annulla. Il
/// testo dei bottoni e il colore del bottone di conferma sono configurabili via
/// costruttore. Si legge <see cref="Confermato"/> dopo
/// <c>await ShowOverlayAsync(...)</c>.
/// </summary>
public partial class ConfermaDialog : OverlayDialogBase
{
    /// <summary>True se l'utente ha premuto il bottone di conferma; false altrimenti.</summary>
    public bool Confermato { get; private set; }

    public ConfermaDialog()
    {
        InitializeComponent();

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Confermato = false;
                Chiudi();
            }
        };
    }

    public ConfermaDialog(string titolo, string messaggio, string dettaglio,
                          string confermaText, string confermaClasse)
        : this()
    {
        // 'titolo' non ha più una title bar (overlay): lo ignoriamo, il messaggio
        // principale fa da intestazione.
        _ = titolo;
        MessaggioText.Text = messaggio;
        DettaglioText.Text = dettaglio;
        ConfermaBtn.Content = confermaText;
        ConfermaBtn.Classes.Add(confermaClasse);
    }

    protected override void OnShown() => AnnullaBtn.Focus();

    private void OnAnnullaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confermato = false;
        Chiudi();
    }

    private void OnConfermaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confermato = true;
        Chiudi();
    }
}
