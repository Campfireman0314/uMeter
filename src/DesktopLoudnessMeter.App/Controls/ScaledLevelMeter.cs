using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class ScaledLevelMeter : FrameworkElement
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata("n/a", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelIndentProperty = DependencyProperty.Register(
        nameof(LabelIndent),
        typeof(double),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueSubtextProperty = DependencyProperty.Register(
        nameof(ValueSubtext),
        typeof(string),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level),
        typeof(double),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ScaleMinimumProperty = DependencyProperty.Register(
        nameof(ScaleMinimum),
        typeof(double),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(-60d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ScaleMaximumProperty = DependencyProperty.Register(
        nameof(ScaleMaximum),
        typeof(double),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TickValuesProperty = DependencyProperty.Register(
        nameof(TickValues),
        typeof(string),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata("-60,-48,-36,-24,-12,0", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueSeverityProperty = DependencyProperty.Register(
        nameof(ValueSeverity),
        typeof(ReadoutSeverity),
        typeof(ScaledLevelMeter),
        new FrameworkPropertyMetadata(ReadoutSeverity.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Typeface TextTypeface = new("Segoe UI");
    private static readonly Typeface ValueTypeface = new(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(174, 183, 194));
    private static readonly Brush ValueSubtleBrush = new SolidColorBrush(Color.FromRgb(215, 224, 234));
    private static readonly Brush ValueMutedBrush = new SolidColorBrush(Color.FromRgb(111, 122, 134));
    private static readonly Brush TrackBrush = new SolidColorBrush(Color.FromRgb(17, 20, 25));
    private static readonly Pen TrackStroke = new(new SolidColorBrush(Color.FromRgb(58, 66, 77)), 1);
    private static readonly Pen TickPen = new(new SolidColorBrush(Color.FromArgb(130, 174, 183, 194)), 1);
    private static readonly Brush MarkerBrush = new SolidColorBrush(Color.FromRgb(255, 176, 0));
    private static readonly Brush ValueNormalBackground = new SolidColorBrush(Color.FromRgb(39, 48, 58));
    private static readonly Brush ValueMutedBackground = new SolidColorBrush(Color.FromRgb(34, 40, 50));
    private static readonly Brush ValueNormalBorder = new SolidColorBrush(Color.FromRgb(66, 80, 94));
    private static readonly Brush ValueWarmBackground = new SolidColorBrush(Color.FromRgb(74, 59, 30));
    private static readonly Brush ValueWarmBorder = new SolidColorBrush(Color.FromRgb(255, 176, 0));
    private static readonly Brush ValueWarmForeground = new SolidColorBrush(Color.FromRgb(255, 232, 168));
    private static readonly Brush ValueHotBackground = new SolidColorBrush(Color.FromRgb(118, 53, 29));
    private static readonly Brush ValueHotBorder = new SolidColorBrush(Color.FromRgb(255, 138, 61));
    private static readonly Brush ValueHotForeground = new SolidColorBrush(Color.FromRgb(255, 240, 232));
    private static readonly Brush ValueCriticalBackground = new SolidColorBrush(Color.FromRgb(201, 42, 42));
    private static readonly Brush ValueCriticalBorder = new SolidColorBrush(Color.FromRgb(255, 107, 107));
    private static readonly Brush ValueOverBackground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
    private static readonly Brush ValueOverBorder = new SolidColorBrush(Color.FromRgb(255, 45, 45));
    private static readonly Brush ValueOverForeground = new SolidColorBrush(Color.FromRgb(176, 0, 32));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public double LabelIndent
    {
        get => (double)GetValue(LabelIndentProperty);
        set => SetValue(LabelIndentProperty, value);
    }

    public string ValueSubtext
    {
        get => (string)GetValue(ValueSubtextProperty);
        set => SetValue(ValueSubtextProperty, value);
    }

    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public double ScaleMinimum
    {
        get => (double)GetValue(ScaleMinimumProperty);
        set => SetValue(ScaleMinimumProperty, value);
    }

    public double ScaleMaximum
    {
        get => (double)GetValue(ScaleMaximumProperty);
        set => SetValue(ScaleMaximumProperty, value);
    }

    public string TickValues
    {
        get => (string)GetValue(TickValuesProperty);
        set => SetValue(TickValuesProperty, value);
    }

    public ReadoutSeverity ValueSeverity
    {
        get => (ReadoutSeverity)GetValue(ValueSeverityProperty);
        set => SetValue(ValueSeverityProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 46);
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

        var labelWidth = 138d + Math.Max(0, LabelIndent);
        var valueText = CreateText(ValueText, 13, GetValueForeground(ValueSeverity), ValueTypeface);
        var valueSubtext = CreateText(ValueSubtext, 11, GetValueForeground(ValueSeverity), ValueTypeface);
        var hasValueSubtext = !string.IsNullOrWhiteSpace(ValueSubtext);
        const double valueWidth = 100;
        const double gap = 8;
        const double barY = 10;
        const double barHeight = 12;

        var barX = labelWidth;
        var barWidth = Math.Max(48, width - labelWidth - valueWidth - (gap * 2));
        var valueX = barX + barWidth + gap;
        var valueRect = new Rect(valueX, 2, Math.Min(valueWidth, Math.Max(0, width - valueX)), 38);
        var bar = new Rect(barX, barY, barWidth, barHeight);

        drawingContext.DrawText(CreateText(Label, 12, LabelBrush, TextTypeface), new Point(Math.Max(0, LabelIndent), 6));
        DrawValueReadout(drawingContext, valueText, valueSubtext, valueRect, hasValueSubtext);

        drawingContext.DrawRoundedRectangle(TrackBrush, TrackStroke, bar, 6, 6);

        var level = Clamp01(Level);
        if (level > 0)
        {
            var fillBrush = CreateFillBrush();
            drawingContext.PushClip(new RectangleGeometry(new Rect(bar.Left, bar.Top, bar.Width * level, bar.Height)));
            drawingContext.DrawRoundedRectangle(fillBrush, null, bar, 6, 6);
            drawingContext.Pop();

            var markerX = bar.Left + (bar.Width * level);
            drawingContext.DrawLine(new Pen(MarkerBrush, 1.4), new Point(markerX, bar.Top - 1), new Point(markerX, bar.Bottom + 2));
        }

        DrawTicks(drawingContext, bar);
    }

    private void DrawTicks(DrawingContext drawingContext, Rect bar)
    {
        if (ScaleMaximum <= ScaleMinimum)
        {
            return;
        }

        foreach (var token in TickValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!double.TryParse(token.Replace("+", string.Empty, StringComparison.Ordinal), NumberStyles.Float, CultureInfo.InvariantCulture, out var tick))
            {
                continue;
            }

            var normalized = Clamp01((tick - ScaleMinimum) / (ScaleMaximum - ScaleMinimum));
            var tickX = bar.Left + (bar.Width * normalized);
            drawingContext.DrawLine(TickPen, new Point(tickX, bar.Top - 4), new Point(tickX, bar.Bottom + 5));

            var tickText = CreateText(token, 9, LabelBrush, TextTypeface);
            var tickLabelX = Math.Clamp(tickX - (tickText.Width / 2), bar.Left, bar.Right - tickText.Width);
            drawingContext.DrawText(tickText, new Point(tickLabelX, bar.Bottom + 8));
        }
    }

    private void DrawValueReadout(
        DrawingContext drawingContext,
        FormattedText valueText,
        FormattedText valueSubtext,
        Rect valueRect,
        bool hasValueSubtext)
    {
        var readout = hasValueSubtext
            ? new Rect(valueRect.Left, 2, valueRect.Width, 36)
            : new Rect(valueRect.Left, 3, valueRect.Width, 22);
        drawingContext.DrawRoundedRectangle(
            GetValueBackground(ValueSeverity),
            new Pen(GetValueBorder(ValueSeverity), 1),
            readout,
            5,
            5);
        DrawValueTextStack(drawingContext, valueText, valueSubtext, readout, hasValueSubtext);
    }

    private static void DrawValueTextStack(
        DrawingContext drawingContext,
        FormattedText valueText,
        FormattedText valueSubtext,
        Rect bounds,
        bool hasValueSubtext)
    {
        if (!hasValueSubtext)
        {
            drawingContext.DrawText(valueText, new Point(bounds.Right - valueText.Width - 6, bounds.Top + 3));
            return;
        }

        drawingContext.DrawText(valueText, new Point(bounds.Right - valueText.Width - 6, bounds.Top + 3));
        drawingContext.DrawText(valueSubtext, new Point(bounds.Right - valueSubtext.Width - 6, bounds.Top + 19));
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

    private static LinearGradientBrush CreateFillBrush()
    {
        return new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(46, 196, 182), 0),
                new GradientStop(Color.FromRgb(46, 196, 182), 0.62),
                new GradientStop(Color.FromRgb(255, 176, 0), 0.86),
                new GradientStop(Color.FromRgb(255, 90, 95), 1)
            ],
            new Point(0, 0.5),
            new Point(1, 0.5));
    }

    private static Brush GetValueBackground(ReadoutSeverity severity)
    {
        return severity switch
        {
            ReadoutSeverity.Muted => ValueMutedBackground,
            ReadoutSeverity.Warm => ValueWarmBackground,
            ReadoutSeverity.Hot => ValueHotBackground,
            ReadoutSeverity.Critical => ValueCriticalBackground,
            ReadoutSeverity.Over => ValueOverBackground,
            _ => ValueNormalBackground
        };
    }

    private static Brush GetValueBorder(ReadoutSeverity severity)
    {
        return severity switch
        {
            ReadoutSeverity.Muted => ValueNormalBorder,
            ReadoutSeverity.Warm => ValueWarmBorder,
            ReadoutSeverity.Hot => ValueHotBorder,
            ReadoutSeverity.Critical => ValueCriticalBorder,
            ReadoutSeverity.Over => ValueOverBorder,
            _ => ValueNormalBorder
        };
    }

    private static Brush GetValueForeground(ReadoutSeverity severity)
    {
        return severity switch
        {
            ReadoutSeverity.Muted => ValueMutedBrush,
            ReadoutSeverity.Warm => ValueWarmForeground,
            ReadoutSeverity.Hot => ValueHotForeground,
            ReadoutSeverity.Critical => Brushes.White,
            ReadoutSeverity.Over => ValueOverForeground,
            _ => ValueSubtleBrush
        };
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }
}
