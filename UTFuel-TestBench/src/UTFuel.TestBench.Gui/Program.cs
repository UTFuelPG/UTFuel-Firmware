using System;
using System.IO;

using Avalonia;


namespace UTFuel.TestBench.Gui;


internal sealed class Program
{
    [STAThread]
    public static void Main(
        string[] args
    )
    {
        string logPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "utfuel_boot.log"
            );


        try
        {
            File.WriteAllText(
                logPath,
                "UTFuel boot started."
                + Environment.NewLine
            );


            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args
                );


            File.AppendAllText(
                logPath,
                "Avalonia lifetime finished."
                + Environment.NewLine
            );
        }
        catch (
            Exception ex
        )
        {
            File.AppendAllText(
                logPath,
                Environment.NewLine +
                "FATAL STARTUP ERROR" +
                Environment.NewLine +
                ex +
                Environment.NewLine
            );


            throw;
        }
    }


    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}