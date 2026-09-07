using System;
using System.IO;
using System.Text.Json;


namespace UTFuel.TestBench.Gui.Settings;


public sealed class AppSettingsService
{
    /*
     * =========================================
     * SINGLETON
     * =========================================
     */

    public static AppSettingsService Instance
    {
        get;
    } =
        new();



    /*
     * =========================================
     * CURRENT SETTINGS
     * =========================================
     */

    public AppSettings Current
    {
        get;
        private set;
    } =
        new();



    /*
     * =========================================
     * PATH
     * =========================================
     */

    public string SettingsDirectory
    {
        get;
    }


    public string SettingsPath
    {
        get;
    }



    private AppSettingsService()
    {
        SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "UTFuel",
                "TestBench"
            );


        SettingsPath =
            Path.Combine(
                SettingsDirectory,
                "settings.json"
            );
    }



    /*
     * =========================================
     * LOAD
     * =========================================
     */

    public void Load()
    {
        try
        {
            if (
                !File.Exists(
                    SettingsPath
                )
            )
            {
                Current =
                    new AppSettings();

                return;
            }


            string json =
                File.ReadAllText(
                    SettingsPath
                );


            AppSettings?
                settings =
                    JsonSerializer.Deserialize<AppSettings>(
                        json
                    );


            Current =
                settings
                ??
                new AppSettings();
        }
        catch (
            Exception exception
        )
        {
            Console.Error.WriteLine(
                "[Settings] Failed to load settings."
            );

            Console.Error.WriteLine(
                exception
            );


            Current =
                new AppSettings();
        }
    }



    /*
     * =========================================
     * SAVE
     * =========================================
     */

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(
                SettingsDirectory
            );


            JsonSerializerOptions options =
                new()
                {
                    WriteIndented =
                        true
                };


            string json =
                JsonSerializer.Serialize(
                    Current,
                    options
                );


            File.WriteAllText(
                SettingsPath,
                json
            );
        }
        catch (
            Exception exception
        )
        {
            /*
             * Settings failure must never
             * terminate TestBench.
             */

            Console.Error.WriteLine(
                "[Settings] Failed to save settings."
            );

            Console.Error.WriteLine(
                exception
            );
        }
    }
}