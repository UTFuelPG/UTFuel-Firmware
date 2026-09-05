using UTFuel.TestBench.Core;


Console.WriteLine();
Console.WriteLine(
    "========================================"
);
Console.WriteLine(
    "            UTFuel TestBench"
);
Console.WriteLine(
    "========================================"
);


if (args.Length < 1)
{
    Console.WriteLine();
    Console.WriteLine(
        "Usage:"
    );

    Console.WriteLine(
        "dotnet run -- <path-to-utfuel_host.exe> [ping-count]"
    );

    return;
}


string firmwarePath =
    Path.GetFullPath(
        args[0]
    );


int pingCount = 1000;


if (
    args.Length >= 2 &&
    int.TryParse(
        args[1],
        out int requestedCount
    ) &&
    requestedCount > 0
)
{
    pingCount =
        requestedCount;
}


Console.WriteLine();
Console.WriteLine(
    $"Firmware: {firmwarePath}"
);

Console.WriteLine(
    $"PING samples: {pingCount}"
);

Console.WriteLine();


await using HostFirmwareConnection connection =
    new(firmwarePath);


await connection.StartAsync();


Console.WriteLine(
    "[OK] UTFuel firmware started."
);

Console.WriteLine();


/*
 * =====================================================
 * PING BENCHMARK
 * =====================================================
 */

Console.WriteLine(
    "Running communication benchmark..."
);

Console.WriteLine();


List<double> latencySamples =
    new();


TimeSpan timeout =
    TimeSpan.FromMilliseconds(
        500
    );


for (
    uint sequence = 1;
    sequence <= pingCount;
    sequence++
)
{
    try
    {
        double rtt =
            await connection.PingAsync(
                sequence,
                timeout
            );


        latencySamples.Add(
            rtt
        );
    }
    catch (TimeoutException)
    {
        // Packet counted as lost.
    }


    if (
        sequence % 100 == 0 ||
        sequence == pingCount
    )
    {
        Console.Write(
            $"\rProgress: {sequence}/{pingCount}"
        );
    }
}


Console.WriteLine();
Console.WriteLine();


LatencyReport report =
    LatencyReport.Calculate(
        pingCount,
        latencySamples
    );


Console.WriteLine(
    "========================================"
);

Console.WriteLine(
    "        COMMUNICATION RESULTS"
);

Console.WriteLine(
    "========================================"
);

Console.WriteLine(
    $"Packets sent:       {report.Sent}"
);

Console.WriteLine(
    $"Packets received:   {report.Received}"
);

Console.WriteLine(
    $"Packets lost:       {report.Lost}"
);

Console.WriteLine(
    $"Packet loss:        {report.LossPercent:F4} %"
);

Console.WriteLine();

Console.WriteLine(
    $"Average RTT:        {report.AverageMs:F3} ms"
);

Console.WriteLine(
    $"Minimum RTT:        {report.MinimumMs:F3} ms"
);

Console.WriteLine(
    $"Maximum RTT:        {report.MaximumMs:F3} ms"
);

Console.WriteLine(
    $"P95:                {report.P95Ms:F3} ms"
);

Console.WriteLine(
    $"P99:                {report.P99Ms:F3} ms"
);

Console.WriteLine(
    $"Jitter:             {report.JitterMs:F3} ms"
);


Console.WriteLine();
Console.WriteLine();


/*
 * =====================================================
 * SENSOR VALIDATION
 * =====================================================
 */

Console.WriteLine(
    "========================================"
);

Console.WriteLine(
    "          SENSOR VALIDATION"
);

Console.WriteLine(
    "========================================"
);


uint sensorSequence =
    (uint)pingCount + 1000;


InputPacket testInput =
    new(
        SequenceId:
            sensorSequence,

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
            6000,

        SpeedKmh:
            90.00
    );


try
{
    var result =
        await connection.SendInputAsync(
            testInput,
            timeout
        );


    OutputPacket output =
        result.Packet;


    /*
     * Expected values based on the
     * temporary UTFuel calibration.
     */

    double expectedTps =
        75.0;


    double expectedMap =
        135.0;


    int expectedGear =
        3;


    bool expectedShift =
        false;


    double tpsError =
        Math.Abs(
            output.TpsPercent -
            expectedTps
        );


    double mapError =
        Math.Abs(
            output.MapKpa -
            expectedMap
        );


    Console.WriteLine();

    Console.WriteLine(
        $"Sequence ID:       {output.SequenceId}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"RPM sent:          {testInput.Rpm}"
    );

    Console.WriteLine(
        $"RPM received:      {output.Rpm}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"TPS expected:      {expectedTps:F2} %"
    );

    Console.WriteLine(
        $"TPS received:      {output.TpsPercent:F2} %"
    );

    Console.WriteLine(
        $"TPS error:         {tpsError:F4} %"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"MAP expected:      {expectedMap:F2} kPa"
    );

    Console.WriteLine(
        $"MAP received:      {output.MapKpa:F2} kPa"
    );

    Console.WriteLine(
        $"MAP error:         {mapError:F4} kPa"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Gear expected:     {expectedGear}"
    );

    Console.WriteLine(
        $"Gear received:     {output.Gear}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Shift expected:    {expectedShift}"
    );

    Console.WriteLine(
        $"Shift received:    {output.ShiftWarning}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Round-trip time:   {result.RttMs:F3} ms"
    );
}
catch (TimeoutException)
{
    Console.WriteLine(
        "[FAIL] Sensor packet timed out."
    );
}

Console.WriteLine();

Console.WriteLine(
    "Running TPS sweep..."
);


SensorSweepReport tpsSweep =
    await SensorSweep.RunTpsAsync(
        connection,
        10000,
        timeout
    );


PrintSweepReport(
    tpsSweep
);

Console.WriteLine();

Console.WriteLine(
    "Running MAP sweep..."
);


SensorSweepReport mapSweep =
    await SensorSweep.RunMapAsync(
        connection,
        20000,
        timeout
    );


PrintSweepReport(
    mapSweep
);

Console.WriteLine();
Console.WriteLine(
    "========================================"
);

Console.WriteLine(
    "       UTFuel TestBench finished"
);

Console.WriteLine(
    "========================================"
);

static void PrintGearReport(
    GearValidationReport report
)
{
    Console.WriteLine();

    Console.WriteLine(
        "========================================"
    );

    Console.WriteLine(
        "          GEAR DETECTION TEST"
    );

    Console.WriteLine(
        "========================================"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"{"GEAR",-8}" +
        $"{"RPM",-10}" +
        $"{"SPEED",-15}" +
        $"{"RECEIVED",-12}" +
        "STATUS"
    );

    Console.WriteLine(
        new string('-', 60)
    );


    foreach (
        GearTestPoint point
        in report.Points
    )
    {
        Console.WriteLine(
            $"{point.ExpectedGear,-8}" +
            $"{point.Rpm,-10}" +
            $"{point.SpeedKmh,-15:F2}" +
            $"{point.ReceivedGear,-12}" +
            $"{(point.Passed ? "PASS" : "FAIL")}"
        );
    }


    Console.WriteLine();

    Console.WriteLine(
        report.Passed
            ? "RESULT: GEAR DETECTION PASS"
            : "RESULT: GEAR DETECTION FAIL"
    );
}

Console.WriteLine();
Console.WriteLine(
    "Running gear detection validation..."
);

GearValidationReport gearReport =
    await GearValidation.RunAsync(
        connection,
        30000,
        timeout
    );

PrintGearReport(
    gearReport
);

static void PrintShiftReport(
    ShiftValidationReport report
)
{
Console.WriteLine(
    $"{"RPM",-10}" +
    $"{"SPEED",-12}" +
    $"{"GEAR",-8}" +
    $"{"EXPECTED",-15}" +
    $"{"RECEIVED",-15}" +
    "STATUS"
);

Console.WriteLine(
    new string('-', 75)
);

foreach (ShiftTestPoint point in report.Points)
{
    Console.WriteLine(
        $"{point.Rpm,-10}" +
        $"{point.SpeedKmh,-12:F2}" +
        $"{point.GearReceived,-8}" +
        $"{point.Expected,-15}" +
        $"{point.Received,-15}" +
        $"{(point.Passed ? "PASS" : "FAIL")}"
    );
}


    Console.WriteLine();

    Console.WriteLine(
        report.Passed
            ? "RESULT: SHIFT INDICATOR PASS"
            : "RESULT: SHIFT INDICATOR FAIL"
    );
}

Console.WriteLine();
Console.WriteLine(
    "Running shift indicator validation..."
);

ShiftValidationReport shiftReport =
    await ShiftValidation.RunAsync(
        connection,
        40000,
        timeout
    );

PrintShiftReport(
    shiftReport
);

static void PrintSweepReport(
    SensorSweepReport report
)
{
    Console.WriteLine();

    Console.WriteLine(
        "========================================"
    );

    Console.WriteLine(
        $"          {report.SensorName} SWEEP"
    );

    Console.WriteLine(
        "========================================"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"{"INPUT",-12}" +
        $"{"EXPECTED",-15}" +
        $"{"RECEIVED",-15}" +
        $"{"ERROR",-12}" +
        "STATUS"
    );

    Console.WriteLine(
        new string('-', 65)
    );


    foreach (
        SensorSweepPoint point
        in report.Points
    )
    {
        Console.WriteLine(
            $"{point.Input,7:F2} {report.InputUnit,-4}" +
            $"{point.Expected,10:F2} {report.OutputUnit,-4}" +
            $"{point.Received,10:F2} {report.OutputUnit,-4}" +
            $"{point.AbsoluteError,10:F4} " +
            $"{(point.Passed ? "PASS" : "FAIL")}"
        );
    }


    Console.WriteLine();

    Console.WriteLine(
        $"Average error: {report.AverageError:F5} {report.OutputUnit}"
    );

    Console.WriteLine(
        $"Maximum error: {report.MaximumError:F5} {report.OutputUnit}"
    );

    Console.WriteLine();

    Console.WriteLine(
        report.Passed
            ? $"RESULT: {report.SensorName} PASS"
            : $"RESULT: {report.SensorName} FAIL"
    );
}