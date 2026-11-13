using System.Diagnostics;
using System.Globalization;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Full <see cref="PcmMetricAnalyzer"/> scenario: identical signal, block size, duration, mode,
/// true-peak oversampling, source count and channel display flags are fed for every run, and the same
/// measurement is repeated so the reported numbers can be reproduced. Timing is evidence, never an
/// assertion.
/// </summary>
internal static class AnalyzerBenchmark
{
    public static void Run(BenchmarkOptions options)
    {
        Console.WriteLine("== Full analyzer ==");
        Console.WriteLine(
            $"Parameters: sampleRate={options.SampleRate} channels={options.Channels} " +
            $"blockFrames={options.BlockFrames} oversampling={options.Oversampling}x " +
            $"sources={options.Sources} channelWaveform={options.ChannelWaveform} " +
            $"channelSpectrum={options.ChannelSpectrum} " +
            $"warmup={options.WarmupRuns} repeats={options.Repeats}");
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
                    Name: $"stereo{options.SampleRate / 1000}k-{options.BlockFrames}block-{seconds:0.###}s-{mode}" +
                        $"{(options.Sources > 1 ? $"-{options.Sources}src" : string.Empty)}" +
                        $"{(options.ChannelWaveform || options.ChannelSpectrum ? "-chan" : string.Empty)}",
                    SampleRate: options.SampleRate,
                    Channels: options.Channels,
                    BlockFrames: options.BlockFrames,
                    FrameCount: frameCount,
                    Mode: mode,
                    Oversampling: options.Oversampling,
                    Sources: options.Sources,
                    ChannelWaveform: options.ChannelWaveform,
                    ChannelSpectrum: options.ChannelSpectrum);

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
        var analyzedFrames = (long)scenario.FrameCount * scenario.Sources;
        for (var repeat = 0; repeat < options.Repeats; repeat++)
        {
            var run = Measure(scenario, signal);
            runs.Add(run);
            Console.WriteLine(
                $"  run {repeat + 1:00}: {run.Elapsed.TotalMilliseconds,9:0.0} ms  " +
                $"{(analyzedFrames / run.Elapsed.TotalSeconds) / 1_000_000d,8:0.000} Mframes/s  " +
                $"allocated {run.AllocatedBytes,12:N0} B  {run.AllocatedBytes / (double)analyzedFrames,7:0.000} B/frame  " +
                $"{run.AllocatedBytes / (double)(blocks * scenario.Sources),9:0.0} B/analyzer-block  " +
                $"source0: M={Format(run.LastSnapshots[0].MomentaryLufs)} " +
                $"S={Format(run.LastSnapshots[0].ShortTermLufs)} " +
                $"I={Format(run.LastSnapshots[0].IntegratedLufs)} " +
                $"TP={Format(run.LastSnapshots[0].TruePeakDb)} " +
                $"rms100={Format(run.LastSnapshots[0].Rms100MsDb)} " +
                $"corr={Format(run.LastSnapshots[0].Correlation)}");
        }

        var elapsedMedian = Median(runs.Select(run => run.Elapsed.TotalMilliseconds).ToArray());
        var elapsedMinimum = runs.Min(run => run.Elapsed.TotalMilliseconds);
        var allocatedMedian = Median(runs.Select(run => (double)run.AllocatedBytes).ToArray());
        var throughputMedian = Median(
            runs.Select(run => (analyzedFrames / run.Elapsed.TotalSeconds) / 1_000_000d).ToArray());
        var throughputBest = runs.Max(run => (analyzedFrames / run.Elapsed.TotalSeconds) / 1_000_000d);
        var identical = runs
            .Select(run => run.Signature)
            .Distinct(StringComparer.Ordinal)
            .Count() == 1;

        Console.WriteLine(
            $"  {scenario.Name}: sources={scenario.Sources} " +
            $"channelWaveform={scenario.ChannelWaveform} channelSpectrum={scenario.ChannelSpectrum}  " +
            $"median {elapsedMedian:0.0} ms  best {elapsedMinimum:0.0} ms  " +
            $"median {throughputMedian:0.000} Mframes/s  best {throughputBest:0.000} Mframes/s  " +
            $"allocated median {allocatedMedian:N0} B " +
            $"({allocatedMedian / analyzedFrames:0.000} B/frame, " +
            $"{allocatedMedian / (blocks * scenario.Sources):0.0} B/analyzer-block over " +
            $"{blocks} blocks x {scenario.Sources} source(s))  numeric-stable={identical}");
        Console.WriteLine($"  {scenario.Name}: aggregate signature {runs[0].AggregateSignature}");
        for (var source = 0; source < scenario.Sources; source++)
        {
            Console.WriteLine($"  {scenario.Name}: source{source} signature {runs[0].SourceSignatures[source]}");
        }

        Console.WriteLine();
    }

    private static AnalyzerRunResult Measure(AnalyzerScenario scenario, float[] signal)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var snapshots = Execute(scenario, signal);
        stopwatch.Stop();
        var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

        return new AnalyzerRunResult(stopwatch.Elapsed, scenario.FrameCount * scenario.Sources, allocatedBytes, snapshots);
    }

    /// <summary>
    /// Mirrors the capture path: interleaved blocks are pushed through <see cref="PcmMetricAnalyzer.Process"/>
    /// for every source and the latest snapshot of each analyzer is read after every block, exactly like a
    /// consumer polling the analyzers. All sources receive the same PCM, so the workload per source is
    /// identical and the aggregate numbers cover <see cref="AnalyzerScenario.Sources"/> analyzers.
    /// </summary>
    private static PcmMetricsSnapshot[] Execute(AnalyzerScenario scenario, float[] signal)
    {
        var analyzers = new PcmMetricAnalyzer[scenario.Sources];
        var snapshots = new PcmMetricsSnapshot[scenario.Sources];
        try
        {
            for (var source = 0; source < scenario.Sources; source++)
            {
                analyzers[source] = new PcmMetricAnalyzer(
                    new PcmMetricAnalyzerOptions(scenario.SampleRate, scenario.Channels, scenario.Oversampling));
                analyzers[source].SetPerformanceMode(scenario.Mode);
                analyzers[source].SetChannelWaveformEnabled(scenario.ChannelWaveform);
                analyzers[source].SetChannelSpectrumEnabled(scenario.ChannelSpectrum);
                snapshots[source] = analyzers[source].Snapshot;
            }

            for (var frameOffset = 0; frameOffset < scenario.FrameCount; frameOffset += scenario.BlockFrames)
            {
                var framesThisBlock = Math.Min(scenario.BlockFrames, scenario.FrameCount - frameOffset);
                var sampleOffset = frameOffset * scenario.Channels;
                var block = signal.AsSpan(sampleOffset, framesThisBlock * scenario.Channels);
                for (var source = 0; source < scenario.Sources; source++)
                {
                    analyzers[source].Process(block);
                    snapshots[source] = analyzers[source].Snapshot;
                }
            }

            return snapshots;
        }
        finally
        {
            foreach (var analyzer in analyzers)
            {
                analyzer?.Dispose();
            }
        }
    }

    private static string Format(double? value)
    {
        return value is double measured ? measured.ToString("0.0000", CultureInfo.InvariantCulture) : "null";
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
        int Oversampling,
        int Sources = 1,
        bool ChannelWaveform = false,
        bool ChannelSpectrum = false);

    internal readonly record struct AnalyzerRunResult(
        TimeSpan Elapsed,
        int Frames,
        long AllocatedBytes,
        PcmMetricsSnapshot[] LastSnapshots)
    {
        /// <summary>Full-precision numeric fingerprint of the final measurement of every source.</summary>
        public string AggregateSignature => string.Join(" | ", SourceSignatures);

        public string[] SourceSignatures => LastSnapshots
            .Select((snapshot, index) => string.Create(
                CultureInfo.InvariantCulture,
                $"source{index}{Describe(snapshot)}"))
            .ToArray();

        public string Signature => AggregateSignature;

        private static string Describe(PcmMetricsSnapshot snapshot)
        {
            var channelWaveform = snapshot.ChannelWaveformSamples;
            var channelSpectrum = snapshot.ChannelSpectrumBuckets;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"[M={snapshot.MomentaryLufs:R} S={snapshot.ShortTermLufs:R} I={snapshot.IntegratedLufs:R} " +
                $"TP={snapshot.TruePeakDb:R} rms100={snapshot.Rms100MsDb:R} corr={snapshot.Correlation:R} " +
                $"X={snapshot.StereoImageX:R} Y={snapshot.StereoImageY:R} " +
                $"points={snapshot.StereoImagePoints?.Count.ToString(CultureInfo.InvariantCulture) ?? "null"} " +
                $"waveform={DescribeSeries(snapshot.WaveformSamples)} " +
                $"channelWaveform={DescribeChannels(channelWaveform)} " +
                $"spectrum={DescribeSeries(snapshot.SpectrumBuckets)} " +
                $"channelSpectrum={DescribeChannels(channelSpectrum)}]");
        }

        private static string DescribeSeries(IReadOnlyList<double>? values)
        {
            if (values is null)
            {
                return "null";
            }

            if (values.Count == 0)
            {
                return "n0/empty";
            }

            var sum = 0d;
            for (var index = 0; index < values.Count; index++)
            {
                sum += values[index];
            }

            return string.Create(
                CultureInfo.InvariantCulture,
                $"n{values.Count}/sum{sum:R}/first{values[0]:R}/last{values[^1]:R}");
        }

        private static string DescribeChannels(IReadOnlyList<IReadOnlyList<double>>? channels)
        {
            return channels is null
                ? "null"
                : string.Join("+", channels.Select(DescribeSeries));
        }
    }
}
