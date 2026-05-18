using Avalonia.Media;
using Wsa.Quiz.Core.Models.Statistiche;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper per una cella della heatmap statistiche: espone etichetta
/// formattata e i brush di bordo/sfondo dipendenti dalla PctErrore.
/// </summary>
public class CategoriaCellaItem
{
    public CategoriaStat Stat { get; }

    public CategoriaCellaItem(CategoriaStat stat)
    {
        Stat = stat;
    }

    public string Etichetta => $"{Stat.Categoria} {Stat.PctErrore:P0}";
    public string Tooltip => $"{Stat.Errori} errori su {Stat.Visti} viste";
    public IBrush ColoreBordo => QuizColors.HeatmapBordo(Stat.PctErrore);
    public IBrush ColoreSfondo => QuizColors.HeatmapSfondo(Stat.PctErrore);
}
