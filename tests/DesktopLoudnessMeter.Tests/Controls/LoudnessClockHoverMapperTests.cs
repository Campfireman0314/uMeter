using System.Windows;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class LoudnessClockHoverMapperTests
{
    [Fact]
    public void HoverPositionMapsAngleToCycleTimeAndRadiusToLoudness()
    {
        var readout = LoudnessClockHoverMapper.Map(
            new Point(200, 100),
            new Point(100, 100),
            innerRadius: 20,
            outerRadius: 100,
            minimumDb: -60,
            maximumDb: 0,
            cycleDurationSeconds: 40);

        Assert.NotNull(readout);
        Assert.Equal(10, readout.Value.ElapsedSeconds, precision: 6);
        Assert.Equal(0, readout.Value.LoudnessDb, precision: 6);
    }

    [Fact]
    public void HoverOutsideClockRingIsIgnored()
    {
        var readout = LoudnessClockHoverMapper.Map(
            new Point(100, 100),
            new Point(100, 100),
            innerRadius: 20,
            outerRadius: 100,
            minimumDb: -60,
            maximumDb: 0,
            cycleDurationSeconds: 40);

        Assert.Null(readout);
    }
}
