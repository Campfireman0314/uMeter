using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Game;

public enum RhythmBand
{
    None,
    Low,
    Mid,
    High
}

public readonly record struct RhythmSpectrumFrame(
    double LowEnergy,
    double MidEnergy,
    double HighEnergy,
    RhythmBand DominantBand);

public readonly record struct RhythmAffinityFrame(
    double Low,
    double Mid,
    double High);

public static class RhythmSpectrumAnalyzer
{
    public static RhythmSpectrumFrame Analyze(IReadOnlyList<double>? spectrumBuckets)
    {
        var low = MeasureBand(spectrumBuckets, 40, 250);
        var mid = MeasureBand(spectrumBuckets, 250, 2000);
        var high = MeasureBand(spectrumBuckets, 2000, 10000);
        var energies = new[]
        {
            (Band: RhythmBand.Low, Energy: low),
            (Band: RhythmBand.Mid, Energy: mid),
            (Band: RhythmBand.High, Energy: high)
        };
        var ordered = energies.OrderByDescending(item => item.Energy).ToArray();
        var dominant = ordered[0].Energy >= 0.008
            && ordered[0].Energy >= ordered[1].Energy * 1.12
                ? ordered[0].Band
                : RhythmBand.None;
        return new RhythmSpectrumFrame(low, mid, high, dominant);
    }

    public static RhythmAffinityFrame CalculateAffinity(RhythmSpectrumFrame frame)
    {
        var sum = frame.LowEnergy + frame.MidEnergy + frame.HighEnergy;
        var maximum = Math.Max(frame.LowEnergy, Math.Max(frame.MidEnergy, frame.HighEnergy));
        if (sum <= double.Epsilon || maximum < 0.008)
        {
            return default;
        }

        var level = Math.Clamp(maximum / 0.06, 0, 1);
        return new RhythmAffinityFrame(
            AffinityFromShare(frame.LowEnergy / sum) * level,
            AffinityFromShare(frame.MidEnergy / sum) * level,
            AffinityFromShare(frame.HighEnergy / sum) * level);
    }

    public static RhythmBand ForTileKind(SignalTileKind kind)
    {
        return kind switch
        {
            SignalTileKind.Cloud or SignalTileKind.Moon or SignalTileKind.Bloom or SignalTileKind.Disc or SignalTileKind.Meter => RhythmBand.Low,
            SignalTileKind.Star or SignalTileKind.Sun or SignalTileKind.Diamond or SignalTileKind.Wave or SignalTileKind.Bars => RhythmBand.Mid,
            SignalTileKind.Note or SignalTileKind.Bolt or SignalTileKind.Pulse or SignalTileKind.Speaker or SignalTileKind.Orbit or SignalTileKind.Arrows => RhythmBand.High,
            _ => RhythmBand.None
        };
    }

    private static double AffinityFromShare(double share)
    {
        var normalized = Math.Clamp((share - 0.2) / 0.6, 0, 1);
        return normalized * normalized * (3 - (2 * normalized));
    }

    internal static double MeasureBand(
        IReadOnlyList<double>? spectrumBuckets,
        double minimumFrequencyHz,
        double maximumFrequencyHz)
    {
        if (spectrumBuckets is null || spectrumBuckets.Count < 2)
        {
            return 0;
        }

        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        var sumOfSquares = 0d;
        var sampleCount = 0;
        for (var index = 0; index < spectrumBuckets.Count; index++)
        {
            var ratio = (double)index / (spectrumBuckets.Count - 1);
            var frequency = SpectrumScale.MelToHertz(minimumMel + ((maximumMel - minimumMel) * ratio));
            if (frequency < minimumFrequencyHz || frequency >= maximumFrequencyHz)
            {
                continue;
            }

            var displayLevel = spectrumBuckets[index];
            displayLevel = double.IsFinite(displayLevel) ? Math.Clamp(displayLevel, 0, 1) : 0;
            var decibels = (displayLevel * 200) - 200;
            var magnitude = Math.Pow(10, decibels / 20);
            sumOfSquares += magnitude * magnitude;
            sampleCount++;
        }

        return sampleCount == 0 ? 0 : Math.Clamp(Math.Sqrt(sumOfSquares / sampleCount), 0, 1);
    }
}
