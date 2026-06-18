using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Wsa.Quiz.Droid;

// Avalonia 12: il bootstrap Android è passato dall'Activity a una sottoclasse
// di Android.App.Application. AvaloniaAndroidApplication<TApp> istanzia l'App
// (qui Wsa.Quiz.App.App, riferita completamente qualificata per evitare il
// CS0118 namespace-vs-tipo — stessa famiglia della Trappola 1) e usa il
// lifetime single-view; App.OnFrameworkInitializationCompleted assegna MainView.
[Application(Label = "WSA Quiz", AllowBackup = true)]
public class MainApplication : AvaloniaAndroidApplication<Wsa.Quiz.App.App>
{
    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
