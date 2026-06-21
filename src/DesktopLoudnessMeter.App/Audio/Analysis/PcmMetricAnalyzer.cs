namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed class PcmMetricAnalyzer : IDisposable
{
    private readonly PcmMetricAnalyzerOptions _options;
    private readonly KWeightingFilter[] _kWeightingFilters;
    private readonly SlidingPowerWindow _momentaryWindow;
    private readonly SlidingPowerWindow _shortTermWindow;
    private readonly IntegratedLoudnessGate _integratedGate;
    private readonly RecentSampleWindow _recentSamples;
    private readonly TruePeakEstimator _truePeakEstimator;
    private readonly double[] _frameBuffer = new double[2];
    private readonly int _truePeakWindowFrames;
    private NativeRfRapidTruePeakEstimator? _nativeTruePeakEstimator;
    private double[] _nativeInputBuffer = [];
    private double[] _nativeOversampledDisplayBuffer = [];
    private double[] _nativeOversampledChannelDisplayBuffer = [];
    private double[] _nativeFramePeaks = [];
    private int[] _nativeFrameOutputCounts = [];
    private readonly double[] _managedOversampledDisplayBuffer;
    private readonly double[] _managedOversampledChannelDisplayBuffer;
    private bool _channelWaveformEnabled;
    private bool _channelSpectrumEnabled;
    private bool _nativeTruePeakDisabled;
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
        _truePeakWindowFrames = MillisecondsToFrames(100);
        _truePeakEstimator = new TruePeakEstimator(
            options.Channels,
            options.TruePeakOversampling,
            _truePeakWindowFrames);
        _managedOversampledDisplayBuffer = new double[options.TruePeakOversampling];
        _managedOversampledChannelDisplayBuffer = new double[options.TruePeakOversampling * options.Channels];
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

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        _recentSamples.SetPerformanceMode(mode);
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

        var frameCount = interleavedSamples.Length / _options.Channels;
        if (TryProcessWithNativeTruePeak(interleavedSamples, frameCount))
        {
            _processedFrames += frameCount;
            RefreshSnapshot();
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
                _managedOversampledDisplayBuffer,
                _managedOversampledChannelDisplayBuffer);
            _recentSamples.Add(
                activeFrame,
                _managedOversampledDisplayBuffer.AsSpan(0, oversampledCount),
                _managedOversampledChannelDisplayBuffer.AsSpan(0, oversampledCount * _options.Channels));
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
        _nativeTruePeakEstimator?.Reset();
        _snapshot = new PcmMetricsSnapshot();
    }

    public void Dispose()
    {
        _nativeTruePeakEstimator?.Dispose();
        _nativeTruePeakEstimator = null;
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
            TruePeakDb: HasTruePeakSamples ? ToDb(CurrentTruePeak) : null,
            Rms100MsDb: seconds >= 0.1 ? ToDb(_recentSamples.Rms) : null,
            StereoImageX: _recentSamples.StereoImageX,
            StereoImageY: _recentSamples.StereoImageY,
            StereoImagePoints: _recentSamples.StereoImagePoints,
            Correlation: _recentSamples.Correlation,
            WaveformSamples: _recentSamples.WaveformSamples,
            ChannelWaveformSamples: _channelWaveformEnabled ? _recentSamples.ChannelWaveformSamples : null,
            SpectrumBuckets: _recentSamples.SpectrumBuckets,
            ChannelSpectrumBuckets: _channelSpectrumEnabled ? _recentSamples.ChannelSpectrumBuckets : null,
            PlotFrameVersion: _processedFrames);
    }

    private bool HasTruePeakSamples => _nativeTruePeakEstimator is not null
        ? _processedFrames > 0
        : _truePeakEstimator.HasSamples;

    private double CurrentTruePeak => _nativeTruePeakEstimator?.TruePeak ?? _truePeakEstimator.TruePeak;

    private bool TryProcessWithNativeTruePeak(ReadOnlySpan<float> interleavedSamples, int frameCount)
    {
        if (_nativeTruePeakDisabled)
        {
            return false;
        }

        try
        {
            _nativeTruePeakEstimator ??= new NativeRfRapidTruePeakEstimator(
                _options.Channels,
                _options.TruePeakOversampling,
                _truePeakWindowFrames);
            EnsureNativeWorkspace(frameCount);

            for (var index = 0; index < interleavedSamples.Length; index++)
            {
                _nativeInputBuffer[index] = NormalizeSample(interleavedSamples[index]);
            }

            _nativeTruePeakEstimator.Process(
                _nativeInputBuffer,
                frameCount,
                _nativeOversampledDisplayBuffer,
                _nativeOversampledChannelDisplayBuffer,
                _nativeFramePeaks,
                _nativeFrameOutputCounts);

            for (var frame = 0; frame < frameCount; frame++)
            {
                var weightedPower = 0d;
                var sampleOffset = frame * _options.Channels;
                for (var channel = 0; channel < _options.Channels; channel++)
                {
                    var sample = _nativeInputBuffer[sampleOffset + channel];
                    _frameBuffer[channel] = sample;

                    var weighted = _kWeightingFilters[channel].Process(sample);
                    weightedPower += weighted * weighted;
                }

                var activeFrame = _frameBuffer.AsSpan(0, _options.Channels);
                var oversampledOffset = frame * _options.TruePeakOversampling;
                var oversampledCount = _nativeFrameOutputCounts[frame];
                _recentSamples.Add(
                    activeFrame,
                    _nativeOversampledDisplayBuffer.AsSpan(oversampledOffset, oversampledCount),
                    _nativeOversampledChannelDisplayBuffer.AsSpan(
                        oversampledOffset * _options.Channels,
                        oversampledCount * _options.Channels));
                _momentaryWindow.Add(weightedPower);
                _shortTermWindow.Add(weightedPower);
                _integratedGate.Add(weightedPower);
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException
            or EntryPointNotFoundException
            or BadImageFormatException
            or InvalidOperationException
            or System.Runtime.InteropServices.SEHException)
        {
            _nativeTruePeakEstimator?.Dispose();
            _nativeTruePeakEstimator = null;
            _nativeTruePeakDisabled = true;
            return false;
        }
    }

    private void EnsureNativeWorkspace(int frameCount)
    {
        var sampleCount = frameCount * _options.Channels;
        var oversampledFrameCount = frameCount * _options.TruePeakOversampling;
        var oversampledSampleCount = oversampledFrameCount * _options.Channels;

        if (_nativeInputBuffer.Length < sampleCount)
        {
            _nativeInputBuffer = new double[sampleCount];
        }

        if (_nativeOversampledDisplayBuffer.Length < oversampledFrameCount)
        {
            _nativeOversampledDisplayBuffer = new double[oversampledFrameCount];
        }

        if (_nativeOversampledChannelDisplayBuffer.Length < oversampledSampleCount)
        {
            _nativeOversampledChannelDisplayBuffer = new double[oversampledSampleCount];
        }

        if (_nativeFramePeaks.Length < frameCount)
        {
            _nativeFramePeaks = new double[frameCount];
        }

        if (_nativeFrameOutputCounts.Length < frameCount)
        {
            _nativeFrameOutputCounts = new int[frameCount];
        }
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
