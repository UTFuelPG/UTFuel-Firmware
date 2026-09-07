using System.Threading.Tasks;
using UTFuel.TestBench.Gui.Localization;
using UTFuel.TestBench.Gui.Settings;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using UTFuel.TestBench.Gui.ViewModels;
using UTFuel.TestBench.Gui.Views;

namespace UTFuel.TestBench.Gui;


public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public override void OnFrameworkInitializationCompleted()
    {
        
AppSettingsService
    .Instance
    .Load();


LocalizationService
    .Instance
    .Initialize(
        AppSettingsService
            .Instance
            .Current
            .LanguageCode
    );
        if (
            ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
        )
        {
            /*
             * =========================================
             * SPLASH WINDOW
             * =========================================
             */

            SplashWindow splash =
                new();


            /*
             * A Splash é inicialmente a janela
             * principal da aplicação.
             *
             * O Avalonia irá exibi-la automaticamente.
             */

            desktop.MainWindow =
                splash;


            /*
             * Quando ela estiver visível,
             * começamos a inicialização.
             */

            splash.Opened +=
                async (_, _) =>
                {
                    await StartApplicationAsync(
                        desktop,
                        splash
                    );
                };
        }


        base.OnFrameworkInitializationCompleted();
    }


    private static async Task StartApplicationAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash
    )
    {
        /*
         * =========================================
         * STARTUP
         * =========================================
         *
         * Hoje é apenas um pequeno delay.
         *
         * Futuramente colocaremos aqui:
         *
         * - carregamento de configurações
         * - detecção de portas COM
         * - carregamento do workspace
         * - inicialização dos logs
         * - busca do ESP32
         * - restauração da última conexão
         */


        await Task.Delay(
            1400
        );


        /*
         * =========================================
         * MAIN WINDOW
         * =========================================
         */

        MainWindowViewModel viewModel =
            new();


        MainWindow mainWindow =
            new()
            {
                DataContext =
                    viewModel
            };


        /*
         * Primeiro mostramos a MainWindow.
         */

        mainWindow.Show();


        /*
         * Depois transferimos o papel de
         * janela principal para ela.
         */

        desktop.MainWindow =
            mainWindow;


        /*
         * Só agora fechamos a Splash.
         */

        splash.Close();
    }
}