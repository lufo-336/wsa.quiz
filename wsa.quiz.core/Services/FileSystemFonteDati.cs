using System.Text;

namespace Wsa.Quiz.Core.Services;

/// <summary>
/// <see cref="IFonteDati"/> che legge da una cartella del file system (uso
/// desktop/CLI: i JSON sono copiati nel bin accanto all'eseguibile). Conserva
/// il comportamento storico di <see cref="StorageService"/> prima di M2,
/// inclusa la validazione anti directory-traversal.
/// </summary>
public sealed class FileSystemFonteDati : IFonteDati
{
    private readonly string _cartellaBase;

    public FileSystemFonteDati(string cartellaBase)
    {
        _cartellaBase = Path.GetFullPath(cartellaBase);
    }

    /// <summary>
    /// Risolve un percorso relativo in assoluto e verifica che resti dentro la
    /// cartella base (no path traversal con "..").
    /// </summary>
    private string Risolvi(string percorsoRelativo)
    {
        string full = Path.GetFullPath(Path.Combine(_cartellaBase, percorsoRelativo));
        string baseConSep = _cartellaBase.EndsWith(Path.DirectorySeparatorChar)
            ? _cartellaBase
            : _cartellaBase + Path.DirectorySeparatorChar;
        if (!full.StartsWith(baseConSep, StringComparison.Ordinal) &&
            !string.Equals(full, _cartellaBase, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Percorso '{percorsoRelativo}' fuori dalla cartella dati.", nameof(percorsoRelativo));
        }
        return full;
    }

    public bool Esiste(string percorsoRelativo) => File.Exists(Risolvi(percorsoRelativo));

    public string LeggiTesto(string percorsoRelativo)
    {
        // FileShare.Read: consente letture concorrenti, blocca scritture.
        using var stream = new FileStream(Risolvi(percorsoRelativo), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public IEnumerable<string> ElencaJson(string cartellaRelativa)
    {
        string dir = Risolvi(cartellaRelativa);
        if (!Directory.Exists(dir))
            yield break;

        string prefisso = cartellaRelativa.Replace('\\', '/').TrimEnd('/');
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            yield return $"{prefisso}/{Path.GetFileName(file)}";
    }
}
