using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class WaveformRenderResolutionTests
{
    [Fact]
    public void PointBudgetGrowsWithPlotWidthWithinBounds()
    {
        var narrow = WaveformRenderResolution.PointBudgetForPlotWidth(120);
        var wide = WaveformRenderResolution.PointBudgetForPlotWidth(640);
        var veryWide = WaveformRenderResolution.PointBudgetForPlotWidth(4000);

        Assert.True(narrow < wide);
        Assert.Equal(WaveformRenderResolution.MinimumPointBudget, narrow);
        Assert.Equal(WaveformRenderResolution.MaximumPointBudget, veryWide);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-50)]
    public void InvalidPlotWidthsUseMinimumPointBudget(double width)
    {
        Assert.Equal(
            WaveformRenderResolution.MinimumPointBudget,
            WaveformRenderResolution.PointBudgetForPlotWidth(width));
    }
}
