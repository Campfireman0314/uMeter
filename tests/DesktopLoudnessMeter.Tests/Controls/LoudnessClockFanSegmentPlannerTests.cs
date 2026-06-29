using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class LoudnessClockFanSegmentPlannerTests
{
    [Fact]
    public void IncreasingPhasesProduceSingleSegment()
    {
        var first = new LoudnessClockMappedSample(new LoudnessClockPoint(-Math.PI / 2, 40, false, false), 0);
        var second = new LoudnessClockMappedSample(new LoudnessClockPoint(0, 70, false, false), 7.5);

        var segments = LoudnessClockFanSegmentPlanner.Build([first, second]);

        Assert.Single(segments);
        Assert.Equal(first.Point, segments[0].First);
        Assert.Equal(second.Point, segments[0].Second);
    }

    [Fact]
    public void WrappedPhasesBridgeThroughCycleOrigin()
    {
        var beforeWrap = new LoudnessClockMappedSample(new LoudnessClockPoint((Math.PI * 3 / 2) - 0.05, 80, false, false), 29.75);
        var afterWrap = new LoudnessClockMappedSample(new LoudnessClockPoint((-Math.PI / 2) + 0.05, 62, true, false), 0.25);

        var segments = LoudnessClockFanSegmentPlanner.Build([beforeWrap, afterWrap]);

        Assert.Equal(2, segments.Count);
        Assert.Equal(beforeWrap.Point, segments[0].First);
        Assert.Equal(-Math.PI / 2, segments[0].Second.AngleRadians, precision: 6);
        Assert.Equal(beforeWrap.Point.Radius, segments[0].Second.Radius);
        Assert.False(segments[0].Second.IsWarning);

        Assert.Equal(-Math.PI / 2, segments[1].First.AngleRadians, precision: 6);
        Assert.Equal(afterWrap.Point.Radius, segments[1].First.Radius);
        Assert.True(segments[1].First.IsWarning);
        Assert.Equal(afterWrap.Point, segments[1].Second);
    }
}
