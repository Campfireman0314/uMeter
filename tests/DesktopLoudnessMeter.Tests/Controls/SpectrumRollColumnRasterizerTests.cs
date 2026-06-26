using DesktopLoudnessMeter.App.Controls;
using System.Windows.Media;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumRollColumnRasterizerTests
{
    [Fact]
    public void OneHotBucketProducesSingleNonZeroPixel()
    {
        var pixels = new int[5];
        double[] buckets = [0, 0, 1, 0, 0];
        var transform = SpectrumRollColumnRasterizer.CreateDisplayTransform(-120, 0);

        SpectrumRollColumnRasterizer.WriteColumn(
            buckets,
            pixels,
            transform,
            Colors.Cyan,
            Colors.Orange);

        Assert.Equal(1, pixels.Count(pixel => pixel != 0));
        Assert.NotEqual(0, pixels[2]);
    }

    [Fact]
    public void FloorTransformKeepsSilenceTransparent()
    {
        var pixels = new int[4];
        double[] buckets = [0, 0, 0, 0];
        var transform = SpectrumRollColumnRasterizer.CreateDisplayTransform(-120, 0);

        SpectrumRollColumnRasterizer.WriteColumn(
            buckets,
            pixels,
            transform,
            Colors.Cyan,
            Colors.Orange);

        Assert.All(pixels, pixel => Assert.Equal(0, pixel));
    }

    [Fact]
    public void PositiveLowLevelBucketsRemainVisibleBelowDisplayFloor()
    {
        var transform = SpectrumRollColumnRasterizer.CreateDisplayTransform(-120, 0);
        var pixel = SpectrumRollColumnRasterizer.LevelPixel(0.05, transform, Colors.Cyan, Colors.Orange);

        Assert.NotEqual(0, pixel);
        Assert.InRange((byte)(pixel >> 24), 1, 80);
    }
}
