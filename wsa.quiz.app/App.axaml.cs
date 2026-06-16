using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Wsa.Quiz.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var mainView = new MainView();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Desktop: la shell vive dentro una Window con le stesse dimensioni di prima.
            desktop.MainWindow = new Window
            {
                Title = "WSA Quiz",
                Width = 1080,
                Height = 760,
                MinWidth = 780,
                MinHeight = 540,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = mainView
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Android / single-view: la shell è la view radice, niente Window.
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
