using System.Windows;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumHistoryCursorMapperTests
{
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);

    [Fact]
    public void PartialHistoryReadsOnlyFilledTrailingColumns()
    {
        var plot = new Rect(0, 0, 80, 100);
        var layout = new SpectrumHistoryLayout(
            HistoryColumns: 8,
            SourceBucketCount: 4,
            FilledColumns: 4,
            NextColumnIndex: 4,
            DpiScaleX: 1.0);

        Assert.Equal(0, MapColumn(new Point(45, 50), plot, layout));
        Assert.Equal(3, MapColumn(new Point(79, 50), plot, layout));
        Assert.Equal(-1, MapColumn(new Point(20, 50), plot, layout));
    }

    [Fact]
    public void WrappedHistoryReadsOldestAtLeftAndNewestAtRight()
    {
        var plot = new Rect(0, 0, 80, 100);
        var layout = FullLayout(plot, historyColumns: 8, nextColumnIndex: 3);

        Assert.Equal(3, MapColumn(new Point(5, 50), plot, layout));
        Assert.Equal(7, MapColumn(new Point(49.5, 50), plot, layout));
        Assert.Equal(0, MapColumn(new Point(55, 50), plot, layout));
        Assert.Equal(2, MapColumn(new Point(79, 50), plot, layout));
    }

    [Fact]
    public void FullHistoryReadsFirstAndLastColumns()
    {
        var plot = new Rect(0, 0, 80, 100);
        var layout = FullLayout(plot, historyColumns: 8, nextColumnIndex: 0);

        Assert.Equal(0, MapColumn(new Point(5, 50), plot, layout));
        Assert.Equal(7, MapColumn(new Point(79, 50), plot, layout));
    }

    [Theory]
    [InlineData(1.0, 56.0)]
    [InlineData(1.25, 56.0)]
    [InlineData(1.5, 56.0)]
    [InlineData(2.0, 56.5)]
    public void WrappedHistoryFollowsDeviceSnappedSeam(double dpiScaleX, double expectedSeam)
    {
        var plot = new Rect(0, 0, 90, 100);
        var layout = FullLayout(plot, historyColumns: 8, nextColumnIndex: 3, dpiScaleX);
        var (first, _) = SpectrumRollBitmapSegmentPlan.SplitWrappedDestination(
            new Rect(plot.Left, plot.Top, plot.Width, plot.Height),
            firstSourceWidth: 5,
            secondSourceWidth: 3,
            dpiScaleX);

        Assert.Equal(expectedSeam, first.Right, precision: 6);
        Assert.Equal(7, MapColumn(new Point(first.Right - 0.01, 50), plot, layout));
        Assert.Equal(0, MapColumn(new Point(first.Right + 0.01, 50), plot, layout));
    }

    [Fact]
    public void WrappedHistoryUsesSnappedSeamInsteadOfExactColumnBoundary()
    {
        var plot = new Rect(0, 0, 90, 100);
        var layout = FullLayout(plot, historyColumns: 8, nextColumnIndex: 3, dpiScaleX: 2.0);

        Assert.Equal(7, MapColumn(new Point(56.2, 50), plot, layout));
        Assert.Equal(0, MapColumn(new Point(56.6, 50), plot, layout));
    }

    [Fact]
    public void NearestSourceBucketFollowsDisplayedMelScale()
    {
        var plot = new Rect(0, 0, 100, 100);
        var layout = FullLayout(plot, historyColumns: 4, nextColumnIndex: 0);

        var bucket160 = SpectrumHistoryCursorMapper.Map(
            new Point(50, BucketY(plot, 160, 320)),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout with { SourceBucketCount = 320 });

        var bucket161 = SpectrumHistoryCursorMapper.Map(
            new Point(50, BucketY(plot, 161, 320)),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout with { SourceBucketCount = 320 });

        Assert.NotNull(bucket160);
        Assert.NotNull(bucket161);
        Assert.Equal(160, bucket160.Value.SourceBucket);
        Assert.Equal(161, bucket161.Value.SourceBucket);
        Assert.InRange(bucket160.Value.FrequencyHz, 3355, 3375);
    }

    [Fact]
    public void ZoomedViewportMapsFrequencyAndBucketInsideZoomedRange()
    {
        var plot = new Rect(0, 0, 100, 100);
        var layout = FullLayout(plot, historyColumns: 4, nextColumnIndex: 0) with { SourceBucketCount = 320 };
        var minimumMel = SpectrumScale.HertzToMel(1000);
        var span = SpectrumScale.HertzToMel(8000) - minimumMel;

        var target = SpectrumHistoryCursorMapper.Map(
            new Point(50, plot.Bottom - (plot.Height / 2)),
            plot,
            minimumMel,
            span,
            layout);

        Assert.NotNull(target);
        Assert.InRange(target.Value.FrequencyHz, 3100, 3200);
        Assert.InRange(target.Value.SourceBucket, 150, 160);
    }

    [Fact]
    public void HoverOutsidePlotOrWithoutHistoryHasNoColumn()
    {
        var plot = new Rect(10, 10, 80, 100);
        var layout = new SpectrumHistoryLayout(8, 4, 0, 0, 1.0);

        Assert.Null(SpectrumHistoryCursorMapper.Map(
            new Point(5, 50),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout));

        var emptyHistory = SpectrumHistoryCursorMapper.Map(
            new Point(50, 50),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout);

        Assert.NotNull(emptyHistory);
        Assert.Equal(-1, emptyHistory.Value.HistoryColumn);
        Assert.InRange(emptyHistory.Value.SourceBucket, 0, 3);

        var noColumns = SpectrumHistoryCursorMapper.Map(
            new Point(50, 50),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout with { HistoryColumns = 0, FilledColumns = 0 });

        Assert.NotNull(noColumns);
        Assert.Equal(-1, noColumns.Value.HistoryColumn);
    }

    [Fact]
    public void MissingSourceBucketsProduceNoBucketIndex()
    {
        var plot = new Rect(0, 0, 80, 100);
        var layout = new SpectrumHistoryLayout(8, 0, 8, 0, 1.0);

        var target = SpectrumHistoryCursorMapper.Map(
            new Point(40, 50),
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout);

        Assert.NotNull(target);
        Assert.Equal(-1, target.Value.SourceBucket);
    }

    private static int MapColumn(Point position, Rect plot, SpectrumHistoryLayout layout)
    {
        var target = SpectrumHistoryCursorMapper.Map(
            position,
            plot,
            MinimumMel,
            MaximumMel - MinimumMel,
            layout);

        Assert.NotNull(target);
        return target.Value.HistoryColumn;
    }

    private static SpectrumHistoryLayout FullLayout(
        Rect plot,
        int historyColumns,
        int nextColumnIndex,
        double dpiScaleX = 1.0)
    {
        return new SpectrumHistoryLayout(historyColumns, 4, historyColumns, nextColumnIndex, dpiScaleX);
    }

    private static double BucketY(Rect plot, int bucket, int bucketCount)
    {
        var ratio = (double)bucket / (bucketCount - 1);
        return plot.Bottom - (plot.Height * ratio);
    }
}
