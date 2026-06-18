namespace Wsa.Quiz.Core.Services;

/// <summary>
/// Astrazione della sorgente dei dati read-only (materie.json + domande/**).
/// Permette di leggere gli stessi dati da origini diverse a seconda della
/// piattaforma: dal file system (desktop/CLI, dove i JSON sono copiati nel bin)
/// oppure dalle risorse embedded dell'assembly (Android, dove non esiste un
/// file system di dati accanto all'eseguibile).
/// I percorsi sono sempre <b>relativi</b> alla radice dei dati e usano '/'
/// come separatore (es. "materie.json", "domande/cpp/cpp_domande.json").
/// </summary>
public interface IFonteDati
{
    /// <summary>True se la risorsa al percorso relativo esiste.</summary>
    bool Esiste(string percorsoRelativo);

    /// <summary>Legge l'intero contenuto testuale (UTF-8) della risorsa.</summary>
    string LeggiTesto(string percorsoRelativo);

    /// <summary>
    /// Elenca i file *.json contenuti nella cartella relativa indicata,
    /// restituendo percorsi relativi alla radice dei dati (con '/'). Cartella
    /// inesistente o vuota: sequenza vuota (non lancia).
    /// </summary>
    IEnumerable<string> ElencaJson(string cartellaRelativa);
}
