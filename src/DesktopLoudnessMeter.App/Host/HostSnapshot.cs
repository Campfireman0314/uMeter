using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

public sealed record HostSnapshot(
    IReadOnlyList<HostSourceSnapshot> Sources,
    HostDetailSnapshot? SelectedDetail,
    HostSettingsSnapshot Settings,
    IReadOnlyList<HostRecentEventSnapshot> RecentEvents,
    string StatusText,
    HostCommandAvailability Commands)
{
    public static HostSnapshot Empty { get; } = new(
        Array.Empty<HostSourceSnapshot>(),
        null,
        HostSettingsSnapshot.Empty,
        Array.Empty<HostRecentEventSnapshot>(),
        "Waiting for audio sources",
        HostCommandAvailability.Empty);
}

public sealed record HostDetailSnapshot(
    HostSourceSnapshot Source,
    IReadOnlyList<HostDetailModuleSnapshot> Modules,
    bool IsWaveformVisible,
    bool IsSpectrumVisible,
    bool IsStereoVisible,
    bool IsLoudnessClockVisible,
    bool IsCollisionVisible,
    HostFrequencyCollisionSnapshot? Collision = null);

public sealed record HostDetailModuleSnapshot(
    DetailModuleKind Kind,
    string Title,
    int OrderIndex,
    bool IsVisible,
    double Height);

public sealed record HostFrequencyCollisionSnapshot(
    string? ComparisonSourceKey,
    string? ComparisonDisplayName,
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    CollisionFrequencyScale FrequencyScale,
    IReadOnlyList<FrequencyCollisionBand> Bands);

public sealed record HostRecentEventSnapshot(
    string TimeText,
    RecentEventKind Kind,
    string Title,
    string Detail,
    ReadoutSeverity Severity)
{
    public static HostRecentEventSnapshot FromViewModel(RecentEventViewModel value)
    {
        return new HostRecentEventSnapshot(
            value.TimeText,
            value.Kind,
            value.Title,
            value.Detail,
            value.Severity);
    }
}

public sealed record HostCommandAvailability(
    bool CanBackToGlobal,
    bool CanResetAll,
    bool CanForgetSelectedSource,
    bool CanMoveSelectedSourceUp,
    bool CanMoveSelectedSourceDown)
{
    public static HostCommandAvailability Empty { get; } = new(
        CanBackToGlobal: false,
        CanResetAll: false,
        CanForgetSelectedSource: false,
        CanMoveSelectedSourceUp: false,
        CanMoveSelectedSourceDown: false);
}
