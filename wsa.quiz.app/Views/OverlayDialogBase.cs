using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Base per i dialoghi modali resi come <b>overlay in-page</b> (M3): un
/// UserControl centrato sopra uno scrim semitrasparente, inserito
/// nell'<see cref="OverlayLayer"/> del TopLevel corrente. Funziona identico su
/// desktop e su Android (single-view), dove <c>Window</c>/<c>ShowDialog</c> non
/// esistono. Il <see cref="Task"/> restituito da <see cref="ShowOverlayAsync"/>
/// si completa quando una sottoclasse chiama <see cref="Chiudi"/> (tipicamente
/// dal click di un bottone), che intanto rimuove l'overlay.
/// </summary>
public abstract class OverlayDialogBase : UserControl
{
    private TaskCompletionSource? _tcs;
    private Control? _radiceOverlay;
    private OverlayLayer? _layer;
    private IDisposable? _boundsSub;

    protected OverlayDialogBase()
    {
        // Serve a ricevere i tasti (Esc, frecce) quando l'overlay prende il focus.
        Focusable = true;
    }

    /// <summary>
    /// Mostra il dialogo come overlay ancorato al TopLevel di <paramref name="ancora"/>.
    /// Restituisce un Task che si completa alla chiusura del dialogo.
    /// </summary>
    public Task ShowOverlayAsync(Visual ancora)
    {
        _layer = OverlayLayer.GetOverlayLayer(ancora)
                 ?? throw new InvalidOperationException("OverlayLayer non disponibile per l'ancora fornita.");
        _tcs = new TaskCompletionSource();

        // Scrim modale: oscura e cattura i click verso il contenuto sottostante.
        var scrim = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // 'this' (il dialogo) fa da card centrata; il suo aspetto è nella sua XAML.
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        var grid = new Grid();
        grid.Children.Add(scrim);
        grid.Children.Add(this);
        _radiceOverlay = grid;

        // OverlayLayer è un Canvas: i figli non si stirano da soli. Vincolo le
        // dimensioni della root dell'overlay ai Bounds del layer per coprirlo tutto.
        grid.Width = _layer.Bounds.Width;
        grid.Height = _layer.Bounds.Height;
        _boundsSub = _layer.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver(b =>
        {
            grid.Width = b.Width;
            grid.Height = b.Height;
        }));

        _layer.Children.Add(grid);

        Focus();
        OnShown();
        return _tcs.Task;
    }

    /// <summary>Hook post-visualizzazione: le sottoclassi vi spostano il focus sul bottone di default.</summary>
    protected virtual void OnShown() { }

    /// <summary>Chiude l'overlay (rimuove dalla scena) e completa il Task.</summary>
    protected void Chiudi()
    {
        _boundsSub?.Dispose();
        _boundsSub = null;
        if (_layer != null && _radiceOverlay != null)
            _layer.Children.Remove(_radiceOverlay);
        _radiceOverlay = null;
        _layer = null;
        _tcs?.TrySetResult();
        _tcs = null;
    }

    /// <summary>Osservatore minimale per <c>GetObservable(...).Subscribe(...)</c> senza dipendere da System.Reactive.</summary>
    private sealed class AnonymousObserver : IObserver<Rect>
    {
        private readonly Action<Rect> _onNext;
        public AnonymousObserver(Action<Rect> onNext) => _onNext = onNext;
        public void OnNext(Rect value) => _onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
