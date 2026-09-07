using System.Diagnostics;


namespace UTFuel.TestBench.Core;


/*
 * =========================================
 * HARDWARE BENCH SESSION
 * =========================================
 *
 * Physical architecture:
 *
 * TestBench
 *    |
 *    | USB Serial
 *    v
 * ESP32-S3 Simulator
 *    |
 *    | Physical signals
 *    v
 * UTFuel ECU / STM32
 *    |
 *    | USB Serial / ECU_DATA
 *    v
 * TestBench
 *
 *
 * IMPORTANT:
 *
 * SIM_SET sequence IDs are NOT transmitted
 * through the physical sensor signals.
 *
 * Therefore the ECU does not know the
 * simulator sequence ID.
 *
 * The ECU publishes an independent sample_id.
 *
 * This class performs the correlation on
 * the PC side only.
 */


public sealed class HardwareBenchSession :
    ITestBenchSession,
    IHardwareBenchMetricsProvider
{
    /*
     * =========================================
     * CONNECTIONS
     * =========================================
     */

    private readonly SimulatorSerialConnection
        _simulator;


    private readonly EcuSerialConnection
        _ecu;



    /*
     * =========================================
     * STATE
     * =========================================
     */

    private bool
        _started;


    private bool
        _disposed;


    private HardwareBenchMetrics?
        _lastHardwareMetrics;

/*
 * =========================================
 * LIVE ECU RX METRICS
 * =========================================
 */

private void OnEcuTelemetryReceived(
    EcuTelemetryPacket packet
)
{
    EcuRxMetricsUpdated?
        .Invoke(
            _ecu.RxRateHz,
            _ecu.RxIntervalMs,
            _ecu.RxJitterMs,
            _ecu.DroppedSamples,
            packet.SampleId,
            packet.EcuUptimeUs
        );
}


    /*
     * =========================================
     * MODE
     * =========================================
     */

    public ConnectionMode Mode =>
        ConnectionMode.HardwareBench;



    /*
     * =========================================
     * PUBLIC DIAGNOSTICS
     * =========================================
     */

    public string SimulatorPort =>
        _simulator.PortName;


    public int SimulatorBaudRate =>
        _simulator.BaudRate;


    public string EcuPort =>
        _ecu.PortName;


    public int EcuBaudRate =>
        _ecu.BaudRate;


    public bool SimulatorConnected =>
        _simulator.IsOpen;


    public bool EcuConnected =>
        _ecu.IsOpen;


    public long EcuDroppedSamples =>
        _ecu.DroppedSamples;


    public EcuTelemetryPacket?
        LastEcuPacket =>
            _ecu.LastPacket;


    public HardwareBenchMetrics?
        LastHardwareMetrics =>
            _lastHardwareMetrics;

            public event Action<
    double,
    double,
    double,
    long,
    uint,
    ulong
>?
    EcuRxMetricsUpdated;

    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public HardwareBenchSession(
        string simulatorPort,
        int simulatorBaudRate,
        string ecuPort,
        int ecuBaudRate
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                simulatorPort
            )
        )
        {
            throw new ArgumentException(
                "Simulator port cannot be empty.",
                nameof(
                    simulatorPort
                )
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                ecuPort
            )
        )
        {
            throw new ArgumentException(
                "ECU port cannot be empty.",
                nameof(
                    ecuPort
                )
            );
        }


        if (
            string.Equals(
                simulatorPort,
                ecuPort,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new ArgumentException(
                "Simulator and ECU must use different serial ports."
            );
        }


        _simulator =
            new SimulatorSerialConnection(
                simulatorPort,
                simulatorBaudRate
            );


        _ecu =
            new EcuSerialConnection(
                ecuPort,
                ecuBaudRate
            );

            _ecu.TelemetryReceived +=
                OnEcuTelemetryReceived;

    }



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


        try
        {
            /*
             * Start ECU first so telemetry is
             * already being captured before the
             * simulator starts producing stimuli.
             */

            await _ecu
                .StartAsync();


            await _simulator
                .StartAsync();


            _started =
                true;
        }
        catch
        {
            /*
             * If either side fails, close both.
             */

            await DisposeConnectionsAfterFailedStartAsync();


            throw;
        }
    }



    /*
     * =========================================
     * PING
     * =========================================
     *
     * For v0.2, Ping measures only the
     * PC <-> ESP simulator serial link.
     *
     * ECU telemetry health is measured
     * independently by EcuSerialConnection.
     */

    public async Task<double> PingAsync(
        uint sequence,
        TimeSpan timeout
    )
    {
        ThrowIfDisposed();
        EnsureStarted();


        return await _simulator
            .PingAsync(
                sequence,
                timeout
            );
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
                nameof(
                    timeout
                ),
                "Timeout must be greater than zero."
            );
        }



        /*
         * -------------------------------------
         * T0 - HOST MONOTONIC CLOCK
         * -------------------------------------
         *
         * This timestamp is from the PC.
         *
         * The final ECU packet also receives a
         * PC Stopwatch timestamp, allowing us
         * to measure the complete chain using
         * one clock domain.
         */

        long stimulusStartedTimestamp =
            Stopwatch
                .GetTimestamp();



        /*
         * -------------------------------------
         * APPLY PHYSICAL SIMULATOR STATE
         * -------------------------------------
         *
         * This waits for SIM_ACK.
         *
         * The ESP ACK should only be sent after
         * the requested simulator state has been
         * applied.
         */

        SimulatorApplyResult simulatorResult =
            await _simulator
                .ApplyAsync(
                    input,
                    timeout
                );



        /*
         * -------------------------------------
         * ECU BASELINE
         * -------------------------------------
         *
         * Capture the most recent ECU sample ID
         * AFTER SIM_ACK.
         *
         * Then require a strictly newer ECU
         * telemetry sample.
         *
         * This prevents us from returning an old
         * telemetry packet that happened to be in
         * the channel before the simulator state
         * was applied.
         */

        uint baselineSampleId =
            _ecu
                .GetLastSampleId();


        EcuTelemetryPacket ecuPacket =
            await _ecu
                .WaitForSampleAfterAsync(
                    baselineSampleId,
                    timeout
                );



        /*
         * -------------------------------------
         * END-TO-END HOST TIME
         * -------------------------------------
         *
         * stimulusStartedTimestamp and
         * HostReceivedTimestamp belong to the
         * same host Stopwatch clock domain.
         */

        double endToEndMs =
            Stopwatch
                .GetElapsedTime(
                    stimulusStartedTimestamp,
                    ecuPacket
                        .HostReceivedTimestamp
                )
                .TotalMilliseconds;



        /*
         * -------------------------------------
         * HARDWARE BENCH METRICS
         * -------------------------------------
         *
         * This snapshot combines:
         *
         * - ESP acknowledgement RTT
         * - Host-observed end-to-end latency
         * - ECU runtime/sample information
         * - ECU telemetry stream health
         */

        _lastHardwareMetrics =
            new HardwareBenchMetrics(
                SimulatorAckRttMs:
                    simulatorResult
                        .AckRttMs,

                EndToEndLatencyMs:
                    endToEndMs,

                EcuSampleId:
                    ecuPacket
                        .SampleId,

                EcuUptimeUs:
                    ecuPacket
                        .EcuUptimeUs,

                EcuDroppedSamples:
                    _ecu
                        .DroppedSamples,

                SimulatorAppliedAtUs:
                    simulatorResult
                        .AppliedAtUs,

                EcuRxRateHz:
                    _ecu
                        .RxRateHz,

                EcuRxIntervalMs:
                    _ecu
                        .RxIntervalMs,

                EcuRxJitterMs:
                    _ecu
                        .RxJitterMs
            );



        /*
         * -------------------------------------
         * ADAPT ECU TELEMETRY TO CURRENT
         * ITestBenchSession CONTRACT
         * -------------------------------------
         *
         * IMPORTANT:
         *
         * SequenceId below is NOT returned by
         * the physical ECU.
         *
         * It is attached here on the PC only so
         * the existing GUI contract continues
         * working during the v0.2 transition.
         */

        OutputPacket output =
            new OutputPacket(
                SequenceId:
                    input.SequenceId,

                Rpm:
                    ecuPacket.Rpm,

                TpsPercent:
                    ecuPacket
                        .TpsPercent,

                MapKpa:
                    ecuPacket
                        .MapKpa,

                BatteryVoltage:
                    ecuPacket
                        .BatteryVoltage,

                SpeedKmh:
                    ecuPacket
                        .SpeedKmh,

                Gear:
                    ecuPacket.Gear,

                ShiftWarning:
                    ecuPacket
                        .ShiftWarning
            );



        /*
         * For the current interface, RttMs is
         * temporarily used for end-to-end
         * stimulus -> ECU telemetry time.
         *
         * The richer hardware-specific metrics
         * are available through:
         *
         * IHardwareBenchMetricsProvider
         *
         * including:
         *
         * - SimulatorAckRttMs
         * - EndToEndLatencyMs
         * - EcuSampleId
         * - EcuUptimeUs
         * - EcuDroppedSamples
         * - EcuRxRateHz
         * - EcuRxIntervalMs
         * - EcuRxJitterMs
         */

        return (
            Packet:
                output,

            RttMs:
                endToEndMs
        );
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
                "Hardware Bench session has not been started."
            );
        }


        if (
            !_simulator
                .IsOpen
        )
        {
            throw new InvalidOperationException(
                "Simulator serial connection is not open."
            );
        }


        if (
            !_ecu
                .IsOpen
        )
        {
            throw new InvalidOperationException(
                "ECU serial connection is not open."
            );
        }
    }



    /*
     * =========================================
     * FAILED START CLEANUP
     * =========================================
     */

private async Task
    DisposeConnectionsAfterFailedStartAsync()
{
    /*
     * Stop receiving callbacks before
     * destroying the ECU connection.
     */

    _ecu.TelemetryReceived -=
        OnEcuTelemetryReceived;


    try
    {
        await _simulator
            .DisposeAsync();
    }
    catch
    {
        /*
         * Ignore cleanup errors.
         */
    }


    try
    {
        await _ecu
            .DisposeAsync();
    }
    catch
    {
        /*
         * Ignore cleanup errors.
         */
    }


    _started =
        false;
}


    /*
     * =========================================
     * DISPOSE
     * =========================================
     */

    public async ValueTask DisposeAsync()
    {
        if (
            _disposed
        )
        {
            return;
        }

_ecu.TelemetryReceived -=
    OnEcuTelemetryReceived;

        _disposed =
            true;


        _started =
            false;


        Exception?
            simulatorException =
                null;


        Exception?
            ecuException =
                null;


        try
        {
            await _simulator
                .DisposeAsync();
        }
        catch (
            Exception ex
        )
        {
            simulatorException =
                ex;
        }


        try
        {
            await _ecu
                .DisposeAsync();
        }
        catch (
            Exception ex
        )
        {
            ecuException =
                ex;
        }


        if (
            simulatorException !=
            null &&
            ecuException !=
            null
        )
        {
            throw new AggregateException(
                "Errors occurred while closing Hardware Bench connections.",
                simulatorException,
                ecuException
            );
        }


        if (
            simulatorException !=
            null
        )
        {
            throw simulatorException;
        }


        if (
            ecuException !=
            null
        )
        {
            throw ecuException;
        }
    }



    /*
     * =========================================
     * DISPOSE GUARD
     * =========================================
     */

    private void ThrowIfDisposed()
    {
        ObjectDisposedException
            .ThrowIf(
                _disposed,
                this
            );
    }
}