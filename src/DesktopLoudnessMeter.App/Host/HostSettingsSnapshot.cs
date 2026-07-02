using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

public sealed record HostSettingsSnapshot(
    int SchemaVersion,
    string? ThemeId,
    int? MaxRecentEventEntries,
    string? EventOutputFolder,
    bool LoadDefaultDetailLayoutFromFile,
    string? DefaultDetailLayoutFile,
    bool ShowHiddenSources,
    IReadOnlyList<HostSourceSettingsSnapshot> Sources)
{
    public static HostSettingsSnapshot Empty { get; } = new(
        UserWorkspaceSettings.CurrentSchemaVersion,
        null,
        null,
        null,
        false,
        null,
        false,
        Array.Empty<HostSourceSettingsSnapshot>());

    public static HostSettingsSnapshot FromWorkspace(UserWorkspaceSettings workspace, bool showHiddenSources = false)
    {
        return new HostSettingsSnapshot(
            UserWorkspaceSettings.NormalizeSchemaVersion(workspace.SchemaVersion),
            workspace.ThemeId,
            workspace.MaxRecentEventEntries,
            workspace.EventOutputFolder,
            workspace.LoadDefaultDetailLayoutFromFile,
            workspace.DefaultDetailLayoutFile,
            showHiddenSources,
            workspace.Sources
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => HostSourceSettingsSnapshot.FromWorkspace(pair.Key, pair.Value))
                .ToArray());
    }
}

public sealed record HostSourceSettingsSnapshot(
    string SourceKey,
    string? Alias,
    bool IsHidden,
    int? OrderIndex,
    TruePeakOversampling? TruePeakOversampling,
    WaveformDisplayMode? WaveformDisplayMode,
    SpectrumDisplayMode? SpectrumDisplayMode,
    bool? InlineStereoVisible,
    bool? InlineWaveformVisible,
    bool? InlineSpectrumVisible,
    bool? InlineLoudnessClockVisible,
    bool? InlineCollisionVisible,
    string? InlineCollisionSourceKey,
    double? CollisionLevelFloorDb,
    double? CollisionProminenceThresholdDb,
    double? CollisionRelativeThreshold,
    CollisionFrequencyScale? CollisionFrequencyScale,
    double? SpectrumDisplayFloorDb,
    double? SpectrumDisplayCeilingDb,
    double? LoudnessClockMinimumDb,
    double? LoudnessClockMaximumDb,
    double? LoudnessClockWarningDb,
    double? LoudnessClockCycleSeconds)
{
    public static HostSourceSettingsSnapshot FromWorkspace(string sourceKey, SourceWorkspaceSettings source)
    {
        return new HostSourceSettingsSnapshot(
            sourceKey,
            source.Alias,
            source.IsHidden,
            source.OrderIndex,
            source.TruePeakOversampling,
            source.WaveformDisplayMode,
            source.SpectrumDisplayMode,
            source.InlineStereoVisible,
            source.InlineWaveformVisible,
            source.InlineSpectrumVisible,
            source.InlineLoudnessClockVisible,
            source.InlineCollisionVisible,
            source.InlineCollisionSourceKey,
            source.CollisionLevelFloorDb,
            source.CollisionProminenceThresholdDb,
            source.CollisionRelativeThreshold,
            source.CollisionFrequencyScale,
            source.SpectrumDisplayFloorDb,
            source.SpectrumDisplayCeilingDb,
            source.LoudnessClockMinimumDb,
            source.LoudnessClockMaximumDb,
            source.LoudnessClockWarningDb,
            source.LoudnessClockCycleSeconds);
    }
}
