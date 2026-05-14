namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Snapshot serializzato di una sessione in pausa.
/// </summary>
public class SessionePausa
{
    public string SessioneId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime DataOraPausa { get; set; } = DateTime.Now;
    public OpzioniQuiz Opzioni { get; set; } = new();
    public bool ModalitaRotazione { get; set; }
    public List<string> CodaDomandeIds { get; set; } = new();
    public Dictionary<string, int> TentativiPerId { get; set; } = new();
    public List<string> CorretteIds { get; set; } = new();
    public int TotaleUnicheRotazione { get; set; }
    public int SbagliateContatore { get; set; }
    public int IndicePosizioneRotazione { get; set; }
    public int CorretteClassica { get; set; }
    public int EffettuateClassica { get; set; }
    public TimeSpan TempoTrascorso { get; set; }
    public List<DettaglioRisposta> Dettagli { get; set; } = new();
}
