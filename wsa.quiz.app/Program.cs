using Avalonia;
using System;

namespace Wsa.Quiz.App;

internal class Program
{
    // Inizializzazione richiesta da Avalonia: deve essere il primo statement di Main,
    // STAThread per compatibilita' con clipboard e dialog su Windows.
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
