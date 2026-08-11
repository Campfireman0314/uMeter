using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Game;

public readonly record struct BeatDetectionFrame(
    double BassEnergy,
    double SignalEnergy,
    double Baseline,
    double Threshold,
    double OnsetStrength,
    double SpectralFlux,
    double CrossBandAgreement,
    double Periodicity,
    double Pulse,
    bool IsBeat);

public sealed class AdaptiveBeatDetector
{
    private const double MinimumDeltaSeconds = 1d / 240;
    private const double MaximumDeltaSeconds = 0.1;
    private const double RefractorySeconds = 0.18;
    private const int HistoryCapacity = 90;
    private const int CorrelationWindow = 18;
    private readonly Queue<double> _onsetHistory = new();
    private readonly Queue<double> _lowFluxHistory = new();
    private readonly Queue<double> _midFluxHistory = new();
    private readonly Queue<double> _highFluxHistory = new();
    private double[]? _previousMagnitudes;
    private double _fluxBaseline;
    private double _lowFluxBaseline;
    private double _midFluxBaseline;
    private double _highFluxBaseline;
    private double _pulse;
    private double _refractoryRemaining;
    private int _framesSinceBeat = int.MaxValue;

    public BeatDetectionFrame Update(
        IReadOnlyList<double>? spectrumBuckets,
        double deltaSeconds,
        double sensitivity = 0.55)
    {
        deltaSeconds = double.IsFinite(deltaSeconds)
            ? Math.Clamp(deltaSeconds, MinimumDeltaSeconds, MaximumDeltaSeconds)
            : 1d / 30;
        sensitivity = double.IsFinite(sensitivity) ? Math.Clamp(sensitivity, 0, 1) : 0.55;

        _refractoryRemaining = Math.Max(0, _refractoryRemaining - deltaSeconds);
        _pulse *= Math.Exp(-deltaSeconds * 10);
        if (_framesSinceBeat < int.MaxValue)
        {
            _framesSinceBeat++;
        }

        var features = ExtractFeatures(spectrumBuckets);
        Push(_lowFluxHistory, features.LowFlux, CorrelationWindow);
        Push(_midFluxHistory, features.MidFlux, CorrelationWindow);
        Push(_highFluxHistory, features.HighFlux, CorrelationWindow);

        var bandPeak = Math.Max(features.LowFlux, Math.Max(features.MidFlux, features.HighFlux));
        var rawOnset = Math.Max(features.SpectralFlux * 1.6, bandPeak);
        Push(_onsetHistory, rawOnset, HistoryCapacity);
        var periodicity = EstimatePeriodicity(_onsetHistory.ToArray());
        var crossCorrelation = EstimateCrossBandCorrelation(
            _lowFluxHistory.ToArray(),
            _midFluxHistory.ToArray(),
            _highFluxHistory.ToArray());

        var agreeingBands = 0;
        if (features.LowFlux > (_lowFluxBaseline * 1.65) + 0.004) agreeingBands++;
        if (features.MidFlux > (_midFluxBaseline * 1.65) + 0.004) agreeingBands++;
        if (features.HighFlux > (_highFluxBaseline * 1.65) + 0.004) agreeingBands++;
        var instantAgreement = agreeingBands / 3d;
        var crossBandAgreement = Math.Clamp(Math.Max(instantAgreement, crossCorrelation), 0, 1);
        var onsetStrength = Math.Clamp(rawOnset * (1 + (crossBandAgreement * 0.3)), 0, 1);

        var minimumThreshold = 0.026 - (sensitivity * 0.015);
        var threshold = Math.Max(minimumThreshold, _fluxBaseline * (1.8 - (sensitivity * 0.7)));
        var periodicProximity = periodicity.Lag > 0 && _framesSinceBeat < int.MaxValue
            ? Math.Exp(-Math.Pow((_framesSinceBeat - periodicity.Lag) / 2.2, 2))
            : 0;
        var adjustedThreshold = threshold * (1 - (periodicity.Score * periodicProximity * 0.18));
        var isBeat = _refractoryRemaining <= 0
            && features.SignalEnergy >= 0.008
            && onsetStrength >= adjustedThreshold;

        if (isBeat)
        {
            _pulse = 1;
            _refractoryRemaining = RefractorySeconds;
            _framesSinceBeat = 0;
        }

        _fluxBaseline = FollowBaseline(_fluxBaseline, rawOnset, deltaSeconds);
        _lowFluxBaseline = FollowBaseline(_lowFluxBaseline, features.LowFlux, deltaSeconds);
        _midFluxBaseline = FollowBaseline(_midFluxBaseline, features.MidFlux, deltaSeconds);
        _highFluxBaseline = FollowBaseline(_highFluxBaseline, features.HighFlux, deltaSeconds);

        return new BeatDetectionFrame(
            features.LowEnergy,
            features.SignalEnergy,
            Math.Clamp(_fluxBaseline, 0, 1),
            Math.Clamp(adjustedThreshold, 0, 1),
            onsetStrength,
            Math.Clamp(features.SpectralFlux, 0, 1),
            crossBandAgreement,
            Math.Clamp(periodicity.Score, 0, 1),
            Math.Clamp(_pulse, 0, 1),
            isBeat);
    }

    public void Reset()
    {
        _onsetHistory.Clear();
        _lowFluxHistory.Clear();
        _midFluxHistory.Clear();
        _highFluxHistory.Clear();
        _previousMagnitudes = null;
        _fluxBaseline = 0;
        _lowFluxBaseline = 0;
        _midFluxBaseline = 0;
        _highFluxBaseline = 0;
        _pulse = 0;
        _refractoryRemaining = 0;
        _framesSinceBeat = int.MaxValue;
    }

    internal static (double Score, int Lag) EstimatePeriodicity(IReadOnlyList<double> onsetHistory)
    {
        if (onsetHistory.Count < 24)
        {
            return default;
        }

        var maximumLag = Math.Min(45, onsetHistory.Count / 2);
        var bestScore = 0d;
        var bestLag = 0;
        for (var lag = 8; lag <= maximumLag; lag++)
        {
            var score = NormalizedDot(onsetHistory, onsetHistory, lag);
            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        return (Math.Clamp(bestScore, 0, 1), bestLag);
    }

    internal static double EstimateCrossBandCorrelation(
        IReadOnlyList<double> low,
        IReadOnlyList<double> mid,
        IReadOnlyList<double> high)
    {
        if (low.Count < 4 || low.Count != mid.Count || low.Count != high.Count)
        {
            return 0;
        }

        var lowMid = NormalizedDot(low, mid, 0);
        var lowHigh = NormalizedDot(low, high, 0);
        var midHigh = NormalizedDot(mid, high, 0);
        return Math.Clamp((lowMid + lowHigh + midHigh) / 3, 0, 1);
    }

    private SpectrumFeatures ExtractFeatures(IReadOnlyList<double>? spectrumBuckets)
    {
        if (spectrumBuckets is null || spectrumBuckets.Count < 2)
        {
            _previousMagnitudes = null;
            return default;
        }

        if (_previousMagnitudes is null || _previousMagnitudes.Length != spectrumBuckets.Count)
        {
            _previousMagnitudes = new double[spectrumBuckets.Count];
            for (var index = 0; index < spectrumBuckets.Count; index++)
            {
                _previousMagnitudes[index] = DisplayLevelToMagnitude(spectrumBuckets[index]);
            }

            var initial = RhythmSpectrumAnalyzer.Analyze(spectrumBuckets);
            return new SpectrumFeatures(initial.LowEnergy, initial.MidEnergy, initial.HighEnergy, 0, 0, 0, 0);
        }

        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        var lowSquare = 0d;
        var midSquare = 0d;
        var highSquare = 0d;
        var broadFluxSquare = 0d;
        var lowFluxSquare = 0d;
        var midFluxSquare = 0d;
        var highFluxSquare = 0d;
        var lowCount = 0;
        var midCount = 0;
        var highCount = 0;
        for (var index = 0; index < spectrumBuckets.Count; index++)
        {
            var magnitude = DisplayLevelToMagnitude(spectrumBuckets[index]);
            var difference = Math.Max(0, magnitude - _previousMagnitudes[index]);
            _previousMagnitudes[index] = magnitude;
            broadFluxSquare += difference * difference;

            var ratio = (double)index / (spectrumBuckets.Count - 1);
            var frequency = SpectrumScale.MelToHertz(minimumMel + ((maximumMel - minimumMel) * ratio));
            if (frequency is >= 40 and < 250)
            {
                lowSquare += magnitude * magnitude;
                lowFluxSquare += difference * difference;
                lowCount++;
            }
            else if (frequency is >= 250 and < 2000)
            {
                midSquare += magnitude * magnitude;
                midFluxSquare += difference * difference;
                midCount++;
            }
            else if (frequency is >= 2000 and < 10000)
            {
                highSquare += magnitude * magnitude;
                highFluxSquare += difference * difference;
                highCount++;
            }
        }

        return new SpectrumFeatures(
            RootMeanSquare(lowSquare, lowCount),
            RootMeanSquare(midSquare, midCount),
            RootMeanSquare(highSquare, highCount),
            Math.Sqrt(broadFluxSquare / spectrumBuckets.Count),
            RootMeanSquare(lowFluxSquare, lowCount),
            RootMeanSquare(midFluxSquare, midCount),
            RootMeanSquare(highFluxSquare, highCount));
    }

    private static double FollowBaseline(double current, double target, double deltaSeconds)
    {
        var timeConstant = target > current ? 0.85 : 0.32;
        return current + ((target - current) * (1 - Math.Exp(-deltaSeconds / timeConstant)));
    }

    private static double DisplayLevelToMagnitude(double displayLevel)
    {
        displayLevel = double.IsFinite(displayLevel) ? Math.Clamp(displayLevel, 0, 1) : 0;
        return Math.Pow(10, ((displayLevel * 200) - 200) / 20);
    }

    private static double RootMeanSquare(double sumOfSquares, int count)
    {
        return count == 0 ? 0 : Math.Clamp(Math.Sqrt(sumOfSquares / count), 0, 1);
    }

    private static double NormalizedDot(
        IReadOnlyList<double> first,
        IReadOnlyList<double> second,
        int lag)
    {
        var count = Math.Min(first.Count, second.Count - lag);
        if (count <= 0)
        {
            return 0;
        }

        var dot = 0d;
        var firstSquare = 0d;
        var secondSquare = 0d;
        for (var index = 0; index < count; index++)
        {
            var firstValue = first[first.Count - count + index];
            var secondValue = second[second.Count - count - lag + index];
            dot += firstValue * secondValue;
            firstSquare += firstValue * firstValue;
            secondSquare += secondValue * secondValue;
        }

        var denominator = Math.Sqrt(firstSquare * secondSquare);
        return denominator <= double.Epsilon ? 0 : Math.Clamp(dot / denominator, 0, 1);
    }

    private static void Push(Queue<double> values, double value, int capacity)
    {
        values.Enqueue(double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0);
        while (values.Count > capacity)
        {
            values.Dequeue();
        }
    }

    private readonly record struct SpectrumFeatures(
        double LowEnergy,
        double MidEnergy,
        double HighEnergy,
        double SpectralFlux,
        double LowFlux,
        double MidFlux,
        double HighFlux)
    {
        public double SignalEnergy => Math.Max(LowEnergy, Math.Max(MidEnergy, HighEnergy));
    }
}
