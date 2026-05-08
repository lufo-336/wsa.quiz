using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading;
using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.State;

/// <summary>
/// State machine di una sessione di quiz, condivisa fra modalita' classica e rotazione.
/// Espone proprieta' observable (testo domanda, risposte, progresso, feedback, tempo)
/// che la <c>QuizView</c> binda direttamente. La logica e' la stessa del console
/// (vedi <c>Wsa.Quiz.Cli.Program.EseguiSessione*</c>) ma riformulata in modo event-driven:
/// niente loop bloccante, ogni interazione utente avanza lo stato.
///
/// <para>Ciclo di vita:</para>
/// <list type="number">
///   <item><see cref="Avvia"/> — porta la prima domanda a video.</item>
///   <item><see cref="RispondiA"/> — l'utente clicca una risposta. Si entra in stato "feedback".</item>
///   <item><see cref="Avanza"/> — l'utente clicca "Prossima". Carica domanda successiva o solleva <see cref="Concluso"/>.</item>
///   <item><see cref="Abbandona"/> — esce e solleva <see cref="Concluso"/> con flag abbandonato.</item>
/// </list>
/// </summary>
public class SessioneQuiz : ObservableObject
{
    // ------------------------------------------------------------------ STATO COMUNE

    public OpzioniQuiz Opzioni { get; }
    private readonly List<Domanda> _poolIniziale;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _cron = new();
    private DateTime _inizio;
    private DomandaPreparata? _domandaCorrente;

    // Identita' della pausa (riusato in caso di re-pausa, e per cancellarla a fine sessione)
    private string _sessioneId = Guid.NewGuid().ToString("N");
    private bool _avviataDaRipresa;

    // Offset usati quando la sessione viene ripresa da una pausa
    private TimeSpan _offsetCronometro = TimeSpan.Zero;
    private int _offsetEffettuate; // classica: domande gia' fatte nelle sessioni pregresse

    // Sessione classica
    private List<Domanda> _ordineClassica = new();
    private int _indiceClassica;
    private int _correttoClassica;

    // Sessione rotazione
    private LinkedList<Domanda> _codaRotazione = new();
    private Dictionary<string, int> _tentativiPerId = new();
    private HashSet<string> _corretteIds = new();
    private int _totUnicoRotazione;
    private int _sbagliateContatoreRotazione;
    private int _indicePosizioneRotazione;
    private static readonly Random Rng = new();

    /// <summary>
    /// Id della pausa da cui questa sessione e' stata ripresa, o <c>null</c> se la sessione
    /// e' stata avviata da zero. Viene usato dalla <c>MainWindow</c> per cancellare la pausa
    /// originale al termine della sessione.
    /// </summary>
    public string? IdSessionePausa => _avviataDaRipresa ? _sessioneId : null;

    /// <summary>Risultato della sessione. Compilato durante l'esecuzione e finalizzato a chiusura.</summary>
    public RisultatoQuiz Risultato { get; } = new();

    public bool Abbandonato { get; private set; }

    // ------------------------------------------------------------------ EVENTI

    /// <summary>
    /// La sessione e' terminata (regolarmente o per abbandono). La <c>MainWindow</c>
    /// si occupa di passare il <see cref="Risultato"/> alla schermata di riepilogo
    /// e di salvarlo in cronologia.
    /// </summary>
    public event EventHandler? Concluso;

    // ------------------------------------------------------------------ PROPRIETA' OBSERVABLE

    private string _domandaTesto = string.Empty;
    public string DomandaTesto { get => _domandaTesto; private set => SetField(ref _domandaTesto, value); }

    private string _categoriaCorrente = string.Empty;
    public string CategoriaCorrente { get => _categoriaCorrente; private set => SetField(ref _categoriaCorrente, value); }

    private string _materiaCorrente = string.Empty;
    public string MateriaCorrente { get => _materiaCorrente; private set => SetField(ref _materiaCorrente, value); }

    private int _numeroDomandaCorrente;
    public int NumeroDomandaCorrente { get => _numeroDomandaCorrente; private set => SetField(ref _numeroDomandaCorrente, value); }

    private int _totalePrevisto;
    public int TotalePrevisto
    {
        get => _totalePrevisto;
        private set
        {
            if (SetField(ref _totalePrevisto, value))
                RaisePropertyChanged(nameof(ProgressoPercentuale));
        }
    }

    private int _corrette;
    public int Corrette { get => _corrette; private set => SetField(ref _corrette, value); }

    private int _errate;
    public int Errate { get => _errate; private set => SetField(ref _errate, value); }

    public double ProgressoPercentuale =>
        _totalePrevisto == 0 ? 0 : 100.0 * (_numeroDomandaCorrente - 1) / _totalePrevisto;

    private string _tempo = "00:00";
    public string Tempo { get => _tempo; private set => SetField(ref _tempo, value); }

    /// <summary>Visibilita' del cronometro nell'header.</summary>
    public bool MostraTempo => Opzioni.Cronometro;

    public ObservableCollection<RispostaItem> Risposte { get; } = new();

    private bool _rispostaInviata;
    public bool RispostaInviata
    {
        get => _rispostaInviata;
        private set
        {
            if (SetField(ref _rispostaInviata, value))
                RaisePropertyChanged(nameof(InAttesaRisposta));
        }
    }

    /// <summary>Inverso di <see cref="RispostaInviata"/>, utile in XAML.</summary>
    public bool InAttesaRisposta => !_rispostaInviata;

    private bool _ultimaRispostaCorretta;
    public bool UltimaRispostaCorretta { get => _ultimaRispostaCorretta; private set => SetField(ref _ultimaRispostaCorretta, value); }

    private string _feedbackTitolo = string.Empty;
    public string FeedbackTitolo { get => _feedbackTitolo; private set => SetField(ref _feedbackTitolo, value); }

    private string _spiegazioneTesto = string.Empty;
    public string SpiegazioneTesto { get => _spiegazioneTesto; private set => SetField(ref _spiegazioneTesto, value); }

    private string _letteraCorretta = string.Empty;
    public string LetteraCorretta { get => _letteraCorretta; private set => SetField(ref _letteraCorretta, value); }

    private string _testoRispostaCorretta = string.Empty;
    public string TestoRispostaCorretta { get => _testoRispostaCorretta; private set => SetField(ref _testoRispostaCorretta, value); }

    // ------------------------------------------------------------------ COSTRUZIONE

    public SessioneQuiz(IEnumerable<Domanda> pool, OpzioniQuiz opzioni)
    {
        _poolIniziale = pool.ToList();
        Opzioni = opzioni;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => AggiornaTempo();
    }

    // ------------------------------------------------------------------ AVVIO

    public void Avvia()
    {
        if (!_avviataDaRipresa)
        {
            // Preparo l'ordine iniziale come fa Program.EseguiSessioneConOpzioni
            var ordine = Opzioni.RandomizzaOrdineDomande
                ? QuizService.DomandeRandomizzate(_poolIniziale)
                : _poolIniziale.ToList();
            if (Opzioni.LimiteDomande > 0 && ordine.Count > Opzioni.LimiteDomande)
                ordine = ordine.Take(Opzioni.LimiteDomande).ToList();

            if (Opzioni.Rotazione)
            {
                _codaRotazione = new LinkedList<Domanda>(ordine);
                _totUnicoRotazione = ordine.Count;
                TotalePrevisto = _totUnicoRotazione;
            }
            else
            {
                _ordineClassica = ordine;
                TotalePrevisto = ordine.Count;
            }
        }
        else
        {
            // Sessione ripresa: pool e contatori sono gia' stati popolati da RiprendiDa.
            TotalePrevisto = Opzioni.Rotazione
                ? _totUnicoRotazione + _sbagliateContatoreRotazione
                : _ordineClassica.Count + _offsetEffettuate;
        }

        Risultato.DataOra = DateTime.Now;
        Risultato.Modalita = Opzioni.NomeModalita + (Opzioni.Rotazione ? " ↻" : "");
        Risultato.MateriaNome = Opzioni.MateriaNomeLabel;
        Risultato.CategorieSelezionate = Opzioni.Categorie;
        Risultato.ModalitaRotazione = Opzioni.Rotazione;
        Risultato.CronometroAttivo = Opzioni.Cronometro;

        _inizio = DateTime.Now - _offsetCronometro;
        _cron.Start();
        if (Opzioni.Cronometro) _timer.Start();

        CaricaProssimaDomanda();
    }

    // ------------------------------------------------------------------ FLUSSO DOMANDA

    private void CaricaProssimaDomanda()
    {
        Domanda? prossima;
        if (Opzioni.Rotazione)
        {
            if (_codaRotazione.Count == 0) { Concludi(false); return; }
            prossima = _codaRotazione.First!.Value;
            _codaRotazione.RemoveFirst();
            _indicePosizioneRotazione++;
            NumeroDomandaCorrente = _indicePosizioneRotazione;
            TotalePrevisto = _totUnicoRotazione + _sbagliateContatoreRotazione;
        }
        else
        {
            if (_indiceClassica >= _ordineClassica.Count) { Concludi(false); return; }
            prossima = _ordineClassica[_indiceClassica];
            NumeroDomandaCorrente = _indiceClassica + 1 + _offsetEffettuate;
        }

        _domandaCorrente = QuizService.PreparaDomanda(prossima);

        DomandaTesto = _domandaCorrente.Originale.DomandaTesto;
        CategoriaCorrente = _domandaCorrente.Originale.Categoria;
        MateriaCorrente = _domandaCorrente.Originale.MateriaNome;

        // Ricostruisco la collezione (4 elementi: cheap; e i RispostaItem sono nuovi
        // -> stato Neutra di default, niente residui dalla domanda precedente)
        Risposte.Clear();
        for (int i = 0; i < _domandaCorrente.RisposteShufflate.Count; i++)
            Risposte.Add(new RispostaItem(i, _domandaCorrente.RisposteShufflate[i]));

        // reset feedback
        RispostaInviata = false;
        FeedbackTitolo = string.Empty;
        SpiegazioneTesto = string.Empty;
        LetteraCorretta = string.Empty;
        TestoRispostaCorretta = string.Empty;
        UltimaRispostaCorretta = false;

        RaisePropertyChanged(nameof(ProgressoPercentuale));
    }

    /// <summary>Registrazione della risposta e transizione allo stato di feedback.</summary>
    public void RispondiA(int indiceShufflato)
    {
        if (RispostaInviata) return;            // gia' risposto: ignora doppi click
        if (_domandaCorrente == null) return;

        bool ok = indiceShufflato == _domandaCorrente.IndiceCorrettoShufflato;
        string lettera = ((char)('A' + indiceShufflato)).ToString();

        // Aggiorno il dettaglio in cronologia (stesso schema del console)
        int tentativi = 1;
        if (Opzioni.Rotazione)
        {
            string id = _domandaCorrente.Originale.Id;
            _tentativiPerId[id] = _tentativiPerId.GetValueOrDefault(id, 0) + 1;
            tentativi = _tentativiPerId[id];
        }

        Risultato.Dettagli.Add(new DettaglioRisposta
        {
            IdDomanda = _domandaCorrente.Originale.Id,
            Categoria = _domandaCorrente.Originale.Categoria,
            MateriaNome = _domandaCorrente.Originale.MateriaNome,
            TestoDomanda = _domandaCorrente.Originale.DomandaTesto,
            RispostaData = $"[{lettera}] {_domandaCorrente.RisposteShufflate[indiceShufflato]}",
            RispostaCorretta = $"[{_domandaCorrente.LetteraCorretta}] {_domandaCorrente.TestoRispostaCorretta}",
            Corretta = ok,
            Spiegazione = _domandaCorrente.Originale.Spiegazione,
            Tentativi = tentativi
        });

        // Aggiorno gli stati visibili delle risposte
        for (int i = 0; i < Risposte.Count; i++)
        {
            if (i == _domandaCorrente.IndiceCorrettoShufflato)
                Risposte[i].Stato = StatoRisposta.Corretta;
            else if (i == indiceShufflato)
                Risposte[i].Stato = StatoRisposta.Sbagliata;
        }

        // Stato sessione + feedback
        if (Opzioni.Rotazione)
        {
            if (ok)
            {
                _corretteIds.Add(_domandaCorrente.Originale.Id);
            }
            else
            {
                _sbagliateContatoreRotazione++;
                // rimetti in coda in posizione casuale (stesso algoritmo di Program.EseguiSessioneRotazione)
                int pos = _codaRotazione.Count == 0 ? 0 : Rng.Next(1, _codaRotazione.Count + 1);
                if (pos >= _codaRotazione.Count) _codaRotazione.AddLast(_domandaCorrente.Originale);
                else
                {
                    var node = _codaRotazione.First;
                    for (int k = 0; k < pos && node != null; k++) node = node.Next;
                    if (node == null) _codaRotazione.AddLast(_domandaCorrente.Originale);
                    else _codaRotazione.AddBefore(node, _domandaCorrente.Originale);
                }
            }
            Corrette = _corretteIds.Count;
            Errate = _sbagliateContatoreRotazione;
            TotalePrevisto = _totUnicoRotazione + _sbagliateContatoreRotazione;
        }
        else
        {
            if (ok) _correttoClassica++;
            Corrette = _correttoClassica;
            Errate = (NumeroDomandaCorrente) - _correttoClassica;
        }

        UltimaRispostaCorretta = ok;
        FeedbackTitolo = ok ? "Corretto!" : "Sbagliato";
        SpiegazioneTesto = _domandaCorrente.Originale.Spiegazione ?? string.Empty;
        LetteraCorretta = _domandaCorrente.LetteraCorretta.ToString();
        TestoRispostaCorretta = _domandaCorrente.TestoRispostaCorretta;
        RispostaInviata = true;
    }

    /// <summary>Avanza alla domanda successiva (o conclude se finita).</summary>
    public void Avanza()
    {
        if (!RispostaInviata) return; // evita avanzamenti senza risposta
        if (!Opzioni.Rotazione) _indiceClassica++;
        CaricaProssimaDomanda();
    }

    public void Abbandona()
    {
        Concludi(true);
    }

    // ------------------------------------------------------------------ CHIUSURA

    private void Concludi(bool abbandonato)
    {
        _cron.Stop();
        _timer.Stop();
        Abbandonato = abbandonato;
        Risultato.Abbandonato = abbandonato;
        Risultato.DurataQuiz = _cron.Elapsed + _offsetCronometro;

        if (Opzioni.Rotazione)
        {
            int rispPrimoColpo = Risultato.Dettagli.Count(d => d.Corretta && d.Tentativi == 1);
            Risultato.RisposteCorrette = Risultato.Dettagli.Count(d => d.Corretta);
            Risultato.RisposteErrate   = Risultato.Dettagli.Count(d => !d.Corretta);
            Risultato.TotaleDomande    = Risultato.Dettagli.Count;
            Risultato.PercentualeCorrette = _totUnicoRotazione == 0
                ? 0
                : 100.0 * rispPrimoColpo / _totUnicoRotazione;
        }
        else
        {
            int effettuate = Risultato.Dettagli.Count;
            Risultato.RisposteCorrette = _correttoClassica;
            Risultato.RisposteErrate   = effettuate - _correttoClassica;
            Risultato.TotaleDomande    = effettuate;
            Risultato.PercentualeCorrette = effettuate == 0 ? 0 : 100.0 * _correttoClassica / effettuate;
        }

        Risultato.Punteggio = QuizService.CalcolaPunteggio(Risultato);
        Concluso?.Invoke(this, EventArgs.Empty);
    }

    private void AggiornaTempo()
    {
        var t = _cron.Elapsed + _offsetCronometro;
        Tempo = t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }

    // ------------------------------------------------------------------ PAUSA: ESPORTA / RIPRENDI

    /// <summary>
    /// Snapshot serializzabile dello stato corrente. Ferma cronometro e timer:
    /// dopo questa chiamata la sessione e' considerata dismessa (la
    /// <c>QuizView</c> la sostituisce con la <c>HomeView</c>).
    ///
    /// Funziona sia in stato "in attesa di risposta" sia in stato "feedback
    /// pendente". Il discriminante e' <see cref="RispostaInviata"/>.
    /// </summary>
    public SessionePausa EsportaPausa()
    {
        _cron.Stop();
        _timer.Stop();

        var pausa = new SessionePausa
        {
            SessioneId = _sessioneId,
            DataOraPausa = DateTime.Now,
            Opzioni = Opzioni,
            ModalitaRotazione = Opzioni.Rotazione,
            TempoTrascorso = _cron.Elapsed + _offsetCronometro,
            Dettagli = Risultato.Dettagli.ToList()
        };

        if (Opzioni.Rotazione)
        {
            // Coda di domande ancora da padroneggiare. Se siamo nello stato
            // "in attesa", la domanda corrente e' gia' stata estratta dalla
            // coda ma non ancora risposta: va rimessa in testa.
            var coda = new List<string>();
            if (!RispostaInviata && _domandaCorrente != null)
                coda.Add(_domandaCorrente.Originale.Id);
            foreach (var d in _codaRotazione) coda.Add(d.Id);

            pausa.CodaDomandeIds = coda;
            pausa.TentativiPerId = new Dictionary<string, int>(_tentativiPerId);
            pausa.CorretteIds = _corretteIds.ToList();
            pausa.TotaleUnicheRotazione = _totUnicoRotazione;
            pausa.SbagliateContatore = _sbagliateContatoreRotazione;
            // In stato "in attesa" l'indice e' gia' stato incrementato in
            // CaricaProssimaDomanda; per coerenza con il console (vedi
            // wsa.quiz.cli/Program.cs:431) salvo la posizione "pre-mostra".
            pausa.IndicePosizioneRotazione = RispostaInviata
                ? _indicePosizioneRotazione
                : _indicePosizioneRotazione - 1;
        }
        else
        {
            // Classica: in stato "in attesa" la domanda corrente NON e' ancora
            // stata risposta -> coda da _indiceClassica. In stato "post-feedback"
            // la domanda corrente e' gia' in Dettagli -> coda da _indiceClassica + 1.
            int skip = RispostaInviata ? _indiceClassica + 1 : _indiceClassica;
            pausa.CodaDomandeIds = _ordineClassica.Skip(skip).Select(d => d.Id).ToList();
            pausa.CorretteClassica = _correttoClassica;
            pausa.EffettuateClassica = skip + _offsetEffettuate;
        }

        return pausa;
    }

    /// <summary>
    /// Costruisce una <see cref="SessioneQuiz"/> a partire da un <see cref="SessionePausa"/>
    /// salvato (dal console o dalla GUI). Replica la logica di
    /// <c>Program.RiprendiSessioneInPausa</c> del console.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Se nessuna delle domande della pausa esiste piu' nel pool corrente
    /// (la pausa e' "orfana" — la <c>MainWindow</c> la elimina).
    /// </exception>
    public static SessioneQuiz RiprendiDa(SessionePausa pausa, IDictionary<string, Domanda> mappaPerId)
    {
        var ricostruite = new List<Domanda>();
        foreach (var id in pausa.CodaDomandeIds)
            if (mappaPerId.TryGetValue(id, out var d)) ricostruite.Add(d);

        if (ricostruite.Count == 0)
            throw new InvalidOperationException(
                "Le domande di questa pausa non sono piu' disponibili.");

        var sessione = new SessioneQuiz(ricostruite, pausa.Opzioni)
        {
            _sessioneId = pausa.SessioneId,
            _avviataDaRipresa = true,
            _offsetCronometro = pausa.TempoTrascorso
        };

        // Dettagli cumulativi delle risposte gia' date pre-pausa
        foreach (var dett in pausa.Dettagli) sessione.Risultato.Dettagli.Add(dett);

        if (pausa.Opzioni.Rotazione)
        {
            sessione._codaRotazione = new LinkedList<Domanda>(ricostruite);
            sessione._tentativiPerId = new Dictionary<string, int>(pausa.TentativiPerId);
            sessione._corretteIds = new HashSet<string>(pausa.CorretteIds);
            sessione._totUnicoRotazione = pausa.TotaleUnicheRotazione;
            sessione._sbagliateContatoreRotazione = pausa.SbagliateContatore;
            sessione._indicePosizioneRotazione = pausa.IndicePosizioneRotazione;
            sessione.Corrette = sessione._corretteIds.Count;
            sessione.Errate = sessione._sbagliateContatoreRotazione;
        }
        else
        {
            sessione._ordineClassica = ricostruite;
            sessione._indiceClassica = 0;
            sessione._correttoClassica = pausa.CorretteClassica;
            sessione._offsetEffettuate = pausa.EffettuateClassica;
            sessione.Corrette = pausa.CorretteClassica;
            sessione.Errate = pausa.EffettuateClassica - pausa.CorretteClassica;
        }

        return sessione;
    }
}
