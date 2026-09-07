using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;


namespace UTFuel.TestBench.Gui.Converters;


public sealed class BooleanStatusBrushConverter :
    IValueConverter
{
    private static readonly IBrush ActiveBrush =
        new SolidColorBrush(
            Color.Parse("#46C982")
        );


    private static readonly IBrush InactiveBrush =
        new SolidColorBrush(
            Color.Parse("#748194")
        );


    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        return value is true
            ? ActiveBrush
            : InactiveBrush;
    }


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