using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class LayeredTileGameTests
{
    [Fact]
    public void GeneratedBoardHasSevenSlotTrayAndKnownLayerShape()
    {
        var game = LayeredTileGame.Create(42);

        Assert.Equal(222, game.Tiles.Count);
        Assert.Equal([4, 8, 13, 18, 23, 28, 34, 28, 23, 18, 13, 8, 4],
            game.Tiles.GroupBy(tile => tile.Layer).OrderBy(group => group.Key).Select(group => group.Count()));
        Assert.Equal(16, Enum.GetValues<SignalTileKind>().Length);
        Assert.Equal(7, game.TrayCapacity);
        Assert.All(game.Tiles.Where(tile => tile.Layer == 12), tile => Assert.True(game.IsSelectable(tile)));
        Assert.Contains(game.Tiles.Where(tile => tile.Layer == 6), tile => !game.IsSelectable(tile));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void GeneratedBoardStartsWithOneImmediatelyCompletableKind(int seed)
    {
        var game = LayeredTileGame.Create(seed);
        var immediateMatches = game.Tiles
            .Where(game.IsSelectable)
            .GroupBy(tile => tile.Kind)
            .Count(group => group.Count() >= 3);

        Assert.Equal(1, immediateMatches);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void GeneratedBoardCanBeClearedWithOnePowerUp(int seed)
    {
        var game = LayeredTileGame.Create(seed);

        foreach (var tile in game.Tiles.OrderByDescending(tile => tile.Layer).ThenBy(tile => tile.Id))
        {
            if (game.IsToolGateActive)
            {
                Assert.True(game.UseDigger());
            }

            Assert.True(game.IsSelectable(tile));
            game.Select(tile.Id);
        }

        Assert.Equal(TileGameState.Won, game.State);
        Assert.Equal(0, game.RemainingTileCount);
        Assert.Empty(game.Tray);
    }

    [Fact]
    public void GeneratedBoardCannotPassMidpointWithoutUsingPowerUp()
    {
        var game = LayeredTileGame.Create(42);
        foreach (var tile in game.Tiles.OrderByDescending(tile => tile.Layer).ThenBy(tile => tile.Id))
        {
            if (game.IsToolGateActive)
            {
                break;
            }

            game.Select(tile.Id);
        }

        Assert.True(game.IsToolGateActive);
        Assert.Equal(111, game.RemainingTileCount);
        Assert.False(game.HasUsedPowerUp);
        Assert.DoesNotContain(game.Tiles, game.IsSelectable);
        Assert.True(game.UseFan());
        Assert.True(game.HasUsedPowerUp);
        Assert.Contains(game.Tiles, game.IsSelectable);
    }

    [Fact]
    public void ConstructedSolutionCarriesSomeTriplesAcrossLayerBoundaries()
    {
        var game = LayeredTileGame.Create(42);
        var solutionGroups = game.Tiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .Chunk(3)
            .ToArray();

        Assert.All(solutionGroups, group => Assert.Single(group.Select(tile => tile.Kind).Distinct()));
        Assert.Contains(solutionGroups, group => group.Select(tile => tile.Layer).Distinct().Count() > 1);
        Assert.Equal(16, solutionGroups.Take(16).Select(group => group[0].Kind).Distinct().Count());
    }

    [Fact]
    public void FanPreservesCardMultisetAndRebuildsGuaranteedCompletion()
    {
        var game = LayeredTileGame.Create(42);
        var firstTile = game.Tiles.OrderByDescending(tile => tile.Layer).ThenBy(tile => tile.Id).First();
        game.Select(firstTile.Id);
        var multisetBefore = game.Tiles
            .Where(tile => !tile.IsRemoved)
            .GroupBy(tile => tile.Kind)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.True(game.UseFan());

        var multisetAfter = game.Tiles
            .Where(tile => !tile.IsRemoved)
            .GroupBy(tile => tile.Kind)
            .ToDictionary(group => group.Key, group => group.Count());
        Assert.Equal(multisetBefore, multisetAfter);
        Assert.False(game.FanAvailable);
        foreach (var tile in game.Tiles.Where(tile => !tile.IsRemoved).OrderByDescending(tile => tile.Layer).ThenBy(tile => tile.Id))
        {
            Assert.True(game.IsSelectable(tile));
            game.Select(tile.Id);
        }

        Assert.Equal(TileGameState.Won, game.State);
    }

    [Fact]
    public void CannonClearsFullTrayAndComplementaryGroupsWithoutBreakingRound()
    {
        var kinds = Enum.GetValues<SignalTileKind>().Take(7).ToArray();
        var tiles = kinds.SelectMany((kind, kindIndex) => Enumerable.Range(0, 3)
            .Select(copy => LayeredTileGame.CreateTileForTesting(
                (kindIndex * 3) + copy,
                kind,
                (kindIndex * 3) + copy,
                0,
                0)));
        var game = LayeredTileGame.CreateForTesting(tiles);

        foreach (var tile in game.Tiles.Where((_, index) => index % 3 == 0).ToArray())
        {
            game.Select(tile.Id);
        }

        Assert.Equal(TileGameState.Lost, game.State);
        Assert.True(game.UseCannon());
        Assert.Equal(TileGameState.Won, game.State);
        Assert.Empty(game.Tray);
        Assert.Equal(0, game.RemainingTileCount);
        Assert.False(game.CannonAvailable);
    }

    [Fact]
    public void DiggerAddsOneSlotAndRecoversFullTray()
    {
        var kinds = Enum.GetValues<SignalTileKind>().Take(8).ToArray();
        var game = LayeredTileGame.CreateForTesting(
            kinds.Select((kind, index) => LayeredTileGame.CreateTileForTesting(index, kind, index * 2, 0, 0)));

        foreach (var tile in game.Tiles.Take(7))
        {
            game.Select(tile.Id);
        }

        Assert.Equal(TileGameState.Lost, game.State);
        Assert.True(game.UseDigger());
        Assert.Equal(TileGameState.Playing, game.State);
        Assert.Equal(8, game.TrayCapacity);
        Assert.True(game.IsSelectable(game.Tiles[^1]));
        Assert.False(game.DiggerAvailable);
    }

    [Fact]
    public void CoveredTileCannotBeSelectedUntilOverlappingTileIsRemoved()
    {
        var bottom = LayeredTileGame.CreateTileForTesting(1, SignalTileKind.Star, 0, 0, 0);
        var top = LayeredTileGame.CreateTileForTesting(2, SignalTileKind.Note, 0.5, 0.5, 1);
        var game = LayeredTileGame.CreateForTesting([bottom, top]);

        Assert.False(game.IsSelectable(bottom));
        Assert.Equal(TileSelectionResult.Ignored, game.Select(bottom.Id));

        Assert.Equal(TileSelectionResult.Added, game.Select(top.Id));
        Assert.True(game.IsSelectable(bottom));
    }

    [Fact]
    public void ThreeMatchingTilesDisappearFromTray()
    {
        var game = LayeredTileGame.CreateForTesting(
        [
            LayeredTileGame.CreateTileForTesting(1, SignalTileKind.Star, 0, 0, 0),
            LayeredTileGame.CreateTileForTesting(2, SignalTileKind.Star, 2, 0, 0),
            LayeredTileGame.CreateTileForTesting(3, SignalTileKind.Star, 4, 0, 0),
            LayeredTileGame.CreateTileForTesting(4, SignalTileKind.Note, 6, 0, 0)
        ]);

        Assert.Equal(TileSelectionResult.Added, game.Select(1));
        Assert.Equal(TileSelectionResult.Added, game.Select(2));
        Assert.Equal(TileSelectionResult.Matched, game.Select(3));
        Assert.Empty(game.Tray);
    }

    [Fact]
    public void SevenUnmatchedTilesLoseTheGame()
    {
        var kinds = Enum.GetValues<SignalTileKind>().Take(7).ToArray();
        var game = LayeredTileGame.CreateForTesting(
            kinds.Select((kind, index) => LayeredTileGame.CreateTileForTesting(index, kind, index * 2, 0, 0)));

        foreach (var tile in game.Tiles)
        {
            game.Select(tile.Id);
        }

        Assert.Equal(TileGameState.Lost, game.State);
        Assert.Equal(7, game.Tray.Count);
        Assert.All(game.Tiles, tile => Assert.False(game.IsSelectable(tile)));
    }
}
