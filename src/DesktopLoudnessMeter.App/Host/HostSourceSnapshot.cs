using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

public sealed record HostSourceWorkspaceState(
    string? Alias = null,
    bool IsHidden = false,
    int? OrderIndex = null,
    TruePeakOversampling? TruePeakOversampling = null,
    WaveformDisplayMode WaveformDisplayMode = WaveformDisplayMode.Overall,
    SpectrumDisplayMode SpectrumDisplayMode = SpectrumDisplayMode.Overall);

public sealed record HostSourceSnapshot(
    string SourceKey,
    SignalSourceKind SourceKind,
    string SourceKindLabel,
    int? ProcessId,
    string DisplayName,
    string? Alias,
    string EffectiveDisplayName,
    string DisplayTitle,
    string SourceIdentityText,
    bool IsHidden,
    int? OrderIndex,
    bool IsInstanceMissing,
    float Peak,
    double PeakDb,
    string PeakDbText,
    string State,
    TruePeakOversampling TruePeakOversampling,
    WaveformDisplayMode WaveformDisplayMode,
    SpectrumDisplayMode SpectrumDisplayMode,
    double? MomentaryLufs,
    double? ShortTermLufs,
    double? IntegratedLufs,
    double? TruePeakDb,
    double? Rms100MsDb,
    double? StereoImageX,
    double? StereoImageY,
    IReadOnlyList<StereoImagePoint>? StereoImagePoints,
    double? Correlation,
    IReadOnlyList<double>? WaveformSamples,
    IReadOnlyList<IReadOnlyList<double>>? ChannelWaveformSamples,
    IReadOnlyList<double>? SpectrumBuckets,
    IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets,
    long PlotFrameVersion)
{
    public SignalSourceSnapshot ToSignalSourceSnapshot()
    {
        return new SignalSourceSnapshot(
            SourceKey,
            SourceKind,
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
            ChannelSpectrumBuckets,
            PlotFrameVersion);
    }

    public static HostSourceSnapshot FromSignalSource(
        SignalSourceSnapshot source,
        HostSourceWorkspaceState? workspaceState = null,
        bool isInstanceMissing = false,
        bool hasDisplayNameCollision = false)
    {
        workspaceState ??= new HostSourceWorkspaceState();
        var alias = NormalizeAlias(workspaceState.Alias);
        var effectiveDisplayName = alias ?? source.DisplayName;
        var sourceQualifier = FormatSourceQualifier(source);

        return new HostSourceSnapshot(
            source.SourceKey,
            source.SourceKind,
            source.SourceKindLabel,
            source.ProcessId,
            source.DisplayName,
            alias,
            effectiveDisplayName,
            hasDisplayNameCollision ? $"{effectiveDisplayName} - {sourceQualifier}" : effectiveDisplayName,
            FormatSourceIdentityText(source),
            workspaceState.IsHidden,
            NormalizeOrderIndex(workspaceState.OrderIndex),
            isInstanceMissing,
            source.Peak,
            source.PeakDb,
            source.PeakDbText,
            source.State,
            workspaceState.TruePeakOversampling ?? TruePeakOversampling.FourX,
            workspaceState.WaveformDisplayMode,
            workspaceState.SpectrumDisplayMode,
            source.MomentaryLufs,
            source.ShortTermLufs,
            source.IntegratedLufs,
            source.TruePeakDb,
            source.Rms100MsDb,
            source.StereoImageX,
            source.StereoImageY,
            source.StereoImagePoints,
            source.Correlation,
            source.WaveformSamples,
            source.ChannelWaveformSamples,
            source.SpectrumBuckets,
            source.ChannelSpectrumBuckets,
            source.PlotFrameVersion);
    }

    internal static HostSourceSnapshot FromNativeProjection(
        SignalSourceSnapshot source,
        NativeHostSourceProjectionRow projection,
        bool isInstanceMissing = false)
    {
        return new HostSourceSnapshot(
            source.SourceKey,
            source.SourceKind,
            projection.SourceKindLabel,
            source.ProcessId,
            source.DisplayName,
            projection.Alias,
            projection.EffectiveDisplayName,
            projection.DisplayTitle,
            projection.SourceIdentityText,
            projection.IsHidden,
            projection.OrderIndex,
            isInstanceMissing,
            source.Peak,
            source.PeakDb,
            source.PeakDbText,
            source.State,
            projection.TruePeakOversampling,
            projection.WaveformDisplayMode,
            projection.SpectrumDisplayMode,
            source.MomentaryLufs,
            source.ShortTermLufs,
            source.IntegratedLufs,
            source.TruePeakDb,
            source.Rms100MsDb,
            source.StereoImageX,
            source.StereoImageY,
            source.StereoImagePoints,
            source.Correlation,
            source.WaveformSamples,
            source.ChannelWaveformSamples,
            source.SpectrumBuckets,
            source.ChannelSpectrumBuckets,
            source.PlotFrameVersion);
    }

    private static string? NormalizeAlias(string? alias)
    {
        return string.IsNullOrWhiteSpace(alias) ? null : alias.Trim();
    }

    private static int? NormalizeOrderIndex(int? orderIndex)
    {
        return orderIndex is int value && value >= 0 ? value : null;
    }

    private static string FormatSourceIdentityText(SignalSourceSnapshot source)
    {
        if (source.SourceKind == SignalSourceKind.WasapiSession && source.ProcessId is int processId)
        {
            return $"{source.SourceKindLabel} source - PID {processId}";
        }

        var stableId = CompactSourceIdentifier(source.SourceKey);
        return string.IsNullOrWhiteSpace(stableId)
            ? $"{source.SourceKindLabel} source"
            : $"{source.SourceKindLabel} source - {stableId}";
    }

    private static string FormatSourceQualifier(SignalSourceSnapshot source)
    {
        if (source.SourceKind == SignalSourceKind.WasapiSession && source.ProcessId is int processId)
        {
            return $"{source.SourceKindLabel} PID {processId}";
        }

        var stableId = CompactSourceIdentifier(source.SourceKey);
        return string.IsNullOrWhiteSpace(stableId)
            ? source.SourceKindLabel
            : $"{source.SourceKindLabel} {stableId}";
    }

    private static string CompactSourceIdentifier(string sourceKey)
    {
        var separatorIndex = sourceKey.IndexOf(':', StringComparison.Ordinal);
        var value = separatorIndex >= 0 && separatorIndex < sourceKey.Length - 1
            ? sourceKey[(separatorIndex + 1)..]
            : sourceKey;

        return value.Length <= 24 ? value : $"{value[..21]}...";
    }
}
