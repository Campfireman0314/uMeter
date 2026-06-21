using System.Diagnostics;
using DesktopLoudnessMeter.App.Audio.Analysis;

const int sampleRate = 48000;
const int channels = 2;
const int seconds = 30;
const int blockFrames = 2400;
const int frames = sampleRate * seconds;

var samples = CreateStereoSignal(frames, sampleRate);
Console.WriteLine("uMeter true-peak benchmark");
Console.WriteLine($"Signal: {seconds}s, {sampleRate} Hz, stereo, {samples.Length:N0} samples, {blockFrames}-frame blocks");
Console.WriteLine();

foreach (var oversampling in new[] { 2, 4, 8 })
{
    var result = BenchmarkManaged(samples, channels, oversampling);
    Console.WriteLine(
        $"Managed {oversampling}x: {result.Elapsed.TotalMilliseconds,8:0.00} ms  " +
        $"{result.MillionFramesPerSecond,7:0.00} Mframes/s  peak={result.TruePeak:0.000000}");

    var nativeResult = BenchmarkNative(samples, channels, oversampling, blockFrames);
    Console.WriteLine(
        $"Native  {oversampling}x: {nativeResult.Elapsed.TotalMilliseconds,8:0.00} ms  " +
        $"{nativeResult.MillionFramesPerSecond,7:0.00} Mframes/s  peak={nativeResult.TruePeak:0.000000}  " +
        $"delta={nativeResult.TruePeak - result.TruePeak:+0.000000;-0.000000;0.000000}");
    Console.WriteLine();
}

static BenchmarkResult BenchmarkManaged(float[] samples, int channels, int oversampling)
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

static double RunManaged(float[] samples, int channels, int oversampling)
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

static BenchmarkResult BenchmarkNative(float[] samples, int channels, int oversampling, int blockFrames)
{
    for (var warmup = 0; warmup < 2; warmup++)
    {
        RunNative(samples, channels, oversampling, blockFrames);
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var stopwatch = Stopwatch.StartNew();
    var truePeak = RunNative(samples, channels, oversampling, blockFrames);
    stopwatch.Stop();

    return new BenchmarkResult(
        stopwatch.Elapsed,
        truePeak,
        (samples.Length / channels) / stopwatch.Elapsed.TotalSeconds / 1_000_000d);
}

static double RunNative(float[] samples, int channels, int oversampling, int blockFrames)
{
    using var estimator = new NativeRfRapidTruePeakEstimator(channels, oversampling);
    var input = new double[blockFrames * channels];
    var oversampledMono = new double[blockFrames * oversampling];
    var oversampledInterleaved = new double[blockFrames * oversampling * channels];
    var framePeaks = new double[blockFrames];
    var frameOutputCounts = new int[blockFrames];
    var totalFrames = samples.Length / channels;

    for (var frameOffset = 0; frameOffset < totalFrames; frameOffset += blockFrames)
    {
        var framesThisBlock = Math.Min(blockFrames, totalFrames - frameOffset);
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

static float[] CreateStereoSignal(int frameCount, int sampleRate)
{
    var samples = new float[frameCount * channels];
    for (var frame = 0; frame < frameCount; frame++)
    {
        var t = (double)frame / sampleRate;
        var left =
            (0.58 * Math.Sin(2 * Math.PI * 997 * t + 0.2)) +
            (0.24 * Math.Sin(2 * Math.PI * 6173 * t + 1.1)) +
            (0.09 * Math.Sin(2 * Math.PI * 13007 * t + 0.5));
        var right =
            (0.52 * Math.Sin(2 * Math.PI * 1009 * t + 0.7)) +
            (0.28 * Math.Sin(2 * Math.PI * 5303 * t + 1.8)) +
            (0.11 * Math.Sin(2 * Math.PI * 11941 * t + 0.1));

        samples[frame * channels] = (float)Math.Clamp(left, -0.98, 0.98);
        samples[(frame * channels) + 1] = (float)Math.Clamp(right, -0.98, 0.98);
    }

    return samples;
}

internal readonly record struct BenchmarkResult(TimeSpan Elapsed, double TruePeak, double MillionFramesPerSecond);
