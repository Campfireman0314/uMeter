using System.Windows;

namespace DesktopLoudnessMeter.App.Controls;

internal readonly record struct SpectrumRollBitmapSegmentPlan(
    Rect Clip,
    Rect ImageDestination,
    int BitmapWidth)
{
    public static SpectrumRollBitmapSegmentPlan ForSegment(
        int sourceX,
        int sourceWidth,
        int bitmapWidth,
        Rect destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceX);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitmapWidth);
        if (sourceX + sourceWidth > bitmapWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidth), "The source segment must fit within the bitmap.");
        }

        var scale = destination.Width / sourceWidth;
        var imageDestination = new Rect(
            destination.Left - (sourceX * scale),
            destination.Top,
            bitmapWidth * scale,
            destination.Height);
        return new SpectrumRollBitmapSegmentPlan(destination, imageDestination, bitmapWidth);
    }

    public static (Rect First, Rect Second) SplitWrappedDestination(
        Rect destination,
        int firstSourceWidth,
        int secondSourceWidth,
        double dpiScaleX)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstSourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(secondSourceWidth);
        var totalSourceWidth = firstSourceWidth + secondSourceWidth;
        var seamX = destination.Left + (destination.Width * firstSourceWidth / totalSourceWidth);
        var snappedSeamX = SnapToDevicePixel(seamX, dpiScaleX);
        if (snappedSeamX <= destination.Left || snappedSeamX >= destination.Right)
        {
            snappedSeamX = seamX;
        }

        return (
            new Rect(destination.Left, destination.Top, snappedSeamX - destination.Left, destination.Height),
            new Rect(snappedSeamX, destination.Top, destination.Right - snappedSeamX, destination.Height));
    }

    public double SourceXToDestinationX(double sourceX)
    {
        return ImageDestination.Left + (sourceX * ImageDestination.Width / BitmapWidth);
    }

    private static double SnapToDevicePixel(double value, double dpiScale)
    {
        if (!double.IsFinite(value) || !double.IsFinite(dpiScale) || dpiScale <= 0)
        {
            return value;
        }

        return Math.Round(value * dpiScale, MidpointRounding.AwayFromZero) / dpiScale;
    }
}
