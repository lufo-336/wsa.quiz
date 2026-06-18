using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;

namespace Wsa.Quiz.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppEnv.TouchMode = false;
            desktop.MainWindow = new Window
            {
                Title = "WSA Quiz",
                Width = 1080,
                Height = 760,
                MinWidth = 780,
                MinHeight = 540,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new MainView()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            AppEnv.TouchMode = true;
            Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Wsa.Quiz.App/"))
            {
                Source = new Uri("avares://Wsa.Quiz.App/Themes/Sizes.Touch.axaml")
            });
            singleView.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
