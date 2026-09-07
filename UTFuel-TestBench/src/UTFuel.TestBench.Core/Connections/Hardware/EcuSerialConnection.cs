using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading.Channels;


namespace UTFuel.TestBench.Core;


public sealed class EcuSerialConnection :
    IAsyncDisposable
{
    /*
     * =========================================
     * CONFIGURATION
     * =========================================
     */

    private readonly string
        _portName;


    private readonly int
        _baudRate;



    /*
     * =========================================
     * SERIAL
     * =========================================
     */

    private SerialPort?
        _serialPort;


    private CancellationTokenSource?
        _readerCancellation;


    private Task?
        _readerTask;



    /*
     * =========================================
     * TELEMETRY BUFFER
     * =========================================
     */

    private readonly Channel<EcuTelemetryPacket>
        _telemetryChannel =
            Channel.CreateBounded<EcuTelemetryPacket>(
                new BoundedChannelOptions(
                    256
                )
                {
                    FullMode =
                        BoundedChannelFullMode
                            .DropOldest,

                    SingleWriter =
                        true,

                    SingleReader =
                        false
                }
            );



    /*
     * =========================================
     * STATE
     * =========================================
     */

    private readonly object
        _stateLock =
            new();


    private EcuTelemetryPacket?
        _lastPacket;


    private uint?
        _previousSampleId;


    private long
        _droppedSamples;



    /*
     * =========================================
     * ECU RX STATISTICS
     * =========================================
     */

    private const int MaxRxIntervalSamples =
        200;


    private readonly Queue<double>
        _rxIntervalsMs =
            new();


    private long?
        _lastRxTimestamp;


    private double
        _rxRateHz;


    private double
        _rxIntervalMs;


    private double
        _rxJitterMs;



    /*
     * =========================================
     * EVENTS
     * =========================================
     */

    public event Action<EcuTelemetryPacket>?
        TelemetryReceived;



    /*
     * =========================================
     * PUBLIC STATE
     * =========================================
     */

    public string PortName =>
        _portName;


    public int BaudRate =>
        _baudRate;


    public bool IsOpen =>
        _serialPort?
            .IsOpen ==
        true;


    public long DroppedSamples =>
        Interlocked.Read(
            ref _droppedSamples
        );


    public double RxRateHz
    {
        get
        {
            lock (
                _stateLock
            )
            {
                return
                    _rxRateHz;
            }
        }
    }


    public double RxIntervalMs
    {
        get
        {
            lock (
                _stateLock
            )
            {
                return
                    _rxIntervalMs;
            }
        }
    }


    public double RxJitterMs
    {
        get
        {
            lock (
                _stateLock
            )
            {
                return
                    _rxJitterMs;
            }
        }
    }


    public EcuTelemetryPacket?
        LastPacket
    {
        get
        {
            lock (
                _stateLock
            )
            {
                return
                    _lastPacket;
            }
        }
    }



    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public EcuSerialConnection(
        string portName,
        int baudRate =
            115200
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                portName
            )
        )
        {
            throw new ArgumentException(
                "ECU serial port name is required.",
                nameof(
                    portName
                )
            );
        }


        if (
            baudRate <=
            0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    baudRate
                )
            );
        }


        _portName =
            portName;


        _baudRate =
            baudRate;
    }



    /*
     * =========================================
     * START
     * =========================================
     */

    public Task StartAsync()
    {
        if (
            IsOpen
        )
        {
            return Task.CompletedTask;
        }


        SerialPort port =
            new(
                _portName,
                _baudRate,
                Parity.None,
                8,
                StopBits.One
            )
            {
                Encoding =
                    Encoding.ASCII,

                NewLine =
                    "\n",

                ReadTimeout =
                    250,

                DtrEnable =
                    true
            };


        try
        {
            port.Open();
        }
        catch
        {
            port.Dispose();

            throw;
        }


        /*
         * Reset telemetry state whenever a new
         * physical connection is started.
         */

        lock (
            _stateLock
        )
        {
            _lastPacket =
                null;


            _previousSampleId =
                null;


            _rxIntervalsMs
                .Clear();


            _lastRxTimestamp =
                null;


            _rxRateHz =
                0.0;


            _rxIntervalMs =
                0.0;


            _rxJitterMs =
                0.0;
        }


        Interlocked.Exchange(
            ref _droppedSamples,
            0
        );


        _serialPort =
            port;


        _readerCancellation =
            new CancellationTokenSource();


        _readerTask =
            Task.Run(
                () =>
                    ReadLoop(
                        _readerCancellation
                            .Token
                    )
            );


        return Task.CompletedTask;
    }



    /*
     * =========================================
     * CURRENT SAMPLE
     * =========================================
     */

    public uint GetLastSampleId()
    {
        lock (
            _stateLock
        )
        {
            return
                _lastPacket?
                    .SampleId ??
                0;
        }
    }



    /*
     * =========================================
     * WAIT FOR FRESH ECU SAMPLE
     * =========================================
     */

    public async Task<EcuTelemetryPacket>
        WaitForSampleAfterAsync(
            uint baselineSampleId,
            TimeSpan timeout,
            CancellationToken cancellationToken =
                default
        )
    {
        using CancellationTokenSource
            timeoutCancellation =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken
                    );


        timeoutCancellation
            .CancelAfter(
                timeout
            );


        try
        {
            while (
                true
            )
            {
                EcuTelemetryPacket packet =
                    await _telemetryChannel
                        .Reader
                        .ReadAsync(
                            timeoutCancellation
                                .Token
                        );


                if (
                    IsSampleNewer(
                        packet.SampleId,
                        baselineSampleId
                    )
                )
                {
                    return
                        packet;
                }
            }
        }
        catch (
            OperationCanceledException
        )
        {
            if (
                cancellationToken
                    .IsCancellationRequested
            )
            {
                throw;
            }


            throw new TimeoutException(
                "Timed out waiting for fresh ECU telemetry."
            );
        }
    }



    /*
     * =========================================
     * SERIAL READER
     * =========================================
     */

    private void ReadLoop(
        CancellationToken cancellationToken
    )
    {
        SerialPort?
            port =
                _serialPort;


        if (
            port ==
            null
        )
        {
            return;
        }


        while (
            !cancellationToken
                .IsCancellationRequested
        )
        {
            string?
                line;


            try
            {
                line =
                    port.ReadLine();
            }
            catch (
                TimeoutException
            )
            {
                continue;
            }
            catch (
                InvalidOperationException
            )
            {
                break;
            }
            catch (
                IOException
            )
            {
                break;
            }


            if (
                string.IsNullOrWhiteSpace(
                    line
                )
            )
            {
                continue;
            }


            if (
                !EcuTelemetryPacket
                    .TryParse(
                        line,
                        out EcuTelemetryPacket?
                            packet
                    )
            )
            {
                Console.WriteLine(
                    $"[ECU SERIAL] {line}"
                );


                continue;
            }


            if (
                packet ==
                null
            )
            {
                continue;
            }


            /*
             * Timestamp the packet as close as
             * possible to the physical receive.
             *
             * Stopwatch is monotonic and therefore
             * suitable for interval measurement.
             */

            packet =
                packet with
                {
                    HostReceivedTimestamp =
                        Stopwatch
                            .GetTimestamp()
                };


            ProcessPacket(
                packet
            );
        }
    }



    /*
     * =========================================
     * PROCESS PACKET
     * =========================================
     */

    private void ProcessPacket(
        EcuTelemetryPacket packet
    )
    {
        /*
         * Use the timestamp generated inside
         * ReadLoop instead of generating another
         * timestamp after parsing.
         */

        long hostRxTimestamp =
            packet
                .HostReceivedTimestamp;


        lock (
            _stateLock
        )
        {
            /*
             * RX statistics are updated while the
             * same state lock is already held.
             */

            UpdateRxStatisticsUnsafe(
                hostRxTimestamp
            );


            /*
             * =================================
             * SAMPLE LOSS DETECTION
             * =================================
             */

            if (
                _previousSampleId
                    .HasValue
            )
            {
                uint difference =
                    unchecked(
                        packet.SampleId -
                        _previousSampleId.Value
                    );


                /*
                 * Difference below half of the
                 * uint range means legitimate
                 * forward movement, including
                 * uint rollover.
                 */

                if (
                    difference >
                    1 &&
                    difference <
                    0x80000000
                )
                {
                    Interlocked.Add(
                        ref _droppedSamples,
                        difference -
                        1
                    );
                }
            }


            _previousSampleId =
                packet.SampleId;


            _lastPacket =
                packet;
        }


        /*
         * Feed consumers waiting for a fresh
         * telemetry sample.
         */

        _telemetryChannel
            .Writer
            .TryWrite(
                packet
            );


        /*
         * Notify passive telemetry observers.
         */

        TelemetryReceived?
            .Invoke(
                packet
            );
    }



    /*
     * =========================================
     * ECU RX STATISTICS
     * =========================================
     *
     * IMPORTANT:
     *
     * This method is always called while
     * _stateLock is already held.
     *
     * It measures the interval between actual
     * ECU_DATA frames received by the PC.
     * =========================================
     */

    private void UpdateRxStatisticsUnsafe(
        long hostRxTimestamp
    )
    {
        if (
            _lastRxTimestamp
                .HasValue
        )
        {
            long elapsedTicks =
                hostRxTimestamp -
                _lastRxTimestamp
                    .Value;


            double intervalMs =
                elapsedTicks *
                1000.0 /
                Stopwatch
                    .Frequency;


            /*
             * Ignore impossible intervals and
             * very long pauses caused by things
             * such as disconnect/reconnect.
             */

            if (
                intervalMs >
                0.0 &&
                intervalMs <
                5000.0
            )
            {
                /*
                 * Current interval.
                 */

                _rxIntervalMs =
                    intervalMs;


                /*
                 * Rolling interval history.
                 */

                _rxIntervalsMs
                    .Enqueue(
                        intervalMs
                    );


                while (
                    _rxIntervalsMs.Count >
                    MaxRxIntervalSamples
                )
                {
                    _rxIntervalsMs
                        .Dequeue();
                }


                /*
                 * =================================
                 * AVERAGE INTERVAL
                 * =================================
                 */

                double sum =
                    0.0;


                foreach (
                    double sample
                    in _rxIntervalsMs
                )
                {
                    sum +=
                        sample;
                }


                double averageIntervalMs =
                    _rxIntervalsMs.Count >
                    0
                        ? sum /
                          _rxIntervalsMs.Count
                        : 0.0;


                /*
                 * =================================
                 * RX RATE
                 * =================================
                 */

                _rxRateHz =
                    averageIntervalMs >
                    0.0
                        ? 1000.0 /
                          averageIntervalMs
                        : 0.0;


                /*
                 * =================================
                 * RX JITTER
                 * =================================
                 *
                 * Population standard deviation of
                 * the packet arrival intervals.
                 * =================================
                 */

                double squaredDifferenceSum =
                    0.0;


                foreach (
                    double sample
                    in _rxIntervalsMs
                )
                {
                    double difference =
                        sample -
                        averageIntervalMs;


                    squaredDifferenceSum +=
                        difference *
                        difference;
                }


                double variance =
                    _rxIntervalsMs.Count >
                    0
                        ? squaredDifferenceSum /
                          _rxIntervalsMs.Count
                        : 0.0;


                _rxJitterMs =
                    Math.Sqrt(
                        variance
                    );
            }
        }


        /*
         * Always update the reference timestamp.
         */

        _lastRxTimestamp =
            hostRxTimestamp;
    }



    /*
     * =========================================
     * SAMPLE ORDER
     * =========================================
     */

    private static bool IsSampleNewer(
        uint candidate,
        uint baseline
    )
    {
        uint difference =
            unchecked(
                candidate -
                baseline
            );


        return
            difference !=
            0 &&
            difference <
            0x80000000;
    }



    /*
     * =========================================
     * DISPOSE
     * =========================================
     */

    public async ValueTask DisposeAsync()
    {
        _readerCancellation?
            .Cancel();


        if (
            _serialPort !=
            null
        )
        {
            try
            {
                if (
                    _serialPort.IsOpen
                )
                {
                    _serialPort.Close();
                }
            }
            catch
            {
            }
        }


        if (
            _readerTask !=
            null
        )
        {
            try
            {
                await _readerTask;
            }
            catch
            {
            }
        }


        _telemetryChannel
            .Writer
            .TryComplete();


        _readerCancellation?
            .Dispose();


        _readerCancellation =
            null;


        _readerTask =
            null;


        _serialPort?
            .Dispose();


        _serialPort =
            null;
    }
}