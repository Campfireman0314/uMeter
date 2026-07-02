using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class HostSnapshotAssemblerTests
{
    [Fact]
    public void BuildsAggregateSnapshotWithSettingsCommandsAndRecentEvents()
    {
        using var recentEvents = new NativeHostRecentEvents();
        var workspace = new UserWorkspaceSettings
        {
            ThemeId = "studio-green",
            MaxRecentEventEntries = 6
        };
        workspace.GetOrCreateSource("plugin:lead").Alias = "Lead Bus";
        var source = Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.1, 0.8, 0.2]);
        AddRecentEvent(recentEvents, source);
        var assembler = new HostSnapshotAssembler(workspace, recentEvents);

        var snapshot = assembler.BuildSnapshot(
            [source],
            selectedSourceKey: null,
            showHiddenSources: false,
            statusTextOverride: null);

        Assert.Equal("1 source tracked", snapshot.StatusText);
        Assert.Single(snapshot.Sources);
        Assert.Null(snapshot.SelectedDetail);
        Assert.Equal("studio-green", snapshot.Settings.ThemeId);
        Assert.Equal(6, snapshot.Settings.MaxRecentEventEntries);
        Assert.False(snapshot.Settings.ShowHiddenSources);
        var recent = Assert.Single(snapshot.RecentEvents);
        Assert.Equal("Source appeared", recent.Title);
        Assert.Equal("Lead", recent.Detail);
        Assert.False(snapshot.Commands.CanBackToGlobal);
        Assert.True(snapshot.Commands.CanResetAll);
    }

    [Fact]
    public void AppliesStatusTextOverrideOverAggregateStatus()
    {
        using var recentEvents = new NativeHostRecentEvents();
        var assembler = new HostSnapshotAssembler(new UserWorkspaceSettings(), recentEvents);

        var snapshot = assembler.BuildSnapshot(
            [],
            selectedSourceKey: null,
            showHiddenSources: false,
            statusTextOverride: "Refresh failed: device disappeared");

        Assert.Equal("Refresh failed: device disappeared", snapshot.StatusText);
    }

    [Fact]
    public void BuildsSelectedDetailSnapshotWithModulesAndCollision()
    {
        using var recentEvents = new NativeHostRecentEvents();
        var workspace = new UserWorkspaceSettings();
        var sourceSettings = workspace.GetOrCreateSource("plugin:lead");
        sourceSettings.InlineWaveformVisible = true;
        sourceSettings.InlineSpectrumVisible = true;
        sourceSettings.InlineCollisionVisible = true;
        sourceSettings.InlineCollisionSourceKey = "plugin:keys";
        sourceSettings.CollisionLevelFloorDb = -120;
        sourceSettings.CollisionProminenceThresholdDb = 0;
        sourceSettings.CollisionRelativeThreshold = 0.25;
        sourceSettings.CollisionFrequencyScale = CollisionFrequencyScale.Mel;
        sourceSettings.InlineDetailModules =
        [
            new DetailModuleLayoutSettings { Kind = DetailModuleKind.Spectrum, OrderIndex = 0, Height = 360 },
            new DetailModuleLayoutSettings { Kind = DetailModuleKind.Waveform, OrderIndex = 1, Height = 240 }
        ];
        var lead = Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.0, 0.42, 0.72, 0.18]);
        var keys = Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.0, 0.43, 0.18, 0.08]);
        var assembler = new HostSnapshotAssembler(workspace, recentEvents);

        var snapshot = assembler.BuildSnapshot(
            [lead, keys],
            selectedSourceKey: "plugin:lead",
            showHiddenSources: false,
            statusTextOverride: null);

        var detail = Assert.IsType<HostDetailSnapshot>(snapshot.SelectedDetail);
        Assert.Equal("plugin:lead", detail.Source.SourceKey);
        Assert.True(detail.IsWaveformVisible);
        Assert.True(detail.IsSpectrumVisible);
        Assert.True(detail.IsCollisionVisible);
        Assert.Contains(detail.Modules, module => module.Kind == DetailModuleKind.Spectrum && module.OrderIndex == 0 && module.Height == 360);
        Assert.Contains(detail.Modules, module => module.Kind == DetailModuleKind.Waveform && module.OrderIndex == 1 && module.Height == 240);
        Assert.NotNull(detail.Collision);
        Assert.Equal("plugin:keys", detail.Collision.ComparisonSourceKey);
        Assert.Equal("Keys", detail.Collision.ComparisonDisplayName);
        Assert.Equal(CollisionFrequencyScale.Mel, detail.Collision.FrequencyScale);
        Assert.Contains(detail.Collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
        Assert.True(snapshot.Commands.CanBackToGlobal);
        Assert.True(snapshot.Commands.CanForgetSelectedSource);
    }

    private static HostSourceSnapshot Source(
        string key,
        string name,
        SignalSourceKind kind,
        IReadOnlyList<double>? spectrumBuckets = null)
    {
        return HostSourceSnapshot.FromSignalSource(
            new SignalSourceSnapshot(
                key,
                kind,
                null,
                name,
                0.5f,
                PeakMath.ToDecibels(0.5f),
                "-6.0 dB",
                "Active",
                SpectrumBuckets: spectrumBuckets));
    }

    private static void AddRecentEvent(NativeHostRecentEvents recentEvents, HostSourceSnapshot source)
    {
        recentEvents.ClearCurrentSources();
        recentEvents.AddLiveSource(source.SourceKey);
        recentEvents.AddVisibleSource(source.SourceKey, source.DisplayTitle);
        recentEvents.Reconcile("12:34:56", 6);
    }
}
