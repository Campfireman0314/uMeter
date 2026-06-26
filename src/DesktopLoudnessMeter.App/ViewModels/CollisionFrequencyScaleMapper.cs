using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.ViewModels;

public static class CollisionFrequencyScaleMapper
{
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
    private static readonly double LogDomain = Math.Log(SpectrumScale.MaximumFrequencyHz / SpectrumScale.MinimumFrequencyHz);

    public static double BucketRatioToScaleRatio(double bucketRatio, CollisionFrequencyScale scale)
    {
        var frequency = BucketRatioToFrequency(bucketRatio);
        return FrequencyToScaleRatio(frequency, scale);
    }

    public static double FrequencyToScaleRatio(double frequency, CollisionFrequencyScale scale)
    {
        var clamped = Math.Clamp(frequency, SpectrumScale.MinimumFrequencyHz, SpectrumScale.MaximumFrequencyHz);
        return scale switch
        {
            CollisionFrequencyScale.Logarithmic => Math.Clamp(
                Math.Log(clamped / SpectrumScale.MinimumFrequencyHz) / LogDomain,
                0,
                1),
            _ => Math.Clamp(
                (SpectrumScale.HertzToMel(clamped) - MinimumMel) / (MaximumMel - MinimumMel),
                0,
                1)
        };
    }

    public static double ScaleRatioToFrequency(double scaleRatio, CollisionFrequencyScale scale)
    {
        var ratio = Math.Clamp(scaleRatio, 0, 1);
        return scale switch
        {
            CollisionFrequencyScale.Logarithmic => SpectrumScale.MinimumFrequencyHz
                * Math.Pow(SpectrumScale.MaximumFrequencyHz / SpectrumScale.MinimumFrequencyHz, ratio),
            _ => SpectrumScale.MelToHertz(MinimumMel + ((MaximumMel - MinimumMel) * ratio))
        };
    }

    private static double BucketRatioToFrequency(double bucketRatio)
    {
        var ratio = Math.Clamp(bucketRatio, 0, 1);
        var mel = MinimumMel + ((MaximumMel - MinimumMel) * ratio);
        return SpectrumScale.MelToHertz(mel);
    }
}
