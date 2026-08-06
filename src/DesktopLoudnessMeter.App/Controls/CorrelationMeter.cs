using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class CorrelationMeter : FrameworkElement
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(CorrelationMeter),
        new FrameworkPropertyMetadata("Correlation", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(CorrelationMeter),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(CorrelationMeter),
        new FrameworkPropertyMetadata("n/a", FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Typeface TextTypeface = new("Segoe UI");
    private static readonly Typeface ValueTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 52);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        if (width <= 0)
        {
            return;
        }

        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, ActualHeight));

        var labelBrush = ThemedDrawing.GetBrush(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var textBrush = ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(244, 247, 250));
        var tickPen = ThemedDrawing.GetPen(this, "SubtleTextBrush", 160, 1, Color.FromRgb(174, 183, 194));
        var label = CreateText(Label, 12, labelBrush, TextTypeface);
        var value = CreateText(ValueText, 13, textBrush, ValueTypeface);
        drawingContext.DrawText(label, new Point(0, 3));
        drawingContext.DrawText(value, new Point(Math.Max(0, width - value.Width), 2));

        var bar = new Rect(4, 25, Math.Max(40, width - 8), 8);
        drawingContext.DrawRoundedRectangle(
            CreateTrackBrush(),
            ThemedDrawing.GetPen(this, "ChromeBorderBrush", 1, Color.FromRgb(58, 66, 77)),
            bar,
            4,
            4);

        var hasValue = !double.IsNaN(Value);
        var normalized = hasValue ? Math.Clamp((Value + 1) / 2, 0, 1) : 0.5;
        var markerX = bar.Left + (bar.Width * normalized);

        foreach (var tick in new[] { -1d, 0d, 1d })
        {
            var tickX = bar.Left + (bar.Width * ((tick + 1) / 2));
            drawingContext.DrawLine(tickPen, new Point(tickX, bar.Top - 5), new Point(tickX, bar.Bottom + 5));
            var tickLabel = tick > 0 ? "+1" : tick.ToString("0", CultureInfo.InvariantCulture);
            var text = CreateText(tickLabel, 9, labelBrush, TextTypeface);
            var textX = Math.Clamp(tickX - (text.Width / 2), bar.Left, bar.Right - text.Width);
            drawingContext.DrawText(text, new Point(textX, bar.Bottom + 8));
        }

        DrawArrow(
            drawingContext,
            markerX,
            bar.Top - 4,
            hasValue
                ? ThemedDrawing.GetBrush(this, "WarmBrush", Color.FromRgb(255, 176, 0))
                : ThemedDrawing.GetBrush(this, "DisabledTextBrush", Color.FromRgb(94, 104, 116)));
    }

    private static void DrawArrow(DrawingContext drawingContext, double centerX, double tipY, Brush brush)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(centerX, tipY), true, true);
            context.LineTo(new Point(centerX - 6, tipY - 11), true, false);
            context.LineTo(new Point(centerX + 6, tipY - 11), true, false);
        }

        drawingContext.DrawGeometry(brush, null, geometry);
        drawingContext.DrawLine(new Pen(brush, 1.2), new Point(centerX, tipY), new Point(centerX, tipY + 8));
    }

    private FormattedText CreateText(string text, double size, Brush brush, Typeface typeface)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private LinearGradientBrush CreateTrackBrush()
    {
        var accent = ThemedDrawing.GetColor(this, "AccentBrush", Color.FromRgb(46, 196, 182));
        var warm = ThemedDrawing.GetColor(this, "WarmBrush", Color.FromRgb(255, 176, 0));
        return new LinearGradientBrush(
            [
                new GradientStop(ThemedDrawing.Blend(Color.FromRgb(255, 90, 95), warm, 0.18), 0),
                new GradientStop(warm, 0.5),
                new GradientStop(accent, 1)
            ],
            new Point(0, 0.5),
            new Point(1, 0.5));
    }
}
