using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Audio;

public sealed record AudioSessionSnapshot : SignalSourceSnapshot
{
    public AudioSessionSnapshot(
        string SessionKey,
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
        IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets = null)
        : base(
            SessionKey,
            SignalSourceKind.WasapiSession,
            ProcessId,
            DisplayName,
            Peak,
            PeakDb,
            PeakDbText,
            State,
            MomentaryLufs,
            ShortTermLufs,
            IntegratedLufs,
            TruePeakDb,
            Rms100MsDb,
            StereoImageX,
            StereoImageY,
            StereoImagePoints,
            Correlation,
            WaveformSamples,
            ChannelWaveformSamples,
            SpectrumBuckets,
            ChannelSpectrumBuckets)
    {
    }

    public string SessionKey => SourceKey;
}
