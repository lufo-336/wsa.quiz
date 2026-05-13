using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Tab "Cronologia": tabella delle sessioni passate in ordine cronologico inverso
/// (piu' recente in alto), con swap a una vista di dettaglio domanda-per-domanda
/// al doppio click di una riga. La cronologia viene ricaricata da disco a ogni
/// chiamata di <see cref="Ricarica"/> (al boot, al click di "Aggiorna" e quando
/// la <c>MainWindow</c> ne fa richiesta dopo la conclusione di un quiz).
/// </summary>
public partial class CronologiaView : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private StorageService? _storage;

    public ObservableCollection<RisultatoCronologiaItem> Sessioni { get; } = new();

    // ------------------------------------------------------------------ STATO

    private bool _nessunaSessione = true;
    public bool NessunaSessione
    {
        get => _nessunaSessione;
        private set { if (_nessunaSessione != value) { _nessunaSessione = value; Raise(); } }
    }

    private string _sottotitolo = "Caricamento...";
    public string Sottotitolo
    {
        get => _sottotitolo;
        private set { if (_sottotitolo != value) { _sottotitolo = value; Raise(); } }
    }

    private bool _modoDettaglio;
    /// <summary>
    /// True quando si sta visualizzando il dettaglio di una sessione (sub-view
    /// <see cref="CronologiaDettaglioView"/>); false quando si vede la lista.
    /// </summary>
    public bool ModoDettaglio
    {
        get => _modoDettaglio;
        private set { if (_modoDettaglio != value) { _modoDettaglio = value; Raise(); } }
    }

    // ------------------------------------------------------------------ COSTRUZIONE

    public CronologiaView()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Inietta lo storage. Chiamato dalla <see cref="MainWindow"/> al boot,
    /// dopo che lo storage e' stato istanziato.
    /// </summary>
    public void Inizializza(StorageService storage)
    {
        _storage = storage;
        Ricarica();
    }

    // ------------------------------------------------------------------ CARICAMENTO

    /// <summary>
    /// Rilegge la cronologia dal disco. Idempotente; sicuro da chiamare ripetutamente.
    /// </summary>
    public void Ricarica()
    {
        if (_storage == null) return;

        // Se siamo nel dettaglio quando arriva un Ricarica, non lo chiudiamo
        // di nascosto: la lista sotto si aggiorna lo stesso e quando l'utente
        // tornera' indietro vedra' il nuovo stato.
        Sessioni.Clear();
        try
        {
            var lista = _storage.CaricaCronologia();
            // Piu' recente prima
            foreach (var r in lista.OrderByDescending(x => x.DataOra))
                Sessioni.Add(new RisultatoCronologiaItem(r));
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nel caricamento: {ex.Message}";
            NessunaSessione = true;
            return;
        }

        NessunaSessione = Sessioni.Count == 0;
        Sottotitolo = NessunaSessione
            ? "Nessuna sessione registrata."
            : $"{Sessioni.Count} sessioni registrate.";
    }

    // ------------------------------------------------------------------ HANDLER

    private void OnAggiornaClick(object? sender, RoutedEventArgs e)
    {
        Ricarica();
    }

    private void OnSessioneDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedItem is not RisultatoCronologiaItem item) return;
        ApriDettaglio(item);
    }

    /// <summary>Primo click su "Elimina" sulla riga: entra in stato di conferma per quella riga.</summary>
    private void OnEliminaRigaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not RisultatoCronologiaItem item) return;
        // azzera eventuali altre conferme aperte (al massimo una alla volta, come SospesiView)
        foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
        item.InAttesaConfermaEliminazione = true;
    }

    /// <summary>Conferma definitiva: elimina dal disco e dalla lista.</summary>
    private void OnConfermaEliminaRigaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not RisultatoCronologiaItem item) return;
        EseguiEliminazione(item);
    }

    /// <summary>Annulla la conferma e torna al layout normale della riga.</summary>
    private void OnAnnullaEliminaRigaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not RisultatoCronologiaItem item) return;
        item.InAttesaConfermaEliminazione = false;
    }

    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Invio apre il dettaglio della riga selezionata; Canc avvia la
    /// conferma inline (e secondo Canc la conferma definitiva); Esc annulla la
    /// conferma se attiva. Attivo solo quando NON siamo nel dettaglio.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ModoDettaglio) { base.OnKeyDown(e); return; }

        if (ListaSessioni.SelectedItem is not RisultatoCronologiaItem item)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                ApriDettaglio(item);
                return;

            case Key.Delete:
                e.Handled = true;
                if (item.InAttesaConfermaEliminazione)
                {
                    // Secondo Canc: conferma definitiva.
                    EseguiEliminazione(item);
                }
                else
                {
                    // Primo Canc: avvia conferma inline (replica OnEliminaRigaClick).
                    foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
                    item.InAttesaConfermaEliminazione = true;
                }
                return;

            case Key.Escape:
                if (item.InAttesaConfermaEliminazione)
                {
                    e.Handled = true;
                    item.InAttesaConfermaEliminazione = false;
                }
                return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>Estratto da OnConfermaEliminaRigaClick: serve a poter eliminare anche da tastiera (Canc).</summary>
    private void EseguiEliminazione(RisultatoCronologiaItem item)
    {
        if (_storage == null) return;
        try
        {
            _storage.EliminaRisultato(item.Id);
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
            return;
        }

        Sessioni.Remove(item);
        NessunaSessione = Sessioni.Count == 0;
        Sottotitolo = NessunaSessione
            ? "Nessuna sessione registrata."
            : $"{Sessioni.Count} sessioni registrate.";
    }

    private void ApriDettaglio(RisultatoCronologiaItem item)
    {
        var dettaglio = new CronologiaDettaglioView(item.Risultato);
        dettaglio.IndietroRichiesto += OnIndietroDalDettaglio;
        dettaglio.EliminazioneRichiesta += OnEliminaDalDettaglio;
        DettaglioArea.Content = dettaglio;
        ModoDettaglio = true;
    }

    private void OnIndietroDalDettaglio(object? sender, EventArgs e)
    {
        DettaglioArea.Content = null;
        ModoDettaglio = false;
    }

    private void OnEliminaDalDettaglio(object? sender, string id)
    {
        if (_storage == null) return;
        try
        {
            _storage.EliminaRisultato(id);
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
            return;
        }

        // chiudi il dettaglio e torna alla lista, ricaricata da disco
        DettaglioArea.Content = null;
        ModoDettaglio = false;
        Ricarica();
    }

    private void OnSvuotaCronologiaClick(object? sender, RoutedEventArgs e)
    {
        ChiediConfermaSvuotaCronologia();
    }

    /// <summary>
    /// Dialog modale di conferma per "Svuota cronologia". Window 420x180,
    /// CenterOwner, no resize, no taskbar. ESC chiude come Annulla.
    /// </summary>
    private async void ChiediConfermaSvuotaCronologia()
    {
        if (_storage == null) return;

        int n = Sessioni.Count;
        var w = new Window
        {
            Title = "Svuota cronologia",
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        bool conferma = false;
        var contenuto = new StackPanel
        {
            Margin = new global::Avalonia.Thickness(20),
            Spacing = 14
        };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Svuota tutta la cronologia?",
            FontSize = 14,
            FontWeight = global::Avalonia.Media.FontWeight.SemiBold
        });
        contenuto.Children.Add(new TextBlock
        {
            Text = $"Verranno eliminate {n} partite dalla cronologia. L'azione non e' reversibile.",
            FontSize = 12,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Foreground = global::Avalonia.Media.Brushes.DimGray
        });

        var pulsantiera = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        var annullaBtn = new Button
        {
            Content = "Annulla",
            Padding = new global::Avalonia.Thickness(14, 5)
        };
        var siBtn = new Button
        {
            Content = "Si', cancella tutto",
            Padding = new global::Avalonia.Thickness(14, 5)
        };
        siBtn.Classes.Add("danger");
        annullaBtn.Click += (_, _) => w.Close();
        siBtn.Click += (_, _) => { conferma = true; w.Close(); };

        // ESC = Annulla (stesso pattern del menu pausa)
        w.KeyDown += (_, ev) =>
        {
            if (ev.Key == Key.Escape)
            {
                ev.Handled = true;
                w.Close();
            }
        };

        pulsantiera.Children.Add(annullaBtn);
        pulsantiera.Children.Add(siBtn);
        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        if (!conferma) return;

        try
        {
            _storage.SvuotaCronologia();
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nello svuotamento: {ex.Message}";
            return;
        }

        Ricarica();
    }

    // ------------------------------------------------------------------ INPC

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
