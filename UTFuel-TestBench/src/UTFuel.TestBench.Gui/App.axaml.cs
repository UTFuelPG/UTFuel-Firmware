using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using UTFuel.TestBench.Gui.ViewModels;
using UTFuel.TestBench.Gui.Views;


namespace UTFuel.TestBench.Gui;


public partial class App :
    Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader
            .Load(
                this
            );
    }


    public override void OnFrameworkInitializationCompleted()
    {
        if (
            ApplicationLifetime
            is
            IClassicDesktopStyleApplicationLifetime
            desktop
        )
        {
            desktop.MainWindow =
                new MainWindow
                {
                    DataContext =
                        new MainWindowViewModel()
                };
        }


        base
            .OnFrameworkInitializationCompleted();
    }
}