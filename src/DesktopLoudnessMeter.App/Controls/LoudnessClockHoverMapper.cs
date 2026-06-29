using System.Windows;

namespace DesktopLoudnessMeter.App.Controls;

internal static class LoudnessClockHoverMapper
{
    public static LoudnessClockHoverReadout? Map(
        Point position,
        Point center,
        double innerRadius,
        double outerRadius,
        double minimumDb,
        double maximumDb,
        double cycleDurationSeconds,
        bool counterClockwise = false)
    {
        if (!double.IsFinite(position.X)
            || !double.IsFinite(position.Y)
            || !double.IsFinite(center.X)
            || !double.IsFinite(center.Y)
            || !double.IsFinite(innerRadius)
            || !double.IsFinite(outerRadius)
            || !double.IsFinite(minimumDb)
            || !double.IsFinite(maximumDb)
            || !double.IsFinite(cycleDurationSeconds)
            || cycleDurationSeconds <= 0)
        {
            return null;
        }

        var radiusMinimum = Math.Max(0, Math.Min(innerRadius, outerRadius));
        var radiusMaximum = Math.Max(radiusMinimum, outerRadius);
        var dx = position.X - center.X;
        var dy = position.Y - center.Y;
        var radius = Math.Sqrt((dx * dx) + (dy * dy));
        if (radius < radiusMinimum || radius > radiusMaximum)
        {
            return null;
        }

        var angle = Math.Atan2(dy, dx);
        var phaseRatio = NormalizeTurn(counterClockwise
            ? ((-Math.PI / 2) - angle) / (2 * Math.PI)
            : (angle + (Math.PI / 2)) / (2 * Math.PI));
        var effectiveMaximum = maximumDb <= minimumDb ? minimumDb + 1 : maximumDb;
        var loudnessRatio = radiusMaximum <= radiusMinimum
            ? 0
            : (radius - radiusMinimum) / (radiusMaximum - radiusMinimum);
        var loudnessDb = minimumDb + ((effectiveMaximum - minimumDb) * loudnessRatio);
        return new LoudnessClockHoverReadout(
            phaseRatio * cycleDurationSeconds,
            loudnessDb,
            radius,
            angle);
    }

    private static double NormalizeTurn(double value)
    {
        var normalized = value % 1;
        return normalized < 0 ? normalized + 1 : normalized;
    }
}

internal readonly record struct LoudnessClockHoverReadout(
    double ElapsedSeconds,
    double LoudnessDb,
    double Radius,
    double AngleRadians);
