using System.Text.Json.Serialization;

namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Singola domanda del quiz.
/// </summary>
public class Domanda
{
    /// <summary>
    /// ID stabile della domanda. Calcolato come hash SHA256 (12 char hex) del
    /// contenuto (testo + risposte + indice corretta) durante il caricamento.
    /// Sopravvive a riordini e inserimenti nei file JSON: stesso contenuto = stesso ID.
    /// Eventuali "id" presenti nel JSON vengono ignorati.
    /// </summary>
    [JsonIgnore]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("domanda")]
    public string DomandaTesto { get; set; } = string.Empty;

    [JsonPropertyName("risposte")]
    public List<string> Risposte { get; set; } = new();

    /// <summary>Indice 0-based della risposta corretta nell'array Risposte.</summary>
    [JsonPropertyName("rispostaCorretta")]
    public int RispostaCorretta { get; set; }

    [JsonPropertyName("spiegazione")]
    public string Spiegazione { get; set; } = string.Empty;

    /// <summary>
    /// ID della materia (es. "cpp", "sql"). Non presente nel JSON, viene
    /// assegnato dallo StorageService in fase di caricamento.
    /// </summary>
    [JsonIgnore]
    public string MateriaId { get; set; } = string.Empty;

    /// <summary>Nome umano della materia (es. "C / C++"). Assegnato al caricamento.</summary>
    [JsonIgnore]
    public string MateriaNome { get; set; } = string.Empty;
}
