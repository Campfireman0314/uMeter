namespace DesktopLoudnessMeter.App.Game;

public enum SignalTileKind
{
    Star,
    Note,
    Cloud,
    Diamond,
    Bloom,
    Sun,
    Moon,
    Bolt,
    Wave,
    Pulse,
    Disc,
    Speaker,
    Meter,
    Orbit,
    Bars,
    Arrows
}

public enum TileGameState
{
    Playing,
    Won,
    Lost
}

public enum TileSelectionResult
{
    Ignored,
    Added,
    Matched,
    Won,
    Lost
}

public sealed class LayeredTile
{
    internal LayeredTile(int id, SignalTileKind kind, double x, double y, int layer)
    {
        Id = id;
        Kind = kind;
        X = x;
        Y = y;
        Layer = layer;
    }

    public int Id { get; }

    public SignalTileKind Kind { get; internal set; }

    public double X { get; }

    public double Y { get; }

    public int Layer { get; }

    public bool IsRemoved { get; internal set; }
}

public sealed class LayeredTileGame
{
    public const int DefaultTrayCapacity = 7;
    private static readonly SignalTileKind[] TileKinds = Enum.GetValues<SignalTileKind>();
    private readonly List<LayeredTile> _tiles;
    private readonly List<SignalTileKind> _tray = [];
    private readonly Random _powerUpRandom;
    private int _trayCapacity;

    private LayeredTileGame(
        IEnumerable<LayeredTile> tiles,
        int trayCapacity,
        Random powerUpRandom)
    {
        _tiles = tiles.OrderBy(tile => tile.Layer).ThenBy(tile => tile.Id).ToList();
        _trayCapacity = trayCapacity;
        _powerUpRandom = powerUpRandom;
    }

    public IReadOnlyList<LayeredTile> Tiles => _tiles;

    public IReadOnlyList<SignalTileKind> Tray => _tray;

    public int TrayCapacity => _trayCapacity;

    public bool CannonAvailable { get; private set; } = true;

    public bool FanAvailable { get; private set; } = true;

    public bool DiggerAvailable { get; private set; } = true;

    public bool CanUseCannon => CannonAvailable && _tray.Count > 0;

    public bool CanUseFan => FanAvailable && State == TileGameState.Playing && RemainingTileCount > 0;

    public bool CanUseDigger => DiggerAvailable && State != TileGameState.Won;

    public TileGameState State { get; private set; } = TileGameState.Playing;

    public int RemainingTileCount => _tiles.Count(tile => !tile.IsRemoved);

    public static LayeredTileGame Create(int seed)
    {
        List<LayeredTile>? bestTiles = null;
        var bestScore = long.MaxValue;
        for (var candidate = 0; candidate < 48; candidate++)
        {
            var candidateSeed = unchecked(seed + (candidate * 104729));
            var random = new Random(candidateSeed);
            var placements = CreatePlacements(random);
            var kindsById = AssignKindsToSolutionOrder(placements, random);
            var tiles = placements.Select(placement => new LayeredTile(
                    placement.Id,
                    kindsById[placement.Id],
                    placement.X,
                    placement.Y,
                    placement.Layer))
                .ToList();
            var score = EstimateBranchScore(tiles);
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTiles = tiles;
        }

        return new LayeredTileGame(
            bestTiles ?? throw new InvalidOperationException("No board candidate was generated."),
            DefaultTrayCapacity,
            new Random(seed ^ 0x51A7F00D));
    }

    private static List<TilePlacement> CreatePlacements(Random random)
    {
        var placements = new List<TilePlacement>(222);
        var nextId = 0;
        AddLayer(placements, random, ref nextId, layer: 0, CreateCenteredGrid(2, 2));
        AddLayer(placements, random, ref nextId, layer: 1, CreateRingLayer());
        AddLayer(placements, random, ref nextId, layer: 2, CreateCrossLayer());
        AddLayer(placements, random, ref nextId, layer: 3, CreateCornerCutGrid(5, 4));
        AddLayer(placements, random, ref nextId, layer: 4, CreateCornerCutGrid(5, 5));
        AddLayer(placements, random, ref nextId, layer: 5, CreateCornerCutGrid(6, 5));
        AddLayer(placements, random, ref nextId, layer: 6, CreateDenseMiddle());
        AddLayer(placements, random, ref nextId, layer: 7, CreateCornerCutGrid(6, 5));
        AddLayer(placements, random, ref nextId, layer: 8, CreateCornerCutGrid(5, 5));
        AddLayer(placements, random, ref nextId, layer: 9, CreateCornerCutGrid(5, 4));
        AddLayer(placements, random, ref nextId, layer: 10, CreateCrossLayer());
        AddLayer(placements, random, ref nextId, layer: 11, CreateRingLayer());
        AddLayer(placements, random, ref nextId, layer: 12, CreateCenteredGrid(2, 2));
        return placements;
    }

    private static long EstimateBranchScore(IReadOnlyList<LayeredTile> tiles)
    {
        var removed = new HashSet<int>();
        var blockersByTileId = tiles.ToDictionary(
            tile => tile.Id,
            tile => tiles
                .Where(other => other.Layer > tile.Layer && Overlaps(tile, other))
                .Select(other => other.Id)
                .ToArray());
        var solutionOrder = tiles
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .ToArray();
        var score = 0L;
        for (var index = 0; index < solutionOrder.Length; index += 3)
        {
            var selectableGroups = tiles
                .Where(tile => !removed.Contains(tile.Id))
                .Where(tile => blockersByTileId[tile.Id].All(removed.Contains))
                .GroupBy(tile => tile.Kind)
                .Select(group => group.Count())
                .ToArray();
            var weight = solutionOrder.Length - index;
            var completeChoices = selectableGroups.Count(count => count >= 3);
            var partialPairs = selectableGroups.Count(count => count == 2);
            score += Math.Max(0, completeChoices - 1) * weight * 10000L;
            score += partialPairs * weight * 100L;
            score += selectableGroups.Length * weight;

            removed.Add(solutionOrder[index].Id);
            removed.Add(solutionOrder[index + 1].Id);
            removed.Add(solutionOrder[index + 2].Id);
        }

        return score;
    }

    internal static LayeredTileGame CreateForTesting(IEnumerable<LayeredTile> tiles, int trayCapacity = DefaultTrayCapacity)
    {
        return new LayeredTileGame(tiles, trayCapacity, new Random(0));
    }

    internal static LayeredTile CreateTileForTesting(int id, SignalTileKind kind, double x, double y, int layer)
    {
        return new LayeredTile(id, kind, x, y, layer);
    }

    public int GetDepthFromTopLayer(LayeredTile tile)
    {
        if (tile.IsRemoved)
        {
            return 0;
        }

        var topLayer = _tiles
            .Where(other => !other.IsRemoved)
            .Max(other => other.Layer);
        return Math.Max(0, topLayer - tile.Layer);
    }

    public bool IsSelectable(LayeredTile tile)
    {
        if (State != TileGameState.Playing || tile.IsRemoved)
        {
            return false;
        }

        return !_tiles.Any(other =>
            !other.IsRemoved
            && other.Layer > tile.Layer
            && Overlaps(tile, other));
    }

    public TileSelectionResult Select(int tileId)
    {
        var tile = _tiles.FirstOrDefault(candidate => candidate.Id == tileId);
        if (tile is null || !IsSelectable(tile))
        {
            return TileSelectionResult.Ignored;
        }

        tile.IsRemoved = true;
        InsertIntoTray(tile.Kind);

        var result = TileSelectionResult.Added;
        if (_tray.Count(kind => kind == tile.Kind) == 3)
        {
            _tray.RemoveAll(kind => kind == tile.Kind);
            result = TileSelectionResult.Matched;
        }

        if (RemainingTileCount == 0 && _tray.Count == 0)
        {
            State = TileGameState.Won;
            return TileSelectionResult.Won;
        }

        if (_tray.Count >= TrayCapacity)
        {
            State = TileGameState.Lost;
            return TileSelectionResult.Lost;
        }

        return result;
    }

    public bool UseCannon()
    {
        if (!CanUseCannon)
        {
            return false;
        }

        var complementaryTiles = new List<LayeredTile>();
        foreach (var group in _tray.GroupBy(kind => kind))
        {
            var needed = 3 - group.Count();
            var candidates = _tiles
                .Where(tile => !tile.IsRemoved && tile.Kind == group.Key)
                .Take(needed)
                .ToArray();
            if (candidates.Length != needed)
            {
                return false;
            }

            complementaryTiles.AddRange(candidates);
        }

        foreach (var tile in complementaryTiles)
        {
            tile.IsRemoved = true;
        }

        _tray.Clear();
        CannonAvailable = false;
        RebuildGuaranteedKindOrder();
        RecoverFromLoss();
        CompleteIfCleared();
        return true;
    }

    public bool UseFan()
    {
        if (!CanUseFan || !TryBuildGuaranteedKindOrder(out var kindOrder))
        {
            return false;
        }

        var solutionOrder = RemainingTilesInSolutionOrder();
        for (var index = 0; index < solutionOrder.Length; index++)
        {
            solutionOrder[index].Kind = kindOrder[index];
        }

        FanAvailable = false;
        return true;
    }

    public bool UseDigger()
    {
        if (!CanUseDigger)
        {
            return false;
        }

        _trayCapacity++;
        DiggerAvailable = false;
        RecoverFromLoss();
        return true;
    }

    private void RebuildGuaranteedKindOrder()
    {
        if (!TryBuildGuaranteedKindOrder(out var kindOrder))
        {
            return;
        }

        var solutionOrder = RemainingTilesInSolutionOrder();
        for (var index = 0; index < solutionOrder.Length; index++)
        {
            solutionOrder[index].Kind = kindOrder[index];
        }
    }

    private bool TryBuildGuaranteedKindOrder(out SignalTileKind[] kindOrder)
    {
        var counts = _tiles
            .Where(tile => !tile.IsRemoved)
            .GroupBy(tile => tile.Kind)
            .ToDictionary(group => group.Key, group => group.Count());
        var orderedKinds = new List<SignalTileKind>(RemainingTileCount);

        foreach (var trayGroup in _tray.GroupBy(kind => kind).OrderByDescending(group => group.Count()))
        {
            var needed = 3 - trayGroup.Count();
            if (!counts.TryGetValue(trayGroup.Key, out var available) || available < needed)
            {
                kindOrder = [];
                return false;
            }

            orderedKinds.AddRange(Enumerable.Repeat(trayGroup.Key, needed));
            counts[trayGroup.Key] -= needed;
        }

        var groups = new List<SignalTileKind>();
        foreach (var pair in counts)
        {
            if (pair.Value % 3 != 0)
            {
                kindOrder = [];
                return false;
            }

            groups.AddRange(Enumerable.Repeat(pair.Key, pair.Value / 3));
        }

        foreach (var kind in SpaceRepeatedKinds(groups, _powerUpRandom))
        {
            orderedKinds.Add(kind);
            orderedKinds.Add(kind);
            orderedKinds.Add(kind);
        }

        kindOrder = orderedKinds.ToArray();
        return kindOrder.Length == RemainingTileCount;
    }

    private LayeredTile[] RemainingTilesInSolutionOrder()
    {
        return _tiles
            .Where(tile => !tile.IsRemoved)
            .OrderByDescending(tile => tile.Layer)
            .ThenBy(tile => tile.Id)
            .ToArray();
    }

    private void RecoverFromLoss()
    {
        if (State == TileGameState.Lost && _tray.Count < TrayCapacity)
        {
            State = TileGameState.Playing;
        }
    }

    private void CompleteIfCleared()
    {
        if (RemainingTileCount == 0 && _tray.Count == 0)
        {
            State = TileGameState.Won;
        }
    }

    private static IReadOnlyList<(double X, double Y)> CreateDenseMiddle()
    {
        return CreateGrid(7, 5, 0, 0)
            .Where(position => position != (3, 2))
            .ToArray();
    }

    private static IReadOnlyList<(double X, double Y)> CreateRingLayer()
    {
        return CreateCenteredGrid(3, 3)
            .Where(position => position != (3, 2))
            .ToArray();
    }

    private static IReadOnlyList<(double X, double Y)> CreateCrossLayer()
    {
        return
        [
            (2, 0), (3, 0), (4, 0),
            (1, 1), (2, 1), (3, 1), (4, 1), (5, 1),
            (2, 2), (3, 2), (4, 2),
            (3, 3),
            (3, 4)
        ];
    }

    private static IReadOnlyList<(double X, double Y)> CreateCornerCutGrid(int columns, int rows)
    {
        var positions = CreateCenteredGrid(columns, rows).ToList();
        positions.RemoveAt(positions.Count - 1);
        positions.RemoveAt(0);
        return positions;
    }

    private static IReadOnlyList<(double X, double Y)> CreateCenteredGrid(int columns, int rows)
    {
        return CreateGrid(
            columns,
            rows,
            (7 - columns) / 2d,
            (5 - rows) / 2d);
    }

    private static IReadOnlyList<(double X, double Y)> CreateGrid(
        int columns,
        int rows,
        double offsetX,
        double offsetY)
    {
        var positions = new List<(double X, double Y)>(columns * rows);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                positions.Add((column + offsetX, row + offsetY));
            }
        }

        return positions;
    }

    private static void AddLayer(
        ICollection<TilePlacement> placements,
        Random random,
        ref int nextId,
        int layer,
        IReadOnlyList<(double X, double Y)> positions)
    {
        var shuffledPositions = positions.ToList();
        Shuffle(shuffledPositions, random);
        foreach (var position in shuffledPositions)
        {
            placements.Add(new TilePlacement(nextId++, position.X, position.Y, layer));
        }
    }

    private static IReadOnlyDictionary<int, SignalTileKind> AssignKindsToSolutionOrder(
        IReadOnlyList<TilePlacement> placements,
        Random random)
    {
        var assignments = new Dictionary<int, SignalTileKind>(placements.Count);
        var solutionOrder = placements
            .OrderByDescending(placement => placement.Layer)
            .ThenBy(placement => placement.Id)
            .ToArray();
        var groupKinds = CreateSpacedKindDeck(solutionOrder.Length / 3, random);

        for (var index = 0; index < solutionOrder.Length; index += 3)
        {
            var kind = groupKinds[index / 3];
            assignments[solutionOrder[index].Id] = kind;
            assignments[solutionOrder[index + 1].Id] = kind;
            assignments[solutionOrder[index + 2].Id] = kind;
        }

        return assignments;
    }

    private static IReadOnlyList<SignalTileKind> CreateSpacedKindDeck(int groupCount, Random random)
    {
        var groups = new List<SignalTileKind>(groupCount);
        while (groups.Count < groupCount)
        {
            var deck = TileKinds.ToList();
            Shuffle(deck, random);
            if (groups.Count > 0 && deck[0] == groups[^1])
            {
                (deck[0], deck[1]) = (deck[1], deck[0]);
            }

            groups.AddRange(deck.Take(Math.Min(deck.Count, groupCount - groups.Count)));
        }

        return groups;
    }

    private static IReadOnlyList<SignalTileKind> SpaceRepeatedKinds(
        IReadOnlyList<SignalTileKind> groups,
        Random random)
    {
        var remaining = groups
            .GroupBy(kind => kind)
            .ToDictionary(group => group.Key, group => group.Count());
        var result = new List<SignalTileKind>(groups.Count);
        SignalTileKind? previous = null;
        while (result.Count < groups.Count)
        {
            var candidates = remaining
                .Where(pair => pair.Value > 0 && pair.Key != previous)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(_ => random.Next())
                .ToArray();
            if (candidates.Length == 0)
            {
                candidates = remaining.Where(pair => pair.Value > 0).ToArray();
            }

            var selected = candidates[0].Key;
            result.Add(selected);
            remaining[selected]--;
            previous = selected;
        }

        return result;
    }

    private readonly record struct TilePlacement(int Id, double X, double Y, int Layer);

    private void InsertIntoTray(SignalTileKind kind)
    {
        var lastMatch = _tray.FindLastIndex(candidate => candidate == kind);
        _tray.Insert(lastMatch < 0 ? _tray.Count : lastMatch + 1, kind);
    }

    private static bool Overlaps(LayeredTile first, LayeredTile second)
    {
        const double tileSpan = 1;
        return first.X < second.X + tileSpan
            && first.X + tileSpan > second.X
            && first.Y < second.Y + tileSpan
            && first.Y + tileSpan > second.Y;
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var other = random.Next(index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }
}
