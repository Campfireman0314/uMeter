using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class StereoFanView : FrameworkElement
{
    private const double SourceHistorySeconds = 0.1;

    public static readonly DependencyProperty ImageXProperty = DependencyProperty.Register(
        nameof(ImageX),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ImageYProperty = DependencyProperty.Register(
        nameof(ImageY),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CorrelationProperty = DependencyProperty.Register(
        nameof(Correlation),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<StereoImagePoint>),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AutoScaleProperty = DependencyProperty.Register(
        nameof(AutoScale),
        typeof(bool),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ScaleRangeDbProperty = DependencyProperty.Register(
        nameof(ScaleRangeDb),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(60d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ManualCeilingDbProperty = DependencyProperty.Register(
        nameof(ManualCeilingDb),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AutoScaleFloorDbProperty = DependencyProperty.Register(
        nameof(AutoScaleFloorDb),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(-24d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AutoScaleSpeedDbPerSecondProperty = DependencyProperty.Register(
        nameof(AutoScaleSpeedDbPerSecond),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TracePersistenceSecondsProperty = DependencyProperty.Register(
        nameof(TracePersistenceSeconds),
        typeof(double),
        typeof(StereoFanView),
        new FrameworkPropertyMetadata(0.08d, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly (double X, double Y, double Size)[] SampleOffsets =
    [
        (-0.74, -0.12, 2.4),
        (-0.58, 0.22, 2.1),
        (-0.43, -0.34, 2.6),
        (-0.31, 0.08, 1.8),
        (-0.18, 0.38, 2.2),
        (-0.08, -0.18, 2.8),
        (0.02, 0.16, 2.0),
        (0.12, -0.42, 2.5),
        (0.23, 0.31, 2.0),
        (0.36, -0.08, 2.7),
        (0.49, 0.18, 2.2),
        (0.62, -0.28, 2.4),
        (0.76, 0.04, 1.9),
        (-0.52, 0.48, 1.7),
        (-0.26, -0.54, 1.9),
        (0.28, 0.52, 1.8),
        (0.55, -0.50, 2.1),
        (0.70, 0.34, 1.6)
    ];

    private static readonly Typeface TextTypeface = new("Segoe UI");
    private double? _smoothedAutoCeilingDb;
    private DateTimeOffset _lastAutoScaleUpdate = DateTimeOffset.Now;

    public double ImageX
    {
        get => (double)GetValue(ImageXProperty);
        set => SetValue(ImageXProperty, value);
    }

    public double ImageY
    {
        get => (double)GetValue(ImageYProperty);
        set => SetValue(ImageYProperty, value);
    }

    public double Correlation
    {
        get => (double)GetValue(CorrelationProperty);
        set => SetValue(CorrelationProperty, value);
    }

    public IReadOnlyList<StereoImagePoint>? Points
    {
        get => (IReadOnlyList<StereoImagePoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public bool AutoScale
    {
        get => (bool)GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public double ScaleRangeDb
    {
        get => (double)GetValue(ScaleRangeDbProperty);
        set => SetValue(ScaleRangeDbProperty, value);
    }

    public double ManualCeilingDb
    {
        get => (double)GetValue(ManualCeilingDbProperty);
        set => SetValue(ManualCeilingDbProperty, value);
    }

    public double AutoScaleFloorDb
    {
        get => (double)GetValue(AutoScaleFloorDbProperty);
        set => SetValue(AutoScaleFloorDbProperty, value);
    }

    public double AutoScaleSpeedDbPerSecond
    {
        get => (double)GetValue(AutoScaleSpeedDbPerSecondProperty);
        set => SetValue(AutoScaleSpeedDbPerSecondProperty, value);
    }

    public double TracePersistenceSeconds
    {
        get => (double)GetValue(TracePersistenceSecondsProperty);
        set => SetValue(TracePersistenceSecondsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 194);
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

        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        var centerX = width / 2;
        var baseY = height - 20;
        var radius = Math.Max(24, Math.Min((width / 2) - 14, height - 34));

        DrawFanSurface(drawingContext, centerX, baseY, radius);
        DrawGuides(drawingContext, centerX, baseY, radius);
        DrawSamples(drawingContext, centerX, baseY, radius);
        DrawAxisLabels(drawingContext, centerX, baseY, radius);
    }

    private void DrawFanSurface(DrawingContext drawingContext, double centerX, double baseY, double radius)
    {
        var fan = new StreamGeometry();
        using (var context = fan.Open())
        {
            context.BeginFigure(new Point(centerX, baseY), true, true);
            for (var i = 0; i <= 72; i++)
            {
                var angle = Math.PI - (Math.PI * i / 72);
                var point = new Point(
                    centerX + (Math.Cos(angle) * radius),
                    baseY - (Math.Sin(angle) * radius));
                context.LineTo(point, true, false);
            }
        }

        drawingContext.DrawGeometry(
            ThemedDrawing.GetBrush(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25)),
            ThemedDrawing.GetPen(this, "ChromeBorderBrush", 1, Color.FromRgb(58, 66, 77)),
            fan);
        drawingContext.DrawGeometry(
            ThemedDrawing.GetBrush(this, "SignalBrush", 34, Color.FromRgb(46, 196, 182)),
            null,
            fan);
    }

    private void DrawGuides(DrawingContext drawingContext, double centerX, double baseY, double radius)
    {
        var guidePen = ThemedDrawing.GetPen(this, "SubtleTextBrush", 105, 1, Color.FromRgb(174, 183, 194));
        foreach (var ratio in new[] { 0.33, 0.66, 1.0 })
        {
            DrawArc(drawingContext, centerX, baseY, radius * ratio, guidePen);
        }

        for (var degrees = 0; degrees <= 180; degrees += 30)
        {
            var angle = Math.PI * degrees / 180;
            var end = new Point(centerX + (Math.Cos(angle) * radius), baseY - (Math.Sin(angle) * radius));
            drawingContext.DrawLine(guidePen, new Point(centerX, baseY), end);
        }
    }

    private void DrawSamples(DrawingContext drawingContext, double centerX, double baseY, double radius)
    {
        if (Points is { Count: > 0 } points)
        {
            DrawPointCloud(drawingContext, centerX, baseY, radius, points);
            return;
        }

        DrawFallbackSamples(drawingContext, centerX, baseY, radius);
    }

    private void DrawPointCloud(
        DrawingContext drawingContext,
        double centerX,
        double baseY,
        double radius,
        IReadOnlyList<StereoImagePoint> points)
    {
        var scale = CalculatePointScale(points);
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var angle = Math.Clamp(point.Angle, -1, 1);
            var pointRadius = scale.Normalize(point.Radius);
            var projected = ProjectPolarToFan(angle, pointRadius, centerX, baseY, radius);
            var ageRatio = points.Count <= 1 ? 0 : (double)(points.Count - 1 - index) / (points.Count - 1);
            var intensity = CalculateTraceIntensity(ageRatio);
            var glowAlpha = (byte)Math.Round(18 + (intensity * 76));
            var dotAlpha = (byte)Math.Round(82 + (intensity * 148));
            var size = 1.2 + (intensity * 1.7);
            var glowBrush = ThemedDrawing.GetBrush(this, "SignalBrush", glowAlpha, Color.FromRgb(46, 196, 182));
            var dotBrush = ThemedDrawing.GetBrush(this, "SignalBrush", dotAlpha, Color.FromRgb(46, 196, 182));

            drawingContext.DrawEllipse(glowBrush, null, projected, size + 5, size + 5);
            drawingContext.DrawEllipse(dotBrush, null, projected, size, size);
        }
    }

    private PointScale CalculatePointScale(IReadOnlyList<StereoImagePoint> points)
    {
        var range = Math.Clamp(ScaleRangeDb, 12, 96);
        var ceiling = Math.Clamp(ManualCeilingDb, -60, 12);
        if (AutoScale)
        {
            var measuredCeiling = points.Count == 0
                ? 0
                : points.Max(point => RadiusToDb(point.Radius));
            var targetCeiling = Math.Max(measuredCeiling, Math.Clamp(AutoScaleFloorDb, -72, 0));
            ceiling = SmoothAutoCeiling(targetCeiling);
        }
        else
        {
            _smoothedAutoCeilingDb = null;
        }

        return new PointScale(ceiling - range, ceiling);
    }

    private double SmoothAutoCeiling(double targetCeilingDb)
    {
        var now = DateTimeOffset.Now;
        var elapsedSeconds = Math.Clamp((now - _lastAutoScaleUpdate).TotalSeconds, 0, 0.25);
        _lastAutoScaleUpdate = now;

        if (_smoothedAutoCeilingDb is not double current)
        {
            _smoothedAutoCeilingDb = targetCeilingDb;
            return targetCeilingDb;
        }

        var speed = Math.Clamp(AutoScaleSpeedDbPerSecond, 1, 24);
        var maxStep = Math.Max(0.1, speed * elapsedSeconds);
        var delta = Math.Clamp(targetCeilingDb - current, -maxStep, maxStep);
        var smoothed = current + delta;
        _smoothedAutoCeilingDb = smoothed;
        return smoothed;
    }

    private static Point ProjectPolarToFan(double normalizedAngle, double normalizedRadius, double centerX, double baseY, double radius)
    {
        var angle = Math.PI - (((normalizedAngle + 1) / 2) * Math.PI);
        var distance = normalizedRadius * radius;
        return new Point(
            centerX + (Math.Cos(angle) * distance),
            baseY - (Math.Sin(angle) * distance));
    }

    private double CalculateTraceIntensity(double normalizedAge)
    {
        var persistenceSeconds = Math.Clamp(TracePersistenceSeconds, 0.02, 0.25);
        var ageSeconds = Math.Clamp(normalizedAge, 0, 1) * SourceHistorySeconds;
        return Math.Clamp(Math.Exp(-ageSeconds / persistenceSeconds), 0, 1);
    }

    private static double RadiusToDb(double radius)
    {
        return radius <= 0 ? -120 : 20 * Math.Log10(radius);
    }

    private void DrawFallbackSamples(DrawingContext drawingContext, double centerX, double baseY, double radius)
    {
        var hasAggregate = !double.IsNaN(ImageX) && !double.IsNaN(ImageY);
        var baseX = hasAggregate ? Math.Clamp(ImageX, -1, 1) : 0;
        var baseDepth = hasAggregate ? Math.Clamp((ImageY + 1) / 2, 0.08, 0.94) : 0.42;
        var correlation = double.IsNaN(Correlation) ? 0 : Math.Clamp(Correlation, -1, 1);
        var spreadFromCorrelation = 1 - Math.Abs(correlation);
        var spreadX = 0.16 + (spreadFromCorrelation * 0.22);
        var spreadY = 0.08 + (spreadFromCorrelation * 0.10);

        foreach (var sample in SampleOffsets)
        {
            var x = Math.Clamp(baseX + (sample.X * spreadX), -1, 1);
            var y = Math.Clamp(baseDepth + (sample.Y * spreadY), 0.05, 0.98);
            var point = ProjectToFan(x, y, centerX, baseY, radius);
            var glowAlpha = hasAggregate ? (byte)76 : (byte)30;
            var dotAlpha = hasAggregate ? (byte)216 : (byte)108;
            var glowBrush = ThemedDrawing.GetBrush(this, "SignalBrush", glowAlpha, Color.FromRgb(46, 196, 182));
            var dotBrush = ThemedDrawing.GetBrush(this, "SignalBrush", dotAlpha, Color.FromRgb(46, 196, 182));

            drawingContext.DrawEllipse(glowBrush, null, point, sample.Size + 5, sample.Size + 5);
            drawingContext.DrawEllipse(dotBrush, null, point, sample.Size, sample.Size);
        }
    }

    private void DrawAxisLabels(DrawingContext drawingContext, double centerX, double baseY, double radius)
    {
        var labelBrush = ThemedDrawing.GetBrush(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var left = CreateText("L", 10, labelBrush);
        var mid = CreateText("M", 10, labelBrush);
        var right = CreateText("R", 10, labelBrush);

        drawingContext.DrawText(left, new Point(centerX - radius - 3, baseY + 3));
        drawingContext.DrawText(mid, new Point(centerX - (mid.Width / 2), baseY - radius - 15));
        drawingContext.DrawText(right, new Point(centerX + radius - right.Width + 3, baseY + 3));
    }

    private static void DrawArc(DrawingContext drawingContext, double centerX, double baseY, double radius, Pen pen)
    {
        var arc = new StreamGeometry();
        using (var context = arc.Open())
        {
            for (var i = 0; i <= 72; i++)
            {
                var angle = Math.PI - (Math.PI * i / 72);
                var point = new Point(
                    centerX + (Math.Cos(angle) * radius),
                    baseY - (Math.Sin(angle) * radius));

                if (i == 0)
                {
                    context.BeginFigure(point, false, false);
                }
                else
                {
                    context.LineTo(point, true, false);
                }
            }
        }

        drawingContext.DrawGeometry(null, pen, arc);
    }

    private static Point ProjectToFan(double x, double depth, double centerX, double baseY, double radius)
    {
        var pointX = centerX + (x * radius);
        var pointY = baseY - (depth * radius);
        var dx = pointX - centerX;
        var dy = baseY - pointY;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));

        if (distance > radius)
        {
            var scale = radius / distance;
            pointX = centerX + (dx * scale);
            pointY = baseY - (dy * scale);
        }

        return new Point(pointX, pointY);
    }

    private FormattedText CreateText(string text, double size, Brush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            TextTypeface,
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private readonly record struct PointScale(double FloorDb, double CeilingDb)
    {
        public double Normalize(double radius)
        {
            if (CeilingDb <= FloorDb)
            {
                return 0;
            }

            return Math.Clamp((RadiusToDb(radius) - FloorDb) / (CeilingDb - FloorDb), 0, 1);
        }
    }
}
