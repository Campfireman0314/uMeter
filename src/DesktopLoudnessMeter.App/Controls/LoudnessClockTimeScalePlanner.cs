namespace DesktopLoudnessMeter.App.Controls;

internal static class LoudnessClockTimeScalePlanner
{
    public static IReadOnlyList<LoudnessClockTimeMarker> Build(double cycleDurationSeconds)
    {
        var cycle = double.IsFinite(cycleDurationSeconds) && cycleDurationSeconds > 0
            ? cycleDurationSeconds
            : 30;
        var interval = IntervalFor(cycle);
        var markers = new List<LoudnessClockTimeMarker>();
        for (var seconds = 0d; seconds < cycle - 0.001; seconds += interval)
        {
            markers.Add(new LoudnessClockTimeMarker(seconds, Format(seconds)));
        }

        return markers;
    }

    private static double IntervalFor(double cycleDurationSeconds)
    {
        return cycleDurationSeconds switch
        {
            <= 15 => 2,
            <= 30 => 5,
            <= 60 => 10,
            <= 180 => 30,
            _ => 60
        };
    }

    private static string Format(double seconds)
    {
        var rounded = (int)Math.Round(seconds, 0, MidpointRounding.AwayFromZero);
        if (rounded < 60)
        {
            return $"{rounded}s";
        }

        var minutes = rounded / 60;
        var remainder = rounded % 60;
        return remainder == 0
            ? $"{minutes}m"
            : $"{minutes}:{remainder:00}";
    }
}

internal readonly record struct LoudnessClockTimeMarker(double ElapsedSeconds, string Label);
