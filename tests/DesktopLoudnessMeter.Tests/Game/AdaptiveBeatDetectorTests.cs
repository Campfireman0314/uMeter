using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class AdaptiveBeatDetectorTests
{
    private const double FrameDuration = 1d / 30;

    [Theory]
    [InlineData(90)]
    [InlineData(800)]
    [InlineData(5000)]
    public void TransientInAnyPrimaryBandProducesBeat(double frequency)
    {
        var detector = new AdaptiveBeatDetector();
        detector.Update(SpectrumWithTone(frequency, 0.4), FrameDuration);

        var frame = detector.Update(SpectrumWithTone(frequency, 1), FrameDuration);

        Assert.True(frame.IsBeat);
        Assert.Equal(1, frame.Pulse);
        Assert.True(frame.OnsetStrength >= frame.Threshold);
        Assert.True(frame.SignalEnergy > 0.05);
    }

    [Fact]
    public void SteadySpectrumDoesNotContinuouslyRetrigger()
    {
        var detector = new AdaptiveBeatDetector();
        var spectrum = SpectrumWithTone(800, 0.8);

        var beatCount = Enumerable.Range(0, 120)
            .Count(_ => detector.Update(spectrum, FrameDuration).IsBeat);

        Assert.Equal(0, beatCount);
    }

    [Fact]
    public void RefractoryPeriodRejectsImmediateSecondTransient()
    {
        var detector = new AdaptiveBeatDetector();
        detector.Update(SpectrumWithTone(90, 0.2), FrameDuration);
        Assert.True(detector.Update(SpectrumWithTone(90, 1), FrameDuration).IsBeat);

        detector.Update(SpectrumWithTone(90, 0), FrameDuration);
        detector.Update(SpectrumWithTone(90, 0), FrameDuration);
        var immediateSecond = detector.Update(SpectrumWithTone(90, 1), FrameDuration);

        Assert.False(immediateSecond.IsBeat);
    }

    [Fact]
    public void SynchronizedBandTransientsReportCrossBandAgreement()
    {
        var detector = new AdaptiveBeatDetector();
        detector.Update(SpectrumWithTones(0.3, 90, 800, 5000), FrameDuration);

        var frame = detector.Update(SpectrumWithTones(1, 90, 800, 5000), FrameDuration);

        Assert.True(frame.IsBeat);
        Assert.True(frame.CrossBandAgreement >= 0.9);
    }

    [Fact]
    public void AutocorrelationFindsRegularPulsePeriod()
    {
        var history = Enumerable.Range(0, 75)
            .Select(index => index % 15 == 0 ? 1d : 0d)
            .ToArray();

        var periodicity = AdaptiveBeatDetector.EstimatePeriodicity(history);

        Assert.Equal(15, periodicity.Lag);
        Assert.True(periodicity.Score > 0.9);
    }

    [Fact]
    public void CrossBandCorrelationRewardsSynchronizedFlux()
    {
        var low = new[] { 0d, 1, 0, 0, 1, 0, 0, 1 };
        var synchronized = AdaptiveBeatDetector.EstimateCrossBandCorrelation(low, low, low);
        var unrelated = AdaptiveBeatDetector.EstimateCrossBandCorrelation(
            low,
            new[] { 1d, 0, 0, 1, 0, 0, 1, 0 },
            new double[low.Length]);

        Assert.True(synchronized > 0.99);
        Assert.True(unrelated < synchronized * 0.5);
    }

    [Fact]
    public void InvalidSpectrumValuesRemainFiniteAndBounded()
    {
        var detector = new AdaptiveBeatDetector();
        detector.Update([double.NaN, double.PositiveInfinity, -1, 2], double.NaN, double.NaN);
        var frame = detector.Update([2, -1, double.NaN, 1], double.NaN, double.NaN);

        Assert.InRange(frame.BassEnergy, 0, 1);
        Assert.InRange(frame.SignalEnergy, 0, 1);
        Assert.InRange(frame.Baseline, 0, 1);
        Assert.InRange(frame.Threshold, 0, 1);
        Assert.InRange(frame.OnsetStrength, 0, 1);
        Assert.InRange(frame.SpectralFlux, 0, 1);
        Assert.InRange(frame.CrossBandAgreement, 0, 1);
        Assert.InRange(frame.Periodicity, 0, 1);
        Assert.InRange(frame.Pulse, 0, 1);
    }

    private static double[] SpectrumWithTone(double frequency, double displayLevel)
    {
        return SpectrumWithTones(displayLevel, frequency);
    }

    private static double[] SpectrumWithTones(double displayLevel, params double[] frequencies)
    {
        var buckets = new double[SpectrumScale.BucketCount];
        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        foreach (var frequency in frequencies)
        {
            var ratio = (SpectrumScale.HertzToMel(frequency) - minimumMel) / (maximumMel - minimumMel);
            buckets[(int)Math.Round(ratio * (buckets.Length - 1))] = displayLevel;
        }

        return buckets;
    }
}
