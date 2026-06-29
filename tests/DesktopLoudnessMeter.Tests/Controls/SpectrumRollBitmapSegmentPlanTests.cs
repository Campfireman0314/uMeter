using System.Windows;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumRollBitmapSegmentPlanTests
{
    [Fact]
    public void FullBitmapDestinationMapsRequestedSourceSegmentToClip()
    {
        var destination = new Rect(10, 20, 300, 120);

        var plan = SpectrumRollBitmapSegmentPlan.ForSegment(
            sourceX: 120,
            sourceWidth: 360,
            bitmapWidth: 480,
            destination);

        Assert.Equal(destination, plan.Clip);
        Assert.Equal(-90, plan.ImageDestination.Left, precision: 6);
        Assert.Equal(20, plan.ImageDestination.Top, precision: 6);
        Assert.Equal(400, plan.ImageDestination.Width, precision: 6);
        Assert.Equal(120, plan.ImageDestination.Height, precision: 6);
        Assert.Equal(destination.Left, plan.SourceXToDestinationX(120), precision: 6);
        Assert.Equal(destination.Right, plan.SourceXToDestinationX(480), precision: 6);
    }

    [Fact]
    public void WrappedDestinationSplitSnapsSharedSeamToDevicePixel()
    {
        var destination = new Rect(10, 20, 301, 120);

        var (first, second) = SpectrumRollBitmapSegmentPlan.SplitWrappedDestination(
            destination,
            firstSourceWidth: 317,
            secondSourceWidth: 163,
            dpiScaleX: 1.25);

        Assert.Equal(first.Right, second.Left, precision: 6);
        Assert.Equal(destination.Left, first.Left, precision: 6);
        Assert.Equal(destination.Right, second.Right, precision: 6);
        Assert.Equal(Math.Round(first.Right * 1.25), first.Right * 1.25, precision: 6);
    }
}
