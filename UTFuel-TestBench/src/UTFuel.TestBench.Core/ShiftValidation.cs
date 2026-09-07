namespace UTFuel.TestBench.Core;


public sealed record ShiftTestPoint(
    uint Rpm,
    double SpeedKmh,
    int GearReceived,
    bool Expected,
    bool Received,
    bool Passed
);


public sealed record ShiftValidationReport(
    IReadOnlyList<ShiftTestPoint> Points,
    bool Passed
);


public static class ShiftValidation
{
    /*
     * =========================================
     * RUN VALIDATION
     * =========================================
     */

    public static async Task<ShiftValidationReport>
        RunAsync(
            ITestBenchSession connection,
            uint initialSequence,
            TimeSpan timeout
        )
    {
        List<ShiftTestPoint>
            results =
                new();


        uint sequence =
            initialSequence;


        const int testGear =
            3;



        /*
         * Current temporary UTFuel
         * configuration:
         *
         * Shift warning = 9000 RPM
         */

        uint[] testRpms =
        {
            8000,
            8500,
            8750,
            8999,
            9000,
            9001,
            9250,
            9500
        };



        foreach (
            uint rpm
            in testRpms
        )
        {
            /*
             * Calculate vehicle speed that
             * corresponds exactly to 3rd gear
             * at the current engine RPM.
             */

            double speed =
                VehicleTestConfiguration
                    .CalculateSpeedKmh(
                        rpm,
                        testGear
                    );



            bool expected =
                rpm >=
                9000;



            InputPacket input =
                new(
                    SequenceId:
                        sequence++,

                    TpsVoltage:
                        3.50,

                    MapVoltage:
                        2.50,

                    CoolantResistance:
                        1200,

                    IntakeResistance:
                        2500,

                    BatteryVoltage:
                        13.80,

                    Rpm:
                        rpm,

                    SpeedKmh:
                        speed
                );



            var response =
                await connection
                    .SendInputAsync(
                        input,
                        timeout
                    );



            bool received =
                response
                    .Packet
                    .ShiftWarning;



            /*
             * The test passes only if:
             *
             * 1. ECU still detects 3rd gear.
             *
             * 2. Shift warning matches the
             *    expected state.
             */

            bool passed =
                response
                    .Packet
                    .Gear ==
                testGear &&
                received ==
                expected;



            results.Add(
                new ShiftTestPoint(
                    Rpm:
                        rpm,

                    SpeedKmh:
                        speed,

                    GearReceived:
                        response
                            .Packet
                            .Gear,

                    Expected:
                        expected,

                    Received:
                        received,

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

        return new ShiftValidationReport(
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