using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class PcmMetricAnalyzerContractTests
{
    [Theory]
    [InlineData(0, 2, 4)]
    [InlineData(48000, 0, 4)]
    [InlineData(48000, 3, 4)]
    [InlineData(48000, 2, 1)]
    [InlineData(48000, 2, 16)]
    public void RejectsInvalidOptions(int sampleRate, int channels, int oversampling)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, channels, oversampling)));
    }

    [Fact]
    public void RejectsUnalignedInterleavedBlocks()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));

        Assert.Throws<ArgumentException>(() => analyzer.Process([0.1f, -0.1f, 0.2f]));
    }

    [Fact]
    public void SnapshotStartsUnavailableAndIndependentOfUiTypes()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));

        var snapshot = analyzer.Snapshot;

        Assert.Null(snapshot.MomentaryLufs);
        Assert.Null(snapshot.ShortTermLufs);
        Assert.Null(snapshot.IntegratedLufs);
        Assert.Null(snapshot.TruePeakDb);
        Assert.Null(snapshot.Rms100MsDb);
        Assert.Null(snapshot.StereoImageX);
        Assert.Null(snapshot.StereoImageY);
        Assert.Null(snapshot.Correlation);
        Assert.Equal("DesktopLoudnessMeter.App.Audio.Analysis", typeof(PcmMetricsSnapshot).Namespace);
    }

    [Fact]
    public void ResetClearsAvailableMetrics()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));
        var block = Sine(seconds: 1, amplitude: 0.25);

        analyzer.Process(block);
        Assert.NotNull(analyzer.Snapshot.MomentaryLufs);
        Assert.NotNull(analyzer.Snapshot.TruePeakDb);

        analyzer.Reset();

        Assert.Null(analyzer.Snapshot.MomentaryLufs);
        Assert.Null(analyzer.Snapshot.TruePeakDb);
        Assert.Null(analyzer.Snapshot.Rms100MsDb);
    }

    private static float[] Sine(double seconds, double amplitude, double frequency = 1000)
    {
        const int sampleRate = 48000;
        var frames = (int)Math.Round(seconds * sampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * frequency * frame / sampleRate) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }
}
