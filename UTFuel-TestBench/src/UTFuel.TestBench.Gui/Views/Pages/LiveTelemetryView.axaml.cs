using System;
using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Threading;

using ScottPlot.Avalonia;
using ScottPlot.Plottables;

using UTFuel.TestBench.Gui.Localization;
using UTFuel.TestBench.Gui.Models;
using UTFuel.TestBench.Gui.ViewModels;


namespace UTFuel.TestBench.Gui.Views.Pages;


public partial class LiveTelemetryView :
    UserControl
{
    /*
     * =========================================
     * PLOTS
     * =========================================
     */

    private AvaPlot?
        _rpmPlot;

    private AvaPlot?
        _tpsPlot;

    private AvaPlot?
        _mapPlot;

    private AvaPlot?
        _speedPlot;



    /*
     * =========================================
     * DATA LOGGERS
     * =========================================
     */

    private DataLogger?
        _rpmLogger;

    private DataLogger?
        _tpsLogger;

    private DataLogger?
        _mapLogger;

    private DataLogger?
        _speedLogger;



    /*
     * =========================================
     * TIMING
     * =========================================
     */

    private readonly Stopwatch
        _telemetryClock =
            new();


    private readonly Stopwatch
        _renderClock =
            new();



    /*
     * =========================================
     * VIEWMODEL
     * =========================================
     */

    private MainWindowViewModel?
        _viewModel;


    private bool
        _isViewModelSubscribed;



    /*
     * =========================================
     * LOCALIZATION
     * =========================================
     */

    private bool
        _isLocalizationSubscribed;



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public LiveTelemetryView()
    {
        InitializeComponent();


        FindPlots();

        InitializePlots();


        /*
         * DataContext can be assigned after
         * this View is created.
         */

        DataContextChanged +=
            (_, _) =>
            {
                SubscribeToViewModel();
            };


        /*
         * ContentControl removes pages from the
         * visual tree when navigating.
         *
         * We therefore subscribe again whenever
         * this page becomes visible.
         */

        AttachedToVisualTree +=
            (_, _) =>
            {
                SubscribeToViewModel();

                SubscribeToLocalization();

                ApplyPlotLocalization();
            };


        DetachedFromVisualTree +=
            (_, _) =>
            {
                UnsubscribeFromViewModel();

                UnsubscribeFromLocalization();
            };


        _telemetryClock.Start();

        _renderClock.Start();
    }



    /*
     * =========================================
     * FIND PLOTS
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
         * DATA LOGGERS
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
         * INITIAL AXIS LIMITS
         */

        _rpmPlot
            .Plot
            .Axes
            .SetLimits(
                0,
                10,
                0,
                12000
            );


        _tpsPlot
            .Plot
            .Axes
            .SetLimits(
                0,
                10,
                0,
                100
            );


        _mapPlot
            .Plot
            .Axes
            .SetLimits(
                0,
                10,
                0,
                250
            );


        _speedPlot
            .Plot
            .Axes
            .SetLimits(
                0,
                10,
                0,
                250
            );



        ApplyPlotLocalization();

        RefreshPlots();
    }



    /*
     * =========================================
     * PLOT LOCALIZATION
     * =========================================
     */

    private void ApplyPlotLocalization()
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



        LocalizationService localization =
            LocalizationService.Instance;



        /*
         * TRANSLATED STRINGS
         */

        string timeAxis =
            localization.GetString(
                "TelemetryTimeAxis",
                "Time (s)"
            );


        string speedAxis =
            localization.GetString(
                "TelemetrySpeedAxis",
                "Speed (km/h)"
            );



        /*
         * X AXES
         */

        _rpmPlot
            .Plot
            .Axes
            .Bottom
            .Label
            .Text =
            timeAxis;


        _tpsPlot
            .Plot
            .Axes
            .Bottom
            .Label
            .Text =
            timeAxis;


        _mapPlot
            .Plot
            .Axes
            .Bottom
            .Label
            .Text =
            timeAxis;


        _speedPlot
            .Plot
            .Axes
            .Bottom
            .Label
            .Text =
            timeAxis;



        /*
         * Y AXES
         *
         * RPM, TPS, MAP and their engineering
         * units do not need translation.
         */

        _rpmPlot
            .Plot
            .Axes
            .Left
            .Label
            .Text =
            "RPM";


        _tpsPlot
            .Plot
            .Axes
            .Left
            .Label
            .Text =
            "TPS (%)";


        _mapPlot
            .Plot
            .Axes
            .Left
            .Label
            .Text =
            "MAP (kPa)";


        _speedPlot
            .Plot
            .Axes
            .Left
            .Label
            .Text =
            speedAxis;



        RefreshPlots();
    }



    /*
     * =========================================
     * LOCALIZATION SUBSCRIPTION
     * =========================================
     */

    private void SubscribeToLocalization()
    {
        if (
            _isLocalizationSubscribed
        )
        {
            return;
        }


        LocalizationService
            .Instance
            .LanguageChanged +=
            OnLanguageChanged;


        _isLocalizationSubscribed =
            true;
    }



    private void UnsubscribeFromLocalization()
    {
        if (
            !_isLocalizationSubscribed
        )
        {
            return;
        }


        LocalizationService
            .Instance
            .LanguageChanged -=
            OnLanguageChanged;


        _isLocalizationSubscribed =
            false;
    }



    private void OnLanguageChanged(
        object? sender,
        EventArgs e
    )
    {
        /*
         * ScottPlot needs to be updated
         * from the Avalonia UI thread.
         */

        Dispatcher
            .UIThread
            .Post(
                ApplyPlotLocalization
            );
    }



    /*
     * =========================================
     * VIEWMODEL SUBSCRIPTION
     * =========================================
     */

    private void SubscribeToViewModel()
    {
        MainWindowViewModel?
            currentViewModel =
                DataContext
                as MainWindowViewModel;


        if (
            currentViewModel ==
            null
        )
        {
            UnsubscribeFromViewModel();

            return;
        }


        if (
            _isViewModelSubscribed &&
            ReferenceEquals(
                _viewModel,
                currentViewModel
            )
        )
        {
            return;
        }



        UnsubscribeFromViewModel();


        _viewModel =
            currentViewModel;


        _viewModel
            .TelemetrySampleReceived +=
            OnTelemetrySampleReceived;


        _isViewModelSubscribed =
            true;
    }



    private void UnsubscribeFromViewModel()
    {
        if (
            _viewModel !=
            null &&
            _isViewModelSubscribed
        )
        {
            _viewModel
                .TelemetrySampleReceived -=
                OnTelemetrySampleReceived;
        }


        _viewModel =
            null;


        _isViewModelSubscribed =
            false;
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
         * Communication can complete outside
         * the Avalonia UI thread.
         */

        Dispatcher
            .UIThread
            .Post(
                () =>
                {
                    AddTelemetrySample(
                        sample
                    );
                }
            );
    }



    /*
     * =========================================
     * ADD SAMPLE
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
         * Store every telemetry sample.
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
         * Render at approximately 20 FPS.
         *
         * Samples continue to be stored even
         * when no screen refresh is required.
         */

        if (
            _renderClock
                .ElapsedMilliseconds
            < 50
        )
        {
            return;
        }


        _renderClock.Restart();



        /*
         * Rolling 10 second visualization
         * window.
         */

        double xMaximum =
            Math.Max(
                10,
                seconds
            );


        double xMinimum =
            Math.Max(
                0,
                xMaximum - 10
            );


        SetHorizontalLimits(
            xMinimum,
            xMaximum
        );


        RefreshPlots();
    }



    /*
     * =========================================
     * HORIZONTAL WINDOW
     * =========================================
     */

    private void SetHorizontalLimits(
        double minimum,
        double maximum
    )
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


        _rpmPlot
            .Plot
            .Axes
            .SetLimitsX(
                minimum,
                maximum
            );


        _tpsPlot
            .Plot
            .Axes
            .SetLimitsX(
                minimum,
                maximum
            );


        _mapPlot
            .Plot
            .Axes
            .SetLimitsX(
                minimum,
                maximum
            );


        _speedPlot
            .Plot
            .Axes
            .SetLimitsX(
                minimum,
                maximum
            );
    }



    /*
     * =========================================
     * REFRESH
     * =========================================
     */

    private void RefreshPlots()
    {
        _rpmPlot?.Refresh();

        _tpsPlot?.Refresh();

        _mapPlot?.Refresh();

        _speedPlot?.Refresh();
    }
}