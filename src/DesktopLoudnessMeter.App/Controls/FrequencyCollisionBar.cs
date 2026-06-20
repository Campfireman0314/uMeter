using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class FrequencyCollisionBar : FrameworkElement
{
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
    private static readonly double[] MajorFrequencies = [20d, 100d, 1000d, 4000d, 12000d, 22000d];
    private DrawingGroup? _backgroundDrawing;
    private double _backgroundWidth;
    private double _backgroundHeight;
    private double _backgroundPixelsPerDip;
    private Color _backgroundTrackColor;
    private Color _backgroundOutlineColor;
    private Color _backgroundSubtleColor;
    private Point? _hoverPoint;

    public FrequencyCollisionBar()
    {
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
    }

    public static readonly DependencyProperty CollisionBandsProperty = DependencyProperty.Register(
        nameof(CollisionBands),
        typeof(IReadOnlyList<FrequencyCollisionBand>),
        typeof(FrequencyCollisionBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<FrequencyCollisionBand>? CollisionBands
    {
        get => (IReadOnlyList<FrequencyCollisionBand>?)GetValue(CollisionBandsProperty);
        set => SetValue(CollisionBandsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 54);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var track = GetTrackRect(ActualWidth);
        DrawBackground(drawingContext, track);
        DrawBands(drawingContext, track);
        DrawHoverMarker(drawingContext, track);
    }

    private void DrawBackground(DrawingContext drawingContext, Rect track)
    {
        var trackColor = ThemedDrawing.GetColor(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25));
        var outlineColor = ThemedDrawing.GetColor(this, "ChromeBorderBrush", Color.FromRgb(58, 66, 77));
        var subtleColor = ThemedDrawing.GetColor(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        if (_backgroundDrawing is null
            || !AreClose(_backgroundWidth, ActualWidth)
            || !AreClose(_backgroundHeight, ActualHeight)
            || !AreClose(_backgroundPixelsPerDip, pixelsPerDip)
            || _backgroundTrackColor != trackColor
            || _backgroundOutlineColor != outlineColor
            || _backgroundSubtleColor != subtleColor)
        {
            _backgroundDrawing = CreateBackgroundDrawing(track, trackColor, outlineColor, subtleColor, pixelsPerDip);
            _backgroundWidth = ActualWidth;
            _backgroundHeight = ActualHeight;
            _backgroundPixelsPerDip = pixelsPerDip;
            _backgroundTrackColor = trackColor;
            _backgroundOutlineColor = outlineColor;
            _backgroundSubtleColor = subtleColor;
        }

        drawingContext.DrawDrawing(_backgroundDrawing);
    }

    private static DrawingGroup CreateBackgroundDrawing(
        Rect track,
        Color trackColor,
        Color outlineColor,
        Color subtleColor,
        double pixelsPerDip)
    {
        var group = new DrawingGroup();
        using (var drawingContext = group.Open())
        {
            drawingContext.DrawRoundedRectangle(
                FrozenBrush(trackColor),
                FrozenPen(outlineColor, 255, 1),
                track,
                5,
                5);
            DrawTicks(drawingContext, track, track.Bottom + 7, subtleColor, pixelsPerDip);
        }

        if (group.CanFreeze)
        {
            group.Freeze();
        }

        return group;
    }

    private void DrawBands(DrawingContext drawingContext, Rect track)
    {
        if (CollisionBands is not { Count: > 0 } bands)
        {
            return;
        }

        var accent = ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
        var warm = ThemedDrawing.GetColor(this, "SpectrumWarmBrush", Color.FromRgb(255, 176, 0));

        foreach (var band in bands)
        {
            var start = Math.Clamp(band.StartRatio, 0, 1);
            var end = Math.Clamp(band.EndRatio, start, 1);
            var x = track.Left + (track.Width * start);
            var width = Math.Max(2, track.Width * (end - start));
            var intensity = Math.Clamp(band.Intensity, 0, 1);
            var core = new Rect(x, track.Top + 2, Math.Min(width, track.Right - x), track.Height - 4);
            if (core.Width <= 0)
            {
                continue;
            }

            var low = ThemedDrawing.Blend(accent, warm, 0.18 + (intensity * 0.22));
            var high = ThemedDrawing.Blend(warm, Colors.White, 0.18);
            var alpha = (byte)Math.Round(72 + (intensity * 150));
            var brush = new LinearGradientBrush(
                [
                    new GradientStop(ThemedDrawing.WithAlpha(low, (byte)Math.Max(42, alpha - 52)), 0),
                    new GradientStop(ThemedDrawing.WithAlpha(high, alpha), 0.55),
                    new GradientStop(ThemedDrawing.WithAlpha(low, (byte)Math.Max(42, alpha - 42)), 1)
                ],
                new Point(0, 1),
                new Point(1, 0));
            FreezeIfPossible(brush);

            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, "SignalBrush", (byte)Math.Round(22 + (intensity * 42)), Color.FromRgb(46, 196, 182)),
                null,
                new Rect(Math.Max(track.Left + 1, core.Left - 2), core.Top - 2, Math.Min(track.Right - core.Left, core.Width + 4), core.Height + 4),
                5,
                5);
            drawingContext.DrawRoundedRectangle(brush, null, core, 4, 4);
        }
    }

    private static void DrawTicks(
        DrawingContext drawingContext,
        Rect track,
        double labelTop,
        Color subtleColor,
        double pixelsPerDip)
    {
        var gridPen = FrozenPen(subtleColor, 52, 1);
        var tickPen = FrozenPen(subtleColor, 110, 1);

        foreach (var frequency in MajorFrequencies)
        {
            var x = FrequencyToX(track, frequency);
            drawingContext.DrawLine(gridPen, new Point(x, track.Top), new Point(x, track.Bottom));
            drawingContext.DrawLine(tickPen, new Point(x, track.Bottom), new Point(x, track.Bottom + 4));
            DrawLabel(drawingContext, FormatFrequency(frequency), x, labelTop, track.Right + 0.5, subtleColor, pixelsPerDip);
        }
    }

    private static void DrawLabel(
        DrawingContext drawingContext,
        string text,
        double centerX,
        double y,
        double width,
        Color subtleColor,
        double pixelsPerDip)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            FrozenBrush(ThemedDrawing.WithAlpha(subtleColor, 180)),
            pixelsPerDip);

        var x = Math.Clamp(centerX - (formatted.Width / 2), 0, Math.Max(0, width - formatted.Width));
        drawingContext.DrawText(formatted, new Point(x, y));
    }

    private void DrawHoverMarker(DrawingContext drawingContext, Rect track)
    {
        if (_hoverPoint is not { } point || !IsMouseOver)
        {
            return;
        }

        var x = Math.Clamp(point.X, track.Left, track.Right);
        var frequency = XToFrequency(track, x);
        var label = FormatFrequency(frequency);
        var accent = ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
        var markerPen = FrozenPen(accent, 220, 1.2);
        drawingContext.DrawLine(markerPen, new Point(x, track.Top - 8), new Point(x, track.Bottom + 5));

        var arrow = new StreamGeometry();
        using (var context = arrow.Open())
        {
            context.BeginFigure(new Point(x, track.Top - 1), true, true);
            context.LineTo(new Point(x - 4, track.Top - 7), true, false);
            context.LineTo(new Point(x + 4, track.Top - 7), true, false);
        }

        arrow.Freeze();
        drawingContext.DrawGeometry(FrozenBrush(ThemedDrawing.WithAlpha(accent, 230)), null, arrow);

        var formatted = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(244, 247, 250)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var paddingX = 6d;
        var bubbleWidth = formatted.Width + (paddingX * 2);
        var bubbleHeight = formatted.Height + 4;
        var bubbleX = Math.Clamp(x - (bubbleWidth / 2), 0, Math.Max(0, ActualWidth - bubbleWidth));
        var bubble = new Rect(bubbleX, 0, bubbleWidth, bubbleHeight);
        drawingContext.DrawRoundedRectangle(
            ThemedDrawing.GetBrush(this, "ControlPressedBrush", Color.FromRgb(53, 65, 77)),
            ThemedDrawing.GetPen(this, "AccentBrush", 1, Color.FromRgb(46, 196, 182)),
            bubble,
            4,
            4);
        drawingContext.DrawText(formatted, new Point(bubble.Left + paddingX, bubble.Top + 2));
    }

    private static double FrequencyToX(Rect track, double frequency)
    {
        var mel = SpectrumScale.HertzToMel(Math.Clamp(
            frequency,
            SpectrumScale.MinimumFrequencyHz,
            SpectrumScale.MaximumFrequencyHz));
        var ratio = (mel - MinimumMel) / (MaximumMel - MinimumMel);
        return track.Left + (track.Width * Math.Clamp(ratio, 0, 1));
    }

    private static double XToFrequency(Rect track, double x)
    {
        var ratio = Math.Clamp((x - track.Left) / track.Width, 0, 1);
        var mel = MinimumMel + ((MaximumMel - MinimumMel) * ratio);
        return SpectrumScale.MelToHertz(mel);
    }

    private static string FormatFrequency(double frequency)
    {
        return frequency >= 1000 ? $"{frequency / 1000:0.#}k" : $"{frequency:0}";
    }

    private static Rect GetTrackRect(double width)
    {
        return new Rect(0.5, 10.5, Math.Max(1, width - 1), 18);
    }

    private static bool AreClose(double first, double second)
    {
        return Math.Abs(first - second) < 0.5;
    }

    private static SolidColorBrush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(Color color, byte alpha, double thickness)
    {
        var pen = new Pen(FrozenBrush(ThemedDrawing.WithAlpha(color, alpha)), thickness);
        pen.Freeze();
        return pen;
    }

    private static void FreezeIfPossible(Freezable freezable)
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var nextPoint = e.GetPosition(this);
        if (_hoverPoint is { } previousPoint
            && Math.Abs(previousPoint.X - nextPoint.X) < 1
            && Math.Abs(previousPoint.Y - nextPoint.Y) < 1)
        {
            return;
        }

        _hoverPoint = nextPoint;
        InvalidateVisual();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_hoverPoint is null)
        {
            return;
        }

        _hoverPoint = null;
        InvalidateVisual();
    }
}
