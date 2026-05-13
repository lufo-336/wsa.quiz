using Wsa.Quiz.Core.Models;
using System.Text;

namespace Wsa.Quiz.Cli.Services;

/// <summary>
/// Tutto il layer di presentazione a terminale. Helper per colori, input e menu.
/// </summary>
public static class ConsoleUI
{
    // Palette
    private const ConsoleColor C_Titolo     = ConsoleColor.Cyan;
    private const ConsoleColor C_Accento    = ConsoleColor.Yellow;
    private const ConsoleColor C_Ok         = ConsoleColor.Green;
    private const ConsoleColor C_Errore     = ConsoleColor.Red;
    private const ConsoleColor C_Codice     = ConsoleColor.Magenta;
    private const ConsoleColor C_Soft       = ConsoleColor.DarkGray;
    private const ConsoleColor C_Testo      = ConsoleColor.Gray;

    // ---------------------------------------------------------------- UTILITY

    public static void PulisciSchermo()
    {
        try { Console.Clear(); } catch { /* terminali senza supporto */ }
    }

    public static void Scrivi(string s, ConsoleColor col)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = col;
        Console.Write(s);
        Console.ForegroundColor = prev;
    }

    public static void ScriviRiga(string s, ConsoleColor col)
    {
        Scrivi(s + Environment.NewLine, col);
    }

    public static void Pausa(string messaggio = "  Premi un tasto per proseguire...")
    {
        ScriviRiga("", C_Testo);
        Scrivi(messaggio, C_Soft);
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }

    public static void MostraErroreFatale(string messaggio)
    {
        PulisciSchermo();
        ScriviRiga("", C_Errore);
        ScriviRiga("  " + new string('=', 60), C_Errore);
        ScriviRiga("  ERRORE", C_Errore);
        ScriviRiga("  " + new string('=', 60), C_Errore);
        Console.WriteLine();
        foreach (var riga in messaggio.Split('\n'))
            ScriviRiga("  " + riga.TrimEnd('\r'), C_Errore);
        Pausa();
    }

    // ---------------------------------------------------------------- CODICE COLORATO

    /// <summary>
    /// Stampa testo con frammenti di codice colorati: `inline` o blocchi ``` ... ```.
    /// Rispetta il prefisso passato come indentazione su ogni riga.
    /// </summary>
    public static void ScriviTestoConCodice(string testo, string prefisso = "  ", ConsoleColor testoColor = C_Testo)
    {
        if (string.IsNullOrEmpty(testo))
        {
            Console.WriteLine();
            return;
        }

        // Prima: blocchi triple-backtick (anche multilinea). Semplifichiamo: trattiamo
        // l'input come sequenza di segmenti normali / inline-code / code-block.
        var segmenti = Tokenizza(testo);

        Console.Write(prefisso);
        foreach (var seg in segmenti)
        {
            switch (seg.Tipo)
            {
                case Segmento.Kind.Normale:
                    StampaConWrap(seg.Testo, prefisso, testoColor);
                    break;
                case Segmento.Kind.InlineCode:
                    Scrivi(seg.Testo, C_Codice);
                    break;
                case Segmento.Kind.CodeBlock:
                    Console.WriteLine();
                    Console.WriteLine();
                    foreach (var riga in seg.Testo.Split('\n'))
                    {
                        var pulita = riga.TrimEnd('\r');
                        Scrivi(prefisso + "│ ", C_Soft);
                        ScriviRiga(pulita, C_Codice);
                    }
                    Console.Write(prefisso);
                    break;
            }
        }
        Console.WriteLine();
    }

    private record Segmento(Segmento.Kind Tipo, string Testo)
    {
        public enum Kind { Normale, InlineCode, CodeBlock }
    }

    private static List<Segmento> Tokenizza(string s)
    {
        var risultato = new List<Segmento>();
        int i = 0;
        var buf = new StringBuilder();

        void Flush()
        {
            if (buf.Length > 0)
            {
                risultato.Add(new Segmento(Segmento.Kind.Normale, buf.ToString()));
                buf.Clear();
            }
        }

        while (i < s.Length)
        {
            // Triple backtick?
            if (i + 2 < s.Length && s[i] == '`' && s[i + 1] == '`' && s[i + 2] == '`')
            {
                Flush();
                int inizio = i + 3;
                // Salto un eventuale identificatore di linguaggio fino a newline
                int nl = s.IndexOf('\n', inizio);
                if (nl >= 0) inizio = nl + 1;
                int fine = s.IndexOf("```", inizio);
                if (fine < 0) fine = s.Length;
                risultato.Add(new Segmento(Segmento.Kind.CodeBlock, s.Substring(inizio, fine - inizio)));
                i = fine + 3;
                continue;
            }

            if (s[i] == '`')
            {
                Flush();
                int fine = s.IndexOf('`', i + 1);
                if (fine < 0) { buf.Append(s[i]); i++; continue; }
                risultato.Add(new Segmento(Segmento.Kind.InlineCode, s.Substring(i + 1, fine - i - 1)));
                i = fine + 1;
                continue;
            }

            buf.Append(s[i]);
            i++;
        }

        Flush();
        return risultato;
    }

    /// <summary>Stampa una stringa con word-wrap rispetto alla larghezza del terminale.</summary>
    private static void StampaConWrap(string testo, string prefisso, ConsoleColor col)
    {
        int largMax;
        try { largMax = Console.WindowWidth - 2; } catch { largMax = 100; }
        if (largMax < 40) largMax = 40;
        int colonnaCorrente;
        try { colonnaCorrente = Console.CursorLeft; } catch { colonnaCorrente = prefisso.Length; }

        foreach (var parola in SpezzaInParole(testo))
        {
            if (parola == "\n")
            {
                Console.WriteLine();
                Console.Write(prefisso);
                colonnaCorrente = prefisso.Length;
                continue;
            }
            if (colonnaCorrente + parola.Length > largMax && colonnaCorrente > prefisso.Length)
            {
                Console.WriteLine();
                Console.Write(prefisso);
                colonnaCorrente = prefisso.Length;
                if (parola == " ") continue;
            }
            Scrivi(parola, col);
            colonnaCorrente += parola.Length;
        }
    }

    private static IEnumerable<string> SpezzaInParole(string s)
    {
        var buf = new StringBuilder();
        foreach (var c in s)
        {
            if (c == '\n')
            {
                if (buf.Length > 0) { yield return buf.ToString(); buf.Clear(); }
                yield return "\n";
            }
            else if (c == ' ')
            {
                if (buf.Length > 0) { yield return buf.ToString(); buf.Clear(); }
                yield return " ";
            }
            else
            {
                buf.Append(c);
            }
        }
        if (buf.Length > 0) yield return buf.ToString();
    }

    // ---------------------------------------------------------------- BANNER

    public static void Banner(string titolo)
    {
        PulisciSchermo();
        Console.WriteLine();
        ScriviRiga("  " + new string('═', 64), C_Titolo);
        ScriviRiga("    " + titolo, C_Titolo);
        ScriviRiga("  " + new string('═', 64), C_Titolo);
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- MENU

    /// <summary>
    /// Menu generico a tasto singolo. Ritorna l'indice (0-based) della voce scelta,
    /// o -1 se l'utente ha premuto Esc/Q per uscire.
    /// </summary>
    public static int MostraMenuSceltaSingola(string titolo, IList<string> voci, string? uscita = "Esci")
    {
        while (true)
        {
            Banner(titolo);
            for (int i = 0; i < voci.Count; i++)
            {
                Scrivi($"    [{i + 1}] ", C_Accento);
                ScriviRiga(voci[i], C_Testo);
            }
            if (uscita != null)
            {
                Scrivi("    [0] ", C_Accento);
                ScriviRiga(uscita, C_Soft);
            }
            Console.WriteLine();
            Scrivi("  Scegli: ", C_Soft);

            var tasto = Console.ReadKey(intercept: true);
            Console.WriteLine();

            if (tasto.Key == ConsoleKey.Escape || tasto.Key == ConsoleKey.Q) return -1;
            if (tasto.Key == ConsoleKey.D0 || tasto.Key == ConsoleKey.NumPad0)
            {
                if (uscita != null) return -1;
                continue;
            }

            int num = DecodificaDigit(tasto);
            if (num >= 1 && num <= voci.Count) return num - 1;
            // altrimenti: loop, scelta non valida
        }
    }

    private static int DecodificaDigit(ConsoleKeyInfo k)
    {
        if (k.Key >= ConsoleKey.D1 && k.Key <= ConsoleKey.D9) return k.Key - ConsoleKey.D0;
        if (k.Key >= ConsoleKey.NumPad1 && k.Key <= ConsoleKey.NumPad9) return k.Key - ConsoleKey.NumPad0;
        return -1;
    }

    // ---------------------------------------------------------------- TOGGLE OPZIONI

    /// <summary>Chiede all'utente se attivare rotazione e cronometro prima di iniziare.</summary>
    public static (bool rotazione, bool cronometro) ChiediOpzioniSessione()
    {
        bool rotazione = false;
        bool cronometro = false;
        int cursore = 0;

        string mark(bool b) => b ? "[ X ]" : "[   ]";
        string freccia(int i) => cursore == i ? "» " : "  ";

        while (true)
        {
            Banner("Opzioni della sessione");
            ScriviRiga("  Attiva/disattiva le opzioni per questa sessione. Invio conferma.", C_Soft);
            Console.WriteLine();

            Scrivi("  " + freccia(0), C_Accento);
            Scrivi(mark(rotazione) + "  ", rotazione ? C_Ok : C_Soft);
            ScriviRiga("Rotazione (le sbagliate rientrano in coda fino a tutte corrette)", C_Testo);

            Scrivi("  " + freccia(1), C_Accento);
            Scrivi(mark(cronometro) + "  ", cronometro ? C_Ok : C_Soft);
            ScriviRiga("Cronometro (mostra tempo trascorso e tempo per domanda)", C_Testo);

            Console.WriteLine();
            Scrivi("  " + freccia(2), C_Accento);
            ScriviRiga("Inizia il quiz", C_Ok);

            Scrivi("  " + freccia(3), C_Accento);
            ScriviRiga("Annulla", C_Soft);
            Console.WriteLine();
            ScriviRiga("  ↑/↓ muovi  · Spazio spunta  · Invio conferma  · Esc annulla", C_Soft);

            var k = Console.ReadKey(intercept: true);
            switch (k.Key)
            {
                case ConsoleKey.UpArrow:   cursore = (cursore - 1 + 4) % 4; break;
                case ConsoleKey.DownArrow: cursore = (cursore + 1) % 4; break;
                case ConsoleKey.Spacebar:
                    if (cursore == 0) rotazione = !rotazione;
                    else if (cursore == 1) cronometro = !cronometro;
                    break;
                case ConsoleKey.Enter:
                    if (cursore == 2) return (rotazione, cronometro);
                    if (cursore == 3) return (false, false);
                    if (cursore == 0) rotazione = !rotazione;
                    else if (cursore == 1) cronometro = !cronometro;
                    break;
                case ConsoleKey.Escape:    return (false, false);
            }
        }
    }

    // ---------------------------------------------------------------- SELEZIONE CATEGORIE (MULTI)

    /// <summary>
    /// Selezione multipla a checkbox con frecce + spazio.
    /// Ritorna l'elenco delle categorie selezionate (può essere vuoto se annullato).
    /// </summary>
    public static List<string> SelezionaCategorieMulti(List<(string Categoria, int Conteggio)> riepilogo)
    {
        if (riepilogo.Count == 0) return new List<string>();
        var selezionate = new bool[riepilogo.Count];
        int cursore = 0;

        while (true)
        {
            Banner("Scelta categorie (multi-selezione)");
            ScriviRiga("  ↑/↓ muovi · Spazio spunta · A seleziona tutte · N pulisci · Invio conferma · Esc annulla",
                       C_Soft);
            Console.WriteLine();

            int totaleDomande = 0;
            for (int i = 0; i < riepilogo.Count; i++)
            {
                string freccia = cursore == i ? "»" : " ";
                string mark = selezionate[i] ? "[X]" : "[ ]";
                string linea = $"  {freccia} {mark}  {riepilogo[i].Categoria}  ({riepilogo[i].Conteggio})";
                ScriviRiga(linea, cursore == i ? C_Accento : (selezionate[i] ? C_Ok : C_Testo));
                if (selezionate[i]) totaleDomande += riepilogo[i].Conteggio;
            }
            Console.WriteLine();
            int numSelez = selezionate.Count(x => x);
            Scrivi($"  Selezionate: {numSelez}", C_Soft);
            ScriviRiga($"   ·   Domande totali: {totaleDomande}", C_Soft);

            var k = Console.ReadKey(intercept: true);
            switch (k.Key)
            {
                case ConsoleKey.UpArrow:   cursore = (cursore - 1 + riepilogo.Count) % riepilogo.Count; break;
                case ConsoleKey.DownArrow: cursore = (cursore + 1) % riepilogo.Count; break;
                case ConsoleKey.Spacebar:  selezionate[cursore] = !selezionate[cursore]; break;
                case ConsoleKey.A:         for (int i = 0; i < selezionate.Length; i++) selezionate[i] = true; break;
                case ConsoleKey.N:         for (int i = 0; i < selezionate.Length; i++) selezionate[i] = false; break;
                case ConsoleKey.Enter:
                    var ris = new List<string>();
                    for (int i = 0; i < riepilogo.Count; i++)
                        if (selezionate[i]) ris.Add(riepilogo[i].Categoria);
                    return ris;
                case ConsoleKey.Escape:    return new List<string>();
            }
        }
    }

    // ---------------------------------------------------------------- SELEZIONE N DOMANDE

    /// <summary>Chiede quante domande estrarre casualmente. Ritorna il numero, o -1 se annullato.</summary>
    public static int ChiediNumeroDomande(int massimo)
    {
        while (true)
        {
            Banner($"Quante domande? (max {massimo})");
            Scrivi("  Inserisci un numero tra 1 e " + massimo + " (Invio) oppure Esc per annullare: ", C_Testo);
            // Leggo una stringa accettando solo cifre + Backspace + Invio + Esc
            var buf = new StringBuilder();
            while (true)
            {
                var k = Console.ReadKey(intercept: true);
                if (k.Key == ConsoleKey.Escape) { Console.WriteLine(); return -1; }
                if (k.Key == ConsoleKey.Enter)  { Console.WriteLine(); break; }
                if (k.Key == ConsoleKey.Backspace)
                {
                    if (buf.Length > 0) { buf.Length--; Console.Write("\b \b"); }
                    continue;
                }
                if (char.IsDigit(k.KeyChar) && buf.Length < 5)
                {
                    buf.Append(k.KeyChar);
                    Scrivi(k.KeyChar.ToString(), C_Accento);
                }
            }
            if (int.TryParse(buf.ToString(), out int n) && n >= 1 && n <= massimo)
                return n;
            ScriviRiga("  Valore non valido, riprova.", C_Errore);
            Pausa();
        }
    }

    // ---------------------------------------------------------------- DOMANDA

    /// <summary>
    /// Mostra una domanda e aspetta la risposta come singolo tasto (A-D).
    /// Prima della risposta, frecce ←/→ scorrono le domande precedenti in sola lettura.
    /// Ritorna: 0..3 = indice risposta, -1 = abbandona (Q), -2 = pausa sessione (P).
    /// </summary>
    public static int ChiediRisposta(DomandaPreparata dp,
                                     int numeroDomanda,
                                     int totale,
                                     int corrette,
                                     List<DettaglioRisposta> storicoSessione,
                                     DateTime inizioSessione,
                                     bool mostraCronometro,
                                     string materiaLabel)
    {
        int cursoreStorico = storicoSessione.Count; // posizione virtuale: se < Count siamo in rivedi-precedente

        while (true)
        {
            if (cursoreStorico < storicoSessione.Count)
            {
                MostraStoricoRiga(storicoSessione[cursoreStorico], cursoreStorico + 1, storicoSessione.Count);
                // Navigazione in storico: ←/→ navigare, qualunque altro tasto torna alla domanda corrente
                var k = Console.ReadKey(intercept: true);
                if (k.Key == ConsoleKey.LeftArrow && cursoreStorico > 0) cursoreStorico--;
                else if (k.Key == ConsoleKey.RightArrow) cursoreStorico++;
                else if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) return -1;
                else cursoreStorico = storicoSessione.Count; // torna alla corrente
                continue;
            }

            DisegnaIntestazioneDomanda(numeroDomanda, totale, corrette, inizioSessione, mostraCronometro,
                                       dp.Originale, materiaLabel);
            DisegnaRisposte(dp, null);

            // Prompt
            ScriviRiga("", C_Testo);
            ScriviRiga("  A/B/C/D rispondi · ← rivedi precedenti · P pausa · Q abbandona",
                       C_Soft);

            var tasto = Console.ReadKey(intercept: true);
            switch (tasto.Key)
            {
                case ConsoleKey.A: return 0;
                case ConsoleKey.B: return 1;
                case ConsoleKey.C: return 2;
                case ConsoleKey.D:
                    if (dp.RisposteShufflate.Count >= 4) return 3;
                    break;
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    return -1;
                case ConsoleKey.P:
                    return -2;
                case ConsoleKey.LeftArrow:
                    if (storicoSessione.Count > 0) cursoreStorico = storicoSessione.Count - 1;
                    break;
            }
        }
    }

    private static void DisegnaIntestazioneDomanda(int numero, int totale, int corrette,
                                                   DateTime inizio, bool cronometro,
                                                   Domanda dom, string materiaLabel)
    {
        PulisciSchermo();
        Console.WriteLine();
        // Barra di progresso
        double frazione = totale == 0 ? 0 : (double)(numero - 1) / totale;
        int largBarra = 40;
        int pieni = (int)(frazione * largBarra);
        string barra = "[" + new string('█', pieni) + new string('·', largBarra - pieni) + "]";

        Scrivi("  ", C_Testo);
        Scrivi(barra, C_Ok);
        Scrivi($"  {numero}/{totale}", C_Accento);
        Scrivi($"   ✓ {corrette}", C_Ok);
        if (cronometro)
        {
            var trascorso = DateTime.UtcNow - inizio;
            Scrivi($"   ⏱ {trascorso:mm\\:ss}", C_Soft);
        }
        Console.WriteLine();
        Scrivi($"  [{materiaLabel}] ", C_Soft);
        ScriviRiga(dom.Categoria, C_Accento);
        ScriviRiga("  " + new string('─', 64), C_Soft);
        Console.WriteLine();
        ScriviTestoConCodice(dom.DomandaTesto, "  ", C_Testo);
        Console.WriteLine();
    }

    private static void DisegnaRisposte(DomandaPreparata dp, int? scelta)
    {
        for (int i = 0; i < dp.RisposteShufflate.Count; i++)
        {
            char lettera = (char)('A' + i);
            ConsoleColor col = C_Testo;
            string prefissoLettera = $"  [{lettera}] ";
            Scrivi(prefissoLettera, C_Accento);
            ScriviTestoConCodice(dp.RisposteShufflate[i], new string(' ', prefissoLettera.Length), col);
        }
    }

    private static void MostraStoricoRiga(DettaglioRisposta d, int posizione, int totale)
    {
        PulisciSchermo();
        Console.WriteLine();
        Scrivi("  ◀ Storico ", C_Soft);
        ScriviRiga($"({posizione}/{totale})   premi ← → per navigare · qualsiasi altro tasto per tornare",
                   C_Soft);
        ScriviRiga("  " + new string('─', 64), C_Soft);
        Console.WriteLine();
        ScriviRiga($"  Categoria: {d.Categoria}", C_Accento);
        Console.WriteLine();
        ScriviTestoConCodice(d.TestoDomanda, "  ", C_Testo);
        Console.WriteLine();
        ScriviRiga("  La tua risposta:", C_Soft);
        ScriviTestoConCodice(d.RispostaData, "    ", d.Corretta ? C_Ok : C_Errore);
        if (!d.Corretta)
        {
            ScriviRiga("  Corretta:", C_Soft);
            ScriviTestoConCodice(d.RispostaCorretta, "    ", C_Ok);
        }
        Console.WriteLine();
        ScriviRiga("  Spiegazione:", C_Soft);
        ScriviTestoConCodice(d.Spiegazione, "    ", C_Testo);
    }

    // ---------------------------------------------------------------- FEEDBACK RISPOSTA

    public static void MostraFeedback(DomandaPreparata dp, int scelta, int corrette, int numero, int totale,
                                      DateTime inizio, bool cronometro)
    {
        Console.WriteLine();
        bool ok = scelta == dp.IndiceCorrettoShufflato;
        if (ok)
        {
            Scrivi("  ✓ CORRETTA", C_Ok);
        }
        else
        {
            Scrivi("  ✗ SBAGLIATA", C_Errore);
            Scrivi("   ·   Risposta corretta: ", C_Soft);
            Scrivi($"[{dp.LetteraCorretta}] ", C_Accento);
        }
        Console.WriteLine();
        if (!ok) ScriviTestoConCodice(dp.TestoRispostaCorretta, "    ", C_Ok);
        Console.WriteLine();
        ScriviRiga("  Spiegazione:", C_Soft);
        ScriviTestoConCodice(dp.Originale.Spiegazione, "    ", C_Testo);
        Console.WriteLine();
        Scrivi("  Premi un tasto per la prossima domanda...", C_Soft);
        Console.ReadKey(intercept: true);
    }

    // ---------------------------------------------------------------- RIEPILOGO FINALE

    public static void MostraRiepilogoFinale(RisultatoQuiz r, List<DettaglioRisposta> sbagliate)
    {
        Banner("Riepilogo sessione");
        ScriviRiga($"  Modalità: {r.Modalita}", C_Accento);
        if (!string.IsNullOrEmpty(r.MateriaNome))
            ScriviRiga($"  Materia:  {r.MateriaNome}", C_Accento);
        if (r.CategorieSelezionate.Count > 0)
            ScriviRiga($"  Categorie: {string.Join(", ", r.CategorieSelezionate)}", C_Soft);
        if (r.ModalitaRotazione) ScriviRiga("  Rotazione: attiva", C_Soft);
        if (r.CronometroAttivo)  ScriviRiga($"  Durata:    {r.DurataQuiz:hh\\:mm\\:ss}", C_Soft);
        Console.WriteLine();

        Scrivi($"  Domande:   ", C_Testo);
        ScriviRiga($"{r.TotaleDomande}", C_Accento);
        Scrivi("  Corrette:  ", C_Testo);
        ScriviRiga($"{r.RisposteCorrette}", C_Ok);
        Scrivi("  Errate:    ", C_Testo);
        ScriviRiga($"{r.RisposteErrate}", r.RisposteErrate == 0 ? C_Ok : C_Errore);
        Scrivi("  Punteggio: ", C_Testo);
        ScriviRiga($"{r.Punteggio}", C_Accento);
        Scrivi("  Accuratezza: ", C_Testo);
        ScriviRiga($"{r.PercentualeCorrette:F1}%",
                   r.PercentualeCorrette >= 80 ? C_Ok : (r.PercentualeCorrette >= 60 ? C_Accento : C_Errore));
        if (r.Abbandonato) ScriviRiga("  (sessione abbandonata)", C_Soft);

        // Breakdown per categoria se la sessione ha attraversato più di una
        if (r.Dettagli.Count > 0)
        {
            var categorieDistinte = r.Dettagli.Select(d => d.Categoria).Distinct().Count();
            if (categorieDistinte >= 2)
            {
                Console.WriteLine();
                StampaBreakdownCategoria(r.Dettagli);
            }
        }

        if (sbagliate.Count > 0)
        {
            Console.WriteLine();
            ScriviRiga($"  Domande sbagliate ({sbagliate.Count}):", C_Errore);
            ScriviRiga("  " + new string('─', 64), C_Soft);
            const int perPagina = 5;
            for (int idx = 0; idx < sbagliate.Count; idx++)
            {
                var s = sbagliate[idx];
                Console.WriteLine();
                ScriviRiga($"  [{s.Categoria}]", C_Accento);
                ScriviTestoConCodice(s.TestoDomanda, "    ", C_Testo);
                ScriviTestoConCodice("Tua: " + s.RispostaData, "    ", C_Errore);
                ScriviTestoConCodice("OK:  " + s.RispostaCorretta, "    ", C_Ok);

                bool ultimaDelBlocco = (idx + 1) % perPagina == 0;
                bool altreRimaste   = idx + 1 < sbagliate.Count;
                if (ultimaDelBlocco && altreRimaste)
                {
                    Console.WriteLine();
                    ScriviRiga($"  [{idx + 1}/{sbagliate.Count}] Premi un tasto per continuare · Esc per saltare", C_Soft);
                    if (Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
                }
            }
        }

        Pausa();
    }

    // ---------------------------------------------------------------- CRONOLOGIA

    /// <summary>
    /// Mostra l'elenco delle sessioni in cronologia. L'utente può selezionarne una
    /// per vedere il dettaglio completo con breakdown per categoria, oppure uscire.
    /// </summary>
    public static void MostraCronologia(List<RisultatoQuiz> cronologia, string? filtraMateria = null)
    {
        while (true)
        {
            var lista = filtraMateria == null
                ? cronologia
                : cronologia.Where(r => r.MateriaNome.Equals(filtraMateria, StringComparison.OrdinalIgnoreCase)).ToList();

            if (lista.Count == 0)
            {
                Banner(filtraMateria == null ? "Cronologia globale" : $"Cronologia — {filtraMateria}");
                ScriviRiga("  Nessuna sessione registrata.", C_Soft);
                Pausa();
                return;
            }

            // Dalla più recente
            var ordinate = lista.OrderByDescending(r => r.DataOra).ToList();
            int scelta = MostraMenuCronologia(ordinate, filtraMateria);
            if (scelta == -1) return;
            MostraDettaglioSessione(ordinate[scelta]);
        }
    }

    private static int MostraMenuCronologia(List<RisultatoQuiz> ordinate, string? filtraMateria)
    {
        while (true)
        {
            Banner(filtraMateria == null ? "Cronologia globale" : $"Cronologia — {filtraMateria}");
            ScriviRiga("  Seleziona una sessione per vederne il dettaglio, o 0/Esc per tornare indietro.",
                       C_Soft);
            Console.WriteLine();

            int maxShow = Math.Min(ordinate.Count, 30);
            for (int i = 0; i < maxShow; i++)
            {
                var r = ordinate[i];
                Scrivi($"  [{i + 1,2}] ", C_Accento);
                Scrivi(r.DataOra.ToString("dd/MM/yyyy HH:mm") + "  ", C_Testo);
                Scrivi($"{r.MateriaNome,-10} ", C_Soft);
                string mod = r.Modalita.Length > 32 ? r.Modalita[..32] + "…" : r.Modalita;
                Scrivi($"{mod,-33} ", C_Soft);
                Scrivi($"{r.RisposteCorrette,3}/{r.TotaleDomande,-3}  ", C_Ok);
                ConsoleColor pctColor = r.PercentualeCorrette >= 80 ? C_Ok
                                      : r.PercentualeCorrette >= 60 ? C_Accento
                                                                    : C_Errore;
                Scrivi($"{r.PercentualeCorrette,5:F1}%  ", pctColor);
                if (r.CronometroAttivo) Scrivi($"⏱{r.DurataQuiz:mm\\:ss} ", C_Soft);
                if (r.ModalitaRotazione) Scrivi("↻ ", C_Soft);
                if (r.Abbandonato) Scrivi("(abb.) ", C_Errore);
                Console.WriteLine();
            }

            if (ordinate.Count > maxShow)
                ScriviRiga($"  ... e altre {ordinate.Count - maxShow} sessioni più vecchie non mostrate.", C_Soft);

            Console.WriteLine();
            Scrivi("  Numero sessione (Invio/0/Esc per indietro): ", C_Soft);

            var buf = new System.Text.StringBuilder();
            while (true)
            {
                var k = Console.ReadKey(intercept: true);
                if (k.Key == ConsoleKey.Escape) { Console.WriteLine(); return -1; }
                if (k.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    if (buf.Length == 0) return -1;
                    if (int.TryParse(buf.ToString(), out int n) && n >= 1 && n <= maxShow)
                        return n - 1;
                    ScriviRiga("  Selezione non valida.", C_Errore);
                    Pausa();
                    break;
                }
                if (k.Key == ConsoleKey.Backspace)
                {
                    if (buf.Length > 0) { buf.Length--; Console.Write("\b \b"); }
                    continue;
                }
                if (char.IsDigit(k.KeyChar) && buf.Length < 4)
                {
                    buf.Append(k.KeyChar);
                    Scrivi(k.KeyChar.ToString(), C_Accento);
                }
            }
        }
    }

    private static void MostraDettaglioSessione(RisultatoQuiz r)
    {
        Banner("Dettaglio sessione");

        ScriviRiga($"  Data / ora:   {r.DataOra:dd/MM/yyyy HH:mm:ss}", C_Testo);
        ScriviRiga($"  Materia:      {r.MateriaNome}", C_Testo);
        ScriviRiga($"  Modalità:     {r.Modalita}", C_Testo);
        if (r.CategorieSelezionate.Count > 0)
            ScriviRiga($"  Categorie:    {string.Join(", ", r.CategorieSelezionate)}", C_Soft);
        if (r.ModalitaRotazione) ScriviRiga("  Rotazione:    attiva", C_Soft);
        if (r.CronometroAttivo)  ScriviRiga($"  Durata:       {r.DurataQuiz:hh\\:mm\\:ss}", C_Soft);
        ScriviRiga($"  Stato:        {(r.Abbandonato ? "interrotto" : "completato")}",
                   r.Abbandonato ? C_Accento : C_Ok);
        Console.WriteLine();

        Scrivi("  Punteggio:    ", C_Testo);
        Scrivi($"{r.RisposteCorrette} / {r.TotaleDomande}  ", C_Ok);
        ConsoleColor pctColor = r.PercentualeCorrette >= 80 ? C_Ok
                              : r.PercentualeCorrette >= 60 ? C_Accento
                                                            : C_Errore;
        Scrivi($"({r.PercentualeCorrette:F1}%)", pctColor);
        Console.WriteLine();
        ScriviRiga($"  Punti:        {r.Punteggio}", C_Accento);

        // Breakdown per categoria
        Console.WriteLine();
        StampaBreakdownCategoria(r.Dettagli);

        Console.WriteLine();
        Pausa("  Premi un tasto per tornare alla cronologia...");
    }

    /// <summary>
    /// Stampa una tabella con il breakdown per categoria: corrette/totali + barra + %.
    /// </summary>
    private static void StampaBreakdownCategoria(List<DettaglioRisposta> dettagli)
    {
        if (dettagli.Count == 0) return;

        // Raggruppa per categoria, contando uniche (in modalità rotazione la stessa
        // domanda può apparire più volte — qui contiamo ogni istanza come tentativo).
        var gruppi = dettagli
            .GroupBy(d => d.Categoria)
            .Select(g => new
            {
                Categoria = g.Key,
                Totale    = g.Count(),
                Corrette  = g.Count(d => d.Corretta)
            })
            .OrderByDescending(x => (double)x.Corrette / Math.Max(1, x.Totale))
            .ThenBy(x => x.Categoria)
            .ToList();

        ScriviRiga("  Dettaglio per categoria:", C_Accento);
        ScriviRiga("  " + new string('─', 72), C_Soft);

        // Larghezza colonna categoria: minimo 24, al massimo la più lunga tra quelle presenti
        int catWidth = Math.Min(32, Math.Max(24, gruppi.Max(g => g.Categoria.Length)));

        foreach (var g in gruppi)
        {
            double p = g.Corrette * 100.0 / g.Totale;
            ConsoleColor col = p >= 80 ? C_Ok
                             : p >= 60 ? C_Accento
                                       : C_Errore;

            string categoriaTroncata = g.Categoria.Length > catWidth
                ? g.Categoria[..(catWidth - 1)] + "…"
                : g.Categoria;

            Scrivi($"  {categoriaTroncata.PadRight(catWidth)} ", C_Testo);
            Scrivi($"{g.Corrette,3}/{g.Totale,-3}  ", C_Testo);
            Scrivi($"{p,5:F1}%  ", col);
            Scrivi(BarraPercentuale(p), col);
            Console.WriteLine();
        }
    }

    private static string BarraPercentuale(double pct)
    {
        const int width = 20;
        int pieni = (int)Math.Round(pct / 100.0 * width);
        if (pieni < 0) pieni = 0;
        if (pieni > width) pieni = width;
        return "[" + new string('█', pieni) + new string('·', width - pieni) + "]";
    }
}
