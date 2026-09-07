namespace UTFuel.TestBench.Core;


public sealed record SensorSweepPoint(
    double Input,
    double Expected,
    double Received,
    double AbsoluteError,
    bool Passed
);


public sealed record SensorSweepReport(
    string SensorName,
    string InputUnit,
    string OutputUnit,
    IReadOnlyList<SensorSweepPoint> Points,
    double MaximumError,
    double AverageError,
    bool Passed
);


public static class SensorSweep
{
    /*
     * =========================================
     * TPS SWEEP
     * =========================================
     */

    public static async Task<SensorSweepReport>
        RunTpsAsync(
            ITestBenchSession connection,
            uint initialSequence,
            TimeSpan timeout
        )
    {
        List<SensorSweepPoint>
            points =
                new();


        uint sequence =
            initialSequence;



        /*
         * TPS calibration currently used by
         * firmware:
         *
         * 0.50 V =   0 %
         * 4.50 V = 100 %
         */

        for (
            double voltage = 0.50;
            voltage <= 4.5001;
            voltage += 0.10
        )
        {
            double expected =
                (
                    voltage -
                    0.50
                ) /
                (
                    4.50 -
                    0.50
                ) *
                100.0;



            InputPacket input =
                new(
                    SequenceId:
                        sequence++,

                    TpsVoltage:
                        voltage,

                    MapVoltage:
                        2.50,

                    CoolantResistance:
                        1200,

                    IntakeResistance:
                        2500,

                    BatteryVoltage:
                        13.80,

                    Rpm:
                        3000,

                    SpeedKmh:
                        0.0
                );



            var response =
                await connection
                    .SendInputAsync(
                        input,
                        timeout
                    );



            double received =
                response
                    .Packet
                    .TpsPercent;



            double error =
                Math.Abs(
                    expected -
                    received
                );



            /*
             * HOST tolerance.
             *
             * IMPORTANT:
             * This tolerance is appropriate for
             * local software simulation only.
             *
             * Hardware Bench will eventually
             * require a different tolerance due
             * to DAC/output conditioning, ADC,
             * wiring and electrical noise.
             */

            const double tolerance =
                0.01;



            points.Add(
                new SensorSweepPoint(
                    Input:
                        voltage,

                    Expected:
                        expected,

                    Received:
                        received,

                    AbsoluteError:
                        error,

                    Passed:
                        error <=
                        tolerance
                )
            );
        }



        return BuildReport(
            "TPS",
            "V",
            "%",
            points
        );
    }



    /*
     * =========================================
     * MAP SWEEP
     * =========================================
     */

    public static async Task<SensorSweepReport>
        RunMapAsync(
            ITestBenchSession connection,
            uint initialSequence,
            TimeSpan timeout
        )
    {
        List<SensorSweepPoint>
            points =
                new();


        uint sequence =
            initialSequence;



        /*
         * Current temporary MAP calibration:
         *
         * 0.50 V = 20 kPa
         * 4.50 V = 250 kPa
         */

        for (
            double voltage = 0.50;
            voltage <= 4.5001;
            voltage += 0.10
        )
        {
            double normalized =
                (
                    voltage -
                    0.50
                ) /
                (
                    4.50 -
                    0.50
                );



            double expected =
                20.0 +
                normalized *
                (
                    250.0 -
                    20.0
                );



            InputPacket input =
                new(
                    SequenceId:
                        sequence++,

                    TpsVoltage:
                        2.50,

                    MapVoltage:
                        voltage,

                    CoolantResistance:
                        1200,

                    IntakeResistance:
                        2500,

                    BatteryVoltage:
                        13.80,

                    Rpm:
                        3000,

                    SpeedKmh:
                        0.0
                );



            var response =
                await connection
                    .SendInputAsync(
                        input,
                        timeout
                    );



            double received =
                response
                    .Packet
                    .MapKpa;



            double error =
                Math.Abs(
                    expected -
                    received
                );



            const double tolerance =
                0.05;



            points.Add(
                new SensorSweepPoint(
                    Input:
                        voltage,

                    Expected:
                        expected,

                    Received:
                        received,

                    AbsoluteError:
                        error,

                    Passed:
                        error <=
                        tolerance
                )
            );
        }



        return BuildReport(
            "MAP",
            "V",
            "kPa",
            points
        );
    }



    /*
     * =========================================
     * REPORT
     * =========================================
     */

    private static SensorSweepReport
        BuildReport(
            string sensorName,
            string inputUnit,
            string outputUnit,
            List<SensorSweepPoint> points
        )
    {
        double maximumError =
            points.Max(
                point =>
                    point.AbsoluteError
            );



        double averageError =
            points.Average(
                point =>
                    point.AbsoluteError
            );



        bool passed =
            points.All(
                point =>
                    point.Passed
            );



        return new SensorSweepReport(
            SensorName:
                sensorName,

            InputUnit:
                inputUnit,

            OutputUnit:
                outputUnit,

            Points:
                points,

            MaximumError:
                maximumError,

            AverageError:
                averageError,

            Passed:
                passed
        );
    }
}