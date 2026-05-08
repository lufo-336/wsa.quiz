using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Services;
using Wsa.Quiz.Cli.Services;

namespace Wsa.Quiz.Cli;

/// <summary>
/// Entry point. Gestisce menu principale (Quizzone, materie, cronologia globale),
/// sottomenu per materia e orchestrazione delle sessioni di quiz.
/// </summary>
public static class Program
{
    private readonly record struct EsitoSessione(RisultatoQuiz Risultato, bool Pausata);

    public static void Main(string[] args)
    {
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

        string cartellaBase = AppContext.BaseDirectory;
        // Dati read-only (materie + domande) accanto all'eseguibile;
        // dati utente (cronologia + sospesi) in cartella condivisa con l'app GUI.
        string cartellaUtente = StorageService.CartellaUtenteDefault();
        var storage = new StorageService(cartellaBase, cartellaUtente);

        List<Materia> materie;
        try
        {
            materie = storage.CaricaMaterie();
        }
        catch (Exception ex)
        {
            ConsoleUI.MostraErroreFatale(
                "Impossibile leggere il file 'materie.json'.\n" +
                $"Dettaglio: {ex.Message}\n\n" +
                "Il file deve esistere accanto all'eseguibile e contenere le materie configurate.");
            return;
        }

        // Carico in anticipo tutte le domande di tutte le materie (per il Quizzone).
        List<Domanda> tutteDomande;
        try
        {
            tutteDomande = storage.CaricaTutteLeDomande(materie);
        }
        catch (Exception ex)
        {
            ConsoleUI.MostraErroreFatale($"Errore nel caricamento domande: {ex.Message}");
            return;
        }

        if (tutteDomande.Count == 0)
        {
            ConsoleUI.MostraErroreFatale(
                "Nessuna domanda trovata. Verifica che la cartella 'domande/' contenga\n" +
                "almeno un file JSON e che 'materie.json' punti alla cartella giusta.");
            return;
        }

        while (true)
        {
            var voci = new List<string>();
            var pause = storage.CaricaPause();
            if (pause.Count > 0)
                voci.Add($"Riprendi quiz in pausa  ({pause.Count})");
            voci.Add($"Il Quizzone  ({tutteDomande.Count} domande, tutte le materie)");
            foreach (var m in materie)
            {
                int n = tutteDomande.Count(d => d.MateriaId == m.Id);
                voci.Add($"{m.Nome}  ({n} domande)");
            }
            voci.Add("Cronologia globale");

            int scelta = ConsoleUI.MostraMenuSceltaSingola("Quiz FdP — Menu principale", voci, "Esci");
            if (scelta == -1) { Congedo(); return; }

            int offset = pause.Count > 0 ? 1 : 0;

            if (pause.Count > 0 && scelta == 0)
            {
                SelezionaERiprendiPausa(pause, tutteDomande, storage);
            }
            else if (scelta == 0 + offset)
            {
                // Quizzone: flusso semplificato — tutte le domande mescolate, opzioni di sessione
                MenuQuizzone(tutteDomande, storage);
            }
            else if (scelta == voci.Count - 1)
            {
                var cronologia = storage.CaricaCronologia();
                ConsoleUI.MostraCronologia(cronologia);
            }
            else
            {
                var materia = materie[scelta - (1 + offset)];
                MenuMateria(materia, tutteDomande.Where(d => d.MateriaId == materia.Id).ToList(), storage);
            }
        }
    }

    private static void Congedo()
    {
        ConsoleUI.PulisciSchermo();
        ConsoleUI.ScriviRiga("  Alla prossima!", ConsoleColor.Yellow);
        Console.WriteLine();
    }

    // ============================================================ QUIZZONE

    private static void MenuQuizzone(List<Domanda> tutte, StorageService storage)
    {
        while (true)
        {
            var voci = new List<string>
            {
                $"Quiz completo randomizzato  ({tutte.Count} domande)",
                "Quiz di N domande (scelte casualmente)",
                "Cronologia globale"
            };
            int scelta = ConsoleUI.MostraMenuSceltaSingola("Il Quizzone — tutte le materie", voci, "Indietro");
            if (scelta == -1) return;

            switch (scelta)
            {
                case 0:
                    EseguiSessioneConOpzioni(
                        tutte, storage,
                        new OpzioniQuiz
                        {
                            NomeModalita = "Quizzone — Completo randomizzato",
                            MateriaNomeLabel = "Quizzone",
                            RandomizzaOrdineDomande = true
                        });
                    break;

                case 1:
                    int n = ConsoleUI.ChiediNumeroDomande(tutte.Count);
                    if (n > 0)
                        EseguiSessioneConOpzioni(
                            tutte, storage,
                            new OpzioniQuiz
                            {
                                NomeModalita = $"Quizzone — {n} domande",
                                MateriaNomeLabel = "Quizzone",
                                RandomizzaOrdineDomande = true,
                                LimiteDomande = n
                            });
                    break;

                case 2:
                    ConsoleUI.MostraCronologia(storage.CaricaCronologia());
                    break;
            }
        }
    }

    // ============================================================ MATERIA

    private static void MenuMateria(Materia materia, List<Domanda> domandeMateria, StorageService storage)
    {
        while (true)
        {
            var voci = new List<string>
            {
                $"Quiz completo  (ordine JSON, {domandeMateria.Count} domande)",
                $"Quiz completo randomizzato  ({domandeMateria.Count} domande)",
                "Quiz per categorie (multi-selezione)",
                "Quiz di N domande (scelte casualmente)",
                $"Cronologia — {materia.Nome}"
            };
            int scelta = ConsoleUI.MostraMenuSceltaSingola($"Materia: {materia.Nome}", voci, "Indietro");
            if (scelta == -1) return;

            switch (scelta)
            {
                case 0:
                    EseguiSessioneConOpzioni(domandeMateria, storage, new OpzioniQuiz
                    {
                        NomeModalita = $"{materia.Nome} — Completo",
                        MateriaNomeLabel = materia.Nome,
                        RandomizzaOrdineDomande = false
                    });
                    break;

                case 1:
                    EseguiSessioneConOpzioni(domandeMateria, storage, new OpzioniQuiz
                    {
                        NomeModalita = $"{materia.Nome} — Completo randomizzato",
                        MateriaNomeLabel = materia.Nome,
                        RandomizzaOrdineDomande = true
                    });
                    break;

                case 2:
                    var riepilogo = QuizService.RiepilogoCategorie(domandeMateria);
                    if (riepilogo.Count == 0)
                    {
                        ConsoleUI.ScriviRiga("  Nessuna categoria disponibile.", ConsoleColor.Red);
                        ConsoleUI.Pausa();
                        break;
                    }
                    var categorie = ConsoleUI.SelezionaCategorieMulti(riepilogo);
                    if (categorie.Count == 0) break;
                    var filtrate = QuizService.FiltraPerCategorie(domandeMateria, categorie);
                    if (filtrate.Count == 0)
                    {
                        ConsoleUI.ScriviRiga("  Nessuna domanda per le categorie scelte.", ConsoleColor.Red);
                        ConsoleUI.Pausa();
                        break;
                    }
                    EseguiSessioneConOpzioni(filtrate, storage, new OpzioniQuiz
                    {
                        NomeModalita = $"{materia.Nome} — Per categoria",
                        MateriaNomeLabel = materia.Nome,
                        RandomizzaOrdineDomande = true,
                        Categorie = categorie
                    });
                    break;

                case 3:
                    int n = ConsoleUI.ChiediNumeroDomande(domandeMateria.Count);
                    if (n > 0)
                        EseguiSessioneConOpzioni(domandeMateria, storage, new OpzioniQuiz
                        {
                            NomeModalita = $"{materia.Nome} — {n} domande",
                            MateriaNomeLabel = materia.Nome,
                            RandomizzaOrdineDomande = true,
                            LimiteDomande = n
                        });
                    break;

                case 4:
                    ConsoleUI.MostraCronologia(storage.CaricaCronologia(), materia.Nome);
                    break;
            }
        }
    }

    // ============================================================ SESSIONE

    /// <summary>
    /// Chiede le opzioni (rotazione, cronometro) e avvia la sessione su un pool di domande.
    /// </summary>
    private static void EseguiSessioneConOpzioni(List<Domanda> pool, StorageService storage, OpzioniQuiz opz)
    {
        if (pool.Count == 0)
        {
            ConsoleUI.ScriviRiga("  Nessuna domanda disponibile.", ConsoleColor.Red);
            ConsoleUI.Pausa();
            return;
        }

        var (rotazione, cronometro) = ConsoleUI.ChiediOpzioniSessione();
        opz.Rotazione = rotazione;
        opz.Cronometro = cronometro;

        // Preparo l'ordine iniziale
        List<Domanda> ordine = opz.RandomizzaOrdineDomande
            ? QuizService.DomandeRandomizzate(pool)
            : pool.ToList();
        if (opz.LimiteDomande > 0 && ordine.Count > opz.LimiteDomande)
            ordine = ordine.Take(opz.LimiteDomande).ToList();

        var esito = opz.Rotazione
            ? EseguiSessioneRotazione(ordine, opz, storage, null)
            : EseguiSessioneClassica(ordine, opz, storage, null);

        if (esito.Pausata)
        {
            ConsoleUI.ScriviRiga("  Sessione messa in pausa. Potrai riprenderla dal menu principale.", ConsoleColor.Yellow);
            ConsoleUI.Pausa();
            return;
        }

        var risultato = esito.Risultato;

        risultato.ModalitaRotazione = opz.Rotazione;
        risultato.CronometroAttivo = opz.Cronometro;
        risultato.Modalita = opz.NomeModalita + (opz.Rotazione ? " ↻" : "");
        risultato.MateriaNome = opz.MateriaNomeLabel;
        risultato.CategorieSelezionate = opz.Categorie;
        risultato.Punteggio = QuizService.CalcolaPunteggio(risultato);

        if (risultato.TotaleDomande > 0)
        {
            try { storage.SalvaRisultato(risultato); }
            catch (Exception ex)
            {
                ConsoleUI.ScriviRiga($"  Attenzione: impossibile salvare cronologia ({ex.Message}).",
                    ConsoleColor.Red);
                ConsoleUI.Pausa();
            }
        }

        var sbagliate = risultato.Dettagli.Where(d => !d.Corretta).ToList();
        ConsoleUI.MostraRiepilogoFinale(risultato, sbagliate);
    }

    // ============================================================ SESSIONE CLASSICA

    private static EsitoSessione EseguiSessioneClassica(List<Domanda> ordine, OpzioniQuiz opz, StorageService storage, SessionePausa? stato)
    {
        int effettuatePregresse = stato?.EffettuateClassica ?? 0;
        int totalePrevisto = effettuatePregresse + ordine.Count;
        var risultato = new RisultatoQuiz
        {
            DataOra = DateTime.Now,
            TotaleDomande = totalePrevisto,
            Dettagli = stato?.Dettagli?.ToList() ?? new List<DettaglioRisposta>()
        };
        var tempoPregresso = stato?.TempoTrascorso ?? TimeSpan.Zero;
        var inizio = DateTime.Now - tempoPregresso;
        var cron = System.Diagnostics.Stopwatch.StartNew();
        int corrette = stato?.CorretteClassica ?? 0;
        int effettuate = effettuatePregresse;
        bool abbandonato = false;

        for (int i = 0; i < ordine.Count; i++)
        {
            var dp = QuizService.PreparaDomanda(ordine[i]);
            int numeroDomanda = effettuatePregresse + i + 1;
            int scelta = ConsoleUI.ChiediRisposta(dp, numeroDomanda, totalePrevisto, corrette,
                                                   risultato.Dettagli, inizio, opz.Cronometro,
                                                   opz.MateriaNomeLabel);
            if (scelta == -1) { abbandonato = true; break; }
            if (scelta == -2)
            {
                storage.SalvaPausa(new SessionePausa
                {
                    SessioneId = stato?.SessioneId ?? Guid.NewGuid().ToString("N"),
                    DataOraPausa = DateTime.Now,
                    Opzioni = opz,
                    ModalitaRotazione = false,
                    CodaDomandeIds = ordine.Skip(i).Select(d => d.Id).ToList(),
                    CorretteClassica = corrette,
                    EffettuateClassica = effettuate,
                    TempoTrascorso = tempoPregresso + cron.Elapsed,
                    Dettagli = risultato.Dettagli.ToList()
                });
                return new EsitoSessione(risultato, true);
            }

            bool ok = scelta == dp.IndiceCorrettoShufflato;
            if (ok) corrette++;
            effettuate++;

            string lettera = ((char)('A' + scelta)).ToString();
            risultato.Dettagli.Add(new DettaglioRisposta
            {
                IdDomanda = dp.Originale.Id,
                Categoria = dp.Originale.Categoria,
                MateriaNome = dp.Originale.MateriaNome,
                TestoDomanda = dp.Originale.DomandaTesto,
                RispostaData = $"[{lettera}] {dp.RisposteShufflate[scelta]}",
                RispostaCorretta = $"[{dp.LetteraCorretta}] {dp.TestoRispostaCorretta}",
                Corretta = ok,
                Spiegazione = dp.Originale.Spiegazione,
                Tentativi = 1
            });

            ConsoleUI.MostraFeedback(dp, scelta, corrette, numeroDomanda, totalePrevisto, inizio, opz.Cronometro);
        }

        cron.Stop();
        risultato.RisposteCorrette = corrette;
        risultato.RisposteErrate = effettuate - corrette;
        risultato.DurataQuiz = tempoPregresso + cron.Elapsed;
        risultato.Abbandonato = abbandonato;
        risultato.TotaleDomande = effettuate;
        risultato.PercentualeCorrette = effettuate == 0 ? 0 : 100.0 * corrette / effettuate;
        if (!string.IsNullOrWhiteSpace(stato?.SessioneId))
            storage.EliminaPausa(stato.SessioneId);
        return new EsitoSessione(risultato, false);
    }

    // ============================================================ SESSIONE ROTAZIONE

    /// <summary>
    /// Modalità rotazione: le sbagliate rientrano in coda con posizione casuale.
    /// Termina solo quando tutte sono state risposte correttamente almeno una volta.
    /// </summary>
    private static EsitoSessione EseguiSessioneRotazione(List<Domanda> ordine, OpzioniQuiz opz, StorageService storage, SessionePausa? stato)
    {
        var risultato = new RisultatoQuiz
        {
            DataOra = DateTime.Now,
            TotaleDomande = ordine.Count,
            Dettagli = stato?.Dettagli?.ToList() ?? new List<DettaglioRisposta>()
        };
        var tempoPregresso = stato?.TempoTrascorso ?? TimeSpan.Zero;
        var inizio = DateTime.Now - tempoPregresso;
        var cron = System.Diagnostics.Stopwatch.StartNew();

        // Coda di domande ancora da padroneggiare (id → tentativi fatti)
        var daFare = new LinkedList<Domanda>(ordine);
        var tentativiPerId = stato?.TentativiPerId?.ToDictionary(kv => kv.Key, kv => kv.Value)
                            ?? new Dictionary<string, int>();
        var corrette = stato?.CorretteIds != null
            ? new HashSet<string>(stato.CorretteIds)
            : new HashSet<string>();
        int totUnico = stato?.TotaleUnicheRotazione ?? ordine.Count;
        int sbagliateContatore = stato?.SbagliateContatore ?? 0;
        bool abbandonato = false;
        var rng = new Random();
        int indicePosizione = stato?.IndicePosizioneRotazione ?? 0;

        while (daFare.Count > 0)
        {
            var corrente = daFare.First!.Value;
            daFare.RemoveFirst();
            var dp = QuizService.PreparaDomanda(corrente);
            indicePosizione++;
            int numCorrette = corrette.Count;

            int scelta = ConsoleUI.ChiediRisposta(dp, indicePosizione, totUnico + sbagliateContatore,
                                                  numCorrette, risultato.Dettagli, inizio, opz.Cronometro,
                                                  opz.MateriaNomeLabel);
            if (scelta == -1) { abbandonato = true; break; }
            if (scelta == -2)
            {
                storage.SalvaPausa(new SessionePausa
                {
                    SessioneId = stato?.SessioneId ?? Guid.NewGuid().ToString("N"),
                    DataOraPausa = DateTime.Now,
                    Opzioni = opz,
                    ModalitaRotazione = true,
                    CodaDomandeIds = daFare.Prepend(corrente).Select(d => d.Id).ToList(),
                    TentativiPerId = tentativiPerId,
                    CorretteIds = corrette.ToList(),
                    TotaleUnicheRotazione = totUnico,
                    SbagliateContatore = sbagliateContatore,
                    IndicePosizioneRotazione = indicePosizione - 1,
                    TempoTrascorso = tempoPregresso + cron.Elapsed,
                    Dettagli = risultato.Dettagli.ToList()
                });
                return new EsitoSessione(risultato, true);
            }

            tentativiPerId[corrente.Id] = tentativiPerId.GetValueOrDefault(corrente.Id, 0) + 1;
            bool ok = scelta == dp.IndiceCorrettoShufflato;
            string lettera = ((char)('A' + scelta)).ToString();
            risultato.Dettagli.Add(new DettaglioRisposta
            {
                IdDomanda = dp.Originale.Id,
                Categoria = dp.Originale.Categoria,
                MateriaNome = dp.Originale.MateriaNome,
                TestoDomanda = dp.Originale.DomandaTesto,
                RispostaData = $"[{lettera}] {dp.RisposteShufflate[scelta]}",
                RispostaCorretta = $"[{dp.LetteraCorretta}] {dp.TestoRispostaCorretta}",
                Corretta = ok,
                Spiegazione = dp.Originale.Spiegazione,
                Tentativi = tentativiPerId[corrente.Id]
            });

            if (ok)
            {
                corrette.Add(corrente.Id);
            }
            else
            {
                sbagliateContatore++;
                // Rimetto in coda in posizione casuale (non subito dopo)
                int pos = daFare.Count == 0 ? 0 : rng.Next(1, daFare.Count + 1);
                if (pos >= daFare.Count) daFare.AddLast(corrente);
                else
                {
                    var node = daFare.First;
                    for (int k = 0; k < pos && node != null; k++) node = node.Next;
                    if (node == null) daFare.AddLast(corrente);
                    else daFare.AddBefore(node, corrente);
                }
            }

            ConsoleUI.MostraFeedback(dp, scelta, corrette.Count, indicePosizione,
                                     totUnico + sbagliateContatore, inizio, opz.Cronometro);
        }

        cron.Stop();
        int rispPrimoColpo = risultato.Dettagli.Count(d => d.Corretta && d.Tentativi == 1);
        int rispTotaliCorrette = risultato.Dettagli.Count(d => d.Corretta);
        int rispTotaliErrate = risultato.Dettagli.Count(d => !d.Corretta);

        risultato.RisposteCorrette = rispTotaliCorrette;
        risultato.RisposteErrate = rispTotaliErrate;
        risultato.DurataQuiz = tempoPregresso + cron.Elapsed;
        risultato.Abbandonato = abbandonato;
        risultato.TotaleDomande = risultato.Dettagli.Count;
        // Percentuale basata sul primo colpo: misura reale di padronanza
        risultato.PercentualeCorrette = totUnico == 0 ? 0 : 100.0 * rispPrimoColpo / totUnico;
        if (!string.IsNullOrWhiteSpace(stato?.SessioneId))
            storage.EliminaPausa(stato.SessioneId);
        return new EsitoSessione(risultato, false);
    }

    private static void SelezionaERiprendiPausa(List<SessionePausa> pause, List<Domanda> tutteDomande, StorageService storage)
    {
        var ordinate = pause.OrderByDescending(p => p.DataOraPausa).ToList();
        var voci = ordinate
            .Select(p => $"{p.DataOraPausa:dd/MM HH:mm}  ·  {p.Opzioni.NomeModalita}  ·  restanti {p.CodaDomandeIds.Count}")
            .ToList();
        int scelta = ConsoleUI.MostraMenuSceltaSingola("Riprendi quiz in pausa", voci, "Indietro");
        if (scelta == -1) return;
        RiprendiSessioneInPausa(ordinate[scelta], tutteDomande, storage);
    }

    private static void RiprendiSessioneInPausa(SessionePausa pausa, List<Domanda> tutteDomande, StorageService storage)
    {
        var mappa = tutteDomande.ToDictionary(d => d.Id, d => d);
        var ordine = new List<Domanda>();
        foreach (var id in pausa.CodaDomandeIds)
        {
            if (mappa.TryGetValue(id, out var d))
                ordine.Add(d);
        }

        if (ordine.Count == 0)
        {
            ConsoleUI.ScriviRiga("  Impossibile riprendere: alcune domande non sono piu disponibili.", ConsoleColor.Red);
            storage.EliminaPausa(pausa.SessioneId);
            ConsoleUI.Pausa();
            return;
        }

        var esito = pausa.ModalitaRotazione
            ? EseguiSessioneRotazione(ordine, pausa.Opzioni, storage, pausa)
            : EseguiSessioneClassica(ordine, pausa.Opzioni, storage, pausa);

        if (esito.Pausata)
        {
            ConsoleUI.ScriviRiga("  Sessione aggiornata e mantenuta in pausa.", ConsoleColor.Yellow);
            ConsoleUI.Pausa();
            return;
        }

        var risultato = esito.Risultato;
        risultato.ModalitaRotazione = pausa.Opzioni.Rotazione;
        risultato.CronometroAttivo = pausa.Opzioni.Cronometro;
        risultato.Modalita = pausa.Opzioni.NomeModalita + (pausa.Opzioni.Rotazione ? " ↻" : "");
        risultato.MateriaNome = pausa.Opzioni.MateriaNomeLabel;
        risultato.CategorieSelezionate = pausa.Opzioni.Categorie;
        risultato.Punteggio = QuizService.CalcolaPunteggio(risultato);

        if (risultato.TotaleDomande > 0)
        {
            try { storage.SalvaRisultato(risultato); }
            catch (Exception ex)
            {
                ConsoleUI.ScriviRiga($"  Attenzione: impossibile salvare cronologia ({ex.Message}).",
                    ConsoleColor.Red);
                ConsoleUI.Pausa();
            }
        }

        var sbagliate = risultato.Dettagli.Where(d => !d.Corretta).ToList();
        ConsoleUI.MostraRiepilogoFinale(risultato, sbagliate);
    }
}
