using System;
using System.Linq;
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
/// chiamata di <see cref="TabellaViewBase{TItem}.Ricarica"/> (al boot, al click di
/// "Aggiorna" e quando la <c>MainWindow</c> ne fa richiesta dopo la conclusione
/// di un quiz).
/// </summary>
public partial class CronologiaView : TabellaViewBase<RisultatoCronologiaItem>
{
    // ------------------------------------------------------------------ STATO

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
        Focusable = true;
        // Step 15: Tunnel + handledEventsToo per intercettare anche eventi
        // gia' marcati Handled da ListBoxItem o ScrollViewer.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        // Se il focus arriva sullo UserControl stesso (es. Ctrl+Tab dal
        // MainWindow), lo trasferiamo alla lista e selezioniamo il primo item.
        GotFocus += (_, e) =>
        {
            if (e.Source == this && !ModoDettaglio)
            {
                ListaSessioni.Focus();
                if (ListaSessioni.SelectedItem == null && ListaSessioni.ItemCount > 0)
                    ListaSessioni.SelectedIndex = 0;
            }
        };
    }

    // ------------------------------------------------------------------ CARICAMENTO

    protected override void RicaricaCore()
    {
        var lista = _storage!.CaricaCronologia();
        foreach (var r in lista.OrderByDescending(x => x.DataOra))
            Sessioni.Add(new RisultatoCronologiaItem(r));
    }

    protected override string FormatSottotitolo(int count) =>
        count == 0 ? "Nessuna sessione registrata." : $"{count} sessioni registrate.";

    protected override void EliminaDaStorage(RisultatoCronologiaItem item)
        => _storage!.EliminaRisultato(item.Id);

    // ------------------------------------------------------------------ HANDLER

    private void OnSessioneDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedItem is not RisultatoCronologiaItem item) return;
        ApriDettaglio(item);
    }

    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Invio apre il dettaglio della riga selezionata; Canc avvia la
    /// conferma inline (e secondo Canc la conferma definitiva); Esc annulla la
    /// conferma se attiva. Attivo solo quando NON siamo nel dettaglio.
    /// </summary>
    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (ModoDettaglio) return;
        if (ListaSessioni.SelectedItem is not RisultatoCronologiaItem item) return;

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
                    EseguiEliminazione(item);
                }
                else
                {
                    AzzeraConferme();
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
    }

    // ------------------------------------------------------------------ DETTAGLIO

    private void ApriDettaglio(RisultatoCronologiaItem item)
    {
        var dettaglio = new CronologiaDettaglioView(item.Risultato);
        dettaglio.IndietroRichiesto += OnIndietroDalDettaglio;
        dettaglio.EliminazioneRichiesta += OnEliminaDalDettaglio;
        DettaglioArea.Content = dettaglio;
        ModoDettaglio = true;
        // Defer focus al prossimo frame: il binding IsVisible deve aggiornarsi
        // prima che il dettaglio possa ricevere focus (Focus() fallisce se
        // l'elemento non e' ancora IsVisible).
        Avalonia.Threading.Dispatcher.UIThread.Post(() => dettaglio.Focus());
    }

    private void OnIndietroDalDettaglio(object? sender, EventArgs e)
    {
        DettaglioArea.Content = null;
        ModoDettaglio = false;
        // Senza questo, il focus resta sul bottone "Indietro" del dettaglio che
        // pero' e' appena stato rimosso dal visual tree: di conseguenza Tab e
        // frecce smettono di funzionare finche' non si clicca con il mouse.
        ListaSessioni.Focus();
    }

    private void OnEliminaDalDettaglio(object? sender, string id)
    {
        try
        {
            _storage!.EliminaRisultato(id);
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

    // ------------------------------------------------------------------ SVUOTA

    private async void OnSvuotaCronologiaClick(object? sender, RoutedEventArgs e)
    {
        try { await ChiediConfermaSvuotaCronologiaAsync(); }
        catch (Exception) { /* best effort */ }
    }

    /// <summary>
    /// Dialog modale di conferma per "Svuota cronologia".
    /// </summary>
    private async Task ChiediConfermaSvuotaCronologiaAsync()
    {
        if (_storage == null) return;

        int n = Sessioni.Count;
        var dialog = new ConfermaDialog(
            "Svuota cronologia",
            "Svuota tutta la cronologia?",
            $"Verranno eliminate {n} partite dalla cronologia. L'azione non e' reversibile.",
            "Si', cancella tutto",
            "danger");

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (!dialog.Confermato) return;

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
}
