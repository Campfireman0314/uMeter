using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Themes;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class MainViewModelAdvancedControlsTests
{
    [Fact]
    public void ChangingMetricModeUpdatesExistingRows()
    {
        var viewModel = CreateViewModel(Snapshot(momentaryLufs: -21.2));
        viewModel.RefreshNow();

        viewModel.SelectedDisplayMode = MeterDisplayMode.Lufs;

        Assert.Equal("-21.2 LUFS", Assert.Single(viewModel.Sessions).PrimaryValueText);
    }

    [Fact]
    public void NewRowsUseCurrentMetricMode()
    {
        var viewModel = CreateViewModel(Snapshot(momentaryLufs: -19.9));
        viewModel.SelectedDisplayMode = MeterDisplayMode.Lufs;

        viewModel.RefreshNow();

        Assert.Equal("-19.9 LUFS", Assert.Single(viewModel.Sessions).PrimaryValueText);
    }

    [Fact]
    public void StoresTruePeakToggleAndOversamplingSelection()
    {
        var viewModel = CreateViewModel();

        viewModel.TruePeakEnabled = true;
        viewModel.SelectedTruePeakOversampling = TruePeakOversampling.EightX;

        Assert.True(viewModel.TruePeakEnabled);
        Assert.Equal(TruePeakOversampling.EightX, viewModel.SelectedTruePeakOversampling);
        Assert.Contains(TruePeakOversampling.TwoX, viewModel.TruePeakOversamplingOptions);
        Assert.Contains(TruePeakOversampling.FourX, viewModel.TruePeakOversamplingOptions);
        Assert.Contains(TruePeakOversampling.EightX, viewModel.TruePeakOversamplingOptions);
    }

    [Fact]
    public void TruePeakOversamplingSelectionAppliesToCurrentRows()
    {
        var service = new StaticAudioSessionMeterService([Snapshot("a", processId: 42), Snapshot("b", processId: 84)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();
        service.TruePeakOversamplingRequests.Clear();

        viewModel.SelectedTruePeakOversampling = TruePeakOversampling.EightX;

        Assert.Equal(
            [("a", 42, 8), ("b", 84, 8)],
            service.TruePeakOversamplingRequests);
    }

    [Fact]
    public void PerRowTruePeakOversamplingSelectionAppliesToThatRow()
    {
        var service = new StaticAudioSessionMeterService([Snapshot("a", processId: 42), Snapshot("b", processId: 84)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();
        service.TruePeakOversamplingRequests.Clear();

        viewModel.Sessions[0].TruePeakOversampling = TruePeakOversampling.TwoX;

        Assert.Equal([("a", 42, 2)], service.TruePeakOversamplingRequests);
        Assert.Equal(TruePeakOversampling.TwoX, viewModel.Sessions[0].TruePeakOversampling);
        Assert.Equal(TruePeakOversampling.FourX, viewModel.Sessions[1].TruePeakOversampling);
    }

    [Fact]
    public void PerRowAnalysisSettingsUseSourceIdentity()
    {
        var service = new StaticSignalSourceMeterService(
            [Source("plugin:lead", "Lead Vocal", SignalSourceKind.Plugin)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();
        service.TruePeakOversamplingRequests.Clear();
        service.ChannelWaveformRequests.Clear();
        service.ChannelSpectrumRequests.Clear();

        var row = Assert.Single(viewModel.Sessions);
        row.TruePeakOversampling = TruePeakOversampling.EightX;
        row.WaveformDisplayMode = WaveformDisplayMode.Channels;
        row.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, 8)], service.TruePeakOversamplingRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelWaveformRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelSpectrumRequests);
    }

    [Fact]
    public void PerSourceAnalysisSettingsSurviveReconnectBySourceKey()
    {
        var service = new SequenceSignalSourceMeterService(
            [Source("plugin:lead", "Lead Vocal", SignalSourceKind.Plugin)],
            [],
            [Source("plugin:lead", "Lead Vocal", SignalSourceKind.Plugin)]);
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            missingRowRetentionTicks: 0,
            isProcessAlive: _ => false);

        viewModel.RefreshNow();
        var firstRow = Assert.Single(viewModel.Sessions);
        service.TruePeakOversamplingRequests.Clear();
        service.ChannelWaveformRequests.Clear();
        service.ChannelSpectrumRequests.Clear();

        firstRow.TruePeakOversampling = TruePeakOversampling.EightX;
        firstRow.WaveformDisplayMode = WaveformDisplayMode.Channels;
        firstRow.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        viewModel.RefreshNow();
        Assert.Empty(viewModel.Sessions);

        viewModel.RefreshNow();
        var reconnected = Assert.Single(viewModel.Sessions);

        Assert.Equal(TruePeakOversampling.EightX, reconnected.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, reconnected.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, reconnected.SpectrumDisplayMode);
        Assert.Contains(("plugin:lead", SignalSourceKind.Plugin, null, 8), service.TruePeakOversamplingRequests);
        Assert.Contains(("plugin:lead", SignalSourceKind.Plugin, null, true), service.ChannelWaveformRequests);
        Assert.Contains(("plugin:lead", SignalSourceKind.Plugin, null, true), service.ChannelSpectrumRequests);
    }

    [Fact]
    public void GlobalTruePeakOversamplingRemainsDefaultForNewSourcesWithoutPreferences()
    {
        var service = new SequenceSignalSourceMeterService(
            [Source("plugin:lead", "Lead Vocal", SignalSourceKind.Plugin)]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.SelectedTruePeakOversampling = TruePeakOversampling.EightX;
        viewModel.RefreshNow();

        Assert.Equal(TruePeakOversampling.EightX, Assert.Single(viewModel.Sessions).TruePeakOversampling);
    }

    [Fact]
    public void PerRowResetUsesSourceIdentity()
    {
        var service = new StaticSignalSourceMeterService(
            [Source("plugin:lead", "Lead Vocal", SignalSourceKind.Plugin)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();

        var row = Assert.Single(viewModel.Sessions);
        row.ResetCommand.Execute(null);

        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null)], service.ResetRequests);
    }

    [Fact]
    public void PerRowSpectrumDisplayModeRequestsChannelSpectrumData()
    {
        var service = new StaticAudioSessionMeterService([Snapshot("a", processId: 42), Snapshot("b", processId: 84)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();
        service.ChannelSpectrumRequests.Clear();

        viewModel.Sessions[0].SpectrumDisplayMode = SpectrumDisplayMode.Channels;
        viewModel.Sessions[0].SpectrumDisplayMode = SpectrumDisplayMode.Overall;

        Assert.Equal([("a", 42, true), ("a", 42, false)], service.ChannelSpectrumRequests);
    }

    [Fact]
    public void PerRowWaveformDisplayModeRequestsChannelWaveformData()
    {
        var service = new StaticAudioSessionMeterService([Snapshot("a", processId: 42), Snapshot("b", processId: 84)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();
        service.ChannelWaveformRequests.Clear();

        viewModel.Sessions[0].WaveformDisplayMode = WaveformDisplayMode.Channels;
        viewModel.Sessions[0].WaveformDisplayMode = WaveformDisplayMode.Overall;

        Assert.Equal([("a", 42, true), ("a", 42, false)], service.ChannelWaveformRequests);
    }

    [Fact]
    public void ResetCommandsAdvanceExpectedCounters()
    {
        var service = new StaticAudioSessionMeterService([Snapshot("a", processId: 42), Snapshot("b", processId: 84)]);
        var viewModel = new MainViewModel(service, startTimer: false);
        viewModel.RefreshNow();

        viewModel.ResetAllCommand.Execute(null);

        Assert.All(viewModel.Sessions, row => Assert.Equal(1, row.ResetCount));
        Assert.Equal(1, service.ResetAllCount);

        var first = viewModel.Sessions[0];
        first.ResetCommand.Execute(null);

        Assert.Equal(2, first.ResetCount);
        Assert.Equal(1, viewModel.Sessions[1].ResetCount);
        Assert.Equal([42], service.ResetProcessIds);
    }

    [Fact]
    public void DetailCommandRaisesRequestForSelectedRow()
    {
        var viewModel = CreateViewModel(Snapshot("a"));
        viewModel.RefreshNow();

        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.True(viewModel.IsDetailViewVisible);

        viewModel.BackToGlobalCommand.Execute(null);

        Assert.Null(viewModel.SelectedDetailSession);
        Assert.False(viewModel.IsDetailViewVisible);
    }

    [Fact]
    public void PopupCommandRaisesRequestWithoutChangingInlineDetail()
    {
        var viewModel = CreateViewModel(Snapshot("a"));
        AudioSessionMeterViewModel? requested = null;
        viewModel.DetailPopupRequested += row => requested = row;
        viewModel.RefreshNow();

        var row = Assert.Single(viewModel.Sessions);
        row.OpenPopupCommand.Execute(null);

        Assert.Same(row, requested);
        Assert.False(viewModel.IsDetailViewVisible);
    }

    [Fact]
    public void InlineStereoSettingsCanResetToDefaults()
    {
        var viewModel = CreateViewModel();

        viewModel.InlineStereoSettings.IsAutoScale = false;
        viewModel.InlineStereoSettings.ScaleRangeDb = 24;
        viewModel.InlineStereoSettings.ManualCeilingDb = -12;
        viewModel.InlineStereoSettings.AutoScaleFloorDb = -36;
        viewModel.InlineStereoSettings.AutoScaleSpeedDbPerSecond = 48;
        viewModel.InlineStereoSettings.TracePersistenceSeconds = 0.24;
        viewModel.InlineStereoSettings.Reset();

        Assert.True(viewModel.InlineStereoSettings.IsAutoScale);
        Assert.Equal(StereoImageDisplaySettings.DefaultScaleRangeDb, viewModel.InlineStereoSettings.ScaleRangeDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultManualCeilingDb, viewModel.InlineStereoSettings.ManualCeilingDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultAutoScaleFloorDb, viewModel.InlineStereoSettings.AutoScaleFloorDb);
        Assert.Equal(StereoImageDisplaySettings.DefaultAutoScaleSpeedDbPerSecond, viewModel.InlineStereoSettings.AutoScaleSpeedDbPerSecond);
        Assert.Equal(StereoImageDisplaySettings.DefaultTracePersistenceSeconds, viewModel.InlineStereoSettings.TracePersistenceSeconds);
    }

    [Fact]
    public void InlineWaveformAndSpectrumRollsAreHiddenByDefault()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.IsInlineWaveformVisible);
        Assert.False(viewModel.IsInlineSpectrumVisible);
        Assert.False(viewModel.IsInlineWaveformOnlyVisible);
        Assert.False(viewModel.IsInlineSpectrumOnlyVisible);
        Assert.False(viewModel.AreInlineRollsVisible);

        viewModel.IsInlineWaveformVisible = true;

        Assert.True(viewModel.IsInlineWaveformOnlyVisible);

        viewModel.IsInlineSpectrumVisible = true;

        Assert.False(viewModel.IsInlineWaveformOnlyVisible);
        Assert.True(viewModel.AreInlineRollsVisible);
    }

    [Fact]
    public void InlineRollPanelMinimumsFollowSelectedDisplayModes()
    {
        var viewModel = CreateViewModel(Snapshot("a"));
        viewModel.RefreshNow();

        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        Assert.Equal(205, viewModel.InlineWaveformOnlyPanelMinHeight);
        Assert.Equal(205, viewModel.InlineSpectrumOnlyPanelMinHeight);
        Assert.Equal(418, viewModel.InlineRollsPanelMinHeight);
        Assert.Equal(205, viewModel.InlineWaveformOnlyPanelDefaultHeight);
        Assert.Equal(205, viewModel.InlineSpectrumOnlyPanelDefaultHeight);
        Assert.Equal(546, viewModel.InlineRollsPanelDefaultHeight);

        row.WaveformDisplayMode = WaveformDisplayMode.Channels;

        Assert.Equal(430, viewModel.InlineWaveformOnlyPanelMinHeight);
        Assert.Equal(430, viewModel.InlineWaveformRollGroupMinHeight);
        Assert.Equal(643, viewModel.InlineRollsPanelMinHeight);
        Assert.Equal(430, viewModel.InlineWaveformOnlyPanelDefaultHeight);
        Assert.Equal(771, viewModel.InlineRollsPanelDefaultHeight);

        row.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        Assert.Equal(430, viewModel.InlineSpectrumOnlyPanelMinHeight);
        Assert.Equal(430, viewModel.InlineSpectrumRollGroupMinHeight);
        Assert.Equal(868, viewModel.InlineRollsPanelMinHeight);
        Assert.Equal(430, viewModel.InlineSpectrumOnlyPanelDefaultHeight);
        Assert.Equal(996, viewModel.InlineRollsPanelDefaultHeight);
    }

    [Fact]
    public void ToggleAlwaysOnTopCommandFlipsState()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.IsAlwaysOnTop);

        viewModel.ToggleAlwaysOnTopCommand.Execute(null);

        Assert.False(viewModel.IsAlwaysOnTop);
    }

    [Fact]
    public void EcoModeUpdatesRefreshLabelAndServicePerformanceMode()
    {
        var service = new StaticAudioSessionMeterService([]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.IsEcoModeEnabled = true;

        Assert.True(viewModel.IsEcoModeEnabled);
        Assert.Equal("100 ms", viewModel.RefreshLabel);
        Assert.Equal([PcmAnalysisPerformanceMode.Eco], service.PerformanceModeRequests);

        viewModel.IsEcoModeEnabled = false;

        Assert.Equal("33 ms", viewModel.RefreshLabel);
        Assert.Equal([PcmAnalysisPerformanceMode.Eco, PcmAnalysisPerformanceMode.Normal], service.PerformanceModeRequests);
    }

    [Fact]
    public void ThemeOptionsIncludeCustomPalettes()
    {
        var viewModel = CreateViewModel();

        Assert.Same(AppThemes.Default, viewModel.SelectedTheme);
        Assert.Equal("Midnight", viewModel.SelectedThemeName);
        Assert.Contains(viewModel.ThemeOptions, theme => theme.Id == "milk-white");
        Assert.Contains(viewModel.ThemeOptions, theme => theme.Id == "forest-green");
        Assert.Contains(viewModel.ThemeOptions, theme => theme.Id == "pink-dream");
    }

    [Fact]
    public void SelectThemeCommandUpdatesSelectedTheme()
    {
        var viewModel = CreateViewModel();
        var theme = viewModel.ThemeOptions.Single(theme => theme.Id == "forest-green");

        viewModel.SelectThemeCommand.Execute(theme);

        Assert.Same(theme, viewModel.SelectedTheme);
        Assert.Equal("Forest Green", viewModel.SelectedThemeName);
    }

    [Fact]
    public void SelectThemeCommandRaisesPersistenceCallback()
    {
        AppTheme? savedTheme = null;
        var viewModel = new MainViewModel(
            new StaticAudioSessionMeterService([]),
            startTimer: false,
            selectedTheme: AppThemes.FindById("milk-white"),
            themeSelected: theme => savedTheme = theme);
        var theme = viewModel.ThemeOptions.Single(theme => theme.Id == "pink-dream");

        viewModel.SelectThemeCommand.Execute(theme);

        Assert.Same(theme, savedTheme);
        Assert.Equal("Pink Dream", viewModel.SelectedThemeName);
    }

    [Fact]
    public void ThemeLookupFallsBackForUnknownSavedTheme()
    {
        Assert.Same(AppThemes.Default, AppThemes.FindById("does-not-exist"));
        Assert.Equal("Milk White", AppThemes.FindById("MILK-WHITE").DisplayName);
        Assert.Equal("Pink Dream", AppThemes.FindById("teen-girl-pink").DisplayName);
    }

    [Fact]
    public void FrequencyCollisionDetectsSimilarSpectrumBins()
    {
        var service = new StaticSignalSourceMeterService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.0, 0.40, 0.72, 0.08]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.0, 0.43, 0.18, 0.09])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "plugin:lead").OpenDetailCommand.Execute(null);
        viewModel.IsInlineCollisionVisible = true;

        Assert.True(viewModel.HasFrequencyCollisionSources);
        Assert.Equal("Global", viewModel.SelectedFrequencyCollisionSource.DisplayName);
        Assert.Contains(viewModel.FrequencyCollisionBands, band => band.StartRatio <= 0.25 && band.EndRatio >= 0.5);
    }

    [Fact]
    public void FrequencyCollisionThresholdsSuppressDistantBins()
    {
        var service = new StaticSignalSourceMeterService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.7]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.2])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false)
        {
            CollisionAbsoluteThresholdDb = 3,
            CollisionRelativeThreshold = 0.1
        };

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "plugin:lead").OpenDetailCommand.Execute(null);
        viewModel.IsInlineCollisionVisible = true;

        Assert.Empty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void FrequencyCollisionIgnoresSharedSilence()
    {
        var service = new StaticSignalSourceMeterService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.01, 0.02]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.01, 0.02])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "plugin:lead").OpenDetailCommand.Execute(null);
        viewModel.IsInlineCollisionVisible = true;

        Assert.Empty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void FrequencyCollisionSourceOptionsFollowLiveSpectrumRows()
    {
        var service = new SequenceSignalSourceMeterService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.4]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.41])
            ],
            [
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.41]),
                Source("plugin:pad", "Pad", SignalSourceKind.Plugin, spectrumBuckets: [0.40])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "plugin:lead").OpenDetailCommand.Execute(null);
        viewModel.IsInlineCollisionVisible = true;
        viewModel.SelectedFrequencyCollisionSourceKey = "plugin:keys";

        Assert.Equal("Keys", viewModel.SelectedFrequencyCollisionSource.DisplayName);

        viewModel.RefreshNow();

        Assert.DoesNotContain(viewModel.FrequencyCollisionSourceOptions, option => option.SourceKey == "plugin:lead");
        Assert.Single(viewModel.FrequencyCollisionSourceOptions);
        Assert.True(viewModel.SelectedFrequencyCollisionSource.IsAllSources);
        Assert.Equal("Global", viewModel.SelectedFrequencyCollisionSource.DisplayName);
        Assert.Equal("__global__", viewModel.SelectedFrequencyCollisionSourceKey);
        Assert.Empty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void FrequencyCollisionGlobalDisplaySurvivesUnchangedRefresh()
    {
        var service = new StaticSignalSourceMeterService(
            [
                Source("wasapi:desktop", "Desktop Audio", SignalSourceKind.WasapiSession, spectrumBuckets: [0.4]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.41])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "wasapi:desktop").OpenDetailCommand.Execute(null);
        viewModel.IsInlineCollisionVisible = true;
        var options = viewModel.FrequencyCollisionSourceOptions;

        Assert.Equal("__global__", viewModel.SelectedFrequencyCollisionSourceKey);
        Assert.Equal("Global", viewModel.SelectedFrequencyCollisionSourceDisplayName);

        viewModel.RefreshNow();

        Assert.Same(options, viewModel.FrequencyCollisionSourceOptions);
        Assert.Equal("__global__", viewModel.SelectedFrequencyCollisionSourceKey);
        Assert.Equal("Global", viewModel.SelectedFrequencyCollisionSourceDisplayName);
    }

    [Fact]
    public void FrequencyCollisionIsDisabledByDefaultInDetailView()
    {
        var service = new StaticSignalSourceMeterService(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin, spectrumBuckets: [0.4]),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin, spectrumBuckets: [0.41])
            ]);
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        viewModel.Sessions.Single(session => session.SourceKey == "plugin:lead").OpenDetailCommand.Execute(null);

        Assert.False(viewModel.IsInlineCollisionVisible);
        Assert.Empty(viewModel.FrequencyCollisionBands);
    }

    [Fact]
    public void DefaultRefreshCadenceIsFasterThanOriginalHundredMilliseconds()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.RefreshInterval < TimeSpan.FromMilliseconds(100));
    }

    private static MainViewModel CreateViewModel(params AudioSessionSnapshot[] snapshots)
    {
        return new MainViewModel(new StaticAudioSessionMeterService(snapshots), startTimer: false);
    }

    private static AudioSessionSnapshot Snapshot(string key = "a", double? momentaryLufs = null, int processId = 42)
    {
        return new AudioSessionSnapshot(
            key,
            processId,
            $"App {key}",
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active",
            momentaryLufs);
    }

    private static SignalSourceSnapshot Source(
        string key,
        string name,
        SignalSourceKind kind,
        IReadOnlyList<double>? spectrumBuckets = null)
    {
        return new SignalSourceSnapshot(
            key,
            kind,
            null,
            name,
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active",
            SpectrumBuckets: spectrumBuckets);
    }

    private sealed class StaticAudioSessionMeterService :
        IAudioSessionMeterService,
        IAudioSessionResetService,
        IAudioSessionAnalysisSettingsService
    {
        private readonly IReadOnlyList<AudioSessionSnapshot> _snapshots;

        public List<int> ResetProcessIds { get; } = [];

        public List<(string SessionKey, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SessionKey, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SessionKey, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public int ResetAllCount { get; private set; }

        public StaticAudioSessionMeterService(IReadOnlyList<AudioSessionSnapshot> snapshots)
        {
            _snapshots = snapshots;
        }

        public IReadOnlyList<AudioSessionSnapshot> GetCurrentSessions()
        {
            return _snapshots;
        }

        public void ResetSession(string sessionKey, int? processId)
        {
            if (processId is int value)
            {
                ResetProcessIds.Add(value);
            }
        }

        public void ResetAllSessions()
        {
            ResetAllCount++;
        }

        public void SetTruePeakOversampling(string sessionKey, int? processId, int oversampling)
        {
            TruePeakOversamplingRequests.Add((sessionKey, processId, oversampling));
        }

        public void SetChannelWaveformEnabled(string sessionKey, int? processId, bool enabled)
        {
            ChannelWaveformRequests.Add((sessionKey, processId, enabled));
        }

        public void SetChannelSpectrumEnabled(string sessionKey, int? processId, bool enabled)
        {
            ChannelSpectrumRequests.Add((sessionKey, processId, enabled));
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            PerformanceModeRequests.Add(mode);
        }
    }

    private sealed class StaticSignalSourceMeterService :
        ISignalSourceMeterService,
        ISignalSourceResetService,
        ISignalSourceAnalysisSettingsService
    {
        private readonly IReadOnlyList<SignalSourceSnapshot> _snapshots;

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId)> ResetRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public StaticSignalSourceMeterService(IReadOnlyList<SignalSourceSnapshot> snapshots)
        {
            _snapshots = snapshots;
        }

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return _snapshots;
        }

        public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
        {
            ResetRequests.Add((sourceKey, sourceKind, processId));
        }

        public void ResetAllSources()
        {
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

    private sealed class SequenceSignalSourceMeterService :
        ISignalSourceMeterService,
        ISignalSourceResetService,
        ISignalSourceAnalysisSettingsService
    {
        private readonly Queue<IReadOnlyList<SignalSourceSnapshot>> _responses;

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public SequenceSignalSourceMeterService(params IReadOnlyList<SignalSourceSnapshot>[] responses)
        {
            _responses = new Queue<IReadOnlyList<SignalSourceSnapshot>>(responses);
        }

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return _responses.Dequeue();
        }

        public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
        {
        }

        public void ResetAllSources()
        {
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
