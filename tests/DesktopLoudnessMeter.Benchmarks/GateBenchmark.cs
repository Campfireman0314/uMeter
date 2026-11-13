using System.Diagnostics;
using System.Globalization;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Isolated long-session scaling scenario for <see cref="IntegratedLoudnessGate"/>: it avoids hours of
/// PCM processing by driving the gate directly with deterministic block powers and querying it after
/// every completed block, exactly as <see cref="PcmMetricAnalyzer"/> does. Small block/hop sizes keep
/// the per-frame bookkeeping negligible so the measured cost is the gate's evaluation work.
/// </summary>
internal static class GateBenchmark
{
    private const int GateBlockFrames = 4;
    private const int GateHopFrames = 4;

    public static void Run(BenchmarkOptions options)
    {
        Console.WriteLine("== Integrated loudness gate scaling ==");
        Console.WriteLine(
            $"Parameters: gateBlockFrames={GateBlockFrames} gateHopFrames={GateHopFrames} " +
            $"blocks={string.Join(", ", options.GateBlockCounts)} " +
            $"warmup={options.WarmupRuns} repeats={options.Repeats} " +
            "workload=1 query per completed block, last-query-snapshot equivalent");
        Console.WriteLine(
            "Pattern: deterministic pseudo-random block powers with repeated values, quiet sections " +
            "below the -70 LUFS absolute gate and loud sections");
        Console.WriteLine();

        foreach (var blockCount in options.GateBlockCounts)
        {
            var scenario = new GateScenario(
                Name: $"gate-{blockCount}blocks",
                BlockCount: blockCount);
            Report(scenario, options);
        }

        Console.WriteLine();
    }

    private static void Report(GateScenario scenario, BenchmarkOptions options)
    {
        // The power sequence is generated outside the measured region so the numbers describe the gate.
        var powers = CreateBlockPowers(scenario.BlockCount);
        for (var warmup = 0; warmup < options.WarmupRuns; warmup++)
        {
            _ = Measure(scenario, powers);
        }

        var runs = new List<GateRunResult>(options.Repeats);
        for (var repeat = 0; repeat < options.Repeats; repeat++)
        {
            var run = Measure(scenario, powers);
            runs.Add(run);
            Console.WriteLine(
                $"  run {repeat + 1:00}: {run.Elapsed.TotalMilliseconds,9:0.0} ms  " +
                $"{run.Elapsed.TotalMilliseconds * 1000 / scenario.BlockCount,8:0.000} us/block  " +
                $"allocated {run.AllocatedBytes,12:N0} B  " +
                $"{run.AllocatedBytes / (double)scenario.BlockCount,8:0.0} B/block  " +
                $"retained {run.RetainedBytes,10:N0} B  " +
                $"evaluations={run.EvaluationCount}  I={Format(run.FinalLufs)}  " +
                $"checksum={run.Checksum:R}");
        }

        var elapsedMedian = Median(runs.Select(run => run.Elapsed.TotalMilliseconds).ToArray());
        var allocatedMedian = Median(runs.Select(run => (double)run.AllocatedBytes).ToArray());
        var retainedMedian = Median(runs.Select(run => (double)run.RetainedBytes).ToArray());
        var identical = runs
            .Select(run => run.Checksum)
            .Distinct()
            .Count() == 1;

        Console.WriteLine(
            $"  {scenario.Name}: median {elapsedMedian:0.0} ms  " +
            $"{elapsedMedian * 1000 / scenario.BlockCount:0.000} us/block  " +
            $"allocated median {allocatedMedian:N0} B " +
            $"({allocatedMedian / scenario.BlockCount:0.0} B/block)  " +
            $"retained median {retainedMedian:N0} B  " +
            $"numeric-stable={identical}");
        Console.WriteLine();
    }

    private static GateRunResult Measure(GateScenario scenario, double[] powers)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var retainedBefore = GC.GetTotalMemory(forceFullCollection: true);
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();

        var gate = new IntegratedLoudnessGate(GateBlockFrames, GateHopFrames);
        var checksum = 0d;
        double? finalLufs = null;
        for (var block = 0; block < powers.Length; block++)
        {
            var power = powers[block];
            for (var frame = 0; frame < GateBlockFrames; frame++)
            {
                gate.Add(power);
            }

            var lufs = gate.CalculateLufs();
            finalLufs = lufs;
            checksum += lufs ?? 0;
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var retainedBytes = Math.Max(0, GC.GetTotalMemory(forceFullCollection: true) - retainedBefore);

        return new GateRunResult(
            stopwatch.Elapsed,
            scenario.BlockCount,
            allocatedBytes,
            retainedBytes,
            gate.EvaluationCount,
            finalLufs,
            checksum);
    }

    /// <summary>
    /// Deterministic pseudo-random block powers. Every fifth block repeats the previous power so
    /// duplicate handling is exercised, and alternating runs of quiet blocks sit below the absolute
    /// gate, which is the case where the relative gate has to be recomputed.
    /// </summary>
    private static double[] CreateBlockPowers(int blockCount)
    {
        var powers = new double[blockCount];
        var state = 0x2545_F491u;
        var last = 0.02;
        for (var block = 0; block < blockCount; block++)
        {
            state = unchecked((state * 1664525u) + 1013904223u);
            var section = (block / 500) % 6;
            var scale = section switch
            {
                0 => 0.05,
                1 => 0.0000001,
                2 => 0.4,
                3 => 0.0000002,
                4 => 0.012,
                _ => 0.25
            };

            if (block % 5 == 0)
            {
                powers[block] = last;
                continue;
            }

            var unit = (state >> 8) / (double)(1 << 24);
            var power = scale * (0.25 + (2.5 * unit));
            // Three-decimal quantization deliberately produces repeated values, and keeps the
            // comparison of equal powers inside the strict-gate path.
            power = Math.Round(power, 6);
            powers[block] = power;
            last = power;
        }

        return powers;
    }

    private static string Format(double? value)
    {
        return value is double measured ? measured.ToString("0.000000", CultureInfo.InvariantCulture) : "null";
    }

    private static double Median(double[] values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2
            : ordered[middle];
    }

    internal sealed record GateScenario(string Name, int BlockCount);

    internal readonly record struct GateRunResult(
        TimeSpan Elapsed,
        int BlockCount,
        long AllocatedBytes,
        long RetainedBytes,
        int EvaluationCount,
        double? FinalLufs,
        double Checksum);
}
