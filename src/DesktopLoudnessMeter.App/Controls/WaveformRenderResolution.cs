namespace DesktopLoudnessMeter.App.Controls;

internal static class WaveformRenderResolution
{
    public const int MinimumPointBudget = 160;
    public const int MaximumPointBudget = 720;
    private const double PointsPerDeviceIndependentPixel = 1.25;

    public static int PointBudgetForPlotWidth(double plotWidth)
    {
        if (!double.IsFinite(plotWidth) || plotWidth <= 0)
        {
            return MinimumPointBudget;
        }

        var budget = (int)Math.Round(plotWidth * PointsPerDeviceIndependentPixel, MidpointRounding.AwayFromZero);
        return Math.Clamp(budget, MinimumPointBudget, MaximumPointBudget);
    }
}
