using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Audio;

public record SignalSourceSnapshot(
    string SourceKey,
    SignalSourceKind SourceKind,
    int? ProcessId,
    string DisplayName,
    float Peak,
    double PeakDb,
    string PeakDbText,
    string State,
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
    IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets = null,
    long PlotFrameVersion = 0)
{
    public string SourceKindLabel => SourceKind.ToDisplayLabel();
}
