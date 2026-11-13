using System.Diagnostics;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Pre-existing true-peak estimator benchmark (managed vs native), retained unchanged in shape so
/// historical numbers stay comparable. Native unavailability is reported instead of thrown.
/// </summary>
internal static class TruePeakBenchmark
{
    private const int FramesPerBlock = 2400;
    private const int Seconds = 30;

    public static void Run(int sampleRate, int channels, string nativeStatus)
    {
        var frames = sampleRate * Seconds;
        var samples = BenchmarkSignal.CreateStereoSignal(frames, sampleRate, channels);

        Console.WriteLine("== True-peak estimator ==");
        Console.WriteLine(
            $"Signal: {Seconds}s, {sampleRate} Hz, {channels} channel(s), {samples.Length:N0} samples, " +
            $"{FramesPerBlock}-frame blocks, warmup=2 repeats=1");
        Console.WriteLine($"Native true peak: {nativeStatus}");
        Console.WriteLine();

        foreach (var oversampling in new[] { 2, 4, 8 })
        {
            var result = BenchmarkManaged(samples, channels, oversampling);
            Console.WriteLine(
                $"Managed {oversampling}x: {result.Elapsed.TotalMilliseconds,8:0.00} ms  " +
                $"{result.MillionFramesPerSecond,7:0.00} Mframes/s  peak={result.TruePeak:0.000000}");

            var nativeResult = TryBenchmarkNative(samples, channels, oversampling);
            if (nativeResult is null)
            {
                Console.WriteLine($"Native  {oversampling}x: unavailable (native library not loadable on this machine)");
            }
            else
            {
                Console.WriteLine(
                    $"Native  {oversampling}x: {nativeResult.Value.Elapsed.TotalMilliseconds,8:0.00} ms  " +
                    $"{nativeResult.Value.MillionFramesPerSecond,7:0.00} Mframes/s  peak={nativeResult.Value.TruePeak:0.000000}  " +
                    $"delta={nativeResult.Value.TruePeak - result.TruePeak:+0.000000;-0.000000;0.000000}");
            }

            Console.WriteLine();
        }
    }

    public static bool TryCreateNativeProbe(out string status)
    {
        try
        {
            using var estimator = new NativeRfRapidTruePeakEstimator(channels: 2, oversampling: 4);
            status = $"available ({estimator.GetType().Name} loaded)";
            return true;
        }
        catch (Exception ex) when (IsNativeUnavailable(ex))
        {
            status = $"unavailable ({ex.GetType().Name}: {ex.Message})";
            return false;
        }
    }

    private static BenchmarkResult BenchmarkManaged(float[] samples, int channels, int oversampling)
    {
        for (var warmup = 0; warmup < 2; warmup++)
        {
            RunManaged(samples, channels, oversampling);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        var truePeak = RunManaged(samples, channels, oversampling);
        stopwatch.Stop();

        return new BenchmarkResult(
            stopwatch.Elapsed,
            truePeak,
            (samples.Length / channels) / stopwatch.Elapsed.TotalSeconds / 1_000_000d);
    }

    private static double RunManaged(float[] samples, int channels, int oversampling)
    {
        var estimator = new TruePeakEstimator(channels, oversampling, windowFrames: null);
        Span<double> frame = stackalloc double[channels];
        Span<double> mono = stackalloc double[oversampling];
        Span<double> interleaved = stackalloc double[oversampling * channels];

        for (var index = 0; index < samples.Length; index += channels)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                frame[channel] = samples[index + channel];
            }

            estimator.Add(frame, mono, interleaved);
        }

        return estimator.TruePeak;
    }

    private static BenchmarkResult? TryBenchmarkNative(float[] samples, int channels, int oversampling)
    {
        try
        {
            return BenchmarkNative(samples, channels, oversampling);
        }
        catch (Exception ex) when (IsNativeUnavailable(ex))
        {
            return null;
        }
    }

    private static BenchmarkResult BenchmarkNative(float[] samples, int channels, int oversampling)
    {
        for (var warmup = 0; warmup < 2; warmup++)
        {
            RunNative(samples, channels, oversampling);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        var truePeak = RunNative(samples, channels, oversampling);
        stopwatch.Stop();

        return new BenchmarkResult(
            stopwatch.Elapsed,
            truePeak,
            (samples.Length / channels) / stopwatch.Elapsed.TotalSeconds / 1_000_000d);
    }

    private static double RunNative(float[] samples, int channels, int oversampling)
    {
        using var estimator = new NativeRfRapidTruePeakEstimator(channels, oversampling);
        var input = new double[FramesPerBlock * channels];
        var oversampledMono = new double[FramesPerBlock * oversampling];
        var oversampledInterleaved = new double[FramesPerBlock * oversampling * channels];
        var framePeaks = new double[FramesPerBlock];
        var frameOutputCounts = new int[FramesPerBlock];
        var totalFrames = samples.Length / channels;

        for (var frameOffset = 0; frameOffset < totalFrames; frameOffset += FramesPerBlock)
        {
            var framesThisBlock = Math.Min(FramesPerBlock, totalFrames - frameOffset);
            for (var frame = 0; frame < framesThisBlock; frame++)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    input[(frame * channels) + channel] = samples[((frameOffset + frame) * channels) + channel];
                }
            }

            estimator.Process(input, framesThisBlock, oversampledMono, oversampledInterleaved, framePeaks, frameOutputCounts);
        }

        return estimator.TruePeak;
    }

    private static bool IsNativeUnavailable(Exception ex)
    {
        return ex is DllNotFoundException
            or EntryPointNotFoundException
            or BadImageFormatException
            or InvalidOperationException
            or System.Runtime.InteropServices.SEHException;
    }

    internal readonly record struct BenchmarkResult(TimeSpan Elapsed, double TruePeak, double MillionFramesPerSecond);
}
