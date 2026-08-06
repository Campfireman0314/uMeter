using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using DesktopLoudnessMeter.App.Localization;
using DesktopLoudnessMeter.App.Settings;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class DetailWindowViewModel : ObservableObject, IDisposable
{
    private const double RollTrackMinHeight = 185;
    private const double RollGroupPadding = 20;
    private const double RollChannelLabelAllowance = 40;
    private const double RollSplitterHeight = 8;
    private const double RollSplitterFlexAllowance = 128;
    private const double CollisionLevelMinimumDb = -200;
    private const double DefaultCollisionLevelFloorDb = -120;
    private const double DefaultCollisionProminenceThresholdDb = 6;
    private const double DefaultCollisionRelativeThreshold = 0.18;
    private const double SpectrumDisplayMinimumDb = -200;
    private const double SpectrumDisplayMaximumDb = 0;
    private const double SpectrumDisplayDefaultFloorDb = -96;
    private const double SpectrumDisplayDefaultCeilingDb = -6;
    private const double SpectrumDisplayMinimumSpanDb = 6;
    private const double LoudnessClockMinimumLimitDb = -120;
    private const double LoudnessClockMaximumLimitDb = 0;
    private const double LoudnessClockDefaultMinimumDb = -32;
    private const double LoudnessClockDefaultMaximumDb = -4;
    private const double LoudnessClockDefaultWarningDb = -12;
    private const double LoudnessClockMinimumSpanDb = 6;
    private const double LoudnessClockMinimumCycleSeconds = 10;
    private const double LoudnessClockMaximumCycleSeconds = 600;
    private const double LoudnessClockDefaultCycleSeconds = 30;
    private static readonly FrequencyCollisionSourceOption AllCollisionSourcesOption = new("__global__", "Global", isAllSources: true);
    private readonly Func<IReadOnlyList<AudioSessionMeterViewModel>> _getActiveSessions;
    private readonly UserWorkspaceSettings _workspaceSettings;
    private readonly MainViewModel? _globalSettingsOwner;
    private readonly RelayCommand _switchSessionCommand;
    private readonly RelayCommand _selectFrequencyCollisionSourceCommand;
    private readonly HashSet<AudioSessionMeterViewModel> _collisionSourceSubscriptions = [];
    private AudioSessionMeterViewModel _session;
    private IReadOnlyList<AudioSessionMeterViewModel> _switchTargets = Array.Empty<AudioSessionMeterViewModel>();
    private IReadOnlyList<FrequencyCollisionSourceOption> _frequencyCollisionSourceOptions = [AllCollisionSourcesOption];
    private IReadOnlyList<FrequencyCollisionBand> _frequencyCollisionBands = Array.Empty<FrequencyCollisionBand>();
    private string _selectedFrequencyCollisionSourceKey = AllCollisionSourcesOption.SourceKey;
    private double _collisionLevelFloorDb = DefaultCollisionLevelFloorDb;
    private double _collisionProminenceThresholdDb = DefaultCollisionProminenceThresholdDb;
    private double _collisionRelativeThreshold = DefaultCollisionRelativeThreshold;
    private CollisionFrequencyScale _collisionFrequencyScale = CollisionFrequencyScale.Logarithmic;
    private double _spectrumDisplayFloorDb = SpectrumDisplayDefaultFloorDb;
    private double _spectrumDisplayCeilingDb = SpectrumDisplayDefaultCeilingDb;
    private double _loudnessClockMinimumDb = LoudnessClockDefaultMinimumDb;
    private double _loudnessClockMaximumDb = LoudnessClockDefaultMaximumDb;
    private double _loudnessClockWarningDb = LoudnessClockDefaultWarningDb;
    private double _loudnessClockCycleSeconds = LoudnessClockDefaultCycleSeconds;
    private bool _isAlwaysOnTop = true;
    private bool _isDisposed;
    private bool _isApplyingWorkspacePreferences;
    private string _detailLayoutStatusText = string.Empty;

    public DetailWindowViewModel(
        AudioSessionMeterViewModel session,
        Func<IReadOnlyList<AudioSessionMeterViewModel>>? getActiveSessions = null,
        UserWorkspaceSettings? workspaceSettings = null,
        MainViewModel? globalSettingsOwner = null)
    {
        _session = session;
        _session.PropertyChanged += SessionOnPropertyChanged;
        _getActiveSessions = getActiveSessions ?? (() => Array.Empty<AudioSessionMeterViewModel>());
        _workspaceSettings = workspaceSettings ?? new UserWorkspaceSettings();
        _globalSettingsOwner = globalSettingsOwner;
        if (_globalSettingsOwner is not null)
        {
            _globalSettingsOwner.PropertyChanged += GlobalSettingsOwnerOnPropertyChanged;
        }
        StereoSettings = new StereoImageDisplaySettings();
        StereoSettings.PropertyChanged += StereoSettingsOnPropertyChanged;
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        ResetCommand = new RelayCommand(_ => Session.RequestReset());
        _selectFrequencyCollisionSourceCommand = new RelayCommand(SelectFrequencyCollisionSource);
        _switchSessionCommand = new RelayCommand(SwitchSession, CanSwitchSession);
        DetailModules =
        [
            StereoModule,
            CollisionModule,
            LoudnessClockModule,
            WaveformModule,
            SpectrumModule
        ];
        ApplyDetailModuleLayouts(DetailModules, null);
        foreach (var module in DetailModules)
        {
            module.PropertyChanged += DetailModuleOnPropertyChanged;
        }

        LocalizationManager.LanguageChanged += LocalizationManagerOnLanguageChanged;

        ApplyWorkspacePreferences();
        RefreshSwitchTargets();
        RefreshFrequencyCollisionOptions();
    }

    public AudioSessionMeterViewModel Session
    {
        get => _session;
        private set
        {
            if (ReferenceEquals(_session, value))
            {
                return;
            }

            _session.PropertyChanged -= SessionOnPropertyChanged;
            _session = value;
            _session.PropertyChanged += SessionOnPropertyChanged;

            OnPropertyChanged();
            ApplyWorkspacePreferences();
            NotifyRollLayoutChanged();
            RefreshSwitchTargets();
            RefreshFrequencyCollisionOptions();
            RefreshFrequencyCollisions();
        }
    }

    public StereoImageDisplaySettings StereoSettings { get; }

    public DetailModuleViewModel StereoModule { get; } = new(DetailModuleKind.Stereo, "Stereo image", isVisible: true);

    public DetailModuleViewModel CollisionModule { get; } = new(DetailModuleKind.Collision, "Frequency collision", isVisible: false);

    public DetailModuleViewModel LoudnessClockModule { get; } = new(DetailModuleKind.LoudnessClock, "Clock", isVisible: false);

    public DetailModuleViewModel WaveformModule { get; } = new(DetailModuleKind.Waveform, "Waveform", isVisible: false);

    public DetailModuleViewModel SpectrumModule { get; } = new(DetailModuleKind.Spectrum, "Spectrum", isVisible: false);

    public IReadOnlyList<DetailModuleViewModel> DetailModules { get; }

    public string DetailLayoutStatusText
    {
        get => _detailLayoutStatusText;
        private set => SetProperty(ref _detailLayoutStatusText, value);
    }

    public IReadOnlyList<TruePeakOversampling> TruePeakOversamplingOptions { get; } =
    [
        TruePeakOversampling.TwoX,
        TruePeakOversampling.FourX,
        TruePeakOversampling.EightX
    ];

    public IReadOnlyList<SpectrumDisplayMode> SpectrumDisplayModeOptions { get; } =
    [
        SpectrumDisplayMode.Overall,
        SpectrumDisplayMode.Channels
    ];

    public IReadOnlyList<WaveformDisplayMode> WaveformDisplayModeOptions { get; } =
    [
        WaveformDisplayMode.Overall,
        WaveformDisplayMode.Channels
    ];

    public IReadOnlyList<SignalMeasurementMode> SignalMeasurementModeOptions { get; } =
    [
        SignalMeasurementMode.Overall,
        SignalMeasurementMode.Mid,
        SignalMeasurementMode.Side
    ];

    public bool EnableDetailHoverHints => _globalSettingsOwner?.EnableDetailHoverHints
        ?? _workspaceSettings.EnableDetailHoverHints
        ?? true;

    public IReadOnlyList<CollisionFrequencyScale> CollisionFrequencyScaleOptions { get; } =
    [
        CollisionFrequencyScale.Logarithmic,
        CollisionFrequencyScale.Mel
    ];

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public bool IsStereoVisible
    {
        get => StereoModule.IsVisible;
        set => StereoModule.IsVisible = value;
    }

    public bool IsWaveformVisible
    {
        get => WaveformModule.IsVisible;
        set => WaveformModule.IsVisible = value;
    }

    public bool IsSpectrumVisible
    {
        get => SpectrumModule.IsVisible;
        set => SpectrumModule.IsVisible = value;
    }

    public bool IsLoudnessClockVisible
    {
        get => LoudnessClockModule.IsVisible;
        set => LoudnessClockModule.IsVisible = value;
    }

    public bool IsCollisionVisible
    {
        get => CollisionModule.IsVisible;
        set => CollisionModule.IsVisible = value;
    }

    public IReadOnlyList<FrequencyCollisionSourceOption> FrequencyCollisionSourceOptions
    {
        get => _frequencyCollisionSourceOptions;
        private set => SetProperty(ref _frequencyCollisionSourceOptions, value);
    }

    public string SelectedFrequencyCollisionSourceKey
    {
        get => _selectedFrequencyCollisionSourceKey;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? AllCollisionSourcesOption.SourceKey : value;
            if (SetProperty(ref _selectedFrequencyCollisionSourceKey, next))
            {
                OnPropertyChanged(nameof(SelectedFrequencyCollisionSource));
                OnPropertyChanged(nameof(SelectedFrequencyCollisionSourceDisplayName));
                SaveWorkspacePreferences();
                RefreshFrequencyCollisions();
            }
        }
    }

    public FrequencyCollisionSourceOption SelectedFrequencyCollisionSource
    {
        get => FindFrequencyCollisionSourceOption(SelectedFrequencyCollisionSourceKey);
        set
        {
            if (value is not null)
            {
                SelectedFrequencyCollisionSourceKey = value.SourceKey;
            }
        }
    }

    public double CollisionLevelFloorDb
    {
        get => _collisionLevelFloorDb;
        set
        {
            var next = Math.Clamp(RoundDisplayDb(value), CollisionLevelMinimumDb, SpectrumDisplayMaximumDb);
            if (SetProperty(ref _collisionLevelFloorDb, next))
            {
                OnPropertyChanged(nameof(CollisionLevelFloorText));
                SaveWorkspacePreferences();
                RefreshFrequencyCollisions();
            }
        }
    }

    public string CollisionLevelFloorText => $"{CollisionLevelFloorDb:0} dB";

    public double CollisionProminenceThresholdDb
    {
        get => _collisionProminenceThresholdDb;
        set
        {
            var next = Math.Clamp(RoundDisplayDb(value), 0, 24);
            if (SetProperty(ref _collisionProminenceThresholdDb, next))
            {
                OnPropertyChanged(nameof(CollisionProminenceThresholdText));
                SaveWorkspacePreferences();
                RefreshFrequencyCollisions();
            }
        }
    }

    public string CollisionProminenceThresholdText => $"{CollisionProminenceThresholdDb:0} dB";

    public double CollisionRelativeThreshold
    {
        get => _collisionRelativeThreshold;
        set
        {
            var next = Math.Clamp(value, 0.02, 0.8);
            if (SetProperty(ref _collisionRelativeThreshold, next))
            {
                OnPropertyChanged(nameof(CollisionRelativeThresholdText));
                SaveWorkspacePreferences();
                RefreshFrequencyCollisions();
            }
        }
    }

    public string CollisionRelativeThresholdText => $"{CollisionRelativeThreshold:P0}";

    public CollisionFrequencyScale CollisionFrequencyScale
    {
        get => _collisionFrequencyScale;
        set
        {
            if (SetProperty(ref _collisionFrequencyScale, value))
            {
                OnPropertyChanged(nameof(CollisionFrequencyScaleText));
                SaveWorkspacePreferences();
                RefreshFrequencyCollisions();
            }
        }
    }

    public string CollisionFrequencyScaleText => CollisionFrequencyScale.ToDisplayLabel();

    public double SpectrumDisplayFloorDb
    {
        get => _spectrumDisplayFloorDb;
        set
        {
            var next = Math.Clamp(
                RoundDisplayDb(value),
                SpectrumDisplayMinimumDb,
                SpectrumDisplayCeilingDb - SpectrumDisplayMinimumSpanDb);
            if (SetProperty(ref _spectrumDisplayFloorDb, next))
            {
                OnPropertyChanged(nameof(SpectrumDisplayRangeText));
                SaveWorkspacePreferences();
            }
        }
    }

    public double SpectrumDisplayCeilingDb
    {
        get => _spectrumDisplayCeilingDb;
        set
        {
            var next = Math.Clamp(
                RoundDisplayDb(value),
                SpectrumDisplayFloorDb + SpectrumDisplayMinimumSpanDb,
                SpectrumDisplayMaximumDb);
            if (SetProperty(ref _spectrumDisplayCeilingDb, next))
            {
                OnPropertyChanged(nameof(SpectrumDisplayRangeText));
                SaveWorkspacePreferences();
            }
        }
    }

    public string SpectrumDisplayRangeText => LocalizationManager.Format(
        "{0} to {1} dB",
        SpectrumDisplayFloorDb.ToString("0"),
        SpectrumDisplayCeilingDb.ToString("0"));

    public double LoudnessClockMinimumDb
    {
        get => _loudnessClockMinimumDb;
        set
        {
            var next = Math.Clamp(
                RoundLoudnessClockDb(value),
                LoudnessClockMinimumLimitDb,
                LoudnessClockMaximumDb - LoudnessClockMinimumSpanDb);
            if (SetProperty(ref _loudnessClockMinimumDb, next))
            {
                CoerceLoudnessClockWarningToRange();
                OnPropertyChanged(nameof(LoudnessClockRangeText));
                SaveWorkspacePreferences();
            }
        }
    }

    public double LoudnessClockMaximumDb
    {
        get => _loudnessClockMaximumDb;
        set
        {
            var next = Math.Clamp(
                RoundLoudnessClockDb(value),
                LoudnessClockMinimumDb + LoudnessClockMinimumSpanDb,
                LoudnessClockMaximumLimitDb);
            if (SetProperty(ref _loudnessClockMaximumDb, next))
            {
                CoerceLoudnessClockWarningToRange();
                OnPropertyChanged(nameof(LoudnessClockRangeText));
                SaveWorkspacePreferences();
            }
        }
    }

    public string LoudnessClockRangeText => LocalizationManager.Format(
        "{0} to {1} LUFS",
        LoudnessClockMinimumDb.ToString("0"),
        LoudnessClockMaximumDb.ToString("0"));

    public double LoudnessClockWarningDb
    {
        get => _loudnessClockWarningDb;
        set
        {
            var next = Math.Clamp(
                RoundLoudnessClockDb(value),
                LoudnessClockMinimumDb,
                LoudnessClockMaximumDb);
            if (SetProperty(ref _loudnessClockWarningDb, next))
            {
                OnPropertyChanged(nameof(LoudnessClockWarningText));
                SaveWorkspacePreferences();
            }
        }
    }

    public string LoudnessClockWarningText => $"{LoudnessClockWarningDb:0} LUFS";

    public double LoudnessClockCycleSeconds
    {
        get => _loudnessClockCycleSeconds;
        set
        {
            var next = Math.Clamp(
                RoundLoudnessClockCycleSeconds(value),
                LoudnessClockMinimumCycleSeconds,
                LoudnessClockMaximumCycleSeconds);
            if (SetProperty(ref _loudnessClockCycleSeconds, next))
            {
                OnPropertyChanged(nameof(LoudnessClockCycleText));
                SaveWorkspacePreferences();
            }
        }
    }

    public string LoudnessClockCycleText => $"{LoudnessClockCycleSeconds:0} s";

    public string SelectedFrequencyCollisionSourceDisplayName => SelectedFrequencyCollisionSource.DisplayName;

    public IReadOnlyList<FrequencyCollisionBand> FrequencyCollisionBands
    {
        get => _frequencyCollisionBands;
        private set => SetProperty(ref _frequencyCollisionBands, value);
    }

    public bool HasFrequencyCollisionSources =>
        !Session.IsInstanceMissing
        && Session.SpectrumBuckets.Count > 0
        && _getActiveSessions().Any(session =>
            !ReferenceEquals(session, Session)
            && !session.IsInstanceMissing
            && session.SpectrumBuckets.Count > 0);

    public bool IsWaveformOnlyVisible => IsWaveformVisible;

    public bool IsSpectrumOnlyVisible => IsSpectrumVisible;

    public bool AreRollsVisible => false;

    public double WaveformOnlyPanelMinHeight => RollGroupMinHeight(Session.IsChannelWaveformVisible);

    public double WaveformOnlyPanelDisplayMinHeight => IsWaveformVisible ? WaveformOnlyPanelMinHeight : 0;

    public double SpectrumOnlyPanelMinHeight => RollGroupMinHeight(Session.IsChannelSpectrumVisible);

    public double SpectrumOnlyPanelDisplayMinHeight => IsSpectrumVisible ? SpectrumOnlyPanelMinHeight : 0;

    public double WaveformOnlyPanelDefaultHeight => WaveformOnlyPanelMinHeight;

    public double SpectrumOnlyPanelDefaultHeight => SpectrumOnlyPanelMinHeight;

    public double WaveformRollGroupMinHeight => RollGroupMinHeight(Session.IsChannelWaveformVisible);

    public double SpectrumRollGroupMinHeight => RollGroupMinHeight(Session.IsChannelSpectrumVisible);

    public double RollsPanelMinHeight => WaveformRollGroupMinHeight + RollSplitterHeight + SpectrumRollGroupMinHeight;

    public double RollsPanelDefaultHeight => RollsPanelMinHeight + RollSplitterFlexAllowance;

    public ICommand ToggleAlwaysOnTopCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand SelectFrequencyCollisionSourceCommand => _selectFrequencyCollisionSourceCommand;

    public ICommand SwitchSessionCommand => _switchSessionCommand;

    public IReadOnlyList<AudioSessionMeterViewModel> SwitchTargets
    {
        get => _switchTargets;
        private set => SetProperty(ref _switchTargets, value);
    }

    public bool HasSwitchTargets => SwitchTargets.Count > 0;

    public void MoveDetailModuleTo(DetailModuleViewModel source, DetailModuleViewModel target, bool insertAfter)
    {
        if (ReferenceEquals(source, target)
            || !DetailModules.Contains(source)
            || !DetailModules.Contains(target))
        {
            return;
        }

        _isApplyingWorkspacePreferences = true;
        try
        {
            MoveDetailModule(DetailModules, source, target, insertAfter);
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }

        SaveWorkspacePreferences();
    }

    public void ClearDetailModuleDropHints()
    {
        foreach (var module in DetailModules)
        {
            module.SetReorderDropHint(before: false, after: false);
        }
    }

    public void RefreshSwitchTargets()
    {
        SwitchTargets = _getActiveSessions()
            .Where(session => !ReferenceEquals(session, Session) && !session.IsInstanceMissing)
            .ToArray();
        OnPropertyChanged(nameof(HasSwitchTargets));
        _switchSessionCommand.RaiseCanExecuteChanged();
        RefreshCollisionSourceSubscriptions();
        RefreshFrequencyCollisionOptions();
        RefreshFrequencyCollisions();
    }

    public bool SaveDetailLayout(string? path)
    {
        if (DetailLayoutPresetStore.Save(path, CreateDetailLayoutPreset(), out var error))
        {
            DetailLayoutStatusText = LocalizationManager.Format("Saved detail layout: {0}", Path.GetFileName(path));
            return true;
        }

        DetailLayoutStatusText = LocalizationManager.Format("Layout save failed: {0}", error);
        return false;
    }

    public bool LoadDetailLayout(string? path)
    {
        if (!DetailLayoutPresetStore.TryLoad(path, out var preset, out var error))
        {
            DetailLayoutStatusText = LocalizationManager.Format("Layout load failed: {0}", error);
            return false;
        }

        ApplyDetailLayoutPreset(preset, persist: true);
        DetailLayoutStatusText = LocalizationManager.Format("Loaded detail layout: {0}", Path.GetFileName(path));
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        LocalizationManager.LanguageChanged -= LocalizationManagerOnLanguageChanged;
        _session.PropertyChanged -= SessionOnPropertyChanged;
        if (_globalSettingsOwner is not null)
        {
            _globalSettingsOwner.PropertyChanged -= GlobalSettingsOwnerOnPropertyChanged;
        }

        StereoSettings.PropertyChanged -= StereoSettingsOnPropertyChanged;
        foreach (var module in DetailModules)
        {
            module.PropertyChanged -= DetailModuleOnPropertyChanged;
        }

        ClearCollisionSourceSubscriptions();
    }

    private void LocalizationManagerOnLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var module in DetailModules)
        {
            module.RefreshLocalizedText();
        }

        Session.RefreshLocalizedText();
        OnPropertyChanged(nameof(CollisionFrequencyScaleText));
        OnPropertyChanged(nameof(SpectrumDisplayRangeText));
        OnPropertyChanged(nameof(LoudnessClockRangeText));
        OnPropertyChanged(nameof(SelectedFrequencyCollisionSourceDisplayName));
    }

    private void StereoSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        SaveWorkspacePreferences();
    }

    private void DetailModuleOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is not DetailModuleViewModel module)
        {
            return;
        }

        if (args.PropertyName is nameof(DetailModuleViewModel.IsVisible))
        {
            NotifyDetailModuleVisibilityChanged(module.Kind);
            if (module.Kind is DetailModuleKind.Waveform or DetailModuleKind.Spectrum)
            {
                NotifyRollVisibilityChanged();
            }

            if (module.Kind is DetailModuleKind.Collision)
            {
                RefreshFrequencyCollisionOptions();
                RefreshFrequencyCollisions();
            }
        }

        if (!_isApplyingWorkspacePreferences
            && (args.PropertyName is nameof(DetailModuleViewModel.IsVisible)
                or nameof(DetailModuleViewModel.Height)
                or nameof(DetailModuleViewModel.OrderIndex)))
        {
            SaveWorkspacePreferences();
        }
    }

    private void SessionOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AudioSessionMeterViewModel.IsChannelWaveformVisible)
            or nameof(AudioSessionMeterViewModel.IsChannelSpectrumVisible))
        {
            NotifyRollLayoutChanged();
        }

        if (args.PropertyName is nameof(AudioSessionMeterViewModel.WaveformDisplayMode)
            or nameof(AudioSessionMeterViewModel.SpectrumDisplayMode)
            or nameof(AudioSessionMeterViewModel.MomentaryMeasurementMode)
            or nameof(AudioSessionMeterViewModel.ShortTermMeasurementMode)
            or nameof(AudioSessionMeterViewModel.IntegratedMeasurementMode)
            or nameof(AudioSessionMeterViewModel.TruePeakMeasurementMode)
            or nameof(AudioSessionMeterViewModel.RmsMeasurementMode))
        {
            SaveWorkspacePreferences();
            NotifyRollLayoutChanged();
        }

        if (args.PropertyName is nameof(AudioSessionMeterViewModel.SpectrumPlotTick))
        {
            RefreshFrequencyCollisions();
        }

        if (args.PropertyName is nameof(AudioSessionMeterViewModel.SpectrumBuckets)
            or nameof(AudioSessionMeterViewModel.IsInstanceMissing))
        {
            RefreshFrequencyCollisionOptions();
            RefreshFrequencyCollisions();
        }
    }

    private void CollisionSourceOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AudioSessionMeterViewModel.SpectrumPlotTick))
        {
            RefreshFrequencyCollisions();
        }

        if (args.PropertyName is nameof(AudioSessionMeterViewModel.SpectrumBuckets)
            or nameof(AudioSessionMeterViewModel.IsInstanceMissing))
        {
            RefreshFrequencyCollisionOptions();
            RefreshFrequencyCollisions();
        }
    }

    private void RefreshCollisionSourceSubscriptions()
    {
        var activeComparisons = _getActiveSessions()
            .Where(session => !ReferenceEquals(session, Session) && !session.IsInstanceMissing)
            .ToHashSet();

        foreach (var session in _collisionSourceSubscriptions.Except(activeComparisons).ToArray())
        {
            session.PropertyChanged -= CollisionSourceOnPropertyChanged;
            _collisionSourceSubscriptions.Remove(session);
        }

        foreach (var session in activeComparisons.Except(_collisionSourceSubscriptions))
        {
            session.PropertyChanged += CollisionSourceOnPropertyChanged;
            _collisionSourceSubscriptions.Add(session);
        }
    }

    private void ClearCollisionSourceSubscriptions()
    {
        foreach (var session in _collisionSourceSubscriptions)
        {
            session.PropertyChanged -= CollisionSourceOnPropertyChanged;
        }

        _collisionSourceSubscriptions.Clear();
    }

    private void SwitchSession(object? parameter)
    {
        if (FindLiveSwitchTarget(parameter) is not { } target)
        {
            RefreshSwitchTargets();
            return;
        }

        Session = target;
    }

    private bool CanSwitchSession(object? parameter)
    {
        return FindLiveSwitchTarget(parameter) is not null;
    }

    private void RefreshFrequencyCollisionOptions()
    {
        var options = new List<FrequencyCollisionSourceOption> { AllCollisionSourcesOption };
        if (!Session.IsInstanceMissing && Session.SpectrumBuckets.Count > 0)
        {
            options.AddRange(
                _getActiveSessions()
                    .Where(session =>
                        !ReferenceEquals(session, Session)
                        && !session.IsInstanceMissing
                        && session.SpectrumBuckets.Count > 0)
                    .Select(session => new FrequencyCollisionSourceOption(session.SourceKey, session.DisplayTitle)));
        }

        var optionsChanged = SetFrequencyCollisionOptions(options);
        if (!options.Any(option => string.Equals(option.SourceKey, SelectedFrequencyCollisionSourceKey, StringComparison.Ordinal)))
        {
            SelectedFrequencyCollisionSourceKey = AllCollisionSourcesOption.SourceKey;
        }
        else if (optionsChanged)
        {
            OnPropertyChanged(nameof(SelectedFrequencyCollisionSource));
            OnPropertyChanged(nameof(SelectedFrequencyCollisionSourceDisplayName));
        }

        if (optionsChanged)
        {
            OnPropertyChanged(nameof(HasFrequencyCollisionSources));
            _selectFrequencyCollisionSourceCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshFrequencyCollisions()
    {
        if (!IsCollisionVisible || Session.IsInstanceMissing || Session.SpectrumBuckets.Count == 0)
        {
            SetFrequencyCollisionBands(Array.Empty<FrequencyCollisionBand>());
            return;
        }

        var selectedComparison = SelectedFrequencyCollisionSource;
        SetFrequencyCollisionBands(FrequencyCollisionAnalyzer.Analyze(
            _getActiveSessions(),
            Session.SourceKey,
            selectedComparison.SourceKey,
            selectedComparison.IsAllSources,
            CollisionLevelFloorDb,
            CollisionProminenceThresholdDb,
            CollisionRelativeThreshold,
            CollisionFrequencyScale));
    }

    private DetailLayoutPreset CreateDetailLayoutPreset()
    {
        return new DetailLayoutPreset
        {
            WaveformDisplayMode = Session.WaveformDisplayMode,
            SpectrumDisplayMode = Session.SpectrumDisplayMode,
            StereoVisible = IsStereoVisible,
            WaveformVisible = IsWaveformVisible,
            SpectrumVisible = IsSpectrumVisible,
            LoudnessClockVisible = IsLoudnessClockVisible,
            CollisionVisible = IsCollisionVisible,
            CollisionSourceKey = SelectedFrequencyCollisionSourceKey == AllCollisionSourcesOption.SourceKey
                ? null
                : SelectedFrequencyCollisionSourceKey,
            CollisionLevelFloorDb = CollisionLevelFloorDb,
            CollisionProminenceThresholdDb = CollisionProminenceThresholdDb,
            CollisionRelativeThreshold = CollisionRelativeThreshold,
            CollisionFrequencyScale = CollisionFrequencyScale,
            SpectrumDisplayFloorDb = SpectrumDisplayFloorDb,
            SpectrumDisplayCeilingDb = SpectrumDisplayCeilingDb,
            LoudnessClockMinimumDb = LoudnessClockMinimumDb,
            LoudnessClockMaximumDb = LoudnessClockMaximumDb,
            LoudnessClockWarningDb = LoudnessClockWarningDb,
            LoudnessClockCycleSeconds = LoudnessClockCycleSeconds,
            Stereo = StereoWorkspaceSettings.FromSettings(StereoSettings),
            Modules = DetailModules.OrderBy(module => module.OrderIndex).Select(module => module.ToLayout()).ToList()
        };
    }

    private void ApplyDetailLayoutPreset(DetailLayoutPreset preset, bool persist)
    {
        preset.Normalize();
        _isApplyingWorkspacePreferences = true;
        try
        {
            Session.WaveformDisplayMode = preset.WaveformDisplayMode;
            Session.SpectrumDisplayMode = preset.SpectrumDisplayMode;
            IsStereoVisible = preset.StereoVisible;
            IsWaveformVisible = preset.WaveformVisible;
            IsSpectrumVisible = preset.SpectrumVisible;
            IsLoudnessClockVisible = preset.LoudnessClockVisible;
            IsCollisionVisible = preset.CollisionVisible;
            CollisionLevelFloorDb = preset.CollisionLevelFloorDb;
            CollisionProminenceThresholdDb = preset.CollisionProminenceThresholdDb;
            CollisionRelativeThreshold = preset.CollisionRelativeThreshold;
            CollisionFrequencyScale = preset.CollisionFrequencyScale;
            SpectrumDisplayFloorDb = preset.SpectrumDisplayFloorDb;
            SpectrumDisplayCeilingDb = preset.SpectrumDisplayCeilingDb;
            LoudnessClockMinimumDb = preset.LoudnessClockMinimumDb;
            LoudnessClockMaximumDb = preset.LoudnessClockMaximumDb;
            LoudnessClockWarningDb = preset.LoudnessClockWarningDb;
            LoudnessClockCycleSeconds = preset.LoudnessClockCycleSeconds;
            preset.CloneStereo().ApplyTo(StereoSettings);
            ApplyDetailModuleLayouts(DetailModules, preset.Modules);
            SelectedFrequencyCollisionSource = FindFrequencyCollisionSourceOption(
                preset.CollisionSourceKey ?? AllCollisionSourcesOption.SourceKey);
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }

        if (persist)
        {
            SaveWorkspacePreferences();
        }

        RefreshFrequencyCollisionOptions();
        RefreshFrequencyCollisions();
        NotifyRollVisibilityChanged();
        NotifyRollLayoutChanged();
        NotifyDetailModuleVisibilityChanged();
    }

    private static bool HasPopupDetailLayout(SourceWorkspaceSettings? sourceSettings)
    {
        return sourceSettings is not null
            && (sourceSettings.WaveformDisplayMode is not null
                || sourceSettings.SpectrumDisplayMode is not null
                || sourceSettings.PopupStereoVisible is not null
                || sourceSettings.PopupWaveformVisible is not null
                || sourceSettings.PopupSpectrumVisible is not null
                || sourceSettings.PopupLoudnessClockVisible is not null
                || sourceSettings.PopupCollisionVisible is not null
                || sourceSettings.PopupCollisionSourceKey is not null
                || sourceSettings.CollisionLevelFloorDb is not null
                || sourceSettings.CollisionAbsoluteThresholdDb is not null
                || sourceSettings.CollisionProminenceThresholdDb is not null
                || sourceSettings.CollisionRelativeThreshold is not null
                || sourceSettings.CollisionFrequencyScale is not null
                || sourceSettings.SpectrumDisplayFloorDb is not null
                || sourceSettings.SpectrumDisplayCeilingDb is not null
                || sourceSettings.LoudnessClockMinimumDb is not null
                || sourceSettings.LoudnessClockMaximumDb is not null
                || sourceSettings.LoudnessClockWarningDb is not null
                || sourceSettings.LoudnessClockCycleSeconds is not null
                || sourceSettings.PopupStereo is not null
                || sourceSettings.PopupDetailModules is { Count: > 0 });
    }

    private void ApplyWorkspacePreferences()
    {
        var sourceSettings = _workspaceSettings.GetSource(Session.SourceKey);
        var defaultLayout = !HasPopupDetailLayout(sourceSettings)
            && DetailLayoutPresetStore.TryLoadDefault(_workspaceSettings, out var loadedDefaultLayout, out _)
                ? loadedDefaultLayout
                : null;
        _isApplyingWorkspacePreferences = true;
        try
        {
            Session.WaveformDisplayMode = defaultLayout?.WaveformDisplayMode
                ?? sourceSettings?.WaveformDisplayMode
                ?? Session.WaveformDisplayMode;
            Session.SpectrumDisplayMode = defaultLayout?.SpectrumDisplayMode
                ?? sourceSettings?.SpectrumDisplayMode
                ?? Session.SpectrumDisplayMode;
            Session.MomentaryMeasurementMode = sourceSettings?.MomentaryMeasurementMode ?? SignalMeasurementMode.Overall;
            Session.ShortTermMeasurementMode = sourceSettings?.ShortTermMeasurementMode ?? SignalMeasurementMode.Overall;
            Session.IntegratedMeasurementMode = sourceSettings?.IntegratedMeasurementMode ?? SignalMeasurementMode.Overall;
            Session.TruePeakMeasurementMode = sourceSettings?.TruePeakMeasurementMode ?? SignalMeasurementMode.Overall;
            Session.RmsMeasurementMode = sourceSettings?.RmsMeasurementMode ?? SignalMeasurementMode.Overall;
            IsStereoVisible = defaultLayout?.StereoVisible ?? sourceSettings?.PopupStereoVisible ?? true;
            IsWaveformVisible = defaultLayout?.WaveformVisible ?? sourceSettings?.PopupWaveformVisible ?? false;
            IsSpectrumVisible = defaultLayout?.SpectrumVisible ?? sourceSettings?.PopupSpectrumVisible ?? false;
            IsLoudnessClockVisible = defaultLayout?.LoudnessClockVisible ?? sourceSettings?.PopupLoudnessClockVisible ?? false;
            IsCollisionVisible = defaultLayout?.CollisionVisible ?? sourceSettings?.PopupCollisionVisible ?? false;
            CollisionLevelFloorDb = defaultLayout?.CollisionLevelFloorDb ?? GetCollisionLevelFloorDb(sourceSettings) ?? DefaultCollisionLevelFloorDb;
            CollisionProminenceThresholdDb = defaultLayout?.CollisionProminenceThresholdDb ?? sourceSettings?.CollisionProminenceThresholdDb ?? DefaultCollisionProminenceThresholdDb;
            CollisionRelativeThreshold = defaultLayout?.CollisionRelativeThreshold ?? sourceSettings?.CollisionRelativeThreshold ?? DefaultCollisionRelativeThreshold;
            CollisionFrequencyScale = defaultLayout?.CollisionFrequencyScale ?? sourceSettings?.CollisionFrequencyScale ?? CollisionFrequencyScale.Logarithmic;
            SpectrumDisplayFloorDb = defaultLayout?.SpectrumDisplayFloorDb ?? sourceSettings?.SpectrumDisplayFloorDb ?? SpectrumDisplayDefaultFloorDb;
            SpectrumDisplayCeilingDb = defaultLayout?.SpectrumDisplayCeilingDb ?? sourceSettings?.SpectrumDisplayCeilingDb ?? SpectrumDisplayDefaultCeilingDb;
            LoudnessClockMinimumDb = defaultLayout?.LoudnessClockMinimumDb ?? sourceSettings?.LoudnessClockMinimumDb ?? LoudnessClockDefaultMinimumDb;
            LoudnessClockMaximumDb = defaultLayout?.LoudnessClockMaximumDb ?? sourceSettings?.LoudnessClockMaximumDb ?? LoudnessClockDefaultMaximumDb;
            LoudnessClockWarningDb = defaultLayout?.LoudnessClockWarningDb ?? sourceSettings?.LoudnessClockWarningDb ?? LoudnessClockDefaultWarningDb;
            LoudnessClockCycleSeconds = defaultLayout?.LoudnessClockCycleSeconds ?? sourceSettings?.LoudnessClockCycleSeconds ?? LoudnessClockDefaultCycleSeconds;
            if (defaultLayout is not null)
            {
                defaultLayout.CloneStereo().ApplyTo(StereoSettings);
            }
            else if (sourceSettings?.PopupStereo is not null)
            {
                sourceSettings.PopupStereo.ApplyTo(StereoSettings);
            }
            else
            {
                StereoSettings.Reset();
            }

            ApplyDetailModuleLayouts(DetailModules, defaultLayout?.Modules ?? sourceSettings?.PopupDetailModules);
            SelectedFrequencyCollisionSource = FindFrequencyCollisionSourceOption(
                defaultLayout?.CollisionSourceKey
                ?? sourceSettings?.PopupCollisionSourceKey
                ?? AllCollisionSourcesOption.SourceKey);
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }
    }

    private static double? GetCollisionLevelFloorDb(SourceWorkspaceSettings? sourceSettings)
    {
        if (sourceSettings?.CollisionLevelFloorDb is double levelFloorDb)
        {
            return levelFloorDb;
        }

        return sourceSettings?.CollisionAbsoluteThresholdDb is double legacyLevelFloorDb && legacyLevelFloorDb <= 0
            ? legacyLevelFloorDb
            : null;
    }

    private void SaveWorkspacePreferences()
    {
        if (_isApplyingWorkspacePreferences)
        {
            return;
        }

        var sourceSettings = _workspaceSettings.GetOrCreateSource(Session.SourceKey);
        sourceSettings.WaveformDisplayMode = Session.WaveformDisplayMode;
        sourceSettings.SpectrumDisplayMode = Session.SpectrumDisplayMode;
        sourceSettings.MomentaryMeasurementMode = Session.MomentaryMeasurementMode;
        sourceSettings.ShortTermMeasurementMode = Session.ShortTermMeasurementMode;
        sourceSettings.IntegratedMeasurementMode = Session.IntegratedMeasurementMode;
        sourceSettings.TruePeakMeasurementMode = Session.TruePeakMeasurementMode;
        sourceSettings.RmsMeasurementMode = Session.RmsMeasurementMode;
        sourceSettings.PopupStereoVisible = IsStereoVisible;
        sourceSettings.PopupWaveformVisible = IsWaveformVisible;
        sourceSettings.PopupSpectrumVisible = IsSpectrumVisible;
        sourceSettings.PopupLoudnessClockVisible = IsLoudnessClockVisible;
        sourceSettings.PopupCollisionVisible = IsCollisionVisible;
        sourceSettings.PopupCollisionSourceKey = SelectedFrequencyCollisionSourceKey == AllCollisionSourcesOption.SourceKey
            ? null
            : SelectedFrequencyCollisionSourceKey;
        sourceSettings.CollisionLevelFloorDb = CollisionLevelFloorDb;
        sourceSettings.CollisionProminenceThresholdDb = CollisionProminenceThresholdDb;
        sourceSettings.CollisionRelativeThreshold = CollisionRelativeThreshold;
        sourceSettings.CollisionFrequencyScale = CollisionFrequencyScale;
        sourceSettings.SpectrumDisplayFloorDb = SpectrumDisplayFloorDb;
        sourceSettings.SpectrumDisplayCeilingDb = SpectrumDisplayCeilingDb;
        sourceSettings.LoudnessClockMinimumDb = LoudnessClockMinimumDb;
        sourceSettings.LoudnessClockMaximumDb = LoudnessClockMaximumDb;
        sourceSettings.LoudnessClockWarningDb = LoudnessClockWarningDb;
        sourceSettings.LoudnessClockCycleSeconds = LoudnessClockCycleSeconds;
        sourceSettings.PopupStereo = StereoWorkspaceSettings.FromSettings(StereoSettings);
        sourceSettings.PopupDetailModules = DetailModules.OrderBy(module => module.OrderIndex).Select(module => module.ToLayout()).ToList();
        _workspaceSettings.Save();
    }

    private void GlobalSettingsOwnerOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.EnableDetailHoverHints))
        {
            OnPropertyChanged(nameof(EnableDetailHoverHints));
        }
    }

    private FrequencyCollisionSourceOption FindFrequencyCollisionSourceOption(string sourceKey)
    {
        return FrequencyCollisionSourceOptions.FirstOrDefault(option =>
            string.Equals(option.SourceKey, sourceKey, StringComparison.Ordinal))
            ?? AllCollisionSourcesOption;
    }

    private void SelectFrequencyCollisionSource(object? parameter)
    {
        if (parameter is FrequencyCollisionSourceOption option)
        {
            SelectedFrequencyCollisionSource = option;
        }
    }

    private bool SetFrequencyCollisionOptions(IReadOnlyList<FrequencyCollisionSourceOption> options)
    {
        if (FrequencyCollisionSourceOptions.SequenceEqual(options))
        {
            return false;
        }

        FrequencyCollisionSourceOptions = options;
        return true;
    }

    private void SetFrequencyCollisionBands(IReadOnlyList<FrequencyCollisionBand> bands)
    {
        if (FrequencyCollisionBandsEqual(FrequencyCollisionBands, bands))
        {
            return;
        }

        FrequencyCollisionBands = bands;
    }

    private AudioSessionMeterViewModel? FindLiveSwitchTarget(object? parameter)
    {
        return parameter is AudioSessionMeterViewModel requested
            ? _getActiveSessions().FirstOrDefault(session =>
                ReferenceEquals(session, requested)
                && !ReferenceEquals(session, Session)
                && !session.IsInstanceMissing)
            : null;
    }

    private void NotifyRollVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsWaveformOnlyVisible));
        OnPropertyChanged(nameof(IsSpectrumOnlyVisible));
        OnPropertyChanged(nameof(AreRollsVisible));
        OnPropertyChanged(nameof(WaveformOnlyPanelDisplayMinHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelDisplayMinHeight));
    }

    private void NotifyDetailModuleVisibilityChanged(DetailModuleKind? kind = null)
    {
        if (kind is null or DetailModuleKind.Stereo)
        {
            OnPropertyChanged(nameof(IsStereoVisible));
        }

        if (kind is null or DetailModuleKind.Collision)
        {
            OnPropertyChanged(nameof(IsCollisionVisible));
        }

        if (kind is null or DetailModuleKind.LoudnessClock)
        {
            OnPropertyChanged(nameof(IsLoudnessClockVisible));
        }

        if (kind is null or DetailModuleKind.Waveform)
        {
            OnPropertyChanged(nameof(IsWaveformVisible));
        }

        if (kind is null or DetailModuleKind.Spectrum)
        {
            OnPropertyChanged(nameof(IsSpectrumVisible));
        }
    }

    private void NotifyRollLayoutChanged()
    {
        OnPropertyChanged(nameof(WaveformOnlyPanelMinHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelMinHeight));
        OnPropertyChanged(nameof(WaveformOnlyPanelDisplayMinHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelDisplayMinHeight));
        OnPropertyChanged(nameof(WaveformOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(WaveformRollGroupMinHeight));
        OnPropertyChanged(nameof(SpectrumRollGroupMinHeight));
        OnPropertyChanged(nameof(RollsPanelMinHeight));
        OnPropertyChanged(nameof(RollsPanelDefaultHeight));
    }

    private static void ApplyDetailModuleLayouts(
        IReadOnlyList<DetailModuleViewModel> modules,
        IEnumerable<DetailModuleLayoutSettings>? layouts)
    {
        var layoutByKind = DetailModuleLayoutNormalizer.Normalize(layouts)
            .ToDictionary(layout => layout.Kind);
        foreach (var module in modules)
        {
            if (layoutByKind.TryGetValue(module.Kind, out var layout))
            {
                module.ApplyLayout(layout);
            }
        }
    }

    private static void MoveDetailModule(
        IReadOnlyList<DetailModuleViewModel> modules,
        DetailModuleViewModel source,
        DetailModuleViewModel target,
        bool insertAfter)
    {
        var ordered = modules
            .OrderBy(module => module.OrderIndex)
            .ThenBy(module => ModuleSortRank(module.Kind))
            .ToList();
        ordered.Remove(source);
        var targetIndex = ordered.IndexOf(target);
        if (targetIndex < 0)
        {
            return;
        }

        ordered.Insert(targetIndex + (insertAfter ? 1 : 0), source);
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].OrderIndex = index;
        }
    }

    private static int ModuleSortRank(DetailModuleKind kind)
    {
        return kind switch
        {
            DetailModuleKind.Stereo => 0,
            DetailModuleKind.Collision => 1,
            DetailModuleKind.LoudnessClock => 2,
            DetailModuleKind.Waveform => 3,
            DetailModuleKind.Spectrum => 4,
            _ => int.MaxValue
        };
    }

    private static double RollGroupMinHeight(bool channelsVisible)
    {
        return RollGroupPadding
            + (channelsVisible
                ? (RollTrackMinHeight * 2) + RollChannelLabelAllowance
                : RollTrackMinHeight);
    }

    private static double RoundDisplayDb(double value)
    {
        return double.IsFinite(value)
            ? Math.Round(value, 0, MidpointRounding.AwayFromZero)
            : SpectrumDisplayDefaultFloorDb;
    }

    private static double RoundLoudnessClockDb(double value)
    {
        return double.IsFinite(value)
            ? Math.Round(value, 0, MidpointRounding.AwayFromZero)
            : LoudnessClockDefaultMinimumDb;
    }

    private static double RoundLoudnessClockCycleSeconds(double value)
    {
        return double.IsFinite(value)
            ? Math.Round(value, 0, MidpointRounding.AwayFromZero)
            : LoudnessClockDefaultCycleSeconds;
    }

    private void CoerceLoudnessClockWarningToRange()
    {
        var next = Math.Clamp(LoudnessClockWarningDb, LoudnessClockMinimumDb, LoudnessClockMaximumDb);
        if (SetProperty(ref _loudnessClockWarningDb, next, nameof(LoudnessClockWarningDb)))
        {
            OnPropertyChanged(nameof(LoudnessClockWarningText));
        }
    }

    private static bool FrequencyCollisionBandsEqual(
        IReadOnlyList<FrequencyCollisionBand> current,
        IReadOnlyList<FrequencyCollisionBand> next)
    {
        if (current.Count != next.Count)
        {
            return false;
        }

        for (var index = 0; index < current.Count; index++)
        {
            var first = current[index];
            var second = next[index];
            if (Math.Abs(first.StartRatio - second.StartRatio) >= 0.002
                || Math.Abs(first.EndRatio - second.EndRatio) >= 0.002
                || Math.Abs(first.Intensity - second.Intensity) >= 0.02)
            {
                return false;
            }
        }

        return true;
    }
}
