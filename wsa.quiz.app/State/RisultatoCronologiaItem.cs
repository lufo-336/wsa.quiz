using Avalonia.Media;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di una riga della tabella cronologia. Espone proprieta' gia' formattate
/// per il binding XAML (data, durata, etichetta materie, colore della percentuale).
/// E' observable per supportare la conferma inline dell'azione Elimina:
/// <see cref="InAttesaConfermaEliminazione"/> commuta i bottoni della riga
/// fra "Elimina" e "Si', elimina/Annulla" (stesso pattern di <see cref="SessioneSospesaItem"/>).
/// </summary>
public class RisultatoCronologiaItem : ObservableObject
{
    /// <summary>Riferimento al risultato originale, usato per aprire il dettaglio e per Elimina.</summary>
    public RisultatoQuiz Risultato { get; }

    /// <summary>Id del risultato, propagato per comodita' sui binding/handler.</summary>
    public string Id => Risultato.Id;

    public string DataFormattata { get; }
    public string Modalita { get; }
    public string MateriaEtichetta { get; }
    public string PercentualeFormattata { get; }
    public IBrush ColorePercentuale { get; }
    public string DurataFormattata { get; }
    public string Conteggio { get; }
    public bool Abbandonato { get; }
    public string StatoEtichetta { get; }

    // ------------------------------------------------------------------ STATO CONFERMA ELIMINA

    private bool _inAttesaConferma;
    public bool InAttesaConfermaEliminazione
    {
        get => _inAttesaConferma;
        set
        {
            if (SetField(ref _inAttesaConferma, value))
                RaisePropertyChanged(nameof(NonInAttesaConferma));
        }
    }

    /// <summary>Inverso di <see cref="InAttesaConfermaEliminazione"/>, comodo per l'XAML.</summary>
    public bool NonInAttesaConferma => !_inAttesaConferma;

    // ------------------------------------------------------------------ COSTRUZIONE

    public RisultatoCronologiaItem(RisultatoQuiz r)
    {
        Risultato = r;

        DataFormattata = r.DataOra.ToString("dd MMM yyyy · HH:mm");

        Modalita = string.IsNullOrWhiteSpace(r.Modalita) ? "—" : r.Modalita;

        MateriaEtichetta = string.IsNullOrWhiteSpace(r.MateriaNome) ? "—" : r.MateriaNome;

        PercentualeFormattata = $"{System.Math.Round(r.PercentualeCorrette)}%";
        ColorePercentuale = QuizColors.Percentuale(r.PercentualeCorrette);

        var d = r.DurataQuiz;
        DurataFormattata = d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
            : $"{d.Minutes:00}:{d.Seconds:00}";

        Conteggio = $"{r.RisposteCorrette} / {r.TotaleDomande}";

        Abbandonato = r.Abbandonato;
        StatoEtichetta = r.Abbandonato ? "Abbandonato" : "Concluso";
    }

}
