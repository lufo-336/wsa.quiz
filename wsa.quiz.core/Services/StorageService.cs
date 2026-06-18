using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.Core.Services;

/// <summary>
/// Gestisce la persistenza: lettura materie + domande, scrittura cronologia, gestione sospesi.
/// Separa la cartella dei dati read-only (materie.json, domande/) da quella dei dati utente
/// (cronologia.json, quiz_in_pausa.json) cosi' console e app GUI possono condividere lo stesso
/// storico anche se hanno bin/output diversi.
/// </summary>
public class StorageService
{
    private readonly IFonteDati _fonte;
    private readonly string _cartellaUtente;
    private readonly string _fileCronologia;
    private readonly string _filePausa;

    private static readonly JsonSerializerOptions OpzioniScrittura = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions OpzioniLettura = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 64
    };

    /// <summary>
    /// Costruttore "monolitico" (uso legacy): stessa cartella per dati read-only e dati utente.
    /// </summary>
    public StorageService(string cartella) : this(cartella, cartella) { }

    /// <summary>
    /// Costruttore con separazione fra dati read-only (materie, domande) e dati utente (cronologia, sospesi),
    /// leggendo i dati read-only dal file system. Usato da desktop e CLI.
    /// </summary>
    /// <param name="cartellaDati">Cartella che contiene materie.json e la sottocartella domande/.</param>
    /// <param name="cartellaUtente">Cartella dove leggere/scrivere cronologia.json e quiz_in_pausa.json.</param>
    public StorageService(string cartellaDati, string cartellaUtente)
        : this(new FileSystemFonteDati(cartellaDati), cartellaUtente) { }

    /// <summary>
    /// Costruttore generico: i dati read-only arrivano da una <see cref="IFonteDati"/>
    /// qualsiasi (file system o risorse embedded su Android), i dati utente da cartella.
    /// </summary>
    /// <param name="fonte">Sorgente dei dati read-only (materie + domande).</param>
    /// <param name="cartellaUtente">Cartella dove leggere/scrivere cronologia.json e quiz_in_pausa.json.</param>
    public StorageService(IFonteDati fonte, string cartellaUtente)
    {
        _fonte = fonte;
        _cartellaUtente = Path.GetFullPath(cartellaUtente);
        _fileCronologia = Path.GetFullPath(Path.Combine(_cartellaUtente, "cronologia.json"));
        _filePausa      = Path.GetFullPath(Path.Combine(_cartellaUtente, "quiz_in_pausa.json"));

        // Validazione anti-directory-traversal sui file utente.
        static string ConSep(string dir) => dir.EndsWith(Path.DirectorySeparatorChar) ? dir : dir + Path.DirectorySeparatorChar;
        if (!_fileCronologia.StartsWith(ConSep(_cartellaUtente), StringComparison.Ordinal))
            throw new ArgumentException("Percorso cronologia.json fuori dalla cartella utente.", nameof(cartellaUtente));
        if (!_filePausa.StartsWith(ConSep(_cartellaUtente), StringComparison.Ordinal))
            throw new ArgumentException("Percorso quiz_in_pausa.json fuori dalla cartella utente.", nameof(cartellaUtente));

        if (!Directory.Exists(_cartellaUtente))
            Directory.CreateDirectory(_cartellaUtente);
    }

    /// <summary>
    /// Cartella standard per i dati utente, condivisa fra console e app GUI:
    /// - Windows: %APPDATA%\WsaQuiz
    /// - macOS:   ~/Library/Application Support/WsaQuiz
    /// - Linux:   ~/.config/WsaQuiz
    /// </summary>
    public static string CartellaUtenteDefault()
    {
        var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseFolder, "WsaQuiz");
    }

    // ---------------------------------------------------------------- MATERIE

    public List<Materia> CaricaMaterie()
    {
        if (!_fonte.Esiste("materie.json"))
            throw new FileNotFoundException("File materie.json non trovato.", "materie.json");

        string json = _fonte.LeggiTesto("materie.json");
        var materie = JsonSerializer.Deserialize<List<Materia>>(json, OpzioniLettura)
                      ?? new List<Materia>();
        return materie;
    }

    // ---------------------------------------------------------------- DOMANDE

    /// <summary>
    /// Carica tutte le domande di tutte le materie configurate.
    /// Ogni domanda viene arricchita con MateriaId e MateriaNome.
    /// </summary>
    public List<Domanda> CaricaTutteLeDomande(List<Materia> materie)
    {
        var tutte = new List<Domanda>();
        foreach (var m in materie)
        {
            var domandeMateria = CaricaDomandeMateria(m);
            tutte.AddRange(domandeMateria);
        }
        return tutte;
    }

    public List<Domanda> CaricaDomandeMateria(Materia materia)
    {
        var risultato = new List<Domanda>();
        // _fonte.ElencaJson restituisce sequenza vuota se la cartella non esiste:
        // una materia senza domande verra' semplicemente mostrata vuota.
        foreach (var file in _fonte.ElencaJson(materia.Cartella))
        {
            try
            {
                var domande = LeggiFileDomande(file, materia.Formato);
                foreach (var d in domande)
                {
                    d.MateriaId = materia.Id;
                    d.MateriaNome = materia.Nome;
                }
                risultato.AddRange(domande);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ATTENZIONE] Impossibile leggere '{file}': {ex.Message}");
            }
        }

        // Assegna ID stabili basati sul contenuto (hash troncato a 12 char hex).
        // Gestisce eventuali collisioni accodando un suffisso "-2", "-3", ...
        // Stesso contenuto -> stesso ID, anche dopo riordini nei JSON.
        var idVisti = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in risultato)
        {
            string baseId = GeneraId(d);
            string finale = baseId;
            int suffisso = 2;
            while (!idVisti.Add(finale))
            {
                finale = $"{baseId}-{suffisso}";
                suffisso++;
            }
            d.Id = finale;
        }

        return risultato;
    }

    /// <summary>
    /// Genera un ID stabile per la domanda usando SHA256 troncato a 12 caratteri hex.
    /// L'hash si calcola su testo + risposte (in ordine) + indice corretta.
    /// 12 hex = 48 bit -> probabilita' di collisione su 1000 domande ~3.5e-12.
    /// </summary>
    private static string GeneraId(Domanda d)
    {
        string raw = $"{d.DomandaTesto}|{string.Join("\u0001", d.Risposte)}|{d.RispostaCorretta}";
        byte[] bytes = Encoding.UTF8.GetBytes(raw);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant(); // 6 byte = 12 char hex
    }

    private List<Domanda> LeggiFileDomande(string percorso, string formato)
    {
        string json = _fonte.LeggiTesto(percorso);
        return formato switch
        {
            "nested" => ParseNested(json),
            _        => ParseStandard(json)
        };
    }

    private List<Domanda> ParseStandard(string json)
    {
        return JsonSerializer.Deserialize<List<Domanda>>(json, OpzioniLettura) ?? new();
    }

    /// <summary>
    /// Parser per il formato "nested": { "quiz": { "questions": [{ "question", "options", "correct_index", ... }] } }.
    /// </summary>
    private List<Domanda> ParseNested(string json)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
        if (!doc.RootElement.TryGetProperty("quiz", out var quiz) ||
            !quiz.TryGetProperty("questions", out var questions) ||
            questions.ValueKind != JsonValueKind.Array)
        {
            return new List<Domanda>();
        }

        var risultato = new List<Domanda>();
        foreach (var q in questions.EnumerateArray())
        {
            var d = new Domanda
            {
                Categoria        = q.TryGetProperty("category", out var cat)    ? cat.GetString() ?? "" : "",
                DomandaTesto     = q.TryGetProperty("question", out var qt)     ? qt.GetString() ?? ""  : "",
                Spiegazione      = q.TryGetProperty("explanation", out var ex)  ? ex.GetString() ?? ""  : ""
            };
            if (q.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
            {
                foreach (var o in opts.EnumerateArray())
                    d.Risposte.Add(o.GetString() ?? "");
            }
            // Validazione: correct_index deve essere all'interno dei bounds delle opzioni.
            int correctIndex = q.TryGetProperty("correct_index", out var ci) && ci.ValueKind == JsonValueKind.Number ? ci.GetInt32() : 0;
            d.RispostaCorretta = correctIndex >= 0 && correctIndex < d.Risposte.Count ? correctIndex : 0;
            risultato.Add(d);
        }
        return risultato;
    }

    // ---------------------------------------------------------------- ATOMIC I/O

    /// <summary>
    /// Scrive il contenuto su file in modo atomico (temp + rename) per evitare
    /// file corrotti in caso di crash o scritture concorrenti.
    /// </summary>
    private static void ScriviAtomico(string percorso, string contenuto)
    {
        string temp = percorso + ".tmp";
        File.WriteAllText(temp, contenuto);
        File.Move(temp, percorso, overwrite: true);
    }

    /// <summary>
    /// Legge tutto il testo da un file con lock condiviso (permette letture
    /// concorrenti, blocca scritture).
    /// </summary>
    private static string LeggiFileCondiviso(string percorso)
    {
        using var stream = new FileStream(percorso, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // ---------------------------------------------------------------- CRONOLOGIA

    public List<RisultatoQuiz> CaricaCronologia()
    {
        if (!File.Exists(_fileCronologia)) return new List<RisultatoQuiz>();
        List<RisultatoQuiz> lista;
        try
        {
            string json = LeggiFileCondiviso(_fileCronologia);
            lista = JsonSerializer.Deserialize<List<RisultatoQuiz>>(json, OpzioniLettura) ?? new();
        }
        catch
        {
            // File corrotto: non blocco l'app, ricomincio da lista vuota.
            return new List<RisultatoQuiz>();
        }

        // Migrazione lazy una-tantum: i record vecchi senza Id ne ricevono uno
        // e il file viene riscritto. Idempotente: dalla seconda chiamata in poi e' un no-op.
        bool serveRiscrittura = false;
        foreach (var r in lista)
        {
            if (string.IsNullOrEmpty(r.Id))
            {
                r.Id = Guid.NewGuid().ToString();
                serveRiscrittura = true;
            }
        }
        if (serveRiscrittura)
        {
            try
            {
                string json = JsonSerializer.Serialize(lista, OpzioniScrittura);
                ScriviAtomico(_fileCronologia, json);
            }
            catch
            {
                // Se la riscrittura fallisce (file in lock, disco pieno), restituisco
                // comunque la lista in memoria con gli Id assegnati: l'app continua a
                // funzionare, e al prossimo caricamento riproveremo la migrazione.
            }
        }
        return lista;
    }

    public void SalvaRisultato(RisultatoQuiz risultato)
    {
        if (string.IsNullOrEmpty(risultato.Id))
            risultato.Id = Guid.NewGuid().ToString();

        var cronologia = CaricaCronologia();
        cronologia.Add(risultato);
        string json = JsonSerializer.Serialize(cronologia, OpzioniScrittura);
        ScriviAtomico(_fileCronologia, json);
    }

    /// <summary>
    /// Elimina il risultato con l'Id specificato dalla cronologia.
    /// No-op silenzioso se l'Id non esiste o se il file non c'e' (es. cronologia mai creata).
    /// </summary>
    public void EliminaRisultato(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!File.Exists(_fileCronologia)) return;

        var cronologia = CaricaCronologia();
        int rimossi = cronologia.RemoveAll(r => r.Id == id);
        if (rimossi == 0) return;

        string json = JsonSerializer.Serialize(cronologia, OpzioniScrittura);
        ScriviAtomico(_fileCronologia, json);
    }

    /// <summary>
    /// Svuota completamente la cronologia. Scrive una lista vuota nel file
    /// (non lo elimina) per evitare race con scritture concorrenti successive
    /// e per coerenza con il pattern di <see cref="CaricaCronologia"/>.
    /// </summary>
    public void SvuotaCronologia()
    {
        string json = JsonSerializer.Serialize(new List<RisultatoQuiz>(), OpzioniScrittura);
        ScriviAtomico(_fileCronologia, json);
    }

    // ---------------------------------------------------------------- PAUSA SESSIONE

    public List<SessionePausa> CaricaPause()
    {
        if (!File.Exists(_filePausa)) return new List<SessionePausa>();
        try
        {
            string json = LeggiFileCondiviso(_filePausa);
            var lista = JsonSerializer.Deserialize<List<SessionePausa>>(json, OpzioniLettura);
            if (lista != null)
            {
                foreach (var s in lista)
                    if (string.IsNullOrWhiteSpace(s.SessioneId))
                        s.SessioneId = Guid.NewGuid().ToString("N");
                return lista;
            }

            // Retrocompatibilita': vecchio formato con un singolo oggetto.
            var singola = JsonSerializer.Deserialize<SessionePausa>(json, OpzioniLettura);
            if (singola == null) return new List<SessionePausa>();
            if (string.IsNullOrWhiteSpace(singola.SessioneId))
                singola.SessioneId = Guid.NewGuid().ToString("N");
            return new List<SessionePausa> { singola };
        }
        catch
        {
            return new List<SessionePausa>();
        }
    }

    public void SalvaPausa(SessionePausa stato)
    {
        var pause = CaricaPause();
        int idx = pause.FindIndex(p => p.SessioneId == stato.SessioneId);
        if (idx >= 0) pause[idx] = stato;
        else pause.Add(stato);
        string json = JsonSerializer.Serialize(pause, OpzioniScrittura);
        ScriviAtomico(_filePausa, json);
    }

    public void EliminaPausa(string sessioneId)
    {
        if (!File.Exists(_filePausa)) return;
        var pause = CaricaPause();
        pause.RemoveAll(p => p.SessioneId == sessioneId);
        if (pause.Count == 0)
        {
            File.Delete(_filePausa);
            return;
        }
        string json = JsonSerializer.Serialize(pause, OpzioniScrittura);
        ScriviAtomico(_filePausa, json);
    }
}
