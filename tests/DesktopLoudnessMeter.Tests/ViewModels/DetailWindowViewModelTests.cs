using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class DetailWindowViewModelTests
{
    [Fact]
    public void DetailWindowOwnsIndependentTopmostAndStereoSettings()
    {
        var session = new AudioSessionMeterViewModel(Snapshot());
        var first = new DetailWindowViewModel(session);
        var second = new DetailWindowViewModel(session);

        first.IsAlwaysOnTop = false;
        first.StereoSettings.IsAutoScale = false;
        first.StereoSettings.ScaleRangeDb = 24;

        Assert.False(first.IsAlwaysOnTop);
        Assert.True(second.IsAlwaysOnTop);
        Assert.False(first.IsWaveformVisible);
        Assert.False(first.IsSpectrumVisible);
        Assert.False(first.IsCollisionVisible);
        Assert.False(first.StereoSettings.IsAutoScale);
        Assert.True(second.StereoSettings.IsAutoScale);
        Assert.Equal(24, first.StereoSettings.ScaleRangeDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultScaleRangeDb, second.StereoSettings.ScaleRangeDb);
    }

    [Fact]
    public void WaveformAndSpectrumVisibilityCanBeControlledSeparately()
    {
        var session = new AudioSessionMeterViewModel(Snapshot());
        var viewModel = new DetailWindowViewModel(session);

        viewModel.IsSpectrumVisible = true;

        Assert.True(viewModel.IsSpectrumOnlyVisible);
        Assert.False(viewModel.IsWaveformOnlyVisible);
        Assert.False(viewModel.AreRollsVisible);

        viewModel.IsWaveformVisible = true;

        Assert.False(viewModel.IsSpectrumOnlyVisible);
        Assert.True(viewModel.AreRollsVisible);
    }

    [Fact]
    public void ResetCommandDelegatesToSessionReset()
    {
        var session = new AudioSessionMeterViewModel(Snapshot(momentaryLufs: -18));
        var viewModel = new DetailWindowViewModel(session);

        viewModel.ResetCommand.Execute(null);

        Assert.Equal(1, session.ResetCount);
        Assert.Equal("LUFS n/a", session.MomentaryLufsText);
    }

    [Fact]
    public void SwitchCommandMovesPopupToLiveTarget()
    {
        var first = new AudioSessionMeterViewModel(Snapshot("app:first", "First"));
        var second = new AudioSessionMeterViewModel(Snapshot("app:second", "Second"));
        var active = new List<AudioSessionMeterViewModel> { first, second };
        var viewModel = new DetailWindowViewModel(first, () => active);

        Assert.Same(second, Assert.Single(viewModel.SwitchTargets));

        viewModel.SwitchSessionCommand.Execute(second);

        Assert.Same(second, viewModel.Session);
        Assert.Same(first, Assert.Single(viewModel.SwitchTargets));
    }

    [Fact]
    public void SwitchCommandIgnoresTargetThatDisappearedAfterMenuOpened()
    {
        var first = new AudioSessionMeterViewModel(Snapshot("app:first", "First"));
        var second = new AudioSessionMeterViewModel(Snapshot("app:second", "Second"));
        var active = new List<AudioSessionMeterViewModel> { first, second };
        var viewModel = new DetailWindowViewModel(first, () => active);

        active.Remove(second);
        viewModel.SwitchSessionCommand.Execute(second);

        Assert.Same(first, viewModel.Session);
        Assert.Empty(viewModel.SwitchTargets);
    }

    [Fact]
    public void DetailWindowExposesTruePeakOversamplingOptions()
    {
        var session = new AudioSessionMeterViewModel(Snapshot());
        var viewModel = new DetailWindowViewModel(session);

        Assert.Equal(TruePeakOversampling.FourX, session.TruePeakOversampling);
        Assert.Contains(TruePeakOversampling.TwoX, viewModel.TruePeakOversamplingOptions);
        Assert.Contains(TruePeakOversampling.FourX, viewModel.TruePeakOversamplingOptions);
        Assert.Contains(TruePeakOversampling.EightX, viewModel.TruePeakOversamplingOptions);
        Assert.Contains(WaveformDisplayMode.Overall, viewModel.WaveformDisplayModeOptions);
        Assert.Contains(WaveformDisplayMode.Channels, viewModel.WaveformDisplayModeOptions);
        Assert.Contains(SpectrumDisplayMode.Overall, viewModel.SpectrumDisplayModeOptions);
        Assert.Contains(SpectrumDisplayMode.Channels, viewModel.SpectrumDisplayModeOptions);
    }

    [Fact]
    public void PopupDetailPreferencesLoadFromWorkspace()
    {
        var workspace = new UserWorkspaceSettings();
        var settings = workspace.GetOrCreateSource("plugin:first");
        settings.PopupWaveformVisible = true;
        settings.PopupSpectrumVisible = true;
        settings.PopupCollisionVisible = true;
        settings.PopupCollisionSourceKey = "plugin:second";
        settings.CollisionLevelFloorDb = -84;
        settings.CollisionProminenceThresholdDb = 9;
        settings.CollisionRelativeThreshold = 0.3;
        settings.SpectrumDisplayFloorDb = -160;
        settings.SpectrumDisplayCeilingDb = -18;
        settings.PopupStereo = new StereoWorkspaceSettings { ScaleRangeDb = 48 };
        var first = new AudioSessionMeterViewModel(Snapshot("plugin:first", "First", spectrumBuckets: [0.0, 0.4]));
        var second = new AudioSessionMeterViewModel(Snapshot("plugin:second", "Second", spectrumBuckets: [0.0, 0.41]));

        var viewModel = new DetailWindowViewModel(first, () => [first, second], workspace);

        Assert.True(viewModel.IsWaveformVisible);
        Assert.True(viewModel.IsSpectrumVisible);
        Assert.True(viewModel.IsCollisionVisible);
        Assert.Equal("plugin:second", viewModel.SelectedFrequencyCollisionSourceKey);
        Assert.Equal(-84, viewModel.CollisionLevelFloorDb);
        Assert.Equal(9, viewModel.CollisionProminenceThresholdDb);
        Assert.Equal(0.3, viewModel.CollisionRelativeThreshold);
        Assert.Equal(-160, viewModel.SpectrumDisplayFloorDb);
        Assert.Equal(-18, viewModel.SpectrumDisplayCeilingDb);
        Assert.Equal(48, viewModel.StereoSettings.ScaleRangeDb);
    }

    [Fact]
    public void PopupDetailPreferencesSaveToWorkspace()
    {
        var workspace = new UserWorkspaceSettings();
        var first = new AudioSessionMeterViewModel(Snapshot("plugin:first", "First", spectrumBuckets: [0.0, 0.4]));
        var second = new AudioSessionMeterViewModel(Snapshot("plugin:second", "Second", spectrumBuckets: [0.0, 0.41]));
        var viewModel = new DetailWindowViewModel(first, () => [first, second], workspace);

        viewModel.IsWaveformVisible = true;
        viewModel.IsSpectrumVisible = true;
        viewModel.IsCollisionVisible = true;
        viewModel.SelectedFrequencyCollisionSourceKey = "plugin:second";
        viewModel.CollisionLevelFloorDb = -90;
        viewModel.CollisionProminenceThresholdDb = 10;
        viewModel.CollisionRelativeThreshold = 0.22;
        viewModel.SpectrumDisplayFloorDb = -140;
        viewModel.SpectrumDisplayCeilingDb = -10;
        viewModel.StereoSettings.TracePersistenceSeconds = 0.2;

        var stored = workspace.GetSource("plugin:first");
        Assert.NotNull(stored);
        Assert.True(stored!.PopupWaveformVisible);
        Assert.True(stored.PopupSpectrumVisible);
        Assert.True(stored.PopupCollisionVisible);
        Assert.Equal("plugin:second", stored.PopupCollisionSourceKey);
        Assert.Equal(-90, stored.CollisionLevelFloorDb);
        Assert.Equal(10, stored.CollisionProminenceThresholdDb);
        Assert.Equal(0.22, stored.CollisionRelativeThreshold);
        Assert.Equal(-140, stored.SpectrumDisplayFloorDb);
        Assert.Equal(-10, stored.SpectrumDisplayCeilingDb);
        Assert.Equal(0.2, stored.PopupStereo?.TracePersistenceSeconds);
    }

    [Fact]
    public void DetailWindowSpectrumDisplayRangeClampsToAnalyzerWindow()
    {
        var session = new AudioSessionMeterViewModel(Snapshot());
        var viewModel = new DetailWindowViewModel(session);

        Assert.Equal("-120 to 0 dB", viewModel.SpectrumDisplayRangeText);

        viewModel.SpectrumDisplayFloorDb = -37.6;
        viewModel.SpectrumDisplayCeilingDb = -40;

        Assert.Equal(-38, viewModel.SpectrumDisplayFloorDb);
        Assert.Equal(-32, viewModel.SpectrumDisplayCeilingDb);
        Assert.Equal("-38 to -32 dB", viewModel.SpectrumDisplayRangeText);
    }

    [Fact]
    public void FrequencyCollisionIsAvailableInPopupDetail()
    {
        var first = new AudioSessionMeterViewModel(Snapshot("plugin:first", "First", spectrumBuckets: [0.0, 0.42, 0.72, 0.18]));
        var second = new AudioSessionMeterViewModel(Snapshot("plugin:second", "Second", spectrumBuckets: [0.0, 0.43, 0.18, 0.08]));
        var active = new List<AudioSessionMeterViewModel> { first, second };
        var viewModel = new DetailWindowViewModel(first, () => active);

        viewModel.IsCollisionVisible = true;

        Assert.True(viewModel.HasFrequencyCollisionSources);
        Assert.Equal("Global", viewModel.SelectedFrequencyCollisionSource.DisplayName);
        Assert.Contains(viewModel.FrequencyCollisionSourceOptions, option => option.SourceKey == "plugin:second");
        Assert.Contains(viewModel.FrequencyCollisionBands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);

        viewModel.SelectedFrequencyCollisionSourceKey = "plugin:second";

        Assert.Equal("Second", viewModel.SelectedFrequencyCollisionSource.DisplayName);
        Assert.NotEmpty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void FrequencyCollisionRefreshesWhenComparisonSpectrumTickAdvances()
    {
        var first = new AudioSessionMeterViewModel(Snapshot("plugin:first", "First", spectrumBuckets: [0.0, 0.40, 0.0], plotFrameVersion: 128));
        var second = new AudioSessionMeterViewModel(Snapshot("plugin:second", "Second", spectrumBuckets: [0.0, 0.08, 0.0], plotFrameVersion: 128));
        var active = new List<AudioSessionMeterViewModel> { first, second };
        using var viewModel = new DetailWindowViewModel(first, () => active)
        {
            IsCollisionVisible = true,
            SelectedFrequencyCollisionSourceKey = "plugin:second"
        };

        Assert.Empty(viewModel.FrequencyCollisionBands);

        second.Update(Snapshot("plugin:second", "Second", spectrumBuckets: [0.0, 0.42, 0.0], plotFrameVersion: 256));

        Assert.NotEmpty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void RollPanelMinimumsReserveOneHundredEightyFivePixelsPerTrack()
    {
        var session = new AudioSessionMeterViewModel(Snapshot());
        var viewModel = new DetailWindowViewModel(session);

        Assert.Equal(205, viewModel.WaveformOnlyPanelMinHeight);
        Assert.Equal(205, viewModel.SpectrumOnlyPanelMinHeight);
        Assert.Equal(418, viewModel.RollsPanelMinHeight);
        Assert.Equal(205, viewModel.WaveformOnlyPanelDefaultHeight);
        Assert.Equal(205, viewModel.SpectrumOnlyPanelDefaultHeight);
        Assert.Equal(546, viewModel.RollsPanelDefaultHeight);

        session.WaveformDisplayMode = WaveformDisplayMode.Channels;

        Assert.Equal(430, viewModel.WaveformOnlyPanelMinHeight);
        Assert.Equal(430, viewModel.WaveformRollGroupMinHeight);
        Assert.Equal(643, viewModel.RollsPanelMinHeight);
        Assert.Equal(430, viewModel.WaveformOnlyPanelDefaultHeight);
        Assert.Equal(771, viewModel.RollsPanelDefaultHeight);

        session.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        Assert.Equal(430, viewModel.SpectrumOnlyPanelMinHeight);
        Assert.Equal(430, viewModel.SpectrumRollGroupMinHeight);
        Assert.Equal(868, viewModel.RollsPanelMinHeight);
        Assert.Equal(430, viewModel.SpectrumOnlyPanelDefaultHeight);
        Assert.Equal(996, viewModel.RollsPanelDefaultHeight);
    }

    [Fact]
    public void StereoSettingsClampAndResetToDefaults()
    {
        var settings = new StereoImageDisplaySettings
        {
            IsAutoScale = false,
            ScaleRangeDb = 200,
            ManualCeilingDb = -100
        };

        Assert.Equal(96, settings.ScaleRangeDb);
        Assert.Equal(-60, settings.ManualCeilingDb);
        settings.AutoScaleFloorDb = -100;
        settings.AutoScaleSpeedDbPerSecond = 200;
        settings.TracePersistenceSeconds = 10;
        Assert.Equal(-72, settings.AutoScaleFloorDb);
        Assert.Equal(24, settings.AutoScaleSpeedDbPerSecond);
        Assert.Equal(0.25, settings.TracePersistenceSeconds);

        settings.TracePersistenceSeconds = 0;
        Assert.Equal(0.02, settings.TracePersistenceSeconds);

        settings.Reset();

        Assert.True(settings.IsAutoScale);
        Assert.Equal(StereoImageDisplaySettings.DefaultScaleRangeDb, settings.ScaleRangeDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultManualCeilingDb, settings.ManualCeilingDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultAutoScaleFloorDb, settings.AutoScaleFloorDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultAutoScaleSpeedDbPerSecond, settings.AutoScaleSpeedDbPerSecond);
        Assert.Equal(StereoImageDisplaySettings.DefaultTracePersistenceSeconds, settings.TracePersistenceSeconds);
    }

    private static AudioSessionSnapshot Snapshot(double? momentaryLufs = null)
    {
        return Snapshot("pid:42", "Test App", momentaryLufs);
    }

    private static AudioSessionSnapshot Snapshot(
        string key,
        string name,
        double? momentaryLufs = null,
        IReadOnlyList<double>? spectrumBuckets = null,
        long plotFrameVersion = 0)
    {
        return new AudioSessionSnapshot(
            key,
            42,
            name,
            0.5f,
            -6,
            "-6.0 dB",
            "Active",
            MomentaryLufs: momentaryLufs,
            SpectrumBuckets: spectrumBuckets,
            PlotFrameVersion: plotFrameVersion);
    }
}
