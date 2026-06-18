namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed record PcmMetricsSnapshot(
    double? MomentaryLufs = null,
    double? ShortTermLufs = null,
    double? IntegratedLufs = null,
    double? TruePeakDb = null,
    double? Rms100MsDb = null,
    double? StereoImageX = null,
    double? StereoImageY = null,
    IReadOnlyList<StereoImagePoint>? StereoImagePoints = null,
    double? Correlation = null,
    IReadOnlyList<double>? WaveformSamples = null,
    IReadOnlyList<IReadOnlyList<double>>? ChannelWaveformSamples = null,
    IReadOnlyList<double>? SpectrumBuckets = null,
    IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets = null);
