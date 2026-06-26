namespace DesktopLoudnessMeter.App.Controls;

internal static class LoudnessClockFanSegmentPlanner
{
    private const double CycleOriginAngleRadians = -Math.PI / 2;

    public static IReadOnlyList<LoudnessClockFanSegment> Build(IReadOnlyList<LoudnessClockMappedSample> samples)
    {
        if (samples.Count < 2)
        {
            return [];
        }

        var segments = new List<LoudnessClockFanSegment>(samples.Count);
        var previous = samples[0];
        for (var index = 1; index < samples.Count; index++)
        {
            var current = samples[index];
            var planned = Build(previous, current);
            segments.Add(planned.First);
            if (planned.Second is { } second)
            {
                segments.Add(second);
            }

            previous = current;
        }

        return segments;
    }

    public static LoudnessClockFanSegmentPair Build(
        LoudnessClockMappedSample previous,
        LoudnessClockMappedSample current)
    {
        if (current.Phase >= previous.Phase)
        {
            return new LoudnessClockFanSegmentPair(
                new LoudnessClockFanSegment(previous.Point, current.Point),
                null);
        }

        return new LoudnessClockFanSegmentPair(
            new LoudnessClockFanSegment(
                previous.Point,
                new LoudnessClockPoint(
                    CycleOriginAngleRadians,
                    previous.Point.Radius,
                    previous.Point.IsWarning,
                    previous.Point.IsOverMaximum)),
            new LoudnessClockFanSegment(
                new LoudnessClockPoint(
                    CycleOriginAngleRadians,
                    current.Point.Radius,
                    current.Point.IsWarning,
                    current.Point.IsOverMaximum),
                current.Point));
    }
}

internal readonly record struct LoudnessClockMappedSample(LoudnessClockPoint Point, double Phase);

internal readonly record struct LoudnessClockFanSegment(LoudnessClockPoint First, LoudnessClockPoint Second);

internal readonly record struct LoudnessClockFanSegmentPair(
    LoudnessClockFanSegment First,
    LoudnessClockFanSegment? Second);
