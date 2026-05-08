using System.Text.Json.Serialization;

namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Descrive una materia del quiz. Letta da materie.json.
/// </summary>
public class Materia
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("cartella")]
    public string Cartella { get; set; } = string.Empty;

    /// <summary>
    /// "standard" = lista root con campi italiani ("domanda", "risposte", "rispostaCorretta").
    /// "nested"   = {quiz:{questions:[{question, options, correct_index, ...}]}} in inglese.
    /// </summary>
    [JsonPropertyName("formato")]
    public string Formato { get; set; } = "standard";
}
