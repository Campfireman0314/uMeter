using System.Collections.ObjectModel;
using DesktopLoudnessMeter.App.Audio;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Themes;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const int DefaultMissingRowRetentionTicks = 150;
    private const double RollTrackMinHeight = 185;
    private const double RollGroupPadding = 20;
    private const double RollChannelLabelAllowance = 40;
    private const double RollSplitterHeight = 8;
    private const double RollSplitterFlexAllowance = 128;
    private const double DefaultCollisionAbsoluteThresholdDb = 6;
    private const double DefaultCollisionRelativeThreshold = 0.18;
    private const double SpectrumDisplayMinimumDb = -96;
    private const double SpectrumDisplayMaximumDb = 0;
    private const double SpectrumDisplayMinimumSpanDb = 6;
    private static readonly FrequencyCollisionSourceOption AllCollisionSourcesOption = new("__global__", "Global", isAllSources: true);
    private static readonly TimeSpan NormalRefreshInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan EcoRefreshInterval = TimeSpan.FromMilliseconds(100);
    private readonly ISignalSourceMeterService _sourceMeterService;
    private readonly DispatcherTimer? _refreshTimer;
    private readonly Dictionary<string, int> _missingTicksByKey = [];
    private readonly Dictionary<string, SourceRowPreferences> _preferencesBySourceKey = [];
    private readonly int _missingRowRetentionTicks;
    private readonly Func<int, bool> _isProcessAlive;
    private readonly Action<AppTheme>? _themeSelected;
    private readonly string? _sourceWarningText;
    private readonly RelayCommand _selectFrequencyCollisionSourceCommand;
    private string _statusText = "Waiting for audio sources";
    private MeterDisplayMode _selectedDisplayMode = MeterDisplayMode.Peak;
    private bool _truePeakEnabled;
    private bool _isEcoModeEnabled;
    private AppTheme _selectedTheme = AppThemes.Default;
    private TruePeakOversampling _selectedTruePeakOversampling = TruePeakOversampling.FourX;
    private string _selectedFrequencyCollisionSourceKey = AllCollisionSourcesOption.SourceKey;
    private double _collisionAbsoluteThresholdDb = DefaultCollisionAbsoluteThresholdDb;
    private double _collisionRelativeThreshold = DefaultCollisionRelativeThreshold;
    private double _spectrumDisplayFloorDb = SpectrumDisplayMinimumDb;
    private double _spectrumDisplayCeilingDb = SpectrumDisplayMaximumDb;
    private IReadOnlyList<FrequencyCollisionSourceOption> _frequencyCollisionSourceOptions = [AllCollisionSourcesOption];
    private IReadOnlyList<FrequencyCollisionBand> _frequencyCollisionBands = Array.Empty<FrequencyCollisionBand>();
    private bool _isAlwaysOnTop = true;
    private bool _isInlineWaveformVisible;
    private bool _isInlineSpectrumVisible;
    private bool _isInlineCollisionVisible;
    private AudioSessionMeterViewModel? _selectedDetailSession;
    private readonly RelayCommand _switchDetailSessionCommand;

    public MainViewModel(
        ISignalSourceMeterService sourceMeterService,
        TimeSpan? refreshInterval = null,
        bool startTimer = true,
        int missingRowRetentionTicks = DefaultMissingRowRetentionTicks,
        Func<int, bool>? isProcessAlive = null,
        string? sourceWarningText = null,
        AppTheme? selectedTheme = null,
        Action<AppTheme>? themeSelected = null)
    {
        _sourceMeterService = sourceMeterService;
        _themeSelected = themeSelected;
        _selectedTheme = selectedTheme ?? AppThemes.Default;
        _sourceWarningText = string.IsNullOrWhiteSpace(sourceWarningText) ? null : sourceWarningText.Trim();
        _missingRowRetentionTicks = Math.Max(0, missingRowRetentionTicks);
        _isProcessAlive = isProcessAlive ?? IsProcessAlive;
        RefreshInterval = refreshInterval ?? NormalRefreshInterval;
        RefreshLabel = FormatRefreshLabel(RefreshInterval);

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
    }

    public ObservableCollection<AudioSessionMeterViewModel> Sessions { get; } = [];

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

    public double CollisionAbsoluteThresholdDb
    {
        get => _collisionAbsoluteThresholdDb;
        set
        {
            var next = Math.Clamp(value, 0.5, 36);
            if (SetProperty(ref _collisionAbsoluteThresholdDb, next))
            {
                OnPropertyChanged(nameof(CollisionAbsoluteThresholdText));
                RefreshFrequencyCollisions();
            }
        }
    }

    public string CollisionAbsoluteThresholdText => $"{CollisionAbsoluteThresholdDb:0.#} dB";

    public double CollisionRelativeThreshold
    {
        get => _collisionRelativeThreshold;
        set
        {
            var next = Math.Clamp(value, 0.02, 0.8);
            if (SetProperty(ref _collisionRelativeThreshold, next))
            {
                OnPropertyChanged(nameof(CollisionRelativeThresholdText));
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
                RefreshFrequencyCollisionOptions();
                RefreshFrequencyCollisions();
            }
        }
    }

    public bool IsInlineWaveformOnlyVisible => IsInlineWaveformVisible && !IsInlineSpectrumVisible;

    public bool IsInlineSpectrumOnlyVisible => IsInlineSpectrumVisible && !IsInlineWaveformVisible;

    public bool AreInlineRollsVisible => IsInlineWaveformVisible && IsInlineSpectrumVisible;

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

    public ICommand SwitchDetailSessionCommand => _switchDetailSessionCommand;

    public IReadOnlyList<AudioSessionMeterViewModel> DetailSwitchTargets =>
        Sessions
            .Where(IsDetailSwitchTarget)
            .ToArray();

    public bool HasDetailSwitchTargets => Sessions.Any(IsDetailSwitchTarget);

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
            NotifyInlineRollLayoutChanged();
            NotifyDetailSwitchTargetsChanged();
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
                if (!Sessions.Contains(existing))
                {
                    Sessions.Add(existing);
                    activeRowsChanged = true;
                }

                var previousKey = existing.SourceKey;
                existing.Update(snapshot);
                _missingTicksByKey.Remove(previousKey);
                ApplyTruePeakOversampling(existing, existing.TruePeakOversampling);
                ApplyWaveformDisplayMode(existing, existing.WaveformDisplayMode);
                ApplySpectrumDisplayMode(existing, existing.SpectrumDisplayMode);
                matchedRows.Add(existing);
            }
            else
            {
                var created = new AudioSessionMeterViewModel(snapshot);
                created.ApplyDisplayMode(SelectedDisplayMode);
                created.DetailRequested += row => SelectedDetailSession = row;
                created.ResetRequested += ResetSession;
                created.PopupRequested += row => DetailPopupRequested?.Invoke(row);
                created.TruePeakOversamplingRequested += ApplyTruePeakOversampling;
                created.WaveformDisplayModeRequested += ApplyWaveformDisplayMode;
                created.SpectrumDisplayModeRequested += ApplySpectrumDisplayMode;
                var preferences = GetPreferences(created.SourceKey);
                var targetOversampling = preferences?.TruePeakOversampling ?? SelectedTruePeakOversampling;
                if (created.TruePeakOversampling == targetOversampling)
                {
                    ApplyTruePeakOversampling(created, created.TruePeakOversampling);
                }
                else
                {
                    created.TruePeakOversampling = targetOversampling;
                }

                created.WaveformDisplayMode = preferences?.WaveformDisplayMode ?? created.WaveformDisplayMode;
                created.SpectrumDisplayMode = preferences?.SpectrumDisplayMode ?? created.SpectrumDisplayMode;
                ApplyWaveformDisplayMode(created, created.WaveformDisplayMode);
                ApplySpectrumDisplayMode(created, created.SpectrumDisplayMode);
                matchedRows.Add(created);
                Sessions.Add(created);
                activeRowsChanged = true;
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
            Sessions.RemoveAt(index);
            activeRowsChanged = true;
        }

        SortSessions();
        RefreshDisplayNameCollisions();
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
            return;
        }

        var selectedComparison = SelectedFrequencyCollisionSource;
        SetFrequencyCollisionBands(FrequencyCollisionAnalyzer.Analyze(
            Sessions,
            selected.SourceKey,
            selectedComparison.SourceKey,
            selectedComparison.IsAllSources,
            CollisionAbsoluteThresholdDb,
            CollisionRelativeThreshold));
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
        if (FrequencyCollisionBands.SequenceEqual(bands))
        {
            return;
        }

        FrequencyCollisionBands = bands;
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
        GetOrCreatePreferences(session.SourceKey).TruePeakOversampling = oversampling;
        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetTruePeakOversampling(session.SourceKey, session.SourceKind, session.ProcessId, (int)oversampling);
        }
    }

    private void ApplySpectrumDisplayMode(AudioSessionMeterViewModel session, SpectrumDisplayMode displayMode)
    {
        GetOrCreatePreferences(session.SourceKey).SpectrumDisplayMode = displayMode;
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
        GetOrCreatePreferences(session.SourceKey).WaveformDisplayMode = displayMode;
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
            : SpectrumDisplayMinimumDb;
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
            .GroupBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        foreach (var session in Sessions)
        {
            session.SetDisplayNameCollision(duplicatedNames.Contains(session.DisplayName));
        }
    }

    private SourceRowPreferences? GetPreferences(string sourceKey)
    {
        return _preferencesBySourceKey.GetValueOrDefault(sourceKey);
    }

    private SourceRowPreferences GetOrCreatePreferences(string sourceKey)
    {
        if (!_preferencesBySourceKey.TryGetValue(sourceKey, out var preferences))
        {
            preferences = new SourceRowPreferences();
            _preferencesBySourceKey[sourceKey] = preferences;
        }

        return preferences;
    }

    private void SortSessions()
    {
        var sorted = Sessions
            .OrderBy(session => SourceKindSortRank(session.SourceKind))
            .ThenBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
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
        public TruePeakOversampling TruePeakOversampling { get; set; } = TruePeakOversampling.FourX;

        public WaveformDisplayMode WaveformDisplayMode { get; set; } = WaveformDisplayMode.Overall;

        public SpectrumDisplayMode SpectrumDisplayMode { get; set; } = SpectrumDisplayMode.Overall;
    }
}
