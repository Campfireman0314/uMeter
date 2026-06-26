namespace DesktopLoudnessMeter.App.Controls;

internal static class LoudnessClockScalePlanner
{
    public const int MinimumMarkerCount = 5;

    public static IReadOnlyList<double> Build(double minimumDb, double maximumDb, int minimumMarkerCount = MinimumMarkerCount)
    {
        var minimum = double.IsFinite(minimumDb) ? minimumDb : -60;
        var maximum = double.IsFinite(maximumDb) ? maximumDb : 0;
        if (maximum <= minimum)
        {
            maximum = minimum + 1;
        }

        var count = Math.Max(MinimumMarkerCount, minimumMarkerCount);
        var markers = new double[count];
        for (var index = 0; index < count; index++)
        {
            var ratio = count == 1 ? 0 : (double)index / (count - 1);
            markers[index] = Math.Round(minimum + ((maximum - minimum) * ratio), 0, MidpointRounding.AwayFromZero);
        }

        markers[0] = Math.Round(minimum, 0, MidpointRounding.AwayFromZero);
        markers[^1] = Math.Round(maximum, 0, MidpointRounding.AwayFromZero);
        return markers;
    }
}
