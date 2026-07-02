namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed class PcmMetricAnalyzer : IDisposable
{
    private readonly PcmMetricAnalyzerOptions _options;
    private readonly NativePcmAnalyzer _nativeAnalyzer;
    private bool _channelWaveformEnabled;
    private bool _channelSpectrumEnabled;
    private PcmMetricsSnapshot _snapshot = new();

    public PcmMetricAnalyzer(PcmMetricAnalyzerOptions options)
    {
        _options = options;
        _nativeAnalyzer = new NativePcmAnalyzer(options);
    }

    public PcmMetricsSnapshot Snapshot => _snapshot;

    public void SetChannelSpectrumEnabled(bool enabled)
    {
        _channelSpectrumEnabled = enabled;
        _nativeAnalyzer.SetChannelSpectrumEnabled(enabled);
        RefreshSnapshot();
    }

    public void SetChannelWaveformEnabled(bool enabled)
    {
        _channelWaveformEnabled = enabled;
        _nativeAnalyzer.SetChannelWaveformEnabled(enabled);
        RefreshSnapshot();
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        _nativeAnalyzer.SetPerformanceMode(mode);
        RefreshSnapshot();
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

        _nativeAnalyzer.Process(interleavedSamples);
        RefreshSnapshot();
    }

    public void Reset()
    {
        _nativeAnalyzer.Reset();
        _snapshot = new PcmMetricsSnapshot();
    }

    public void Dispose()
    {
        _nativeAnalyzer.Dispose();
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
        var native = _nativeAnalyzer.Snapshot;
        _snapshot = new PcmMetricsSnapshot(
            MomentaryLufs: ToNullable(native.HasMomentaryLufs, native.MomentaryLufs),
            ShortTermLufs: ToNullable(native.HasShortTermLufs, native.ShortTermLufs),
            IntegratedLufs: ToNullable(native.HasIntegratedLufs, native.IntegratedLufs),
            TruePeakDb: ToFiniteNullable(native.HasTruePeakDb, native.TruePeakDb),
            Rms100MsDb: ToNullable(native.HasRms100MsDb, native.Rms100MsDb),
            StereoImageX: ToNullable(native.HasStereoImageX, native.StereoImageX),
            StereoImageY: ToNullable(native.HasStereoImageY, native.StereoImageY),
            StereoImagePoints: _nativeAnalyzer.CopyStereoImagePoints(native.StereoImagePointCount),
            Correlation: ToNullable(native.HasCorrelation, native.Correlation),
            WaveformSamples: _nativeAnalyzer.CopyWaveform(native.WaveformCount),
            ChannelWaveformSamples: _channelWaveformEnabled
                ? _nativeAnalyzer.CopyChannelWaveform(native.ChannelWaveformCount)
                : null,
            SpectrumBuckets: _nativeAnalyzer.CopySpectrum(native.SpectrumCount),
            ChannelSpectrumBuckets: _channelSpectrumEnabled
                ? _nativeAnalyzer.CopyChannelSpectrum(native.ChannelSpectrumCount)
                : null,
            PlotFrameVersion: native.PlotFrameVersion);
    }

    private static double? ToNullable(int hasValue, double value)
    {
        return hasValue == 0 ? null : value;
    }

    private static double? ToFiniteNullable(int hasValue, double value)
    {
        return hasValue == 0 || !double.IsFinite(value) ? null : value;
    }
}
