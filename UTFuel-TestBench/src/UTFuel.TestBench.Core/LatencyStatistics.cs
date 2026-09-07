namespace UTFuel.TestBench.Core;

public sealed record LatencyReport(
    int Sent,
    int Received,
    int Lost,

    double LossPercent,

    double AverageMs,
    double MinimumMs,
    double MaximumMs,

    double P95Ms,
    double P99Ms,

    double JitterMs
)
{
    public static LatencyReport Calculate(
        int sent,
        IReadOnlyCollection<double> samples
    )
    {
        int received =
            samples.Count;

        int lost =
            sent - received;


        double loss =
            sent == 0
                ? 0
                : lost * 100.0 / sent;


        if (received == 0)
        {
            return new LatencyReport(
                sent,
                0,
                lost,
                loss,
                0,
                0,
                0,
                0,
                0,
                0
            );
        }


        double[] ordered =
            samples
                .OrderBy(x => x)
                .ToArray();


        double average =
            ordered.Average();


        double variance =
            ordered
                .Select(
                    x =>
                        Math.Pow(
                            x - average,
                            2
                        )
                )
                .Average();


        double jitter =
            Math.Sqrt(variance);


        return new LatencyReport(
            sent,
            received,
            lost,
            loss,

            average,

            ordered.First(),
            ordered.Last(),

            Percentile(
                ordered,
                0.95
            ),

            Percentile(
                ordered,
                0.99
            ),

            jitter
        );
    }


    private static double Percentile(
        double[] sorted,
        double percentile
    )
    {
        int index =
            (int)Math.Ceiling(
                percentile *
                sorted.Length
            ) - 1;


        index =
            Math.Clamp(
                index,
                0,
                sorted.Length - 1
            );


        return sorted[index];
    }
}