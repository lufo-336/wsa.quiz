using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia.Android;
using Wsa.Quiz.App;

namespace Wsa.Quiz.Droid;

// Avalonia 12: l'Activity NON è più generica (era AvaloniaMainActivity<App> in
// v11). La scelta dell'App e la configurazione dell'AppBuilder vivono ora in
// una sottoclasse di Android.App.Application — vedi MainApplication.cs.
[Activity(
    Label = "WSA Quiz",
    Theme = "@style/WsaQuizTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity, View.IOnApplyWindowInsetsListener
{
    // Su targetSdk 35+ l'edge-to-edge è forzato: leggo le insets delle barre di
    // sistema dalla decor view (fonte autorevole) e le passo alla UI condivisa via
    // AppEnv. L'IInsetsManager di Avalonia su questo device riporta bottom=0, quindi
    // la bottom nav finiva sotto i tasti Android: questa è la fonte affidabile.
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var decor = Window?.DecorView;
        if (decor is not null)
        {
            decor.SetOnApplyWindowInsetsListener(this);
            decor.RequestApplyInsets();
        }
    }

    public WindowInsets? OnApplyWindowInsets(View? v, WindowInsets? insets)
    {
        if (insets is not null)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var bars = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
                AppEnv.SetSystemBarInsetsPx(bars.Left, bars.Top, bars.Right, bars.Bottom);
            }
            else
            {
#pragma warning disable CA1422 // API deprecate da 30: ramo solo per device < API 30
                AppEnv.SetSystemBarInsetsPx(
                    insets.SystemWindowInsetLeft, insets.SystemWindowInsetTop,
                    insets.SystemWindowInsetRight, insets.SystemWindowInsetBottom);
#pragma warning restore CA1422
            }
        }
        // Non consumo le insets: lascio che la decor view le dispatchi normalmente.
        return v?.OnApplyWindowInsets(insets!) ?? insets;
    }
}
