using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Schermata di configurazione di una nuova sessione di quiz.
/// E' la sua stessa DataContext (no MVVM): espone proprieta' observable
/// usate dai binding XAML, e gestisce l'avvio della sessione tramite event.
/// </summary>
public partial class HomeView : UserControl, INotifyPropertyChanged
{
    /// <summary>Sollevato al click di "Avvia quiz". L'argomento e' la sessione gia' configurata ma non ancora avviata.</summary>
    public event EventHandler<SessioneQuiz>? QuizAvviato;

    public new event PropertyChangedEventHandler? PropertyChanged;

    // ------------------------------------------------------------------ DATI

    /// <summary>Tutte le domande caricate (passate dalla MainWindow). Servono per costruire il pool al click di Avvia.</summary>
    private List<Domanda> _tutteDomande = new();

    /// <summary>Mappa materiaId -> tutte le sue categorie (preservando lo stato IsSelected fra rebuild).</summary>
    private readonly Dictionary<string, List<CategoriaSelezionabile>> _categoriePerMateria = new();

    public ObservableCollection<MateriaSelezionabile> Materie { get; } = new();
    public ObservableCollection<CategoriaSelezionabile> CategorieVisibili { get; } = new();

    // ------------------------------------------------------------------ OPZIONI

    private bool _randomizzaOrdine = true;
    public bool RandomizzaOrdine
    {
        get => _randomizzaOrdine;
        set { if (_randomizzaOrdine != value) { _randomizzaOrdine = value; Raise(); AggiornaRiepilogo(); } }
    }

    private bool _rotazione;
    public bool Rotazione
    {
        get => _rotazione;
        set { if (_rotazione != value) { _rotazione = value; Raise(); AggiornaRiepilogo(); } }
    }

    private bool _cronometro;
    public bool Cronometro
    {
        get => _cronometro;
        set { if (_cronometro != value) { _cronometro = value; Raise(); } }
    }

    private bool _limitaDomande;
    public bool LimitaDomande
    {
        get => _limitaDomande;
        set { if (_limitaDomande != value) { _limitaDomande = value; Raise(); AggiornaRiepilogo(); } }
    }

    private decimal _limiteN = 10;
    public decimal LimiteN
    {
        get => _limiteN;
        set { if (_limiteN != value) { _limiteN = value; Raise(); AggiornaRiepilogo(); } }
    }

    // ------------------------------------------------------------------ STATO DERIVATO (per UI)

    public bool NessunaCategoriaDisponibile => CategorieVisibili.Count == 0;

    private bool _haCategorieSelezionate;
    public bool HaCategorieSelezionate
    {
        get => _haCategorieSelezionate;
        private set { if (_haCategorieSelezionate != value) { _haCategorieSelezionate = value; Raise(); } }
    }

    private string _riepilogoSelezione = string.Empty;
    public string RiepilogoSelezione
    {
        get => _riepilogoSelezione;
        private set { if (_riepilogoSelezione != value) { _riepilogoSelezione = value; Raise(); } }
    }

    private string _avvisoSelezione = string.Empty;
    public string AvvisoSelezione
    {
        get => _avvisoSelezione;
        private set { if (_avvisoSelezione != value) { _avvisoSelezione = value; Raise(); } }
    }

    public bool HaAvviso => !string.IsNullOrEmpty(_avvisoSelezione);

    private bool _puoAvviare;
    public bool PuoAvviare
    {
        get => _puoAvviare;
        private set { if (_puoAvviare != value) { _puoAvviare = value; Raise(); } }
    }

    // ------------------------------------------------------------------ COSTRUZIONE

    public HomeView()
    {
        InitializeComponent();
        DataContext = this;
        AggiornaRiepilogo();
        // Step 8: handler in fase di tunneling (capture). Se restassimo sul
        // classico override OnKeyDown (bubble), la ScrollViewer che avvolge
        // tutto il contenuto di HomeView intercetta su/giu' per lo scroll e
        // marca l'evento come Handled prima che arrivi a noi. Con il Tunnel
        // riceviamo l'evento dall'alto verso il focused element, prima della
        // ScrollViewer.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    // ------------------------------------------------------------------ TASTIERA HOME (step 8)

    /// <summary>
    /// Step 8: dentro una zona (Materie, Categorie, Opzioni, Avvia) le frecce su/giu'
    /// spostano il focus fra i controlli focusable della zona stessa.
    /// Filtro: sul NumericUpDown lasciamo che le frecce incrementino il valore.
    /// </summary>
    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Up && e.Key != Key.Down) return;

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        if (focused == null) return;

        // Se il focus e' su un NumericUpDown (o suo interno), lascia il comportamento nativo.
        if (focused is NumericUpDown || focused.FindAncestorOfType<NumericUpDown>() != null)
            return;

        // Trova il Border-zona piu' vicino antenato.
        var zona = focused.GetVisualAncestors()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name is "ZonaAvvia" or "ZonaMaterie" or "ZonaCategorie" or "ZonaOpzioni");
        if (zona == null) return;

        // Raccogli tutti i controlli focusable dentro la zona, in ordine visivo.
        var focusables = zona.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled)
            .Where(c => c is Button || c is CheckBox || c is NumericUpDown)
            .ToList();
        if (focusables.Count == 0) return;

        int idx = focusables.IndexOf(focused);
        if (idx < 0)
        {
            focusables[0].Focus();
            e.Handled = true;
            return;
        }

        int delta = e.Key == Key.Down ? +1 : -1;
        int nuovo = idx + delta;
        // No wrap: se sforiamo, restiamo dove siamo (Tab serve a cambiare zona).
        if (nuovo < 0 || nuovo >= focusables.Count) { e.Handled = true; return; }
        focusables[nuovo].Focus();
        e.Handled = true;
    }

    /// <summary>
    /// Inizializza la lista materie e l'indice di categorie a partire dai dati caricati.
    /// Chiamato dalla <see cref="MainWindow"/> al boot.
    /// </summary>
    public void Inizializza(List<Materia> materie, List<Domanda> tutteDomande)
    {
        _tutteDomande = tutteDomande;

        // 1. Popolo materie con conteggio domande
        Materie.Clear();
        foreach (var m in materie)
        {
            int n = tutteDomande.Count(d => d.MateriaId == m.Id);
            var ms = new MateriaSelezionabile(m, n);
            ms.PropertyChanged += OnMateriaPropertyChanged;
            Materie.Add(ms);
        }

        // 2. Per ogni materia, calcolo le sue categorie (con conteggio per-materia)
        _categoriePerMateria.Clear();
        foreach (var m in materie)
        {
            var domandeM = tutteDomande.Where(d => d.MateriaId == m.Id).ToList();
            var cats = domandeM
                .GroupBy(d => d.Categoria)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CategoriaSelezionabile(m.Id, m.Nome, g.Key, g.Count()))
                .ToList();
            foreach (var c in cats)
                c.PropertyChanged += OnCategoriaPropertyChanged;
            _categoriePerMateria[m.Id] = cats;
        }

        AggiornaCategorieVisibili();
        AggiornaRiepilogo();
    }

    // ------------------------------------------------------------------ EVENTI INTERNI

    private void OnMateriaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MateriaSelezionabile.IsSelected)) return;
        AggiornaCategorieVisibili();
        AggiornaRiepilogo();
    }

    private void OnCategoriaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategoriaSelezionabile.IsSelected)) return;
        AggiornaRiepilogo();
    }

    private void AggiornaCategorieVisibili()
    {
        CategorieVisibili.Clear();
        foreach (var m in Materie.Where(x => x.IsSelected))
        {
            if (_categoriePerMateria.TryGetValue(m.Id, out var cats))
                foreach (var c in cats) CategorieVisibili.Add(c);
        }
        Raise(nameof(NessunaCategoriaDisponibile));
    }

    private void OnPulisciCategorieClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var c in CategorieVisibili) c.IsSelected = false;
    }

    // ------------------------------------------------------------------ RIEPILOGO + AVVIO

    /// <summary>
    /// Calcola il pool di domande corrente in base alla selezione di materie/categorie.
    /// Se ci sono categorie selezionate, filtra per coppia (MateriaId, Categoria) cosi'
    /// nomi categoria identici su materie diverse non si confondono.
    /// </summary>
    private List<Domanda> CalcolaPoolCorrente()
    {
        var materieSel = Materie.Where(m => m.IsSelected).Select(m => m.Id).ToHashSet();
        if (materieSel.Count == 0) return new List<Domanda>();

        var pool = _tutteDomande.Where(d => materieSel.Contains(d.MateriaId)).ToList();

        var catSel = CategorieVisibili.Where(c => c.IsSelected)
            .Select(c => (c.MateriaId, c.Categoria))
            .ToHashSet();
        if (catSel.Count > 0)
            pool = pool.Where(d => catSel.Contains((d.MateriaId, d.Categoria))).ToList();

        return pool;
    }

    private void AggiornaRiepilogo()
    {
        int nMaterie = Materie.Count(m => m.IsSelected);
        int nCat     = CategorieVisibili.Count(c => c.IsSelected);
        var pool = CalcolaPoolCorrente();
        int disponibili = pool.Count;

        // Quante davvero giocheremo (limite N)
        int giocate = disponibili;
        if (LimitaDomande && (int)LimiteN > 0 && giocate > (int)LimiteN)
            giocate = (int)LimiteN;

        string testoMaterie = nMaterie switch
        {
            0 => "Nessuna materia selezionata",
            1 => $"1 materia: {Materie.First(m => m.IsSelected).Nome}",
            _ => $"{nMaterie} materie selezionate"
        };
        string testoCat = nCat == 0 ? "tutte le categorie" : $"{nCat} categorie";
        string testoDom = giocate == disponibili
            ? $"{disponibili} domande"
            : $"{giocate} domande (limitate da {disponibili})";

        RiepilogoSelezione = nMaterie == 0
            ? testoMaterie
            : $"{testoMaterie}  ·  {testoCat}  ·  {testoDom}";

        if (nMaterie == 0)            AvvisoSelezione = "Seleziona almeno una materia per avviare un quiz.";
        else if (disponibili == 0)    AvvisoSelezione = "Nessuna domanda corrisponde ai filtri scelti.";
        else                          AvvisoSelezione = string.Empty;

        Raise(nameof(HaAvviso));
        PuoAvviare = (nMaterie > 0 && disponibili > 0);
        HaCategorieSelezionate = (nCat > 0);
    }

    private void OnAvviaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var pool = CalcolaPoolCorrente();
        if (pool.Count == 0) return;

        var materieSel = Materie.Where(m => m.IsSelected).ToList();
        var catSel     = CategorieVisibili.Where(c => c.IsSelected).ToList();

        bool quizzone = materieSel.Count > 1;
        string materiaLabel = quizzone ? "Quizzone" : materieSel[0].Nome;
        string nomeMod;
        if (catSel.Count > 0)
            nomeMod = $"{materiaLabel} — Per categoria";
        else if (LimitaDomande && (int)LimiteN > 0)
            nomeMod = $"{materiaLabel} — {(int)LimiteN} domande";
        else
            nomeMod = quizzone ? "Quizzone — Completo randomizzato" : $"{materiaLabel} — Completo";

        var opz = new OpzioniQuiz
        {
            NomeModalita            = nomeMod,
            MateriaNomeLabel        = materiaLabel,
            RandomizzaOrdineDomande = RandomizzaOrdine,
            Rotazione               = Rotazione,
            Cronometro              = Cronometro,
            LimiteDomande           = LimitaDomande ? (int)LimiteN : 0,
            Categorie               = catSel.Select(c => c.ChiaveCombinata).ToList(),
            Materie                 = materieSel.Select(m => m.Id).ToList()
        };

        var sessione = new SessioneQuiz(pool, opz);
        QuizAvviato?.Invoke(this, sessione);
    }

    // ------------------------------------------------------------------ INPC helper

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
