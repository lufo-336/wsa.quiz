using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Platform;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App;

/// <summary>
/// <see cref="IFonteDati"/> che legge i dati read-only dalle risorse Avalonia
/// embedded nell'assembly <c>Wsa.Quiz.App</c> (cartella <c>Assets/</c>), via
/// <see cref="AssetLoader"/> e URI <c>avares://</c>. È il percorso usato su
/// Android, dove non esiste un file system di dati accanto all'eseguibile: le
/// risorse viaggiano dentro l'APK. Funziona identico anche su desktop (stesso
/// meccanismo), quindi è testabile lì.
/// </summary>
public sealed class AvaloniaResourceFonteDati : IFonteDati
{
    private const string Base = "avares://Wsa.Quiz.App/Assets/";

    private static Uri Uri(string percorsoRelativo)
        => new Uri(Base + percorsoRelativo.Replace('\\', '/'));

    public bool Esiste(string percorsoRelativo) => AssetLoader.Exists(Uri(percorsoRelativo), null);

    public string LeggiTesto(string percorsoRelativo)
    {
        using var stream = AssetLoader.Open(Uri(percorsoRelativo), null);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public IEnumerable<string> ElencaJson(string cartellaRelativa)
    {
        string rel = cartellaRelativa.Replace('\\', '/').TrimEnd('/');
        Uri dir = new Uri($"{Base}{rel}/");

        IEnumerable<Uri> assets;
        try
        {
            assets = AssetLoader.GetAssets(dir, null);
        }
        catch (Exception)
        {
            // Cartella/risorsa inesistente: nessuna domanda per questa materia.
            yield break;
        }

        foreach (var u in assets)
        {
            string s = u.ToString();
            if (!s.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            // Riconverte l'URI assoluto avares:// in percorso relativo alla radice dati.
            yield return s.StartsWith(Base, StringComparison.OrdinalIgnoreCase)
                ? s.Substring(Base.Length)
                : s;
        }
    }
}
