using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class LoudnessClockTimeScalePlannerTests
{
    [Fact]
    public void DefaultCycleBuildsReadableFiveSecondMarkers()
    {
        var markers = LoudnessClockTimeScalePlanner.Build(30);

        Assert.Equal([0, 5, 10, 15, 20, 25], markers.Select(marker => marker.ElapsedSeconds));
        Assert.Equal("15s", markers[3].Label);
    }

    [Fact]
    public void LongCycleUsesMinuteLabels()
    {
        var markers = LoudnessClockTimeScalePlanner.Build(600);

        Assert.Contains(markers, marker => marker.ElapsedSeconds == 300 && marker.Label == "5m");
    }
}
