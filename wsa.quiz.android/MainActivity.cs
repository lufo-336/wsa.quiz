using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Wsa.Quiz.Droid;

// Avalonia 12: l'Activity NON è più generica (era AvaloniaMainActivity<App> in
// v11). La scelta dell'App e la configurazione dell'AppBuilder vivono ora in
// una sottoclasse di Android.App.Application — vedi MainApplication.cs.
[Activity(
    Label = "WSA Quiz",
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
