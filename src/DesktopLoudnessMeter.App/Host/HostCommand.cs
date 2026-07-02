using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

public enum HostCommandKind
{
    Refresh,
    OpenNativeUi,
    OpenGlobalSettings,
    BeginWindowDrag,
    ToggleWindowState,
    HideToTray,
    CloseWindow,
    SetAlwaysOnTop,
    SetDisplayMode,
    SetTruePeakEnabled,
    SaveDetailLayout,
    LoadDetailLayout,
    OpenStereoSettings,
    BackToGlobal,
    ResetAll,
    SetTheme,
    SetPerformanceMode,
    SetRecentEventLimit,
    SetEventOutputFolder,
    SetShowHiddenSources,
    SelectDetail,
    OpenPopup,
    ResetSource,
    SetSourceAlias,
    HideSource,
    RestoreSource,
    MoveSource,
    MoveSourceToIndex,
    ForgetSource,
    SetTruePeakOversampling,
    SetWaveformDisplayMode,
    SetSpectrumDisplayMode,
    SetDetailModuleVisibility,
    SetDetailModuleHeight,
    MoveDetailModule,
    SetSpectrumDisplayRange,
    SetLoudnessClockSettings,
    SetCollisionSettings
}

public enum HostCommandScope
{
    Global,
    Source
}

public enum HostSourceMoveDirection
{
    Up,
    Down
}

public enum HostPerformanceMode
{
    Normal,
    Eco
}

public abstract record HostCommand(HostCommandKind Kind, HostCommandScope Scope)
{
    public virtual string? TargetSourceKey => null;
}

public sealed record RefreshHostCommand()
    : HostCommand(HostCommandKind.Refresh, HostCommandScope.Global);

public sealed record OpenNativeUiHostCommand()
    : HostCommand(HostCommandKind.OpenNativeUi, HostCommandScope.Global);

public sealed record OpenGlobalSettingsHostCommand()
    : HostCommand(HostCommandKind.OpenGlobalSettings, HostCommandScope.Global);

public sealed record BeginWindowDragHostCommand()
    : HostCommand(HostCommandKind.BeginWindowDrag, HostCommandScope.Global);

public sealed record ToggleWindowStateHostCommand()
    : HostCommand(HostCommandKind.ToggleWindowState, HostCommandScope.Global);

public sealed record HideToTrayHostCommand()
    : HostCommand(HostCommandKind.HideToTray, HostCommandScope.Global);

public sealed record CloseWindowHostCommand()
    : HostCommand(HostCommandKind.CloseWindow, HostCommandScope.Global);

public sealed record SetAlwaysOnTopHostCommand(bool IsAlwaysOnTop)
    : HostCommand(HostCommandKind.SetAlwaysOnTop, HostCommandScope.Global);

public sealed record SetDisplayModeHostCommand(MeterDisplayMode DisplayMode)
    : HostCommand(HostCommandKind.SetDisplayMode, HostCommandScope.Global);

public sealed record SetTruePeakEnabledHostCommand(bool IsEnabled)
    : HostCommand(HostCommandKind.SetTruePeakEnabled, HostCommandScope.Global);

public sealed record SaveDetailLayoutHostCommand()
    : HostCommand(HostCommandKind.SaveDetailLayout, HostCommandScope.Global);

public sealed record LoadDetailLayoutHostCommand()
    : HostCommand(HostCommandKind.LoadDetailLayout, HostCommandScope.Global);

public sealed record OpenStereoSettingsHostCommand()
    : HostCommand(HostCommandKind.OpenStereoSettings, HostCommandScope.Global);

public sealed record BackToGlobalHostCommand()
    : HostCommand(HostCommandKind.BackToGlobal, HostCommandScope.Global);

public sealed record ResetAllHostCommand()
    : HostCommand(HostCommandKind.ResetAll, HostCommandScope.Global);

public sealed record SetThemeHostCommand(string? ThemeId)
    : HostCommand(HostCommandKind.SetTheme, HostCommandScope.Global);

public sealed record SetPerformanceModeHostCommand(HostPerformanceMode Mode)
    : HostCommand(HostCommandKind.SetPerformanceMode, HostCommandScope.Global);

public sealed record SetRecentEventLimitHostCommand(int MaxRecentEventEntries)
    : HostCommand(HostCommandKind.SetRecentEventLimit, HostCommandScope.Global);

public sealed record SetEventOutputFolderHostCommand(string? Folder)
    : HostCommand(HostCommandKind.SetEventOutputFolder, HostCommandScope.Global);

public sealed record SetShowHiddenSourcesHostCommand(bool ShowHiddenSources)
    : HostCommand(HostCommandKind.SetShowHiddenSources, HostCommandScope.Global);

public abstract record HostSourceCommand(
    HostCommandKind Kind,
    string SourceKey,
    SignalSourceKind? TargetSourceKind = null,
    int? ProcessId = null)
    : HostCommand(Kind, HostCommandScope.Source)
{
    public override string TargetSourceKey => SourceKey;
}

public sealed record SelectDetailHostCommand(string SourceKey)
    : HostSourceCommand(HostCommandKind.SelectDetail, SourceKey);

public sealed record OpenPopupHostCommand(string SourceKey)
    : HostSourceCommand(HostCommandKind.OpenPopup, SourceKey);

public sealed record ResetSourceHostCommand(string SourceKey, SignalSourceKind SourceKind, int? ProcessId)
    : HostSourceCommand(HostCommandKind.ResetSource, SourceKey, SourceKind, ProcessId);

public sealed record SetSourceAliasHostCommand(string SourceKey, string? Alias)
    : HostSourceCommand(HostCommandKind.SetSourceAlias, SourceKey);

public sealed record HideSourceHostCommand(string SourceKey)
    : HostSourceCommand(HostCommandKind.HideSource, SourceKey);

public sealed record RestoreSourceHostCommand(string SourceKey)
    : HostSourceCommand(HostCommandKind.RestoreSource, SourceKey);

public sealed record MoveSourceHostCommand(string SourceKey, HostSourceMoveDirection Direction)
    : HostSourceCommand(HostCommandKind.MoveSource, SourceKey);

public sealed record MoveSourceToIndexHostCommand(string SourceKey, int TargetIndex)
    : HostSourceCommand(HostCommandKind.MoveSourceToIndex, SourceKey);

public sealed record ForgetSourceHostCommand(string SourceKey)
    : HostSourceCommand(HostCommandKind.ForgetSource, SourceKey);

public sealed record SetTruePeakOversamplingHostCommand(
    string SourceKey,
    SignalSourceKind SourceKind,
    int? ProcessId,
    TruePeakOversampling Oversampling)
    : HostSourceCommand(HostCommandKind.SetTruePeakOversampling, SourceKey, SourceKind, ProcessId);

public sealed record SetWaveformDisplayModeHostCommand(
    string SourceKey,
    SignalSourceKind SourceKind,
    int? ProcessId,
    WaveformDisplayMode DisplayMode)
    : HostSourceCommand(HostCommandKind.SetWaveformDisplayMode, SourceKey, SourceKind, ProcessId);

public sealed record SetSpectrumDisplayModeHostCommand(
    string SourceKey,
    SignalSourceKind SourceKind,
    int? ProcessId,
    SpectrumDisplayMode DisplayMode)
    : HostSourceCommand(HostCommandKind.SetSpectrumDisplayMode, SourceKey, SourceKind, ProcessId);

public sealed record SetDetailModuleVisibilityHostCommand(
    string SourceKey,
    DetailModuleKind ModuleKind,
    bool IsVisible)
    : HostSourceCommand(HostCommandKind.SetDetailModuleVisibility, SourceKey);

public sealed record SetDetailModuleHeightHostCommand(
    string SourceKey,
    DetailModuleKind ModuleKind,
    double Height)
    : HostSourceCommand(HostCommandKind.SetDetailModuleHeight, SourceKey);

public sealed record MoveDetailModuleHostCommand(
    string SourceKey,
    DetailModuleKind ModuleKind,
    DetailModuleKind TargetModuleKind,
    bool InsertAfter)
    : HostSourceCommand(HostCommandKind.MoveDetailModule, SourceKey);

public sealed record SetSpectrumDisplayRangeHostCommand(
    string SourceKey,
    double FloorDb,
    double CeilingDb)
    : HostSourceCommand(HostCommandKind.SetSpectrumDisplayRange, SourceKey);

public sealed record SetLoudnessClockSettingsHostCommand(
    string SourceKey,
    double MinimumDb,
    double MaximumDb,
    double WarningDb,
    double CycleSeconds)
    : HostSourceCommand(HostCommandKind.SetLoudnessClockSettings, SourceKey);

public sealed record SetCollisionSettingsHostCommand(
    string SourceKey,
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    CollisionFrequencyScale FrequencyScale,
    string? ComparisonSourceKey)
    : HostSourceCommand(HostCommandKind.SetCollisionSettings, SourceKey);
