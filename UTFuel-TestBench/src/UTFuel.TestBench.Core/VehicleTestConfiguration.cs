using System;

namespace UTFuel.TestBench.Core;

public static class VehicleTestConfiguration
{
    /*
     * Temporary vehicle configuration.
     *
     * These values will later be replaced by
     * actual UTFuel / UTFast vehicle configuration.
     */

    public const double TireCircumferenceM =
        1.60;

    public const double FinalDriveRatio =
        4.0;


    public static readonly double[] GearRatios =
    {
        3.00,
        2.10,
        1.60,
        1.30,
        1.05,
        0.85
    };


    public static double CalculateSpeedKmh(
        uint engineRpm,
        int gear
    )
    {
        if (
            gear < 1 ||
            gear > GearRatios.Length
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(gear)
            );
        }


        double gearRatio =
            GearRatios[gear - 1];


        double wheelRpm =
            engineRpm /
            (
                gearRatio *
                FinalDriveRatio
            );


        double wheelRps =
            wheelRpm / 60.0;


        double speedMs =
            wheelRps *
            TireCircumferenceM;


        return speedMs * 3.6;
    }
}