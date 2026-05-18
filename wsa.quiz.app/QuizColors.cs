using Avalonia.Media;

namespace Wsa.Quiz.App;

/// <summary>
/// Colori condivisi dell'applicazione. Centralizza i valori esadecimali
/// per eliminare duplicazioni e garantire coerenza visiva.
/// </summary>
public static class QuizColors
{
    public const string Verde     = "#1F7A4D";
    public const string Ambra     = "#B8860B";
    public const string Rosso     = "#B85450";
    public const string Focus     = "#FFD500";
    public const string Danger    = "#B85450";
    public const string DangerHover  = "#A04340";
    public const string DangerPressed = "#8C3B38";
    public const string DisabilitatoSfondo = "#D6D6D6";
    public const string DisabilitatoTesto  = "#7A7A7A";

    private static readonly IBrush _verdeBrush  = new SolidColorBrush(Color.Parse(Verde));
    private static readonly IBrush _ambraBrush  = new SolidColorBrush(Color.Parse(Ambra));
    private static readonly IBrush _rossoBrush  = new SolidColorBrush(Color.Parse(Rosso));

    // Sfondi tenui per le celle della heatmap statistiche (step 9).
    private static readonly IBrush _verdeBrushSfondo = new SolidColorBrush(Color.Parse("#E5F4EC"));
    private static readonly IBrush _ambraBrushSfondo = new SolidColorBrush(Color.Parse("#FFF4D6"));
    private static readonly IBrush _rossoBrushSfondo = new SolidColorBrush(Color.Parse("#F9E7E6"));

    /// <summary>
    /// Bordo cella heatmap in base alla percentuale di errore (0..1):
    /// verde &lt; 20%, ambra 20-59%, rosso &gt;= 60%.
    /// </summary>
    public static IBrush HeatmapBordo(double pctErrore)
    {
        if (pctErrore < 0.20) return _verdeBrush;
        if (pctErrore < 0.60) return _ambraBrush;
        return _rossoBrush;
    }

    /// <summary>
    /// Sfondo tenue cella heatmap, abbinato a <see cref="HeatmapBordo"/>.
    /// </summary>
    public static IBrush HeatmapSfondo(double pctErrore)
    {
        if (pctErrore < 0.20) return _verdeBrushSfondo;
        if (pctErrore < 0.60) return _ambraBrushSfondo;
        return _rossoBrushSfondo;
    }

    /// <summary>
    /// Restituisce il colore associato a una percentuale:
    /// verde &gt;= 80%, ambra 50-79%, rosso &lt; 50%.
    /// </summary>
    public static IBrush Percentuale(double pct)
    {
        if (pct >= 80) return _verdeBrush;
        if (pct >= 50) return _ambraBrush;
        return _rossoBrush;
    }

    /// <summary>
    /// Restituisce il colore di esito (verde per corretto, rosso per sbagliato).
    /// </summary>
    public static IBrush Esito(bool corretto)
    {
        return corretto ? _verdeBrush : _rossoBrush;
    }
}
