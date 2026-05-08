using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Tab "Sospesi": elenco delle sessioni messe in pausa.
/// Per ora le pause sono create solo dal console (la pausa GUI arriva nello step 4),
/// ma essendo lo storage condiviso le vediamo qui.
/// Il bottone Riprendi e' disabilitato finche' non c'e' la factory dello step 4;
/// l'azione Elimina e' attiva con conferma inline.
/// </summary>
public partial class SospesiView : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private StorageService? _storage;

    public ObservableCollection<SessioneSospesaItem> Sessioni { get; } = new();

    // ------------------------------------------------------------------ STATO

    private bool _nessunaPausa = true;
    public bool NessunaPausa
    {
        get => _nessunaPausa;
        private set { if (_nessunaPausa != value) { _nessunaPausa = value; Raise(); } }
    }

    private string _sottotitolo = "Caricamento...";
    public string Sottotitolo
    {
        get => _sottotitolo;
        private set { if (_sottotitolo != value) { _sottotitolo = value; Raise(); } }
    }

    // ------------------------------------------------------------------ COSTRUZIONE

    public SospesiView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void Inizializza(StorageService storage)
    {
        _storage = storage;
        Ricarica();
    }

    // ------------------------------------------------------------------ CARICAMENTO

    public void Ricarica()
    {
        if (_storage == null) return;

        Sessioni.Clear();
        try
        {
            var pause = _storage.CaricaPause();
            // Piu' recente prima
            foreach (var p in pause.OrderByDescending(x => x.DataOraPausa))
                Sessioni.Add(new SessioneSospesaItem(p));
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nel caricamento: {ex.Message}";
            NessunaPausa = true;
            return;
        }

        NessunaPausa = Sessioni.Count == 0;
        Sottotitolo = NessunaPausa
            ? "Nessuna sessione in pausa."
            : $"{Sessioni.Count} sessioni in pausa.";
    }

    // ------------------------------------------------------------------ HANDLER

    private void OnAggiornaClick(object? sender, RoutedEventArgs e)
    {
        Ricarica();
    }

    /// <summary>Primo click su "Elimina": entra in stato di conferma per quella riga.</summary>
    private void OnEliminaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        // azzera eventuali altre conferme aperte (al massimo una alla volta)
        foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
        item.InAttesaConfermaEliminazione = true;
    }

    /// <summary>Conferma definitiva: elimina dal disco e dalla lista.</summary>
    private void OnConfermaEliminaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        if (_storage == null) return;

        try
        {
            _storage.EliminaPausa(item.SessioneId);
        }
        catch (Exception ex)
        {
            // Non blocco l'app: lascio la riga in lista e segnalo nel sottotitolo.
            Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
            return;
        }

        Sessioni.Remove(item);
        NessunaPausa = Sessioni.Count == 0;
        Sottotitolo = NessunaPausa
            ? "Nessuna sessione in pausa."
            : $"{Sessioni.Count} sessioni in pausa.";
    }

    /// <summary>Annulla la conferma e torna al layout normale della riga.</summary>
    private void OnAnnullaEliminaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        item.InAttesaConfermaEliminazione = false;
    }

    // ------------------------------------------------------------------ INPC

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
