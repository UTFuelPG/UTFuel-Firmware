using System;

using Avalonia.Controls;
using Avalonia.Interactivity;


namespace UTFuel.TestBench.Gui.Views.Pages;


public partial class ShowcaseControlView :
    UserControl
{
    private ShowcaseWindow?
        _showcaseWindow;



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public ShowcaseControlView()
    {
        InitializeComponent();
    }



    /*
     * =========================================
     * OPEN SHOWCASE
     * =========================================
     */

    private void OpenShowcase_Click(
        object? sender,
        RoutedEventArgs e
    )
    {
        /*
         * Do not create multiple Showcase
         * windows.
         */

        if (
            _showcaseWindow !=
            null
        )
        {
            _showcaseWindow
                .Activate();


            return;
        }



        /*
         * Showcase uses exactly the same
         * MainWindowViewModel as the TestBench.
         *
         * No second ECU connection is created.
         */

        _showcaseWindow =
            new ShowcaseWindow
            {
                DataContext =
                    DataContext
            };


        _showcaseWindow
            .Closed +=
            OnShowcaseClosed;



        /*
         * Use MainWindow as owner when
         * available.
         */

        Window? owner =
            TopLevel
                .GetTopLevel(
                    this
                )
            as Window;


        if (
            owner !=
            null
        )
        {
            _showcaseWindow
                .Show(
                    owner
                );
        }
        else
        {
            _showcaseWindow
                .Show();
        }
    }



    /*
     * =========================================
     * SHOWCASE CLOSED
     * =========================================
     */

    private void OnShowcaseClosed(
        object? sender,
        EventArgs e
    )
    {
        if (
            _showcaseWindow !=
            null
        )
        {
            _showcaseWindow
                .Closed -=
                OnShowcaseClosed;
        }


        _showcaseWindow =
            null;
    }
}