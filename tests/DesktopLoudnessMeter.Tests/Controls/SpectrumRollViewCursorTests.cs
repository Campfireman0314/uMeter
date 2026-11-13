using System.Windows;
using System.Windows.Threading;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

/// <summary>
/// Exercises the deferred spectrum roll through the same readout path the
/// renderer uses, so the hover level is verified against real control state
/// rather than an isolated copy of the mapping math.
/// </summary>
public sealed class SpectrumRollViewCursorTests
{
    private const int SourceBucketCount = 320;

    [Fact]
    public void DeferredHistoryColumnExposesTheRetainedSourceBucketLevel()
    {
        SpectrumCursorReadout? readout = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var buckets = new double[SourceBucketCount];
            buckets[160] = 0.5;
            buckets[161] = 0.9;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            readout = view.ResolveCursorReadout(
                new Point(plot.Right - 0.5, BucketY(plot, 160, SourceBucketCount)));
        });

        Assert.NotNull(readout);
        Assert.Equal(SpectrumCursorLevelState.Measured, readout.Value.LevelState);
        Assert.Equal(-100.0, readout.Value.LevelDbfs, precision: 6);
        Assert.Equal("-100.0 dBFS", readout.Value.LevelText);
        Assert.Equal(160, readout.Value.SourceBucket);
        Assert.True(readout.Value.HistoryColumn >= 0);
    }

    [Fact]
    public void UnfilledHistoryReportsNoFabricatedLevel()
    {
        SpectrumCursorReadout? readout = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var buckets = new double[SourceBucketCount];
            buckets[160] = 0.5;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            readout = view.ResolveCursorReadout(
                new Point(plot.Left + 1, BucketY(plot, 160, SourceBucketCount)));
        });

        Assert.NotNull(readout);
        Assert.Equal(SpectrumCursorLevelState.Unavailable, readout.Value.LevelState);
        Assert.Equal("—", readout.Value.LevelText);
        Assert.Equal(-1, readout.Value.HistoryColumn);
        Assert.NotEqual(string.Empty, readout.Value.FrequencyText);
    }

    [Fact]
    public void DisplayRangeChangesKeepTheRetainedNumericReadout()
    {
        SpectrumCursorReadout? warm = null;
        SpectrumCursorReadout? dimmed = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var buckets = new double[SourceBucketCount];
            buckets[160] = 0.25;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            var hover = new Point(plot.Right - 0.5, BucketY(plot, 160, SourceBucketCount));
            warm = view.ResolveCursorReadout(hover);

            view.DisplayFloorDb = -60;
            view.DisplayCeilingDb = -3;
            Pump();
            dimmed = view.ResolveCursorReadout(hover);
        });

        Assert.NotNull(warm);
        Assert.NotNull(dimmed);
        Assert.Equal(warm.Value.LevelDbfs, dimmed.Value.LevelDbfs, precision: 9);
        Assert.Equal(warm.Value.LevelText, dimmed.Value.LevelText);
        Assert.Equal(warm.Value.SourceBucket, dimmed.Value.SourceBucket);
        Assert.Equal(warm.Value.HistoryColumn, dimmed.Value.HistoryColumn);
        Assert.Equal(warm.Value.FrequencyHz, dimmed.Value.FrequencyHz, precision: 9);
    }

    [Fact]
    public void ClearedHistoryReportsNoLevel()
    {
        SpectrumCursorReadout? readout = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var buckets = new double[SourceBucketCount];
            buckets[160] = 0.5;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();

            view.SpectrumBuckets = Array.Empty<double>();
            view.PlotTick = 2;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            readout = view.ResolveCursorReadout(
                new Point(plot.Right - 0.5, BucketY(plot, 160, SourceBucketCount)));
        });

        Assert.NotNull(readout);
        Assert.Equal(SpectrumCursorLevelState.Unavailable, readout.Value.LevelState);
        Assert.Equal(-1, readout.Value.HistoryColumn);
    }

    [Fact]
    public void ChangedSourceBucketCountStopsReportingPreviousLevels()
    {
        SpectrumCursorReadout? readout = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var wide = new double[SourceBucketCount];
            wide[160] = 0.5;
            view.SpectrumBuckets = wide;
            view.PlotTick = 1;
            Pump();

            var narrow = new double[160];
            narrow[80] = 0.25;
            view.SpectrumBuckets = narrow;
            view.PlotTick = 2;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            readout = view.ResolveCursorReadout(
                new Point(plot.Right - 0.5, BucketY(plot, 80, 160)));
        });

        Assert.NotNull(readout);
        Assert.Equal(SpectrumCursorLevelState.Measured, readout.Value.LevelState);
        Assert.Equal(-150.0, readout.Value.LevelDbfs, precision: 6);
        Assert.Equal(80, readout.Value.SourceBucket);
    }

    [Fact]
    public void HoverOutsideThePlotProducesNoReadout()
    {
        SpectrumCursorReadout? readout = null;
        var resolved = true;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            view.SpectrumBuckets = new double[SourceBucketCount];
            view.PlotTick = 1;
            Pump();

            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            readout = view.ResolveCursorReadout(new Point(plot.Left - 20, plot.Top + 1));
            resolved = readout is not null;
        });

        Assert.False(resolved);
        Assert.Null(readout);
    }

    [Fact]
    public void WrappedHistoryReadsNewestAtRightAndOldestAtLeft()
    {
        SpectrumCursorReadout? newest = null;
        SpectrumCursorReadout? oldest = null;
        double expectedNewest = 0;
        double expectedOldest = 0;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            var historyColumns = SpectrumRollResolution.ForPlotSize(plot.Width, plot.Height, SourceBucketCount)
                .HistoryColumns;
            var appendCount = historyColumns + 2;
            var buckets = new double[SourceBucketCount];
            for (var append = 1; append <= appendCount; append++)
            {
                buckets[160] = 0.1 + (append * 0.0005);
                view.SpectrumBuckets = buckets;
                view.PlotTick = append;
                Pump();
            }

            // Column zero is written by append 1 and then overwritten by
            // append historyColumns + 1, so the oldest retained column holds the
            // level from append 3.
            expectedOldest = 0.1 + (3 * 0.0005);
            expectedNewest = 0.1 + (appendCount * 0.0005);
            var hoverY = BucketY(plot, 160, SourceBucketCount);
            newest = view.ResolveCursorReadout(new Point(plot.Right - 0.5, hoverY));
            oldest = view.ResolveCursorReadout(new Point(plot.Left + 0.5, hoverY));
        });

        Assert.NotNull(newest);
        Assert.NotNull(oldest);
        Assert.Equal(SpectrumCursorLevelState.Measured, newest.Value.LevelState);
        Assert.Equal(SpectrumCursorLevelState.Measured, oldest.Value.LevelState);
        Assert.Equal(-200 + (200 * expectedNewest), newest.Value.LevelDbfs, precision: 6);
        Assert.Equal(-200 + (200 * expectedOldest), oldest.Value.LevelDbfs, precision: 6);
        Assert.NotEqual(newest.Value.HistoryColumn, oldest.Value.HistoryColumn);
    }

    [Fact]
    public void FloorAndCeilingLevelsAreIndicatedInTheControl()
    {
        SpectrumCursorReadout? floor = null;
        SpectrumCursorReadout? ceiling = null;
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var plot = SpectrumRollView.GetPlotRect(view.ActualWidth, view.ActualHeight);
            var buckets = new double[SourceBucketCount];
            buckets[160] = 0;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();
            var hoverY = BucketY(plot, 160, SourceBucketCount);
            var hover = new Point(plot.Right - 0.5, hoverY);
            floor = view.ResolveCursorReadout(hover);

            buckets[160] = 1;
            view.SpectrumBuckets = buckets;
            view.PlotTick = 2;
            Pump();
            ceiling = view.ResolveCursorReadout(hover);
        });

        Assert.NotNull(floor);
        Assert.NotNull(ceiling);
        Assert.Equal(SpectrumCursorLevelState.AtFloor, floor.Value.LevelState);
        Assert.Equal("≤ -200 dBFS", floor.Value.LevelText);
        Assert.Equal(SpectrumCursorLevelState.AtCeiling, ceiling.Value.LevelState);
        Assert.Equal("≥ 0 dBFS", ceiling.Value.LevelText);
    }

    [Fact]
    public void DisplayRangeChangeRecolorsRetainedHistoryFromRawLevels()
    {
        byte[] warm = [];
        byte[] dimmed = [];
        StaControlHost.Run(() =>
        {
            var view = new SpectrumRollView();
            StaControlHost.Arrange(view, 320, 160);
            using var host = StaControlHost.CreateFocusHost(view);
            var buckets = new double[SourceBucketCount];
            for (var bucket = 120; bucket < 200; bucket++)
            {
                buckets[bucket] = 0.4 + ((bucket % 5) * 0.05);
            }

            view.SpectrumBuckets = buckets;
            view.PlotTick = 1;
            Pump();
            warm = StaControlHost.RenderPixels(view);

            view.DisplayFloorDb = -120;
            view.DisplayCeilingDb = 0;
            Pump();
            dimmed = StaControlHost.RenderPixels(view);
        });

        Assert.NotEmpty(warm);
        Assert.Equal(warm.Length, dimmed.Length);
        Assert.False(warm.SequenceEqual(dimmed), "Changing the display range must recolor the retained history.");
        Assert.Contains(warm, value => value != 0);
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            (Action)(() => frame.Continue = false),
            DispatcherPriority.ContextIdle);
        Dispatcher.PushFrame(frame);
    }

    private static double BucketY(Rect plot, int bucket, int bucketCount)
    {
        var ratio = (double)bucket / (bucketCount - 1);
        return plot.Bottom - (plot.Height * ratio);
    }
}
