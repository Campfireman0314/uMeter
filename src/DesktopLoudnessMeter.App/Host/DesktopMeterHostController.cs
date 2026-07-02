using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

public sealed class DesktopMeterHostController : IDisposable
{
    private readonly ISignalSourceMeterService _sourceMeterService;
    private readonly UserWorkspaceSettings _workspaceSettings;
    private readonly HostWorkspaceCommandEffectApplier _workspaceEffects;
    private readonly HostRuntimeServiceEffectApplier _runtimeServiceEffects;
    private readonly HostSnapshotAssembler _snapshotAssembler;
    private readonly string? _sourceWarningText;
    private readonly NativeHostRecentEvents _nativeRecentEvents = new();
    private readonly NativeHostRuntimeState _runtimeState = new();
    private readonly List<HostSourceSnapshot> _sources = [];

    public DesktopMeterHostController(
        ISignalSourceMeterService sourceMeterService,
        UserWorkspaceSettings? workspaceSettings = null,
        string? sourceWarningText = null)
    {
        _sourceMeterService = sourceMeterService;
        _workspaceSettings = workspaceSettings ?? new UserWorkspaceSettings();
        _workspaceEffects = new HostWorkspaceCommandEffectApplier(_workspaceSettings);
        _runtimeServiceEffects = new HostRuntimeServiceEffectApplier(_sourceMeterService);
        _snapshotAssembler = new HostSnapshotAssembler(_workspaceSettings, _nativeRecentEvents);
        _sourceWarningText = string.IsNullOrWhiteSpace(sourceWarningText) ? null : sourceWarningText.Trim();
        Snapshot = BuildSnapshot("Waiting for audio sources");
    }

    public HostSnapshot Snapshot { get; private set; }

    public IReadOnlyList<SignalSourceSnapshot> LastSourceSnapshots { get; private set; } =
        Array.Empty<SignalSourceSnapshot>();

    public bool ShowHiddenSources
    {
        get => _runtimeState.ShowHiddenSources;
        set => _runtimeState.ShowHiddenSources = value;
    }

    public void Dispose()
    {
        _nativeRecentEvents.Dispose();
        _runtimeState.Dispose();
    }

    public HostSnapshot Refresh()
    {
        try
        {
            var snapshots = _sourceMeterService.GetCurrentSources();
            LastSourceSnapshots = snapshots;
            Reconcile(snapshots);
            Snapshot = BuildSnapshot(_sourceWarningText);
        }
        catch (Exception ex)
        {
            Snapshot = BuildSnapshot($"Refresh failed: {ex.Message}");
        }

        return Snapshot;
    }

    public HostCommandResult Execute(HostCommand command)
    {
        try
        {
            return command switch
            {
                RefreshHostCommand => HostCommandResult.Ok(command, Refresh()),
                BackToGlobalHostCommand => SelectGlobal(command),
                ResetAllHostCommand => ResetAll(command),
                SetThemeHostCommand setTheme => SetTheme(command, setTheme.ThemeId),
                SetPerformanceModeHostCommand setMode => SetPerformanceMode(command, setMode.Mode),
                SetRecentEventLimitHostCommand setLimit => SetRecentEventLimit(command, setLimit.MaxRecentEventEntries),
                SetEventOutputFolderHostCommand setFolder => SetEventOutputFolder(command, setFolder.Folder),
                SetShowHiddenSourcesHostCommand setShowHidden => SetShowHidden(command, setShowHidden.ShowHiddenSources),
                SelectDetailHostCommand select => SelectSource(command, select.SourceKey),
                ResetSourceHostCommand reset => ResetSource(command, reset.SourceKey, reset.SourceKind, reset.ProcessId),
                SetSourceAliasHostCommand alias => SetSourceAlias(command, alias.SourceKey, alias.Alias),
                HideSourceHostCommand hide => SetSourceHidden(command, hide.SourceKey, isHidden: true),
                RestoreSourceHostCommand restore => SetSourceHidden(command, restore.SourceKey, isHidden: false),
                MoveSourceHostCommand move => MoveSource(command, move.SourceKey, move.Direction),
                MoveSourceToIndexHostCommand moveToIndex => MoveSourceToIndex(command, moveToIndex.SourceKey, moveToIndex.TargetIndex),
                ForgetSourceHostCommand forget => ForgetSource(command, forget.SourceKey),
                SetTruePeakOversamplingHostCommand truePeak => SetTruePeakOversampling(command, truePeak),
                SetWaveformDisplayModeHostCommand waveform => SetWaveformDisplayMode(command, waveform),
                SetSpectrumDisplayModeHostCommand spectrum => SetSpectrumDisplayMode(command, spectrum),
                SetDetailModuleVisibilityHostCommand moduleVisibility => SetDetailModuleVisibility(command, moduleVisibility),
                SetDetailModuleHeightHostCommand moduleHeight => SetDetailModuleHeight(command, moduleHeight),
                MoveDetailModuleHostCommand moveModule => MoveDetailModule(command, moveModule),
                SetSpectrumDisplayRangeHostCommand spectrumRange => SetSpectrumDisplayRange(command, spectrumRange),
                SetLoudnessClockSettingsHostCommand clockSettings => SetLoudnessClockSettings(command, clockSettings),
                SetCollisionSettingsHostCommand collisionSettings => SetCollisionSettings(command, collisionSettings),
                _ => HostCommandResult.Fail(command, Snapshot, HostCommandError.Invalid("Unknown host command."))
            };
        }
        catch (Exception ex)
        {
            Snapshot = BuildSnapshot(Snapshot.StatusText);
            return HostCommandResult.Fail(
                command,
                Snapshot,
                HostCommandError.Failed("Host command failed.", command.TargetSourceKey, ex.Message));
        }
    }

    private void Reconcile(IReadOnlyList<SignalSourceSnapshot> snapshots)
    {
        var runtimeSnapshot = _runtimeState.Snapshot;
        var projection = NativeHostSourceProjection.Project(
            snapshots,
            GetWorkspaceState,
            runtimeSnapshot.ShowHiddenSources,
            runtimeSnapshot.SelectedSourceKey);
        var projected = projection.Rows
            .Select(row => HostSourceSnapshot.FromNativeProjection(snapshots[row.OriginalSourceIndex], row))
            .ToList();

        _sources.Clear();
        _sources.AddRange(projected);

        _nativeRecentEvents.ClearCurrentSources();
        foreach (var source in snapshots)
        {
            _nativeRecentEvents.AddLiveSource(source.SourceKey);
        }

        foreach (var source in projected)
        {
            _nativeRecentEvents.AddVisibleSource(source.SourceKey, source.DisplayTitle);
        }

        _nativeRecentEvents.Reconcile(FormatRecentEventTime(), GetRecentEventLimit());

        var selectedSourcePlan = NativeHostSelectedSourceCommandPlanner.PlanReconcile(
            _sources.Select(source => source.SourceKey).ToArray(),
            _runtimeState.SelectedSourceKey);
        _runtimeState.ApplySelectedSourcePlan(selectedSourcePlan);
    }

    private HostSourceWorkspaceState GetWorkspaceState(string sourceKey)
    {
        var source = _workspaceSettings.GetSource(sourceKey);
        return new HostSourceWorkspaceState(
            source?.Alias,
            source?.IsHidden ?? false,
            source?.OrderIndex,
            source?.TruePeakOversampling,
            source?.WaveformDisplayMode ?? WaveformDisplayMode.Overall,
            source?.SpectrumDisplayMode ?? SpectrumDisplayMode.Overall);
    }

    private HostSnapshot BuildSnapshot(string? statusTextOverride)
    {
        var runtimeSnapshot = _runtimeState.Snapshot;
        return _snapshotAssembler.BuildSnapshot(
            _sources,
            runtimeSnapshot.SelectedSourceKey,
            runtimeSnapshot.ShowHiddenSources,
            statusTextOverride);
    }

    private HostCommandResult SelectGlobal(HostCommand command)
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanBackToGlobal();
        _runtimeState.ApplySelectedSourcePlan(plan);

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SelectSource(HostCommand command, string sourceKey)
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanSelect(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey);
        if (!plan.Success)
        {
            Snapshot = BuildSnapshot(Snapshot.StatusText);
            return HostCommandResult.Fail(command, Snapshot, HostCommandError.NotFound(sourceKey, "Source was not found."));
        }

        _runtimeState.ApplySelectedSourcePlan(plan);

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult ResetAll(HostCommand command)
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanResetAll();
        _runtimeServiceEffects.ApplyResetAll(plan);

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult ResetSource(HostCommand command, string sourceKey, SignalSourceKind sourceKind, int? processId)
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey,
            sourceKind,
            processId);
        if (!plan.Success)
        {
            return SourceNotFound(command, sourceKey);
        }

        _runtimeServiceEffects.ApplyResetSource(plan);

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetTheme(HostCommand command, string? themeId)
    {
        var plan = NativeHostGlobalSettingsCommandPlanner.PlanSetTheme(themeId);
        _workspaceEffects.ApplyTheme(plan);
        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetPerformanceMode(HostCommand command, HostPerformanceMode mode)
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(mode);
        _runtimeState.ApplyPerformanceModePlan(plan);
        _runtimeServiceEffects.ApplyPerformanceMode(plan);

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetRecentEventLimit(HostCommand command, int maxRecentEventEntries)
    {
        var plan = NativeHostGlobalSettingsCommandPlanner.PlanSetRecentEventLimit(maxRecentEventEntries);
        _workspaceEffects.ApplyRecentEventLimit(plan);
        _nativeRecentEvents.Reconcile(FormatRecentEventTime(), GetRecentEventLimit());
        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetEventOutputFolder(HostCommand command, string? folder)
    {
        var plan = NativeHostGlobalSettingsCommandPlanner.PlanSetEventOutputFolder(folder);
        _workspaceEffects.ApplyEventOutputFolder(plan);
        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetShowHidden(HostCommand command, bool showHiddenSources)
    {
        AppLogger.DebugInfo($"Host SetShowHidden requested={showHiddenSources} beforeRuntime={_runtimeState.ShowHiddenSources} lastSourceCount={LastSourceSnapshots.Count}.");
        var plan = NativeHostGlobalSettingsCommandPlanner.PlanSetShowHiddenSources(showHiddenSources);
        _runtimeState.ApplyShowHiddenPlan(plan);
        Snapshot = RebuildFromLastSources();
        AppLogger.DebugInfo($"Host SetShowHidden afterRuntime={_runtimeState.ShowHiddenSources} snapshot.showHiddenSources={Snapshot.Settings.ShowHiddenSources} visibleSourceCount={Snapshot.Sources.Count}.");
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetSourceAlias(HostCommand command, string sourceKey, string? alias)
    {
        var plan = NativeHostSourcePreferenceCommandPlanner.PlanSetAlias(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey,
            alias);
        if (!plan.Success)
        {
            return SourceNotFound(command, sourceKey);
        }

        _workspaceEffects.ApplySourceAlias(sourceKey, plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetSourceHidden(HostCommand command, string sourceKey, bool isHidden)
    {
        var plan = NativeHostSourcePreferenceCommandPlanner.PlanSetHidden(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey,
            isHidden);
        if (!plan.Success)
        {
            return SourceNotFound(command, sourceKey);
        }

        _workspaceEffects.ApplySourceHidden(sourceKey, plan);
        var selectedSourcePlan = NativeHostSelectedSourceCommandPlanner.PlanHiddenSource(
            sourceKey,
            plan.IsHidden,
            _runtimeState.ShowHiddenSources,
            _runtimeState.SelectedSourceKey);
        _runtimeState.ApplySelectedSourcePlan(selectedSourcePlan);

        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult MoveSource(HostCommand command, string sourceKey, HostSourceMoveDirection direction)
    {
        var plan = NativeHostSourceOrderCommandPlanner.PlanMove(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey,
            direction);
        return ApplySourceOrderPlan(command, sourceKey, plan);
    }

    private HostCommandResult MoveSourceToIndex(HostCommand command, string sourceKey, int targetIndex)
    {
        var plan = NativeHostSourceOrderCommandPlanner.PlanMoveToIndex(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey,
            targetIndex);
        return ApplySourceOrderPlan(command, sourceKey, plan);
    }

    private HostCommandResult ApplySourceOrderPlan(
        HostCommand command,
        string sourceKey,
        NativeHostSourceOrderCommandPlan plan)
    {
        if (!plan.Success)
        {
            Snapshot = BuildSnapshot(Snapshot.StatusText);
            return HostCommandResult.Fail(command, Snapshot, HostCommandError.NotFound(sourceKey, "Source was not found."));
        }

        _workspaceEffects.ApplySourceOrder(plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult ForgetSource(HostCommand command, string sourceKey)
    {
        var plan = NativeHostSourcePreferenceCommandPlanner.PlanForget(sourceKey, _runtimeState.SelectedSourceKey);
        _workspaceEffects.ApplyForgetSource(sourceKey, plan);
        if (plan.ShouldClearSelected)
        {
            _runtimeState.ClearSelectedSourceKey();
        }

        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SourceNotFound(HostCommand command, string sourceKey)
    {
        Snapshot = BuildSnapshot(Snapshot.StatusText);
        return HostCommandResult.Fail(command, Snapshot, HostCommandError.NotFound(sourceKey, "Source was not found."));
    }

    private HostCommandResult SetTruePeakOversampling(HostCommand command, SetTruePeakOversamplingHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetTruePeakOversampling(request.Oversampling);
        _workspaceEffects.ApplyTruePeakOversampling(request.SourceKey, plan);
        _runtimeServiceEffects.ApplyTruePeakOversampling(request.SourceKey, request.SourceKind, request.ProcessId, plan);

        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetWaveformDisplayMode(HostCommand command, SetWaveformDisplayModeHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(request.DisplayMode);
        _workspaceEffects.ApplyWaveformDisplayMode(request.SourceKey, plan);
        _runtimeServiceEffects.ApplyWaveformDisplayMode(request.SourceKey, request.SourceKind, request.ProcessId, plan);

        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetSpectrumDisplayMode(HostCommand command, SetSpectrumDisplayModeHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(request.DisplayMode);
        _workspaceEffects.ApplySpectrumDisplayMode(request.SourceKey, plan);
        _runtimeServiceEffects.ApplySpectrumDisplayMode(request.SourceKey, request.SourceKind, request.ProcessId, plan);

        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetDetailModuleVisibility(HostCommand command, SetDetailModuleVisibilityHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetDetailModuleVisibility(request.ModuleKind, request.IsVisible);
        _workspaceEffects.ApplyDetailModuleVisibility(request.SourceKey, plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetDetailModuleHeight(HostCommand command, SetDetailModuleHeightHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        _workspaceEffects.ApplyDetailModuleHeight(request.SourceKey, request.ModuleKind, request.Height);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult MoveDetailModule(HostCommand command, MoveDetailModuleHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        _workspaceEffects.ApplyDetailModuleOrder(
            request.SourceKey,
            request.ModuleKind,
            request.TargetModuleKind,
            request.InsertAfter);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetSpectrumDisplayRange(HostCommand command, SetSpectrumDisplayRangeHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayRange(request.FloorDb, request.CeilingDb);
        _workspaceEffects.ApplySpectrumDisplayRange(request.SourceKey, plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetLoudnessClockSettings(HostCommand command, SetLoudnessClockSettingsHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetLoudnessClockSettings(
            request.MinimumDb,
            request.MaximumDb,
            request.WarningDb,
            request.CycleSeconds);
        _workspaceEffects.ApplyLoudnessClockSettings(request.SourceKey, plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private HostCommandResult SetCollisionSettings(HostCommand command, SetCollisionSettingsHostCommand request)
    {
        if (!EnsureKnownSource(command, request.SourceKey, out var notFound))
        {
            return notFound!;
        }

        var plan = NativeHostChartSettingsCommandPlanner.PlanSetCollisionSettings(
            request.LevelFloorDb,
            request.ProminenceThresholdDb,
            request.RelativeThreshold,
            request.FrequencyScale,
            request.ComparisonSourceKey);
        _workspaceEffects.ApplyCollisionSettings(request.SourceKey, plan);
        Snapshot = RebuildFromLastSources();
        return HostCommandResult.Ok(command, Snapshot);
    }

    private bool EnsureKnownSource(HostCommand command, string sourceKey, out HostCommandResult? result)
    {
        var plan = NativeHostSourceTargetCommandPlanner.Plan(
            _sources.Select(source => source.SourceKey).ToArray(),
            sourceKey);
        if (plan.Success)
        {
            result = null;
            return true;
        }

        Snapshot = BuildSnapshot(Snapshot.StatusText);
        result = HostCommandResult.Fail(command, Snapshot, HostCommandError.NotFound(sourceKey, "Source was not found."));
        return false;
    }

    private HostSnapshot RebuildFromLastSources()
    {
        Reconcile(LastSourceSnapshots);
        return BuildSnapshot(_sourceWarningText);
    }

    private int GetRecentEventLimit()
    {
        return Math.Clamp(_workspaceSettings.MaxRecentEventEntries ?? 6, 1, 200);
    }

    private static string FormatRecentEventTime()
    {
        return DateTimeOffset.Now.ToString("HH:mm:ss");
    }

}
