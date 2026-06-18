using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class LoudnessMetricTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void MomentaryLoudnessRequiresFourHundredMilliseconds()
    {
        var analyzer = Analyzer();

        analyzer.Process(Sine(seconds: 0.39, amplitude: 0.5));
        Assert.Null(analyzer.Snapshot.MomentaryLufs);

        analyzer.Process(Sine(seconds: 0.02, amplitude: 0.5, startFrame: (int)(SampleRate * 0.39)));
        Assert.NotNull(analyzer.Snapshot.MomentaryLufs);
    }

    [Fact]
    public void ShortTermLoudnessRequiresThreeSeconds()
    {
        var analyzer = Analyzer();

        analyzer.Process(Sine(seconds: 2.9, amplitude: 0.5));
        Assert.Null(analyzer.Snapshot.ShortTermLufs);

        analyzer.Process(Sine(seconds: 0.2, amplitude: 0.5, startFrame: (int)(SampleRate * 2.9)));
        Assert.NotNull(analyzer.Snapshot.ShortTermLufs);
    }

    [Fact]
    public void SilenceDoesNotProduceLoudnessMetrics()
    {
        var analyzer = Analyzer();

        analyzer.Process(new float[SampleRate * 4 * 2]);

        Assert.Null(analyzer.Snapshot.MomentaryLufs);
        Assert.Null(analyzer.Snapshot.ShortTermLufs);
        Assert.Null(analyzer.Snapshot.IntegratedLufs);
    }

    [Fact]
    public void IntegratedLoudnessIgnoresQuietTailBelowGate()
    {
        var loudOnly = Analyzer();
        loudOnly.Process(Sine(seconds: 2, amplitude: 0.45));

        var loudThenQuiet = Analyzer();
        loudThenQuiet.Process(Sine(seconds: 2, amplitude: 0.45));
        loudThenQuiet.Process(Sine(seconds: 2, amplitude: 0.0001, startFrame: SampleRate * 2));

        Assert.NotNull(loudOnly.Snapshot.IntegratedLufs);
        Assert.NotNull(loudThenQuiet.Snapshot.IntegratedLufs);
        Assert.InRange(
            Math.Abs(loudOnly.Snapshot.IntegratedLufs.Value - loudThenQuiet.Snapshot.IntegratedLufs.Value),
            0,
            0.5);
    }

    private static PcmMetricAnalyzer Analyzer()
    {
        return new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 4));
    }

    private static float[] Sine(double seconds, double amplitude, int startFrame = 0, double frequency = 1000)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * frequency * (startFrame + frame) / SampleRate) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }
}
