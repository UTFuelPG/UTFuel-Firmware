using System.Linq;

using Avalonia.Controls;
using Avalonia.Data;

using UTFuel.TestBench.Gui.Localization;
using UTFuel.TestBench.Gui.Settings;


namespace UTFuel.TestBench.Gui.Controls;


public partial class LanguageSelector :
    UserControl
{
    private bool
        _initializing =
            true;


    private bool
        _restoringSelection;



    public LanguageSelector()
    {
        InitializeComponent();


        LanguageComboBox.ItemsSource =
            LocalizationService
                .Instance
                .AvailableLanguages;


        LanguageComboBox.DisplayMemberBinding =
            new Binding(
                nameof(
                    LanguageInfo.DisplayName
                )
            );


        SelectCurrentLanguage();


        _initializing =
            false;
    }



    /*
     * =========================================
     * LANGUAGE CHANGE
     * =========================================
     */

    private void LanguageComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e
    )
    {
        if (
            _initializing ||
            _restoringSelection
        )
        {
            return;
        }


        if (
            LanguageComboBox.SelectedItem
            is not LanguageInfo language
        )
        {
            return;
        }


        bool success =
            LocalizationService
                .Instance
                .SetLanguage(
                    language.Code
                );


        if (
            !success
        )
        {
            _restoringSelection =
                true;


            SelectCurrentLanguage();


            _restoringSelection =
                false;


            return;
        }


        /*
         * Save user's language preference.
         */

        AppSettingsService
            .Instance
            .Current
            .LanguageCode =
            language.Code;


        AppSettingsService
            .Instance
            .Save();
    }



    /*
     * =========================================
     * CURRENT LANGUAGE
     * =========================================
     */

    private void SelectCurrentLanguage()
    {
        LanguageComboBox.SelectedItem =
            LocalizationService
                .Instance
                .AvailableLanguages
                .FirstOrDefault(
                    language =>
                        language.Code ==
                        LocalizationService
                            .Instance
                            .CurrentLanguageCode
                );
    }
}