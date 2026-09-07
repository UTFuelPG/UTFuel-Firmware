using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

using UTFuel.TestBench.Gui.ViewModels;


namespace UTFuel.TestBench.Gui.Views;


public partial class ShowcaseWindow :
    Window
{
    private MainWindowViewModel?
        _viewModel;


    private bool
        _lastShiftState;


    private bool
        _shiftArmed =
            true;


    private CancellationTokenSource?
        _shiftAnimationCancellation;



    public ShowcaseWindow()
    {
        InitializeComponent();


        DataContextChanged +=
            OnDataContextChanged;


        KeyDown +=
            OnKeyDown;


        Closed +=
            OnClosed;
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
        if (
            _viewModel !=
            null
        )
        {
            _viewModel
                .PropertyChanged -=
                OnViewModelPropertyChanged;
        }


        _viewModel =
            DataContext
            as MainWindowViewModel;


        if (
            _viewModel !=
            null
        )
        {
            _viewModel
                .PropertyChanged +=
                OnViewModelPropertyChanged;


            _lastShiftState =
                _viewModel
                    .EcuShiftWarning;
        }
    }



    /*
     * =========================================
     * ECU PROPERTY CHANGES
     * =========================================
     */

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName !=
            nameof(
                MainWindowViewModel
                    .EcuShiftWarning
            )
        )
        {
            return;
        }


        if (
            _viewModel ==
            null
        )
        {
            return;
        }


        bool current =
            _viewModel
                .EcuShiftWarning;


        /*
         * Rising edge:
         *
         * false -> true
         */

        if (
            current &&
            !_lastShiftState &&
            _shiftArmed
        )
        {
            _shiftArmed =
                false;


            _ =
                RunShiftAlertAsync();
        }


        /*
         * Rearm only after ECU goes
         * back to false.
         */

        if (
            !current
        )
        {
            _shiftArmed =
                true;
        }


        _lastShiftState =
            current;
    }



    /*
     * =========================================
     * SHIFT ALERT
     * =========================================
     */

    private async Task RunShiftAlertAsync()
    {
        /*
         * Stop an old animation if,
         * for some reason, one still exists.
         */

        _shiftAnimationCancellation?
            .Cancel();


        _shiftAnimationCancellation?
            .Dispose();


        _shiftAnimationCancellation =
            new CancellationTokenSource();


        CancellationToken token =
            _shiftAnimationCancellation
                .Token;


        try
        {
            ShiftOverlay
                .IsVisible =
                true;


            /*
             * Fixed solid backgrounds.
             *
             * No gradients.
             * No glow.
             * White SHIFT text.
             */

            Color[] colors =
            {
                Color.Parse(
                    "#FFD400"
                ),

                Color.Parse(
                    "#FF7A00"
                ),

                Color.Parse(
                    "#D91E18"
                ),

                Color.Parse(
                    "#FFD400"
                ),

                Color.Parse(
                    "#FF7A00"
                ),

                Color.Parse(
                    "#D91E18"
                )
            };


            foreach (
                Color color
                in colors
            )
            {
                token
                    .ThrowIfCancellationRequested();


                await Dispatcher
                    .UIThread
                    .InvokeAsync(
                        () =>
                        {
                            ShiftOverlay
                                .Background =
                                new SolidColorBrush(
                                    color
                                );
                        }
                    );


                await Task.Delay(
                    100,
                    token
                );
            }
        }
        catch (
            OperationCanceledException
        )
        {
        }
        finally
        {
            await Dispatcher
                .UIThread
                .InvokeAsync(
                    () =>
                    {
                        ShiftOverlay
                            .IsVisible =
                            false;
                    }
                );
        }
    }



    /*
     * =========================================
     * ESC = CLOSE SHOWCASE
     * =========================================
     */

    private void OnKeyDown(
        object? sender,
        KeyEventArgs e
    )
    {
        if (
            e.Key ==
            Key.Escape
        )
        {
            Close();
        }
    }



    /*
     * =========================================
     * CLEANUP
     * =========================================
     */

    private void OnClosed(
        object? sender,
        EventArgs e
    )
    {
        if (
            _viewModel !=
            null
        )
        {
            _viewModel
                .PropertyChanged -=
                OnViewModelPropertyChanged;
        }


        _shiftAnimationCancellation?
            .Cancel();


        _shiftAnimationCancellation?
            .Dispose();


        _shiftAnimationCancellation =
            null;
    }
}