using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class LoudnessClockView : FrameworkElement
{
    private const int MaximumSampleCount = 1440;
    private const double InnerRadiusRatio = 0.18;
    private const double DefaultCycleDurationSeconds = 30;
    private readonly List<LoudnessClockSample> _samples = [];
    private long? _startTimestamp;

    public static readonly DependencyProperty CurrentLoudnessDbProperty = DependencyProperty.Register(
        nameof(CurrentLoudnessDb),
        typeof(double),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CurrentLoudnessDb
    {
        get => (double)GetValue(CurrentLoudnessDbProperty);
        set => SetValue(CurrentLoudnessDbProperty, value);
    }

    public static readonly DependencyProperty PlotTickProperty = DependencyProperty.Register(
        nameof(PlotTick),
        typeof(long),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(0L, PlotTickChanged));

    public long PlotTick
    {
        get => (long)GetValue(PlotTickProperty);
        set => SetValue(PlotTickProperty, value);
    }

    public static readonly DependencyProperty MinimumLoudnessDbProperty = DependencyProperty.Register(
        nameof(MinimumLoudnessDb),
        typeof(double),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(-32d, DisplayRangeChanged));

    public double MinimumLoudnessDb
    {
        get => (double)GetValue(MinimumLoudnessDbProperty);
        set => SetValue(MinimumLoudnessDbProperty, value);
    }

    public static readonly DependencyProperty MaximumLoudnessDbProperty = DependencyProperty.Register(
        nameof(MaximumLoudnessDb),
        typeof(double),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(-4d, DisplayRangeChanged));

    public double MaximumLoudnessDb
    {
        get => (double)GetValue(MaximumLoudnessDbProperty);
        set => SetValue(MaximumLoudnessDbProperty, value);
    }

    public static readonly DependencyProperty WarningLoudnessDbProperty = DependencyProperty.Register(
        nameof(WarningLoudnessDb),
        typeof(double),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(-12d, DisplayRangeChanged));

    public double WarningLoudnessDb
    {
        get => (double)GetValue(WarningLoudnessDbProperty);
        set => SetValue(WarningLoudnessDbProperty, value);
    }

    public static readonly DependencyProperty CycleDurationSecondsProperty = DependencyProperty.Register(
        nameof(CycleDurationSeconds),
        typeof(double),
        typeof(LoudnessClockView),
        new FrameworkPropertyMetadata(DefaultCycleDurationSeconds, CycleDurationChanged));

    public double CycleDurationSeconds
    {
        get => (double)GetValue(CycleDurationSecondsProperty);
        set => SetValue(CycleDurationSecondsProperty, value);
    }

    public LoudnessClockView()
    {
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = double.IsInfinity(availableSize.Width)
            ? 220
            : Math.Min(availableSize.Width, double.IsInfinity(availableSize.Height) ? availableSize.Width : availableSize.Height);
        side = Math.Clamp(side, 180, 320);
        return new Size(side, side);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0)
        {
            return;
        }

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var outerRadius = Math.Max(1, (side / 2) - 6);
        var innerRadius = outerRadius * InnerRadiusRatio;
        DrawGrid(drawingContext, center, innerRadius, outerRadius);
        DrawFan(drawingContext, center, innerRadius, outerRadius);
        DrawScanLine(drawingContext, center, innerRadius, outerRadius);
    }

    private static void PlotTickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not LoudnessClockView view)
        {
            return;
        }

        view.AppendCurrentSample();
    }

    private static void DisplayRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is LoudnessClockView view)
        {
            view.InvalidateVisual();
        }
    }

    private static void CycleDurationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is LoudnessClockView view)
        {
            view.TrimSamples(view.CurrentElapsedSeconds(), view.EffectiveCycleDurationSeconds());
            view.InvalidateVisual();
        }
    }

    private void AppendCurrentSample()
    {
        if (!double.IsFinite(CurrentLoudnessDb))
        {
            if (_samples.Count > 0)
            {
                _samples.Clear();
                _startTimestamp = null;
                InvalidateVisual();
            }

            return;
        }

        var now = Stopwatch.GetTimestamp();
        _startTimestamp ??= now;
        var elapsedSeconds = TimestampDeltaSeconds(_startTimestamp.Value, now);
        _samples.Add(new LoudnessClockSample(CurrentLoudnessDb, elapsedSeconds));
        TrimSamples(elapsedSeconds, EffectiveCycleDurationSeconds());
        InvalidateVisual();
    }

    private void DrawGrid(DrawingContext drawingContext, Point center, double innerRadius, double outerRadius)
    {
        var gridPen = ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 44, 1);
        var innerPen = ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 84, 1);
        var warningPen = ThemedDrawing.GetPen(this, PlotColorRole.SpectrumWarm, 126, 1);
        var textBrush = ThemedDrawing.GetBrush(this, PlotColorRole.Subtle, 180);
        foreach (var marker in LoudnessClockScalePlanner.Build(MinimumLoudnessDb, MaximumLoudnessDb))
        {
            var radius = RadiusForLoudness(marker, innerRadius, outerRadius);
            var pen = Math.Abs(marker - MinimumLoudnessDb) < 0.5 ? innerPen : gridPen;
            drawingContext.DrawEllipse(null, pen, center, radius, radius);
            drawingContext.DrawText(
                Label($"{marker:0}", textBrush),
                new Point(center.X + radius + 4, center.Y - 9));
        }

        if (WarningLoudnessDb > MinimumLoudnessDb && WarningLoudnessDb < MaximumLoudnessDb)
        {
            var warningRadius = RadiusForLoudness(WarningLoudnessDb, innerRadius, outerRadius);
            drawingContext.DrawEllipse(null, warningPen, center, warningRadius, warningRadius);
        }
    }

    private void DrawFan(DrawingContext drawingContext, Point center, double innerRadius, double outerRadius)
    {
        if (_samples.Count < 2)
        {
            return;
        }

        var cycleDurationSeconds = EffectiveCycleDurationSeconds();
        var normalFan = new StreamGeometry();
        var warningFan = new StreamGeometry();
        var overFan = new StreamGeometry();
        var normalTrace = new StreamGeometry();
        var warningTrace = new StreamGeometry();
        var overTrace = new StreamGeometry();
        using (var normalFanContext = normalFan.Open())
        using (var warningFanContext = warningFan.Open())
        using (var overFanContext = overFan.Open())
        using (var normalTraceContext = normalTrace.Open())
        using (var warningTraceContext = warningTrace.Open())
        using (var overTraceContext = overTrace.Open())
        {
            LoudnessClockMappedSample? previous = null;
            for (var index = 0; index < _samples.Count; index++)
            {
                var point = LoudnessClockPointMapper.Map(
                    _samples[index].LoudnessDb,
                    MinimumLoudnessDb,
                    MaximumLoudnessDb,
                    innerRadius,
                    outerRadius,
                    _samples[index].ElapsedSeconds,
                    cycleDurationSeconds,
                    warningDb: WarningLoudnessDb);
                if (point is null)
                {
                    previous = null;
                    continue;
                }

                var phase = CyclePhase(_samples[index].ElapsedSeconds, cycleDurationSeconds);
                var current = new LoudnessClockMappedSample(point.Value, phase);
                if (previous is { } last)
                {
                    AppendFanSegmentPlan(
                        normalFanContext,
                        warningFanContext,
                        overFanContext,
                        normalTraceContext,
                        warningTraceContext,
                        overTraceContext,
                        center,
                        innerRadius,
                        LoudnessClockFanSegmentPlanner.Build(last, current));
                }

                previous = current;
            }
        }

        FreezeIfPossible(normalFan);
        FreezeIfPossible(warningFan);
        FreezeIfPossible(overFan);
        FreezeIfPossible(normalTrace);
        FreezeIfPossible(warningTrace);
        FreezeIfPossible(overTrace);

        drawingContext.DrawGeometry(
            ThemedDrawing.GetBrush(this, PlotColorRole.Signal, 44),
            null,
            normalFan);
        drawingContext.DrawGeometry(
            ThemedDrawing.GetBrush(this, PlotColorRole.SpectrumWarm, 72),
            null,
            warningFan);
        drawingContext.DrawGeometry(
            ThemedDrawing.GetBrush(this, PlotColorRole.Danger, 128),
            null,
            overFan);
        drawingContext.DrawGeometry(
            null,
            ThemedDrawing.GetPen(this, PlotColorRole.Signal, 1.15),
            normalTrace);
        drawingContext.DrawGeometry(
            null,
            ThemedDrawing.GetPen(this, PlotColorRole.SpectrumWarm, 1.35),
            warningTrace);
        drawingContext.DrawGeometry(
            null,
            ThemedDrawing.GetPen(this, PlotColorRole.Danger, 2.15),
            overTrace);
    }

    private void DrawScanLine(DrawingContext drawingContext, Point center, double innerRadius, double outerRadius)
    {
        if (!double.IsFinite(CurrentLoudnessDb))
        {
            return;
        }

        var cycleDurationSeconds = EffectiveCycleDurationSeconds();
        var elapsedSeconds = CurrentElapsedSeconds();
        var point = LoudnessClockPointMapper.Map(
            CurrentLoudnessDb,
            MinimumLoudnessDb,
            MaximumLoudnessDb,
            innerRadius,
            outerRadius,
            elapsedSeconds,
            cycleDurationSeconds,
            warningDb: WarningLoudnessDb);
        var angle = point?.AngleRadians ?? AngleForElapsed(elapsedSeconds, cycleDurationSeconds);
        var inner = ToInnerPoint(center, angle, innerRadius);
        drawingContext.DrawLine(
            ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 82, 1.05),
            inner,
            ToInnerPoint(center, angle, outerRadius));

        if (point is not { } current)
        {
            return;
        }

        var role = ColorRoleFor(current);
        var currentPosition = ToPoint(center, current);
        drawingContext.DrawLine(
            ThemedDrawing.GetPen(this, role, 228, current.IsOverMaximum ? 2.45 : 1.8),
            inner,
            currentPosition);
        drawingContext.DrawEllipse(
            ThemedDrawing.GetBrush(this, role, 230),
            ThemedDrawing.GetPen(this, PlotColorRole.Text, 96, 0.75),
            currentPosition,
            current.IsOverMaximum ? 5.2 : 3.8,
            current.IsOverMaximum ? 5.2 : 3.8);
    }

    private void TrimSamples(double elapsedSeconds, double cycleDurationSeconds)
    {
        while (_samples.Count > 0 && elapsedSeconds - _samples[0].ElapsedSeconds > cycleDurationSeconds)
        {
            _samples.RemoveAt(0);
        }

        while (_samples.Count > MaximumSampleCount)
        {
            _samples.RemoveAt(0);
        }
    }

    private double CurrentElapsedSeconds()
    {
        return _startTimestamp is long startTimestamp
            ? TimestampDeltaSeconds(startTimestamp, Stopwatch.GetTimestamp())
            : 0;
    }

    private double EffectiveCycleDurationSeconds()
    {
        return double.IsFinite(CycleDurationSeconds) && CycleDurationSeconds > 0
            ? CycleDurationSeconds
            : DefaultCycleDurationSeconds;
    }

    private double RadiusForLoudness(double loudnessDb, double innerRadius, double outerRadius)
    {
        var effectiveMaximum = MaximumLoudnessDb <= MinimumLoudnessDb ? MinimumLoudnessDb + 1 : MaximumLoudnessDb;
        var ratio = Math.Clamp((loudnessDb - MinimumLoudnessDb) / (effectiveMaximum - MinimumLoudnessDb), 0, 1);
        return innerRadius + ((outerRadius - innerRadius) * ratio);
    }

    private static void AppendFanWedge(
        StreamGeometryContext context,
        Point center,
        double innerRadius,
        LoudnessClockPoint first,
        LoudnessClockPoint second)
    {
        context.BeginFigure(ToInnerPoint(center, first.AngleRadians, innerRadius), true, true);
        context.LineTo(ToPoint(center, first), true, false);
        context.LineTo(ToPoint(center, second), true, false);
        context.LineTo(ToInnerPoint(center, second.AngleRadians, innerRadius), true, false);
    }

    private static void AppendTraceSegment(
        StreamGeometryContext context,
        Point center,
        LoudnessClockPoint first,
        LoudnessClockPoint second)
    {
        context.BeginFigure(ToPoint(center, first), false, false);
        context.LineTo(ToPoint(center, second), true, false);
    }

    private static void AppendFanSegmentPlan(
        StreamGeometryContext normalFanContext,
        StreamGeometryContext warningFanContext,
        StreamGeometryContext overFanContext,
        StreamGeometryContext normalTraceContext,
        StreamGeometryContext warningTraceContext,
        StreamGeometryContext overTraceContext,
        Point center,
        double innerRadius,
        LoudnessClockFanSegmentPair plan)
    {
        AppendFanSegment(
            normalFanContext,
            warningFanContext,
            overFanContext,
            normalTraceContext,
            warningTraceContext,
            overTraceContext,
            center,
            innerRadius,
            plan.First);
        if (plan.Second is { } second)
        {
            AppendFanSegment(
                normalFanContext,
                warningFanContext,
                overFanContext,
                normalTraceContext,
                warningTraceContext,
                overTraceContext,
                center,
                innerRadius,
                second);
        }
    }

    private static void AppendFanSegment(
        StreamGeometryContext normalFanContext,
        StreamGeometryContext warningFanContext,
        StreamGeometryContext overFanContext,
        StreamGeometryContext normalTraceContext,
        StreamGeometryContext warningTraceContext,
        StreamGeometryContext overTraceContext,
        Point center,
        double innerRadius,
        LoudnessClockFanSegment segment)
    {
        var role = ColorRoleFor(segment);
        var fanContext = role switch
        {
            PlotColorRole.Danger => overFanContext,
            PlotColorRole.SpectrumWarm => warningFanContext,
            _ => normalFanContext
        };
        var traceContext = role switch
        {
            PlotColorRole.Danger => overTraceContext,
            PlotColorRole.SpectrumWarm => warningTraceContext,
            _ => normalTraceContext
        };
        AppendFanWedge(fanContext, center, innerRadius, segment.First, segment.Second);
        AppendTraceSegment(traceContext, center, segment.First, segment.Second);
    }

    private static PlotColorRole ColorRoleFor(LoudnessClockFanSegment segment)
    {
        if (segment.First.IsOverMaximum || segment.Second.IsOverMaximum)
        {
            return PlotColorRole.Danger;
        }

        return segment.First.IsWarning || segment.Second.IsWarning
            ? PlotColorRole.SpectrumWarm
            : PlotColorRole.Signal;
    }

    private static PlotColorRole ColorRoleFor(LoudnessClockPoint point)
    {
        if (point.IsOverMaximum)
        {
            return PlotColorRole.Danger;
        }

        return point.IsWarning ? PlotColorRole.SpectrumWarm : PlotColorRole.Signal;
    }

    private static void FreezeIfPossible(Freezable freezable)
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }
    }

    private FormattedText Label(string text, Brush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private static Point ToPoint(Point center, LoudnessClockPoint point)
    {
        return ToInnerPoint(center, point.AngleRadians, point.Radius);
    }

    private static Point ToInnerPoint(Point center, double angleRadians, double radius)
    {
        return new Point(
            center.X + (Math.Cos(angleRadians) * radius),
            center.Y + (Math.Sin(angleRadians) * radius));
    }

    private static double CyclePhase(double elapsedSeconds, double cycleDurationSeconds)
    {
        var phase = elapsedSeconds % cycleDurationSeconds;
        return phase < 0 ? phase + cycleDurationSeconds : phase;
    }

    private static double AngleForElapsed(double elapsedSeconds, double cycleDurationSeconds)
    {
        return (-Math.PI / 2) + (2 * Math.PI * CyclePhase(elapsedSeconds, cycleDurationSeconds) / cycleDurationSeconds);
    }

    private static double TimestampDeltaSeconds(long startTimestamp, long currentTimestamp)
    {
        return (currentTimestamp - startTimestamp) / (double)Stopwatch.Frequency;
    }

    private readonly record struct LoudnessClockSample(double LoudnessDb, double ElapsedSeconds);
}
