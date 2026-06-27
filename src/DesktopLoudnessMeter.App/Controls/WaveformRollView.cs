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
    private const int MaximumSmoothedWaveformPoints = 360;
    private static readonly double[] DecibelMarks = [0, -6, -12, -18];
    private bool _isPanning;
    private Point _panStart;
    private double _panStartCenter;
    private double _panStartSpan;
    private double _viewportCenter;
    private double _viewportSpan = DomainSpan;
    private readonly DeferredRenderUpdate _waveformUpdate;
    private IReadOnlyList<double>? _pendingWaveformSamples;
    private IReadOnlyList<double>? _renderedWaveformSamples;
    private StreamGeometry? _waveformGeometry;
    private Rect _waveformGeometryPlot;
    private double _waveformGeometryViewportMinimum;
    private double _waveformGeometryViewportSpan;
    private readonly PlotBackgroundCache _backgroundCache = new();

    public WaveformRollView()
    {
        _waveformUpdate = new DeferredRenderUpdate(this, CanApplyWaveformUpdate, ApplyPendingWaveformUpdate);
        Cursor = Cursors.SizeNS;
        SnapsToDevicePixels = true;
        Loaded += (_, _) => RequestWaveformUpdateIfReady();
        IsVisibleChanged += (_, _) => RequestWaveformUpdateIfReady();
        SizeChanged += (_, _) => RequestWaveformUpdateIfReady();
    }

    public static readonly DependencyProperty WaveformSamplesProperty = DependencyProperty.Register(
        nameof(WaveformSamples),
        typeof(IReadOnlyList<double>),
        typeof(WaveformRollView),
        new FrameworkPropertyMetadata(null, WaveformSamplesChanged));

    public IReadOnlyList<double>? WaveformSamples
    {
        get => (IReadOnlyList<double>?)GetValue(WaveformSamplesProperty);
        set => SetValue(WaveformSamplesProperty, value);
    }

    public static readonly DependencyProperty PlotTickProperty = DependencyProperty.Register(
        nameof(PlotTick),
        typeof(long),
        typeof(WaveformRollView),
        new FrameworkPropertyMetadata(0L, PlotTickChanged));

    public long PlotTick
    {
        get => (long)GetValue(PlotTickProperty);
        set => SetValue(PlotTickProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(WaveformRollView),
        new FrameworkPropertyMetadata(true, IsActiveChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
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

        var plot = GetPlotRect(width, height);
        NormalizeViewport(plot);
        DrawBackground(drawingContext, width, height, plot);
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
        InvalidateBackground();
        InvalidateWaveformGeometry();
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
        InvalidateBackground();
        InvalidateWaveformGeometry();
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
            var centerPen = ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 110, 1);
            var gridPen = ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 58, 1);
            drawingContext.DrawLine(centerPen, new Point(plot.Left, centerY), new Point(plot.Right, centerY));
            drawingContext.DrawLine(gridPen, new Point(plot.Left - 5, centerY), new Point(plot.Left, centerY));
            DrawLabel(drawingContext, "-inf", new Point(15, centerY - 7));
        }
    }

    private void DrawBackground(DrawingContext drawingContext, double width, double height, Rect plot)
    {
        var viewport = GetEffectiveViewport(plot);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var trackColor = CurrentTrack();
        var outlineColor = CurrentOutline();
        var subtleColor = CurrentSubtle();

        drawingContext.DrawDrawing(_backgroundCache.GetOrCreate(
            width,
            height,
            pixelsPerDip,
            viewport.Minimum,
            viewport.Span,
            trackColor,
            outlineColor,
            subtleColor,
            () => CreateBackgroundDrawing(width, height, plot)));
    }

    private DrawingGroup CreateBackgroundDrawing(double width, double height, Rect plot)
    {
        var group = new DrawingGroup();
        using (var drawingContext = group.Open())
        {
            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, PlotColorRole.Track),
                ThemedDrawing.GetPen(this, PlotColorRole.Outline, 1),
                new Rect(0, 0, width, height),
                6,
                6);
            DrawScale(drawingContext, plot);
        }

        if (group.CanFreeze)
        {
            group.Freeze();
        }

        return group;
    }

    private void DrawScaleTick(DrawingContext drawingContext, Rect plot, double amplitude, string label)
    {
        var y = ValueToY(plot, amplitude);
        if (!IsVisibleY(plot, y))
        {
            return;
        }

        var gridPen = ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 58, 1);
        drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
        drawingContext.DrawLine(gridPen, new Point(plot.Left - 5, y), new Point(plot.Left, y));
        if (!string.IsNullOrEmpty(label))
        {
            DrawLabel(drawingContext, label, new Point(14, y - 7));
        }
    }

    private void DrawWaveform(DrawingContext drawingContext, Rect plot)
    {
        if (_renderedWaveformSamples is not { Count: > 1 } samples)
        {
            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, PlotColorRole.Subtle, 28),
                null,
                plot,
                4,
                4);
            return;
        }

        var geometry = GetWaveformGeometry(samples, plot);
        drawingContext.DrawGeometry(
            null,
            ThemedDrawing.GetPen(this, PlotColorRole.Signal, 1.45),
            geometry);
    }

    private StreamGeometry GetWaveformGeometry(IReadOnlyList<double> samples, Rect plot)
    {
        var viewport = GetEffectiveViewport(plot);
        if (_waveformGeometry is not null
            && ReferenceEquals(_renderedWaveformSamples, samples)
            && RectsClose(_waveformGeometryPlot, plot)
            && AreClose(_waveformGeometryViewportMinimum, viewport.Minimum)
            && AreClose(_waveformGeometryViewportSpan, viewport.Span))
        {
            return _waveformGeometry;
        }

        _waveformGeometry = CreateSmoothGeometry(samples, plot);
        _waveformGeometryPlot = plot;
        _waveformGeometryViewportMinimum = viewport.Minimum;
        _waveformGeometryViewportSpan = viewport.Span;
        return _waveformGeometry;
    }

    private StreamGeometry CreateSmoothGeometry(IReadOnlyList<double> samples, Rect plot)
    {
        var points = CreateDisplayPoints(samples, plot);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], false, false);
            if (points.Count < 4 || points.Count > MaximumSmoothedWaveformPoints)
            {
                for (var index = 1; index < points.Count; index++)
                {
                    context.LineTo(points[index], true, false);
                }

                geometry.Freeze();
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

    private IReadOnlyList<Point> CreateDisplayPoints(IReadOnlyList<double> samples, Rect plot)
    {
        var pointBudget = WaveformRenderResolution.PointBudgetForPlotWidth(plot.Width);
        if (samples.Count <= pointBudget)
        {
            var points = new Point[samples.Count];
            for (var index = 0; index < samples.Count; index++)
            {
                points[index] = SampleToPoint(samples, index, samples.Count, plot);
            }

            return points;
        }

        var stride = (double)(samples.Count - 1) / (pointBudget - 1);
        var decimated = new Point[pointBudget];
        for (var index = 0; index < decimated.Length; index++)
        {
            decimated[index] = SampleToPoint(samples, (int)Math.Round(index * stride), samples.Count, plot);
        }

        return decimated;
    }

    private Point SampleToPoint(IReadOnlyList<double> samples, int index, int sampleCount, Rect plot)
    {
        var x = plot.Left + (plot.Width * index / (sampleCount - 1));
        var y = ValueToY(plot, Math.Clamp(samples[index], -1, 1));
        return new Point(x, y);
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
            ThemedDrawing.GetBrush(this, PlotColorRole.Subtle, 180),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        drawingContext.DrawText(formatted, origin);
    }

    private static void WaveformSamplesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WaveformRollView view)
        {
            view.ScheduleWaveformUpdate(args.NewValue as IReadOnlyList<double>);
        }
    }

    private static void PlotTickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WaveformRollView view)
        {
            view.ScheduleWaveformUpdate(view.WaveformSamples);
        }
    }

    private static void IsActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is WaveformRollView view)
        {
            view.RequestWaveformUpdateIfReady();
        }
    }

    private void ScheduleWaveformUpdate(IReadOnlyList<double>? samples)
    {
        _pendingWaveformSamples = samples is { Count: > 0 } ? samples : null;
        RequestWaveformUpdateIfReady();
    }

    private void RequestWaveformUpdateIfReady()
    {
        if (CanApplyWaveformUpdate())
        {
            _waveformUpdate.Request();
        }
    }

    private void ApplyPendingWaveformUpdate()
    {
        if (ReferenceEquals(_renderedWaveformSamples, _pendingWaveformSamples))
        {
            return;
        }

        _renderedWaveformSamples = _pendingWaveformSamples;
        InvalidateWaveformGeometry();
        InvalidateVisual();
    }

    private bool CanApplyWaveformUpdate()
    {
        return IsLoaded
            && IsActive
            && IsVisible
            && Visibility == Visibility.Visible
            && ActualWidth > 0
            && ActualHeight > 0;
    }

    private Color CurrentTrack()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Track);
    }

    private Color CurrentOutline()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Outline);
    }

    private Color CurrentSubtle()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Subtle);
    }

    private void InvalidateBackground()
    {
        _backgroundCache.Invalidate();
    }

    private void InvalidateWaveformGeometry()
    {
        _waveformGeometry = null;
    }

    private static bool AreClose(double first, double second)
    {
        return Math.Abs(first - second) < 0.5;
    }

    private static bool RectsClose(Rect first, Rect second)
    {
        return AreClose(first.Left, second.Left)
            && AreClose(first.Top, second.Top)
            && AreClose(first.Width, second.Width)
            && AreClose(first.Height, second.Height);
    }
}
