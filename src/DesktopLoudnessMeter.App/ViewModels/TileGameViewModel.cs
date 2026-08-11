using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopLoudnessMeter.App.Game;
using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class TileGameViewModel : ObservableObject, IDisposable
{
    private const double TileSize = 48;
    private const double TileStep = 54;
    private const double BoardOffsetX = 18;
    private const double BoardOffsetY = 25;
    private const double LayerRise = 2;
    private readonly MainViewModel _mainViewModel;
    private readonly DispatcherTimer _danceTimer;
    private readonly AudioDanceController _danceController = new();
    private readonly AdaptiveBeatDetector _beatDetector = new();
    private readonly RhythmInteractionController _rhythmController = new();
    private readonly RelayCommand _selectTileCommand;
    private readonly RelayCommand _useCannonCommand;
    private readonly RelayCommand _useFanCommand;
    private readonly RelayCommand _useDiggerCommand;
    private readonly Stopwatch _danceClock = Stopwatch.StartNew();
    private LayeredTileGame _game;
    private AudioSessionMeterViewModel? _selectedAudioSource;
    private int _seed;
    private int _matchCount;
    private long _lastDanceTimestamp;
    private SignalTileKind? _pendingRhythmEchoKind;
    private double _lowRhythmAffinity;
    private double _midRhythmAffinity;
    private double _highRhythmAffinity;
    private double _groove;
    private int _rhythmPhraseStep;
    private double _rhythmTempoBpm;
    private double _rhythmTempoConfidence;
    private double _rhythmBeatPhase;
    private RhythmTimingGrade _rhythmTimingGrade;
    private RhythmBand _activeRhythmBand;
    private double _danceEnergy;
    private double _sheepBounce;
    private double _sheepLean;
    private double _sheepScaleX = 1;
    private double _sheepScaleY = 1;
    private double _sheepTorsoTwist;
    private double _sheepHeadAngle;
    private double _sheepHeadBob;
    private double _sheepEyeScaleY = 1;
    private double _sheepMouthScaleY = 0.22;
    private double _sheepLeftForelegAngle;
    private double _sheepRightForelegAngle;
    private double _sheepLeftElbowAngle;
    private double _sheepRightElbowAngle;
    private double _sheepLeftForehoofAngle;
    private double _sheepRightForehoofAngle;
    private double _sheepLeftHindLegAngle;
    private double _sheepRightHindLegAngle;
    private double _sheepLeftKneeAngle;
    private double _sheepRightKneeAngle;
    private double _sheepLeftHindHoofAngle;
    private double _sheepRightHindHoofAngle;
    private double _sheepLeftEarAngle;
    private double _sheepRightEarAngle;
    private double _sheepLeftEarTipAngle;
    private double _sheepRightEarTipAngle;
    private double _sheepTailAngle;
    private double _sheepTailTipAngle;
    private double _sheepLeftWoolFringeAngle;
    private double _sheepRightWoolFringeAngle;
    private double _bassEnergy;
    private double _signalEnergy;
    private double _spectralFlux;
    private double _crossBandAgreement;
    private double _beatPeriodicity;
    private double _beatBaseline;
    private double _beatThreshold;
    private double _onsetStrength;
    private double _beatPulse;
    private double _beatSensitivity = 0.55;
    private bool _isBeatDebugVisible;
    private bool _isBeatDetected;
    private bool _isDisposed;

    public TileGameViewModel(MainViewModel mainViewModel, int? seed = null, bool startDanceTimer = true)
    {
        _mainViewModel = mainViewModel;
        _seed = seed ?? Random.Shared.Next();
        _game = LayeredTileGame.Create(_seed);
        _selectTileCommand = new RelayCommand(SelectTile, CanSelectTile);
        _useCannonCommand = new RelayCommand(_ => UseCannon(), _ => _game.CanUseCannon);
        _useFanCommand = new RelayCommand(_ => UseFan(), _ => _game.CanUseFan);
        _useDiggerCommand = new RelayCommand(_ => UseDigger(), _ => _game.CanUseDigger);
        SelectTileCommand = _selectTileCommand;
        UseCannonCommand = _useCannonCommand;
        UseFanCommand = _useFanCommand;
        UseDiggerCommand = _useDiggerCommand;
        RestartCommand = new RelayCommand(_ => Restart());
        NewGameCommand = new RelayCommand(_ => NewGame());
        AudioSources = mainViewModel.Sessions;
        AudioSources.CollectionChanged += AudioSourcesOnCollectionChanged;
        LocalizationManager.LanguageChanged += LocalizationManagerOnLanguageChanged;
        SelectInitialAudioSource();

        _danceTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _danceTimer.Tick += DanceTimerOnTick;
        if (startDanceTimer)
        {
            _danceTimer.Start();
        }

        RefreshGameSurface();
    }

    public ObservableCollection<GameTileViewModel> BoardTiles { get; } = [];

    public ObservableCollection<TraySlotViewModel> TraySlots { get; } = [];

    public ObservableCollection<AudioSessionMeterViewModel> AudioSources { get; }

    public ICommand SelectTileCommand { get; }

    public ICommand UseCannonCommand { get; }

    public ICommand UseFanCommand { get; }

    public ICommand UseDiggerCommand { get; }

    public ICommand RestartCommand { get; }

    public ICommand NewGameCommand { get; }

    public event Action<TileVisualEffectRequest>? VisualEffectRequested;

    public event Action<GameRoundPresentation>? RoundPresentationRequested;

    public event Action<GamePowerUpKind>? PowerUpPresentationRequested;

    internal int Seed => _seed;

    public AudioSessionMeterViewModel? SelectedAudioSource
    {
        get => _selectedAudioSource;
        set
        {
            if (SetProperty(ref _selectedAudioSource, value))
            {
                _danceController.Reset();
                _beatDetector.Reset();
                ResetRhythmInteraction();
                OnPropertyChanged(nameof(HasAudioSource));
            }
        }
    }

    public bool HasAudioSource => SelectedAudioSource is not null;

    public string GameStatusText => _game.State switch
    {
        TileGameState.Won => LocalizationManager.Translate("Board cleared!"),
        TileGameState.Lost => LocalizationManager.Translate("Tray full"),
        _ => LocalizationManager.Translate("Playing")
    };

    public string RemainingText => LocalizationManager.Format("{0} tiles", _game.RemainingTileCount);

    public string MatchesText => LocalizationManager.Format("{0} matches", _matchCount);

    public bool IsGameOver => _game.State != TileGameState.Playing;

    public bool IsCannonAvailable => _game.CannonAvailable;

    public bool IsFanAvailable => _game.FanAvailable;

    public bool IsDiggerAvailable => _game.DiggerAvailable;

    public double Groove
    {
        get => _groove;
        private set => SetProperty(ref _groove, value);
    }

    public int RhythmPhraseStep
    {
        get => _rhythmPhraseStep;
        private set => SetProperty(ref _rhythmPhraseStep, value);
    }

    public string RhythmTimingText => _rhythmTimingGrade switch
    {
        RhythmTimingGrade.Perfect => LocalizationManager.Translate("Perfect"),
        RhythmTimingGrade.Good => LocalizationManager.Translate("On beat"),
        _ => string.Empty
    };

    public bool IsRhythmTimingVisible => _rhythmTimingGrade != RhythmTimingGrade.None;

    public string RhythmTempoText => _rhythmTempoConfidence >= 0.72
        ? $"{_rhythmTempoBpm:0} BPM"
        : "-- BPM";

    public bool IsTempoConfident => _rhythmTempoConfidence >= 0.72;

    public double RhythmBeatPhase
    {
        get => _rhythmBeatPhase;
        private set => SetProperty(ref _rhythmBeatPhase, value);
    }

    public RhythmBand ActiveRhythmBand
    {
        get => _activeRhythmBand;
        private set => SetProperty(ref _activeRhythmBand, value);
    }

    public double LowRhythmAffinity
    {
        get => _lowRhythmAffinity;
        private set => SetProperty(ref _lowRhythmAffinity, value);
    }

    public double MidRhythmAffinity
    {
        get => _midRhythmAffinity;
        private set => SetProperty(ref _midRhythmAffinity, value);
    }

    public double HighRhythmAffinity
    {
        get => _highRhythmAffinity;
        private set => SetProperty(ref _highRhythmAffinity, value);
    }

    public double DanceEnergy
    {
        get => _danceEnergy;
        private set => SetProperty(ref _danceEnergy, value);
    }

    public double SheepBounce
    {
        get => _sheepBounce;
        private set => SetProperty(ref _sheepBounce, value);
    }

    public double SheepLean
    {
        get => _sheepLean;
        private set => SetProperty(ref _sheepLean, value);
    }

    public double SheepScaleX
    {
        get => _sheepScaleX;
        private set => SetProperty(ref _sheepScaleX, value);
    }

    public double SheepScaleY
    {
        get => _sheepScaleY;
        private set => SetProperty(ref _sheepScaleY, value);
    }

    public double SheepTorsoTwist
    {
        get => _sheepTorsoTwist;
        private set => SetProperty(ref _sheepTorsoTwist, value);
    }

    public double SheepHeadAngle
    {
        get => _sheepHeadAngle;
        private set => SetProperty(ref _sheepHeadAngle, value);
    }

    public double SheepHeadBob
    {
        get => _sheepHeadBob;
        private set => SetProperty(ref _sheepHeadBob, value);
    }

    public double SheepEyeScaleY
    {
        get => _sheepEyeScaleY;
        private set => SetProperty(ref _sheepEyeScaleY, value);
    }

    public double SheepMouthScaleY
    {
        get => _sheepMouthScaleY;
        private set => SetProperty(ref _sheepMouthScaleY, value);
    }

    public double SheepLeftForelegAngle
    {
        get => _sheepLeftForelegAngle;
        private set => SetProperty(ref _sheepLeftForelegAngle, value);
    }

    public double SheepRightForelegAngle
    {
        get => _sheepRightForelegAngle;
        private set => SetProperty(ref _sheepRightForelegAngle, value);
    }

    public double SheepLeftElbowAngle
    {
        get => _sheepLeftElbowAngle;
        private set => SetProperty(ref _sheepLeftElbowAngle, value);
    }

    public double SheepRightElbowAngle
    {
        get => _sheepRightElbowAngle;
        private set => SetProperty(ref _sheepRightElbowAngle, value);
    }

    public double SheepLeftForehoofAngle
    {
        get => _sheepLeftForehoofAngle;
        private set => SetProperty(ref _sheepLeftForehoofAngle, value);
    }

    public double SheepRightForehoofAngle
    {
        get => _sheepRightForehoofAngle;
        private set => SetProperty(ref _sheepRightForehoofAngle, value);
    }

    public double SheepLeftHindLegAngle
    {
        get => _sheepLeftHindLegAngle;
        private set => SetProperty(ref _sheepLeftHindLegAngle, value);
    }

    public double SheepRightHindLegAngle
    {
        get => _sheepRightHindLegAngle;
        private set => SetProperty(ref _sheepRightHindLegAngle, value);
    }

    public double SheepLeftKneeAngle
    {
        get => _sheepLeftKneeAngle;
        private set => SetProperty(ref _sheepLeftKneeAngle, value);
    }

    public double SheepRightKneeAngle
    {
        get => _sheepRightKneeAngle;
        private set => SetProperty(ref _sheepRightKneeAngle, value);
    }

    public double SheepLeftHindHoofAngle
    {
        get => _sheepLeftHindHoofAngle;
        private set => SetProperty(ref _sheepLeftHindHoofAngle, value);
    }

    public double SheepRightHindHoofAngle
    {
        get => _sheepRightHindHoofAngle;
        private set => SetProperty(ref _sheepRightHindHoofAngle, value);
    }

    public double SheepLeftEarAngle
    {
        get => _sheepLeftEarAngle;
        private set => SetProperty(ref _sheepLeftEarAngle, value);
    }

    public double SheepRightEarAngle
    {
        get => _sheepRightEarAngle;
        private set => SetProperty(ref _sheepRightEarAngle, value);
    }

    public double SheepLeftEarTipAngle
    {
        get => _sheepLeftEarTipAngle;
        private set => SetProperty(ref _sheepLeftEarTipAngle, value);
    }

    public double SheepRightEarTipAngle
    {
        get => _sheepRightEarTipAngle;
        private set => SetProperty(ref _sheepRightEarTipAngle, value);
    }

    public double SheepTailAngle
    {
        get => _sheepTailAngle;
        private set => SetProperty(ref _sheepTailAngle, value);
    }

    public double SheepTailTipAngle
    {
        get => _sheepTailTipAngle;
        private set => SetProperty(ref _sheepTailTipAngle, value);
    }

    public double SheepLeftWoolFringeAngle
    {
        get => _sheepLeftWoolFringeAngle;
        private set => SetProperty(ref _sheepLeftWoolFringeAngle, value);
    }

    public double SheepRightWoolFringeAngle
    {
        get => _sheepRightWoolFringeAngle;
        private set => SetProperty(ref _sheepRightWoolFringeAngle, value);
    }

    public double BassEnergy
    {
        get => _bassEnergy;
        private set => SetProperty(ref _bassEnergy, value);
    }

    public double SignalEnergy
    {
        get => _signalEnergy;
        private set => SetProperty(ref _signalEnergy, value);
    }

    public double SpectralFlux
    {
        get => _spectralFlux;
        private set => SetProperty(ref _spectralFlux, value);
    }

    public double CrossBandAgreement
    {
        get => _crossBandAgreement;
        private set => SetProperty(ref _crossBandAgreement, value);
    }

    public double BeatPeriodicity
    {
        get => _beatPeriodicity;
        private set => SetProperty(ref _beatPeriodicity, value);
    }

    public double BeatBaseline
    {
        get => _beatBaseline;
        private set => SetProperty(ref _beatBaseline, value);
    }

    public double BeatThreshold
    {
        get => _beatThreshold;
        private set => SetProperty(ref _beatThreshold, value);
    }

    public double OnsetStrength
    {
        get => _onsetStrength;
        private set => SetProperty(ref _onsetStrength, value);
    }

    public double BeatPulse
    {
        get => _beatPulse;
        private set => SetProperty(ref _beatPulse, value);
    }

    public double BeatSensitivity
    {
        get => _beatSensitivity;
        set => SetProperty(ref _beatSensitivity, double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0.55);
    }

    public bool IsBeatDebugVisible
    {
        get => _isBeatDebugVisible;
        set => SetProperty(ref _isBeatDebugVisible, value);
    }

    public bool IsBeatDetected
    {
        get => _isBeatDetected;
        private set => SetProperty(ref _isBeatDetected, value);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _danceTimer.Stop();
        _danceTimer.Tick -= DanceTimerOnTick;
        AudioSources.CollectionChanged -= AudioSourcesOnCollectionChanged;
        LocalizationManager.LanguageChanged -= LocalizationManagerOnLanguageChanged;
    }

    internal void AdvanceDance(IReadOnlyList<double>? spectrumBuckets, double deltaSeconds)
    {
        var beatFrame = _beatDetector.Update(spectrumBuckets, deltaSeconds, BeatSensitivity);
        ApplyBeatFrame(beatFrame);
        ApplyDanceFrame(_danceController.Update(beatFrame.SignalEnergy, beatFrame.Pulse, deltaSeconds));

        var spectrumFrame = RhythmSpectrumAnalyzer.Analyze(spectrumBuckets);
        var affinityFrame = RhythmSpectrumAnalyzer.CalculateAffinity(spectrumFrame);
        ActiveRhythmBand = spectrumFrame.DominantBand;
        LowRhythmAffinity = FollowAffinity(LowRhythmAffinity, affinityFrame.Low, deltaSeconds);
        MidRhythmAffinity = FollowAffinity(MidRhythmAffinity, affinityFrame.Mid, deltaSeconds);
        HighRhythmAffinity = FollowAffinity(HighRhythmAffinity, affinityFrame.High, deltaSeconds);

        var timestampSeconds = _danceClock.Elapsed.TotalSeconds;
        ApplyRhythmFrame(_rhythmController.Update(timestampSeconds, beatFrame.IsBeat));
        if (beatFrame.IsBeat && _pendingRhythmEchoKind is { } echoKind)
        {
            _pendingRhythmEchoKind = null;
            VisualEffectRequested?.Invoke(new TileVisualEffectRequest(
                TileVisualEffectKind.RhythmEcho,
                echoKind,
                144,
                210));
        }
    }

    private void SelectTile(object? parameter)
    {
        if (parameter is not int tileId
            || _game.Tiles.FirstOrDefault(candidate => candidate.Id == tileId) is not { } tile)
        {
            return;
        }

        var result = _game.Select(tileId);
        if (result == TileSelectionResult.Ignored)
        {
            return;
        }

        ApplyRhythmFrame(_rhythmController.RegisterAction(_danceClock.Elapsed.TotalSeconds));

        var position = GetBoardPosition(tile);
        VisualEffectRequested?.Invoke(new TileVisualEffectRequest(
            TileVisualEffectKind.Selection,
            tile.Kind,
            position.Left + (TileSize / 2),
            position.Top + (TileSize / 2)));
        if (result is TileSelectionResult.Matched or TileSelectionResult.Won)
        {
            _matchCount++;
            _pendingRhythmEchoKind = tile.Kind;
            VisualEffectRequested?.Invoke(new TileVisualEffectRequest(
                TileVisualEffectKind.Match,
                tile.Kind,
                position.Left + (TileSize / 2),
                position.Top + (TileSize / 2)));
        }

        RefreshGameSurface();
        if (result == TileSelectionResult.Won)
        {
            RoundPresentationRequested?.Invoke(GameRoundPresentation.Won);
        }
        else if (result == TileSelectionResult.Lost)
        {
            RoundPresentationRequested?.Invoke(GameRoundPresentation.Lost);
        }
    }

    private bool CanSelectTile(object? parameter)
    {
        return parameter is int tileId
            && _game.Tiles.FirstOrDefault(tile => tile.Id == tileId) is { } tile
            && _game.IsSelectable(tile);
    }

    private void Restart()
    {
        ResetRound(GameRoundPresentation.Restarted);
    }

    private void NewGame()
    {
        var nextSeed = Random.Shared.Next();
        while (nextSeed == _seed)
        {
            nextSeed = Random.Shared.Next();
        }

        _seed = nextSeed;
        ResetRound(GameRoundPresentation.NewGame);
    }

    private void ResetRound(GameRoundPresentation presentation)
    {
        _game = LayeredTileGame.Create(_seed);
        _matchCount = 0;
        ResetRhythmInteraction();
        RefreshGameSurface();
        RoundPresentationRequested?.Invoke(presentation);
    }

    private void UseCannon()
    {
        UsePowerUp(GamePowerUpKind.Cannon, _game.UseCannon);
    }

    private void UseFan()
    {
        UsePowerUp(GamePowerUpKind.Fan, _game.UseFan);
    }

    private void UseDigger()
    {
        UsePowerUp(GamePowerUpKind.Digger, _game.UseDigger);
    }

    private void UsePowerUp(GamePowerUpKind kind, Func<bool> use)
    {
        if (!use())
        {
            return;
        }

        RefreshGameSurface();
        PowerUpPresentationRequested?.Invoke(kind);
        if (_game.State == TileGameState.Won)
        {
            RoundPresentationRequested?.Invoke(GameRoundPresentation.Won);
        }
    }

    private void RefreshGameSurface()
    {
        BoardTiles.Clear();
        foreach (var tile in _game.Tiles.Where(tile => !tile.IsRemoved))
        {
            var position = GetBoardPosition(tile);
            BoardTiles.Add(new GameTileViewModel(
                tile.Id,
                tile.Kind,
                position.Left,
                position.Top,
                tile.Layer,
                _game.IsSelectable(tile),
                _game.GetDepthFromTopLayer(tile)));
        }

        TraySlots.Clear();
        for (var slot = 0; slot < _game.TrayCapacity; slot++)
        {
            TraySlots.Add(new TraySlotViewModel(slot < _game.Tray.Count ? _game.Tray[slot] : null));
        }

        OnPropertyChanged(nameof(GameStatusText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(MatchesText));
        OnPropertyChanged(nameof(IsGameOver));
        OnPropertyChanged(nameof(IsCannonAvailable));
        OnPropertyChanged(nameof(IsFanAvailable));
        OnPropertyChanged(nameof(IsDiggerAvailable));
        _selectTileCommand.RaiseCanExecuteChanged();
        _useCannonCommand.RaiseCanExecuteChanged();
        _useFanCommand.RaiseCanExecuteChanged();
        _useDiggerCommand.RaiseCanExecuteChanged();
    }

    private void DanceTimerOnTick(object? sender, EventArgs e)
    {
        var currentTimestamp = _danceClock.ElapsedTicks;
        var deltaSeconds = _lastDanceTimestamp == 0
            ? 1d / 30
            : (currentTimestamp - _lastDanceTimestamp) / (double)Stopwatch.Frequency;
        _lastDanceTimestamp = currentTimestamp;

        var spectrumBuckets = SelectedAudioSource is { IsInstanceMissing: false } source
            ? source.SpectrumBuckets
            : null;
        AdvanceDance(spectrumBuckets, deltaSeconds);
    }

    private void ApplyBeatFrame(BeatDetectionFrame frame)
    {
        BassEnergy = frame.BassEnergy;
        SignalEnergy = frame.SignalEnergy;
        SpectralFlux = frame.SpectralFlux;
        CrossBandAgreement = frame.CrossBandAgreement;
        BeatPeriodicity = frame.Periodicity;
        BeatBaseline = frame.Baseline;
        BeatThreshold = frame.Threshold;
        OnsetStrength = frame.OnsetStrength;
        BeatPulse = frame.Pulse;
        IsBeatDetected = frame.IsBeat;
    }

    private void ApplyRhythmFrame(RhythmInteractionFrame frame)
    {
        Groove = frame.Groove;
        RhythmPhraseStep = frame.PhraseStep;
        if (SetProperty(ref _rhythmTimingGrade, frame.TimingGrade))
        {
            OnPropertyChanged(nameof(RhythmTimingText));
            OnPropertyChanged(nameof(IsRhythmTimingVisible));
        }

        RhythmBeatPhase = frame.PredictedBeatPhase;
        var tempoChanged = SetProperty(ref _rhythmTempoBpm, frame.TempoBpm);
        tempoChanged |= SetProperty(ref _rhythmTempoConfidence, frame.TempoConfidence);
        if (tempoChanged)
        {
            OnPropertyChanged(nameof(RhythmTempoText));
            OnPropertyChanged(nameof(IsTempoConfident));
        }
    }

    private void ResetRhythmInteraction()
    {
        _rhythmController.Reset();
        _pendingRhythmEchoKind = null;
        ActiveRhythmBand = RhythmBand.None;
        LowRhythmAffinity = 0;
        MidRhythmAffinity = 0;
        HighRhythmAffinity = 0;
        ApplyRhythmFrame(default);
    }

    private static double FollowAffinity(double current, double target, double deltaSeconds)
    {
        deltaSeconds = double.IsFinite(deltaSeconds) ? Math.Clamp(deltaSeconds, 1d / 240, 0.1) : 1d / 30;
        var timeConstant = target > current ? 0.08 : 0.22;
        return current + ((target - current) * (1 - Math.Exp(-deltaSeconds / timeConstant)));
    }

    private void ApplyDanceFrame(SheepDanceFrame frame)
    {
        DanceEnergy = frame.Energy;
        SheepBounce = -frame.Bounce;
        SheepLean = frame.Lean;
        SheepScaleX = 2 - frame.Squash;
        SheepScaleY = frame.Squash;
        SheepTorsoTwist = frame.TorsoTwist;
        SheepHeadAngle = frame.HeadAngle;
        SheepHeadBob = frame.HeadBob;
        SheepEyeScaleY = frame.EyeScaleY;
        SheepMouthScaleY = frame.MouthScaleY;
        SheepLeftForelegAngle = frame.LeftForelegAngle;
        SheepRightForelegAngle = frame.RightForelegAngle;
        SheepLeftElbowAngle = frame.LeftElbowAngle;
        SheepRightElbowAngle = frame.RightElbowAngle;
        SheepLeftForehoofAngle = frame.LeftForehoofAngle;
        SheepRightForehoofAngle = frame.RightForehoofAngle;
        SheepLeftHindLegAngle = frame.LeftHindLegAngle;
        SheepRightHindLegAngle = frame.RightHindLegAngle;
        SheepLeftKneeAngle = frame.LeftKneeAngle;
        SheepRightKneeAngle = frame.RightKneeAngle;
        SheepLeftHindHoofAngle = frame.LeftHindHoofAngle;
        SheepRightHindHoofAngle = frame.RightHindHoofAngle;
        SheepLeftEarAngle = frame.LeftEarAngle;
        SheepRightEarAngle = frame.RightEarAngle;
        SheepLeftEarTipAngle = frame.LeftEarTipAngle;
        SheepRightEarTipAngle = frame.RightEarTipAngle;
        SheepTailAngle = frame.TailAngle;
        SheepTailTipAngle = frame.TailTipAngle;
        SheepLeftWoolFringeAngle = frame.LeftWoolFringeAngle;
        SheepRightWoolFringeAngle = frame.RightWoolFringeAngle;
    }

    private static (double Left, double Top) GetBoardPosition(LayeredTile tile)
    {
        return (
            BoardOffsetX + (tile.Y * TileStep),
            BoardOffsetY + ((6 - tile.X) * TileStep) - (tile.Layer * LayerRise));
    }

    private void AudioSourcesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedAudioSource is null || !AudioSources.Contains(SelectedAudioSource))
        {
            SelectInitialAudioSource();
        }
    }

    private void SelectInitialAudioSource()
    {
        SelectedAudioSource = _mainViewModel.SelectedDetailSession is { IsInstanceMissing: false } selected
            ? selected
            : AudioSources.FirstOrDefault(source => !source.IsInstanceMissing);
    }

    private void LocalizationManagerOnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(GameStatusText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(MatchesText));
        OnPropertyChanged(nameof(RhythmTimingText));
    }
}

public sealed record GameTileViewModel(
    int Id,
    SignalTileKind Kind,
    double Left,
    double Top,
    int Layer,
    bool IsSelectable,
    int DepthFromTopLayer)
{
    public bool IsTwoOrMoreLayersBelowTop => DepthFromTopLayer >= 2;
}

public sealed record TraySlotViewModel(SignalTileKind? Kind)
{
    public bool HasTile => Kind is not null;
}
