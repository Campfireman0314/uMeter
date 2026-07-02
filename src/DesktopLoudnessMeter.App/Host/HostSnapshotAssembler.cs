using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

internal sealed class HostSnapshotAssembler
{
    private readonly UserWorkspaceSettings _workspaceSettings;
    private readonly NativeHostRecentEvents _nativeRecentEvents;

    public HostSnapshotAssembler(
        UserWorkspaceSettings workspaceSettings,
        NativeHostRecentEvents nativeRecentEvents)
    {
        _workspaceSettings = workspaceSettings;
        _nativeRecentEvents = nativeRecentEvents;
    }

    public HostSnapshot BuildSnapshot(
        IReadOnlyList<HostSourceSnapshot> sources,
        string? selectedSourceKey,
        bool showHiddenSources,
        string? statusTextOverride)
    {
        var aggregate = NativeHostAggregateSnapshotPlanner.Project(
            sources.Select(source => source.SourceKey).ToArray(),
            selectedSourceKey);
        var selectedIndex = aggregate.SelectedSourceIndex;
        var selected = aggregate.IncludeSelectedDetail && selectedIndex is int index && index >= 0 && index < sources.Count
            ? sources[index]
            : null;
        var chrome = NativeHostSnapshotChromeProjection.Project(
            _workspaceSettings,
            showHiddenSources,
            selectedIndex,
            sources.Count);

        return new HostSnapshot(
            sources.ToArray(),
            selected is null ? null : BuildDetailSnapshot(sources, selected),
            chrome.Settings,
            _nativeRecentEvents.GetEvents().ToArray(),
            statusTextOverride ?? aggregate.StatusText,
            chrome.Commands);
    }

    private HostDetailSnapshot BuildDetailSnapshot(
        IReadOnlyList<HostSourceSnapshot> sources,
        HostSourceSnapshot selected)
    {
        var sourceSettings = _workspaceSettings.GetSource(selected.SourceKey);
        var plan = NativeHostSelectedDetailSnapshotPlanner.Project(
            ToSelectedDetailSources(sources),
            ToSelectedDetailLayouts(sourceSettings?.InlineDetailModules ?? []),
            new NativeHostDetailModuleVisibility(
                sourceSettings?.InlineStereoVisible,
                sourceSettings?.InlineCollisionVisible,
                sourceSettings?.InlineLoudnessClockVisible,
                sourceSettings?.InlineWaveformVisible,
                sourceSettings?.InlineSpectrumVisible),
            selected.SourceKey,
            sourceSettings?.InlineCollisionSourceKey,
            sourceSettings?.CollisionLevelFloorDb,
            sourceSettings?.CollisionProminenceThresholdDb,
            sourceSettings?.CollisionRelativeThreshold,
            sourceSettings?.CollisionFrequencyScale);
        var modules = plan.Modules
            .Select(layout => new HostDetailModuleSnapshot(
                layout.Kind,
                layout.Title,
                layout.OrderIndex,
                layout.IsVisible,
                layout.Height))
            .ToArray();

        return new HostDetailSnapshot(
            selected,
            modules,
            IsWaveformVisible: plan.IsWaveformVisible,
            IsSpectrumVisible: plan.IsSpectrumVisible,
            IsStereoVisible: plan.IsStereoVisible,
            IsLoudnessClockVisible: plan.IsLoudnessClockVisible,
            IsCollisionVisible: plan.IsCollisionVisible,
            Collision: ToHostFrequencyCollisionSnapshot(plan.Collision));
    }

    private static NativeHostSelectedDetailSnapshotSourceInput[] ToSelectedDetailSources(IEnumerable<HostSourceSnapshot> sources)
    {
        return sources
            .Select(source => new NativeHostSelectedDetailSnapshotSourceInput(
                source.SourceKey,
                source.DisplayTitle,
                source.SpectrumBuckets))
            .ToArray();
    }

    private static NativeHostDetailModuleLayoutInput[] ToSelectedDetailLayouts(IEnumerable<DetailModuleLayoutSettings> layouts)
    {
        return layouts
            .Select(layout => new NativeHostDetailModuleLayoutInput(
                layout.Kind,
                layout.OrderIndex,
                layout.Height))
            .ToArray();
    }

    private static HostFrequencyCollisionSnapshot ToHostFrequencyCollisionSnapshot(
        NativeHostSelectedDetailCollisionPlan collision)
    {
        return new HostFrequencyCollisionSnapshot(
            collision.ComparisonSourceKey,
            collision.ComparisonDisplayName,
            collision.LevelFloorDb,
            collision.ProminenceThresholdDb,
            collision.RelativeThreshold,
            collision.FrequencyScale,
            collision.Bands);
    }
}
