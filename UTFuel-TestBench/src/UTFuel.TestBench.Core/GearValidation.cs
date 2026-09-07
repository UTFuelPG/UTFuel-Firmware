namespace UTFuel.TestBench.Core;


public sealed record GearTestPoint(
    int ExpectedGear,
    int ReceivedGear,
    uint Rpm,
    double SpeedKmh,
    bool Passed
);


public sealed record GearValidationReport(
    IReadOnlyList<GearTestPoint> Points,
    bool Passed
);


public static class GearValidation
{
    /*
     * =========================================
     * TEMPORARY VEHICLE CONFIGURATION
     * =========================================
     *
     * IMPORTANT:
     *
     * These values must remain identical to
     * the temporary vehicle configuration
     * currently used by the firmware.
     */

    private const double
        TireCircumferenceM =
            1.60;


    private const double
        FinalDriveRatio =
            4.0;


    private static readonly double[]
        GearRatios =
        {
            3.00,
            2.10,
            1.60,
            1.30,
            1.05,
            0.85
        };



    /*
     * =========================================
     * RUN VALIDATION
     * =========================================
     */

    public static async Task<GearValidationReport>
        RunAsync(
            ITestBenchSession connection,
            uint initialSequence,
            TimeSpan timeout
        )
    {
        List<GearTestPoint>
            results =
                new();


        uint sequence =
            initialSequence;


        const uint testRpm =
            6000;



        /*
         * Test every configured gear.
         */

        for (
            int gearIndex = 0;
            gearIndex < GearRatios.Length;
            gearIndex++
        )
        {
            int expectedGear =
                gearIndex +
                1;



            double gearRatio =
                GearRatios[
                    gearIndex
                ];



            /*
             * wheelRPM =
             *
             * engineRPM /
             * (gearRatio * finalDrive)
             */

            double wheelRpm =
                testRpm /
                (
                    gearRatio *
                    FinalDriveRatio
                );



            /*
             * wheel RPM
             *     ↓
             * rotations per second
             */

            double wheelRps =
                wheelRpm /
                60.0;



            /*
             * wheel rotations
             *     ↓
             * vehicle linear speed
             */

            double speedMs =
                wheelRps *
                TireCircumferenceM;



            double speedKmh =
                speedMs *
                3.6;



            InputPacket input =
                new(
                    SequenceId:
                        sequence++,

                    TpsVoltage:
                        2.50,

                    MapVoltage:
                        2.50,

                    CoolantResistance:
                        1200,

                    IntakeResistance:
                        2500,

                    BatteryVoltage:
                        13.80,

                    Rpm:
                        testRpm,

                    SpeedKmh:
                        speedKmh
                );



            var response =
                await connection
                    .SendInputAsync(
                        input,
                        timeout
                    );



            int received =
                response
                    .Packet
                    .Gear;



            bool passed =
                received ==
                expectedGear;



            results.Add(
                new GearTestPoint(
                    ExpectedGear:
                        expectedGear,

                    ReceivedGear:
                        received,

                    Rpm:
                        testRpm,

                    SpeedKmh:
                        speedKmh,

                    Passed:
                        passed
                )
            );
        }



        /*
         * =========================================
         * REPORT
         * =========================================
         */

        return new GearValidationReport(
            Points:
                results,

            Passed:
                results.All(
                    result =>
                        result.Passed
                )
        );
    }
}