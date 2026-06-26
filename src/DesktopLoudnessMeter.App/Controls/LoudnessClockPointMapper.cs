namespace DesktopLoudnessMeter.App.Controls;

internal static class LoudnessClockPointMapper
{
    public static LoudnessClockPoint? Map(
        double loudnessDb,
        double minimumDb,
        double maximumDb,
        double innerRadius,
        double outerRadius,
        double elapsedSeconds,
        double cycleDurationSeconds,
        bool counterClockwise = false,
        double? warningDb = null)
    {
        if (!double.IsFinite(loudnessDb)
            || !double.IsFinite(minimumDb)
            || !double.IsFinite(maximumDb)
            || !double.IsFinite(innerRadius)
            || !double.IsFinite(outerRadius)
            || !double.IsFinite(elapsedSeconds)
            || !double.IsFinite(cycleDurationSeconds)
            || cycleDurationSeconds <= 0
            || loudnessDb < minimumDb)
        {
            return null;
        }

        var effectiveMaximum = maximumDb <= minimumDb ? minimumDb + 1 : maximumDb;
        var radiusMinimum = Math.Max(0, Math.Min(innerRadius, outerRadius));
        var radiusMaximum = Math.Max(radiusMinimum, outerRadius);
        var loudnessRatio = Math.Clamp((loudnessDb - minimumDb) / (effectiveMaximum - minimumDb), 0, 1);
        var radius = radiusMinimum + ((radiusMaximum - radiusMinimum) * loudnessRatio);
        var cyclePhaseSeconds = elapsedSeconds % cycleDurationSeconds;
        if (cyclePhaseSeconds < 0)
        {
            cyclePhaseSeconds += cycleDurationSeconds;
        }

        var direction = counterClockwise ? -1 : 1;
        var angle = (-Math.PI / 2) + (direction * 2 * Math.PI * cyclePhaseSeconds / cycleDurationSeconds);
        var isOverMaximum = loudnessDb > effectiveMaximum;
        var isWarning = warningDb is double warning
            && double.IsFinite(warning)
            && loudnessDb >= warning;
        return new LoudnessClockPoint(angle, radius, isWarning || isOverMaximum, isOverMaximum);
    }
}

internal readonly record struct LoudnessClockPoint(
    double AngleRadians,
    double Radius,
    bool IsWarning,
    bool IsOverMaximum);
