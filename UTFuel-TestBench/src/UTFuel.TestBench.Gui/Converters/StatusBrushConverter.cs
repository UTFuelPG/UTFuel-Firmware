using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;


namespace UTFuel.TestBench.Gui.Converters;


public sealed class StatusBrushConverter :
    IValueConverter
{
    /*
     * =========================================
     * COLORS
     * =========================================
     */

    private static readonly IBrush SuccessBrush =
        new SolidColorBrush(
            Color.Parse("#46C982")
        );


    private static readonly IBrush WarningBrush =
        new SolidColorBrush(
            Color.Parse("#DFAE4A")
        );


    private static readonly IBrush DangerBrush =
        new SolidColorBrush(
            Color.Parse("#E54B4B")
        );


    private static readonly IBrush NeutralBrush =
        new SolidColorBrush(
            Color.Parse("#748194")
        );



    /*
     * =========================================
     * CONVERT
     * =========================================
     */

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        string status =
            value?
                .ToString()?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        /*
         * -------------------------------------
         * EMPTY
         * -------------------------------------
         */

        if (
            string.IsNullOrWhiteSpace(
                status
            )
        )
        {
            return NeutralBrush;
        }



        /*
         * -------------------------------------
         * MOCK
         *
         * Must be checked before ONLINE.
         * "MOCK ONLINE" should remain amber.
         * -------------------------------------
         */

        if (
            status.Contains("MOCK")
        )
        {
            return WarningBrush;
        }



        /*
         * -------------------------------------
         * NEGATIVE CONNECTION STATES
         *
         * Must be checked before CONNECTED.
         *
         * DISCONNECTED contains CONNECTED.
         * NOT CONNECTED contains CONNECTED.
         * -------------------------------------
         */

        if (
            status.Contains("NOT CONNECTED") ||
            status.Contains("DISCONNECTED") ||
            status.Contains("OFFLINE")
        )
        {
            return NeutralBrush;
        }



        /*
         * -------------------------------------
         * FAILURE
         * -------------------------------------
         */

        if (
            status.Contains("FAULT") ||
            status.Contains("ERROR") ||
            status.Contains("FAILED") ||
            status == "FAIL"
        )
        {
            return DangerBrush;
        }



        /*
         * -------------------------------------
         * WARNING
         * -------------------------------------
         */

        if (
            status.Contains("WARNING") ||
            status.Contains("WARN") ||
            status.Contains("CANCELLED") ||
            status.Contains("CANCELED")
        )
        {
            return WarningBrush;
        }



        /*
         * -------------------------------------
         * SUCCESS / ACTIVE
         * -------------------------------------
         */

        if (
            status.Contains("CONNECTED") ||
            status.Contains("ONLINE") ||
            status.Contains("READY") ||
            status.Contains("RUNNING") ||
            status == "PASS" ||
            status.Contains("SUCCESS")
        )
        {
            return SuccessBrush;
        }



        /*
         * -------------------------------------
         * NEUTRAL / IDLE
         * -------------------------------------
         */

        if (
            status.Contains("STOPPED") ||
            status.Contains("WAITING") ||
            status.Contains("IDLE") ||
            status.Contains("NOT RUN") ||
            status.Contains("UNKNOWN")
        )
        {
            return NeutralBrush;
        }



        /*
         * -------------------------------------
         * FALLBACK
         * -------------------------------------
         */

        return NeutralBrush;
    }



    /*
     * =========================================
     * CONVERT BACK
     * =========================================
     */

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException();
    }
}