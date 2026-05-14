using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Schermata di esecuzione del quiz. Riceve nel costruttore una <see cref="SessioneQuiz"/>
/// gia' configurata (ma non ancora avviata) e la usa come DataContext.
/// La sessione viene avviata in <c>OnAttachedToVisualTree</c>, cosi' il cronometro
/// parte solo quando la view e' effettivamente visibile.
///
/// Tastiera (step 4): A/B/C/D selezionano la risposta, Invio avanza, ESC apre il
/// menu pausa modale unificato (Annulla / Abbandona / Salva e esci) — anche il
/// bottone "Pausa" del header apre la stessa modale (step 6).
/// </summary>
public partial class QuizView : UserControl
{
    /// <summary>Sollevato quando la sessione termina (regolarmente o per abbandono).</summary>
    public event EventHandler<SessioneQuiz>? QuizConcluso;

    /// <summary>
    /// Sollevato quando l'utente ha scelto "Salva e esci" dal menu pausa: la
    /// pausa e' gia' stata persistita, la <c>MainWindow</c> deve solo navigare.
    /// </summary>
    public event EventHandler? QuizMessoInPausa;

    private readonly SessioneQuiz _sessione;
    private readonly StorageService _storage;
    private bool _avviata;

    public QuizView(SessioneQuiz sessione, StorageService storage)
    {
        InitializeComponent();
        _sessione = sessione;
        _storage = storage;
        DataContext = sessione;
        _sessione.Concluso += OnSessioneConclusa;
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_avviata)
        {
            _sessione.Avvia();
            _avviata = true;
        }
        // Focus sulla view per ricevere la tastiera (A/B/C/D, Invio, ESC).
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.A: case Key.B: case Key.C: case Key.D:
                if (!_sessione.InViewMode && _sessione.InAttesaRisposta)
                {
                    int idx = e.Key - Key.A;
                    if (idx < _sessione.Risposte.Count)
                        _sessione.RispondiA(idx);
                }
                e.Handled = true;
                return;

            case Key.Up:
                if (!_sessione.InViewMode) _sessione.HighlightSu();
                e.Handled = true;
                return;

            case Key.Down:
                if (!_sessione.InViewMode) _sessione.HighlightGiu();
                e.Handled = true;
                return;

            case Key.Left:
                _sessione.VaiAPassataPrecedente();
                e.Handled = true;
                return;

            case Key.Right:
                _sessione.VaiAPassataSuccessiva();
                e.Handled = true;
                return;

            case Key.Enter:
                if (!_sessione.InViewMode)
                {
                    if (!_sessione.ConfermaHighlight() && _sessione.RispostaInviata)
                        _sessione.Avanza();
                }
                e.Handled = true;
                return;

            case Key.Escape:
                e.Handled = true;
                OnPausaClick(null, new RoutedEventArgs());
                return;
        }

        base.OnKeyDown(e);
    }

    private void OnRispostaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RispostaItem r })
            _sessione.RispondiA(r.Indice);
        // Riprendo il focus sulla view: dopo il click il focus va sul Button
        // risposta, che diventa disabilitato; senza riprenderlo, A/B/C/D e frecce
        // non arrivano piu' al QuizView.
        Focus();
    }

    private void OnAvanzaClick(object? sender, RoutedEventArgs e)
    {
        _sessione.Avanza();
        // Dopo Avanza il bottone "Prossima domanda" sparisce (IsVisible=false
        // perche' RispostaInviata torna false), e il focus si perde. Riprendolo.
        Focus();
    }

    private async void OnPausaClick(object? sender, RoutedEventArgs e)
    {
        try { await ApriMenuPausaAsync(); }
        catch (Exception) { /* best effort */ }
    }

    private void OnTornaACorrenteClick(object? sender, RoutedEventArgs e)
    {
        _sessione.TornaACorrente();
        Focus();
    }

    /// <summary>
    /// Apre la modale pausa unificata (PausaDialog) e gestisce il risultato.
    /// </summary>
    private async Task ApriMenuPausaAsync()
    {
        var dialog = new PausaDialog();
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        Focus();

        switch (dialog.Risultato)
        {
            case PausaDialogResult.Abbandona:
                _sessione.Abbandona();
                return;

            case PausaDialogResult.Salva:
                try
                {
                    var pausa = _sessione.EsportaPausa();
                    _storage.SalvaPausa(pausa);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SalvaPausa failed: {ex}");
                    return;
                }
                QuizMessoInPausa?.Invoke(this, EventArgs.Empty);
                return;

            default: // Annulla
                return;
        }
    }

    private void OnSessioneConclusa(object? sender, EventArgs e)
    {
        QuizConcluso?.Invoke(this, _sessione);
    }
}
