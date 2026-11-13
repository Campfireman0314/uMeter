namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// Bounded raw spectrum history: one column of source bucket levels per retained
/// time step. Stored values are the analyzer's normalized display levels, where
/// zero means at or below the -200 dBFS analyzer floor and one means at or above
/// the 0 dBFS ceiling. Colors, display floor/ceiling, and row reduction are
/// applied later, so a numeric readout never depends on them.
/// </summary>
internal sealed class SpectrumHistoryLevels
{
    private double[] _levels = [];

    public int ColumnCount { get; private set; }

    public int BucketCount { get; private set; }

    public long ByteSize => (long)_levels.Length * sizeof(double);

    public void Reset(int columnCount, int bucketCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columnCount);
        ArgumentOutOfRangeException.ThrowIfNegative(bucketCount);

        var length = columnCount * bucketCount;
        if (_levels.Length != length)
        {
            _levels = length > 0 ? new double[length] : [];
        }
        else
        {
            Array.Clear(_levels);
        }

        ColumnCount = columnCount;
        BucketCount = bucketCount;
    }

    public void Clear()
    {
        Array.Clear(_levels);
    }

    public void WriteColumn(int column, IReadOnlyList<double> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        if (column < 0 || column >= ColumnCount || BucketCount <= 0)
        {
            return;
        }

        var start = column * BucketCount;
        var count = Math.Min(levels.Count, BucketCount);
        for (var bucket = 0; bucket < BucketCount; bucket++)
        {
            _levels[start + bucket] = bucket < count ? Normalize(levels[bucket]) : 0;
        }
    }

    public bool TryReadLevel(int column, int bucket, out double level)
    {
        level = 0;
        if (column < 0 || column >= ColumnCount || bucket < 0 || bucket >= BucketCount)
        {
            return false;
        }

        level = _levels[(column * BucketCount) + bucket];
        return true;
    }

    /// <summary>
    /// Strongest level of the source buckets that a displayed row represents.
    /// Display replay uses this so a theme or range change recolors the retained
    /// measurement exactly as the original column write did.
    /// </summary>
    public double ReadRowLevel(int column, int row, int rowCount)
    {
        if (column < 0 || column >= ColumnCount || BucketCount <= 0)
        {
            return 0;
        }

        var start = column * BucketCount;
        return SpectrumRollColumnRasterizer.RowValue(
            _levels.AsSpan(start, BucketCount),
            row,
            rowCount);
    }

    private static double Normalize(double level)
    {
        return double.IsFinite(level) ? Math.Clamp(level, 0, 1) : double.NaN;
    }
}
