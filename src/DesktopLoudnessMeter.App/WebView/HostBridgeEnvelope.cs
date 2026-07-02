using System.Text.Json.Serialization;

namespace DesktopLoudnessMeter.App.WebView;

public sealed record HostBridgeRequest(
    int Version,
    string? Id,
    string? Kind,
    string? Command,
    HostBridgeRequestPayload? Payload);

public sealed record HostBridgeRequestPayload(
    string? SourceKey = null,
    bool? ShowHiddenSources = null,
    string? ThemeId = null,
    int? MaxRecentEventEntries = null,
    string? EventOutputFolder = null,
    string? PerformanceMode = null,
    bool? IsAlwaysOnTop = null,
    string? DisplayMode = null,
    bool? TruePeakEnabled = null,
    string? MoveDirection = null,
    string? SourceKind = null,
    int? ProcessId = null,
    string? TruePeakOversampling = null,
    string? WaveformDisplayMode = null,
    string? SpectrumDisplayMode = null,
    string? ModuleKind = null,
    string? TargetModuleKind = null,
    bool? IsVisible = null,
    double? ModuleHeight = null,
    bool? InsertAfter = null,
    double? SpectrumDisplayFloorDb = null,
    double? SpectrumDisplayCeilingDb = null,
    double? LoudnessClockMinimumDb = null,
    double? LoudnessClockMaximumDb = null,
    double? LoudnessClockWarningDb = null,
    double? LoudnessClockCycleSeconds = null,
    double? CollisionLevelFloorDb = null,
    double? CollisionProminenceThresholdDb = null,
    double? CollisionRelativeThreshold = null,
    string? CollisionFrequencyScale = null,
    string? InlineCollisionSourceKey = null);

public sealed record HostBridgeResponse(
    int Version,
    string Id,
    string Kind,
    bool Ok,
    HostSnapshotDto? Snapshot = null,
    HostCommandResultDto? Result = null,
    HostBridgeError? Error = null)
{
    public static HostBridgeResponse ForSnapshot(string id, HostSnapshotDto snapshot)
    {
        return new HostBridgeResponse(1, id, "snapshot", Ok: true, Snapshot: snapshot);
    }

    public static HostBridgeResponse ForCommandResult(string id, HostCommandResultDto result)
    {
        return new HostBridgeResponse(1, id, "commandResult", result.Success, Result: result);
    }

    public static HostBridgeResponse ForError(string id, HostBridgeError error)
    {
        return new HostBridgeResponse(1, id, "error", Ok: false, Error: error);
    }
}

public sealed record HostBridgeError(
    string Code,
    string Message,
    string? TargetSourceKey = null,
    string? Detail = null)
{
    public static HostBridgeError InvalidRequest(string message)
    {
        return new HostBridgeError("invalidRequest", message);
    }

    public static HostBridgeError FromHostError(HostCommandErrorDto error)
    {
        return new HostBridgeError(error.Code, error.Message, error.TargetSourceKey, error.Detail);
    }
}

public sealed record HostSnapshotDto(
    IReadOnlyList<HostSourceSnapshotDto> Sources,
    HostDetailSnapshotDto? SelectedDetail,
    HostSettingsSnapshotDto Settings,
    IReadOnlyList<HostRecentEventSnapshotDto> RecentEvents,
    string StatusText,
    HostCommandAvailabilityDto Commands);

public sealed record HostSourceSnapshotDto(
    string SourceKey,
    string SourceKind,
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
    double Peak,
    double PeakDb,
    string PeakDbText,
    string State,
    string TruePeakOversampling,
    string WaveformDisplayMode,
    string SpectrumDisplayMode,
    double? MomentaryLufs,
    double? ShortTermLufs,
    double? IntegratedLufs,
    double? TruePeakDb,
    double? Rms100MsDb,
    double? StereoImageX,
    double? StereoImageY,
    IReadOnlyList<StereoImagePointDto>? StereoImagePoints,
    double? Correlation,
    IReadOnlyList<double>? WaveformSamples,
    IReadOnlyList<IReadOnlyList<double>>? ChannelWaveformSamples,
    IReadOnlyList<double>? SpectrumBuckets,
    IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets,
    long PlotFrameVersion);

public sealed record StereoImagePointDto(double Angle, double Radius);

public sealed record HostDetailSnapshotDto(
    HostSourceSnapshotDto Source,
    IReadOnlyList<HostDetailModuleSnapshotDto> Modules,
    bool IsWaveformVisible,
    bool IsSpectrumVisible,
    bool IsStereoVisible,
    bool IsLoudnessClockVisible,
    bool IsCollisionVisible,
    HostFrequencyCollisionSnapshotDto? Collision);

public sealed record HostDetailModuleSnapshotDto(
    string Kind,
    string Title,
    int OrderIndex,
    bool IsVisible,
    double Height);

public sealed record HostFrequencyCollisionSnapshotDto(
    string? ComparisonSourceKey,
    string? ComparisonDisplayName,
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    string FrequencyScale,
    IReadOnlyList<FrequencyCollisionBandDto> Bands);

public sealed record FrequencyCollisionBandDto(
    double StartRatio,
    double EndRatio,
    double Intensity);

public sealed record HostRecentEventSnapshotDto(
    string TimeText,
    string Kind,
    string Title,
    string Detail,
    string Severity);

public sealed record HostSettingsSnapshotDto(
    int SchemaVersion,
    string? ThemeId,
    int? MaxRecentEventEntries,
    string? EventOutputFolder,
    bool LoadDefaultDetailLayoutFromFile,
    string? DefaultDetailLayoutFile,
    bool ShowHiddenSources,
    IReadOnlyList<HostSourceSettingsSnapshotDto> Sources);

public sealed record HostSourceSettingsSnapshotDto(
    string SourceKey,
    string? Alias,
    bool IsHidden,
    int? OrderIndex,
    string? TruePeakOversampling,
    string? WaveformDisplayMode,
    string? SpectrumDisplayMode,
    bool? InlineStereoVisible,
    bool? InlineWaveformVisible,
    bool? InlineSpectrumVisible,
    bool? InlineLoudnessClockVisible,
    bool? InlineCollisionVisible,
    string? InlineCollisionSourceKey,
    double? CollisionLevelFloorDb,
    double? CollisionProminenceThresholdDb,
    double? CollisionRelativeThreshold,
    string? CollisionFrequencyScale,
    double? SpectrumDisplayFloorDb,
    double? SpectrumDisplayCeilingDb,
    double? LoudnessClockMinimumDb,
    double? LoudnessClockMaximumDb,
    double? LoudnessClockWarningDb,
    double? LoudnessClockCycleSeconds);

public sealed record HostCommandAvailabilityDto(
    bool CanBackToGlobal,
    bool CanResetAll,
    bool CanForgetSelectedSource,
    bool CanMoveSelectedSourceUp,
    bool CanMoveSelectedSourceDown);

public sealed record HostCommandResultDto(
    string CommandKind,
    bool Success,
    HostSnapshotDto Snapshot,
    HostCommandErrorDto? Error);

public sealed record HostCommandErrorDto(
    string Code,
    string Message,
    string? TargetSourceKey,
    string? Detail);

[JsonSerializable(typeof(HostBridgeResponse))]
internal sealed partial class HostBridgeJsonSerializerContext : JsonSerializerContext;
