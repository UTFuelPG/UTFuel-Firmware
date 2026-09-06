using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;

using UTFuel.TestBench.Gui.ViewModels;
using UTFuel.TestBench.Gui.Views.Pages;


namespace UTFuel.TestBench.Gui.Views;


public partial class MainWindow :
    Window
{
    /*
     * =========================================
     * PAGE STORAGE
     * =========================================
     *
     * Pages are created only once and reused.
     *
     * This preserves internal state between
     * navigation changes.
     */

    private readonly Dictionary<
        string,
        Control
    > _pages;



    /*
     * =========================================
     * NAVIGATION BUTTONS
     * =========================================
     */

    private readonly List<Button>
        _navigationButtons;



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public MainWindow()
    {
        InitializeComponent();



        /*
         * Create every page only once.
         */

        _pages =
            new Dictionary<string, Control>
            {
                {
                    "Overview",
                    new OverviewView()
                },

                {
                    "Manual",
                    new ManualControlView()
                },

                {
                    "Benchmark",
                    new DynamicBenchmarkView()
                },

                {
                    "Telemetry",
                    new LiveTelemetryView()
                },

                {
                    "Validation",
                    new ValidationView()
                },

                {
                    "Showcase",
                    new ShowcaseControlView()
                }
            };



        /*
         * Store navigation buttons.
         */

        _navigationButtons =
            new List<Button>
            {
                OverviewNavButton,
                ManualNavButton,
                BenchmarkNavButton,
                TelemetryNavButton,
                ValidationNavButton,
                ShowcaseNavButton
            };



        /*
         * When MainWindow receives or changes
         * its ViewModel, propagate it to every
         * stored page.
         */

        DataContextChanged +=
            OnMainDataContextChanged;



        /*
         * Opened happens after the MainWindow
         * has completed initialization.
         *
         * Synchronize again here to guarantee
         * that the first page also receives
         * the final MainWindowViewModel.
         */

        Opened +=
            OnWindowOpened;



        /*
         * Initial page.
         *
         * It may be created before the final
         * DataContext is assigned, but the
         * Opened/DataContext handlers above
         * will synchronize it afterwards.
         */

        NavigateTo(
            "Overview",
            OverviewNavButton
        );



        Closed +=
            OnWindowClosed;
    }



    /*
     * =========================================
     * WINDOW OPENED
     * =========================================
     */

    private void OnWindowOpened(
        object? sender,
        EventArgs e
    )
    {
        SynchronizePageDataContexts();
    }



    /*
     * =========================================
     * DATACONTEXT CHANGED
     * =========================================
     */

    private void OnMainDataContextChanged(
        object? sender,
        EventArgs e
    )
    {
        SynchronizePageDataContexts();
    }



    /*
     * =========================================
     * SYNCHRONIZE PAGE DATACONTEXTS
     * =========================================
     */

    private void SynchronizePageDataContexts()
    {
        /*
         * Only propagate a valid
         * MainWindowViewModel.
         *
         * This prevents pages from receiving
         * an accidental temporary null
         * DataContext during initialization.
         */

        if (
            DataContext
            is not MainWindowViewModel
                mainViewModel
        )
        {
            return;
        }



        foreach (
            Control page
            in _pages.Values
        )
        {
            if (
                ReferenceEquals(
                    page.DataContext,
                    mainViewModel
                )
            )
            {
                continue;
            }


            page.DataContext =
                mainViewModel;
        }



        /*
         * Keep ContentControl itself synchronized
         * as an additional safeguard.
         */

        PageHost.DataContext =
            mainViewModel;
    }



    /*
     * =========================================
     * NAVIGATION CLICK
     * =========================================
     */

    private void NavigationButton_Click(
        object? sender,
        RoutedEventArgs e
    )
    {
        if (
            sender
            is not Button button
        )
        {
            return;
        }


        if (
            button.Tag
            is not string pageKey
        )
        {
            return;
        }


        NavigateTo(
            pageKey,
            button
        );
    }



    /*
     * =========================================
     * NAVIGATE
     * =========================================
     */

    private void NavigateTo(
        string pageKey,
        Button selectedButton
    )
    {
        if (
            !_pages.TryGetValue(
                pageKey,
                out Control? page
            )
        )
        {
            return;
        }



        /*
         * Explicitly synchronize the selected
         * page every time it is displayed.
         *
         * This makes navigation independent
         * from Avalonia DataContext inheritance.
         */

        if (
            DataContext
            is MainWindowViewModel
                mainViewModel
        )
        {
            page.DataContext =
                mainViewModel;
        }



        /*
         * Change displayed page.
         */

        PageHost.Content =
            page;



        /*
         * Remove selected state from all
         * navigation buttons.
         */

        foreach (
            Button navigationButton
            in _navigationButtons
        )
        {
            navigationButton
                .Classes
                .Remove(
                    "selected"
                );
        }



        /*
         * Apply selected state.
         */

        if (
            !selectedButton
                .Classes
                .Contains(
                    "selected"
                )
        )
        {
            selectedButton
                .Classes
                .Add(
                    "selected"
                );
        }
    }



    /*
     * =========================================
     * SHUTDOWN
     * =========================================
     */

    private async void OnWindowClosed(
        object? sender,
        EventArgs e
    )
    {
        if (
            DataContext
            is MainWindowViewModel viewModel
        )
        {
            await viewModel
                .ShutdownAsync();
        }
    }
}