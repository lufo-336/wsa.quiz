using Avalonia.Controls;
using Avalonia.Input;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Dialog modale riutilizzabile di conferma. Due uscite: true (conferma) o
/// false/null (annulla). ESC chiude come annulla. Il testo dei bottoni e il
/// colore del bottone di conferma sono configurabili via costruttore.
/// </summary>
public partial class ConfermaDialog : Window
{
    /// <summary>True se l'utente ha premuto il bottone di conferma; false/null altrimenti.
    /// Da leggere dopo <c>ShowDialog</c> (che restituisce <c>Task</c>, non il risultato).</summary>
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
                Close();
            }
        };
    }

    public ConfermaDialog(string titolo, string messaggio, string dettaglio,
                          string confermaText, string confermaClasse)
        : this()
    {
        Title = titolo;
        MessaggioText.Text = messaggio;
        DettaglioText.Text = dettaglio;
        ConfermaBtn.Content = confermaText;
        ConfermaBtn.Classes.Add(confermaClasse);
    }

    private void OnAnnullaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confermato = false;
        Close();
    }

    private void OnConfermaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confermato = true;
        Close();
    }
}
