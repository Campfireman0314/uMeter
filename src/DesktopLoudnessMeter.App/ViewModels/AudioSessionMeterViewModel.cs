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
    private long _lastWaveformPlotFrameVersion;
    private long _lastSpectrumPlotFrameVersion;
    private int _resetCount;
    private TruePeakOversampling _truePeakOversampling = TruePeakOversampling.FourX;
    private WaveformDisplayMode _waveformDisplayMode = WaveformDisplayMode.Overall;
    private SpectrumDisplayMode _spectrumDisplayMode = SpectrumDisplayMode.Overall;
    private bool _isInstanceMissing;

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
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string DisplayTitle => _hasDisplayNameCollision
        ? $"{DisplayName} - {SourceQualifier}"
        : DisplayName;

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

    public event Action<AudioSessionMeterViewModel>? DetailRequested;

    public event Action<AudioSessionMeterViewModel>? PopupRequested;

    public event Action<AudioSessionMeterViewModel, TruePeakOversampling>? TruePeakOversamplingRequested;

    public event Action<AudioSessionMeterViewModel, WaveformDisplayMode>? WaveformDisplayModeRequested;

    public event Action<AudioSessionMeterViewModel, SpectrumDisplayMode>? SpectrumDisplayModeRequested;

    public string MomentaryLufsText => FormatLufs(_momentaryLufs);

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

    public void MarkAudioHandleMissing()
    {
        PeakDbText = "handle n/a";
        _hasLivePeak = false;
        DisplayLevel = SmoothLevel(0);
        State = "Source handle missing";

        if (HasAnalyzerMetrics)
        {
            ClearAnalyzerMetrics();
        }
        else
        {
            RefreshPrimaryValueText();
        }
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
        var stereoXChanged = SetMetric(ref _stereoImageX, snapshot.StereoImageX);
        var stereoYChanged = SetMetric(ref _stereoImageY, snapshot.StereoImageY);
        var stereoPointsChanged = SetMetric(ref _stereoImagePoints, snapshot.StereoImagePoints ?? EmptyStereoImagePoints);
        var correlationChanged = SetMetric(ref _correlation, snapshot.Correlation);
        var waveformChanged = SetMetric(ref _waveformSamples, snapshot.WaveformSamples ?? EmptyDoubleSamples);
        var leftWaveformChanged = SetMetric(ref _leftWaveformSamples, GetChannelWaveform(snapshot, 0));
        var rightWaveformChanged = SetMetric(ref _rightWaveformSamples, GetChannelWaveform(snapshot, 1));
        var spectrumChanged = SetMetric(ref _spectrumBuckets, snapshot.SpectrumBuckets ?? EmptyDoubleSamples);
        var leftSpectrumChanged = SetMetric(ref _leftSpectrumBuckets, GetChannelSpectrum(snapshot, 0));
        var rightSpectrumChanged = SetMetric(ref _rightSpectrumBuckets, GetChannelSpectrum(snapshot, 1));

        if (momentaryChanged)
        {
            OnPropertyChanged(nameof(MomentaryLufsText));
            OnPropertyChanged(nameof(MomentaryLufsLevel));
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

        if (_waveformSamples.Count > 0 && snapshot.PlotFrameVersion > _lastWaveformPlotFrameVersion)
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

        if (_spectrumBuckets.Count > 0 && snapshot.PlotFrameVersion > _lastSpectrumPlotFrameVersion)
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
        RefreshPrimaryValueText();

        OnPropertyChanged(nameof(MomentaryLufsText));
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
        OnPropertyChanged(nameof(CorrelationLevel));
        OnPropertyChanged(nameof(StereoImageX));
        OnPropertyChanged(nameof(StereoImageY));
        OnPropertyChanged(nameof(StereoImagePoints));
        OnPropertyChanged(nameof(CorrelationValue));
        OnPropertyChanged(nameof(WaveformSamples));
        OnPropertyChanged(nameof(LeftWaveformSamples));
        OnPropertyChanged(nameof(RightWaveformSamples));
        OnPropertyChanged(nameof(SpectrumBuckets));
        OnPropertyChanged(nameof(LeftSpectrumBuckets));
        OnPropertyChanged(nameof(RightSpectrumBuckets));
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

    private bool UpdateTruePeakMax(double? value)
    {
        if (value is null)
        {
            return false;
        }

        if (_truePeakMaxDb is not null && value.Value <= _truePeakMaxDb.Value)
        {
            return false;
        }

        _truePeakMaxDb = value.Value;
        return true;
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
