using DesktopLoudnessMeter.App.Controls;
using System.Windows.Media;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumHistoryLevelsTests
{
    [Fact]
    public void WrittenColumnsKeepRawSourceBucketLevels()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 4, bucketCount: 3);

        levels.WriteColumn(1, [0.25, 0.5, 0.75]);

        Assert.True(levels.TryReadLevel(1, 0, out var low));
        Assert.True(levels.TryReadLevel(1, 1, out var middle));
        Assert.True(levels.TryReadLevel(1, 2, out var high));
        Assert.Equal(0.25, low, precision: 12);
        Assert.Equal(0.5, middle, precision: 12);
        Assert.Equal(0.75, high, precision: 12);
    }

    [Fact]
    public void BucketLevelsAreClampedToTheAnalyzerRange()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 2, bucketCount: 2);

        levels.WriteColumn(0, [-0.5, 1.5]);

        Assert.True(levels.TryReadLevel(0, 0, out var belowFloor));
        Assert.True(levels.TryReadLevel(0, 1, out var aboveCeiling));
        Assert.Equal(0, belowFloor);
        Assert.Equal(1, aboveCeiling);
    }

    [Fact]
    public void NonFiniteLevelsAreReadableButDoNotPoisonRowAggregation()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 2, bucketCount: 2);

        levels.WriteColumn(0, [double.NaN, 0.5]);

        Assert.True(levels.TryReadLevel(0, 0, out var invalid));
        Assert.True(double.IsNaN(invalid));
        Assert.Equal(0.5, levels.ReadRowLevel(0, row: 0, rowCount: 2), precision: 12);
    }

    [Fact]
    public void ResetForChangedSourceBucketCountDropsOldLevels()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 4, bucketCount: 4);
        levels.WriteColumn(0, [0.1, 0.2, 0.3, 0.4]);

        levels.Reset(columnCount: 4, bucketCount: 2);

        Assert.Equal(2, levels.BucketCount);
        Assert.False(levels.TryReadLevel(0, 3, out _));
        Assert.True(levels.TryReadLevel(0, 1, out var rebuilt));
        Assert.Equal(0, rebuilt);
    }

    [Fact]
    public void ClearDropsRetainedLevelsButKeepsShape()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 4, bucketCount: 2);
        levels.WriteColumn(2, [0.6, 0.7]);
        var byteSize = levels.ByteSize;

        levels.Clear();

        Assert.Equal(byteSize, levels.ByteSize);
        Assert.True(levels.TryReadLevel(2, 0, out var cleared));
        Assert.Equal(0, cleared);
    }

    [Fact]
    public void RowLevelUsesStrongestBucketInsideTheRenderedRow()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 2, bucketCount: 320);
        var buckets = new double[320];
        buckets[160] = 0.25;
        buckets[161] = 0.75;
        levels.WriteColumn(0, buckets);

        Assert.True(levels.TryReadLevel(0, 160, out var nearest));
        Assert.Equal(0.25, nearest, precision: 12);
        Assert.Equal(0.75, levels.ReadRowLevel(0, row: 71, rowCount: 144), precision: 12);
    }

    [Fact]
    public void OutOfRangeReadsReportNoLevel()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 2, bucketCount: 2);

        Assert.False(levels.TryReadLevel(2, 0, out _));
        Assert.False(levels.TryReadLevel(0, 2, out _));
        Assert.False(levels.TryReadLevel(-1, 0, out _));
    }

    [Fact]
    public void ReplayedRowLevelsReproduceTheRenderedColumn()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(columnCount: 2, bucketCount: 320);
        var buckets = new double[320];
        for (var bucket = 0; bucket < buckets.Length; bucket += 7)
        {
            buckets[bucket] = (bucket % 11) / 10d;
        }

        buckets[3] = 1.5;
        buckets[5] = double.NaN;
        levels.WriteColumn(0, buckets);

        var transform = SpectrumRollColumnRasterizer.CreateDisplayTransform(-96, -6);
        var rendered = new int[144];
        SpectrumRollColumnRasterizer.WriteColumn(buckets, rendered, transform, Colors.Cyan, Colors.Orange);

        for (var row = 0; row < rendered.Length; row++)
        {
            Assert.Equal(
                rendered[row],
                SpectrumRollColumnRasterizer.LevelPixel(
                    levels.ReadRowLevel(0, row, rendered.Length),
                    transform,
                    Colors.Cyan,
                    Colors.Orange));
        }
    }

    [Fact]
    public void RetainedHistoryIsBoundedByMaximumColumnsAndSourceBuckets()
    {
        var levels = new SpectrumHistoryLevels();
        levels.Reset(
            columnCount: SpectrumRollResolution.MaximumHistoryColumns,
            bucketCount: 320);

        Assert.Equal(
            SpectrumRollResolution.MaximumHistoryColumns * 320 * sizeof(double),
            levels.ByteSize);
    }
}
