using System.ComponentModel;
using System.Windows.Input;
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
    private const double SpectrumDisplayDefaultFloorDb = -120;
    private const double SpectrumDisplayMinimumSpanDb = 6;
    private static readonly FrequencyCollisionSourceOption AllCollisionSourcesOption = new("__global__", "Global", isAllSources: true);
    private readonly Func<IReadOnlyList<AudioSessionMeterViewModel>> _getActiveSessions;
    private readonly UserWorkspaceSettings _workspaceSettings;
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
    private double _spectrumDisplayFloorDb = SpectrumDisplayDefaultFloorDb;
    private double _spectrumDisplayCeilingDb = SpectrumDisplayMaximumDb;
    private bool _isAlwaysOnTop = true;
    private bool _isWaveformVisible;
    private bool _isSpectrumVisible;
    private bool _isCollisionVisible;
    private bool _isDisposed;
    private bool _isApplyingWorkspacePreferences;

    public DetailWindowViewModel(
        AudioSessionMeterViewModel session,
        Func<IReadOnlyList<AudioSessionMeterViewModel>>? getActiveSessions = null,
        UserWorkspaceSettings? workspaceSettings = null)
    {
        _session = session;
        _session.PropertyChanged += SessionOnPropertyChanged;
        _getActiveSessions = getActiveSessions ?? (() => Array.Empty<AudioSessionMeterViewModel>());
        _workspaceSettings = workspaceSettings ?? new UserWorkspaceSettings();
        StereoSettings = new StereoImageDisplaySettings();
        StereoSettings.PropertyChanged += StereoSettingsOnPropertyChanged;
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        ResetCommand = new RelayCommand(_ => Session.RequestReset());
        _selectFrequencyCollisionSourceCommand = new RelayCommand(SelectFrequencyCollisionSource);
        _switchSessionCommand = new RelayCommand(SwitchSession, CanSwitchSession);
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

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public bool IsWaveformVisible
    {
        get => _isWaveformVisible;
        set
        {
            if (SetProperty(ref _isWaveformVisible, value))
            {
                NotifyRollVisibilityChanged();
                SaveWorkspacePreferences();
            }
        }
    }

    public bool IsSpectrumVisible
    {
        get => _isSpectrumVisible;
        set
        {
            if (SetProperty(ref _isSpectrumVisible, value))
            {
                NotifyRollVisibilityChanged();
                SaveWorkspacePreferences();
            }
        }
    }

    public bool IsCollisionVisible
    {
        get => _isCollisionVisible;
        set
        {
            if (SetProperty(ref _isCollisionVisible, value))
            {
                SaveWorkspacePreferences();
                RefreshFrequencyCollisionOptions();
                RefreshFrequencyCollisions();
            }
        }
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

    public string SpectrumDisplayRangeText => $"{SpectrumDisplayFloorDb:0} to {SpectrumDisplayCeilingDb:0} dB";

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

    public bool IsWaveformOnlyVisible => IsWaveformVisible && !IsSpectrumVisible;

    public bool IsSpectrumOnlyVisible => IsSpectrumVisible && !IsWaveformVisible;

    public bool AreRollsVisible => IsWaveformVisible && IsSpectrumVisible;

    public double WaveformOnlyPanelMinHeight => RollGroupMinHeight(Session.IsChannelWaveformVisible);

    public double SpectrumOnlyPanelMinHeight => RollGroupMinHeight(Session.IsChannelSpectrumVisible);

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

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _session.PropertyChanged -= SessionOnPropertyChanged;
        StereoSettings.PropertyChanged -= StereoSettingsOnPropertyChanged;
        ClearCollisionSourceSubscriptions();
    }

    private void StereoSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        SaveWorkspacePreferences();
    }

    private void SessionOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AudioSessionMeterViewModel.IsChannelWaveformVisible)
            or nameof(AudioSessionMeterViewModel.IsChannelSpectrumVisible))
        {
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
            CollisionRelativeThreshold));
    }

    private void ApplyWorkspacePreferences()
    {
        var sourceSettings = _workspaceSettings.GetSource(Session.SourceKey);
        _isApplyingWorkspacePreferences = true;
        try
        {
            IsWaveformVisible = sourceSettings?.PopupWaveformVisible ?? false;
            IsSpectrumVisible = sourceSettings?.PopupSpectrumVisible ?? false;
            IsCollisionVisible = sourceSettings?.PopupCollisionVisible ?? false;
            CollisionLevelFloorDb = GetCollisionLevelFloorDb(sourceSettings) ?? DefaultCollisionLevelFloorDb;
            CollisionProminenceThresholdDb = sourceSettings?.CollisionProminenceThresholdDb ?? DefaultCollisionProminenceThresholdDb;
            CollisionRelativeThreshold = sourceSettings?.CollisionRelativeThreshold ?? DefaultCollisionRelativeThreshold;
            SpectrumDisplayFloorDb = sourceSettings?.SpectrumDisplayFloorDb ?? SpectrumDisplayDefaultFloorDb;
            SpectrumDisplayCeilingDb = sourceSettings?.SpectrumDisplayCeilingDb ?? SpectrumDisplayMaximumDb;
            if (sourceSettings?.PopupStereo is not null)
            {
                sourceSettings.PopupStereo.ApplyTo(StereoSettings);
            }
            else
            {
                StereoSettings.Reset();
            }

            SelectedFrequencyCollisionSourceKey = sourceSettings?.PopupCollisionSourceKey ?? AllCollisionSourcesOption.SourceKey;
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
        sourceSettings.PopupWaveformVisible = IsWaveformVisible;
        sourceSettings.PopupSpectrumVisible = IsSpectrumVisible;
        sourceSettings.PopupCollisionVisible = IsCollisionVisible;
        sourceSettings.PopupCollisionSourceKey = SelectedFrequencyCollisionSourceKey == AllCollisionSourcesOption.SourceKey
            ? null
            : SelectedFrequencyCollisionSourceKey;
        sourceSettings.CollisionLevelFloorDb = CollisionLevelFloorDb;
        sourceSettings.CollisionProminenceThresholdDb = CollisionProminenceThresholdDb;
        sourceSettings.CollisionRelativeThreshold = CollisionRelativeThreshold;
        sourceSettings.SpectrumDisplayFloorDb = SpectrumDisplayFloorDb;
        sourceSettings.SpectrumDisplayCeilingDb = SpectrumDisplayCeilingDb;
        sourceSettings.PopupStereo = StereoWorkspaceSettings.FromSettings(StereoSettings);
        _workspaceSettings.Save();
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
    }

    private void NotifyRollLayoutChanged()
    {
        OnPropertyChanged(nameof(WaveformOnlyPanelMinHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelMinHeight));
        OnPropertyChanged(nameof(WaveformOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(SpectrumOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(WaveformRollGroupMinHeight));
        OnPropertyChanged(nameof(SpectrumRollGroupMinHeight));
        OnPropertyChanged(nameof(RollsPanelMinHeight));
        OnPropertyChanged(nameof(RollsPanelDefaultHeight));
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
