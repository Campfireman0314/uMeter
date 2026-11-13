using System.Windows;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// Describes the retained spectrum roll layout needed to resolve a hovered
/// position back to a retained history column and source frequency bucket.
/// </summary>
internal readonly record struct SpectrumHistoryLayout(
    int HistoryColumns,
    int SourceBucketCount,
    int FilledColumns,
    int NextColumnIndex,
    double DpiScaleX);

internal readonly record struct SpectrumHistoryTarget(
    double FrequencyHz,
    int SourceBucket,
    int HistoryColumn);

/// <summary>
/// Maps a pointer position onto the displayed frequency and the retained history
/// column drawn at that horizontal position. Column resolution mirrors the
/// rendered segments, including the device-pixel-snapped wrap seam, so the
/// readout always describes the column the user is pointing at.
/// </summary>
internal static class SpectrumHistoryCursorMapper
{
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
    private static readonly double MelSpan = MaximumMel - MinimumMel;

    public static SpectrumHistoryTarget? Map(
        Point position,
        Rect plot,
        double viewportMinimumMel,
        double viewportMelSpan,
        SpectrumHistoryLayout layout)
    {
        if (SpectrumFrequencyHoverMapper.Map(position, plot, viewportMinimumMel, viewportMelSpan) is not { } frequency)
        {
            return null;
        }

        return new SpectrumHistoryTarget(
            frequency,
            NearestSourceBucket(frequency, layout.SourceBucketCount),
            ResolveHistoryColumn(position.X, plot, layout));
    }

    public static int NearestSourceBucket(double frequencyHz, int sourceBucketCount)
    {
        if (sourceBucketCount <= 0)
        {
            return -1;
        }

        if (sourceBucketCount == 1)
        {
            return 0;
        }

        var mel = Math.Clamp(SpectrumScale.HertzToMel(frequencyHz), MinimumMel, MaximumMel);
        var ratio = Math.Clamp((mel - MinimumMel) / MelSpan, 0, 1);
        return (int)Math.Round(ratio * (sourceBucketCount - 1), MidpointRounding.AwayFromZero);
    }

    private static int ResolveHistoryColumn(double x, Rect plot, in SpectrumHistoryLayout layout)
    {
        if (layout.HistoryColumns <= 0
            || layout.FilledColumns <= 0
            || plot.Width <= 0
            || !double.IsFinite(x)
            || x < plot.Left
            || x > plot.Right)
        {
            return -1;
        }

        var filledColumns = Math.Min(layout.FilledColumns, layout.HistoryColumns);
        if (filledColumns < layout.HistoryColumns)
        {
            var width = plot.Width * filledColumns / layout.HistoryColumns;
            var left = plot.Right - width;
            return x < left
                ? -1
                : ColumnFromSegment(x, left, width, 0, filledColumns);
        }

        var nextColumnIndex = ((layout.NextColumnIndex % layout.HistoryColumns) + layout.HistoryColumns)
            % layout.HistoryColumns;
        if (nextColumnIndex == 0)
        {
            return ColumnFromSegment(x, plot.Left, plot.Width, 0, layout.HistoryColumns);
        }

        var firstSourceWidth = layout.HistoryColumns - nextColumnIndex;
        var (first, second) = SpectrumRollBitmapSegmentPlan.SplitWrappedDestination(
            new Rect(plot.Left, plot.Top, plot.Width, plot.Height),
            firstSourceWidth,
            nextColumnIndex,
            layout.DpiScaleX);

        return x < first.Right
            ? ColumnFromSegment(x, first.Left, first.Width, nextColumnIndex, firstSourceWidth)
            : ColumnFromSegment(x, second.Left, second.Width, 0, nextColumnIndex);
    }

    private static int ColumnFromSegment(double x, double left, double width, int sourceX, int sourceWidth)
    {
        if (width <= 0 || sourceWidth <= 0)
        {
            return -1;
        }

        var ratio = Math.Clamp((x - left) / width, 0, 1);
        var offset = (int)Math.Floor(ratio * sourceWidth);
        return sourceX + Math.Clamp(offset, 0, sourceWidth - 1);
    }
}
