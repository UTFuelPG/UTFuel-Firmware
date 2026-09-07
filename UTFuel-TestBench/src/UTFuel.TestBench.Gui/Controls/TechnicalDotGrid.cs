using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;


namespace UTFuel.TestBench.Gui.Controls;


public sealed class TechnicalDotGrid :
    Control
{
    /*
     * =========================================
     * PROPERTIES
     * =========================================
     */

    public static readonly StyledProperty<double>
        SpacingProperty =
            AvaloniaProperty.Register<
                TechnicalDotGrid,
                double
            >(
                nameof(Spacing),
                26.0
            );


    public static readonly StyledProperty<double>
        DotRadiusProperty =
            AvaloniaProperty.Register<
                TechnicalDotGrid,
                double
            >(
                nameof(DotRadius),
                0.7
            );


    public static readonly StyledProperty<IBrush?>
        DotBrushProperty =
            AvaloniaProperty.Register<
                TechnicalDotGrid,
                IBrush?
            >(
                nameof(DotBrush),
                new SolidColorBrush(
                    Color.FromArgb(
                        30,
                        116,
                        129,
                        148
                    )
                )
            );



    /*
     * =========================================
     * PUBLIC PROPERTIES
     * =========================================
     */

    public double Spacing
    {
        get =>
            GetValue(
                SpacingProperty
            );

        set =>
            SetValue(
                SpacingProperty,
                value
            );
    }


    public double DotRadius
    {
        get =>
            GetValue(
                DotRadiusProperty
            );

        set =>
            SetValue(
                DotRadiusProperty,
                value
            );
    }


    public IBrush? DotBrush
    {
        get =>
            GetValue(
                DotBrushProperty
            );

        set =>
            SetValue(
                DotBrushProperty,
                value
            );
    }



    /*
     * =========================================
     * STATIC INITIALIZATION
     * =========================================
     */

    static TechnicalDotGrid()
    {
        AffectsRender<TechnicalDotGrid>(
            SpacingProperty,
            DotRadiusProperty,
            DotBrushProperty
        );
    }



    /*
     * =========================================
     * RENDER
     * =========================================
     */

    public override void Render(
        DrawingContext context
    )
    {
        base.Render(
            context
        );


        IBrush? brush =
            DotBrush;


        if (
            brush == null
        )
        {
            return;
        }


        double spacing =
            Math.Max(
                12.0,
                Spacing
            );


        double radius =
            Math.Clamp(
                DotRadius,
                0.25,
                2.0
            );


        double width =
            Bounds.Width;


        double height =
            Bounds.Height;


        /*
         * Start half a cell away from the edge.
         * This avoids the grid looking glued
         * directly to the sidebar/header border.
         */

        double start =
            spacing / 2.0;


        for (
            double y = start;
            y < height;
            y += spacing
        )
        {
            for (
                double x = start;
                x < width;
                x += spacing
            )
            {
                context.DrawEllipse(
                    brush,
                    null,
                    new Point(
                        x,
                        y
                    ),
                    radius,
                    radius
                );
            }
        }
    }
}