namespace DesktopLoudnessMeter.App.Controls;

internal readonly record struct SpectrumRollResolution(int HistoryColumns, int FrequencyRows)
{
    public const int MinimumHistoryColumns = 120;
    public const int MaximumHistoryColumns = 480;
    public const int MinimumFrequencyRows = 96;
    private const int ResolutionStep = 16;
    private const double ColumnsPerDeviceIndependentPixel = 0.75;
    private const double RowsPerDeviceIndependentPixel = 1.0;

    public static SpectrumRollResolution ForPlotSize(
        double plotWidth,
        double plotHeight,
        int sourceBucketCount)
    {
        var maxFrequencyRows = Math.Max(1, sourceBucketCount);
        var minFrequencyRows = Math.Min(MinimumFrequencyRows, maxFrequencyRows);
        if (!double.IsFinite(plotWidth)
            || !double.IsFinite(plotHeight)
            || plotWidth <= 0
            || plotHeight <= 0)
        {
            return new SpectrumRollResolution(MinimumHistoryColumns, minFrequencyRows);
        }

        var historyColumns = ClampQuantized(
            plotWidth * ColumnsPerDeviceIndependentPixel,
            MinimumHistoryColumns,
            MaximumHistoryColumns);
        var frequencyRows = ClampQuantized(
            plotHeight * RowsPerDeviceIndependentPixel,
            minFrequencyRows,
            maxFrequencyRows);
        return new SpectrumRollResolution(historyColumns, frequencyRows);
    }

    private static int ClampQuantized(double value, int minimum, int maximum)
    {
        var quantized = (int)Math.Round(value / ResolutionStep, MidpointRounding.AwayFromZero) * ResolutionStep;
        return Math.Clamp(quantized, minimum, maximum);
    }
}
