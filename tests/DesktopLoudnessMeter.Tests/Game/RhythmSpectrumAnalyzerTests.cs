using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class RhythmSpectrumAnalyzerTests
{
    [Theory]
    [InlineData(90, RhythmBand.Low)]
    [InlineData(800, RhythmBand.Mid)]
    [InlineData(5000, RhythmBand.High)]
    public void DominantToneSelectsExpectedRhythmBand(double frequency, RhythmBand expectedBand)
    {
        var frame = RhythmSpectrumAnalyzer.Analyze(SpectrumWithTone(frequency));

        Assert.Equal(expectedBand, frame.DominantBand);
    }

    [Fact]
    public void SilenceHasNoDominantBand()
    {
        var frame = RhythmSpectrumAnalyzer.Analyze(new double[SpectrumScale.BucketCount]);

        Assert.Equal(RhythmBand.None, frame.DominantBand);
    }

    [Theory]
    [InlineData(SignalTileKind.Cloud, RhythmBand.Low)]
    [InlineData(SignalTileKind.Moon, RhythmBand.Low)]
    [InlineData(SignalTileKind.Star, RhythmBand.Mid)]
    [InlineData(SignalTileKind.Diamond, RhythmBand.Mid)]
    [InlineData(SignalTileKind.Note, RhythmBand.High)]
    [InlineData(SignalTileKind.Bolt, RhythmBand.High)]
    [InlineData(SignalTileKind.Disc, RhythmBand.Low)]
    [InlineData(SignalTileKind.Wave, RhythmBand.Mid)]
    [InlineData(SignalTileKind.Pulse, RhythmBand.High)]
    [InlineData(SignalTileKind.Speaker, RhythmBand.High)]
    [InlineData(SignalTileKind.Meter, RhythmBand.Low)]
    [InlineData(SignalTileKind.Bars, RhythmBand.Mid)]
    [InlineData(SignalTileKind.Orbit, RhythmBand.High)]
    [InlineData(SignalTileKind.Arrows, RhythmBand.High)]
    public void TileKindsHaveFixedCosmeticBandAffinity(SignalTileKind kind, RhythmBand expectedBand)
    {
        Assert.Equal(expectedBand, RhythmSpectrumAnalyzer.ForTileKind(kind));
    }

    [Fact]
    public void AffinityUsesContinuousBandSharesInsteadOfSingleBandLatch()
    {
        var affinity = RhythmSpectrumAnalyzer.CalculateAffinity(new RhythmSpectrumFrame(
            LowEnergy: 0.06,
            MidEnergy: 0.03,
            HighEnergy: 0.015,
            DominantBand: RhythmBand.Low));

        Assert.True(affinity.Low > affinity.Mid);
        Assert.True(affinity.Mid > affinity.High);
        Assert.InRange(affinity.Low, 0, 1);
        Assert.InRange(affinity.Mid, 0, 1);
        Assert.InRange(affinity.High, 0, 1);
    }

    private static double[] SpectrumWithTone(double frequency)
    {
        var buckets = new double[SpectrumScale.BucketCount];
        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        var ratio = (SpectrumScale.HertzToMel(frequency) - minimumMel) / (maximumMel - minimumMel);
        var index = (int)Math.Round(ratio * (buckets.Length - 1));
        buckets[index] = 1;
        return buckets;
    }
}
