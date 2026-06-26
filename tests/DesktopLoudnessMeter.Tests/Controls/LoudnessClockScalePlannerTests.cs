using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class LoudnessClockScalePlannerTests
{
    [Fact]
    public void BuildsAtLeastFiveMarkersAcrossConfiguredRange()
    {
        var markers = LoudnessClockScalePlanner.Build(-60, 0);

        Assert.True(markers.Count >= 5);
        Assert.Equal(-60, markers[0]);
        Assert.Equal(0, markers[^1]);
    }
}
