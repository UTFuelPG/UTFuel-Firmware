using System.Diagnostics;

namespace UTFuel.TestBench.Core;


/*
 * =========================================
 * MOCK HARDWARE BENCH SESSION
 * =========================================
 *
 * Development-only simulation of:
 *
 * TestBench
 *    ↓
 * ESP32-S3
 *    ↓
 * Physical signals
 *    ↓
 * STM32 ECU
 *    ↓
 * TestBench
 *
 * No serial port is opened.
 *
 * This exists only so the complete Hardware
 * Bench GUI/session flow can be tested before
 * the physical hardware is available.
 */

public sealed class MockHardwareBenchSession :
    ITestBenchSession,
    IHardwareBenchMetricsProvider
{
    private bool _started;
    private bool _disposed;

    private HardwareBenchMetrics?
    _lastHardwareMetrics;

private uint _sampleId;

private readonly long _startupTimestamp =
    Stopwatch.GetTimestamp();


    /*
     * =========================================
     * CONFIGURATION
     * =========================================
     */

    private const double TireCircumferenceM =
        1.60;

    private const double FinalDriveRatio =
        4.0;

    private const double GearTolerance =
        0.15;


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
     * MODE
     * =========================================
     */

public HardwareBenchMetrics?
    LastHardwareMetrics =>
        _lastHardwareMetrics;

    public ConnectionMode Mode =>
        ConnectionMode.HardwareBench;


    /*
     * =========================================
     * START
     * =========================================
     */

    public async Task StartAsync()
    {
        ThrowIfDisposed();


        if (
            _started
        )
        {
            return;
        }


        /*
         * Small deterministic delay representing
         * opening/initializing the two devices.
         */

        await Task.Delay(
            50
        );


        _started =
            true;
    }


    /*
     * =========================================
     * PING
     * =========================================
     */

    public async Task<double> PingAsync(
    uint sequence,
    TimeSpan timeout
)
{
    ThrowIfDisposed();
    EnsureStarted();


    if (
        timeout <=
        TimeSpan.Zero
    )
    {
        throw new ArgumentOutOfRangeException(
            nameof(timeout)
        );
    }


    long start =
        Stopwatch.GetTimestamp();


    /*
     * Represents:
     *
     * PC -> ESP
     * ESP -> PONG
     */

    await Task.Delay(
        2
    );


    return Stopwatch
        .GetElapsedTime(
            start
        )
        .TotalMilliseconds;
}


    /*
     * =========================================
     * SEND INPUT
     * =========================================
     */

public async Task<(
    OutputPacket Packet,
    double RttMs
)>
    SendInputAsync(
        InputPacket input,
        TimeSpan timeout
    )
{
    ThrowIfDisposed();
    EnsureStarted();

    ArgumentNullException.ThrowIfNull(
        input
    );


    if (
        timeout <=
        TimeSpan.Zero
    )
    {
        throw new ArgumentOutOfRangeException(
            nameof(timeout)
        );
    }


    /*
     * =========================================
     * COMPLETE END-TO-END TIMER
     * =========================================
     */

    long start =
        Stopwatch.GetTimestamp();


    /*
     * =========================================
     * SIMULATOR COMMUNICATION TIMER
     * =========================================
     */

    long simulatorStart =
        Stopwatch.GetTimestamp();


    /*
     * Represents:
     *
     * PC -> ESP32
     * ESP32 applies simulator state
     * ESP32 -> SIM_ACK
     */

    await Task.Delay(
        2
    );


    double simulatorAckRttMs =
        Stopwatch
            .GetElapsedTime(
                simulatorStart
            )
            .TotalMilliseconds;


    /*
     * =========================================
     * PHYSICAL SIGNAL PROPAGATION
     * =========================================
     */

    await Task.Delay(
        1
    );


    /*
     * =========================================
     * ECU PROCESSING
     * =========================================
     */

    double tpsPercent =
        CalculateTpsPercent(
            input.TpsVoltage
        );


    double mapKpa =
        CalculateMapKpa(
            input.MapVoltage
        );


    int gear =
        CalculateGear(
            input.Rpm,
            input.SpeedKmh
        );


    bool shiftWarning =
        gear > 0 &&
        input.Rpm >=
        9000;


    /*
     * Represents:
     *
     * STM32 processing
     * +
     * ECU_DATA transmission to PC
     */

    await Task.Delay(
        2
    );


    /*
     * =========================================
     * OUTPUT PACKET
     * =========================================
     */

    OutputPacket output =
        new OutputPacket(
            SequenceId:
                input.SequenceId,

            Rpm:
                input.Rpm,

            TpsPercent:
                tpsPercent,

            MapKpa:
                mapKpa,

            BatteryVoltage:
                input.BatteryVoltage,

            SpeedKmh:
                input.SpeedKmh,

            Gear:
                gear,

            ShiftWarning:
                shiftWarning
        );


    /*
     * =========================================
     * END-TO-END LATENCY
     * =========================================
     */

    double elapsedMs =
        Stopwatch
            .GetElapsedTime(
                start
            )
            .TotalMilliseconds;


    /*
     * =========================================
     * MOCK ECU SAMPLE
     * =========================================
     */

    uint sampleId =
        ++_sampleId;


    ulong ecuUptimeUs =
        (ulong)(
            Stopwatch
                .GetElapsedTime(
                    _startupTimestamp
                )
                .TotalMilliseconds *
            1000.0
        );


    /*
     * =========================================
     * HARDWARE BENCH METRICS
     * =========================================
     */

    _lastHardwareMetrics =
        new HardwareBenchMetrics(
            SimulatorAckRttMs:
                simulatorAckRttMs,

            EndToEndLatencyMs:
                elapsedMs,

            EcuSampleId:
                sampleId,

            EcuUptimeUs:
                ecuUptimeUs,

            EcuDroppedSamples:
                0,

            SimulatorAppliedAtUs:
                null
        );


    /*
     * The legacy RttMs field temporarily carries
     * the total end-to-end latency.
     */

    return (
        Packet:
            output,

        RttMs:
            elapsedMs
    );
}

    /*
     * =========================================
     * TPS
     * =========================================
     *
     * Current temporary calibration:
     *
     * 0.50 V =   0 %
     * 4.50 V = 100 %
     */

    private static double CalculateTpsPercent(
        double voltage
    )
    {
        return Math.Clamp(
            (
                voltage -
                0.50
            ) /
            4.00 *
            100.0,

            0.0,
            100.0
        );
    }


    /*
     * =========================================
     * MAP
     * =========================================
     *
     * Current temporary calibration:
     *
     * 0.50 V = 20 kPa
     * 4.50 V = 250 kPa
     */

    private static double CalculateMapKpa(
        double voltage
    )
    {
        double normalized =
            Math.Clamp(
                (
                    voltage -
                    0.50
                ) /
                4.00,

                0.0,
                1.0
            );


        return
            20.0 +
            normalized *
            230.0;
    }


    /*
     * =========================================
     * GEAR DETECTION
     * =========================================
     */

    private static int CalculateGear(
        uint rpm,
        double speedKmh
    )
    {
        if (
            rpm <
            500
        )
        {
            return 0;
        }


        if (
            speedKmh <
            2.0
        )
        {
            return 0;
        }


        double speedMs =
            speedKmh /
            3.6;


        double wheelRps =
            speedMs /
            TireCircumferenceM;


        double wheelRpm =
            wheelRps *
            60.0;


        if (
            wheelRpm <=
            0.0
        )
        {
            return 0;
        }


        double measuredRatio =
            rpm /
            (
                wheelRpm *
                FinalDriveRatio
            );


        int bestGear =
            0;

        double bestError =
            double.MaxValue;


        for (
            int i = 0;
            i < GearRatios.Length;
            i++
        )
        {
            double configuredRatio =
                GearRatios[i];


            double relativeError =
                Math.Abs(
                    measuredRatio -
                    configuredRatio
                ) /
                configuredRatio;


            if (
                relativeError <
                bestError
            )
            {
                bestError =
                    relativeError;

                bestGear =
                    i +
                    1;
            }
        }


        if (
            bestError >
            GearTolerance
        )
        {
            return 0;
        }


        return bestGear;
    }


    /*
     * =========================================
     * ENSURE STARTED
     * =========================================
     */

    private void EnsureStarted()
    {
        if (
            !_started
        )
        {
            throw new InvalidOperationException(
                "Mock Hardware Bench session has not been started."
            );
        }
    }


    /*
     * =========================================
     * DISPOSE
     * =========================================
     */

    public ValueTask DisposeAsync()
    {
        if (
            _disposed
        )
        {
            return ValueTask.CompletedTask;
        }


        _started =
            false;

        _disposed =
            true;


        return ValueTask.CompletedTask;
    }


    /*
     * =========================================
     * DISPOSE GUARD
     * =========================================
     */

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this
        );
    }
}