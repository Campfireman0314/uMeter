using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Game;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class TileGameViewModelTests
{
    [Fact]
    public void SelectsFirstLiveMeterSourceAndFollowsItsBassSpectrum()
    {
        var service = new FakeSourceService
        {
            Snapshots = [Snapshot("music", "Music", 0.75f)]
        };
        using var mainViewModel = new MainViewModel(service, startTimer: false);
        mainViewModel.RefreshNow();
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);

        Assert.Equal("music", gameViewModel.SelectedAudioSource?.SourceKey);

        gameViewModel.AdvanceDance(Enumerable.Repeat(0.4, 320).ToArray(), 1d / 30);
        var highBassSpectrum = Enumerable.Repeat(0.98, 320).ToArray();
        for (var index = 0; index < 6; index++)
        {
            gameViewModel.AdvanceDance(highBassSpectrum, 1d / 30);
        }

        Assert.True(gameViewModel.DanceEnergy > 0.3);
        Assert.True(gameViewModel.SheepBounce < -12);
        Assert.InRange(gameViewModel.SheepLean, -14, 14);
        Assert.True(gameViewModel.SheepHeadBob < 0);
        Assert.NotEqual(gameViewModel.SheepLeftForelegAngle, gameViewModel.SheepRightForelegAngle);
        Assert.NotEqual(gameViewModel.SheepLeftForelegAngle, gameViewModel.SheepLeftElbowAngle);
        Assert.NotEqual(gameViewModel.SheepLeftHindLegAngle, gameViewModel.SheepLeftKneeAngle);
        Assert.NotEqual(gameViewModel.SheepLeftEarAngle, gameViewModel.SheepLeftEarTipAngle);
        Assert.NotEqual(gameViewModel.SheepTailAngle, gameViewModel.SheepTailTipAngle);
    }

    [Fact]
    public void BoardCoordinatesUseExpandedDemoPortraitFootprint()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var denseMiddle = gameViewModel.BoardTiles.Where(tile => tile.Layer == 6).ToArray();

        Assert.Equal(9, denseMiddle.Select(tile => tile.Left).Distinct().Count());
        Assert.True(denseMiddle.Select(tile => tile.Top).Distinct().Count() >= 6);
        Assert.True(denseMiddle.Max(tile => tile.Left) - denseMiddle.Min(tile => tile.Left) >= 400);
        Assert.True(gameViewModel.BoardTiles.Max(tile => tile.Top) < TileGameViewModel.BoardHeight);
        Assert.True(gameViewModel.BoardTiles.Max(tile => tile.Left) < TileGameViewModel.BoardWidth);
    }

    [Fact]
    public void SelectingAnExposedTripleUpdatesBoardTrayAndMatchCount()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var effects = new List<TileVisualEffectRequest>();
        gameViewModel.VisualEffectRequested += effects.Add;
        var triple = gameViewModel.BoardTiles
            .Where(tile => tile.IsSelectable)
            .GroupBy(tile => tile.Kind)
            .First(group => group.Count() >= 3)
            .Take(3)
            .ToArray();

        foreach (var tile in triple)
        {
            gameViewModel.SelectTileCommand.Execute(tile.Id);
        }

        Assert.Equal(219, gameViewModel.BoardTiles.Count);
        Assert.All(gameViewModel.TraySlots, slot => Assert.False(slot.HasTile));
        Assert.Equal("1 matches", gameViewModel.MatchesText);
        Assert.Equal(3, effects.Count(effect => effect.EffectKind == TileVisualEffectKind.Selection));
        var matchEffect = Assert.Single(effects, effect => effect.EffectKind == TileVisualEffectKind.Match);
        Assert.Equal(triple[0].Kind, matchEffect.TileKind);

        gameViewModel.AdvanceDance(SpectrumWithTone(90, 0.4), 1d / 30);
        gameViewModel.AdvanceDance(SpectrumWithTone(90, 1), 1d / 30);

        var rhythmEcho = Assert.Single(effects, effect => effect.EffectKind == TileVisualEffectKind.RhythmEcho);
        Assert.Equal(triple[0].Kind, rhythmEcho.TileKind);
        Assert.All(effects, effect =>
        {
            Assert.InRange(effect.CenterX, 0, TileGameViewModel.BoardWidth);
            Assert.InRange(effect.CenterY, 0, TileGameViewModel.BoardHeight);
        });
    }

    [Fact]
    public void RestartKeepsSeedWhileNewGameChangesSeedAndRaisesPresentationEvents()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var presentations = new List<GameRoundPresentation>();
        gameViewModel.RoundPresentationRequested += presentations.Add;
        var initialSeed = gameViewModel.Seed;
        var initialBoard = gameViewModel.BoardTiles
            .Select(tile => (tile.Id, tile.Kind, tile.Left, tile.Top, tile.Layer))
            .ToArray();

        gameViewModel.SelectTileCommand.Execute(gameViewModel.BoardTiles.First(tile => tile.IsSelectable).Id);
        gameViewModel.RestartCommand.Execute(null);

        Assert.Equal(initialSeed, gameViewModel.Seed);
        Assert.Equal(initialBoard, gameViewModel.BoardTiles
            .Select(tile => (tile.Id, tile.Kind, tile.Left, tile.Top, tile.Layer)));
        Assert.Equal(GameRoundPresentation.Restarted, presentations[^1]);

        gameViewModel.NewGameCommand.Execute(null);

        Assert.NotEqual(initialSeed, gameViewModel.Seed);
        Assert.Equal(GameRoundPresentation.NewGame, presentations[^1]);
        Assert.Equal(222, gameViewModel.BoardTiles.Count);
    }

    [Fact]
    public void FillingTrayRaisesLossPresentationForRagdoll()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var presentations = new List<GameRoundPresentation>();
        var selectedKinds = new HashSet<SignalTileKind>();
        gameViewModel.RoundPresentationRequested += presentations.Add;

        while (selectedKinds.Count < 7)
        {
            var tile = gameViewModel.BoardTiles.First(candidate =>
                candidate.IsSelectable && !selectedKinds.Contains(candidate.Kind));
            selectedKinds.Add(tile.Kind);
            gameViewModel.SelectTileCommand.Execute(tile.Id);
        }

        Assert.True(gameViewModel.IsGameOver);
        Assert.Equal("Tray full", gameViewModel.GameStatusText);
        Assert.Equal(GameRoundPresentation.Lost, presentations[^1]);
    }

    [Fact]
    public void MidpointRemainsPlayableWithoutPowerUp()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var firstHalf = gameViewModel.BoardTiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .Take(111)
            .Select(tile => tile.Id)
            .ToArray();

        foreach (var tileId in firstHalf)
        {
            gameViewModel.SelectTileCommand.Execute(tileId);
        }

        Assert.Equal("Playing", gameViewModel.GameStatusText);
        Assert.Equal(111, gameViewModel.BoardTiles.Count);
        Assert.Contains(gameViewModel.BoardTiles, tile => tile.IsSelectable);
        Assert.True(gameViewModel.IsCannonAvailable);
        Assert.True(gameViewModel.IsFanAvailable);
        Assert.True(gameViewModel.IsDiggerAvailable);
    }

    [Fact]
    public void PowerUpCommandsAreSingleUseAndRestartRestoresThem()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var presentations = new List<GamePowerUpKind>();
        gameViewModel.PowerUpPresentationRequested += presentations.Add;

        Assert.False(gameViewModel.UseCannonCommand.CanExecute(null));
        Assert.True(gameViewModel.UseFanCommand.CanExecute(null));
        Assert.True(gameViewModel.UseDiggerCommand.CanExecute(null));

        gameViewModel.UseDiggerCommand.Execute(null);

        Assert.Equal(8, gameViewModel.TraySlots.Count);
        Assert.False(gameViewModel.IsDiggerAvailable);
        Assert.Equal(GamePowerUpKind.Digger, presentations[^1]);

        gameViewModel.SelectTileCommand.Execute(gameViewModel.BoardTiles.First(tile => tile.IsSelectable).Id);
        Assert.True(gameViewModel.UseCannonCommand.CanExecute(null));
        gameViewModel.UseCannonCommand.Execute(null);

        Assert.False(gameViewModel.IsCannonAvailable);
        Assert.Equal(GamePowerUpKind.Cannon, presentations[^1]);

        gameViewModel.UseFanCommand.Execute(null);

        Assert.False(gameViewModel.IsFanAvailable);
        Assert.Equal(GamePowerUpKind.Fan, presentations[^1]);

        gameViewModel.RestartCommand.Execute(null);

        Assert.Equal(7, gameViewModel.TraySlots.Count);
        Assert.True(gameViewModel.IsCannonAvailable);
        Assert.True(gameViewModel.IsFanAvailable);
        Assert.True(gameViewModel.IsDiggerAvailable);
    }

    [Fact]
    public void ClearingConstructedSolutionRaisesWinPresentation()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        var presentations = new List<GameRoundPresentation>();
        gameViewModel.RoundPresentationRequested += presentations.Add;
        var solutionIds = gameViewModel.BoardTiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .Select(tile => tile.Id)
            .ToArray();

        foreach (var tileId in solutionIds)
        {
            Assert.True(gameViewModel.SelectTileCommand.CanExecute(tileId));
            gameViewModel.SelectTileCommand.Execute(tileId);
        }

        Assert.True(gameViewModel.IsGameOver);
        Assert.Equal("Board cleared!", gameViewModel.GameStatusText);
        Assert.Equal(GameRoundPresentation.Won, presentations[^1]);
        Assert.Empty(gameViewModel.BoardTiles);
    }

    [Fact]
    public void MatchEffectProfileIsVisuallyStrongerThanSelectionProfile()
    {
        var selection = TileVisualEffectProfile.For(TileVisualEffectKind.Selection);
        var match = TileVisualEffectProfile.For(TileVisualEffectKind.Match);
        var rhythmEcho = TileVisualEffectProfile.For(TileVisualEffectKind.RhythmEcho);

        Assert.True(match.ParticleCount > selection.ParticleCount);
        Assert.True(match.TravelDistance > selection.TravelDistance);
        Assert.True(match.Duration > selection.Duration);
        Assert.True(match.RingCount > selection.RingCount);
        Assert.False(selection.ShowsGlyph);
        Assert.True(match.ShowsGlyph);
        Assert.InRange(rhythmEcho.ParticleCount, selection.ParticleCount + 1, match.ParticleCount - 1);
        Assert.True(rhythmEcho.Duration > selection.Duration);
        Assert.False(rhythmEcho.ShowsGlyph);
    }

    [Fact]
    public void OnBeatSelectionBuildsGrooveAndTracksSpectralAffinity()
    {
        using var mainViewModel = new MainViewModel(new FakeSourceService(), startTimer: false);
        using var gameViewModel = new TileGameViewModel(mainViewModel, seed: 42, startDanceTimer: false);
        gameViewModel.AdvanceDance(SpectrumWithTone(90, 0.4), 1d / 30);
        gameViewModel.AdvanceDance(SpectrumWithTone(90, 1), 1d / 30);
        gameViewModel.AdvanceDance(SpectrumWithTone(5000, 1), 1d / 30);
        var selectableTile = gameViewModel.BoardTiles.First(tile => tile.IsSelectable);

        gameViewModel.SelectTileCommand.Execute(selectableTile.Id);

        Assert.Equal(RhythmBand.High, gameViewModel.ActiveRhythmBand);
        Assert.True(gameViewModel.HighRhythmAffinity > 0);
        Assert.True(gameViewModel.Groove > 0);
        Assert.NotEmpty(gameViewModel.RhythmTimingText);
        Assert.Equal(1, gameViewModel.RhythmPhraseStep);
    }

    private static double[] SpectrumWithTone(double frequency, double displayLevel)
    {
        var buckets = new double[SpectrumScale.BucketCount];
        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        var ratio = (SpectrumScale.HertzToMel(frequency) - minimumMel) / (maximumMel - minimumMel);
        var index = (int)Math.Round(ratio * (buckets.Length - 1));
        buckets[index] = displayLevel;
        return buckets;
    }

    private static AudioSessionSnapshot Snapshot(string key, string name, float peak)
    {
        return new AudioSessionSnapshot(
            key,
            42,
            name,
            peak,
            -6,
            "-6.0 dB",
            "AudioSessionStateActive");
    }

    private sealed class FakeSourceService : ISignalSourceMeterService
    {
        public IReadOnlyList<SignalSourceSnapshot> Snapshots { get; init; } = [];

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return Snapshots;
        }
    }
}
