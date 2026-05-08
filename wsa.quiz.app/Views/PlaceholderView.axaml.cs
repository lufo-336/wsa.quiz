using Avalonia.Controls;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Vista neutra per i tab non ancora popolati (Cronologia, Sospesi).
/// Verra' rimpiazzata negli step 3+.
/// </summary>
public partial class PlaceholderView : UserControl
{
    public PlaceholderView()
    {
        InitializeComponent();
    }

    public PlaceholderView(string titolo, string messaggio) : this()
    {
        TitoloText.Text = titolo;
        MessaggioText.Text = messaggio;
    }
}
