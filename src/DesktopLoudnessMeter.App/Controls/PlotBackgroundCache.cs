using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

internal sealed class PlotBackgroundCache
{
    private DrawingGroup? _drawing;
    private double _width;
    private double _height;
    private double _pixelsPerDip;
    private double _viewportMinimum;
    private double _viewportSpan;
    private Color _trackColor;
    private Color _outlineColor;
    private Color _subtleColor;

    public DrawingGroup GetOrCreate(
        double width,
        double height,
        double pixelsPerDip,
        Color trackColor,
        Color outlineColor,
        Color subtleColor,
        Func<DrawingGroup> createDrawing)
    {
        return GetOrCreate(width, height, pixelsPerDip, 0, 0, trackColor, outlineColor, subtleColor, createDrawing);
    }

    public DrawingGroup GetOrCreate(
        double width,
        double height,
        double pixelsPerDip,
        double viewportMinimum,
        double viewportSpan,
        Color trackColor,
        Color outlineColor,
        Color subtleColor,
        Func<DrawingGroup> createDrawing)
    {
        if (_drawing is null
            || !AreClose(_width, width)
            || !AreClose(_height, height)
            || !AreClose(_pixelsPerDip, pixelsPerDip)
            || !AreClose(_viewportMinimum, viewportMinimum)
            || !AreClose(_viewportSpan, viewportSpan)
            || _trackColor != trackColor
            || _outlineColor != outlineColor
            || _subtleColor != subtleColor)
        {
            _drawing = createDrawing();
            _width = width;
            _height = height;
            _pixelsPerDip = pixelsPerDip;
            _viewportMinimum = viewportMinimum;
            _viewportSpan = viewportSpan;
            _trackColor = trackColor;
            _outlineColor = outlineColor;
            _subtleColor = subtleColor;
        }

        return _drawing;
    }

    public void Invalidate()
    {
        _drawing = null;
    }

    private static bool AreClose(double first, double second)
    {
        return Math.Abs(first - second) < 0.5;
    }
}
