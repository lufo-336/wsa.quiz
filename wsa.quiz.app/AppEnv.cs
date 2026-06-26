using System;
using Avalonia;

namespace Wsa.Quiz.App;

/// <summary>
/// Ambiente di esecuzione corrente, deciso una sola volta all'avvio in
/// <see cref="App.OnFrameworkInitializationCompleted"/>. Unico punto di verità
/// per distinguere desktop da touch (Android) senza #if sparsi nelle view.
/// </summary>
public static class AppEnv
{
    /// <summary>True su Android/single-view: abilita dimensioni e layout touch.</summary>
    public static bool TouchMode { get; set; }

    /// <summary>
    /// Insets delle barre di sistema (status bar in alto, navigation bar in basso)
    /// in PIXEL FISICI, lette direttamente da <c>WindowInsets.systemBars()</c> nella
    /// <c>MainActivity</c> Android. Fonte autorevole: l'<see cref="IInsetsManager"/>
    /// di Avalonia su questo device riporta <c>bottom=0</c> (nav bar non vista) →
    /// la bottom nav finiva sotto i tasti Android. La conversione px→unità di layout
    /// avviene in <c>MainView.ApplicaSafeArea</c>. Desktop: resta a zero.
    /// </summary>
    public static Thickness SystemBarInsetsPx { get; private set; }

    /// <summary>Sollevato quando <see cref="SystemBarInsetsPx"/> cambia (l'Android
    /// dispatcha le insets dopo il primo layout e a ogni rotazione/resume).</summary>
    public static event Action? SystemBarInsetsChanged;

    /// <summary>Chiamato dalla MainActivity Android col valore in pixel fisici.</summary>
    public static void SetSystemBarInsetsPx(double left, double top, double right, double bottom)
    {
        var t = new Thickness(left, top, right, bottom);
        if (t.Equals(SystemBarInsetsPx)) return;
        SystemBarInsetsPx = t;
        SystemBarInsetsChanged?.Invoke();
    }

    /// <summary>
    /// Fattore di scala globale dell'UI applicato SOLO su touch (Android), via
    /// <c>LayoutTransformControl</c> attorno alla <c>MainView</c> in
    /// <see cref="App.OnFrameworkInitializationCompleted"/>.
    /// &lt;1 rimpicciolisce tutto uniformemente: su Android Avalonia rende alla
    /// density del device, lasciando un viewport logico stretto in cui font e
    /// tap target risultavano troppo grandi e il contenuto verticale veniva
    /// tagliato. Scalando sotto 1 il figlio riceve più spazio logico (size/scala)
    /// → entra più contenuto e nulla esce dallo schermo. Unico punto da tarare se
    /// serve "più piccolo" (es. 0.8) o "più grande" (es. 0.9). Desktop ignora
    /// questo valore.
    /// </summary>
    public const double ScalaTouch = 0.85;
}
