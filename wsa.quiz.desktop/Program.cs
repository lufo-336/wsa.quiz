using Avalonia;
using System;

namespace Wsa.Quiz.Desktop;

internal class Program
{
    // STAThread per compatibilita' clipboard/dialog su Windows.
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Wsa.Quiz.App.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
