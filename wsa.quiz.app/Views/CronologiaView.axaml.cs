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

    private void ApriDettaglio(RisultatoCronologiaItem item)
    {
        var dettaglio = new CronologiaDettaglioView(item.Risultato);
        dettaglio.IndietroRichiesto += OnIndietroDalDettaglio;
        DettaglioArea.Content = dettaglio;
        ModoDettaglio = true;
    }

    private void OnIndietroDalDettaglio(object? sender, EventArgs e)
    {
        DettaglioArea.Content = null;
        ModoDettaglio = false;
    }

    // ------------------------------------------------------------------ INPC

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
