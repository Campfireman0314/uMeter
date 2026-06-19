using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

internal static class ThemedDrawing
{
    public static Brush GetBrush(FrameworkElement element, string key, Color fallback)
    {
        return element.TryFindResource(key) as Brush
            ?? new SolidColorBrush(fallback);
    }

    public static Brush GetBrush(FrameworkElement element, string key, byte alpha, Color fallback)
    {
        var color = GetColor(element, key, fallback);
        return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
    }

    public static Pen GetPen(FrameworkElement element, string key, double thickness, Color fallback)
    {
        return new Pen(GetBrush(element, key, fallback), thickness);
    }

    public static Pen GetPen(FrameworkElement element, string key, byte alpha, double thickness, Color fallback)
    {
        return new Pen(GetBrush(element, key, alpha, fallback), thickness);
    }

    public static Color GetColor(FrameworkElement element, string key, Color fallback)
    {
        return element.TryFindResource(key) switch
        {
            SolidColorBrush brush => brush.Color,
            Color color => color,
            _ => fallback
        };
    }

    public static Color WithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static LinearGradientBrush SpectrumBrush(Color accent, Color warm, double value)
    {
        var cool = Blend(Color.FromRgb(14, 20, 26), accent, 0.42);
        var mid = Blend(accent, warm, 0.30);
        var hot = Blend(warm, Color.FromRgb(255, 248, 222), 0.36);
        return new LinearGradientBrush(
            [
                new GradientStop(WithAlpha(cool, 42), 0),
                new GradientStop(WithAlpha(mid, 118), Math.Clamp(value * 0.72, 0.18, 0.82)),
                new GradientStop(WithAlpha(hot, 230), 1)
            ],
            new Point(0, 1),
            new Point(0, 0));
    }

    public static Color SpectrumPixel(Color accent, Color warm, double value)
    {
        value = Math.Clamp(value, 0, 1);
        var veil = Color.FromRgb(238, 241, 245);
        var low = Blend(veil, accent, 0.34);
        var mid = Blend(veil, accent, 0.72);
        var high = Blend(warm, Color.FromRgb(255, 248, 230), 0.28);
        var color = value < 0.72
            ? Blend(low, mid, value / 0.72)
            : Blend(mid, high, (value - 0.72) / 0.28);
        var alpha = (byte)Math.Round(64 + (value * 154));
        return WithAlpha(color, alpha);
    }

    public static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + ((to.R - from.R) * amount)),
            (byte)Math.Round(from.G + ((to.G - from.G) * amount)),
            (byte)Math.Round(from.B + ((to.B - from.B) * amount)));
    }
}
