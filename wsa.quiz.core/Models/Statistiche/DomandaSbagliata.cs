namespace Wsa.Quiz.Core.Models.Statistiche;

/// <summary>
/// Domanda sbagliata almeno una volta in una data categoria,
/// con conteggio di quante volte è stata vista e quante sbagliata.
/// </summary>
public record DomandaSbagliata(
    string IdDomanda,
    string Testo,
    string RispostaCorretta,
    string Spiegazione,
    int VolteSbagliata,
    int VolteVista);
