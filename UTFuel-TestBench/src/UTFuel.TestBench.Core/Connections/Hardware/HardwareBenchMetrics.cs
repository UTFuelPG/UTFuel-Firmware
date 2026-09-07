namespace UTFuel.TestBench.Core;


/*
 * =========================================
 * HARDWARE BENCH METRICS
 * =========================================
 *
 * Snapshot of communication and telemetry
 * information collected during a hardware
 * bench transaction.
 * =========================================
 */

public sealed record HardwareBenchMetrics(
    double SimulatorAckRttMs,
    double EndToEndLatencyMs,
    uint EcuSampleId,
    ulong EcuUptimeUs,
    long EcuDroppedSamples,
    ulong? SimulatorAppliedAtUs,

    /*
     * Actual ECU telemetry stream statistics.
     *
     * Defaults preserve compatibility with
     * existing mock/test constructors.
     */

    double EcuRxRateHz = 0.0,
    double EcuRxIntervalMs = 0.0,
    double EcuRxJitterMs = 0.0
);


/*
 * =========================================
 * HARDWARE METRICS PROVIDER
 * =========================================
 */

public interface IHardwareBenchMetricsProvider
{
    HardwareBenchMetrics?
        LastHardwareMetrics
    {
        get;
    }
}