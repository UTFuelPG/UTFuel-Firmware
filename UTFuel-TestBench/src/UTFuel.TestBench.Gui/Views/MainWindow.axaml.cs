using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;

using ScottPlot.Avalonia;
using ScottPlot.Plottables;

using UTFuel.TestBench.Gui.Models;
using UTFuel.TestBench.Gui.ViewModels;


namespace UTFuel.TestBench.Gui.Views;


public partial class MainWindow :
    Window
{
    /*
     * =========================================
     * PLOTS
     * =========================================
     */

    private AvaPlot? _rpmPlot;
    private AvaPlot? _tpsPlot;
    private AvaPlot? _mapPlot;
    private AvaPlot? _speedPlot;


    private DataLogger? _rpmLogger;
    private DataLogger? _tpsLogger;
    private DataLogger? _mapLogger;
    private DataLogger? _speedLogger;


    /*
     * Used to generate the X axis
     * in seconds.
     */

    private readonly Stopwatch
        _telemetryClock =
            Stopwatch.StartNew();


    /*
     * Rendering four graphs at 100 Hz would be
     * unnecessarily expensive.
     *
     * Data can arrive faster, but the GUI will
     * redraw at approximately 20 FPS.
     */

    private readonly Stopwatch
        _renderClock =
            Stopwatch.StartNew();


    private MainWindowViewModel?
        _subscribedViewModel;
    
    private ShowcaseWindow?
    _showcaseWindow;

    private void OpenShowcaseWindow_Click(
    object? sender,
    RoutedEventArgs e
)
{
    /*
     * Don't create multiple showcase windows.
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


    _showcaseWindow =
        new ShowcaseWindow
        {
            DataContext =
                DataContext
        };


    _showcaseWindow
        .Closed +=
        (
            _,
            _
        ) =>
        {
            _showcaseWindow =
                null;
        };


    _showcaseWindow
        .Show();
}


    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public MainWindow()
    {
        InitializeComponent();


        FindPlots();


        InitializePlots();


        DataContextChanged +=
            OnDataContextChanged;


        Closed +=
            OnWindowClosed;
    }



    /*
     * =========================================
     * FIND AVALONIA CONTROLS
     * =========================================
     */

    private void FindPlots()
    {
        _rpmPlot =
            this.FindControl<AvaPlot>(
                "RpmPlot"
            );


        _tpsPlot =
            this.FindControl<AvaPlot>(
                "TpsPlot"
            );


        _mapPlot =
            this.FindControl<AvaPlot>(
                "MapPlot"
            );


        _speedPlot =
            this.FindControl<AvaPlot>(
                "SpeedPlot"
            );
    }



    /*
     * =========================================
     * INITIALIZE PLOTS
     * =========================================
     */

    private void InitializePlots()
    {
        if (
            _rpmPlot == null ||
            _tpsPlot == null ||
            _mapPlot == null ||
            _speedPlot == null
        )
        {
            return;
        }


        /*
         * DataLogger is designed specifically
         * for data that grows while the
         * application is running.
         */

        _rpmLogger =
            _rpmPlot
                .Plot
                .Add
                .DataLogger();


        _tpsLogger =
            _tpsPlot
                .Plot
                .Add
                .DataLogger();


        _mapLogger =
            _mapPlot
                .Plot
                .Add
                .DataLogger();


        _speedLogger =
            _speedPlot
                .Plot
                .Add
                .DataLogger();


        /*
         * Fixed Y ranges make the graphs much
         * easier to read during a test.
         */

        _rpmPlot
            .Plot
            .Axes
            .SetLimitsY(
                0,
                12000
            );


        _tpsPlot
            .Plot
            .Axes
            .SetLimitsY(
                0,
                100
            );


        _mapPlot
            .Plot
            .Axes
            .SetLimitsY(
                0,
                250
            );


        _speedPlot
            .Plot
            .Axes
            .SetLimitsY(
                0,
                250
            );


        /*
         * Initial 10 second window.
         */

        SetTimeWindow(
            0
        );


        _rpmPlot.Refresh();
        _tpsPlot.Refresh();
        _mapPlot.Refresh();
        _speedPlot.Refresh();
    }



    /*
     * =========================================
     * DATACONTEXT
     * =========================================
     */

    private void OnDataContextChanged(
        object? sender,
        EventArgs e
    )
    {
        /*
         * Remove subscription from the old VM.
         */

        if (
            _subscribedViewModel !=
            null
        )
        {
            _subscribedViewModel
                .TelemetrySampleReceived -=
                OnTelemetrySampleReceived;
        }


        _subscribedViewModel =
            DataContext
            as MainWindowViewModel;


        /*
         * Subscribe to new VM.
         */

        if (
            _subscribedViewModel !=
            null
        )
        {
            _subscribedViewModel
                .TelemetrySampleReceived +=
                OnTelemetrySampleReceived;
        }
    }



    /*
     * =========================================
     * TELEMETRY EVENT
     * =========================================
     */

    private void OnTelemetrySampleReceived(
        LiveTelemetrySample sample
    )
    {
        /*
         * Always update Avalonia controls
         * through the UI thread.
         */

        Dispatcher
            .UIThread
            .Post(
                () =>
                    AddTelemetrySample(
                        sample
                    )
            );
    }



    /*
     * =========================================
     * ADD DATA
     * =========================================
     */

    private void AddTelemetrySample(
        LiveTelemetrySample sample
    )
    {
        if (
            _rpmLogger == null ||
            _tpsLogger == null ||
            _mapLogger == null ||
            _speedLogger == null
        )
        {
            return;
        }


        double seconds =
            _telemetryClock
                .Elapsed
                .TotalSeconds;


        /*
         * Store every received sample.
         */

        _rpmLogger.Add(
            seconds,
            sample.Rpm
        );


        _tpsLogger.Add(
            seconds,
            sample.TpsPercent
        );


        _mapLogger.Add(
            seconds,
            sample.MapKpa
        );


        _speedLogger.Add(
            seconds,
            sample.SpeedKmh
        );


        /*
         * Limit screen rendering to about
         * 20 frames per second.
         *
         * The ECU communication can still
         * operate at a higher frequency.
         */

        if (
            _renderClock
                .ElapsedMilliseconds <
            50
        )
        {
            return;
        }


        _renderClock
            .Restart();


        SetTimeWindow(
            seconds
        );


        _rpmPlot?.Refresh();

        _tpsPlot?.Refresh();

        _mapPlot?.Refresh();

        _speedPlot?.Refresh();
    }



    /*
     * =========================================
     * 10 SECOND MOVING WINDOW
     * =========================================
     */

    private void SetTimeWindow(
        double currentSeconds
    )
    {
        double right =
            Math.Max(
                10.0,
                currentSeconds
            );


        double left =
            Math.Max(
                0.0,
                right -
                10.0
            );


        _rpmPlot?
            .Plot
            .Axes
            .SetLimitsX(
                left,
                right
            );


        _tpsPlot?
            .Plot
            .Axes
            .SetLimitsX(
                left,
                right
            );


        _mapPlot?
            .Plot
            .Axes
            .SetLimitsX(
                left,
                right
            );


        _speedPlot?
            .Plot
            .Axes
            .SetLimitsX(
                left,
                right
            );
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
    _showcaseWindow !=
    null
)
{
    _showcaseWindow
        .Close();


    _showcaseWindow =
        null;
}

        if (
            _subscribedViewModel !=
            null
        )
        {
            _subscribedViewModel
                .TelemetrySampleReceived -=
                OnTelemetrySampleReceived;


            await _subscribedViewModel
                .ShutdownAsync();
        }
    }
}