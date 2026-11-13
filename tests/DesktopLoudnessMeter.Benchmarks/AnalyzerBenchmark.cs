using System.Diagnostics;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Full <see cref="PcmMetricAnalyzer"/> scenario: identical signal, block size, duration, mode and
/// true-peak oversampling are fed for every run, and the same measurement is repeated so the
/// reported numbers can be reproduced. Timing is evidence, never an assertion.
/// </summary>
internal static class AnalyzerBenchmark
{
    public static void Run(BenchmarkOptions options)
    {
        Console.WriteLine("== Full analyzer ==");
        Console.WriteLine(
            $"Parameters: sampleRate={options.SampleRate} channels={options.Channels} " +
            $"blockFrames={options.BlockFrames} oversampling={options.Oversampling}x " +
            $"warmup={options.WarmupRuns} repeats={options.Repeats} " +
            $"channelWaveform=false channelSpectrum=false");
        Console.WriteLine(
            $"Durations: {string.Join(", ", options.DurationsSeconds.Select(seconds => $"{seconds:0.###}s"))}" +
            $"  modes: {string.Join(", ", options.Modes)}");
        Console.WriteLine();

        var longestFrames = (int)Math.Round(options.DurationsSeconds.Max() * options.SampleRate);
        var signal = BenchmarkSignal.CreateStereoSignal(longestFrames, options.SampleRate, options.Channels);

        foreach (var seconds in options.DurationsSeconds)
        {
            var frameCount = (int)Math.Round(seconds * options.SampleRate);
            foreach (var mode in options.Modes)
            {
                var scenario = new AnalyzerScenario(
                    Name: $"stereo{options.SampleRate / 1000}k-{options.BlockFrames}block-{seconds:0.###}s-{mode}",
                    SampleRate: options.SampleRate,
                    Channels: options.Channels,
                    BlockFrames: options.BlockFrames,
                    FrameCount: frameCount,
                    Mode: mode,
                    Oversampling: options.Oversampling);

                Report(scenario, signal, options);
            }
        }

        Console.WriteLine();
    }

    private static void Report(AnalyzerScenario scenario, float[] signal, BenchmarkOptions options)
    {
        for (var warmup = 0; warmup < options.WarmupRuns; warmup++)
        {
            _ = Measure(scenario, signal);
        }

        var runs = new List<AnalyzerRunResult>(options.Repeats);
        var blocks = (int)Math.Ceiling(scenario.FrameCount / (double)scenario.BlockFrames);
        for (var repeat = 0; repeat < options.Repeats; repeat++)
        {
            var run = Measure(scenario, signal);
            runs.Add(run);
            Console.WriteLine(
                $"  run {repeat + 1:00}: {run.Elapsed.TotalMilliseconds,9:0.0} ms  " +
                $"{(run.Frames / run.Elapsed.TotalSeconds) / 1_000_000d,8:0.000} Mframes/s  " +
                $"allocated {run.AllocatedBytes,12:N0} B  {run.AllocatedBytes / (double)run.Frames,7:0.000} B/frame  " +
                $"{run.AllocatedBytes / (double)blocks,9:0.0} B/block  " +
                $"M={Format(run.Snapshot.MomentaryLufs)} S={Format(run.Snapshot.ShortTermLufs)} " +
                $"I={Format(run.Snapshot.IntegratedLufs)} TP={Format(run.Snapshot.TruePeakDb)} " +
                $"rms100={Format(run.Snapshot.Rms100MsDb)} corr={Format(run.Snapshot.Correlation)} " +
                $"points={run.Snapshot.StereoImagePoints?.Count.ToString() ?? "null"}");
        }

        var elapsedMedian = Median(runs.Select(run => run.Elapsed.TotalMilliseconds).ToArray());
        var elapsedMinimum = runs.Min(run => run.Elapsed.TotalMilliseconds);
        var allocatedMedian = Median(runs.Select(run => (double)run.AllocatedBytes).ToArray());
        var throughputMedian = Median(
            runs.Select(run => (run.Frames / run.Elapsed.TotalSeconds) / 1_000_000d).ToArray());
        var throughputBest = runs.Max(run => (run.Frames / run.Elapsed.TotalSeconds) / 1_000_000d);
        var identical = runs
            .Select(run => run.Signature)
            .Distinct(StringComparer.Ordinal)
            .Count() == 1;

        Console.WriteLine(
            $"  {scenario.Name}: median {elapsedMedian:0.0} ms  best {elapsedMinimum:0.0} ms  " +
            $"median {throughputMedian:0.000} Mframes/s  best {throughputBest:0.000} Mframes/s  " +
            $"allocated median {allocatedMedian:N0} B ({allocatedMedian / scenario.FrameCount:0.000} B/frame, " +
            $"{allocatedMedian / blocks:0.0} B/block over {blocks} blocks)  numeric-stable={identical}");
        Console.WriteLine();
    }

    private static AnalyzerRunResult Measure(AnalyzerScenario scenario, float[] signal)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var snapshot = Execute(scenario, signal);
        stopwatch.Stop();
        var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

        return new AnalyzerRunResult(stopwatch.Elapsed, scenario.FrameCount, allocatedBytes, snapshot);
    }

    /// <summary>
    /// Mirrors the capture path: interleaved blocks are pushed through <see cref="PcmMetricAnalyzer.Process"/>
    /// and the latest snapshot is read after every block, exactly like a consumer polling the analyzer.
    /// </summary>
    private static PcmMetricsSnapshot Execute(AnalyzerScenario scenario, float[] signal)
    {
        using var analyzer = new PcmMetricAnalyzer(
            new PcmMetricAnalyzerOptions(scenario.SampleRate, scenario.Channels, scenario.Oversampling));
        analyzer.SetPerformanceMode(scenario.Mode);

        var snapshot = analyzer.Snapshot;
        for (var frameOffset = 0; frameOffset < scenario.FrameCount; frameOffset += scenario.BlockFrames)
        {
            var framesThisBlock = Math.Min(scenario.BlockFrames, scenario.FrameCount - frameOffset);
            var sampleOffset = frameOffset * scenario.Channels;
            analyzer.Process(signal.AsSpan(sampleOffset, framesThisBlock * scenario.Channels));
            snapshot = analyzer.Snapshot;
        }

        return snapshot;
    }

    private static string Format(double? value)
    {
        return value is double measured ? measured.ToString("0.0000") : "null";
    }

    private static double Median(double[] values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2
            : ordered[middle];
    }

    internal sealed record AnalyzerScenario(
        string Name,
        int SampleRate,
        int Channels,
        int BlockFrames,
        int FrameCount,
        PcmAnalysisPerformanceMode Mode,
        int Oversampling);

    internal readonly record struct AnalyzerRunResult(
        TimeSpan Elapsed,
        int Frames,
        long AllocatedBytes,
        PcmMetricsSnapshot Snapshot)
    {
        public string Signature => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Snapshot.MomentaryLufs:R}|{Snapshot.ShortTermLufs:R}|{Snapshot.IntegratedLufs:R}|" +
            $"{Snapshot.TruePeakDb:R}|{Snapshot.Rms100MsDb:R}|{Snapshot.Correlation:R}|" +
            $"{Snapshot.StereoImageX:R}|{Snapshot.StereoImageY:R}");
    }
}
