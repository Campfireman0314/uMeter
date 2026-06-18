using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class CorrelationMeter : FrameworkElement
{
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
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(174, 183, 194));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(244, 247, 250));
    private static readonly Brush ArrowBrush = new SolidColorBrush(Color.FromRgb(255, 176, 0));
    private static readonly Brush MutedArrowBrush = new SolidColorBrush(Color.FromRgb(94, 104, 116));
    private static readonly Pen TickPen = new(new SolidColorBrush(Color.FromArgb(160, 174, 183, 194)), 1);
    private static readonly Pen TrackStroke = new(new SolidColorBrush(Color.FromRgb(58, 66, 77)), 1);

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

        var label = CreateText("Correlation", 12, LabelBrush, TextTypeface);
        var value = CreateText(ValueText, 13, TextBrush, ValueTypeface);
        drawingContext.DrawText(label, new Point(0, 3));
        drawingContext.DrawText(value, new Point(Math.Max(0, width - value.Width), 2));

        var bar = new Rect(4, 25, Math.Max(40, width - 8), 8);
        drawingContext.DrawRoundedRectangle(CreateTrackBrush(), TrackStroke, bar, 4, 4);

        var hasValue = !double.IsNaN(Value);
        var normalized = hasValue ? Math.Clamp((Value + 1) / 2, 0, 1) : 0.5;
        var markerX = bar.Left + (bar.Width * normalized);

        foreach (var tick in new[] { -1d, 0d, 1d })
        {
            var tickX = bar.Left + (bar.Width * ((tick + 1) / 2));
            drawingContext.DrawLine(TickPen, new Point(tickX, bar.Top - 5), new Point(tickX, bar.Bottom + 5));
            var tickLabel = tick > 0 ? "+1" : tick.ToString("0", CultureInfo.InvariantCulture);
            var text = CreateText(tickLabel, 9, LabelBrush, TextTypeface);
            var textX = Math.Clamp(tickX - (text.Width / 2), bar.Left, bar.Right - text.Width);
            drawingContext.DrawText(text, new Point(textX, bar.Bottom + 8));
        }

        DrawArrow(drawingContext, markerX, bar.Top - 4, hasValue ? ArrowBrush : MutedArrowBrush);
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

    private static LinearGradientBrush CreateTrackBrush()
    {
        return new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(255, 90, 95), 0),
                new GradientStop(Color.FromRgb(255, 176, 0), 0.5),
                new GradientStop(Color.FromRgb(46, 196, 182), 1)
            ],
            new Point(0, 0.5),
            new Point(1, 0.5));
    }
}
