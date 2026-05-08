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
}
