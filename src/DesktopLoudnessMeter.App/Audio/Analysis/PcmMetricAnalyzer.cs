namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed class PcmMetricAnalyzer
{
    private readonly PcmMetricAnalyzerOptions _options;
    private readonly KWeightingFilter[] _kWeightingFilters;
    private readonly SlidingPowerWindow _momentaryWindow;
    private readonly SlidingPowerWindow _shortTermWindow;
    private readonly IntegratedLoudnessGate _integratedGate;
    private readonly RecentSampleWindow _recentSamples;
    private readonly TruePeakEstimator _truePeakEstimator;
    private readonly double[] _frameBuffer = new double[2];
    private readonly double[] _oversampledDisplayBuffer;
    private readonly double[] _oversampledChannelDisplayBuffer;
    private bool _channelWaveformEnabled;
    private bool _channelSpectrumEnabled;
    private long _processedFrames;
    private PcmMetricsSnapshot _snapshot = new();

    public PcmMetricAnalyzer(PcmMetricAnalyzerOptions options)
    {
        _options = options;
        _kWeightingFilters = Enumerable.Range(0, options.Channels)
            .Select(_ => new KWeightingFilter())
            .ToArray();
        _momentaryWindow = new SlidingPowerWindow(MillisecondsToFrames(400));
        _shortTermWindow = new SlidingPowerWindow(MillisecondsToFrames(3000));
        _integratedGate = new IntegratedLoudnessGate(MillisecondsToFrames(400), MillisecondsToFrames(100));
        _recentSamples = new RecentSampleWindow(
            options.Channels,
            options.SampleRate,
            options.TruePeakOversampling,
            MillisecondsToFrames(100));
        _truePeakEstimator = new TruePeakEstimator(
            options.Channels,
            options.TruePeakOversampling,
            MillisecondsToFrames(100));
        _oversampledDisplayBuffer = new double[options.TruePeakOversampling];
        _oversampledChannelDisplayBuffer = new double[options.TruePeakOversampling * options.Channels];
    }

    public PcmMetricsSnapshot Snapshot => _snapshot;

    public void SetChannelSpectrumEnabled(bool enabled)
    {
        _channelSpectrumEnabled = enabled;
    }

    public void SetChannelWaveformEnabled(bool enabled)
    {
        _channelWaveformEnabled = enabled;
    }

    public void Process(ReadOnlySpan<float> interleavedSamples)
    {
        if (interleavedSamples.Length % _options.Channels != 0)
        {
            throw new ArgumentException("Interleaved sample block length must align to the channel count.", nameof(interleavedSamples));
        }

        if (interleavedSamples.IsEmpty)
        {
            return;
        }

        for (var index = 0; index < interleavedSamples.Length; index += _options.Channels)
        {
            var weightedPower = 0d;
            for (var channel = 0; channel < _options.Channels; channel++)
            {
                var sample = NormalizeSample(interleavedSamples[index + channel]);
                _frameBuffer[channel] = sample;

                var weighted = _kWeightingFilters[channel].Process(sample);
                weightedPower += weighted * weighted;
            }

            var activeFrame = _frameBuffer.AsSpan(0, _options.Channels);
            var oversampledCount = _truePeakEstimator.Add(
                activeFrame,
                _oversampledDisplayBuffer,
                _oversampledChannelDisplayBuffer);
            _recentSamples.Add(
                activeFrame,
                _oversampledDisplayBuffer.AsSpan(0, oversampledCount),
                _oversampledChannelDisplayBuffer.AsSpan(0, oversampledCount * _options.Channels));
            _momentaryWindow.Add(weightedPower);
            _shortTermWindow.Add(weightedPower);
            _integratedGate.Add(weightedPower);
        }

        _processedFrames += interleavedSamples.Length / _options.Channels;
        RefreshSnapshot();
    }

    public void Reset()
    {
        _processedFrames = 0;
        foreach (var filter in _kWeightingFilters)
        {
            filter.Reset();
        }

        _momentaryWindow.Reset();
        _shortTermWindow.Reset();
        _integratedGate.Reset();
        _recentSamples.Reset();
        _truePeakEstimator.Reset();
        _snapshot = new PcmMetricsSnapshot();
    }

    internal static double MeanSquareToLufs(double meanSquare)
    {
        return meanSquare <= 0 ? double.NegativeInfinity : -0.691 + (10 * Math.Log10(meanSquare));
    }

    internal static double LufsToMeanSquare(double lufs)
    {
        return Math.Pow(10, (lufs + 0.691) / 10);
    }

    private void RefreshSnapshot()
    {
        var seconds = (double)_processedFrames / _options.SampleRate;
        var momentary = _momentaryWindow.IsFull ? MeanSquareToNullableLufs(_momentaryWindow.Mean) : null;
        var shortTerm = _shortTermWindow.IsFull ? MeanSquareToNullableLufs(_shortTermWindow.Mean) : null;

        _snapshot = new PcmMetricsSnapshot(
            MomentaryLufs: momentary,
            ShortTermLufs: shortTerm,
            IntegratedLufs: _integratedGate.CalculateLufs(),
            TruePeakDb: _truePeakEstimator.HasSamples ? ToDb(_truePeakEstimator.TruePeak) : null,
            Rms100MsDb: seconds >= 0.1 ? ToDb(_recentSamples.Rms) : null,
            StereoImageX: _recentSamples.StereoImageX,
            StereoImageY: _recentSamples.StereoImageY,
            StereoImagePoints: _recentSamples.StereoImagePoints,
            Correlation: _recentSamples.Correlation,
            WaveformSamples: _recentSamples.WaveformSamples,
            ChannelWaveformSamples: _channelWaveformEnabled ? _recentSamples.ChannelWaveformSamples : null,
            SpectrumBuckets: _recentSamples.SpectrumBuckets,
            ChannelSpectrumBuckets: _channelSpectrumEnabled ? _recentSamples.ChannelSpectrumBuckets : null);
    }

    private int MillisecondsToFrames(int milliseconds)
    {
        return Math.Max(1, (int)Math.Round(_options.SampleRate * milliseconds / 1000d));
    }

    private static double? MeanSquareToNullableLufs(double meanSquare)
    {
        return meanSquare <= 0 ? null : MeanSquareToLufs(meanSquare);
    }

    private static double NormalizeSample(float sample)
    {
        return double.IsFinite(sample) ? sample : 0;
    }

    private static double ToDb(double amplitude)
    {
        return amplitude <= 0 ? double.NegativeInfinity : 20 * Math.Log10(amplitude);
    }
}
