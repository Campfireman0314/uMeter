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

internal readonly record struct ConstructedPathDifficultyMetrics(
    long Score,
    int PeakTrayOccupancy,
    int RiskyDecoyChoices,
    int NewFamilyDecoyChoices);

public sealed class LayeredTileGame
{
    public const int DefaultTrayCapacity = 7;
    private const int BoardCandidateCount = 48;
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
        var bestDifficultyScore = long.MinValue;
        for (var candidate = 0; candidate < BoardCandidateCount; candidate++)
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
            var difficultyScore = AnalyzeConstructedPathDifficulty(tiles).Score;
            if (difficultyScore <= bestDifficultyScore)
            {
                continue;
            }

            bestDifficultyScore = difficultyScore;
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
        for (var layer = 0; layer < SignalShuffleTopology.LayerTileCounts.Count; layer++)
        {
            AddLayer(
                placements,
                random,
                ref nextId,
                layer,
                SignalShuffleTopology.CreateLayer(layer));
        }

        return placements;
    }

    private static ConstructedPathDifficultyMetrics AnalyzeConstructedPathDifficulty(IReadOnlyList<LayeredTile> tiles)
    {
        var removed = new HashSet<int>();
        var tray = new List<SignalTileKind>(DefaultTrayCapacity);
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
        var peakTrayOccupancy = 0;
        var riskyDecoyChoices = 0;
        var newFamilyDecoyChoices = 0;
        for (var index = 0; index < solutionOrder.Length; index++)
        {
            var canonicalTile = solutionOrder[index];
            var selectable = tiles
                .Where(tile => !removed.Contains(tile.Id))
                .Where(tile => blockersByTileId[tile.Id].All(removed.Contains))
                .ToArray();
            var riskyDecoys = 0;
            var newFamilyDecoys = 0;
            var obviousCompletions = 0;
            foreach (var option in selectable)
            {
                if (option.Id == canonicalTile.Id)
                {
                    continue;
                }

                var matchingTrayCount = tray.Count(kind => kind == option.Kind);
                if (matchingTrayCount == 2)
                {
                    obviousCompletions++;
                }
                else
                {
                    if (matchingTrayCount == 0)
                    {
                        newFamilyDecoys++;
                    }

                    if (tray.Count >= DefaultTrayCapacity - 1)
                    {
                        riskyDecoys++;
                    }
                }
            }

            var remainingWeight = solutionOrder.Length - index;
            score += tray.Count * tray.Count * 1000L;
            score += tray.Distinct().Count() * 200L;
            score += riskyDecoys * remainingWeight * 40L;
            score += newFamilyDecoys * remainingWeight * 3L;
            score -= obviousCompletions * remainingWeight * 6L;
            riskyDecoyChoices += riskyDecoys;
            newFamilyDecoyChoices += newFamilyDecoys;

            removed.Add(canonicalTile.Id);
            AddKindToTray(tray, canonicalTile.Kind);
            peakTrayOccupancy = Math.Max(peakTrayOccupancy, tray.Count);
        }

        return new ConstructedPathDifficultyMetrics(
            score,
            peakTrayOccupancy,
            riskyDecoyChoices,
            newFamilyDecoyChoices);
    }

    internal ConstructedPathDifficultyMetrics ConstructedPathDifficulty => AnalyzeConstructedPathDifficulty(_tiles);

    internal int MaximumImmediateLowerLayerFanOut => _tiles.Max(upper =>
        _tiles.Count(lower => lower.Layer == upper.Layer - 1 && Overlaps(lower, upper)));

    internal int MultiRouteBlockerCount => _tiles.Count(upper =>
        _tiles.Count(lower => lower.Layer == upper.Layer - 1 && Overlaps(lower, upper)) >= 2);

    internal int FractionallyOffsetTileCount => _tiles.Count(tile =>
        Math.Abs(tile.X - Math.Round(tile.X)) > 0.001
        || Math.Abs(tile.Y - Math.Round(tile.Y)) > 0.001);

    private static void AddKindToTray(List<SignalTileKind> tray, SignalTileKind kind)
    {
        var lastMatch = tray.FindLastIndex(candidate => candidate == kind);
        tray.Insert(lastMatch < 0 ? tray.Count : lastMatch + 1, kind);
        if (tray.Count(candidate => candidate == kind) == 3)
        {
            tray.RemoveAll(candidate => candidate == kind);
        }
    }

    internal static LayeredTileGame CreateForTesting(IEnumerable<LayeredTile> tiles, int trayCapacity = DefaultTrayCapacity)
    {
        return new LayeredTileGame(tiles, trayCapacity, new Random(0));
    }

    internal static LayeredTile CreateTileForTesting(int id, SignalTileKind kind, double x, double y, int layer)
    {
        return new LayeredTile(id, kind, x, y, layer);
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
        var solutionGroups = CreateInterleavedSolutionGroups(solutionOrder, random);
        var groupKinds = CreateSpacedKindDeck(solutionGroups.Count, random);

        for (var groupIndex = 0; groupIndex < solutionGroups.Count; groupIndex++)
        {
            foreach (var placement in solutionGroups[groupIndex])
            {
                assignments[placement.Id] = groupKinds[groupIndex];
            }
        }

        return assignments;
    }

    private static IReadOnlyList<TilePlacement[]> CreateInterleavedSolutionGroups(
        IReadOnlyList<TilePlacement> solutionOrder,
        Random random)
    {
        if (solutionOrder.Count % 3 != 0)
        {
            throw new InvalidOperationException("The solution order must contain complete triples.");
        }

        var groups = new List<TilePlacement[]>(solutionOrder.Count / 3)
        {
            solutionOrder.Take(3).ToArray()
        };
        var index = 3;
        while (solutionOrder.Count - index >= 9)
        {
            var block = solutionOrder.Skip(index).Take(9).ToArray();
            var interleaved = new[]
            {
                new List<TilePlacement>(3),
                new List<TilePlacement>(3),
                new List<TilePlacement>(3)
            };
            for (var pass = 0; pass < 3; pass++)
            {
                var groupOrder = new List<int> { 0, 1, 2 };
                Shuffle(groupOrder, random);
                for (var slot = 0; slot < 3; slot++)
                {
                    interleaved[groupOrder[slot]].Add(block[(pass * 3) + slot]);
                }
            }

            groups.AddRange(interleaved.Select(group => group.ToArray()));
            index += 9;
        }

        if (index < solutionOrder.Count)
        {
            groups.Add(solutionOrder.Skip(index).ToArray());
        }

        return groups;
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
        return Overlaps(first.X, first.Y, second.X, second.Y);
    }


    private static bool Overlaps(double firstX, double firstY, double secondX, double secondY)
    {
        const double tileSpan = 1;
        return firstX < secondX + tileSpan
            && firstX + tileSpan > secondX
            && firstY < secondY + tileSpan
            && firstY + tileSpan > secondY;
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
