using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using System.Windows.Input;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class AudioSessionMeterViewModel : ObservableObject
{
    private static readonly IReadOnlyList<StereoImagePoint> EmptyStereoImagePoints = Array.Empty<StereoImagePoint>();
    private static readonly IReadOnlyList<double> EmptyDoubleSamples = Array.Empty<double>();
    private string _sourceKey;
    private SignalSourceKind _sourceKind;
    private string _displayName;
    private string? _alias;
    private int? _processId;
    private double _peakDb;
    private bool _hasLivePeak = true;
    private string _peakDbText;
    private string _primaryValueText;
    private ReadoutSeverity _primaryValueSeverity;
    private float _displayLevel;
    private string _state;
    private bool _hasDisplayNameCollision;
    private MeterDisplayMode _displayMode = MeterDisplayMode.Peak;
    private double? _momentaryLufs;
    private double? _shortTermLufs;
    private double? _integratedLufs;
    private double? _truePeakDb;
    private double? _truePeakMaxDb;
    private double? _rms100MsDb;
    private SignalLevelMetrics? _midMetrics;
    private SignalLevelMetrics? _sideMetrics;
    private double? _midTruePeakMaxDb;
    private double? _sideTruePeakMaxDb;
    private SignalMeasurementMode _momentaryMeasurementMode;
    private SignalMeasurementMode _shortTermMeasurementMode;
    private SignalMeasurementMode _integratedMeasurementMode;
    private SignalMeasurementMode _truePeakMeasurementMode;
    private SignalMeasurementMode _rmsMeasurementMode;
    private double? _stereoImageX;
    private double? _stereoImageY;
    private IReadOnlyList<StereoImagePoint> _stereoImagePoints = EmptyStereoImagePoints;
    private double? _correlation;
    private IReadOnlyList<double> _waveformSamples = EmptyDoubleSamples;
    private IReadOnlyList<double> _leftWaveformSamples = EmptyDoubleSamples;
    private IReadOnlyList<double> _rightWaveformSamples = EmptyDoubleSamples;
    private IReadOnlyList<double> _spectrumBuckets = EmptyDoubleSamples;
    private IReadOnlyList<double> _leftSpectrumBuckets = EmptyDoubleSamples;
    private IReadOnlyList<double> _rightSpectrumBuckets = EmptyDoubleSamples;
    private long _waveformPlotTick;
    private long _spectrumPlotTick;
    private long _loudnessClockPlotTick;
    private long _lastWaveformPlotFrameVersion;
    private long _lastSpectrumPlotFrameVersion;
    private long _lastLoudnessClockPlotFrameVersion;
    private int _resetCount;
    private TruePeakOversampling _truePeakOversampling = TruePeakOversampling.FourX;
    private WaveformDisplayMode _waveformDisplayMode = WaveformDisplayMode.Overall;
    private SpectrumDisplayMode _spectrumDisplayMode = SpectrumDisplayMode.Overall;
    private bool _isInstanceMissing;
    private bool _isHidden;
    private int? _orderIndex;
    private bool _isAliasEditing;
    private string _aliasEditText = string.Empty;
    private bool _isReorderDropBefore;
    private bool _isReorderDropAfter;

    public AudioSessionMeterViewModel(SignalSourceSnapshot snapshot)
    {
        _sourceKey = snapshot.SourceKey;
        _sourceKind = snapshot.SourceKind;
        _processId = snapshot.ProcessId;
        _displayName = snapshot.DisplayName;
        _peakDb = snapshot.PeakDb;
        _peakDbText = snapshot.PeakDbText;
        _primaryValueText = snapshot.PeakDbText;
        _primaryValueSeverity = CalculatePrimaryValueSeverity();
        _displayLevel = snapshot.Peak;
        _state = snapshot.State;
        ApplyAnalyzerMetrics(snapshot);
        ResetCommand = new RelayCommand(_ => RequestReset());
        OpenDetailCommand = new RelayCommand(_ => DetailRequested?.Invoke(this));
        OpenPopupCommand = new RelayCommand(_ => PopupRequested?.Invoke(this));
        StartAliasEditCommand = new RelayCommand(_ => StartAliasEdit());
        CommitAliasEditCommand = new RelayCommand(_ => CommitAliasEdit());
        CancelAliasEditCommand = new RelayCommand(_ => CancelAliasEdit());
        HideSourceCommand = new RelayCommand(_ => HideRequested?.Invoke(this), _ => !IsHidden);
        RestoreSourceCommand = new RelayCommand(_ => RestoreRequested?.Invoke(this), _ => IsHidden);
        MoveUpCommand = new RelayCommand(_ => MoveUpRequested?.Invoke(this));
        MoveDownCommand = new RelayCommand(_ => MoveDownRequested?.Invoke(this));
    }

    public string SourceKey
    {
        get => _sourceKey;
        private set
        {
            if (SetProperty(ref _sourceKey, value))
            {
                OnPropertyChanged(nameof(SessionKey));
                OnPropertyChanged(nameof(SourceIdentityText));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string SessionKey => SourceKey;

    public SignalSourceKind SourceKind
    {
        get => _sourceKind;
        private set
        {
            if (SetProperty(ref _sourceKind, value))
            {
                OnPropertyChanged(nameof(SourceKindLabel));
                OnPropertyChanged(nameof(SourceIdentityText));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string SourceKindLabel => SourceKind.ToDisplayLabel();

    public string SourceIdentityText
    {
        get
        {
            if (SourceKind == SignalSourceKind.WasapiSession && ProcessId is int processId)
            {
                return $"{SourceKindLabel} source - PID {processId}";
            }

            var stableId = CompactSourceIdentifier(SourceKey);
            return string.IsNullOrWhiteSpace(stableId)
                ? $"{SourceKindLabel} source"
                : $"{SourceKindLabel} source - {stableId}";
        }
    }

    public int? ProcessId
    {
        get => _processId;
        private set
        {
            if (SetProperty(ref _processId, value))
            {
                OnPropertyChanged(nameof(SourceIdentityText));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string DisplayName
    {
        get => _displayName;
        private set
        {
            if (SetProperty(ref _displayName, value))
            {
                OnPropertyChanged(nameof(EffectiveDisplayName));
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(HasAlias));
            }
        }
    }

    public string EffectiveDisplayName => string.IsNullOrWhiteSpace(Alias) ? DisplayName : Alias!;

    public string? Alias
    {
        get => _alias;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (SetProperty(ref _alias, next))
            {
                OnPropertyChanged(nameof(EffectiveDisplayName));
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(HasAlias));
                AliasChanged?.Invoke(this, next);
            }
        }
    }

    public bool HasAlias => !string.IsNullOrWhiteSpace(Alias);

    public string DisplayTitle => _hasDisplayNameCollision
        ? $"{EffectiveDisplayName} - {SourceQualifier}"
        : EffectiveDisplayName;

    public string PeakDbText
    {
        get => _peakDbText;
        private set => SetProperty(ref _peakDbText, value);
    }

    public string PrimaryValueText
    {
        get => _primaryValueText;
        private set => SetProperty(ref _primaryValueText, value);
    }

    public ReadoutSeverity PrimaryValueSeverity
    {
        get => _primaryValueSeverity;
        private set => SetProperty(ref _primaryValueSeverity, value);
    }

    public float DisplayLevel
    {
        get => _displayLevel;
        private set => SetProperty(ref _displayLevel, value);
    }

    public string State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(SourceStatusText));
            }
        }
    }

    public string SourceStatusText => State;

    public MeterDisplayMode DisplayMode
    {
        get => _displayMode;
        private set => SetProperty(ref _displayMode, value);
    }

    public int ResetCount
    {
        get => _resetCount;
        private set => SetProperty(ref _resetCount, value);
    }

    public TruePeakOversampling TruePeakOversampling
    {
        get => _truePeakOversampling;
        set
        {
            if (SetProperty(ref _truePeakOversampling, value))
            {
                TruePeakOversamplingRequested?.Invoke(this, value);
            }
        }
    }

    public SpectrumDisplayMode SpectrumDisplayMode
    {
        get => _spectrumDisplayMode;
        set
        {
            if (SetProperty(ref _spectrumDisplayMode, value))
            {
                OnPropertyChanged(nameof(IsOverallSpectrumVisible));
                OnPropertyChanged(nameof(IsChannelSpectrumVisible));
                SpectrumDisplayModeRequested?.Invoke(this, value);
            }
        }
    }

    public WaveformDisplayMode WaveformDisplayMode
    {
        get => _waveformDisplayMode;
        set
        {
            if (SetProperty(ref _waveformDisplayMode, value))
            {
                OnPropertyChanged(nameof(IsOverallWaveformVisible));
                OnPropertyChanged(nameof(IsChannelWaveformVisible));
                WaveformDisplayModeRequested?.Invoke(this, value);
            }
        }
    }

    public ICommand ResetCommand { get; }

    public ICommand OpenDetailCommand { get; }

    public ICommand OpenPopupCommand { get; }

    public ICommand StartAliasEditCommand { get; }

    public ICommand CommitAliasEditCommand { get; }

    public ICommand CancelAliasEditCommand { get; }

    public ICommand HideSourceCommand { get; }

    public ICommand RestoreSourceCommand { get; }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }

    public event Action<AudioSessionMeterViewModel>? DetailRequested;

    public event Action<AudioSessionMeterViewModel>? PopupRequested;

    public event Action<AudioSessionMeterViewModel, TruePeakOversampling>? TruePeakOversamplingRequested;

    public event Action<AudioSessionMeterViewModel, WaveformDisplayMode>? WaveformDisplayModeRequested;

    public event Action<AudioSessionMeterViewModel, SpectrumDisplayMode>? SpectrumDisplayModeRequested;

    public event Action<AudioSessionMeterViewModel, string?>? AliasChanged;

    public event Action<AudioSessionMeterViewModel>? HideRequested;

    public event Action<AudioSessionMeterViewModel>? RestoreRequested;

    public event Action<AudioSessionMeterViewModel>? MoveUpRequested;

    public event Action<AudioSessionMeterViewModel>? MoveDownRequested;

    public string MomentaryLufsText => FormatLufs(_momentaryLufs);

    public SignalMeasurementMode MomentaryMeasurementMode
    {
        get => _momentaryMeasurementMode;
        set
        {
            if (SetProperty(ref _momentaryMeasurementMode, value))
            {
                OnPropertyChanged(nameof(DetailMomentaryLufsText));
                OnPropertyChanged(nameof(DetailMomentaryLufsLevel));
                OnPropertyChanged(nameof(DetailMomentaryLufsLabel));
            }
        }
    }

    public SignalMeasurementMode ShortTermMeasurementMode
    {
        get => _shortTermMeasurementMode;
        set
        {
            if (SetProperty(ref _shortTermMeasurementMode, value))
            {
                OnPropertyChanged(nameof(DetailShortTermLufsText));
                OnPropertyChanged(nameof(DetailShortTermLufsLevel));
                OnPropertyChanged(nameof(DetailShortTermLufsLabel));
            }
        }
    }

    public SignalMeasurementMode IntegratedMeasurementMode
    {
        get => _integratedMeasurementMode;
        set
        {
            if (SetProperty(ref _integratedMeasurementMode, value))
            {
                OnPropertyChanged(nameof(DetailIntegratedLufsText));
                OnPropertyChanged(nameof(DetailIntegratedLufsLevel));
                OnPropertyChanged(nameof(DetailIntegratedLufsLabel));
            }
        }
    }

    public SignalMeasurementMode TruePeakMeasurementMode
    {
        get => _truePeakMeasurementMode;
        set
        {
            if (SetProperty(ref _truePeakMeasurementMode, value))
            {
                NotifyDetailTruePeakChanged();
                OnPropertyChanged(nameof(DetailTruePeakLabel));
            }
        }
    }

    public SignalMeasurementMode RmsMeasurementMode
    {
        get => _rmsMeasurementMode;
        set
        {
            if (SetProperty(ref _rmsMeasurementMode, value))
            {
                OnPropertyChanged(nameof(DetailRms100MsText));
                OnPropertyChanged(nameof(DetailRms100MsLevel));
                OnPropertyChanged(nameof(DetailRms100MsLabel));
            }
        }
    }

    public double LoudnessClockMomentaryDb => _momentaryLufs ?? double.NaN;

    public string ShortTermLufsText => FormatLufs(_shortTermLufs);

    public string IntegratedLufsText => FormatLufs(_integratedLufs);

    public string TruePeakText => FormatCompactDb(_truePeakDb);

    public string TruePeakMaxText => _truePeakMaxDb is null
        ? "max n/a"
        : $"max {FormatCompactDb(_truePeakMaxDb)}";

    public ReadoutSeverity TruePeakReadoutSeverity => CalculateDbReadoutSeverity(GetTruePeakSeverityValue(), hasValue: true);

    public string Rms100MsText => FormatDb(_rms100MsDb);

    public string StereoImageText => _stereoImageX is null || _stereoImageY is null
        ? "n/a"
        : $"({_stereoImageX.Value:0.00}, {_stereoImageY.Value:0.00})";

    public string CorrelationText => _correlation is null
        ? "n/a"
        : $"{_correlation.Value:0.00}";

    public double MomentaryLufsLevel => NormalizeLufs(_momentaryLufs);

    public double ShortTermLufsLevel => NormalizeLufs(_shortTermLufs);

    public double IntegratedLufsLevel => NormalizeLufs(_integratedLufs);

    public double TruePeakLevel => NormalizeDb(_truePeakDb, -60, 3);

    public double Rms100MsLevel => NormalizeDb(_rms100MsDb, -60, 0);

    public string DetailMomentaryLufsText => FormatLufs(GetMetrics(MomentaryMeasurementMode).MomentaryLufs);

    public string DetailMomentaryLufsLabel => FormatDetailLabel("Momentary loudness", MomentaryMeasurementMode);

    public double DetailMomentaryLufsLevel => NormalizeLufs(GetMetrics(MomentaryMeasurementMode).MomentaryLufs);

    public string DetailShortTermLufsText => FormatLufs(GetMetrics(ShortTermMeasurementMode).ShortTermLufs);

    public string DetailShortTermLufsLabel => FormatDetailLabel("Short-term loudness", ShortTermMeasurementMode);

    public double DetailShortTermLufsLevel => NormalizeLufs(GetMetrics(ShortTermMeasurementMode).ShortTermLufs);

    public string DetailIntegratedLufsText => FormatLufs(GetMetrics(IntegratedMeasurementMode).IntegratedLufs);

    public string DetailIntegratedLufsLabel => FormatDetailLabel("Integrated loudness", IntegratedMeasurementMode);

    public double DetailIntegratedLufsLevel => NormalizeLufs(GetMetrics(IntegratedMeasurementMode).IntegratedLufs);

    public string DetailTruePeakText => FormatCompactDb(GetMetrics(TruePeakMeasurementMode).TruePeakDb);

    public string DetailTruePeakLabel => FormatDetailLabel("True-peak level", TruePeakMeasurementMode);

    public string DetailTruePeakMaxText => GetTruePeakMax(TruePeakMeasurementMode) is not double maximum
        ? "max n/a"
        : $"max {FormatCompactDb(maximum)}";

    public ReadoutSeverity DetailTruePeakReadoutSeverity => CalculateDbReadoutSeverity(
        GetDetailTruePeakSeverityValue(),
        hasValue: true);

    public double DetailTruePeakLevel => NormalizeDb(GetMetrics(TruePeakMeasurementMode).TruePeakDb, -60, 3);

    public string DetailRms100MsText => FormatDb(GetMetrics(RmsMeasurementMode).Rms100MsDb);

    public string DetailRms100MsLabel => FormatDetailLabel("100 ms RMS level", RmsMeasurementMode);

    public double DetailRms100MsLevel => NormalizeDb(GetMetrics(RmsMeasurementMode).Rms100MsDb, -60, 0);

    public double CorrelationLevel => _correlation is null
        ? 0.5
        : Clamp01((_correlation.Value + 1) / 2);

    public double StereoImageX => _stereoImageX ?? double.NaN;

    public double StereoImageY => _stereoImageY ?? double.NaN;

    public IReadOnlyList<StereoImagePoint> StereoImagePoints => _stereoImagePoints;

    public double CorrelationValue => _correlation ?? double.NaN;

    public IReadOnlyList<double> WaveformSamples => _waveformSamples;

    public long WaveformPlotTick => _waveformPlotTick;

    public IReadOnlyList<double> LeftWaveformSamples => _leftWaveformSamples;

    public IReadOnlyList<double> RightWaveformSamples => _rightWaveformSamples;

    public IReadOnlyList<double> SpectrumBuckets => _spectrumBuckets;

    public long SpectrumPlotTick => _spectrumPlotTick;

    public long LoudnessClockPlotTick => _loudnessClockPlotTick;

    public IReadOnlyList<double> LeftSpectrumBuckets => _leftSpectrumBuckets;

    public IReadOnlyList<double> RightSpectrumBuckets => _rightSpectrumBuckets;

    public bool IsOverallSpectrumVisible => SpectrumDisplayMode == SpectrumDisplayMode.Overall;

    public bool IsChannelSpectrumVisible => SpectrumDisplayMode == SpectrumDisplayMode.Channels;

    public bool IsOverallWaveformVisible => WaveformDisplayMode == WaveformDisplayMode.Overall;

    public bool IsChannelWaveformVisible => WaveformDisplayMode == WaveformDisplayMode.Channels;

    public bool IsInstanceMissing
    {
        get => _isInstanceMissing;
        private set => SetProperty(ref _isInstanceMissing, value);
    }

    public bool IsHidden
    {
        get => _isHidden;
        private set
        {
            if (SetProperty(ref _isHidden, value))
            {
                (HideSourceCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RestoreSourceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int? OrderIndex
    {
        get => _orderIndex;
        private set => SetProperty(ref _orderIndex, value);
    }

    public bool IsAliasEditing
    {
        get => _isAliasEditing;
        private set => SetProperty(ref _isAliasEditing, value);
    }

    public string AliasEditText
    {
        get => _aliasEditText;
        set => SetProperty(ref _aliasEditText, value ?? string.Empty);
    }

    public bool IsReorderDropBefore
    {
        get => _isReorderDropBefore;
        private set => SetProperty(ref _isReorderDropBefore, value);
    }

    public bool IsReorderDropAfter
    {
        get => _isReorderDropAfter;
        private set => SetProperty(ref _isReorderDropAfter, value);
    }

    public event Action<AudioSessionMeterViewModel>? ResetRequested;

    public void Update(SignalSourceSnapshot snapshot)
    {
        SourceKey = snapshot.SourceKey;
        SourceKind = snapshot.SourceKind;
        ProcessId = snapshot.ProcessId;
        IsInstanceMissing = false;
        _hasLivePeak = true;
        _peakDb = snapshot.PeakDb;
        DisplayName = snapshot.DisplayName;
        PeakDbText = snapshot.PeakDbText;
        DisplayLevel = SmoothLevel(snapshot.Peak);
        State = snapshot.State;
        ApplyAnalyzerMetrics(snapshot);
        RefreshPrimaryValueText();
    }

    public void ApplyDisplayMode(MeterDisplayMode displayMode)
    {
        DisplayMode = displayMode;
        RefreshPrimaryValueText();
    }

    public void RequestReset()
    {
        Reset();
        ResetRequested?.Invoke(this);
    }

    public void Reset()
    {
        ResetCount++;
        ClearAnalyzerMetrics();
    }

    public void ApplyWorkspaceState(bool isHidden, int? orderIndex)
    {
        IsHidden = isHidden;
        OrderIndex = orderIndex is int value && value >= 0 ? value : null;
    }

    public void SetReorderDropHint(bool before, bool after)
    {
        if (before && after)
        {
            after = false;
        }

        IsReorderDropBefore = before;
        IsReorderDropAfter = after;
    }

    public void StartAliasEdit()
    {
        AliasEditText = Alias ?? DisplayName;
        IsAliasEditing = true;
    }

    public void CommitAliasEdit()
    {
        Alias = AliasEditText;
        IsAliasEditing = false;
    }

    public void CancelAliasEdit()
    {
        AliasEditText = Alias ?? DisplayName;
        IsAliasEditing = false;
    }

    public void MarkAudioHandleMissing()
    {
        PeakDbText = "handle n/a";
        _hasLivePeak = false;
        DisplayLevel = SmoothLevel(0);
        State = "Source handle missing";
        RefreshPrimaryValueText();
    }

    public void MarkInstanceNotFound()
    {
        IsInstanceMissing = true;
        PeakDbText = "instance n/a";
        _hasLivePeak = false;
        DisplayLevel = SmoothLevel(0);
        State = "Instance not found";

        if (HasAnalyzerMetrics)
        {
            ClearAnalyzerMetrics();
        }
        else
        {
            RefreshPrimaryValueText();
        }
    }

    private float SmoothLevel(float nextPeak)
    {
        var smoothing = nextPeak >= DisplayLevel ? 0.42f : 0.18f;
        var smoothed = DisplayLevel + ((nextPeak - DisplayLevel) * smoothing);
        if (Math.Abs(nextPeak - smoothed) < 0.01f)
        {
            return nextPeak;
        }

        return Math.Clamp(smoothed, 0, 1);
    }

    private void ApplyAnalyzerMetrics(SignalSourceSnapshot snapshot)
    {
        var momentaryChanged = SetMetric(ref _momentaryLufs, snapshot.MomentaryLufs);
        var shortTermChanged = SetMetric(ref _shortTermLufs, snapshot.ShortTermLufs);
        var integratedChanged = SetMetric(ref _integratedLufs, snapshot.IntegratedLufs);
        var truePeakChanged = SetMetric(ref _truePeakDb, snapshot.TruePeakDb);
        var truePeakMaxChanged = UpdateTruePeakMax(snapshot.TruePeakDb);
        var rmsChanged = SetMetric(ref _rms100MsDb, snapshot.Rms100MsDb);
        var midMetricsChanged = SetMetric(ref _midMetrics, snapshot.MidMetrics);
        var sideMetricsChanged = SetMetric(ref _sideMetrics, snapshot.SideMetrics);
        var midTruePeakMaxChanged = UpdateTruePeakMax(ref _midTruePeakMaxDb, snapshot.MidMetrics?.TruePeakDb);
        var sideTruePeakMaxChanged = UpdateTruePeakMax(ref _sideTruePeakMaxDb, snapshot.SideMetrics?.TruePeakDb);
        var stereoXChanged = SetMetric(ref _stereoImageX, snapshot.StereoImageX);
        var stereoYChanged = SetMetric(ref _stereoImageY, snapshot.StereoImageY);
        var stereoPointsChanged = SetMetric(ref _stereoImagePoints, snapshot.StereoImagePoints ?? EmptyStereoImagePoints);
        var correlationChanged = SetMetric(ref _correlation, snapshot.Correlation);
        var hasWaveformFrame = HasSamples(snapshot.WaveformSamples);
        var hasSpectrumFrame = HasSamples(snapshot.SpectrumBuckets);
        var waveformChanged = SetMetric(ref _waveformSamples, GetSamplesOrExisting(snapshot.WaveformSamples, _waveformSamples));
        var leftWaveformChanged = SetMetric(ref _leftWaveformSamples, GetSamplesOrExisting(GetChannelWaveform(snapshot, 0), _leftWaveformSamples));
        var rightWaveformChanged = SetMetric(ref _rightWaveformSamples, GetSamplesOrExisting(GetChannelWaveform(snapshot, 1), _rightWaveformSamples));
        var spectrumChanged = SetMetric(ref _spectrumBuckets, GetSamplesOrExisting(snapshot.SpectrumBuckets, _spectrumBuckets));
        var leftSpectrumChanged = SetMetric(ref _leftSpectrumBuckets, GetSamplesOrExisting(GetChannelSpectrum(snapshot, 0), _leftSpectrumBuckets));
        var rightSpectrumChanged = SetMetric(ref _rightSpectrumBuckets, GetSamplesOrExisting(GetChannelSpectrum(snapshot, 1), _rightSpectrumBuckets));

        if (momentaryChanged)
        {
            OnPropertyChanged(nameof(MomentaryLufsText));
            OnPropertyChanged(nameof(MomentaryLufsLevel));
            OnPropertyChanged(nameof(LoudnessClockMomentaryDb));
        }

        if (snapshot.MomentaryLufs is double momentaryLufs
            && double.IsFinite(momentaryLufs)
            && IsFreshPlotFrame(snapshot.PlotFrameVersion, _lastLoudnessClockPlotFrameVersion))
        {
            _lastLoudnessClockPlotFrameVersion = snapshot.PlotFrameVersion;
            _loudnessClockPlotTick++;
            OnPropertyChanged(nameof(LoudnessClockPlotTick));
        }

        if (shortTermChanged)
        {
            OnPropertyChanged(nameof(ShortTermLufsText));
            OnPropertyChanged(nameof(ShortTermLufsLevel));
        }

        if (integratedChanged)
        {
            OnPropertyChanged(nameof(IntegratedLufsText));
            OnPropertyChanged(nameof(IntegratedLufsLevel));
        }

        if (truePeakChanged || truePeakMaxChanged)
        {
            OnPropertyChanged(nameof(TruePeakText));
            OnPropertyChanged(nameof(TruePeakMaxText));
            OnPropertyChanged(nameof(TruePeakLevel));
            OnPropertyChanged(nameof(TruePeakReadoutSeverity));
        }

        if (rmsChanged)
        {
            OnPropertyChanged(nameof(Rms100MsText));
            OnPropertyChanged(nameof(Rms100MsLevel));
        }

        if (momentaryChanged || midMetricsChanged || sideMetricsChanged)
        {
            OnPropertyChanged(nameof(DetailMomentaryLufsText));
            OnPropertyChanged(nameof(DetailMomentaryLufsLevel));
        }

        if (shortTermChanged || midMetricsChanged || sideMetricsChanged)
        {
            OnPropertyChanged(nameof(DetailShortTermLufsText));
            OnPropertyChanged(nameof(DetailShortTermLufsLevel));
        }

        if (integratedChanged || midMetricsChanged || sideMetricsChanged)
        {
            OnPropertyChanged(nameof(DetailIntegratedLufsText));
            OnPropertyChanged(nameof(DetailIntegratedLufsLevel));
        }

        if (truePeakChanged || truePeakMaxChanged || midMetricsChanged || sideMetricsChanged
            || midTruePeakMaxChanged || sideTruePeakMaxChanged)
        {
            NotifyDetailTruePeakChanged();
        }

        if (rmsChanged || midMetricsChanged || sideMetricsChanged)
        {
            OnPropertyChanged(nameof(DetailRms100MsText));
            OnPropertyChanged(nameof(DetailRms100MsLevel));
        }

        if (stereoXChanged || stereoYChanged)
        {
            OnPropertyChanged(nameof(StereoImageText));
        }

        if (stereoXChanged)
        {
            OnPropertyChanged(nameof(StereoImageX));
        }

        if (stereoYChanged)
        {
            OnPropertyChanged(nameof(StereoImageY));
        }

        if (stereoPointsChanged)
        {
            OnPropertyChanged(nameof(StereoImagePoints));
        }

        if (correlationChanged)
        {
            OnPropertyChanged(nameof(CorrelationText));
            OnPropertyChanged(nameof(CorrelationLevel));
            OnPropertyChanged(nameof(CorrelationValue));
        }

        if (hasWaveformFrame
            && (waveformChanged || leftWaveformChanged || rightWaveformChanged)
            && IsFreshPlotFrame(snapshot.PlotFrameVersion, _lastWaveformPlotFrameVersion))
        {
            _lastWaveformPlotFrameVersion = snapshot.PlotFrameVersion;
            _waveformPlotTick++;
            OnPropertyChanged(nameof(WaveformPlotTick));
        }

        if (waveformChanged)
        {
            OnPropertyChanged(nameof(WaveformSamples));
        }

        if (leftWaveformChanged)
        {
            OnPropertyChanged(nameof(LeftWaveformSamples));
        }

        if (rightWaveformChanged)
        {
            OnPropertyChanged(nameof(RightWaveformSamples));
        }

        if (hasSpectrumFrame && IsFreshPlotFrame(snapshot.PlotFrameVersion, _lastSpectrumPlotFrameVersion))
        {
            _lastSpectrumPlotFrameVersion = snapshot.PlotFrameVersion;
            _spectrumPlotTick++;
            OnPropertyChanged(nameof(SpectrumPlotTick));
        }

        if (spectrumChanged)
        {
            OnPropertyChanged(nameof(SpectrumBuckets));
        }

        if (leftSpectrumChanged)
        {
            OnPropertyChanged(nameof(LeftSpectrumBuckets));
        }

        if (rightSpectrumChanged)
        {
            OnPropertyChanged(nameof(RightSpectrumBuckets));
        }
    }

    private void ClearAnalyzerMetrics()
    {
        _momentaryLufs = null;
        _shortTermLufs = null;
        _integratedLufs = null;
        _truePeakDb = null;
        _truePeakMaxDb = null;
        _rms100MsDb = null;
        _midMetrics = null;
        _sideMetrics = null;
        _midTruePeakMaxDb = null;
        _sideTruePeakMaxDb = null;
        _stereoImageX = null;
        _stereoImageY = null;
        _stereoImagePoints = EmptyStereoImagePoints;
        _correlation = null;
        _waveformSamples = EmptyDoubleSamples;
        _leftWaveformSamples = EmptyDoubleSamples;
        _rightWaveformSamples = EmptyDoubleSamples;
        _spectrumBuckets = EmptyDoubleSamples;
        _leftSpectrumBuckets = EmptyDoubleSamples;
        _rightSpectrumBuckets = EmptyDoubleSamples;
        _lastWaveformPlotFrameVersion = -1;
        _lastSpectrumPlotFrameVersion = -1;
        _lastLoudnessClockPlotFrameVersion = -1;
        _waveformPlotTick++;
        _spectrumPlotTick++;
        _loudnessClockPlotTick++;
        RefreshPrimaryValueText();

        OnPropertyChanged(nameof(MomentaryLufsText));
        OnPropertyChanged(nameof(LoudnessClockMomentaryDb));
        OnPropertyChanged(nameof(ShortTermLufsText));
        OnPropertyChanged(nameof(IntegratedLufsText));
        OnPropertyChanged(nameof(TruePeakText));
        OnPropertyChanged(nameof(TruePeakMaxText));
        OnPropertyChanged(nameof(TruePeakReadoutSeverity));
        OnPropertyChanged(nameof(Rms100MsText));
        OnPropertyChanged(nameof(StereoImageText));
        OnPropertyChanged(nameof(CorrelationText));
        OnPropertyChanged(nameof(MomentaryLufsLevel));
        OnPropertyChanged(nameof(ShortTermLufsLevel));
        OnPropertyChanged(nameof(IntegratedLufsLevel));
        OnPropertyChanged(nameof(TruePeakLevel));
        OnPropertyChanged(nameof(Rms100MsLevel));
        OnPropertyChanged(nameof(DetailMomentaryLufsText));
        OnPropertyChanged(nameof(DetailMomentaryLufsLevel));
        OnPropertyChanged(nameof(DetailShortTermLufsText));
        OnPropertyChanged(nameof(DetailShortTermLufsLevel));
        OnPropertyChanged(nameof(DetailIntegratedLufsText));
        OnPropertyChanged(nameof(DetailIntegratedLufsLevel));
        NotifyDetailTruePeakChanged();
        OnPropertyChanged(nameof(DetailRms100MsText));
        OnPropertyChanged(nameof(DetailRms100MsLevel));
        OnPropertyChanged(nameof(CorrelationLevel));
        OnPropertyChanged(nameof(StereoImageX));
        OnPropertyChanged(nameof(StereoImageY));
        OnPropertyChanged(nameof(StereoImagePoints));
        OnPropertyChanged(nameof(CorrelationValue));
        OnPropertyChanged(nameof(WaveformSamples));
        OnPropertyChanged(nameof(LeftWaveformSamples));
        OnPropertyChanged(nameof(RightWaveformSamples));
        OnPropertyChanged(nameof(WaveformPlotTick));
        OnPropertyChanged(nameof(SpectrumBuckets));
        OnPropertyChanged(nameof(LeftSpectrumBuckets));
        OnPropertyChanged(nameof(RightSpectrumBuckets));
        OnPropertyChanged(nameof(SpectrumPlotTick));
        OnPropertyChanged(nameof(LoudnessClockPlotTick));
    }

    private void RefreshPrimaryValueText()
    {
        PrimaryValueText = DisplayMode switch
        {
            MeterDisplayMode.Lufs => FormatLufs(_momentaryLufs),
            _ => PeakDbText
        };
        PrimaryValueSeverity = CalculatePrimaryValueSeverity();
    }

    private ReadoutSeverity CalculatePrimaryValueSeverity()
    {
        if (DisplayMode != MeterDisplayMode.Peak || !_hasLivePeak || IsInstanceMissing)
        {
            return _hasLivePeak && !IsInstanceMissing
                ? ReadoutSeverity.Normal
                : ReadoutSeverity.Muted;
        }

        return CalculateDbReadoutSeverity(_peakDb, _hasLivePeak);
    }

    private static string FormatLufs(double? value)
    {
        return value is null ? "LUFS n/a" : $"{value.Value:0.0} LUFS";
    }

    private static string FormatDb(double? value)
    {
        return value is null ? "n/a" : $"{value.Value:0.0} dB";
    }

    private static string FormatCompactDb(double? value)
    {
        if (value is null)
        {
            return "n/a";
        }

        var sign = value.Value > 0 ? "+" : string.Empty;
        return $"{sign}{value.Value:0.0}dB";
    }

    private static string FormatDetailLabel(string label, SignalMeasurementMode mode)
    {
        var marker = mode switch
        {
            SignalMeasurementMode.Mid => "M",
            SignalMeasurementMode.Side => "S",
            _ => "I"
        };
        return $"{label} ({marker})";
    }

    private static double NormalizeLufs(double? value)
    {
        return NormalizeDb(value, -60, 0);
    }

    private static double NormalizeDb(double? value, double floor, double ceiling)
    {
        if (value is null)
        {
            return 0;
        }

        return Clamp01((value.Value - floor) / (ceiling - floor));
    }

    private static double Clamp01(double value)
    {
        return Math.Min(1, Math.Max(0, value));
    }

    private static bool SetMetric<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        return true;
    }

    private static bool IsFreshPlotFrame(long plotFrameVersion, long lastPlotFrameVersion)
    {
        return plotFrameVersion != lastPlotFrameVersion;
    }

    private static IReadOnlyList<double> GetSamplesOrExisting(
        IReadOnlyList<double>? samples,
        IReadOnlyList<double> existingSamples)
    {
        return samples is { Count: > 0 } ? samples : existingSamples;
    }

    private static bool HasSamples(IReadOnlyList<double>? samples)
    {
        return samples is { Count: > 0 };
    }

    private bool UpdateTruePeakMax(double? value)
    {
        return UpdateTruePeakMax(ref _truePeakMaxDb, value);
    }

    private static bool UpdateTruePeakMax(ref double? maximum, double? value)
    {
        if (value is null)
        {
            return false;
        }

        if (maximum is not null && value.Value <= maximum.Value)
        {
            return false;
        }

        maximum = value.Value;
        return true;
    }

    private SignalLevelMetrics GetMetrics(SignalMeasurementMode mode)
    {
        return mode switch
        {
            SignalMeasurementMode.Mid => _midMetrics ?? new SignalLevelMetrics(),
            SignalMeasurementMode.Side => _sideMetrics ?? new SignalLevelMetrics(),
            _ => new SignalLevelMetrics(_momentaryLufs, _shortTermLufs, _integratedLufs, _truePeakDb, _rms100MsDb)
        };
    }

    private double? GetTruePeakMax(SignalMeasurementMode mode)
    {
        return mode switch
        {
            SignalMeasurementMode.Mid => _midTruePeakMaxDb,
            SignalMeasurementMode.Side => _sideTruePeakMaxDb,
            _ => _truePeakMaxDb
        };
    }

    private double? GetDetailTruePeakSeverityValue()
    {
        var current = GetMetrics(TruePeakMeasurementMode).TruePeakDb;
        var maximum = GetTruePeakMax(TruePeakMeasurementMode);
        return current is null ? maximum : maximum is null ? current : Math.Max(current.Value, maximum.Value);
    }

    private void NotifyDetailTruePeakChanged()
    {
        OnPropertyChanged(nameof(DetailTruePeakText));
        OnPropertyChanged(nameof(DetailTruePeakMaxText));
        OnPropertyChanged(nameof(DetailTruePeakLevel));
        OnPropertyChanged(nameof(DetailTruePeakReadoutSeverity));
    }

    private double? GetTruePeakSeverityValue()
    {
        if (_truePeakDb is null)
        {
            return _truePeakMaxDb;
        }

        if (_truePeakMaxDb is null)
        {
            return _truePeakDb;
        }

        return Math.Max(_truePeakDb.Value, _truePeakMaxDb.Value);
    }

    private static ReadoutSeverity CalculateDbReadoutSeverity(double? value, bool hasValue)
    {
        if (!hasValue || value is null)
        {
            return ReadoutSeverity.Muted;
        }

        if (value.Value > 0)
        {
            return ReadoutSeverity.Over;
        }

        if (value.Value >= -1)
        {
            return ReadoutSeverity.Critical;
        }

        if (value.Value >= -6)
        {
            return ReadoutSeverity.Hot;
        }

        if (value.Value >= -12)
        {
            return ReadoutSeverity.Warm;
        }

        return ReadoutSeverity.Normal;
    }

    private static IReadOnlyList<double> GetChannelSpectrum(SignalSourceSnapshot snapshot, int channel)
    {
        return snapshot.ChannelSpectrumBuckets is { Count: > 0 } channels && channel < channels.Count
            ? channels[channel]
            : EmptyDoubleSamples;
    }

    private static IReadOnlyList<double> GetChannelWaveform(SignalSourceSnapshot snapshot, int channel)
    {
        return snapshot.ChannelWaveformSamples is { Count: > 0 } channels && channel < channels.Count
            ? channels[channel]
            : EmptyDoubleSamples;
    }

    private bool HasAnalyzerMetrics =>
        _momentaryLufs is not null
        || _shortTermLufs is not null
        || _integratedLufs is not null
        || _truePeakDb is not null
        || _rms100MsDb is not null
        || _stereoImageX is not null
        || _stereoImageY is not null
        || _correlation is not null
        || _stereoImagePoints.Count > 0
        || _waveformSamples.Count > 0
        || _leftWaveformSamples.Count > 0
        || _rightWaveformSamples.Count > 0
        || _spectrumBuckets.Count > 0
        || _leftSpectrumBuckets.Count > 0
        || _rightSpectrumBuckets.Count > 0;

    public void SetDisplayNameCollision(bool hasCollision)
    {
        if (_hasDisplayNameCollision == hasCollision)
        {
            return;
        }

        _hasDisplayNameCollision = hasCollision;
        OnPropertyChanged(nameof(DisplayTitle));
    }

    private string SourceQualifier
    {
        get
        {
            if (SourceKind == SignalSourceKind.WasapiSession && ProcessId is int processId)
            {
                return $"{SourceKindLabel} PID {processId}";
            }

            var stableId = CompactSourceIdentifier(SourceKey);
            return string.IsNullOrWhiteSpace(stableId)
                ? SourceKindLabel
                : $"{SourceKindLabel} {stableId}";
        }
    }

    private static string CompactSourceIdentifier(string sourceKey)
    {
        var separatorIndex = sourceKey.IndexOf(':', StringComparison.Ordinal);
        var value = separatorIndex >= 0 && separatorIndex < sourceKey.Length - 1
            ? sourceKey[(separatorIndex + 1)..]
            : sourceKey;

        return value.Length <= 24 ? value : $"{value[..21]}...";
    }
}
