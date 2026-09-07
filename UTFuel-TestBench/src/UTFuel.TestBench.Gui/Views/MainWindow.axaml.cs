using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
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
     * PAGES
     * =========================================
     */

    private readonly Dictionary<string, Control>
        _pages;


    /*
     * =========================================
     * NAVIGATION BUTTONS
     * =========================================
     */

    private readonly List<Button>
        _navigationButtons;


    /*
     * =========================================
     * SIDEBAR STATE
     * =========================================
     */

    private bool
        _isSidebarCollapsed;


    private const double
        ExpandedSidebarWidth =
            240;


    private const double
        CollapsedSidebarWidth =
            64;



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public MainWindow()
    {
        InitializeComponent();


        /*
         * -------------------------------------
         * Create pages once.
         * -------------------------------------
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
    "Diagnostics",
    new DiagnosticsView()
},

                {
                    "Showcase",
                    new ShowcaseControlView()
                }
            };


        /*
         * -------------------------------------
         * Normal + compact navigation buttons.
         * -------------------------------------
         */

        _navigationButtons =
            new List<Button>
            {
                OverviewNavButton,
                ManualNavButton,
                BenchmarkNavButton,
                TelemetryNavButton,
                ValidationNavButton,
                DiagnosticsNavButton,
                ShowcaseNavButton,

                CompactOverviewNavButton,
                CompactManualNavButton,
                CompactBenchmarkNavButton,
                CompactTelemetryNavButton,
                CompactValidationNavButton,
                CompactDiagnosticsNavButton,
                CompactShowcaseNavButton
            };


        /*
         * -------------------------------------
         * DataContext synchronization.
         *
         * Keep this logic.
         * It prevents cached pages from losing
         * the MainWindow ViewModel.
         * -------------------------------------
         */

        DataContextChanged +=
            OnMainDataContextChanged;


        Opened +=
            OnWindowOpened;


        /*
         * -------------------------------------
         * Initial sidebar state.
         * -------------------------------------
         */

        SetSidebarCollapsed(
            false
        );


        /*
         * -------------------------------------
         * Initial page.
         * -------------------------------------
         */

        NavigateTo(
            "Overview"
        );


        /*
         * -------------------------------------
         * Shutdown.
         * -------------------------------------
         */

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
     * SYNCHRONIZE PAGE DATACONTEXT
     * =========================================
     */

    private void SynchronizePageDataContexts()
    {
        if (
            DataContext is not
                MainWindowViewModel
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


        PageHost.DataContext =
            mainViewModel;
    }



    /*
     * =========================================
     * NAVIGATION BUTTON
     * =========================================
     */

    private void NavigationButton_Click(
        object? sender,
        RoutedEventArgs e
    )
    {
        if (
            sender is not Button button ||
            button.Tag is not string pageKey
        )
        {
            return;
        }


        NavigateTo(
            pageKey
        );
    }



    /*
     * =========================================
     * NAVIGATE
     * =========================================
     */

    private void NavigateTo(
        string pageKey
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
         * -------------------------------------
         * Explicitly propagate ViewModel.
         * -------------------------------------
         */

        if (
            DataContext is
                MainWindowViewModel
                    mainViewModel
        )
        {
            page.DataContext =
                mainViewModel;
        }


        /*
         * -------------------------------------
         * Show page.
         * -------------------------------------
         */

        PageHost.Content =
            page;


        /*
         * -------------------------------------
         * Remove selected state everywhere.
         * -------------------------------------
         */

        foreach (
            Button button
            in _navigationButtons
        )
        {
            button.Classes.Remove(
                "selected"
            );
        }


        /*
         * -------------------------------------
         * Select both versions:
         *
         * expanded button
         * compact button
         * -------------------------------------
         */

        foreach (
            Button button
            in _navigationButtons.Where(
                button =>
                    string.Equals(
                        button.Tag as string,
                        pageKey,
                        StringComparison.Ordinal
                    )
            )
        )
        {
            if (
                !button.Classes.Contains(
                    "selected"
                )
            )
            {
                button.Classes.Add(
                    "selected"
                );
            }
        }
    }



    /*
     * =========================================
     * SIDEBAR TOGGLE
     * =========================================
     */

    private void ToggleSidebar_Click(
        object? sender,
        RoutedEventArgs e
    )
    {
        SetSidebarCollapsed(
            !_isSidebarCollapsed
        );
    }



    /*
     * =========================================
     * SIDEBAR STATE
     * =========================================
     */

    private void SetSidebarCollapsed(
        bool collapsed
    )
    {
        _isSidebarCollapsed =
            collapsed;


        /*
         * -------------------------------------
         * IMPORTANT:
         *
         * Do NOT use:
         *
         * SidebarColumn.Width
         *
         * The first BodyGrid column is the
         * sidebar column.
         * -------------------------------------
         */

        BodyGrid
            .ColumnDefinitions[0]
            .Width =
                new GridLength(
                    collapsed
                        ? CollapsedSidebarWidth
                        : ExpandedSidebarWidth
                );


        /*
         * -------------------------------------
         * Switch sidebar visual state.
         * -------------------------------------
         */

        ExpandedSidebarContent.IsVisible =
            !collapsed;


        CollapsedSidebarContent.IsVisible =
            collapsed;
    }



    /*
     * =========================================
     * WINDOW CLOSED
     * =========================================
     */

    private async void OnWindowClosed(
        object? sender,
        EventArgs e
    )
    {
        if (
            DataContext is
                MainWindowViewModel vm
        )
        {
            await vm
                .ShutdownAsync();
        }
    }
}