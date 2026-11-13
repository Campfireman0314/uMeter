using System.Windows;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumCursorLabelPlacementTests
{
    private static readonly Thickness Padding = new(7, 3, 7, 4);

    [Fact]
    public void LabelSitsRightOfTheCursorInsideThePlot()
    {
        var plot = new Rect(0, 0, 200, 100);

        var placement = SpectrumCursorLabelPlacement.Place(
            new Point(50, 50),
            plot,
            new Size(60, 14),
            Padding);

        Assert.Equal(60, placement.Label.Left, precision: 6);
        Assert.Equal(74, placement.Label.Width, precision: 6);
        Assert.Equal(21, placement.Label.Height, precision: 6);
        Assert.Equal(39.5, placement.Label.Top, precision: 6);
        Assert.Equal(67, placement.TextOriginX, precision: 6);
        Assert.Equal(42.5, placement.TextOriginY, precision: 6);
    }

    [Fact]
    public void LabelFlipsToTheLeftNearTheRightEdge()
    {
        var plot = new Rect(0, 0, 200, 100);

        var placement = SpectrumCursorLabelPlacement.Place(
            new Point(180, 50),
            plot,
            new Size(60, 14),
            Padding);

        Assert.Equal(96, placement.Label.Left, precision: 6);
        Assert.True(placement.Label.Right <= plot.Right);
    }

    [Fact]
    public void NarrowPlotKeepsTheLabelInsideThePlot()
    {
        var plot = new Rect(42, 9, 68, 140);

        var placement = SpectrumCursorLabelPlacement.Place(
            new Point(100, 60),
            plot,
            new Size(120, 30),
            Padding);

        Assert.Equal(46, placement.Label.Left, precision: 6);
        Assert.Equal(60, placement.Label.Width, precision: 6);
        Assert.True(placement.Label.Right <= plot.Right - 4 + 0.001);
        Assert.True(placement.Label.Left >= plot.Left + 4 - 0.001);
    }

    [Fact]
    public void TallTextIsClampedInsideThePlot()
    {
        var plot = new Rect(0, 0, 200, 100);

        var placement = SpectrumCursorLabelPlacement.Place(
            new Point(50, 50),
            plot,
            new Size(40, 200),
            Padding);

        Assert.Equal(94, placement.Label.Height, precision: 6);
        Assert.True(placement.Label.Top >= plot.Top + 3 - 0.001);
        Assert.True(placement.Label.Bottom <= plot.Bottom - 3 + 0.001);
    }

    [Fact]
    public void DegeneratePlotProducesAnEmptyPlacement()
    {
        var plot = new Rect(10, 10, 0, 0);

        var placement = SpectrumCursorLabelPlacement.Place(
            new Point(10, 10),
            plot,
            new Size(20, 10),
            Padding);

        Assert.Equal(0, placement.Label.Width);
        Assert.Equal(0, placement.Label.Height);
    }
}
