using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
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
public partial class SospesiView : TabellaViewBase<SessioneSospesaItem>
{
    /// <summary>L'utente ha cliccato "Riprendi" su una pausa: la <c>MainWindow</c>
    /// costruisce la sessione e cambia view.</summary>
    public event EventHandler<SessionePausa>? RiprendiRichiesto;

    public SospesiView()
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
            if (e.Source == this)
            {
                ListaPause.Focus();
                if (ListaPause.SelectedItem == null && ListaPause.ItemCount > 0)
                    ListaPause.SelectedIndex = 0;
            }
        };
    }

    protected override void RicaricaCore()
    {
        var pause = _storage!.CaricaPause();
        foreach (var p in pause.OrderByDescending(x => x.DataOraPausa))
            Sessioni.Add(new SessioneSospesaItem(p));
    }

    protected override string FormatSottotitolo(int count) =>
        count == 0 ? "Nessuna sessione in pausa." : $"{count} sessioni in pausa.";

    protected override void EliminaDaStorage(SessioneSospesaItem item)
        => _storage!.EliminaPausa(item.SessioneId);

    // ------------------------------------------------------------------ HANDLER

    /// <summary>Chiede alla <c>MainWindow</c> di riprendere questa sessione.</summary>
    private void OnRiprendiClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        RiprendiRichiesto?.Invoke(this, item.Pausa);
    }

    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Invio sulla riga selezionata = Riprendi; Canc avvia conferma
    /// inline (secondo Canc = conferma definitiva); Esc annulla la conferma.
    /// </summary>
    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (ListaPause.SelectedItem is not SessioneSospesaItem item) return;

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                RiprendiRichiesto?.Invoke(this, item.Pausa);
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
}
