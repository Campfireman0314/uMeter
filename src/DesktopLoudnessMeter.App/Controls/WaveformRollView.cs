using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class WaveformRollView : FrameworkElement
{
    private const double MinimumValue = -1;
    private const double MaximumValue = 1;
    private const double MinimumViewportSpan = 0.08;
    private const double PreferredPlotHeight = 122;
    private static readonly Brush SurfaceBrush = new SolidColorBrush(Color.FromRgb(17, 20, 25));
    private static readonly Brush EmptyBrush = new SolidColorBrush(Color.FromArgb(28, 174, 183, 194));
    private static readonly Brush ScaleBrush = new SolidColorBrush(Color.FromArgb(180, 174, 183, 194));
    private static readonly Pen OutlinePen = new(new SolidColorBrush(Color.FromRgb(58, 66, 77)), 1);
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromArgb(55, 174, 183, 194)), 1);
    private static readonly Pen CenterPen = new(new SolidColorBrush(Color.FromArgb(105, 174, 183, 194)), 1);
    private static readonly Pen WavePen = new(new SolidColorBrush(Color.FromRgb(46, 196, 182)), 1.45);
    private static readonly double[] DecibelMarks = [0, -6, -12, -18];
    private bool _isPanning;
    private Point _panStart;
    private double _panStartCenter;
    private double _panStartSpan;
    private double _viewportCenter;
    private double _viewportSpan = DomainSpan;

    public WaveformRollView()
    {
        Cursor = Cursors.SizeNS;
        SnapsToDevicePixels = true;
    }

    public static readonly DependencyProperty WaveformSamplesProperty = DependencyProperty.Register(
        nameof(WaveformSamples),
        typeof(IReadOnlyList<double>),
        typeof(WaveformRollView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double>? WaveformSamples
    {
        get => (IReadOnlyList<double>?)GetValue(WaveformSamplesProperty);
        set => SetValue(WaveformSamplesProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 140);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        drawingContext.DrawRoundedRectangle(SurfaceBrush, OutlinePen, new Rect(0, 0, width, height), 6, 6);

        var plot = GetPlotRect(width, height);
        NormalizeViewport(plot);
        DrawScale(drawingContext, plot);
        drawingContext.PushClip(new RectangleGeometry(plot));
        DrawWaveform(drawingContext, plot);
        drawingContext.Pop();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ClickCount >= 2)
        {
            ResetViewport();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _isPanning = true;
        _panStart = e.GetPosition(this);
        var viewport = GetEffectiveViewport(GetPlotRect());
        _panStartCenter = viewport.Center;
        _panStartSpan = viewport.Span;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isPanning)
        {
            return;
        }

        var deltaY = e.GetPosition(this).Y - _panStart.Y;
        var plot = GetPlotRect();
        var valueDelta = deltaY * _panStartSpan / plot.Height;
        SetViewport(_panStartCenter + valueDelta, _viewportSpan, plot);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var plot = GetPlotRect();
        var viewport = GetEffectiveViewport(plot);
        var mouseY = Math.Clamp(e.GetPosition(this).Y, plot.Top, plot.Bottom);
        var anchorRatio = (plot.Bottom - mouseY) / plot.Height;
        var anchorValue = viewport.Minimum + (viewport.Span * anchorRatio);
        var zoom = Math.Pow(1.16, e.Delta / 120.0);
        var newSpan = Math.Clamp(viewport.Span / zoom, MinimumViewportSpan, DomainSpan);
        var effectiveNewSpan = Math.Min(newSpan, GetLayoutLimitedSpan(plot));
        var newCenter = anchorValue + (effectiveNewSpan * (0.5 - anchorRatio));
        SetViewport(newCenter, newSpan, plot);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _isPanning = false;
    }

    private void DrawScale(DrawingContext drawingContext, Rect plot)
    {
        foreach (var decibels in DecibelMarks)
        {
            var amplitude = Math.Pow(10, decibels / 20);
            DrawScaleTick(drawingContext, plot, amplitude, decibels == 0 ? "0 dB" : $"{decibels:0}");
            DrawScaleTick(drawingContext, plot, -amplitude, decibels == 0 ? "0 dB" : string.Empty);
        }

        var centerY = ValueToY(plot, 0);
        if (IsVisibleY(plot, centerY))
        {
            drawingContext.DrawLine(CenterPen, new Point(plot.Left, centerY), new Point(plot.Right, centerY));
            drawingContext.DrawLine(GridPen, new Point(plot.Left - 5, centerY), new Point(plot.Left, centerY));
            DrawLabel(drawingContext, "-inf", new Point(15, centerY - 7));
        }
    }

    private void DrawScaleTick(DrawingContext drawingContext, Rect plot, double amplitude, string label)
    {
        var y = ValueToY(plot, amplitude);
        if (!IsVisibleY(plot, y))
        {
            return;
        }

        drawingContext.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));
        drawingContext.DrawLine(GridPen, new Point(plot.Left - 5, y), new Point(plot.Left, y));
        if (!string.IsNullOrEmpty(label))
        {
            DrawLabel(drawingContext, label, new Point(14, y - 7));
        }
    }

    private void DrawWaveform(DrawingContext drawingContext, Rect plot)
    {
        if (WaveformSamples is not { Count: > 1 } samples)
        {
            drawingContext.DrawRoundedRectangle(EmptyBrush, null, plot, 4, 4);
            return;
        }

        var points = new Point[samples.Count];
        for (var index = 0; index < samples.Count; index++)
        {
            var x = plot.Left + (plot.Width * index / (samples.Count - 1));
            var y = ValueToY(plot, Math.Clamp(samples[index], -1, 1));
            points[index] = new Point(x, y);
        }

        var geometry = CreateSmoothGeometry(points, plot);
        drawingContext.DrawGeometry(null, WavePen, geometry);
    }

    private static StreamGeometry CreateSmoothGeometry(IReadOnlyList<Point> points, Rect plot)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], false, false);
            if (points.Count < 4)
            {
                for (var index = 1; index < points.Count; index++)
                {
                    context.LineTo(points[index], true, false);
                }

                return geometry;
            }

            for (var index = 0; index < points.Count - 1; index++)
            {
                var previous = index == 0 ? points[index] : points[index - 1];
                var current = points[index];
                var next = points[index + 1];
                var following = index + 2 < points.Count ? points[index + 2] : next;
                var firstControl = new Point(
                    current.X + ((next.X - previous.X) / 6),
                    Math.Clamp(current.Y + ((next.Y - previous.Y) / 6), plot.Top, plot.Bottom));
                var secondControl = new Point(
                    next.X - ((following.X - current.X) / 6),
                    Math.Clamp(next.Y - ((following.Y - current.Y) / 6), plot.Top, plot.Bottom));
                context.BezierTo(firstControl, secondControl, next, true, false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private double ValueToY(Rect plot, double value)
    {
        var viewport = GetEffectiveViewport(plot);
        var ratio = (value - viewport.Minimum) / viewport.Span;
        return plot.Bottom - (plot.Height * ratio);
    }

    private Rect GetPlotRect()
    {
        return GetPlotRect(ActualWidth, ActualHeight);
    }

    private static Rect GetPlotRect(double width, double height)
    {
        return new Rect(52, 9, Math.Max(1, width - 62), Math.Max(1, height - 18));
    }

    private static bool IsVisibleY(Rect plot, double y)
    {
        return y >= plot.Top && y <= plot.Bottom;
    }

    private static double DomainSpan => MaximumValue - MinimumValue;

    private void ResetViewport()
    {
        _viewportCenter = 0;
        _viewportSpan = DomainSpan;
    }

    private void NormalizeViewport(Rect plot)
    {
        SetViewport(_viewportCenter, _viewportSpan, plot);
    }

    private void SetViewport(double center, double span, Rect plot)
    {
        _viewportSpan = Math.Clamp(span, MinimumViewportSpan, DomainSpan);
        var effectiveSpan = Math.Min(_viewportSpan, GetLayoutLimitedSpan(plot));
        _viewportCenter = ClampCenter(center, effectiveSpan);
    }

    private Viewport GetEffectiveViewport(Rect plot)
    {
        var span = Math.Min(Math.Clamp(_viewportSpan, MinimumViewportSpan, DomainSpan), GetLayoutLimitedSpan(plot));
        var center = ClampCenter(_viewportCenter, span);
        return new Viewport(center - (span / 2), span);
    }

    private static double GetLayoutLimitedSpan(Rect plot)
    {
        var ratio = Math.Clamp(plot.Height / PreferredPlotHeight, MinimumViewportSpan / DomainSpan, 1);
        return Math.Clamp(DomainSpan * ratio, MinimumViewportSpan, DomainSpan);
    }

    private static double ClampCenter(double center, double span)
    {
        var halfSpan = span / 2;
        return Math.Clamp(center, MinimumValue + halfSpan, MaximumValue - halfSpan);
    }

    private readonly record struct Viewport(double Minimum, double Span)
    {
        public double Center => Minimum + (Span / 2);
    }

    private void DrawLabel(DrawingContext drawingContext, string text, Point origin)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            ScaleBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        drawingContext.DrawText(formatted, origin);
    }
}
