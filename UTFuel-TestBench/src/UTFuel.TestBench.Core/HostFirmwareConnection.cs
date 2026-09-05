using System.Collections.Concurrent;
using System.Diagnostics;

namespace UTFuel.TestBench.Core;

public sealed class HostFirmwareConnection : IAsyncDisposable
{
    private sealed record PendingRequest(
        long SentTimestamp,
        TaskCompletionSource<FirmwareResponse> Completion
    );

    private readonly string _firmwarePath;

    private readonly ConcurrentDictionary<uint, PendingRequest>
        _pendingRequests = new();

    private Process? _process;
    private StreamWriter? _stdin;

    private Task? _stdoutReaderTask;
    private Task? _stderrReaderTask;


    public HostFirmwareConnection(string firmwarePath)
    {
        _firmwarePath = firmwarePath;
    }


    public Task StartAsync()
    {
        if (!File.Exists(_firmwarePath))
        {
            throw new FileNotFoundException(
                "UTFuel firmware executable not found.",
                _firmwarePath
            );
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = _firmwarePath,

            WorkingDirectory =
                Path.GetDirectoryName(_firmwarePath)!,

            UseShellExecute = false,

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            CreateNoWindow = true
        };

        _process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException(
                "Could not start UTFuel firmware."
            );
        }

        _stdin = _process.StandardInput;
        _stdin.AutoFlush = true;

        _stdoutReaderTask =
            Task.Run(ReadStdoutLoopAsync);

        _stderrReaderTask =
            Task.Run(ReadStderrLoopAsync);

        return Task.CompletedTask;
    }


    public async Task<double> PingAsync(
        uint sequence,
        TimeSpan timeout
    )
    {
        FirmwareResponse response =
            await SendCommandAsync(
                $"PING,{sequence}",
                sequence,
                timeout
            );

        string expected =
            $"PONG,{sequence}";

        if (response.Line != expected)
        {
            throw new InvalidDataException(
                $"Unexpected response: {response.Line}"
            );
        }

        return response.RttMilliseconds;
    }


    public async Task<(OutputPacket Packet, double RttMs)>
        SendInputAsync(
            InputPacket input,
            TimeSpan timeout
        )
    {
        FirmwareResponse response =
            await SendCommandAsync(
                input.ToProtocolLine(),
                input.SequenceId,
                timeout
            );

        if (!OutputPacket.TryParse(
                response.Line,
                out OutputPacket? packet))
        {
            throw new InvalidDataException(
                $"Invalid OUT packet: {response.Line}"
            );
        }

        return (
            packet!,
            response.RttMilliseconds
        );
    }


    private async Task<FirmwareResponse> SendCommandAsync(
        string command,
        uint sequence,
        TimeSpan timeout
    )
    {
        if (_stdin == null)
        {
            throw new InvalidOperationException(
                "Firmware connection is not started."
            );
        }

        TaskCompletionSource<FirmwareResponse> completion =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        long timestamp =
            Stopwatch.GetTimestamp();

        PendingRequest request =
            new(
                timestamp,
                completion
            );

        if (!_pendingRequests.TryAdd(
                sequence,
                request))
        {
            throw new InvalidOperationException(
                $"Sequence {sequence} is already pending."
            );
        }

        await _stdin.WriteLineAsync(command);

        try
        {
            return await completion
                .Task
                .WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            _pendingRequests.TryRemove(
                sequence,
                out _
            );

            throw;
        }
    }


    private async Task ReadStdoutLoopAsync()
    {
        if (_process == null)
            return;

        while (true)
        {
            string? line =
                await _process
                    .StandardOutput
                    .ReadLineAsync();

            if (line == null)
                break;

            if (!TryGetSequence(
                    line,
                    out uint sequence))
            {
                Console.WriteLine(
                    $"[FIRMWARE STDOUT] {line}"
                );

                continue;
            }

            if (!_pendingRequests.TryRemove(
                    sequence,
                    out PendingRequest? request))
            {
                Console.WriteLine(
                    $"[ORPHAN RESPONSE] {line}"
                );

                continue;
            }

            long receivedTimestamp =
                Stopwatch.GetTimestamp();

            long elapsedTicks =
                receivedTimestamp -
                request.SentTimestamp;

            double elapsedMs =
                elapsedTicks *
                1000.0 /
                Stopwatch.Frequency;

            request.Completion.TrySetResult(
                new FirmwareResponse(
                    line,
                    elapsedMs
                )
            );
        }
    }


    private async Task ReadStderrLoopAsync()
    {
        if (_process == null)
            return;

        while (true)
        {
            string? line =
                await _process
                    .StandardError
                    .ReadLineAsync();

            if (line == null)
                break;

            Console.Error.WriteLine(
                $"[FIRMWARE] {line}"
            );
        }
    }


    private static bool TryGetSequence(
        string line,
        out uint sequence
    )
    {
        sequence = 0;

        string[] parts =
            line.Split(',');

        if (parts.Length < 2)
            return false;

        if (
            parts[0] != "PONG" &&
            parts[0] != "OUT" &&
            parts[0] != "ERR"
        )
        {
            return false;
        }

        return uint.TryParse(
            parts[1],
            out sequence
        );
    }


    public async ValueTask DisposeAsync()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(
                        entireProcessTree: true
                    );
                }
            }
            catch
            {
                // Ignore shutdown errors.
            }

            if (_stdoutReaderTask != null)
            {
                await _stdoutReaderTask;
            }

            if (_stderrReaderTask != null)
            {
                await _stderrReaderTask;
            }

            _process.Dispose();
        }

        _stdin?.Dispose();
    }
}