using System.ComponentModel;
using System.Windows.Input;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class DetailWindowViewModel : ObservableObject
{
    private const double RollTrackMinHeight = 185;
    private const double RollGroupPadding = 20;
    private const double RollChannelLabelAllowance = 40;
    private const double RollSplitterHeight = 8;
    private const double RollSplitterFlexAllowance = 128;
    private readonly Func<IReadOnlyList<AudioSessionMeterViewModel>> _getActiveSessions;
    private readonly RelayCommand _switchSessionCommand;
    private AudioSessionMeterViewModel _session;
    private IReadOnlyList<AudioSessionMeterViewModel> _switchTargets = Array.Empty<AudioSessionMeterViewModel>();
    private bool _isAlwaysOnTop = true;
    private bool _isWaveformVisible;
    private bool _isSpectrumVisible;

    public DetailWindowViewModel(
        AudioSessionMeterViewModel session,
        Func<IReadOnlyList<AudioSessionMeterViewModel>>? getActiveSessions = null)
    {
        _session = session;
        _session.PropertyChanged += SessionOnPropertyChanged;
        _getActiveSessions = getActiveSessions ?? (() => Array.Empty<AudioSessionMeterViewModel>());
        StereoSettings = new StereoImageDisplaySettings();
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        ResetCommand = new RelayCommand(_ => Session.RequestReset());
        _switchSessionCommand = new RelayCommand(SwitchSession, CanSwitchSession);
        RefreshSwitchTargets();
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
            NotifyRollLayoutChanged();
            RefreshSwitchTargets();
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
            }
        }
    }

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
    }

    private void SessionOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AudioSessionMeterViewModel.IsChannelWaveformVisible)
            or nameof(AudioSessionMeterViewModel.IsChannelSpectrumVisible))
        {
            NotifyRollLayoutChanged();
        }
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
}
