using System.Collections.ObjectModel;
using DesktopLoudnessMeter.App.Audio;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.Themes;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const int DefaultMissingRowRetentionTicks = 150;
    private const int DefaultMaxRecentEventEntries = 6;
    private const int MinimumMaxRecentEventEntries = 1;
    private const int MaximumMaxRecentEventEntries = 200;
    private const double CollisionEventMinimumBandWidthRatio = 0.02;
    private const double CollisionEventMinimumIntensity = 0.08;
    private const double RollTrackMinHeight = 185;
    private const double RollGroupPadding = 20;
    private const double RollChannelLabelAllowance = 40;
    private const double RollSplitterHeight = 8;
    private const double RollSplitterFlexAllowance = 128;
    private const double TruePeakRiskThresholdDb = 0;
    private const double LoudnessJumpThresholdLufs = 8;
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
    private static readonly TimeSpan NormalRefreshInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan EcoRefreshInterval = TimeSpan.FromMilliseconds(100);
    private readonly ISignalSourceMeterService _sourceMeterService;
    private readonly DispatcherTimer? _refreshTimer;
    private readonly Dictionary<string, int> _missingTicksByKey = [];
    private readonly Dictionary<string, SourceRowPreferences> _preferencesBySourceKey = [];
    private readonly UserWorkspaceSettings _workspaceSettings;
    private readonly int _missingRowRetentionTicks;
    private readonly Func<int, bool> _isProcessAlive;
    private readonly Func<DateTimeOffset> _now;
    private readonly Action<AppTheme>? _themeSelected;
    private readonly string? _sourceWarningText;
    private readonly RelayCommand _selectFrequencyCollisionSourceCommand;
    private readonly RelayCommand _forgetSelectedSourceCommand;
    private readonly RelayCommand _selectEventOutputFolderCommand;
    private readonly Func<string?, string?> _selectFolder;
    private readonly Dictionary<string, bool> _truePeakRiskBySourceKey = [];
    private readonly Dictionary<string, double> _lastMomentaryLufsBySourceKey = [];
    private string? _activeCollisionEventSignature;
    private bool _isApplyingWorkspacePreferences;
    private RecentEventFilterOption _selectedRecentEventFilter;
    private int _maxRecentEventEntries = DefaultMaxRecentEventEntries;
    private string? _eventOutputFolder;
    private string _statusText = "Waiting for audio sources";
    private MeterDisplayMode _selectedDisplayMode = MeterDisplayMode.Peak;
    private bool _truePeakEnabled;
    private bool _isEcoModeEnabled;
    private AppTheme _selectedTheme = AppThemes.Default;
    private TruePeakOversampling _selectedTruePeakOversampling = TruePeakOversampling.FourX;
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
    private IReadOnlyList<FrequencyCollisionSourceOption> _frequencyCollisionSourceOptions = [AllCollisionSourcesOption];
    private IReadOnlyList<FrequencyCollisionBand> _frequencyCollisionBands = Array.Empty<FrequencyCollisionBand>();
    private bool _isAlwaysOnTop = true;
    private bool _isInlineWaveformVisible;
    private bool _isInlineSpectrumVisible;
    private bool _isInlineLoudnessClockVisible;
    private bool _isInlineCollisionVisible;
    private bool _showHiddenSources;
    private AudioSessionMeterViewModel? _selectedDetailSession;
    private readonly RelayCommand _switchDetailSessionCommand;
    private readonly RelayCommand _moveSourceUpCommand;
    private readonly RelayCommand _moveSourceDownCommand;

    public MainViewModel(
        ISignalSourceMeterService sourceMeterService,
        TimeSpan? refreshInterval = null,
        bool startTimer = true,
        int missingRowRetentionTicks = DefaultMissingRowRetentionTicks,
        Func<int, bool>? isProcessAlive = null,
        string? sourceWarningText = null,
        AppTheme? selectedTheme = null,
        Action<AppTheme>? themeSelected = null,
        UserWorkspaceSettings? workspaceSettings = null,
        Func<DateTimeOffset>? now = null,
        Func<string?, string?>? selectFolder = null)
    {
        _sourceMeterService = sourceMeterService;
        _themeSelected = themeSelected;
        _workspaceSettings = workspaceSettings ?? new UserWorkspaceSettings();
        _selectedTheme = selectedTheme ?? AppThemes.Default;
        _sourceWarningText = string.IsNullOrWhiteSpace(sourceWarningText) ? null : sourceWarningText.Trim();
        _missingRowRetentionTicks = Math.Max(0, missingRowRetentionTicks);
        _isProcessAlive = isProcessAlive ?? IsProcessAlive;
        _now = now ?? (() => DateTimeOffset.Now);
        _selectFolder = selectFolder ?? (_ => null);
        _maxRecentEventEntries = ClampMaxRecentEventEntries(_workspaceSettings.MaxRecentEventEntries ?? DefaultMaxRecentEventEntries);
        _eventOutputFolder = NormalizeOptionalText(_workspaceSettings.EventOutputFolder);
        _workspaceSettings.MaxRecentEventEntries = _maxRecentEventEntries;
        _workspaceSettings.EventOutputFolder = _eventOutputFolder;
        RefreshInterval = refreshInterval ?? NormalRefreshInterval;
        RefreshLabel = FormatRefreshLabel(RefreshInterval);
        _selectedRecentEventFilter = RecentEventFilterOptions[0];

        if (startTimer)
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = RefreshInterval
            };
            _refreshTimer.Tick += (_, _) => RefreshNow();
            _refreshTimer.Start();
        }

        ResetAllCommand = new RelayCommand(_ => ResetAllSessions());
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        BackToGlobalCommand = new RelayCommand(_ => SelectedDetailSession = null);
        SelectThemeCommand = new RelayCommand(SelectTheme);
        _selectFrequencyCollisionSourceCommand = new RelayCommand(SelectFrequencyCollisionSource);
        _switchDetailSessionCommand = new RelayCommand(SwitchDetailSession, CanSwitchDetailSession);
        _moveSourceUpCommand = new RelayCommand(MoveSourceUp, CanMoveSourceUp);
        _moveSourceDownCommand = new RelayCommand(MoveSourceDown, CanMoveSourceDown);
        _forgetSelectedSourceCommand = new RelayCommand(_ => ForgetSelectedSource(), _ => SelectedDetailSession is not null);
        _selectEventOutputFolderCommand = new RelayCommand(_ => SelectEventOutputFolder());
        InlineStereoSettings.PropertyChanged += InlineStereoSettingsOnPropertyChanged;
    }

    public ObservableCollection<AudioSessionMeterViewModel> Sessions { get; } = [];

    public ObservableCollection<RecentEventViewModel> RecentEvents { get; } = [];

    public ObservableCollection<RecentEventViewModel> VisibleRecentEvents { get; } = [];

    public IReadOnlyList<RecentEventFilterOption> RecentEventFilterOptions { get; } =
    [
        new(null, "All"),
        new(RecentEventKind.Source, "Sources"),
        new(RecentEventKind.TruePeak, "Peaks"),
        new(RecentEventKind.Loudness, "Loudness"),
        new(RecentEventKind.Collision, "Overlap")
    ];

    public RecentEventFilterOption SelectedRecentEventFilter
    {
        get => _selectedRecentEventFilter;
        set
        {
            if (value is null)
            {
                return;
            }

            var next = RecentEventFilterOptions.FirstOrDefault(option => Equals(option.Kind, value.Kind))
                ?? RecentEventFilterOptions[0];
            if (SetProperty(ref _selectedRecentEventFilter, next))
            {
                RefreshVisibleRecentEvents();
            }
        }
    }

    public bool HasRecentEvents => RecentEvents.Count > 0;

    public bool HasVisibleRecentEvents => VisibleRecentEvents.Count > 0;

    public int MaxRecentEventEntries
    {
        get => _maxRecentEventEntries;
        set
        {
            var next = ClampMaxRecentEventEntries(value);
            if (SetProperty(ref _maxRecentEventEntries, next))
            {
                _workspaceSettings.MaxRecentEventEntries = next;
                _workspaceSettings.Save();
                TrimRecentEvents();
                RefreshVisibleRecentEvents();
                OnPropertyChanged(nameof(MaxRecentEventEntriesText));
            }
        }
    }

    public string MaxRecentEventEntriesText
    {
        get => MaxRecentEventEntries.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
            {
                MaxRecentEventEntries = parsed;
                OnPropertyChanged(nameof(MaxRecentEventEntriesText));
            }
        }
    }

    public string? EventOutputFolder
    {
        get => _eventOutputFolder;
        set
        {
            var next = NormalizeOptionalText(value);
            if (SetProperty(ref _eventOutputFolder, next))
            {
                _workspaceSettings.EventOutputFolder = next;
                _workspaceSettings.Save();
                OnPropertyChanged(nameof(EventOutputFolderDisplayText));
            }
        }
    }

    public string EventOutputFolderDisplayText => string.IsNullOrWhiteSpace(EventOutputFolder)
        ? "Not selected"
        : EventOutputFolder;

    public IReadOnlyList<MeterDisplayMode> DisplayModeOptions { get; } =
    [
        MeterDisplayMode.Peak,
        MeterDisplayMode.Lufs
    ];

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

    public IReadOnlyList<CollisionFrequencyScale> CollisionFrequencyScaleOptions { get; } =
    [
        CollisionFrequencyScale.Logarithmic,
        CollisionFrequencyScale.Mel
    ];

    public IReadOnlyList<AppTheme> ThemeOptions => AppThemes.All;

    public IReadOnlyList<FrequencyCollisionSourceOption> FrequencyCollisionSourceOptions
    {
        get => _frequencyCollisionSourceOptions;
        private set => SetProperty(ref _frequencyCollisionSourceOptions, value);
    }

    public TimeSpan RefreshInterval { get; }

    public string RefreshLabel { get; private set; }

    public MeterDisplayMode SelectedDisplayMode
    {
        get => _selectedDisplayMode;
        set
        {
            if (SetProperty(ref _selectedDisplayMode, value))
            {
                foreach (var session in Sessions)
                {
                    session.ApplyDisplayMode(value);
                }
            }
        }
    }

    public bool TruePeakEnabled
    {
        get => _truePeakEnabled;
        set => SetProperty(ref _truePeakEnabled, value);
    }

    public bool IsEcoModeEnabled
    {
        get => _isEcoModeEnabled;
        set
        {
            if (SetProperty(ref _isEcoModeEnabled, value))
            {
                ApplyPerformanceMode(value ? PcmAnalysisPerformanceMode.Eco : PcmAnalysisPerformanceMode.Normal);
            }
        }
    }

    public AppTheme SelectedTheme
    {
        get => _selectedTheme;
        private set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                OnPropertyChanged(nameof(SelectedThemeName));
            }
        }
    }

    public string SelectedThemeName => SelectedTheme.DisplayName;

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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
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
                ClearCollisionEventState();
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
            }
        }
    }

    public string SpectrumDisplayRangeText => $"{SpectrumDisplayFloorDb:0} to {SpectrumDisplayCeilingDb:0} dB";

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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
            }
        }
    }

    public string LoudnessClockRangeText => $"{LoudnessClockMinimumDb:0} to {LoudnessClockMaximumDb:0} LUFS";

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
                SaveSelectedDetailPreferences();
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
                SaveSelectedDetailPreferences();
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
        SelectedDetailSession is { IsInstanceMissing: false } selected
        && selected.SpectrumBuckets.Count > 0
        && Sessions.Any(session =>
            !ReferenceEquals(session, selected)
            && !session.IsInstanceMissing
            && session.SpectrumBuckets.Count > 0);

    public TruePeakOversampling SelectedTruePeakOversampling
    {
        get => _selectedTruePeakOversampling;
        set
        {
            if (SetProperty(ref _selectedTruePeakOversampling, value))
            {
                foreach (var session in Sessions)
                {
                    session.TruePeakOversampling = value;
                }
            }
        }
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public bool IsInlineWaveformVisible
    {
        get => _isInlineWaveformVisible;
        set
        {
            if (SetProperty(ref _isInlineWaveformVisible, value))
            {
                NotifyInlineRollVisibilityChanged();
                SaveSelectedDetailPreferences();
            }
        }
    }

    public bool IsInlineSpectrumVisible
    {
        get => _isInlineSpectrumVisible;
        set
        {
            if (SetProperty(ref _isInlineSpectrumVisible, value))
            {
                NotifyInlineRollVisibilityChanged();
                SaveSelectedDetailPreferences();
            }
        }
    }

    public bool IsInlineLoudnessClockVisible
    {
        get => _isInlineLoudnessClockVisible;
        set
        {
            if (SetProperty(ref _isInlineLoudnessClockVisible, value))
            {
                SaveSelectedDetailPreferences();
            }
        }
    }

    public bool IsInlineCollisionVisible
    {
        get => _isInlineCollisionVisible;
        set
        {
            if (SetProperty(ref _isInlineCollisionVisible, value))
            {
                SaveSelectedDetailPreferences();
                RefreshFrequencyCollisionOptions();
                RefreshFrequencyCollisions();
            }
        }
    }

    public bool IsInlineWaveformOnlyVisible => IsInlineWaveformVisible && !IsInlineSpectrumVisible;

    public bool IsInlineSpectrumOnlyVisible => IsInlineSpectrumVisible && !IsInlineWaveformVisible;

    public bool AreInlineRollsVisible => IsInlineWaveformVisible && IsInlineSpectrumVisible;

    public bool ShowHiddenSources
    {
        get => _showHiddenSources;
        set
        {
            if (SetProperty(ref _showHiddenSources, value))
            {
                RefreshNow();
                OnPropertyChanged(nameof(ShowHiddenSourcesText));
            }
        }
    }

    public string ShowHiddenSourcesText => ShowHiddenSources ? "Showing hidden" : "Hidden filtered";

    public bool HasHiddenSources => _preferencesBySourceKey.Values.Any(preferences => preferences.IsHidden);

    public double InlineWaveformOnlyPanelMinHeight => RollGroupMinHeight(SelectedDetailSession?.IsChannelWaveformVisible == true);

    public double InlineSpectrumOnlyPanelMinHeight => RollGroupMinHeight(SelectedDetailSession?.IsChannelSpectrumVisible == true);

    public double InlineWaveformOnlyPanelDefaultHeight => InlineWaveformOnlyPanelMinHeight;

    public double InlineSpectrumOnlyPanelDefaultHeight => InlineSpectrumOnlyPanelMinHeight;

    public double InlineWaveformRollGroupMinHeight => RollGroupMinHeight(SelectedDetailSession?.IsChannelWaveformVisible == true);

    public double InlineSpectrumRollGroupMinHeight => RollGroupMinHeight(SelectedDetailSession?.IsChannelSpectrumVisible == true);

    public double InlineRollsPanelMinHeight => InlineWaveformRollGroupMinHeight + RollSplitterHeight + InlineSpectrumRollGroupMinHeight;

    public double InlineRollsPanelDefaultHeight => InlineRollsPanelMinHeight + RollSplitterFlexAllowance;

    public ICommand ResetAllCommand { get; }

    public ICommand ToggleAlwaysOnTopCommand { get; }

    public ICommand BackToGlobalCommand { get; }

    public ICommand SelectThemeCommand { get; }

    public ICommand SelectFrequencyCollisionSourceCommand => _selectFrequencyCollisionSourceCommand;

    public ICommand SelectEventOutputFolderCommand => _selectEventOutputFolderCommand;

    public ICommand SwitchDetailSessionCommand => _switchDetailSessionCommand;

    public ICommand ForgetSelectedSourceCommand => _forgetSelectedSourceCommand;

    public ICommand MoveSourceUpCommand => _moveSourceUpCommand;

    public ICommand MoveSourceDownCommand => _moveSourceDownCommand;

    public IReadOnlyList<AudioSessionMeterViewModel> DetailSwitchTargets =>
        Sessions
            .Where(IsDetailSwitchTarget)
            .ToArray();

    public bool HasDetailSwitchTargets => Sessions.Any(IsDetailSwitchTarget);

    public void MoveSourceTo(AudioSessionMeterViewModel source, AudioSessionMeterViewModel target)
    {
        MoveSourceTo(source, target, insertAfter: false);
    }

    public void MoveSourceTo(AudioSessionMeterViewModel source, AudioSessionMeterViewModel target, bool insertAfter)
    {
        if (ReferenceEquals(source, target))
        {
            return;
        }

        var currentIndex = Sessions.IndexOf(source);
        var targetIndex = Sessions.IndexOf(target);
        if (currentIndex < 0 || targetIndex < 0)
        {
            return;
        }

        var insertionIndex = targetIndex + (insertAfter ? 1 : 0);
        MoveSourceToIndex(source, insertionIndex);
    }

    public void MoveSourceToIndex(AudioSessionMeterViewModel source, int insertionIndex)
    {
        var currentIndex = Sessions.IndexOf(source);
        if (currentIndex < 0)
        {
            return;
        }

        insertionIndex = Math.Clamp(insertionIndex, 0, Sessions.Count);
        if (currentIndex < insertionIndex)
        {
            insertionIndex--;
        }

        if (insertionIndex == currentIndex)
        {
            return;
        }

        Sessions.Move(currentIndex, insertionIndex);
        PersistCurrentSourceOrder();
        RaiseMoveCommandStateChanged();
    }

    public AudioSessionMeterViewModel? SelectedDetailSession
    {
        get => _selectedDetailSession;
        private set
        {
            if (ReferenceEquals(_selectedDetailSession, value))
            {
                return;
            }

            if (_selectedDetailSession is not null)
            {
                _selectedDetailSession.PropertyChanged -= SelectedDetailSessionOnPropertyChanged;
            }

            _selectedDetailSession = value;
            if (_selectedDetailSession is not null)
            {
                _selectedDetailSession.PropertyChanged += SelectedDetailSessionOnPropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDetailViewVisible));
            ApplySelectedDetailPreferences();
            NotifyInlineRollLayoutChanged();
            NotifyDetailSwitchTargetsChanged();
            _forgetSelectedSourceCommand.RaiseCanExecuteChanged();
            RefreshFrequencyCollisionOptions();
            RefreshFrequencyCollisions();
        }
    }

    public bool IsDetailViewVisible => SelectedDetailSession is not null;

    public StereoImageDisplaySettings InlineStereoSettings { get; } = new();

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public void RefreshNow()
    {
        try
        {
            var snapshots = _sourceMeterService.GetCurrentSources();
            Reconcile(snapshots);
            RefreshFrequencyCollisionOptions();
            RefreshFrequencyCollisions();
            StatusText = _sourceWarningText ?? (Sessions.Count switch
            {
                0 => "No active audio sources",
                1 => "1 source tracked",
                _ => $"{Sessions.Count} sources tracked"
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Refresh failed: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _refreshTimer?.Stop();
        if (_selectedDetailSession is not null)
        {
            _selectedDetailSession.PropertyChanged -= SelectedDetailSessionOnPropertyChanged;
        }

        if (_sourceMeterService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        InlineStereoSettings.PropertyChanged -= InlineStereoSettingsOnPropertyChanged;
    }

    private void Reconcile(IReadOnlyList<SignalSourceSnapshot> snapshots)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var existingByKey = Sessions.ToDictionary(session => session.SourceKey, StringComparer.Ordinal);
        var matchedRows = new HashSet<AudioSessionMeterViewModel>();
        var activeRowsChanged = false;

        foreach (var snapshot in snapshots)
        {
            if (existingByKey.TryGetValue(snapshot.SourceKey, out var existing)
                || TryFindRecoverableRow(snapshot, seenKeys, out existing))
            {
                var previousKey = existing.SourceKey;
                existing.Update(snapshot);
                if (!string.Equals(previousKey, existing.SourceKey, StringComparison.Ordinal))
                {
                    ClearRecentEventState(previousKey);
                }

                var preferences = GetOrCreatePreferences(existing.SourceKey);
                existing.ApplyWorkspaceState(preferences.IsHidden, preferences.OrderIndex);
                var shouldTrack = ShouldDisplaySource(preferences);
                var wasTracked = Sessions.Contains(existing);
                if (shouldTrack && !wasTracked)
                {
                    Sessions.Add(existing);
                    activeRowsChanged = true;
                }
                else if (!shouldTrack && wasTracked)
                {
                    Sessions.Remove(existing);
                    activeRowsChanged = true;
                }

                _missingTicksByKey.Remove(previousKey);
                ApplyTruePeakOversampling(existing, existing.TruePeakOversampling, persist: false);
                ApplyWaveformDisplayMode(existing, existing.WaveformDisplayMode, persist: false);
                ApplySpectrumDisplayMode(existing, existing.SpectrumDisplayMode, persist: false);
                matchedRows.Add(existing);
                if (shouldTrack && (!wasTracked || !string.Equals(previousKey, existing.SourceKey, StringComparison.Ordinal)))
                {
                    AddRecentEvent(
                        RecentEventKind.Source,
                        "Source appeared",
                        existing.DisplayTitle,
                        ReadoutSeverity.Normal);
                }

                if (shouldTrack)
                {
                    TrackMeasurementEvents(existing, snapshot, isNewSource: !wasTracked);
                }
            }
            else
            {
                var created = new AudioSessionMeterViewModel(snapshot);
                created.ApplyDisplayMode(SelectedDisplayMode);
                AttachRowHandlers(created);
                var preferences = GetOrCreatePreferences(created.SourceKey);
                _isApplyingWorkspacePreferences = true;
                try
                {
                    created.Alias = preferences.Alias;
                    created.ApplyWorkspaceState(preferences.IsHidden, preferences.OrderIndex);
                    var targetOversampling = preferences.TruePeakOversampling ?? SelectedTruePeakOversampling;
                    if (created.TruePeakOversampling == targetOversampling)
                    {
                        ApplyTruePeakOversampling(created, created.TruePeakOversampling, persist: false);
                    }
                    else
                    {
                        created.TruePeakOversampling = targetOversampling;
                    }

                    created.WaveformDisplayMode = preferences.WaveformDisplayMode;
                    created.SpectrumDisplayMode = preferences.SpectrumDisplayMode;
                }
                finally
                {
                    _isApplyingWorkspacePreferences = false;
                }

                ApplyWaveformDisplayMode(created, created.WaveformDisplayMode, persist: false);
                ApplySpectrumDisplayMode(created, created.SpectrumDisplayMode, persist: false);
                matchedRows.Add(created);
                var shouldDisplay = ShouldDisplaySource(preferences);
                if (shouldDisplay)
                {
                    Sessions.Add(created);
                    activeRowsChanged = true;
                    AddRecentEvent(
                        RecentEventKind.Source,
                        "Source appeared",
                        created.DisplayTitle,
                        ReadoutSeverity.Normal);
                }

                if (shouldDisplay)
                {
                    TrackMeasurementEvents(created, snapshot, isNewSource: true);
                }
            }

            seenKeys.Add(snapshot.SourceKey);
            _missingTicksByKey.Remove(snapshot.SourceKey);
        }

        for (var index = Sessions.Count - 1; index >= 0; index--)
        {
            var session = Sessions[index];
            if (matchedRows.Contains(session))
            {
                continue;
            }

            var missingTicks = _missingTicksByKey.GetValueOrDefault(session.SourceKey) + 1;
            _missingTicksByKey[session.SourceKey] = missingTicks;
            if (ShouldRetainMissingRow(session, missingTicks))
            {
                continue;
            }

            session.MarkInstanceNotFound();

            _missingTicksByKey.Remove(session.SourceKey);
            ClearRecentEventState(session.SourceKey);
            AddRecentEvent(
                RecentEventKind.Source,
                "Source disappeared",
                session.DisplayTitle,
                ReadoutSeverity.Muted);
            DetachRowHandlers(session);
            Sessions.RemoveAt(index);
            activeRowsChanged = true;
        }

        SortSessions();
        RefreshDisplayNameCollisions();
        OnPropertyChanged(nameof(HasHiddenSources));
        if (activeRowsChanged)
        {
            NotifyDetailSwitchTargetsChanged();
        }
    }

    private void RefreshFrequencyCollisionOptions()
    {
        var options = new List<FrequencyCollisionSourceOption> { AllCollisionSourcesOption };
        if (SelectedDetailSession is { IsInstanceMissing: false } selected && selected.SpectrumBuckets.Count > 0)
        {
            options.AddRange(
                Sessions
                    .Where(session =>
                        !ReferenceEquals(session, selected)
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
        if (!IsInlineCollisionVisible
            || SelectedDetailSession is not { IsInstanceMissing: false } selected
            || selected.SpectrumBuckets.Count == 0)
        {
            SetFrequencyCollisionBands(Array.Empty<FrequencyCollisionBand>());
            ClearCollisionEventState();
            return;
        }

        var selectedComparison = SelectedFrequencyCollisionSource;
        var bands = FrequencyCollisionAnalyzer.Analyze(
            Sessions,
            selected.SourceKey,
            selectedComparison.SourceKey,
            selectedComparison.IsAllSources,
            CollisionLevelFloorDb,
            CollisionProminenceThresholdDb,
            CollisionRelativeThreshold,
            CollisionFrequencyScale);
        SetFrequencyCollisionBands(bands);
        TrackCollisionEvent(selected, selectedComparison, bands);
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

    private void SelectEventOutputFolder()
    {
        var selectedFolder = _selectFolder(EventOutputFolder);
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            EventOutputFolder = selectedFolder;
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

    private void TrackMeasurementEvents(
        AudioSessionMeterViewModel session,
        SignalSourceSnapshot snapshot,
        bool isNewSource)
    {
        TrackTruePeakMaxEvent(session, snapshot.TruePeakDb);
        TrackLoudnessJumpEvent(session, snapshot.MomentaryLufs, isNewSource);
    }

    private void TrackTruePeakMaxEvent(AudioSessionMeterViewModel session, double? truePeakDb)
    {
        var sourceKey = session.SourceKey;
        if (!TryGetFinite(truePeakDb, out var currentTruePeakDb))
        {
            _truePeakRiskBySourceKey[sourceKey] = false;
            return;
        }

        var isRisk = currentTruePeakDb >= TruePeakRiskThresholdDb;
        var wasRisk = _truePeakRiskBySourceKey.GetValueOrDefault(sourceKey);
        if (isRisk && !wasRisk)
        {
            AddRecentEvent(
                RecentEventKind.TruePeak,
                "True peak max",
                $"{session.DisplayTitle} reached {FormatSignedDbTp(currentTruePeakDb)}",
                ReadoutSeverity.Over);
        }

        _truePeakRiskBySourceKey[sourceKey] = isRisk;
    }

    private void TrackLoudnessJumpEvent(
        AudioSessionMeterViewModel session,
        double? momentaryLufs,
        bool isNewSource)
    {
        var sourceKey = session.SourceKey;
        if (!TryGetFinite(momentaryLufs, out var currentMomentaryLufs))
        {
            _lastMomentaryLufsBySourceKey.Remove(sourceKey);
            return;
        }

        if (!isNewSource
            && _lastMomentaryLufsBySourceKey.TryGetValue(sourceKey, out var previousLufs))
        {
            var delta = currentMomentaryLufs - previousLufs;
            if (Math.Abs(delta) >= LoudnessJumpThresholdLufs)
            {
                var direction = delta >= 0 ? "rose" : "fell";
                AddRecentEvent(
                    RecentEventKind.Loudness,
                    "Loudness jump",
                    $"{session.DisplayTitle} {direction} {Math.Abs(delta):0.#} LUFS to {currentMomentaryLufs:0.#} LUFS",
                    delta >= 0 ? ReadoutSeverity.Warm : ReadoutSeverity.Normal);
            }
        }

        _lastMomentaryLufsBySourceKey[sourceKey] = currentMomentaryLufs;
    }

    private void AddRecentEvent(RecentEventKind kind, string title, string detail, ReadoutSeverity severity)
    {
        var hadEvents = HasRecentEvents;
        RecentEvents.Insert(
            0,
            new RecentEventViewModel(
                _now().ToString("HH:mm:ss"),
                kind,
                title,
                detail,
                severity));

        TrimRecentEvents();

        RefreshVisibleRecentEvents();
        if (hadEvents != HasRecentEvents)
        {
            OnPropertyChanged(nameof(HasRecentEvents));
        }
    }

    private void RefreshVisibleRecentEvents()
    {
        var hadVisibleEvents = HasVisibleRecentEvents;
        var selectedKind = SelectedRecentEventFilter.Kind;
        var visibleEvents = selectedKind is null
            ? RecentEvents
            : RecentEvents.Where(item => item.Kind == selectedKind.Value);

        VisibleRecentEvents.Clear();
        foreach (var item in visibleEvents)
        {
            VisibleRecentEvents.Add(item);
        }

        if (hadVisibleEvents != HasVisibleRecentEvents)
        {
            OnPropertyChanged(nameof(HasVisibleRecentEvents));
        }
    }

    private void ClearRecentEventState(string sourceKey)
    {
        _truePeakRiskBySourceKey.Remove(sourceKey);
        _lastMomentaryLufsBySourceKey.Remove(sourceKey);
        if (_activeCollisionEventSignature?.Contains(sourceKey, StringComparison.Ordinal) == true)
        {
            ClearCollisionEventState();
        }
    }

    private void TrackCollisionEvent(
        AudioSessionMeterViewModel selected,
        FrequencyCollisionSourceOption selectedComparison,
        IReadOnlyList<FrequencyCollisionBand> bands)
    {
        var strongestBand = bands
            .Where(band => band.EndRatio - band.StartRatio >= CollisionEventMinimumBandWidthRatio)
            .OrderByDescending(band => band.Intensity)
            .FirstOrDefault(band => band.Intensity >= CollisionEventMinimumIntensity);
        if (strongestBand is null)
        {
            ClearCollisionEventState();
            return;
        }

        var signature = BuildCollisionEventSignature(selected.SourceKey, selectedComparison, strongestBand);
        if (string.Equals(signature, _activeCollisionEventSignature, StringComparison.Ordinal))
        {
            return;
        }

        _activeCollisionEventSignature = signature;
        AddRecentEvent(
            RecentEventKind.Collision,
            "Possible overlap",
            $"{selected.DisplayTitle} vs {selectedComparison.DisplayName}: {FormatCollisionRange(strongestBand, CollisionFrequencyScale)}",
            ReadoutSeverity.Warm);
    }

    private void ClearCollisionEventState()
    {
        _activeCollisionEventSignature = null;
    }

    private static bool TryGetFinite(double? value, out double finiteValue)
    {
        if (value is double current && !double.IsNaN(current) && !double.IsInfinity(current))
        {
            finiteValue = current;
            return true;
        }

        finiteValue = default;
        return false;
    }

    private void TrimRecentEvents()
    {
        var hadEvents = HasRecentEvents;
        while (RecentEvents.Count > MaxRecentEventEntries)
        {
            RecentEvents.RemoveAt(RecentEvents.Count - 1);
        }

        if (hadEvents != HasRecentEvents)
        {
            OnPropertyChanged(nameof(HasRecentEvents));
        }
    }

    private static string FormatSignedDbTp(double value)
    {
        return value >= 0
            ? $"+{value:0.#} dBTP"
            : $"{value:0.#} dBTP";
    }

    private static string BuildCollisionEventSignature(
        string selectedSourceKey,
        FrequencyCollisionSourceOption selectedComparison,
        FrequencyCollisionBand band)
    {
        var comparisonKey = selectedComparison.IsAllSources
            ? "__global__"
            : selectedComparison.SourceKey;
        return string.Join(
            "|",
            selectedSourceKey,
            comparisonKey,
            QuantizeCollisionRatio(band.StartRatio),
            QuantizeCollisionRatio(band.EndRatio));
    }

    private static int QuantizeCollisionRatio(double ratio)
    {
        return (int)Math.Round(Math.Clamp(ratio, 0, 1) * 32, MidpointRounding.AwayFromZero);
    }

    private static string FormatCollisionRange(FrequencyCollisionBand band, CollisionFrequencyScale frequencyScale)
    {
        return $"{FormatFrequencyLabel(CollisionFrequencyScaleMapper.ScaleRatioToFrequency(band.StartRatio, frequencyScale))}-{FormatFrequencyLabel(CollisionFrequencyScaleMapper.ScaleRatioToFrequency(band.EndRatio, frequencyScale))}";
    }

    private static string FormatFrequencyLabel(double frequency)
    {
        return frequency >= 1_000
            ? $"{frequency / 1_000:0.#} kHz"
            : $"{frequency:0} Hz";
    }

    private static int ClampMaxRecentEventEntries(int value)
    {
        return Math.Clamp(value, MinimumMaxRecentEventEntries, MaximumMaxRecentEventEntries);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private bool ShouldRetainMissingRow(AudioSessionMeterViewModel session, int missingTicks)
    {
        if (session.SourceKind == SignalSourceKind.Plugin)
        {
            return false;
        }

        if (session.ProcessId is int processId)
        {
            if (!IsProcessStillAlive(processId))
            {
                return false;
            }

            if (missingTicks > _missingRowRetentionTicks)
            {
                session.MarkAudioHandleMissing();
            }

            return true;
        }

        return missingTicks <= _missingRowRetentionTicks;
    }

    private bool TryFindRecoverableRow(
        SignalSourceSnapshot snapshot,
        HashSet<string> seenKeys,
        out AudioSessionMeterViewModel row)
    {
        if (SelectedDetailSession is not null
            && !seenKeys.Contains(SelectedDetailSession.SourceKey)
            && string.Equals(SelectedDetailSession.SourceKey, snapshot.SourceKey, StringComparison.Ordinal))
        {
            row = SelectedDetailSession;
            return true;
        }

        if (SelectedDetailSession is not null
            && !seenKeys.Contains(SelectedDetailSession.SourceKey)
            && SelectedDetailSession.SourceKind == snapshot.SourceKind
            && string.Equals(SelectedDetailSession.DisplayName, snapshot.DisplayName, StringComparison.CurrentCultureIgnoreCase))
        {
            row = SelectedDetailSession;
            return true;
        }

        row = Sessions.FirstOrDefault(session =>
            !seenKeys.Contains(session.SourceKey)
            && session.SourceKind == snapshot.SourceKind
            && !string.IsNullOrWhiteSpace(session.DisplayName)
            && string.Equals(session.DisplayName, snapshot.DisplayName, StringComparison.CurrentCultureIgnoreCase))!;

        return row is not null;
    }

    private void ResetAllSessions()
    {
        if (_sourceMeterService is ISignalSourceResetService resetService)
        {
            resetService.ResetAllSources();
        }

        foreach (var session in Sessions)
        {
            session.Reset();
        }
    }

    private void ResetSession(AudioSessionMeterViewModel session)
    {
        if (_sourceMeterService is ISignalSourceResetService resetService)
        {
            resetService.ResetSource(session.SourceKey, session.SourceKind, session.ProcessId);
        }
    }

    private void AttachRowHandlers(AudioSessionMeterViewModel session)
    {
        session.DetailRequested += RowDetailRequested;
        session.ResetRequested += ResetSession;
        session.PopupRequested += RowPopupRequested;
        session.AliasChanged += RowAliasChanged;
        session.HideRequested += RowHideRequested;
        session.RestoreRequested += RowRestoreRequested;
        session.MoveUpRequested += RowMoveUpRequested;
        session.MoveDownRequested += RowMoveDownRequested;
        session.TruePeakOversamplingRequested += ApplyTruePeakOversampling;
        session.WaveformDisplayModeRequested += ApplyWaveformDisplayMode;
        session.SpectrumDisplayModeRequested += ApplySpectrumDisplayMode;
    }

    private void DetachRowHandlers(AudioSessionMeterViewModel session)
    {
        session.DetailRequested -= RowDetailRequested;
        session.ResetRequested -= ResetSession;
        session.PopupRequested -= RowPopupRequested;
        session.AliasChanged -= RowAliasChanged;
        session.HideRequested -= RowHideRequested;
        session.RestoreRequested -= RowRestoreRequested;
        session.MoveUpRequested -= RowMoveUpRequested;
        session.MoveDownRequested -= RowMoveDownRequested;
        session.TruePeakOversamplingRequested -= ApplyTruePeakOversampling;
        session.WaveformDisplayModeRequested -= ApplyWaveformDisplayMode;
        session.SpectrumDisplayModeRequested -= ApplySpectrumDisplayMode;
    }

    private void RowDetailRequested(AudioSessionMeterViewModel session)
    {
        SelectedDetailSession = session;
    }

    private void RowPopupRequested(AudioSessionMeterViewModel session)
    {
        DetailPopupRequested?.Invoke(session);
    }

    private void RowHideRequested(AudioSessionMeterViewModel session)
    {
        var preferences = GetOrCreatePreferences(session.SourceKey);
        preferences.IsHidden = true;
        session.ApplyWorkspaceState(isHidden: true, preferences.OrderIndex);
        SaveSourcePreferences(session.SourceKey);
        OnPropertyChanged(nameof(HasHiddenSources));
        if (!ShowHiddenSources)
        {
            Sessions.Remove(session);
            if (ReferenceEquals(SelectedDetailSession, session))
            {
                SelectedDetailSession = null;
            }

            NotifyDetailSwitchTargetsChanged();
        }
    }

    private void RowRestoreRequested(AudioSessionMeterViewModel session)
    {
        var preferences = GetOrCreatePreferences(session.SourceKey);
        preferences.IsHidden = false;
        session.ApplyWorkspaceState(isHidden: false, preferences.OrderIndex);
        SaveSourcePreferences(session.SourceKey);
        OnPropertyChanged(nameof(HasHiddenSources));
    }

    private void RowMoveUpRequested(AudioSessionMeterViewModel session)
    {
        MoveSourceUp(session);
    }

    private void RowMoveDownRequested(AudioSessionMeterViewModel session)
    {
        MoveSourceDown(session);
    }

    private void MoveSourceUp(object? parameter)
    {
        if (parameter is AudioSessionMeterViewModel session)
        {
            MoveSource(session, -1);
        }
    }

    private bool CanMoveSourceUp(object? parameter)
    {
        return parameter is AudioSessionMeterViewModel session && Sessions.IndexOf(session) > 0;
    }

    private void MoveSourceDown(object? parameter)
    {
        if (parameter is AudioSessionMeterViewModel session)
        {
            MoveSource(session, 1);
        }
    }

    private bool CanMoveSourceDown(object? parameter)
    {
        return parameter is AudioSessionMeterViewModel session
            && Sessions.IndexOf(session) >= 0
            && Sessions.IndexOf(session) < Sessions.Count - 1;
    }

    private void MoveSource(AudioSessionMeterViewModel session, int offset)
    {
        var currentIndex = Sessions.IndexOf(session);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = Math.Clamp(currentIndex + offset, 0, Sessions.Count - 1);
        if (targetIndex == currentIndex)
        {
            return;
        }

        Sessions.Move(currentIndex, targetIndex);
        PersistCurrentSourceOrder();
        RaiseMoveCommandStateChanged();
    }

    private void PersistCurrentSourceOrder()
    {
        for (var index = 0; index < Sessions.Count; index++)
        {
            var session = Sessions[index];
            var preferences = GetOrCreatePreferences(session.SourceKey);
            preferences.OrderIndex = index;
            session.ApplyWorkspaceState(preferences.IsHidden, preferences.OrderIndex);
            SaveSourcePreferences(session.SourceKey);
        }
    }

    private void RaiseMoveCommandStateChanged()
    {
        _moveSourceUpCommand.RaiseCanExecuteChanged();
        _moveSourceDownCommand.RaiseCanExecuteChanged();
    }

    public void RefreshDetailSwitchTargets()
    {
        NotifyDetailSwitchTargetsChanged();
    }

    private void SwitchDetailSession(object? parameter)
    {
        if (FindLiveDetailSwitchTarget(parameter) is not { } target)
        {
            NotifyDetailSwitchTargetsChanged();
            return;
        }

        SelectedDetailSession = target;
    }

    private void SelectTheme(object? parameter)
    {
        if (parameter is not AppTheme theme)
        {
            return;
        }

        ThemeManager.Apply(theme);
        SelectedTheme = theme;
        _themeSelected?.Invoke(theme);
    }

    private bool CanSwitchDetailSession(object? parameter)
    {
        return FindLiveDetailSwitchTarget(parameter) is not null;
    }

    private AudioSessionMeterViewModel? FindLiveDetailSwitchTarget(object? parameter)
    {
        return parameter is AudioSessionMeterViewModel requested
            ? Sessions.FirstOrDefault(session => ReferenceEquals(session, requested) && IsDetailSwitchTarget(session))
            : null;
    }

    private bool IsDetailSwitchTarget(AudioSessionMeterViewModel session)
    {
        return !ReferenceEquals(session, SelectedDetailSession)
            && !session.IsInstanceMissing;
    }

    private void NotifyDetailSwitchTargetsChanged()
    {
        OnPropertyChanged(nameof(DetailSwitchTargets));
        OnPropertyChanged(nameof(HasDetailSwitchTargets));
        _switchDetailSessionCommand.RaiseCanExecuteChanged();
    }

    private void ApplyTruePeakOversampling(AudioSessionMeterViewModel session, TruePeakOversampling oversampling)
    {
        ApplyTruePeakOversampling(session, oversampling, persist: true);
    }

    private void ApplyTruePeakOversampling(AudioSessionMeterViewModel session, TruePeakOversampling oversampling, bool persist)
    {
        GetOrCreatePreferences(session.SourceKey).TruePeakOversampling = oversampling;
        if (persist && !_isApplyingWorkspacePreferences)
        {
            SaveSourcePreferences(session.SourceKey);
        }

        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetTruePeakOversampling(session.SourceKey, session.SourceKind, session.ProcessId, (int)oversampling);
        }
    }

    private void ApplySpectrumDisplayMode(AudioSessionMeterViewModel session, SpectrumDisplayMode displayMode)
    {
        ApplySpectrumDisplayMode(session, displayMode, persist: true);
    }

    private void ApplySpectrumDisplayMode(AudioSessionMeterViewModel session, SpectrumDisplayMode displayMode, bool persist)
    {
        GetOrCreatePreferences(session.SourceKey).SpectrumDisplayMode = displayMode;
        if (persist && !_isApplyingWorkspacePreferences)
        {
            SaveSourcePreferences(session.SourceKey);
        }

        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetChannelSpectrumEnabled(
                session.SourceKey,
                session.SourceKind,
                session.ProcessId,
                displayMode == SpectrumDisplayMode.Channels);
        }
    }

    private void ApplyPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetPerformanceMode(mode);
        }

        var interval = mode == PcmAnalysisPerformanceMode.Eco ? EcoRefreshInterval : RefreshInterval;
        if (_refreshTimer is not null)
        {
            _refreshTimer.Interval = interval;
        }

        RefreshLabel = FormatRefreshLabel(interval);
        OnPropertyChanged(nameof(RefreshLabel));
    }

    private static string FormatRefreshLabel(TimeSpan interval)
    {
        return $"{interval.TotalMilliseconds:0} ms";
    }

    private void ApplyWaveformDisplayMode(AudioSessionMeterViewModel session, WaveformDisplayMode displayMode)
    {
        ApplyWaveformDisplayMode(session, displayMode, persist: true);
    }

    private void ApplyWaveformDisplayMode(AudioSessionMeterViewModel session, WaveformDisplayMode displayMode, bool persist)
    {
        GetOrCreatePreferences(session.SourceKey).WaveformDisplayMode = displayMode;
        if (persist && !_isApplyingWorkspacePreferences)
        {
            SaveSourcePreferences(session.SourceKey);
        }

        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetChannelWaveformEnabled(
                session.SourceKey,
                session.SourceKind,
                session.ProcessId,
                displayMode == WaveformDisplayMode.Channels);
        }
    }

    private void NotifyInlineRollVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsInlineWaveformOnlyVisible));
        OnPropertyChanged(nameof(IsInlineSpectrumOnlyVisible));
        OnPropertyChanged(nameof(AreInlineRollsVisible));
    }

    private void SelectedDetailSessionOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AudioSessionMeterViewModel.IsChannelWaveformVisible)
            or nameof(AudioSessionMeterViewModel.IsChannelSpectrumVisible))
        {
            NotifyInlineRollLayoutChanged();
        }

        if (args.PropertyName is nameof(AudioSessionMeterViewModel.SpectrumPlotTick))
        {
            RefreshFrequencyCollisions();
        }
    }

    private void InlineStereoSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        SaveSelectedDetailPreferences();
    }

    private void NotifyInlineRollLayoutChanged()
    {
        OnPropertyChanged(nameof(InlineWaveformOnlyPanelMinHeight));
        OnPropertyChanged(nameof(InlineSpectrumOnlyPanelMinHeight));
        OnPropertyChanged(nameof(InlineWaveformOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(InlineSpectrumOnlyPanelDefaultHeight));
        OnPropertyChanged(nameof(InlineWaveformRollGroupMinHeight));
        OnPropertyChanged(nameof(InlineSpectrumRollGroupMinHeight));
        OnPropertyChanged(nameof(InlineRollsPanelMinHeight));
        OnPropertyChanged(nameof(InlineRollsPanelDefaultHeight));
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

    private bool IsProcessStillAlive(int processId)
    {
        try
        {
            return _isProcessAlive(processId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool IsProcessAlive(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public event Action<AudioSessionMeterViewModel>? DetailPopupRequested;

    private void RefreshDisplayNameCollisions()
    {
        var duplicatedNames = Sessions
            .GroupBy(session => session.EffectiveDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        foreach (var session in Sessions)
        {
            session.SetDisplayNameCollision(duplicatedNames.Contains(session.EffectiveDisplayName));
        }
    }

    private SourceRowPreferences GetOrCreatePreferences(string sourceKey)
    {
        if (!_preferencesBySourceKey.TryGetValue(sourceKey, out var preferences))
        {
            preferences = SourceRowPreferences.FromWorkspace(_workspaceSettings.GetSource(sourceKey));
            _preferencesBySourceKey[sourceKey] = preferences;
        }

        return preferences;
    }

    private bool ShouldDisplaySource(SourceRowPreferences preferences)
    {
        return ShowHiddenSources || !preferences.IsHidden;
    }

    private void RowAliasChanged(AudioSessionMeterViewModel session, string? alias)
    {
        if (_isApplyingWorkspacePreferences)
        {
            return;
        }

        GetOrCreatePreferences(session.SourceKey).Alias = alias;
        SaveSourcePreferences(session.SourceKey);
        RefreshDisplayNameCollisions();
        RefreshFrequencyCollisionOptions();
    }

    private void SaveSelectedDetailPreferences()
    {
        if (_isApplyingWorkspacePreferences || SelectedDetailSession is null)
        {
            return;
        }

        var preferences = GetOrCreatePreferences(SelectedDetailSession.SourceKey);
        preferences.InlineWaveformVisible = IsInlineWaveformVisible;
        preferences.InlineSpectrumVisible = IsInlineSpectrumVisible;
        preferences.InlineLoudnessClockVisible = IsInlineLoudnessClockVisible;
        preferences.InlineCollisionVisible = IsInlineCollisionVisible;
        preferences.InlineCollisionSourceKey = SelectedFrequencyCollisionSourceKey == AllCollisionSourcesOption.SourceKey
            ? null
            : SelectedFrequencyCollisionSourceKey;
        preferences.CollisionLevelFloorDb = CollisionLevelFloorDb;
        preferences.CollisionProminenceThresholdDb = CollisionProminenceThresholdDb;
        preferences.CollisionRelativeThreshold = CollisionRelativeThreshold;
        preferences.CollisionFrequencyScale = CollisionFrequencyScale;
        preferences.SpectrumDisplayFloorDb = SpectrumDisplayFloorDb;
        preferences.SpectrumDisplayCeilingDb = SpectrumDisplayCeilingDb;
        preferences.LoudnessClockMinimumDb = LoudnessClockMinimumDb;
        preferences.LoudnessClockMaximumDb = LoudnessClockMaximumDb;
        preferences.LoudnessClockWarningDb = LoudnessClockWarningDb;
        preferences.LoudnessClockCycleSeconds = LoudnessClockCycleSeconds;
        preferences.InlineStereo = StereoWorkspaceSettings.FromSettings(InlineStereoSettings);
        SaveSourcePreferences(SelectedDetailSession.SourceKey);
    }

    private void ApplySelectedDetailPreferences()
    {
        if (SelectedDetailSession is not { } session)
        {
            return;
        }

        var preferences = GetOrCreatePreferences(session.SourceKey);
        _isApplyingWorkspacePreferences = true;
        try
        {
            IsInlineWaveformVisible = preferences.InlineWaveformVisible ?? false;
            IsInlineSpectrumVisible = preferences.InlineSpectrumVisible ?? false;
            IsInlineLoudnessClockVisible = preferences.InlineLoudnessClockVisible ?? false;
            IsInlineCollisionVisible = preferences.InlineCollisionVisible ?? false;
            CollisionLevelFloorDb = preferences.CollisionLevelFloorDb ?? DefaultCollisionLevelFloorDb;
            CollisionProminenceThresholdDb = preferences.CollisionProminenceThresholdDb ?? DefaultCollisionProminenceThresholdDb;
            CollisionRelativeThreshold = preferences.CollisionRelativeThreshold ?? DefaultCollisionRelativeThreshold;
            CollisionFrequencyScale = preferences.CollisionFrequencyScale ?? CollisionFrequencyScale.Logarithmic;
            SpectrumDisplayFloorDb = preferences.SpectrumDisplayFloorDb ?? SpectrumDisplayDefaultFloorDb;
            SpectrumDisplayCeilingDb = preferences.SpectrumDisplayCeilingDb ?? SpectrumDisplayDefaultCeilingDb;
            LoudnessClockMinimumDb = preferences.LoudnessClockMinimumDb ?? LoudnessClockDefaultMinimumDb;
            LoudnessClockMaximumDb = preferences.LoudnessClockMaximumDb ?? LoudnessClockDefaultMaximumDb;
            LoudnessClockWarningDb = preferences.LoudnessClockWarningDb ?? LoudnessClockDefaultWarningDb;
            LoudnessClockCycleSeconds = preferences.LoudnessClockCycleSeconds ?? LoudnessClockDefaultCycleSeconds;
            if (preferences.InlineStereo is not null)
            {
                preferences.InlineStereo.ApplyTo(InlineStereoSettings);
            }
            else
            {
                InlineStereoSettings.Reset();
            }

            SelectedFrequencyCollisionSourceKey = preferences.InlineCollisionSourceKey ?? AllCollisionSourcesOption.SourceKey;
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }
    }

    private void SaveSourcePreferences(string sourceKey)
    {
        if (!_preferencesBySourceKey.TryGetValue(sourceKey, out var preferences))
        {
            return;
        }

        preferences.ApplyToWorkspace(_workspaceSettings.GetOrCreateSource(sourceKey));
        _workspaceSettings.Save();
    }

    private void ForgetSelectedSource()
    {
        if (SelectedDetailSession is not { } session)
        {
            return;
        }

        _preferencesBySourceKey.Remove(session.SourceKey);
        _workspaceSettings.ForgetSource(session.SourceKey);
        _workspaceSettings.Save();
        _isApplyingWorkspacePreferences = true;
        try
        {
            session.Alias = null;
            session.TruePeakOversampling = SelectedTruePeakOversampling;
            session.WaveformDisplayMode = WaveformDisplayMode.Overall;
            session.SpectrumDisplayMode = SpectrumDisplayMode.Overall;
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }

        ApplyTruePeakOversampling(session, session.TruePeakOversampling, persist: false);
        ApplyWaveformDisplayMode(session, session.WaveformDisplayMode, persist: false);
        ApplySpectrumDisplayMode(session, session.SpectrumDisplayMode, persist: false);
        _isApplyingWorkspacePreferences = true;
        try
        {
            IsInlineWaveformVisible = false;
            IsInlineSpectrumVisible = false;
            IsInlineLoudnessClockVisible = false;
            IsInlineCollisionVisible = false;
            SelectedFrequencyCollisionSourceKey = AllCollisionSourcesOption.SourceKey;
            CollisionLevelFloorDb = DefaultCollisionLevelFloorDb;
            CollisionProminenceThresholdDb = DefaultCollisionProminenceThresholdDb;
            CollisionRelativeThreshold = DefaultCollisionRelativeThreshold;
            CollisionFrequencyScale = CollisionFrequencyScale.Logarithmic;
            SpectrumDisplayFloorDb = SpectrumDisplayDefaultFloorDb;
            SpectrumDisplayCeilingDb = SpectrumDisplayDefaultCeilingDb;
            LoudnessClockMinimumDb = LoudnessClockDefaultMinimumDb;
            LoudnessClockMaximumDb = LoudnessClockDefaultMaximumDb;
            LoudnessClockWarningDb = LoudnessClockDefaultWarningDb;
            LoudnessClockCycleSeconds = LoudnessClockDefaultCycleSeconds;
            InlineStereoSettings.Reset();
        }
        finally
        {
            _isApplyingWorkspacePreferences = false;
        }

        RefreshDisplayNameCollisions();
        RefreshFrequencyCollisionOptions();
    }

    private void SortSessions()
    {
        var sorted = Sessions
            .OrderBy(session => session.OrderIndex is null ? 1 : 0)
            .ThenBy(session => session.OrderIndex ?? int.MaxValue)
            .ThenBy(session => SourceKindSortRank(session.SourceKind))
            .ThenBy(session => session.EffectiveDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(session => session.SourceKey, StringComparer.Ordinal)
            .ToArray();

        for (var targetIndex = 0; targetIndex < sorted.Length; targetIndex++)
        {
            var currentIndex = Sessions.IndexOf(sorted[targetIndex]);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                Sessions.Move(currentIndex, targetIndex);
            }
        }

        RaiseMoveCommandStateChanged();
    }

    private static int SourceKindSortRank(SignalSourceKind kind)
    {
        return kind switch
        {
            SignalSourceKind.Plugin => 0,
            SignalSourceKind.LocalLoopback => 1,
            SignalSourceKind.WasapiSession => 2,
            _ => 3
        };
    }

    private sealed class SourceRowPreferences
    {
        public string? Alias { get; set; }

        public bool IsHidden { get; set; }

        public int? OrderIndex { get; set; }

        public TruePeakOversampling? TruePeakOversampling { get; set; }

        public WaveformDisplayMode WaveformDisplayMode { get; set; } = WaveformDisplayMode.Overall;

        public SpectrumDisplayMode SpectrumDisplayMode { get; set; } = SpectrumDisplayMode.Overall;

        public bool? InlineWaveformVisible { get; set; }

        public bool? InlineSpectrumVisible { get; set; }

        public bool? InlineLoudnessClockVisible { get; set; }

        public bool? InlineCollisionVisible { get; set; }

        public string? InlineCollisionSourceKey { get; set; }

        public double? CollisionLevelFloorDb { get; set; }

        public double? CollisionProminenceThresholdDb { get; set; }

        public double? CollisionRelativeThreshold { get; set; }

        public CollisionFrequencyScale? CollisionFrequencyScale { get; set; }

        public double? SpectrumDisplayFloorDb { get; set; }

        public double? SpectrumDisplayCeilingDb { get; set; }

        public double? LoudnessClockMinimumDb { get; set; }

        public double? LoudnessClockMaximumDb { get; set; }

        public double? LoudnessClockWarningDb { get; set; }

        public double? LoudnessClockCycleSeconds { get; set; }

        public StereoWorkspaceSettings? InlineStereo { get; set; }

        public static SourceRowPreferences FromWorkspace(SourceWorkspaceSettings? workspace)
        {
            return new SourceRowPreferences
            {
                Alias = workspace?.Alias,
                IsHidden = workspace?.IsHidden ?? false,
                OrderIndex = workspace?.OrderIndex,
                TruePeakOversampling = workspace?.TruePeakOversampling,
                WaveformDisplayMode = workspace?.WaveformDisplayMode ?? WaveformDisplayMode.Overall,
                SpectrumDisplayMode = workspace?.SpectrumDisplayMode ?? SpectrumDisplayMode.Overall,
                InlineWaveformVisible = workspace?.InlineWaveformVisible,
                InlineSpectrumVisible = workspace?.InlineSpectrumVisible,
                InlineLoudnessClockVisible = workspace?.InlineLoudnessClockVisible,
                InlineCollisionVisible = workspace?.InlineCollisionVisible,
                InlineCollisionSourceKey = workspace?.InlineCollisionSourceKey,
                CollisionLevelFloorDb = GetCollisionLevelFloorDb(workspace),
                CollisionProminenceThresholdDb = workspace?.CollisionProminenceThresholdDb,
                CollisionRelativeThreshold = workspace?.CollisionRelativeThreshold,
                CollisionFrequencyScale = workspace?.CollisionFrequencyScale,
                SpectrumDisplayFloorDb = workspace?.SpectrumDisplayFloorDb,
                SpectrumDisplayCeilingDb = workspace?.SpectrumDisplayCeilingDb,
                LoudnessClockMinimumDb = workspace?.LoudnessClockMinimumDb,
                LoudnessClockMaximumDb = workspace?.LoudnessClockMaximumDb,
                LoudnessClockWarningDb = workspace?.LoudnessClockWarningDb,
                LoudnessClockCycleSeconds = workspace?.LoudnessClockCycleSeconds,
                InlineStereo = workspace?.InlineStereo
            };
        }

        private static double? GetCollisionLevelFloorDb(SourceWorkspaceSettings? workspace)
        {
            if (workspace?.CollisionLevelFloorDb is double levelFloorDb)
            {
                return levelFloorDb;
            }

            return workspace?.CollisionAbsoluteThresholdDb is double legacyLevelFloorDb && legacyLevelFloorDb <= 0
                ? legacyLevelFloorDb
                : null;
        }

        public void ApplyToWorkspace(SourceWorkspaceSettings workspace)
        {
            workspace.Alias = Alias;
            workspace.IsHidden = IsHidden;
            workspace.OrderIndex = OrderIndex;
            workspace.TruePeakOversampling = TruePeakOversampling;
            workspace.WaveformDisplayMode = WaveformDisplayMode;
            workspace.SpectrumDisplayMode = SpectrumDisplayMode;
            workspace.InlineWaveformVisible = InlineWaveformVisible;
            workspace.InlineSpectrumVisible = InlineSpectrumVisible;
            workspace.InlineLoudnessClockVisible = InlineLoudnessClockVisible;
            workspace.InlineCollisionVisible = InlineCollisionVisible;
            workspace.InlineCollisionSourceKey = InlineCollisionSourceKey;
            workspace.CollisionLevelFloorDb = CollisionLevelFloorDb;
            workspace.CollisionProminenceThresholdDb = CollisionProminenceThresholdDb;
            workspace.CollisionRelativeThreshold = CollisionRelativeThreshold;
            workspace.CollisionFrequencyScale = CollisionFrequencyScale;
            workspace.SpectrumDisplayFloorDb = SpectrumDisplayFloorDb;
            workspace.SpectrumDisplayCeilingDb = SpectrumDisplayCeilingDb;
            workspace.LoudnessClockMinimumDb = LoudnessClockMinimumDb;
            workspace.LoudnessClockMaximumDb = LoudnessClockMaximumDb;
            workspace.LoudnessClockWarningDb = LoudnessClockWarningDb;
            workspace.LoudnessClockCycleSeconds = LoudnessClockCycleSeconds;
            workspace.InlineStereo = InlineStereo;
        }
    }
}
