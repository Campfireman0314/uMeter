using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class DesktopMeterHostControllerTests
{
    [Fact]
    public void RefreshProjectsSourcesAndWorkspacePreferences()
    {
        var workspace = new UserWorkspaceSettings();
        var sourceSettings = workspace.GetOrCreateSource("plugin:lead");
        sourceSettings.Alias = "Lead Bus";
        sourceSettings.OrderIndex = 0;
        sourceSettings.TruePeakOversampling = TruePeakOversampling.EightX;
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f, truePeakDb: -0.5)]);
        var controller = new DesktopMeterHostController(service, workspace);

        var snapshot = controller.Refresh();

        var source = Assert.Single(snapshot.Sources);
        Assert.Equal("plugin:lead", source.SourceKey);
        Assert.Equal("Lead Bus", source.EffectiveDisplayName);
        Assert.Equal(TruePeakOversampling.EightX, source.TruePeakOversampling);
        Assert.Equal(-0.5, source.TruePeakDb);
        Assert.Equal("1 source tracked", snapshot.StatusText);
        Assert.Single(snapshot.RecentEvents);
    }

    [Fact]
    public void RefreshConvertsServiceFailureToStatusSnapshot()
    {
        var service = new FakeSignalSourceService([Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)])
        {
            ThrowOnGet = true
        };
        var controller = new DesktopMeterHostController(service);

        var snapshot = controller.Refresh();

        Assert.Empty(snapshot.Sources);
        Assert.Contains("Refresh failed", snapshot.StatusText);
    }

    [Fact]
    public void RefreshProjectsNoSourcesStatusFromAggregatePlanner()
    {
        var controller = new DesktopMeterHostController(new FakeSignalSourceService([]));

        var snapshot = controller.Refresh();

        Assert.Empty(snapshot.Sources);
        Assert.Equal("No active audio sources", snapshot.StatusText);
    }

    [Fact]
    public void RefreshPreservesSourceWarningStatusOverride()
    {
        var controller = new DesktopMeterHostController(
            new FakeSignalSourceService([Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]),
            sourceWarningText: "  Capture fallback active  ");

        var snapshot = controller.Refresh();

        Assert.Single(snapshot.Sources);
        Assert.Equal("Capture fallback active", snapshot.StatusText);
    }

    [Fact]
    public void CommandsPersistWorkspaceAndForwardAnalysisSettings()
    {
        var workspace = new UserWorkspaceSettings();
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();

        var aliasResult = controller.Execute(new SetSourceAliasHostCommand("plugin:lead", "  Lead Bus  "));
        var truePeakResult = controller.Execute(new SetTruePeakOversamplingHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            TruePeakOversampling.EightX));
        var waveformResult = controller.Execute(new SetWaveformDisplayModeHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            WaveformDisplayMode.Channels));
        var spectrumResult = controller.Execute(new SetSpectrumDisplayModeHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            SpectrumDisplayMode.Channels));

        Assert.True(aliasResult.Success);
        Assert.True(truePeakResult.Success);
        Assert.True(waveformResult.Success);
        Assert.True(spectrumResult.Success);
        var sourceSettings = workspace.GetSource("plugin:lead");
        Assert.Equal("Lead Bus", sourceSettings?.Alias);
        Assert.Equal(TruePeakOversampling.EightX, sourceSettings?.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, sourceSettings?.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, sourceSettings?.SpectrumDisplayMode);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, 8)], service.TruePeakOversamplingRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelWaveformRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelSpectrumRequests);
    }

    [Fact]
    public void SelectedDetailProjectsInlineChartModules()
    {
        var workspace = new UserWorkspaceSettings();
        var sourceSettings = workspace.GetOrCreateSource("plugin:lead");
        sourceSettings.InlineWaveformVisible = true;
        sourceSettings.InlineSpectrumVisible = true;
        sourceSettings.InlineLoudnessClockVisible = true;
        sourceSettings.InlineCollisionVisible = true;
        sourceSettings.InlineDetailModules =
        [
            new DetailModuleLayoutSettings { Kind = DetailModuleKind.Spectrum, OrderIndex = 0, Height = 360 },
            new DetailModuleLayoutSettings { Kind = DetailModuleKind.Waveform, OrderIndex = 1, Height = 240 }
        ];
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();

        var snapshot = controller.Execute(new SelectDetailHostCommand("plugin:lead")).Snapshot;

        Assert.NotNull(snapshot.SelectedDetail);
        var detail = snapshot.SelectedDetail;
        Assert.Equal(
            [DetailModuleKind.Spectrum, DetailModuleKind.Waveform, DetailModuleKind.Stereo, DetailModuleKind.Collision, DetailModuleKind.LoudnessClock],
            detail.Modules.OrderBy(module => module.OrderIndex).Select(module => module.Kind).ToArray());
        Assert.True(detail.IsWaveformVisible);
        Assert.True(detail.IsSpectrumVisible);
        Assert.True(detail.IsStereoVisible);
        Assert.True(detail.IsLoudnessClockVisible);
        Assert.True(detail.IsCollisionVisible);
        Assert.Equal(360, detail.Modules.Single(module => module.Kind == DetailModuleKind.Spectrum).Height);
        Assert.Equal(240, detail.Modules.Single(module => module.Kind == DetailModuleKind.Waveform).Height);
    }

    [Fact]
    public void SelectedDetailProjectsFrequencyCollisionBands()
    {
        var workspace = new UserWorkspaceSettings();
        var sourceSettings = workspace.GetOrCreateSource("plugin:lead");
        sourceSettings.InlineCollisionVisible = true;
        sourceSettings.InlineCollisionSourceKey = "plugin:keys";
        sourceSettings.CollisionLevelFloorDb = -120;
        sourceSettings.CollisionProminenceThresholdDb = 0;
        sourceSettings.CollisionRelativeThreshold = 0.25;
        sourceSettings.CollisionFrequencyScale = CollisionFrequencyScale.Mel;
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f, spectrumBuckets: [0.0, 0.42, 0.72, 0.18]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f, spectrumBuckets: [0.0, 0.43, 0.18, 0.08])
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();

        var snapshot = controller.Execute(new SelectDetailHostCommand("plugin:lead")).Snapshot;

        Assert.NotNull(snapshot.SelectedDetail?.Collision);
        var collision = snapshot.SelectedDetail.Collision;
        Assert.Equal("plugin:keys", collision.ComparisonSourceKey);
        Assert.Equal(CollisionFrequencyScale.Mel, collision.FrequencyScale);
        Assert.Contains(collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
    }

    [Fact]
    public void SelectedDetailUsesAllOtherSourcesWhenCollisionComparisonKeyIsMissing()
    {
        var workspace = new UserWorkspaceSettings();
        var sourceSettings = workspace.GetOrCreateSource("plugin:lead");
        sourceSettings.InlineCollisionVisible = true;
        sourceSettings.InlineCollisionSourceKey = "plugin:missing";
        sourceSettings.CollisionLevelFloorDb = -120;
        sourceSettings.CollisionProminenceThresholdDb = 0;
        sourceSettings.CollisionRelativeThreshold = 0.25;
        sourceSettings.CollisionFrequencyScale = CollisionFrequencyScale.Logarithmic;
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f, spectrumBuckets: [0.0, 0.42, 0.72, 0.18]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f, spectrumBuckets: [0.0, 0.43, 0.18, 0.08])
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();

        var snapshot = controller.Execute(new SelectDetailHostCommand("plugin:lead")).Snapshot;

        Assert.NotNull(snapshot.SelectedDetail?.Collision);
        var collision = snapshot.SelectedDetail.Collision;
        Assert.Equal("plugin:missing", collision.ComparisonSourceKey);
        Assert.Null(collision.ComparisonDisplayName);
        Assert.Contains(collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
    }

    [Fact]
    public void CommandsRebuildFromLastSnapshotWithoutPollingSourceService()
    {
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service);
        controller.Refresh();
        service.GetCallCount = 0;

        var result = controller.Execute(new SetSourceAliasHostCommand("plugin:lead", "Lead Bus"));

        Assert.True(result.Success);
        Assert.Equal(0, service.GetCallCount);
        Assert.Equal("Lead Bus", Assert.Single(result.Snapshot.Sources).EffectiveDisplayName);
    }

    [Fact]
    public void ChartSettingsCommandsPersistForwardAndRebuildWithoutPolling()
    {
        var workspace = new UserWorkspaceSettings();
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();
        controller.Execute(new SelectDetailHostCommand("plugin:lead"));
        service.GetCallCount = 0;

        var truePeak = controller.Execute(new SetTruePeakOversamplingHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            TruePeakOversampling.EightX));
        var waveform = controller.Execute(new SetWaveformDisplayModeHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            WaveformDisplayMode.Channels));
        var spectrum = controller.Execute(new SetSpectrumDisplayModeHostCommand(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            SpectrumDisplayMode.Channels));
        var visibility = controller.Execute(new SetDetailModuleVisibilityHostCommand(
            "plugin:lead",
            DetailModuleKind.Collision,
            true));
        var moduleHeight = controller.Execute(new SetDetailModuleHeightHostCommand(
            "plugin:lead",
            DetailModuleKind.Spectrum,
            333));
        var moduleOrder = controller.Execute(new MoveDetailModuleHostCommand(
            "plugin:lead",
            DetailModuleKind.Waveform,
            DetailModuleKind.Spectrum,
            InsertAfter: true));
        var spectrumRange = controller.Execute(new SetSpectrumDisplayRangeHostCommand("plugin:lead", -84, -12));
        var loudnessClock = controller.Execute(new SetLoudnessClockSettingsHostCommand("plugin:lead", -72, -12, -18, 90));
        var collision = controller.Execute(new SetCollisionSettingsHostCommand(
            "plugin:lead",
            -96,
            9,
            0.25,
            CollisionFrequencyScale.Mel,
            "\u3000plugin:keys\u00A0"));

        Assert.All(
            [truePeak, waveform, spectrum, visibility, moduleHeight, moduleOrder, spectrumRange, loudnessClock, collision],
            result => Assert.True(result.Success));
        var sourceSettings = workspace.GetSource("plugin:lead");
        Assert.Equal(TruePeakOversampling.EightX, sourceSettings?.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, sourceSettings?.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, sourceSettings?.SpectrumDisplayMode);
        Assert.True(sourceSettings?.InlineCollisionVisible);
        Assert.Equal(333, sourceSettings?.InlineDetailModules?.Single(module => module.Kind == DetailModuleKind.Spectrum).Height);
        Assert.Equal(
            [DetailModuleKind.Stereo, DetailModuleKind.Collision, DetailModuleKind.LoudnessClock, DetailModuleKind.Spectrum, DetailModuleKind.Waveform],
            sourceSettings?.InlineDetailModules?.OrderBy(module => module.OrderIndex).Select(module => module.Kind).ToArray());
        Assert.Equal(-84, sourceSettings?.SpectrumDisplayFloorDb);
        Assert.Equal(-12, sourceSettings?.SpectrumDisplayCeilingDb);
        Assert.Equal(-72, sourceSettings?.LoudnessClockMinimumDb);
        Assert.Equal(-12, sourceSettings?.LoudnessClockMaximumDb);
        Assert.Equal(-18, sourceSettings?.LoudnessClockWarningDb);
        Assert.Equal(90, sourceSettings?.LoudnessClockCycleSeconds);
        Assert.Equal(-96, sourceSettings?.CollisionLevelFloorDb);
        Assert.Equal(9, sourceSettings?.CollisionProminenceThresholdDb);
        Assert.Equal(0.25, sourceSettings?.CollisionRelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, sourceSettings?.CollisionFrequencyScale);
        Assert.Equal("plugin:keys", sourceSettings?.InlineCollisionSourceKey);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, 8)], service.TruePeakOversamplingRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelWaveformRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelSpectrumRequests);
        Assert.Equal(0, service.GetCallCount);
        Assert.Equal(SpectrumDisplayMode.Channels, collision.Snapshot.Sources.Single(source => source.SourceKey == "plugin:lead").SpectrumDisplayMode);
        Assert.NotNull(collision.Snapshot.SelectedDetail);
        Assert.True(collision.Snapshot.SelectedDetail.IsCollisionVisible);
        Assert.Equal(CollisionFrequencyScale.Mel, collision.Snapshot.SelectedDetail.Collision?.FrequencyScale);
        Assert.Equal("plugin:keys", collision.Snapshot.SelectedDetail.Collision?.ComparisonSourceKey);
        Assert.Equal(-84, collision.Snapshot.Settings.Sources.Single(source => source.SourceKey == "plugin:lead").SpectrumDisplayFloorDb);
    }

    [Fact]
    public void MissingSourceCommandReturnsTypedError()
    {
        var controller = new DesktopMeterHostController(new FakeSignalSourceService([]));
        controller.Refresh();

        var result = controller.Execute(new ResetSourceHostCommand("missing", SignalSourceKind.Plugin, null));

        Assert.False(result.Success);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, result.Error?.Code);
        Assert.Equal("missing", result.Error?.TargetSourceKey);
    }

    [Fact]
    public void RuntimeServiceCommandsForwardAndRebuildWithoutPolling()
    {
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service);
        controller.Refresh();
        service.GetCallCount = 0;

        var resetAll = controller.Execute(new ResetAllHostCommand());
        var resetSource = controller.Execute(new ResetSourceHostCommand("plugin:lead", SignalSourceKind.Plugin, 17));
        var eco = controller.Execute(new SetPerformanceModeHostCommand(HostPerformanceMode.Eco));
        var normal = controller.Execute(new SetPerformanceModeHostCommand(HostPerformanceMode.Normal));

        Assert.True(resetAll.Success);
        Assert.True(resetSource.Success);
        Assert.True(eco.Success);
        Assert.True(normal.Success);
        Assert.Equal(1, service.ResetAllSourcesCount);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, 17)], service.ResetRequests);
        Assert.Equal([PcmAnalysisPerformanceMode.Eco, PcmAnalysisPerformanceMode.Normal], service.PerformanceModeRequests);
        Assert.Equal(0, service.GetCallCount);
    }

    [Fact]
    public void RuntimeServiceCommandsDoNotForwardMissingResetSource()
    {
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service);
        controller.Refresh();

        var result = controller.Execute(new ResetSourceHostCommand("plugin:missing", SignalSourceKind.Plugin, null));

        Assert.False(result.Success);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, result.Error?.Code);
        Assert.Empty(service.ResetRequests);
    }

    [Fact]
    public void HiddenSourcesAreFilteredUntilRequested()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:hidden").IsHidden = true;
        var service = new FakeSignalSourceService(
            [Source("plugin:hidden", "Hidden", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service, workspace);

        var hidden = controller.Refresh();
        var shown = controller.Execute(new SetShowHiddenSourcesHostCommand(true)).Snapshot;

        Assert.Empty(hidden.Sources);
        var source = Assert.Single(shown.Sources);
        Assert.True(source.IsHidden);
        Assert.True(shown.Settings.ShowHiddenSources);
    }

    [Fact]
    public void SourcePreferenceCommandsPersistAndRebuildWithoutPolling()
    {
        var workspace = new UserWorkspaceSettings();
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();
        controller.Execute(new SelectDetailHostCommand("plugin:lead"));
        service.GetCallCount = 0;

        var hidden = controller.Execute(new HideSourceHostCommand("plugin:lead"));
        Assert.True(hidden.Success);
        Assert.Empty(hidden.Snapshot.Sources.Where(source => source.SourceKey == "plugin:lead"));
        Assert.True(workspace.GetSource("plugin:lead")?.IsHidden);

        var shown = controller.Execute(new SetShowHiddenSourcesHostCommand(true));
        Assert.True(shown.Snapshot.Sources.Single(source => source.SourceKey == "plugin:lead").IsHidden);

        var restored = controller.Execute(new RestoreSourceHostCommand("plugin:lead"));
        Assert.True(restored.Success);
        Assert.False(workspace.GetSource("plugin:lead")?.IsHidden);

        var forgotten = controller.Execute(new ForgetSourceHostCommand("plugin:lead"));
        Assert.True(forgotten.Success);
        Assert.Null(workspace.GetSource("plugin:lead"));
        Assert.Null(forgotten.Snapshot.SelectedDetail);
        Assert.Equal(0, service.GetCallCount);
    }

    [Fact]
    public void RefreshClearsSelectedDetailWhenSelectedSourceDisappears()
    {
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service);
        controller.Refresh();

        var selected = controller.Execute(new SelectDetailHostCommand("plugin:lead")).Snapshot;
        service.CurrentSources =
        [
            Source("plugin:keys", "Keys", SignalSourceKind.Plugin, 0.5f)
        ];

        var refreshed = controller.Refresh();

        Assert.NotNull(selected.SelectedDetail);
        Assert.Null(refreshed.SelectedDetail);
        Assert.False(refreshed.Commands.CanBackToGlobal);
    }

    [Fact]
    public void SourcePreferenceCommandsReturnTypedErrorForMissingSource()
    {
        var service = new FakeSignalSourceService(
            [Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service);
        controller.Refresh();

        var alias = controller.Execute(new SetSourceAliasHostCommand("plugin:missing", "Missing"));
        var hidden = controller.Execute(new HideSourceHostCommand("plugin:missing"));
        var restored = controller.Execute(new RestoreSourceHostCommand("plugin:missing"));

        Assert.False(alias.Success);
        Assert.False(hidden.Success);
        Assert.False(restored.Success);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, alias.Error?.Code);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, hidden.Error?.Code);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, restored.Error?.Code);
    }

    [Fact]
    public void ShowingPreviouslyHiddenLiveSourceDoesNotCreateAppearanceEvent()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:hidden").IsHidden = true;
        var service = new FakeSignalSourceService(
            [Source("plugin:hidden", "Hidden", SignalSourceKind.Plugin, 0.5f)]);
        var controller = new DesktopMeterHostController(service, workspace);

        var hidden = controller.Refresh();
        var shown = controller.Execute(new SetShowHiddenSourcesHostCommand(true)).Snapshot;

        Assert.Empty(hidden.RecentEvents);
        Assert.Empty(shown.RecentEvents);
    }

    [Fact]
    public void RecentEventLimitCommandTrimsNativeHistoryImmediately()
    {
        var workspace = new UserWorkspaceSettings
        {
            MaxRecentEventEntries = 3
        };
        var service = new FakeSignalSourceService(
            [
                Source("plugin:a", "A", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:b", "B", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:c", "C", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);

        var snapshot = controller.Refresh();
        var trimmed = controller.Execute(new SetRecentEventLimitHostCommand(1)).Snapshot;

        Assert.Equal(["C", "B", "A"], snapshot.RecentEvents.Select(item => item.Detail).ToArray());
        var item = Assert.Single(trimmed.RecentEvents);
        Assert.Equal("C", item.Detail);
        Assert.Equal(1, trimmed.Settings.MaxRecentEventEntries);
    }

    [Fact]
    public void GlobalSettingsCommandsPersistAndRebuildWithoutPolling()
    {
        var workspace = new UserWorkspaceSettings
        {
            MaxRecentEventEntries = 3
        };
        workspace.GetOrCreateSource("plugin:hidden").IsHidden = true;
        var service = new FakeSignalSourceService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:hidden", "Hidden", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();
        service.GetCallCount = 0;

        var theme = controller.Execute(new SetThemeHostCommand("\u3000studio-green\u00A0"));
        var folder = controller.Execute(new SetEventOutputFolderHostCommand("\u3000C:\\uMeter Events\u00A0"));
        var limit = controller.Execute(new SetRecentEventLimitHostCommand(999));
        var showHidden = controller.Execute(new SetShowHiddenSourcesHostCommand(true));

        Assert.True(theme.Success);
        Assert.True(folder.Success);
        Assert.True(limit.Success);
        Assert.True(showHidden.Success);
        Assert.Equal("studio-green", workspace.ThemeId);
        Assert.Equal("C:\\uMeter Events", workspace.EventOutputFolder);
        Assert.Equal(200, workspace.MaxRecentEventEntries);
        Assert.Equal("studio-green", theme.Snapshot.Settings.ThemeId);
        Assert.Equal("C:\\uMeter Events", folder.Snapshot.Settings.EventOutputFolder);
        Assert.Equal(200, limit.Snapshot.Settings.MaxRecentEventEntries);
        Assert.True(showHidden.Snapshot.Settings.ShowHiddenSources);
        Assert.Contains(showHidden.Snapshot.Sources, source => source.SourceKey == "plugin:hidden" && source.IsHidden);
        Assert.Equal(0, service.GetCallCount);
    }

    [Fact]
    public void SnapshotProjectsCommandAvailabilityForSelectedSourceBoundaries()
    {
        var service = new FakeSignalSourceService(
            [
                Source("plugin:a", "A", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:b", "B", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:c", "C", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service);
        var global = controller.Refresh();

        Assert.False(global.Commands.CanBackToGlobal);
        Assert.True(global.Commands.CanResetAll);
        Assert.False(global.Commands.CanForgetSelectedSource);
        Assert.False(global.Commands.CanMoveSelectedSourceUp);
        Assert.False(global.Commands.CanMoveSelectedSourceDown);

        var first = controller.Execute(new SelectDetailHostCommand("plugin:a")).Snapshot;
        Assert.True(first.Commands.CanBackToGlobal);
        Assert.True(first.Commands.CanResetAll);
        Assert.True(first.Commands.CanForgetSelectedSource);
        Assert.False(first.Commands.CanMoveSelectedSourceUp);
        Assert.True(first.Commands.CanMoveSelectedSourceDown);

        var last = controller.Execute(new SelectDetailHostCommand("plugin:c")).Snapshot;
        Assert.True(last.Commands.CanBackToGlobal);
        Assert.True(last.Commands.CanResetAll);
        Assert.True(last.Commands.CanForgetSelectedSource);
        Assert.True(last.Commands.CanMoveSelectedSourceUp);
        Assert.False(last.Commands.CanMoveSelectedSourceDown);
    }

    [Fact]
    public void MoveSourceCommandsPersistOrderAndRebuildWithoutPolling()
    {
        var workspace = new UserWorkspaceSettings();
        var service = new FakeSignalSourceService(
            [
                Source("plugin:a", "A", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:b", "B", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:c", "C", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();
        service.GetCallCount = 0;

        var moved = controller.Execute(new MoveSourceHostCommand("plugin:b", HostSourceMoveDirection.Up));

        Assert.True(moved.Success);
        Assert.Equal(0, service.GetCallCount);
        Assert.Equal(["plugin:b", "plugin:a", "plugin:c"], moved.Snapshot.Sources.Select(source => source.SourceKey).ToArray());
        Assert.Equal(0, workspace.GetSource("plugin:b")?.OrderIndex);
        Assert.Equal(1, workspace.GetSource("plugin:a")?.OrderIndex);
        Assert.Equal(2, workspace.GetSource("plugin:c")?.OrderIndex);
    }

    [Fact]
    public void MoveSourceToIndexClampsAndMissingSourceReturnsTypedError()
    {
        var workspace = new UserWorkspaceSettings();
        var service = new FakeSignalSourceService(
            [
                Source("plugin:a", "A", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:b", "B", SignalSourceKind.Plugin, 0.5f),
                Source("plugin:c", "C", SignalSourceKind.Plugin, 0.5f)
            ]);
        var controller = new DesktopMeterHostController(service, workspace);
        controller.Refresh();

        var clamped = controller.Execute(new MoveSourceToIndexHostCommand("plugin:a", 99));
        var missing = controller.Execute(new MoveSourceToIndexHostCommand("plugin:missing", 0));

        Assert.True(clamped.Success);
        Assert.Equal(["plugin:b", "plugin:c", "plugin:a"], clamped.Snapshot.Sources.Select(source => source.SourceKey).ToArray());
        Assert.Equal(2, workspace.GetSource("plugin:a")?.OrderIndex);
        Assert.False(missing.Success);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, missing.Error?.Code);
        Assert.Equal("plugin:missing", missing.Error?.TargetSourceKey);
    }

    private static SignalSourceSnapshot Source(
        string key,
        string name,
        SignalSourceKind kind,
        float peak,
        double? truePeakDb = null,
        IReadOnlyList<double>? spectrumBuckets = null)
    {
        return new SignalSourceSnapshot(
            key,
            kind,
            null,
            name,
            peak,
            PeakMath.ToDecibels(peak),
            $"{peak:0.0} dB",
            "Active",
            TruePeakDb: truePeakDb,
            SpectrumBuckets: spectrumBuckets);
    }

    private sealed class FakeSignalSourceService :
        ISignalSourceMeterService,
        ISignalSourceResetService,
        ISignalSourceAnalysisSettingsService
    {
        public FakeSignalSourceService(IReadOnlyList<SignalSourceSnapshot> sources)
        {
            CurrentSources = sources;
        }

        public IReadOnlyList<SignalSourceSnapshot> CurrentSources { get; set; }

        public bool ThrowOnGet { get; init; }

        public int GetCallCount { get; set; }

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId)> ResetRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public int ResetAllSourcesCount { get; private set; }

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            GetCallCount++;
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("device disappeared");
            }

            return CurrentSources;
        }

        public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
        {
            ResetRequests.Add((sourceKey, sourceKind, processId));
        }

        public void ResetAllSources()
        {
            ResetAllSourcesCount++;
        }

        public void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling)
        {
            TruePeakOversamplingRequests.Add((sourceKey, sourceKind, processId, oversampling));
        }

        public void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelWaveformRequests.Add((sourceKey, sourceKind, processId, enabled));
        }

        public void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelSpectrumRequests.Add((sourceKey, sourceKind, processId, enabled));
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            PerformanceModeRequests.Add(mode);
        }
    }
}
