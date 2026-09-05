using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using UTFuel.TestBench.Core;


namespace UTFuel.TestBench.Gui.ViewModels;


public partial class MainWindowViewModel :
    ViewModelBase
{
    /*
     * =========================================
     * PRIVATE FIELDS
     * =========================================
     */

    private HostFirmwareConnection?
        _connection;


    private CancellationTokenSource?
        _benchmarkCancellation;


    private Task?
        _benchmarkTask;


    private CancellationTokenSource?
        _manualLiveCancellation;


    private Task?
        _manualLiveTask;


    private uint _manualSequence =
        1000;


    private uint _benchmarkSequence =
        50000;


    private readonly Queue<double>
        _latencySamples =
            new();


    private readonly Queue<DateTime>
        _receiveTimestamps =
            new();


    private const int MaxLatencySamples =
        2000;



    /*
     * =========================================
     * CONNECTION
     * =========================================
     */

    [ObservableProperty]
    private string firmwarePath =
        FindFirmwarePath();


    [ObservableProperty]
    private string connectionStatus =
        "DISCONNECTED";


    [ObservableProperty]
    private bool isConnected;


    [ObservableProperty]
    private string lastMessage =
        "Ready.";



    /*
     * =========================================
     * MANUAL INPUT
     * =========================================
     */

    [ObservableProperty]
    private decimal manualTpsVoltage =
        3.50m;


    [ObservableProperty]
    private decimal manualMapVoltage =
        2.50m;


    [ObservableProperty]
    private decimal manualCoolantResistance =
        1200m;


    [ObservableProperty]
    private decimal manualIntakeResistance =
        2500m;


    [ObservableProperty]
    private decimal manualBatteryVoltage =
        13.80m;


    [ObservableProperty]
    private decimal manualRpm =
        6000m;


    [ObservableProperty]
    private decimal manualSpeedKmh =
        90m;


    [ObservableProperty]
    private decimal manualUpdateRateHz =
        20m;


    [ObservableProperty]
    private bool isManualLiveRunning;



    /*
     * =========================================
     * TARGET / SIMULATOR
     * =========================================
     */

    [ObservableProperty]
    private double targetTpsVoltage;


    [ObservableProperty]
    private double targetMapVoltage;


    [ObservableProperty]
    private double targetTpsPercent;


    [ObservableProperty]
    private double targetMapKpa;


    [ObservableProperty]
    private uint targetRpm;


    [ObservableProperty]
    private double targetSpeedKmh;


    [ObservableProperty]
    private int targetGear;



    /*
     * =========================================
     * ECU RESPONSE
     * =========================================
     */

    [ObservableProperty]
    private uint ecuRpm;


    [ObservableProperty]
    private double ecuTpsPercent;


    [ObservableProperty]
    private double ecuMapKpa;


    [ObservableProperty]
    private double ecuBatteryVoltage;


    [ObservableProperty]
    private double ecuSpeedKmh;


    [ObservableProperty]
    private int ecuGear;


    [ObservableProperty]
    private bool ecuShiftWarning;


    [ObservableProperty]
    private double currentRttMs;



    /*
     * =========================================
     * TARGET vs ECU ERROR
     * =========================================
     */

    [ObservableProperty]
    private int rpmError;


    [ObservableProperty]
    private double tpsErrorPercent;


    [ObservableProperty]
    private double mapErrorKpa;


    [ObservableProperty]
    private double speedErrorKmh;



    /*
     * =========================================
     * COMMUNICATION STATISTICS
     * =========================================
     */

    [ObservableProperty]
    private long packetsSent;


    [ObservableProperty]
    private long packetsReceived;


    [ObservableProperty]
    private long packetsLost;


    [ObservableProperty]
    private double packetLossPercent;


    [ObservableProperty]
    private double averageRttMs;


    [ObservableProperty]
    private double minimumRttMs;


    [ObservableProperty]
    private double maximumRttMs;


    [ObservableProperty]
    private double p95RttMs;


    [ObservableProperty]
    private double p99RttMs;


    [ObservableProperty]
    private double jitterMs;


    [ObservableProperty]
    private double measuredUpdateRateHz;



    /*
     * =========================================
     * DYNAMIC BENCHMARK
     * =========================================
     */

    [ObservableProperty]
    private bool isBenchmarkRunning;


    [ObservableProperty]
    private string benchmarkPhase =
        "Stopped";



    /*
     * =========================================
     * UI STATE
     * =========================================
     */

    public bool CanEditManualInputs =>
        IsConnected &&
        !IsBenchmarkRunning;


    public bool CanSendManual =>
        IsConnected &&
        !IsBenchmarkRunning &&
        !IsManualLiveRunning;


    public bool CanStartManualLive =>
        IsConnected &&
        !IsBenchmarkRunning &&
        !IsManualLiveRunning;


    public bool CanStopManualLive =>
        IsManualLiveRunning;


    public bool CanStartBenchmark =>
        IsConnected &&
        !IsBenchmarkRunning &&
        !IsManualLiveRunning;


    public bool CanStopBenchmark =>
        IsBenchmarkRunning;



    /*
     * =========================================
     * SLIDER PROPERTIES
     * =========================================
     */

    public double ManualTpsVoltageSlider
    {
        get =>
            (double)ManualTpsVoltage;

        set =>
            ManualTpsVoltage =
                (decimal)value;
    }


    public double ManualMapVoltageSlider
    {
        get =>
            (double)ManualMapVoltage;

        set =>
            ManualMapVoltage =
                (decimal)value;
    }


    public double ManualRpmSlider
    {
        get =>
            (double)ManualRpm;

        set =>
            ManualRpm =
                (decimal)value;
    }


    public double ManualSpeedSlider
    {
        get =>
            (double)ManualSpeedKmh;

        set =>
            ManualSpeedKmh =
                (decimal)value;
    }



    /*
     * =========================================
     * PROPERTY CHANGE CALLBACKS
     * =========================================
     */

    partial void OnIsConnectedChanged(
        bool value
    )
    {
        NotifyUiState();
    }


    partial void OnIsBenchmarkRunningChanged(
        bool value
    )
    {
        NotifyUiState();
    }


    partial void OnIsManualLiveRunningChanged(
        bool value
    )
    {
        NotifyUiState();
    }


    partial void OnManualTpsVoltageChanged(
        decimal value
    )
    {
        OnPropertyChanged(
            nameof(
                ManualTpsVoltageSlider
            )
        );
    }


    partial void OnManualMapVoltageChanged(
        decimal value
    )
    {
        OnPropertyChanged(
            nameof(
                ManualMapVoltageSlider
            )
        );
    }


    partial void OnManualRpmChanged(
        decimal value
    )
    {
        OnPropertyChanged(
            nameof(
                ManualRpmSlider
            )
        );
    }


    partial void OnManualSpeedKmhChanged(
        decimal value
    )
    {
        OnPropertyChanged(
            nameof(
                ManualSpeedSlider
            )
        );
    }


    private void NotifyUiState()
    {
        OnPropertyChanged(
            nameof(
                CanEditManualInputs
            )
        );


        OnPropertyChanged(
            nameof(
                CanSendManual
            )
        );


        OnPropertyChanged(
            nameof(
                CanStartManualLive
            )
        );


        OnPropertyChanged(
            nameof(
                CanStopManualLive
            )
        );


        OnPropertyChanged(
            nameof(
                CanStartBenchmark
            )
        );


        OnPropertyChanged(
            nameof(
                CanStopBenchmark
            )
        );
    }



    /*
     * =========================================
     * CONNECT
     * =========================================
     */

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (
            IsConnected
        )
        {
            return;
        }


        try
        {
            ConnectionStatus =
                "CONNECTING...";


            LastMessage =
                "Starting UTFuel firmware...";


            _connection =
                new HostFirmwareConnection(
                    FirmwarePath
                );


            await _connection
                .StartAsync();


            IsConnected =
                true;


            ConnectionStatus =
                "CONNECTED";


            LastMessage =
                "UTFuel firmware connected.";
        }
        catch (Exception ex)
        {
            IsConnected =
                false;


            ConnectionStatus =
                "CONNECTION ERROR";


            LastMessage =
                ex.Message;


            if (
                _connection != null
            )
            {
                await _connection
                    .DisposeAsync();


                _connection =
                    null;
            }
        }
    }



    /*
     * =========================================
     * DISCONNECT
     * =========================================
     */

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await StopBenchmarkInternalAsync();


        await StopManualLiveInternalAsync();


        if (
            _connection != null
        )
        {
            await _connection
                .DisposeAsync();


            _connection =
                null;
        }


        IsConnected =
            false;


        ConnectionStatus =
            "DISCONNECTED";


        LastMessage =
            "Firmware disconnected.";
    }



    /*
     * =========================================
     * MANUAL SINGLE SEND
     * =========================================
     */

    [RelayCommand]
    private async Task SendManualAsync()
    {
        if (
            _connection == null ||
            !CanSendManual
        )
        {
            return;
        }


        try
        {
            InputPacket input =
                BuildManualPacket();


            await SendAndDisplayAsync(
                input,
                targetGear:
                    0
            );


            LastMessage =
                "Manual packet sent successfully.";
        }
        catch (TimeoutException)
        {
            LastMessage =
                "Manual packet timeout.";
        }
        catch (Exception ex)
        {
            LastMessage =
                $"Manual send failed: {ex.Message}";
        }
    }



    /*
     * =========================================
     * MANUAL LIVE START
     * =========================================
     */

    [RelayCommand]
    private async Task StartManualLiveAsync()
    {
        if (
            _connection == null ||
            !CanStartManualLive
        )
        {
            return;
        }


        IsManualLiveRunning =
            true;


        LastMessage =
            "Manual Live Control started.";


        _manualLiveCancellation =
            new CancellationTokenSource();


        _manualLiveTask =
            RunManualLiveAsync(
                _manualLiveCancellation
                    .Token
            );


        try
        {
            await _manualLiveTask;
        }
        catch (
            OperationCanceledException
        )
        {
        }
        catch (
            Exception ex
        )
        {
            LastMessage =
                $"Manual Live error: {ex.Message}";
        }
        finally
        {
            IsManualLiveRunning =
                false;
        }
    }



    /*
     * =========================================
     * MANUAL LIVE STOP
     * =========================================
     */

    [RelayCommand]
    private async Task StopManualLiveAsync()
    {
        await StopManualLiveInternalAsync();
    }


    private async Task StopManualLiveInternalAsync()
    {
        if (
            _manualLiveCancellation ==
            null
        )
        {
            return;
        }


        _manualLiveCancellation
            .Cancel();


        try
        {
            if (
                _manualLiveTask !=
                null
            )
            {
                await _manualLiveTask;
            }
        }
        catch (
            OperationCanceledException
        )
        {
        }


        _manualLiveCancellation
            .Dispose();


        _manualLiveCancellation =
            null;


        _manualLiveTask =
            null;


        IsManualLiveRunning =
            false;


        LastMessage =
            "Manual Live Control stopped.";
    }



    /*
     * =========================================
     * MANUAL LIVE LOOP
     * =========================================
     */

    private async Task RunManualLiveAsync(
        CancellationToken cancellationToken
    )
    {
        while (
            !cancellationToken
                .IsCancellationRequested
        )
        {
            InputPacket input =
                BuildManualPacket();


            try
            {
                await SendAndDisplayAsync(
                    input,
                    targetGear:
                        0
                );
            }
            catch (
                TimeoutException
            )
            {
                LastMessage =
                    "Manual Live packet timeout.";
            }


            int updateRate =
                Math.Clamp(
                    (int)
                    ManualUpdateRateHz,

                    1,
                    100
                );


            int delayMs =
                Math.Max(
                    1,
                    1000 /
                    updateRate
                );


            await Task.Delay(
                delayMs,
                cancellationToken
            );
        }
    }



    /*
     * =========================================
     * BUILD MANUAL PACKET
     * =========================================
     */

    private InputPacket BuildManualPacket()
    {
        return new InputPacket(
            SequenceId:
                _manualSequence++,

            TpsVoltage:
                (double)
                ManualTpsVoltage,

            MapVoltage:
                (double)
                ManualMapVoltage,

            CoolantResistance:
                (double)
                ManualCoolantResistance,

            IntakeResistance:
                (double)
                ManualIntakeResistance,

            BatteryVoltage:
                (double)
                ManualBatteryVoltage,

            Rpm:
                (uint)
                ManualRpm,

            SpeedKmh:
                (double)
                ManualSpeedKmh
        );
    }



    /*
     * =========================================
     * START DYNAMIC BENCHMARK
     * =========================================
     */

    [RelayCommand]
    private async Task StartBenchmarkAsync()
    {
        if (
            _connection == null ||
            !CanStartBenchmark
        )
        {
            return;
        }


        IsBenchmarkRunning =
            true;


        BenchmarkPhase =
            "Starting";


        LastMessage =
            "Dynamic benchmark started.";


        _benchmarkCancellation =
            new CancellationTokenSource();


        _benchmarkTask =
            RunBenchmarkAsync(
                _benchmarkCancellation
                    .Token
            );


        try
        {
            await _benchmarkTask;
        }
        catch (
            OperationCanceledException
        )
        {
        }
        catch (
            Exception ex
        )
        {
            LastMessage =
                $"Benchmark error: {ex.Message}";
        }
        finally
        {
            IsBenchmarkRunning =
                false;


            BenchmarkPhase =
                "Stopped";
        }
    }



    /*
     * =========================================
     * STOP DYNAMIC BENCHMARK
     * =========================================
     */

    [RelayCommand]
    private async Task StopBenchmarkAsync()
    {
        await StopBenchmarkInternalAsync();
    }


    private async Task StopBenchmarkInternalAsync()
    {
        if (
            _benchmarkCancellation ==
            null
        )
        {
            return;
        }


        _benchmarkCancellation
            .Cancel();


        try
        {
            if (
                _benchmarkTask !=
                null
            )
            {
                await _benchmarkTask;
            }
        }
        catch (
            OperationCanceledException
        )
        {
        }


        _benchmarkCancellation
            .Dispose();


        _benchmarkCancellation =
            null;


        _benchmarkTask =
            null;


        IsBenchmarkRunning =
            false;


        BenchmarkPhase =
            "Stopped";


        LastMessage =
            "Dynamic benchmark stopped.";
    }



    /*
     * =========================================
     * DYNAMIC BENCHMARK LOOP
     * =========================================
     */

    private async Task RunBenchmarkAsync(
        CancellationToken cancellationToken
    )
    {
        if (
            _connection == null
        )
        {
            return;
        }


        await foreach (
            DynamicBenchmarkFrame frame
            in DynamicBenchmark.RunAsync(
                intervalMs:
                    50,

                cancellationToken:
                    cancellationToken
            )
        )
        {
            BenchmarkPhase =
                frame.Phase;


            InputPacket input =
                new(
                    SequenceId:
                        _benchmarkSequence++,

                    TpsVoltage:
                        frame.TpsVoltage,

                    MapVoltage:
                        frame.MapVoltage,

                    CoolantResistance:
                        frame.CoolantResistance,

                    IntakeResistance:
                        frame.IntakeResistance,

                    BatteryVoltage:
                        frame.BatteryVoltage,

                    Rpm:
                        frame.Rpm,

                    SpeedKmh:
                        frame.SpeedKmh
                );


            try
            {
                await SendAndDisplayAsync(
                    input,
                    frame.TargetGear
                );
            }
            catch (
                TimeoutException
            )
            {
                LastMessage =
                    "Benchmark packet timeout.";
            }
        }
    }



    /*
     * =========================================
     * SEND TO ECU + UPDATE DASHBOARD
     * =========================================
     */

    private async Task SendAndDisplayAsync(
        InputPacket input,
        int targetGear
    )
    {
        if (
            _connection == null
        )
        {
            return;
        }


        PacketsSent++;


        try
        {
            var response =
                await _connection
                    .SendInputAsync(
                        input,

                        TimeSpan
                            .FromMilliseconds(
                                500
                            )
                    );


            PacketsReceived++;


            CurrentRttMs =
                response.RttMs;


            AddLatencySample(
                response.RttMs
            );


            AddReceiveTimestamp();


            /*
             * =================================
             * TARGET
             * =================================
             */

            TargetTpsVoltage =
                input.TpsVoltage;


            TargetMapVoltage =
                input.MapVoltage;


            TargetRpm =
                input.Rpm;


            TargetSpeedKmh =
                input.SpeedKmh;


            TargetGear =
                targetGear;


            TargetTpsPercent =
                Math.Clamp(
                    (
                        input.TpsVoltage -
                        0.50
                    ) /
                    4.00 *
                    100.0,

                    0.0,
                    100.0
                );


            double normalizedMap =
                Math.Clamp(
                    (
                        input.MapVoltage -
                        0.50
                    ) /
                    4.00,

                    0.0,
                    1.0
                );


            TargetMapKpa =
                20.0 +
                normalizedMap *
                230.0;


            /*
             * =================================
             * ECU RESPONSE
             * =================================
             */

            EcuRpm =
                response.Packet.Rpm;


            EcuTpsPercent =
                response.Packet
                    .TpsPercent;


            EcuMapKpa =
                response.Packet
                    .MapKpa;


            EcuBatteryVoltage =
                response.Packet
                    .BatteryVoltage;


            EcuSpeedKmh =
                response.Packet
                    .SpeedKmh;


            EcuGear =
                response.Packet
                    .Gear;


            EcuShiftWarning =
                response.Packet
                    .ShiftWarning;


            /*
             * =================================
             * ERROR
             * =================================
             */

            RpmError =
                (int)EcuRpm -
                (int)TargetRpm;


            TpsErrorPercent =
                EcuTpsPercent -
                TargetTpsPercent;


            MapErrorKpa =
                EcuMapKpa -
                TargetMapKpa;


            SpeedErrorKmh =
                EcuSpeedKmh -
                TargetSpeedKmh;


            UpdatePacketStatistics();
        }
        catch (
            TimeoutException
        )
        {
            PacketsLost++;


            UpdatePacketStatistics();


            throw;
        }
    }



    /*
     * =========================================
     * LATENCY STATISTICS
     * =========================================
     */

    private void AddLatencySample(
        double latency
    )
    {
        _latencySamples
            .Enqueue(
                latency
            );


        while (
            _latencySamples.Count >
            MaxLatencySamples
        )
        {
            _latencySamples
                .Dequeue();
        }


        if (
            _latencySamples.Count ==
            0
        )
        {
            return;
        }


        double[] ordered =
            _latencySamples
                .OrderBy(
                    x => x
                )
                .ToArray();


        AverageRttMs =
            ordered
                .Average();


        MinimumRttMs =
            ordered
                .First();


        MaximumRttMs =
            ordered
                .Last();


        P95RttMs =
            Percentile(
                ordered,
                0.95
            );


        P99RttMs =
            Percentile(
                ordered,
                0.99
            );


        double average =
            AverageRttMs;


        double variance =
            ordered
                .Select(
                    x =>
                        Math.Pow(
                            x -
                            average,

                            2
                        )
                )
                .Average();


        JitterMs =
            Math.Sqrt(
                variance
            );
    }



    /*
     * =========================================
     * UPDATE RATE
     * =========================================
     */

    private void AddReceiveTimestamp()
    {
        DateTime now =
            DateTime.UtcNow;


        _receiveTimestamps
            .Enqueue(
                now
            );


        while (
            _receiveTimestamps.Count >
            0 &&
            (
                now -
                _receiveTimestamps
                    .Peek()
            ).TotalSeconds >
            1.0
        )
        {
            _receiveTimestamps
                .Dequeue();
        }


        MeasuredUpdateRateHz =
            _receiveTimestamps
                .Count;
    }



    /*
     * =========================================
     * PACKET STATISTICS
     * =========================================
     */

    private void UpdatePacketStatistics()
    {
        PacketLossPercent =
            PacketsSent ==
            0
                ? 0.0
                : PacketsLost *
                  100.0 /
                  PacketsSent;
    }



    /*
     * =========================================
     * PERCENTILE
     * =========================================
     */

    private static double Percentile(
        double[] sorted,
        double percentile
    )
    {
        if (
            sorted.Length ==
            0
        )
        {
            return 0.0;
        }


        int index =
            (int)Math.Ceiling(
                percentile *
                sorted.Length
            ) -
            1;


        index =
            Math.Clamp(
                index,
                0,
                sorted.Length -
                1
            );


        return sorted[
            index
        ];
    }



    /*
     * =========================================
     * RESET STATISTICS
     * =========================================
     */

    [RelayCommand]
    private void ResetStatistics()
    {
        _latencySamples
            .Clear();


        _receiveTimestamps
            .Clear();


        PacketsSent =
            0;


        PacketsReceived =
            0;


        PacketsLost =
            0;


        PacketLossPercent =
            0;


        CurrentRttMs =
            0;


        AverageRttMs =
            0;


        MinimumRttMs =
            0;


        MaximumRttMs =
            0;


        P95RttMs =
            0;


        P99RttMs =
            0;


        JitterMs =
            0;


        MeasuredUpdateRateHz =
            0;


        LastMessage =
            "Communication statistics reset.";
    }



    /*
     * =========================================
     * SHUTDOWN
     * =========================================
     */

    public async Task ShutdownAsync()
    {
        await DisconnectAsync();
    }



    /*
     * =========================================
     * AUTO-DETECT UTFUEL HOST
     * =========================================
     */

    private static string FindFirmwarePath()
    {
        DirectoryInfo? directory =
            new(
                AppContext
                    .BaseDirectory
            );


        while (
            directory != null
        )
        {
            string candidate =
                Path.Combine(
                    directory
                        .FullName,

                    "build",

                    "utfuel_host.exe"
                );


            if (
                File.Exists(
                    candidate
                )
            )
            {
                return candidate;
            }


            directory =
                directory
                    .Parent;
        }


        return string.Empty;
    }
}