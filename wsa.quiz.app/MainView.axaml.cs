using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Wsa.Quiz.App.State;
using Wsa.Quiz.App.Views;
using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App;

/// <summary>
/// Shell principale dell'app. Orchestra il caricamento dati al boot,
/// il TabControl con le tre sezioni (Home / Cronologia / Sospesi)
/// e la transizione fra Home / Quiz / Riepilogo all'interno del tab Home.
/// </summary>
public partial class MainView : UserControl
{
    private StorageService? _storage;
    private List<Materia> _materie = new();
    private List<Domanda> _tutteDomande = new();
    private Dictionary<string, Domanda> _mappaDomande = new();

    private HomeView? _homeView;
    private CronologiaView? _cronologiaView;
    private SospesiView? _sospesiView;
    private StatisticheView? _statisticheView;

    // Aree contenuto dell'host attivo: su desktop puntano ai ContentControl dentro
    // il TabControl, su touch a quelli dentro il Carousel. Impostate in
    // ConfiguraNavigazione; tutta la logica di swap passa di qui (vedi _homeArea.Content).
    private ContentControl _homeArea = null!;
    private ContentControl _cronologiaArea = null!;
    private ContentControl _sospesiArea = null!;
    private ContentControl _statisticheArea = null!;

    // Brush icone bottom nav (esplicite: il system accent su questo device rende
    // scuro — come l'header — quindi userei un colore poco visibile sulla barra).
    private static readonly IBrush NavAttiva = new SolidColorBrush(Color.Parse("#5AA0F0"));
    private static readonly IBrush NavInattiva = Brushes.White;

    // Animazione dello scroll orizzontale per il tap sulla bottom nav (lo swipe col
    // dito usa l'inerzia nativa, qui serve solo per il cambio tab da bottone).
    private DispatcherTimer? _scrollAnim;

    public MainView()
    {
        InitializeComponent();
        ConfiguraNavigazione();
        Caricamento();
    }

    // ------------------------------------------------------------------ NAVIGAZIONE (touch vs desktop)

    /// <summary>Larghezza di una pagina = viewport dello SwipeHost. Le pagine sono
    /// affiancate, quindi lo scroll-offset orizzontale è (indice × larghezza).</summary>
    private double LarghezzaPagina => SwipeHost.Bounds.Width;

    /// <summary>Indice della sezione attiva, indipendente dall'host: TabControl su
    /// desktop, posizione dello scroll orizzontale su touch.</summary>
    private int IndiceSezione
    {
        get
        {
            if (!AppEnv.TouchMode) return Tabs.SelectedIndex;
            double w = LarghezzaPagina;
            if (w <= 0) return 0;
            return Math.Clamp((int)Math.Round(SwipeHost.Offset.X / w), 0, 3);
        }
        set
        {
            if (AppEnv.TouchMode) ScorriAllaPagina(value, animato: true);
            else Tabs.SelectedIndex = value;
        }
    }

    /// <summary>
    /// Desktop: tab in alto + footer diagnostico (TabControl, invariato). Touch:
    /// SwipeHost (ScrollViewer orizzontale con snap) pilotato da swipe nativo + bottom
    /// navigation a icone; il TabControl viene nascosto. Le 4 aree contenuto vengono
    /// instradate ai ContentControl dell'host attivo via i campi _*Area.
    /// </summary>
    private void ConfiguraNavigazione()
    {
        if (AppEnv.TouchMode)
        {
            _homeArea = HomeAreaTouch;
            _cronologiaArea = CronologiaAreaTouch;
            _sospesiArea = SospesiAreaTouch;
            _statisticheArea = StatisticheAreaTouch;

            Tabs.IsVisible = false;
            SwipeHost.IsVisible = true;
            FooterBar.IsVisible = false;    // footer diagnostico solo su desktop
            BottomNav.IsVisible = true;

            // Ogni pagina larga/alta quanto il viewport: lo snap aggancia ai bordi pagina
            // e il contenuto verticale resta dentro il proprio ScrollViewer.
            SwipeHost.SizeChanged += (_, _) => DimensionaPagine();
            // Lo scroll (swipe o animazione) aggiorna l'icona attiva nella bottom nav.
            SwipeHost.AddHandler(ScrollViewer.ScrollChangedEvent,
                new EventHandler<ScrollChangedEventArgs>((_, _) => AggiornaNav()));

            AggiornaNav();
        }
        else
        {
            _homeArea = HomeArea;
            _cronologiaArea = CronologiaArea;
            _sospesiArea = SospesiArea;
            _statisticheArea = StatisticheArea;

            // Evidenzia (no-op su desktop) tenuta per simmetria; AggiornaNav esce subito.
            Tabs.SelectionChanged += (_, _) => AggiornaNav();
        }
    }

    /// <summary>Allinea larghezza e altezza delle 4 pagine al viewport dello SwipeHost,
    /// così lo snap orizzontale cade esattamente su un confine di pagina.</summary>
    private void DimensionaPagine()
    {
        double w = SwipeHost.Bounds.Width;
        double h = SwipeHost.Bounds.Height;
        if (w <= 0 || h <= 0) return;
        foreach (var pagina in new[] { HomeAreaTouch, CronologiaAreaTouch, SospesiAreaTouch, StatisticheAreaTouch })
        {
            pagina.Width = w;
            pagina.Height = h;
        }
    }

    /// <summary>Porta lo SwipeHost sulla pagina <paramref name="indice"/>. Con
    /// <paramref name="animato"/> anima l'offset (tap su bottom nav); lo swipe col dito
    /// usa invece l'inerzia nativa e non passa di qui.</summary>
    private void ScorriAllaPagina(int indice, bool animato)
    {
        double w = LarghezzaPagina;
        if (w <= 0) return;
        double target = Math.Clamp(indice, 0, 3) * w;
        _scrollAnim?.Stop();

        if (!animato)
        {
            SwipeHost.Offset = new Vector(target, 0);
            return;
        }

        double start = SwipeHost.Offset.X;
        if (Math.Abs(target - start) < 0.5) return;
        var orologio = System.Diagnostics.Stopwatch.StartNew();
        const double durataMs = 220;
        _scrollAnim = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        _scrollAnim.Tick += (_, _) =>
        {
            double t = Math.Min(1.0, orologio.Elapsed.TotalMilliseconds / durataMs);
            double e = 1 - Math.Pow(1 - t, 3); // easeOutCubic
            SwipeHost.Offset = new Vector(start + (target - start) * e, 0);
            if (t >= 1.0) _scrollAnim!.Stop();
        };
        _scrollAnim.Start();
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tag } && int.TryParse(tag, out int indice))
            IndiceSezione = indice;
    }

    /// <summary>Evidenzia l'icona della bottom nav corrispondente alla sezione attiva.</summary>
    private void AggiornaNav()
    {
        if (!AppEnv.TouchMode) return;
        var icone = new[] { NavHomeIcon, NavCronologiaIcon, NavSospesiIcon, NavStatisticheIcon };
        for (int i = 0; i < icone.Length; i++)
        {
            bool attiva = i == IndiceSezione;
            icone[i].Stroke = attiva ? NavAttiva : NavInattiva;
            icone[i].Opacity = attiva ? 1.0 : 0.45;
        }
    }

    // ------------------------------------------------------------------ SAFE AREA (edge-to-edge Android)

    /// <summary>
    /// Su Android con targetSdk 35+ l'edge-to-edge è forzato: l'app disegna dietro
    /// status bar e barra di navigazione di sistema, che altrimenti coprono header
    /// e bottom navigation. Le insets reali arrivano da <see cref="AppEnv.SystemBarInsetsPx"/>,
    /// popolate dalla MainActivity Android via <c>WindowInsets.systemBars()</c> (fonte
    /// autorevole: l'InsetsManager di Avalonia qui riporta bottom=0). Le applico come
    /// padding: header accent e bottom nav si estendono DIETRO le barre di sistema, ma
    /// il loro contenuto resta nell'area sicura. Solo su touch (desktop: return anticipato).
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!AppEnv.TouchMode) return;

        AppEnv.SystemBarInsetsChanged += OnSystemBarInsetsChanged;
        ApplicaSafeArea();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        AppEnv.SystemBarInsetsChanged -= OnSystemBarInsetsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    // Le insets Android arrivano su thread UI ma le rimando comunque sul dispatcher
    // per disaccoppiarle dal dispatch di sistema.
    private void OnSystemBarInsetsChanged() => Dispatcher.UIThread.Post(ApplicaSafeArea);

    private void ApplicaSafeArea()
    {
        if (!AppEnv.TouchMode) return;
        var s = AppEnv.SystemBarInsetsPx;
        // Prima del primo dispatch di WindowInsets le insets sono tutte-zero: non applico
        // nulla (eviterei comunque divisioni inutili) e attendo l'evento reale.
        if (s.Top == 0 && s.Bottom == 0 && s.Left == 0 && s.Right == 0) return;
        // s è in PIXEL FISICI. Le Thickness sono in unità di layout della MainView, che
        // vive dentro un LayoutTransformControl scalato ScalaTouch su un TopLevel con
        // RenderScaling: 1 unità locale = ScalaTouch × RenderScaling pixel fisici. Per
        // riservare S px servono S/(ScalaTouch×RenderScaling) unità. RenderScaling è letto
        // a runtime (su questo device vale 1, non la densità).
        double f = AppEnv.ScalaTouch * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
        HeaderBorder.Padding = new Thickness(20, 12 + s.Top / f, 20, 12);
        BottomNav.Padding = new Thickness(0, 0, 0, s.Bottom / f);
        RootDock.Margin = new Thickness(s.Left / f, 0, s.Right / f, 0);
    }

    // ------------------------------------------------------------------ TASTIERA GLOBALE

    /// <summary>
    /// Step 8: Ctrl+Tab / Ctrl+Shift+Tab cambiano tab principale in modo ciclico.
    /// Funziona ovunque nell'app, anche dentro al quiz (il quiz resta vivo sotto
    /// e si ritrova quando si torna sulla tab Home).
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            const int n = 4;
            int delta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : +1;
            IndiceSezione = (IndiceSezione + delta + n) % n;
            // Sposto il focus nella view appena attivata cosi' le scorciatoie locali
            // partono immediate senza dover cliccare.
            switch (IndiceSezione)
            {
                case 0: (_homeArea.Content as Control)?.Focus(); break;
                case 1: (_cronologiaArea.Content as Control)?.Focus(); break;
                case 2: (_sospesiArea.Content as Control)?.Focus(); break;
                case 3: (_statisticheArea.Content as Control)?.Focus(); break;
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    // ------------------------------------------------------------------ BOOT

    private void Caricamento()
    {
        string cartellaUtente = StorageService.CartellaUtenteDefault();

        // Scelta della sorgente dati read-only:
        // - desktop/CLI: i JSON sono copiati nel bin → file system (identico a prima).
        // - Android: nessun file accanto all'eseguibile → risorse embedded (avares://).
        string cartellaDati = AppContext.BaseDirectory;
        IFonteDati fonte;
        string origine;
        if (File.Exists(Path.Combine(cartellaDati, "materie.json")))
        {
            fonte = new FileSystemFonteDati(cartellaDati);
            origine = $"file system: {cartellaDati}";
        }
        else
        {
            fonte = new AvaloniaResourceFonteDati();
            origine = "risorse embedded (avares://)";
        }
        FooterText.Text = $"Dati read-only: {origine}    ·    Cartella utente: {cartellaUtente}";

        try
        {
            _storage = new StorageService(fonte, cartellaUtente);
            _materie = _storage.CaricaMaterie();
            _tutteDomande = _storage.CaricaTutteLeDomande(_materie);
            _mappaDomande = _tutteDomande.ToDictionary(d => d.Id, d => d);

            StatoHeaderText.Text = $"{_materie.Count} materie · {_tutteDomande.Count} domande";

            // ---------- Tab Home ----------
            _homeView = new HomeView();
            _homeView.Inizializza(_materie, _tutteDomande);
            _homeView.QuizAvviato += OnQuizAvviato;
            _homeArea.Content = _homeView;

            // ---------- Tab Cronologia ----------
            _cronologiaView = new CronologiaView();
            _cronologiaView.Inizializza(_storage);
            _cronologiaArea.Content = _cronologiaView;

            // ---------- Tab Sospesi ----------
            _sospesiView = new SospesiView();
            _sospesiView.Inizializza(_storage);
            _sospesiView.RiprendiRichiesto += OnRiprendiSospesa;
            _sospesiArea.Content = _sospesiView;

            // ---------- Tab Statistiche ----------
            _statisticheView = new StatisticheView();
            _statisticheView.Inizializza(_storage);
            _statisticheArea.Content = _statisticheView;
        }
        catch (Exception ex)
        {
            ErrorePanel.IsVisible = true;
            ErroreText.Text = ex.Message;
            _homeArea.Content = new PlaceholderView(
                "Impossibile avviare",
                "Verifica la presenza di materie.json e della cartella domande/ accanto all'eseguibile.");
        }
    }

    // ------------------------------------------------------------------ NAVIGAZIONE TAB HOME

    private void OnQuizAvviato(object? sender, SessioneQuiz sessione)
    {
        AvviaQuizView(sessione);
    }

    private void AvviaQuizView(SessioneQuiz sessione)
    {
        if (_storage == null) return;
        var quizView = new QuizView(sessione, _storage);
        quizView.QuizConcluso += OnQuizConcluso;
        quizView.QuizMessoInPausa += OnQuizMessoInPausa;
        _homeArea.Content = quizView;
        IndiceSezione = 0; // resta sulla sezione Home, ma sostituisce il contenuto
    }

    private void OnQuizConcluso(object? sender, SessioneQuiz sessione)
    {
        // Salvataggio cronologia (solo se c'e' almeno una risposta data)
        if (_storage != null && sessione.Risultato.TotaleDomande > 0)
        {
            try
            {
                _storage.SalvaRisultato(sessione.Risultato);
                // La nuova sessione dev'essere visibile la prossima volta che si apre il tab.
                _cronologiaView?.Ricarica();
                _statisticheView?.Ricarica();
            }
            catch (Exception ex)
            {
                ErrorePanel.IsVisible = true;
                ErroreText.Text = $"Impossibile salvare la cronologia: {ex.Message}";
            }
        }

        // Se la sessione era stata ripresa da una pausa, rimuovo la pausa originale.
        // Stesso comportamento del console (vedi Program.EseguiSessione*: EliminaPausa(stato.SessioneId)).
        if (_storage != null && sessione.IdSessionePausa is { } idPausa)
        {
            try
            {
                _storage.EliminaPausa(idPausa);
                _sospesiView?.Ricarica();
            }
            catch (Exception ex)
            {
                ErrorePanel.IsVisible = true;
                ErroreText.Text = $"Impossibile eliminare la pausa originale: {ex.Message}";
            }
        }

        var riep = new RiepilogoView(sessione.Risultato);
        riep.TornaAllaHome += (_, _) => RitornaAllaHome();
        _homeArea.Content = riep;
    }

    private void OnQuizMessoInPausa(object? sender, EventArgs e)
    {
        // La pausa e' gia' stata salvata dal QuizView. Torno alla Home e ricarico i sospesi.
        RitornaAllaHome();
        _sospesiView?.Ricarica();
    }

    private void OnRiprendiSospesa(object? sender, SessionePausa pausa)
    {
        if (_storage == null) return;
        try
        {
            var sessione = SessioneQuiz.RiprendiDa(pausa, _mappaDomande);
            ErrorePanel.IsVisible = false;
            AvviaQuizView(sessione);
        }
        catch (InvalidOperationException ex)
        {
            ErrorePanel.IsVisible = true;
            ErroreText.Text = $"{ex.Message} La pausa e' stata rimossa.";
            try { _storage.EliminaPausa(pausa.SessioneId); }
            catch { /* best effort: la card rimane finche' non si preme Aggiorna */ }
            _sospesiView?.Ricarica();
        }
    }

    private void RitornaAllaHome()
    {
        ErrorePanel.IsVisible = false;
        if (_homeView == null)
        {
            _homeView = new HomeView();
            _homeView.Inizializza(_materie, _tutteDomande);
            _homeView.QuizAvviato += OnQuizAvviato;
        }
        _homeArea.Content = _homeView;
    }
}
