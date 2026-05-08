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

    // Sessione classica
    private List<Domanda> _ordineClassica = new();
    private int _indiceClassica;
    private int _correttoClassica;

    // Sessione rotazione
    private LinkedList<Domanda> _codaRotazione = new();
    private readonly Dictionary<string, int> _tentativiPerId = new();
    private readonly HashSet<string> _corretteIds = new();
    private int _totUnicoRotazione;
    private int _sbagliateContatoreRotazione;
    private int _indicePosizioneRotazione;
    private static readonly Random Rng = new();

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
            TotalePrevisto = _totUnicoRotazione; // si aggiornera' man mano che le sbagliate rientrano
        }
        else
        {
            _ordineClassica = ordine;
            TotalePrevisto = ordine.Count;
        }

        Risultato.DataOra = DateTime.Now;
        Risultato.Modalita = Opzioni.NomeModalita + (Opzioni.Rotazione ? " ↻" : "");
        Risultato.MateriaNome = Opzioni.MateriaNomeLabel;
        Risultato.CategorieSelezionate = Opzioni.Categorie;
        Risultato.ModalitaRotazione = Opzioni.Rotazione;
        Risultato.CronometroAttivo = Opzioni.Cronometro;

        _inizio = DateTime.Now;
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
            NumeroDomandaCorrente = _indiceClassica + 1;
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
        Risultato.DurataQuiz = _cron.Elapsed;

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
        var t = _cron.Elapsed;
        Tempo = t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }
}
