namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Dettaglio di una singola risposta data durante un quiz. Salvato in cronologia.
/// </summary>
public class DettaglioRisposta
{
    public string IdDomanda { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string MateriaNome { get; set; } = string.Empty;
    public string TestoDomanda { get; set; } = string.Empty;
    public string RispostaData { get; set; } = string.Empty;
    public string RispostaCorretta { get; set; } = string.Empty;
    public bool Corretta { get; set; }
    public string Spiegazione { get; set; } = string.Empty;

    /// <summary>Numero di tentativi prima che fosse data la risposta corretta (modalità rotazione). 1 = al primo colpo.</summary>
    public int Tentativi { get; set; } = 1;

    // ----- Step 7: campi per ricostruire i 4 bottoni A/B/C/D in view-mode -----
    // Additivi: i record JSON esistenti caricano con default vuoto/-1. Ignorati
    // dal CronologiaDettaglioView. Popolati in SessioneQuiz.RispondiA.

    /// <summary>Le 4 risposte nell'ordine mostrato all'utente (post-shuffle).</summary>
    public List<string> RisposteShufflate { get; set; } = new();

    /// <summary>Indice (0..3) della risposta corretta nell'ordine shufflato.</summary>
    public int IndiceCorrettoShufflato { get; set; } = -1;

    /// <summary>Indice (0..3) della risposta data dall'utente nell'ordine shufflato.</summary>
    public int IndiceDataShufflato { get; set; } = -1;
}
