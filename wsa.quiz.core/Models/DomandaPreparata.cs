namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Domanda con risposte già mescolate e indice della corretta ricalcolato.
/// Prodotta da QuizService.PreparaDomanda al momento di mostrare la domanda.
/// </summary>
public class DomandaPreparata
{
    public Domanda Originale { get; set; } = new();
    public List<string> RisposteShufflate { get; set; } = new();
    public int IndiceCorrettoShufflato { get; set; }

    public char LetteraCorretta => (char)('A' + IndiceCorrettoShufflato);
    public string TestoRispostaCorretta =>
        IndiceCorrettoShufflato >= 0 && IndiceCorrettoShufflato < RisposteShufflate.Count
            ? RisposteShufflate[IndiceCorrettoShufflato]
            : string.Empty;
}
