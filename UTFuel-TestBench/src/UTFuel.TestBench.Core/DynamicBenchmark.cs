using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UTFuel.TestBench.Core;


public enum DynamicBenchmarkPhase
{
    Idle,
    Accelerating,
    Shift,
    Decelerating
}


public sealed record DynamicBenchmarkFrame(
    double TpsVoltage,
    double MapVoltage,
    double CoolantResistance,
    double IntakeResistance,
    double BatteryVoltage,
    uint Rpm,
    double SpeedKmh,
    int TargetGear,
    string Phase
);


public static class DynamicBenchmark
{
    public static async IAsyncEnumerable<DynamicBenchmarkFrame>
        RunAsync(
            int intervalMs = 50,

            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        )
    {
        double dt =
            intervalMs / 1000.0;


        double totalTime =
            0.0;


        double phaseTime =
            0.0;


        double rpm =
            1200.0;


        int gear =
            0;


        DynamicBenchmarkPhase phase =
            DynamicBenchmarkPhase.Idle;


        while (
            !cancellationToken
                .IsCancellationRequested
        )
        {
            totalTime +=
                dt;


            phaseTime +=
                dt;


            /*
             * =========================================
             * IDLE
             * =========================================
             */

            if (
                phase ==
                DynamicBenchmarkPhase.Idle
            )
            {
                gear =
                    0;


                rpm =
                    Approach(
                        rpm,
                        1200.0,
                        1500.0 * dt
                    );


                if (
                    phaseTime >=
                    2.0
                )
                {
                    gear =
                        1;


                    rpm =
                        1000.0;


                    phase =
                        DynamicBenchmarkPhase
                            .Accelerating;


                    phaseTime =
                        0.0;
                }
            }


            /*
             * =========================================
             * ACCELERATION
             * =========================================
             */

            else if (
                phase ==
                DynamicBenchmarkPhase.Accelerating
            )
            {
                /*
                 * Approx. 1500 RPM / second.
                 */

                rpm +=
                    1500.0 *
                    dt;


                if (
                    rpm >=
                    9000.0
                )
                {
                    rpm =
                        9000.0;


                    phase =
                        DynamicBenchmarkPhase.Shift;


                    phaseTime =
                        0.0;
                }
            }


            /*
             * =========================================
             * SHIFT
             * =========================================
             */

            else if (
                phase ==
                DynamicBenchmarkPhase.Shift
            )
            {
                /*
                 * Hold 9000 RPM briefly so
                 * the ECU can detect SHIFT.
                 */

                rpm =
                    9000.0;


                if (
                    phaseTime >=
                    0.30
                )
                {
                    if (
                        gear <
                        VehicleTestConfiguration
                            .GearRatios
                            .Length
                    )
                    {
                        int oldGear =
                            gear;


                        int newGear =
                            gear + 1;


                        double oldRatio =
                            VehicleTestConfiguration
                                .GearRatios[
                                    oldGear - 1
                                ];


                        double newRatio =
                            VehicleTestConfiguration
                                .GearRatios[
                                    newGear - 1
                                ];


                        /*
                         * Preserve vehicle speed
                         * during gear change.
                         */

                        rpm =
                            rpm *
                            newRatio /
                            oldRatio;


                        gear =
                            newGear;


                        phase =
                            DynamicBenchmarkPhase
                                .Accelerating;


                        phaseTime =
                            0.0;
                    }
                    else
                    {
                        /*
                         * Top gear reached.
                         */

                        phase =
                            DynamicBenchmarkPhase
                                .Decelerating;


                        phaseTime =
                            0.0;
                    }
                }
            }


            /*
             * =========================================
             * DECELERATION
             * =========================================
             */

            else if (
                phase ==
                DynamicBenchmarkPhase.Decelerating
            )
            {
                rpm -=
                    1100.0 *
                    dt;


                /*
                 * Downshift around 2500 RPM.
                 */

                if (
                    rpm <=
                    2500.0 &&
                    gear > 1
                )
                {
                    int oldGear =
                        gear;


                    int newGear =
                        gear - 1;


                    double oldRatio =
                        VehicleTestConfiguration
                            .GearRatios[
                                oldGear - 1
                            ];


                    double newRatio =
                        VehicleTestConfiguration
                            .GearRatios[
                                newGear - 1
                            ];


                    /*
                     * Preserve vehicle speed.
                     */

                    rpm =
                        rpm *
                        newRatio /
                        oldRatio;


                    gear =
                        newGear;
                }


                if (
                    gear == 1 &&
                    rpm <=
                    1000.0
                )
                {
                    gear =
                        0;


                    rpm =
                        1200.0;


                    phase =
                        DynamicBenchmarkPhase.Idle;


                    phaseTime =
                        0.0;
                }
            }


            /*
             * =========================================
             * SENSOR MODEL
             * =========================================
             */

            double tpsPercent;

            double mapKpa;


            switch (phase)
            {
                case
                    DynamicBenchmarkPhase.Idle:

                    tpsPercent =
                        2.5 +
                        Math.Sin(
                            totalTime *
                            2.0
                        ) *
                        0.6;


                    mapKpa =
                        30.0 +
                        Math.Sin(
                            totalTime *
                            1.4
                        ) *
                        1.5;

                    break;


                case
                    DynamicBenchmarkPhase.Accelerating:

                    tpsPercent =
                        84.0 +
                        Math.Sin(
                            totalTime *
                            0.8
                        ) *
                        7.0;


                    mapKpa =
                        94.0 +
                        Math.Sin(
                            totalTime *
                            0.6
                        ) *
                        4.0;

                    break;


                case
                    DynamicBenchmarkPhase.Shift:

                    /*
                     * Simulate throttle lift
                     * during gear change.
                     */

                    tpsPercent =
                        15.0;


                    mapKpa =
                        45.0;

                    break;


                case
                    DynamicBenchmarkPhase.Decelerating:

                    tpsPercent =
                        1.5;


                    mapKpa =
                        26.0 +
                        Math.Sin(
                            totalTime
                        );

                    break;


                default:

                    tpsPercent =
                        0.0;


                    mapKpa =
                        30.0;

                    break;
            }


            tpsPercent =
                Math.Clamp(
                    tpsPercent,
                    0.0,
                    100.0
                );


            mapKpa =
                Math.Clamp(
                    mapKpa,
                    20.0,
                    250.0
                );


            /*
             * TPS:
             *
             * 0.50 V = 0 %
             * 4.50 V = 100 %
             */

            double tpsVoltage =
                0.50 +
                (
                    tpsPercent /
                    100.0
                ) *
                4.0;


            /*
             * MAP:
             *
             * 0.50 V = 20 kPa
             * 4.50 V = 250 kPa
             */

            double mapVoltage =
                0.50 +
                (
                    (
                        mapKpa -
                        20.0
                    ) /
                    230.0
                ) *
                4.0;


            /*
             * Simulated alternator /
             * battery behavior.
             */

            double batteryVoltage =
                13.60 +
                Math.Clamp(
                    (
                        rpm -
                        1000.0
                    ) /
                    5000.0,

                    0.0,
                    1.0
                ) *
                0.40 +
                Math.Sin(
                    totalTime *
                    0.7
                ) *
                0.03;


            double speedKmh =
                0.0;


            if (
                gear >= 1
            )
            {
                speedKmh =
                    VehicleTestConfiguration
                        .CalculateSpeedKmh(
                            (uint)Math.Round(
                                rpm
                            ),

                            gear
                        );
            }


            yield return
                new DynamicBenchmarkFrame(
                    TpsVoltage:
                        tpsVoltage,

                    MapVoltage:
                        mapVoltage,

                    CoolantResistance:
                        1200.0,

                    IntakeResistance:
                        2500.0,

                    BatteryVoltage:
                        batteryVoltage,

                    Rpm:
                        (uint)Math.Round(
                            rpm
                        ),

                    SpeedKmh:
                        speedKmh,

                    TargetGear:
                        gear,

                    Phase:
                        phase.ToString()
                );


            await Task.Delay(
                intervalMs,
                cancellationToken
            );
        }
    }


    private static double Approach(
        double current,
        double target,
        double amount
    )
    {
        if (
            current <
            target
        )
        {
            return Math.Min(
                current +
                amount,

                target
            );
        }


        return Math.Max(
            current -
            amount,

            target
        );
    }
}