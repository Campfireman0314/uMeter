using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class LayeredTileGameTests
{
    [Fact]
    public void GeneratedBoardHasSevenSlotTrayAndKnownLayerShape()
    {
        var game = LayeredTileGame.Create(42);

        Assert.Equal(222, game.Tiles.Count);
        Assert.Equal([48, 48, 46, 34, 24, 10, 4, 4, 4],
            game.Tiles.GroupBy(tile => tile.Layer).OrderBy(group => group.Key).Select(group => group.Count()));
        Assert.Equal(16, Enum.GetValues<SignalTileKind>().Length);
        Assert.Equal(7, game.TrayCapacity);
        Assert.All(game.Tiles.Where(tile => tile.Layer == 8), tile => Assert.True(game.IsSelectable(tile)));
        Assert.Contains(game.Tiles.Where(tile => tile.Layer == 4), tile => !game.IsSelectable(tile));
    }

    [Fact]
    public void GeneratedBoardUsesNestedSymmetricReferenceTopology()
    {
        var game = LayeredTileGame.Create(42);
        var baseLayer = game.Tiles.Where(tile => tile.Layer == 0).ToArray();
        var rowCounts = baseLayer
            .GroupBy(tile => Math.Round(tile.Y, 2))
            .OrderBy(group => group.Key)
            .Select(group => group.Count())
            .ToArray();
        var centerX = (game.Tiles.Min(tile => tile.X) + game.Tiles.Max(tile => tile.X)) / 2;

        Assert.InRange(game.Tiles.Max(tile => tile.X) - game.Tiles.Min(tile => tile.X), 6.5, 6.7);
        Assert.InRange(game.Tiles.Max(tile => tile.Y) - game.Tiles.Min(tile => tile.Y), 8.4, 8.5);
        Assert.Equal([4, 4, 4, 4, 8, 4, 2, 4, 8, 6], rowCounts);
        Assert.All(game.Tiles, tile => Assert.Contains(
            game.Tiles,
            mirror => mirror.Layer == tile.Layer
                && Math.Abs(mirror.Y - tile.Y) < 0.001
                && Math.Abs(mirror.X - ((2 * centerX) - tile.X)) < 0.001));
        Assert.All(game.Tiles.Where(tile => tile.Layer > 0), upper => Assert.Contains(
            game.Tiles,
            lower => lower.Layer == upper.Layer - 1
                && Math.Abs(lower.X - upper.X) < 1
                && Math.Abs(lower.Y - upper.Y) < 1));
        Assert.True(game.FractionallyOffsetTileCount >= 200);
        Assert.True(game.MaximumImmediateLowerLayerFanOut >= 2);
        Assert.True(game.MultiRouteBlockerCount >= 40);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void GeneratedBoardStartsWithCompactNonDominantFrontier(int seed)
    {
        var game = LayeredTileGame.Create(seed);
        var selectable = game.Tiles.Where(game.IsSelectable).ToArray();
        var familyCounts = selectable.GroupBy(tile => tile.Kind).Select(group => group.Count()).ToArray();

        Assert.Equal(16, selectable.Length);
        Assert.InRange(selectable.Max(tile => tile.X) - selectable.Min(tile => tile.X), 4.3, 4.4);
        Assert.InRange(selectable.Max(tile => tile.Y) - selectable.Min(tile => tile.Y), 8.4, 8.5);
        Assert.True(familyCounts.Max() <= 3);
        Assert.True(familyCounts.Length >= 10);
    }

    [Fact]
    public void GeneratedBoardsExposeDangerousDecoysAlongTheConstructedPath()
    {
        foreach (var seed in new[] { 1, 2, 3, 10, 42, 99, 256, 1024, 2026, 65535 })
        {
            var metrics = LayeredTileGame.Create(seed).ConstructedPathDifficulty;

            Assert.Equal(6, metrics.PeakTrayOccupancy);
            Assert.True(metrics.RiskyDecoyChoices > 0, $"Seed {seed} exposed no tray-filling decoy choices.");
            Assert.True(metrics.NewFamilyDecoyChoices > 0, $"Seed {seed} exposed no new-family decoy choices.");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void GeneratedBoardCanBeClearedWithoutPowerUpsUnderHighTrayPressure(int seed)
    {
        var game = LayeredTileGame.Create(seed);
        var maximumTrayOccupancy = 0;

        foreach (var tile in game.Tiles.OrderByDescending(tile => tile.Layer).ThenBy(tile => tile.Id))
        {
            Assert.True(game.IsSelectable(tile));
            game.Select(tile.Id);
            maximumTrayOccupancy = Math.Max(maximumTrayOccupancy, game.Tray.Count);
        }

        Assert.Equal(6, maximumTrayOccupancy);
        Assert.Equal(TileGameState.Won, game.State);
        Assert.Equal(0, game.RemainingTileCount);
        Assert.Empty(game.Tray);
        Assert.True(game.CannonAvailable);
        Assert.True(game.FanAvailable);
        Assert.True(game.DiggerAvailable);
    }

    [Fact]
    public void GeneratedBoardRemainsPlayableThroughMidpointWithoutPowerUp()
    {
        var game = LayeredTileGame.Create(42);
        var firstHalf = game.Tiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .Take(111)
            .ToArray();

        foreach (var tile in firstHalf)
        {
            Assert.True(game.IsSelectable(tile));
            game.Select(tile.Id);
        }

        Assert.Equal(111, game.RemainingTileCount);
        Assert.Equal(TileGameState.Playing, game.State);
        Assert.Contains(game.Tiles, game.IsSelectable);
    }

    [Fact]
    public void ConstructedSolutionInterleavesThreeFamiliesPerPressureBlock()
    {
        var game = LayeredTileGame.Create(42);
        var solutionOrder = game.Tiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .ToArray();
        var opening = solutionOrder.Take(3).ToArray();
        var pressureBlocks = solutionOrder.Skip(3).Take(216).Chunk(9).ToArray();
        var closing = solutionOrder.TakeLast(3).ToArray();

        Assert.Single(opening.Select(tile => tile.Kind).Distinct());
        Assert.All(pressureBlocks, block =>
        {
            var families = block.GroupBy(tile => tile.Kind).ToArray();
            Assert.Equal(3, families.Length);
            Assert.All(families, family => Assert.Equal(3, family.Count()));
            Assert.All(block.Chunk(3), pass => Assert.Equal(3, pass.Select(tile => tile.Kind).Distinct().Count()));
        });
        Assert.Single(closing.Select(tile => tile.Kind).Distinct());
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
