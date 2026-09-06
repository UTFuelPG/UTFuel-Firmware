using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Avalonia;
using Avalonia.Platform;


namespace UTFuel.TestBench.Gui.Localization;


public sealed class LocalizationService
{
    /*
     * =========================================
     * SINGLETON
     * =========================================
     */

    public static LocalizationService Instance
    {
        get;
    } =
        new();



    /*
     * =========================================
     * AVAILABLE LANGUAGES
     * =========================================
     */

    public IReadOnlyList<LanguageInfo>
        AvailableLanguages
    {
        get;
    } =
        new[]
        {
            new LanguageInfo(
                "pt-BR",
                "Português"
            ),

            new LanguageInfo(
                "en-US",
                "English"
            ),

            new LanguageInfo(
                "es-ES",
                "Español"
            )
        };



    /*
     * =========================================
     * CURRENT LANGUAGE
     * =========================================
     */

    public string CurrentLanguageCode
    {
        get;
        private set;
    } =
        "pt-BR";



    /*
     * =========================================
     * CURRENT TRANSLATIONS
     * =========================================
     */

    private Dictionary<string, string>
        _currentTranslations =
            new();



    /*
     * =========================================
     * EVENT
     * =========================================
     */

    public event EventHandler?
        LanguageChanged;



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    private LocalizationService()
    {
    }



    /*
     * =========================================
     * INITIALIZATION
     * =========================================
     */

    public bool Initialize(
        string languageCode
    )
    {
        return SetLanguageInternal(
            languageCode,
            raiseEvent: false
        );
    }



    /*
     * =========================================
     * PUBLIC LANGUAGE CHANGE
     * =========================================
     */

    public bool SetLanguage(
        string languageCode
    )
    {
        return SetLanguageInternal(
            languageCode,
            raiseEvent: true
        );
    }



    /*
     * =========================================
     * GET TRANSLATED STRING
     * =========================================
     */

    public string GetString(
        string key,
        string fallback = ""
    )
    {
        if (
            _currentTranslations.TryGetValue(
                key,
                out string? value
            )
        )
        {
            return value;
        }


        return fallback;
    }



    /*
     * =========================================
     * INTERNAL LANGUAGE CHANGE
     * =========================================
     */

    private bool SetLanguageInternal(
        string languageCode,
        bool raiseEvent
    )
    {
        try
        {
            /*
             * Find requested language.
             */

            LanguageInfo?
                language =
                    AvailableLanguages
                        .FirstOrDefault(
                            item =>
                                string.Equals(
                                    item.Code,
                                    languageCode,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        );


            if (
                language ==
                null
            )
            {
                Console.Error.WriteLine(
                    $"[Localization] Unsupported language: {languageCode}"
                );


                return false;
            }



            /*
             * Load all translations before
             * modifying application resources.
             */

            Dictionary<string, string>
                translations =
                    LoadLanguageFile(
                        language.Code
                    );



            /*
             * Get current Avalonia application.
             */

            Application?
                application =
                    Application.Current;


            if (
                application ==
                null
            )
            {
                Console.Error.WriteLine(
                    "[Localization] Application.Current is null."
                );


                return false;
            }



            /*
             * Update Avalonia DynamicResources.
             */

            foreach (
                KeyValuePair<string, string>
                    translation
                in translations
            )
            {
                application
                    .Resources[
                        translation.Key
                    ] =
                    translation.Value;
            }



            /*
             * Store translations for code-behind
             * components such as ScottPlot.
             */

            _currentTranslations =
                translations;



            /*
             * Update active language.
             */

            CurrentLanguageCode =
                language.Code;



            /*
             * IMPORTANT
             * =================================
             *
             * Do NOT modify CurrentCulture here.
             *
             * GUI language must remain separated
             * from numeric protocol formatting.
             *
             * Example:
             *
             * UI:
             * Português / English / Español
             *
             * Protocol:
             * always invariant numeric format
             *
             * 3.42
             *
             * never:
             *
             * 3,42
             */

            if (
                raiseEvent
            )
            {
                LanguageChanged?.Invoke(
                    this,
                    EventArgs.Empty
                );
            }



            Console.WriteLine(
                $"[Localization] Active language: {language.Code}"
            );


            return true;
        }
        catch (
            Exception exception
        )
        {
            /*
             * Localization failures must NEVER
             * terminate the TestBench.
             */

            Console.Error.WriteLine(
                "[Localization] Language change failed."
            );


            Console.Error.WriteLine(
                exception
            );


            return false;
        }
    }



    /*
     * =========================================
     * LOAD LANGUAGE RESOURCE
     * =========================================
     */

    private static Dictionary<string, string>
        LoadLanguageFile(
            string languageCode
        )
    {
        Uri resourceUri =
            new(
                $"avares://UTFuel.TestBench.Gui/Localization/Languages/{languageCode}.json"
            );


        Console.WriteLine(
            $"[Localization] Loading {resourceUri}"
        );



        using Stream stream =
            AssetLoader.Open(
                resourceUri
            );


        using StreamReader reader =
            new(
                stream
            );


        string json =
            reader.ReadToEnd();



        Dictionary<string, string>?
            translations =
                JsonSerializer.Deserialize<
                    Dictionary<string, string>
                >(
                    json
                );



        if (
            translations ==
            null
        )
        {
            throw new InvalidDataException(
                $"Localization resource is empty: {languageCode}"
            );
        }



        if (
            translations.Count ==
            0
        )
        {
            throw new InvalidDataException(
                $"Localization resource contains no strings: {languageCode}"
            );
        }



        return translations;
    }
}