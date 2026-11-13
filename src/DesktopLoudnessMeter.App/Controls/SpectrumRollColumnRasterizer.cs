using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

internal static class SpectrumRollColumnRasterizer
{
    public const double AnalyzerFloorDb = -200;
    public const double AnalyzerCeilingDb = 0;
    public const double AnalyzerRangeDb = AnalyzerCeilingDb - AnalyzerFloorDb;
    public const double DefaultDisplayFloorDb = -96;
    public const double DefaultDisplayCeilingDb = -6;
    public const double MinimumDisplayRangeDb = 1;
    public const double SoftFloorKneeDb = 9;
    public const double SoftFloorMaximumDisplayValue = 0.16;

    public static DisplayTransform CreateDisplayTransform(double floorDb, double ceilingDb)
    {
        var floorLevel = Math.Clamp((floorDb - AnalyzerFloorDb) / AnalyzerRangeDb, 0, 1);
        var ceilingLevel = Math.Clamp((ceilingDb - AnalyzerFloorDb) / AnalyzerRangeDb, floorLevel + double.Epsilon, 1);
        var softFloorLevel = Math.Clamp(((floorDb - SoftFloorKneeDb) - AnalyzerFloorDb) / AnalyzerRangeDb, 0, floorLevel);
        return new DisplayTransform(softFloorLevel, floorLevel, ceilingLevel - floorLevel);
    }

    public static void WriteColumn(
        IReadOnlyList<double> buckets,
        Span<int> pixels,
        DisplayTransform transform,
        Color accent,
        Color warm)
    {
        pixels.Clear();
        for (var row = 0; row < pixels.Length; row++)
        {
            var value = RowValue(buckets, row, pixels.Length);
            pixels[row] = LevelPixel(value, transform, accent, warm);
        }
    }

    /// <summary>
    /// Source bucket range rendered by one displayed row. The roll keeps the
    /// strongest bucket of the range, so row rendering and any later replay from
    /// retained raw buckets share this single mapping.
    /// </summary>
    public static (int Low, int High) RowBucketRange(int row, int rowCount, int bucketCount)
    {
        if (bucketCount <= 0 || rowCount <= 0)
        {
            return (0, 0);
        }

        var low = (rowCount - 1 - row) * bucketCount / rowCount;
        var high = Math.Max(low + 1, (rowCount - row) * bucketCount / rowCount);
        return (low, Math.Min(high, bucketCount));
    }

    public static double RowValue(ReadOnlySpan<double> columnLevels, int row, int rowCount)
    {
        var (low, high) = RowBucketRange(row, rowCount, columnLevels.Length);
        var strongest = 0d;
        for (var bucket = low; bucket < high; bucket++)
        {
            var level = columnLevels[bucket];
            if (double.IsFinite(level))
            {
                strongest = Math.Max(strongest, Math.Clamp(level, 0, 1));
            }
        }

        return strongest;
    }

    public static int LevelPixel(double value, DisplayTransform transform, Color accent, Color warm)
    {
        var displayValue = DisplayValue(value, transform);
        return displayValue > 0
            ? ColorToBgra(ThemedDrawing.SpectrumPixel(accent, warm, displayValue))
            : 0;
    }

    private static double DisplayValue(double normalizedLevel, DisplayTransform transform)
    {
        if (normalizedLevel <= 0)
        {
            return 0;
        }

        var level = Math.Clamp(normalizedLevel, 0, 1);
        if (level <= transform.FloorLevel)
        {
            var softRatio = level / Math.Max(transform.FloorLevel, double.Epsilon);
            return Math.Clamp(softRatio * softRatio * SoftFloorMaximumDisplayValue, 0, SoftFloorMaximumDisplayValue);
        }

        var visibleRatio = Math.Clamp((level - transform.FloorLevel) / transform.LevelRange, 0, 1);
        return Math.Clamp(SoftFloorMaximumDisplayValue + ((1 - SoftFloorMaximumDisplayValue) * visibleRatio), 0, 1);
    }

    private static int ColorToBgra(Color color)
    {
        return color.B | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }

    private static double RowValue(IReadOnlyList<double> buckets, int row, int rowCount)
    {
        if (buckets.Count == 0 || rowCount <= 0)
        {
            return 0;
        }

        var (lowBucket, highBucket) = RowBucketRange(row, rowCount, buckets.Count);
        var strongest = 0d;
        for (var bucket = lowBucket; bucket < highBucket; bucket++)
        {
            var level = buckets[bucket];
            if (double.IsFinite(level))
            {
                strongest = Math.Max(strongest, Math.Clamp(level, 0, 1));
            }
        }

        return strongest;
    }
}

internal readonly record struct DisplayTransform(double SoftFloorLevel, double FloorLevel, double LevelRange);
