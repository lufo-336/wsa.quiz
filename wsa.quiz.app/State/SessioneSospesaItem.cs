using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di una riga della tabella sospesi. Espone proprieta' formattate
/// per il binding XAML (data pausa, modalita' label, avanzamento parziale).
/// E' observable solo per supportare la conferma inline dell'azione Elimina:
/// <see cref="InAttesaConfermaEliminazione"/> commuta i bottoni della riga
/// fra "Riprendi/Elimina" e "Si', elimina/Annulla".
/// </summary>
public class SessioneSospesaItem : ObservableObject
{
    /// <summary>Riferimento alla pausa originale, usata per il bottone Riprendi (step 4) e per Elimina.</summary>
    public SessionePausa Pausa { get; }

    public string SessioneId => Pausa.SessioneId;
    public string DataFormattata { get; }
    public string Modalita { get; }
    public string MateriaEtichetta { get; }
    public string Avanzamento { get; }
    public string TempoTrascorsoFormattato { get; }
    public string TipoEtichetta { get; }

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

    public SessioneSospesaItem(SessionePausa p)
    {
        Pausa = p;

        DataFormattata = p.DataOraPausa.ToString("dd MMM yyyy · HH:mm");

        Modalita = string.IsNullOrWhiteSpace(p.Opzioni.NomeModalita)
            ? (p.ModalitaRotazione ? "Rotazione" : "Classica")
            : p.Opzioni.NomeModalita;

        MateriaEtichetta = string.IsNullOrWhiteSpace(p.Opzioni.MateriaNomeLabel)
            ? "—"
            : p.Opzioni.MateriaNomeLabel;

        if (p.ModalitaRotazione)
        {
            int corrette = p.CorretteIds?.Count ?? 0;
            int totale = p.TotaleUnicheRotazione;
            Avanzamento = totale > 0
                ? $"{corrette}/{totale} padroneggiate · {p.SbagliateContatore} sbagliate"
                : $"{corrette} padroneggiate";
            TipoEtichetta = "Rotazione";
        }
        else
        {
            int fatte = p.EffettuateClassica;
            int rimaste = p.CodaDomandeIds?.Count ?? 0;
            int totale = fatte + rimaste;
            Avanzamento = totale > 0
                ? $"{fatte}/{totale} risposte"
                : $"{fatte} risposte";
            TipoEtichetta = "Classica";
        }

        var t = p.TempoTrascorso;
        TempoTrascorsoFormattato = t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }
}
