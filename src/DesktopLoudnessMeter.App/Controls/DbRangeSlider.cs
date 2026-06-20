using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class DbRangeSlider : FrameworkElement
{
    private const double DefaultMinimum = -96;
    private const double DefaultMaximum = 0;
    private const double DefaultStep = 1;
    private const double ThumbRadius = 7;
    private const double HaloRadius = 12;
    private const double TrackHeight = 8;
    private const double SelectedTrackHeight = 6;
    private const double HorizontalPadding = HaloRadius + 2;
    private const double PreferredWidth = 264;
    private const double PreferredHeight = 54;
    private DragThumb? _dragThumb;
    private DragThumb? _hotThumb;

    public DbRangeSlider()
    {
        Cursor = Cursors.SizeWE;
        Focusable = true;
        SnapsToDevicePixels = true;
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultMinimum, OnBoundsChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultMaximum, OnBoundsChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultStep, OnBoundsChanged));

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
        nameof(LowerValue),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(
            DefaultMinimum,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceLowerValue));

    public double LowerValue
    {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
        nameof(UpperValue),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(
            DefaultMaximum,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceUpperValue));

    public double UpperValue
    {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? PreferredWidth : availableSize.Width;
        return new Size(Math.Max(180, width), PreferredHeight);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var track = TrackRect();
        if (track.Width <= 0 || track.Height <= 0)
        {
            return;
        }

        var lowerX = ValueToX(LowerValue);
        var upperX = ValueToX(UpperValue);
        var centerY = track.Top + (track.Height / 2);
        var accent = ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
        var warm = ThemedDrawing.GetColor(this, "SpectrumWarmBrush", Color.FromRgb(255, 176, 0));
        var text = ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(238, 241, 245));
        var subtle = ThemedDrawing.GetBrush(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var opacity = IsEnabled ? 1.0 : 0.48;

        drawingContext.PushOpacity(opacity);
        DrawTrack(drawingContext, track, lowerX, upperX, accent, warm);
        DrawTicks(drawingContext, track, subtle);
        DrawEndpointLabels(drawingContext, track, subtle);
        DrawThumb(drawingContext, new Point(lowerX, centerY), accent, _hotThumb == DragThumb.Lower || _dragThumb == DragThumb.Lower, text);
        DrawThumb(drawingContext, new Point(upperX, centerY), warm, _hotThumb == DragThumb.Upper || _dragThumb == DragThumb.Upper, text);
        drawingContext.Pop();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        _dragThumb = ChooseThumb(e.GetPosition(this));
        _hotThumb = _dragThumb;
        CaptureMouse();
        UpdateDrag(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);
        if (_dragThumb is not null && IsMouseCaptured)
        {
            UpdateDrag(position);
            e.Handled = true;
            return;
        }

        var nextHotThumb = HitThumb(position);
        if (nextHotThumb != _hotThumb)
        {
            _hotThumb = nextHotThumb;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragThumb is null)
        {
            return;
        }

        _dragThumb = null;
        ReleaseMouseCapture();
        _hotThumb = HitThumb(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (IsMouseCaptured)
        {
            return;
        }

        _hotThumb = null;
        InvalidateVisual();
    }

    private void DrawTrack(DrawingContext drawingContext, Rect track, double lowerX, double upperX, Color accent, Color warm)
    {
        drawingContext.DrawRoundedRectangle(
            ThemedDrawing.GetBrush(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25)),
            ThemedDrawing.GetPen(this, "ControlBorderBrush", 1, Color.FromRgb(58, 66, 77)),
            track,
            TrackHeight / 2,
            TrackHeight / 2);

        if (upperX <= lowerX)
        {
            return;
        }

        var selected = new Rect(
            lowerX,
            track.Top + ((track.Height - SelectedTrackHeight) / 2),
            upperX - lowerX,
            SelectedTrackHeight);
        var selectedBrush = new LinearGradientBrush(
            [
                new GradientStop(ThemedDrawing.WithAlpha(accent, 216), 0),
                new GradientStop(ThemedDrawing.WithAlpha(warm, 226), 1)
            ],
            new Point(0, 0),
            new Point(1, 0));
        drawingContext.DrawRoundedRectangle(selectedBrush, null, selected, SelectedTrackHeight / 2, SelectedTrackHeight / 2);
    }

    private void DrawTicks(DrawingContext drawingContext, Rect track, Brush subtle)
    {
        var pen = new Pen(subtle, 1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var tickValues = new[] { Minimum, Minimum + ((Maximum - Minimum) * 0.25), Minimum + ((Maximum - Minimum) * 0.5), Minimum + ((Maximum - Minimum) * 0.75), Maximum };
        foreach (var value in tickValues)
        {
            var x = ValueToX(value);
            drawingContext.DrawLine(pen, new Point(x, track.Bottom + 4), new Point(x, track.Bottom + 9));
        }
    }

    private void DrawEndpointLabels(DrawingContext drawingContext, Rect track, Brush subtle)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var minimum = Label($"{Minimum:0}", subtle, pixelsPerDip);
        var maximum = Label($"{Maximum:0}", subtle, pixelsPerDip);
        drawingContext.DrawText(minimum, new Point(track.Left, track.Bottom + 12));
        drawingContext.DrawText(maximum, new Point(track.Right - maximum.Width, track.Bottom + 12));
    }

    private void DrawThumb(DrawingContext drawingContext, Point center, Color accent, bool isHot, Brush fill)
    {
        var haloAlpha = isHot ? (byte)58 : (byte)34;
        var strokeAlpha = isHot ? (byte)246 : (byte)210;
        var haloBrush = new SolidColorBrush(ThemedDrawing.WithAlpha(accent, haloAlpha));
        var stroke = new Pen(new SolidColorBrush(ThemedDrawing.WithAlpha(accent, strokeAlpha)), 2);
        drawingContext.DrawEllipse(haloBrush, null, center, HaloRadius, HaloRadius);
        drawingContext.DrawEllipse(fill, stroke, center, ThumbRadius, ThumbRadius);
        drawingContext.DrawEllipse(new SolidColorBrush(ThemedDrawing.WithAlpha(accent, 46)), null, center, ThumbRadius - 3, ThumbRadius - 3);
    }

    private FormattedText Label(string text, Brush brush, double pixelsPerDip)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            brush,
            pixelsPerDip);
    }

    private Rect TrackRect()
    {
        var width = Math.Max(1, ActualWidth - (HorizontalPadding * 2));
        var centerY = Math.Clamp(ActualHeight * 0.43, HaloRadius + 2, Math.Max(HaloRadius + 2, ActualHeight - 24));
        return new Rect(HorizontalPadding, centerY - (TrackHeight / 2), width, TrackHeight);
    }

    private double ValueToX(double value)
    {
        var track = TrackRect();
        var span = Math.Max(0.0001, Maximum - Minimum);
        var ratio = Math.Clamp((value - Minimum) / span, 0, 1);
        return track.Left + (track.Width * ratio);
    }

    private double XToValue(double x)
    {
        var track = TrackRect();
        var span = Math.Max(0.0001, Maximum - Minimum);
        var ratio = Math.Clamp((x - track.Left) / track.Width, 0, 1);
        return Minimum + (span * ratio);
    }

    private DragThumb ChooseThumb(Point position)
    {
        return HitThumb(position) ?? (Math.Abs(position.X - ValueToX(LowerValue)) <= Math.Abs(position.X - ValueToX(UpperValue))
            ? DragThumb.Lower
            : DragThumb.Upper);
    }

    private DragThumb? HitThumb(Point position)
    {
        var y = TrackRect().Top + (TrackRect().Height / 2);
        var lowerDistance = Distance(position, new Point(ValueToX(LowerValue), y));
        var upperDistance = Distance(position, new Point(ValueToX(UpperValue), y));
        var hitRadius = HaloRadius + 2;
        if (lowerDistance > hitRadius && upperDistance > hitRadius)
        {
            return null;
        }

        return lowerDistance <= upperDistance ? DragThumb.Lower : DragThumb.Upper;
    }

    private void UpdateDrag(Point position)
    {
        if (_dragThumb is null)
        {
            return;
        }

        var value = Snap(XToValue(position.X), EffectiveStep());
        var separation = EffectiveStep();
        if (_dragThumb == DragThumb.Lower)
        {
            SetCurrentValue(LowerValueProperty, Math.Clamp(value, Minimum, UpperValue - separation));
        }
        else
        {
            SetCurrentValue(UpperValueProperty, Math.Clamp(value, LowerValue + separation, Maximum));
        }
    }

    private double EffectiveStep()
    {
        return double.IsFinite(Step) && Step > 0 ? Step : DefaultStep;
    }

    private static double Distance(Point first, Point second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt((x * x) + (y * y));
    }

    private static void OnBoundsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not DbRangeSlider slider)
        {
            return;
        }

        slider.CoerceValue(LowerValueProperty);
        slider.CoerceValue(UpperValueProperty);
        slider.InvalidateVisual();
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is DbRangeSlider slider)
        {
            slider.InvalidateVisual();
        }
    }

    private static object CoerceLowerValue(DependencyObject dependencyObject, object baseValue)
    {
        var slider = (DbRangeSlider)dependencyObject;
        var separation = slider.EffectiveStep();
        var minimum = Math.Min(slider.Minimum, slider.Maximum - separation);
        var maximum = Math.Max(minimum, slider.UpperValue - separation);
        return Snap(Math.Clamp(AsFinite(baseValue, minimum), minimum, maximum), separation);
    }

    private static object CoerceUpperValue(DependencyObject dependencyObject, object baseValue)
    {
        var slider = (DbRangeSlider)dependencyObject;
        var separation = slider.EffectiveStep();
        var maximum = Math.Max(slider.Maximum, slider.Minimum + separation);
        var minimum = Math.Min(maximum, slider.LowerValue + separation);
        return Snap(Math.Clamp(AsFinite(baseValue, maximum), minimum, maximum), separation);
    }

    private static double AsFinite(object value, double fallback)
    {
        return value is double number && double.IsFinite(number) ? number : fallback;
    }

    private static double Snap(double value, double step)
    {
        return Math.Round(value / step, 0, MidpointRounding.AwayFromZero) * step;
    }

    private enum DragThumb
    {
        Lower,
        Upper
    }
}
