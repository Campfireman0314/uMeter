namespace DesktopLoudnessMeter.App.Game;

internal static class SignalShuffleTopology
{
    private const double AnchorPitch = 0.94;
    private const double StackFanPerLayer = 0.035;

    // Each digit is the highest layer present at that anchor. The shared map keeps
    // every layer on the same shelves, rails, bridge, waist, and lower basin.
    private static readonly string[] DepthMap =
    [
        ".88..88.",
        ".44..44.",
        "23....32",
        "34....43",
        "45555554",
        ".23..32.",
        ".2....2.",
        "12....21",
        "23444432",
        ".234432."
    ];

    internal static IReadOnlyList<int> LayerTileCounts { get; } =
        [48, 48, 46, 34, 24, 10, 4, 4, 4];

    internal static IReadOnlyList<(double X, double Y)> CreateLayer(int layer)
    {
        if (layer < 0 || layer >= LayerTileCounts.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(layer));
        }

        var positions = new List<(double X, double Y)>(LayerTileCounts[layer]);
        for (var row = 0; row < DepthMap.Length; row++)
        {
            for (var column = 0; column < DepthMap[row].Length; column++)
            {
                var marker = DepthMap[row][column];
                if (marker == '.' || marker - '0' < layer)
                {
                    continue;
                }

                positions.Add(((column * AnchorPitch) + StackFanOffset(column, layer), row * AnchorPitch));
            }
        }

        if (positions.Count != LayerTileCounts[layer])
        {
            throw new InvalidOperationException(
                $"Signal Shuffle layer {layer} generated {positions.Count} positions instead of {LayerTileCounts[layer]}.");
        }

        return positions;
    }

    private static double StackFanOffset(int column, int layer)
    {
        if (column <= 3)
        {
            return layer * StackFanPerLayer;
        }

        return -layer * StackFanPerLayer;
    }
}
