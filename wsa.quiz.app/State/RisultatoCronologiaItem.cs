using Avalonia.Media;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di una riga della tabella cronologia. Espone proprieta' gia' formattate
/// per il binding XAML (data, durata, etichetta materie, colore della percentuale).
/// Costruito una sola volta dal <see cref="Wsa.Quiz.App.Views.CronologiaView"/>;
/// non cambia dopo la creazione, quindi non implementa INPC.
/// </summary>
public class RisultatoCronologiaItem
{
    /// <summary>Riferimento al risultato originale, usato per aprire il dettaglio.</summary>
    public RisultatoQuiz Risultato { get; }

    public string DataFormattata { get; }
    public string Modalita { get; }
    public string MateriaEtichetta { get; }
    public string PercentualeFormattata { get; }
    public IBrush ColorePercentuale { get; }
    public string DurataFormattata { get; }
    public string Conteggio { get; }
    public bool Abbandonato { get; }
    public string StatoEtichetta { get; }

    public RisultatoCronologiaItem(RisultatoQuiz r)
    {
        Risultato = r;

        DataFormattata = r.DataOra.ToString("dd MMM yyyy · HH:mm");

        Modalita = string.IsNullOrWhiteSpace(r.Modalita) ? "—" : r.Modalita;

        MateriaEtichetta = string.IsNullOrWhiteSpace(r.MateriaNome) ? "—" : r.MateriaNome;

        PercentualeFormattata = $"{System.Math.Round(r.PercentualeCorrette)}%";
        ColorePercentuale = ColoreDaPercentuale(r.PercentualeCorrette);

        var d = r.DurataQuiz;
        DurataFormattata = d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
            : $"{d.Minutes:00}:{d.Seconds:00}";

        Conteggio = $"{r.RisposteCorrette} / {r.TotaleDomande}";

        Abbandonato = r.Abbandonato;
        StatoEtichetta = r.Abbandonato ? "Abbandonato" : "Concluso";
    }

    private static IBrush ColoreDaPercentuale(double pct)
    {
        // Stessa scala di RiepilogoView: verde >=80, ambra 50-79, rosso <50.
        if (pct >= 80) return new SolidColorBrush(Color.Parse("#1F7A4D"));
        if (pct >= 50) return new SolidColorBrush(Color.Parse("#B8860B"));
        return new SolidColorBrush(Color.Parse("#B85450"));
    }
}
