using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Tab "Sospesi": elenco delle sessioni messe in pausa.
/// Lo storage e' condiviso fra console e GUI, quindi vediamo qui anche le
/// pause create dal console.
/// Riprendi (step 4) solleva <see cref="RiprendiRichiesto"/> con la pausa
/// originale; la <c>MainWindow</c> costruisce la <c>SessioneQuiz</c> e naviga
/// al <c>QuizView</c>. Elimina ha conferma inline.
/// </summary>
public partial class SospesiView : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>L'utente ha cliccato "Riprendi" su una pausa: la <c>MainWindow</c>
    /// costruisce la sessione e cambia view.</summary>
    public event EventHandler<SessionePausa>? RiprendiRichiesto;

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

    /// <summary>Chiede alla <c>MainWindow</c> di riprendere questa sessione.</summary>
    private void OnRiprendiClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        RiprendiRichiesto?.Invoke(this, item.Pausa);
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
