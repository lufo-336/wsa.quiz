namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Risultato completo di una sessione di quiz. Persistito in cronologia.json.
/// </summary>
public class RisultatoQuiz
{
    public DateTime DataOra { get; set; }
    public string Modalita { get; set; } = string.Empty;
    public string MateriaNome { get; set; } = string.Empty;
    public List<string> CategorieSelezionate { get; set; } = new();
    public int TotaleDomande { get; set; }
    public int RisposteCorrette { get; set; }
    public int RisposteErrate { get; set; }
    public double PercentualeCorrette { get; set; }
    public TimeSpan DurataQuiz { get; set; }
    public bool Abbandonato { get; set; }
    public bool ModalitaRotazione { get; set; }
    public bool CronometroAttivo { get; set; }
    public int Punteggio { get; set; }
    public List<DettaglioRisposta> Dettagli { get; set; } = new();
}
