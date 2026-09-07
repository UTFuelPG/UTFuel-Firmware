using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace UTFuel.TestBench.Core;


public sealed class SimulatorSerialConnection :
    IAsyncDisposable
{
    /*
     * =========================================
     * FIELDS
     * =========================================
     */

    private readonly SerialPort _serialPort;

    private readonly ConcurrentDictionary<
        uint,
        TaskCompletionSource<SimulatorResponse>
    > _pendingRequests =
        new();

    private readonly object _writeLock =
        new();

    private CancellationTokenSource?
        _readCancellation;

    private Task?
        _readTask;

    private bool _disposed;


    /*
     * =========================================
     * PROPERTIES
     * =========================================
     */

    public string PortName =>
        _serialPort.PortName;


    public int BaudRate =>
        _serialPort.BaudRate;


    public bool IsOpen =>
        _serialPort.IsOpen;


    /*
     * =========================================
     * CONSTRUCTOR
     * =========================================
     */

    public SimulatorSerialConnection(
        string portName,
        int baudRate = 115200
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                portName
            )
        )
        {
            throw new ArgumentException(
                "Serial port name cannot be empty.",
                nameof(portName)
            );
        }


        if (
            baudRate <=
            0
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(baudRate)
            );
        }


        _serialPort =
            new SerialPort(
                portName,
                baudRate,
                Parity.None,
                8,
                StopBits.One
            )
            {
                Handshake =
                    Handshake.None,

                Encoding =
                    Encoding.ASCII,

                NewLine =
                    "\n",

                ReadTimeout =
                    250,

                WriteTimeout =
                    500,

                DtrEnable =
                    false,

                RtsEnable =
                    false
            };
    }


    /*
     * =========================================
     * START
     * =========================================
     */

    public Task StartAsync()
    {
        ThrowIfDisposed();


        if (
            _serialPort.IsOpen
        )
        {
            return Task.CompletedTask;
        }


        _serialPort.Open();


        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();


        _readCancellation =
            new CancellationTokenSource();


        _readTask =
            Task.Run(
                () =>
                    ReadLoop(
                        _readCancellation.Token
                    )
            );


        return Task.CompletedTask;
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
        string command =
            SimulatorProtocol.BuildPing(
                sequence
            );


        (
            SimulatorResponse Response,
            double RttMs
        )
            result =
                await SendAndWaitAsync(
                    command,
                    sequence,
                    timeout
                );


        if (
            result.Response.Type ==
            SimulatorResponseType.Error
        )
        {
            throw new InvalidDataException(
                $"Simulator returned error: " +
                $"{result.Response.ErrorCode ?? "UNKNOWN"}"
            );
        }


        if (
            result.Response.Type !=
            SimulatorResponseType.Pong
        )
        {
            throw new InvalidDataException(
                "Unexpected simulator response to PING."
            );
        }


        return
            result.RttMs;
    }


    /*
     * =========================================
     * APPLY SIMULATOR STATE
     * =========================================
     */

    public async Task<SimulatorApplyResult>
        ApplyAsync(
            InputPacket input,
            TimeSpan timeout
        )
    {
        ArgumentNullException.ThrowIfNull(
            input
        );


        string command =
            SimulatorProtocol.BuildSet(
                input
            );


        (
            SimulatorResponse Response,
            double RttMs
        )
            result =
                await SendAndWaitAsync(
                    command,
                    input.SequenceId,
                    timeout
                );


        if (
            result.Response.Type ==
            SimulatorResponseType.Error
        )
        {
            throw new InvalidDataException(
                $"Simulator rejected sequence " +
                $"{input.SequenceId}: " +
                $"{result.Response.ErrorCode ?? "UNKNOWN"}"
            );
        }


        if (
            result.Response.Type !=
            SimulatorResponseType.Ack
        )
        {
            throw new InvalidDataException(
                "Unexpected simulator response to SIM_SET."
            );
        }


        return
            new SimulatorApplyResult(
                Sequence:
                    input.SequenceId,

                AckRttMs:
                    result.RttMs,

                AppliedAtUs:
                    result.Response
                        .AppliedAtUs
            );
    }


    /*
     * =========================================
     * SEND + WAIT
     * =========================================
     */

    private async Task<(
        SimulatorResponse Response,
        double RttMs
    )>
        SendAndWaitAsync(
            string command,
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
                nameof(timeout),
                "Timeout must be greater than zero."
            );
        }


        TaskCompletionSource<SimulatorResponse>
            completion =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously
                );


        if (
            !_pendingRequests.TryAdd(
                sequence,
                completion
            )
        )
        {
            throw new InvalidOperationException(
                $"Sequence {sequence} is already pending."
            );
        }


        Stopwatch stopwatch =
            Stopwatch.StartNew();


        try
        {
            /*
             * SerialPort.WriteLine is synchronous.
             *
             * The lock guarantees that two concurrent
             * requests cannot interleave bytes on the
             * serial connection.
             */

            lock (
                _writeLock
            )
            {
                if (
                    !_serialPort.IsOpen
                )
                {
                    throw new InvalidOperationException(
                        "Simulator serial port is not open."
                    );
                }


                _serialPort.WriteLine(
                    command
                );
            }


            SimulatorResponse response;

            try
            {
                response =
                    await completion
                        .Task
                        .WaitAsync(
                            timeout
                        );
            }
            catch (
                TimeoutException
            )
            {
                throw new TimeoutException(
                    $"Simulator timeout waiting for " +
                    $"sequence {sequence}."
                );
            }


            stopwatch.Stop();


            return (
                Response:
                    response,

                RttMs:
                    stopwatch
                        .Elapsed
                        .TotalMilliseconds
            );
        }
        finally
        {
            stopwatch.Stop();


            _pendingRequests.TryRemove(
                sequence,
                out _
            );
        }
    }


    /*
     * =========================================
     * SERIAL READ LOOP
     * =========================================
     */

    private void ReadLoop(
        CancellationToken cancellationToken
    )
    {
        while (
            !cancellationToken
                .IsCancellationRequested
        )
        {
            try
            {
                string line =
                    _serialPort
                        .ReadLine();


                if (
                    !SimulatorProtocol
                        .TryParseResponse(
                            line,
                            out SimulatorResponse response
                        )
                )
                {
                    /*
                     * Ignore malformed / unsupported lines.
                     *
                     * Later we can expose these through
                     * diagnostics/logging.
                     */

                    continue;
                }


                if (
                    _pendingRequests
                        .TryRemove(
                            response.Sequence,
                            out TaskCompletionSource<
                                SimulatorResponse
                            >? completion
                        )
                )
                {
                    completion
                        .TrySetResult(
                            response
                        );
                }
            }
            catch (
    TimeoutException
)
{
    /*
     * Expected because ReadTimeout is short.
     */
}
catch (
    ObjectDisposedException
)
{
    /*
     * SerialPort was disposed during shutdown.
     */

    break;
}
catch (
    InvalidOperationException
)
{
    /*
     * SerialPort was probably closed.
     */

    break;
}
catch (
    IOException ex
)
{
    FailAllPendingRequests(
        ex
    );

    break;
}
catch (
    Exception ex
)
{
    FailAllPendingRequests(
        ex
    );

    break;
}
        }
    }


    /*
     * =========================================
     * FAIL PENDING REQUESTS
     * =========================================
     */

    private void FailAllPendingRequests(
        Exception exception
    )
    {
        foreach (
            KeyValuePair<
                uint,
                TaskCompletionSource<SimulatorResponse>
            > request
            in _pendingRequests
        )
        {
            if (
                _pendingRequests.TryRemove(
                    request.Key,
                    out TaskCompletionSource<
                        SimulatorResponse
                    >? completion
                )
            )
            {
                completion
                    .TrySetException(
                        exception
                    );
            }
        }
    }


    /*
     * =========================================
     * ENSURE STARTED
     * =========================================
     */

    private void EnsureStarted()
    {
        if (
            !_serialPort.IsOpen
        )
        {
            throw new InvalidOperationException(
                "Simulator serial connection has not been started."
            );
        }


        if (
            _readTask ==
            null
        )
        {
            throw new InvalidOperationException(
                "Simulator serial reader is not running."
            );
        }
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


        _disposed =
            true;


        _readCancellation?
            .Cancel();


        /*
         * Closing the port also helps unblock
         * ReadLine immediately.
         */

        if (
            _serialPort.IsOpen
        )
        {
            try
            {
                _serialPort.Close();
            }
            catch
            {
                /*
                 * Ignore serial close errors during
                 * shutdown.
                 */
            }
        }


        if (
            _readTask !=
            null
        )
        {
            try
            {
                await _readTask;
            }
            catch
            {
                /*
                 * Reader failures during shutdown
                 * should not prevent cleanup.
                 */
            }
        }


        FailAllPendingRequests(
            new ObjectDisposedException(
                nameof(
                    SimulatorSerialConnection
                )
            )
        );


        _readCancellation?
            .Dispose();


        _readCancellation =
            null;


        _readTask =
            null;


        _serialPort.Dispose();
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