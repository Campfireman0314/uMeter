using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

internal static class ThemedDrawing
{
    public static Brush GetBrush(FrameworkElement element, PlotColorRole role)
    {
        var (key, fallback) = GetRoleDefinition(role);
        return GetBrush(element, key, fallback);
    }

    public static Brush GetBrush(FrameworkElement element, PlotColorRole role, byte alpha)
    {
        var color = GetColor(element, role);
        return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
    }

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

    public static Pen GetPen(FrameworkElement element, PlotColorRole role, double thickness)
    {
        return new Pen(GetBrush(element, role), thickness);
    }

    public static Pen GetPen(FrameworkElement element, PlotColorRole role, byte alpha, double thickness)
    {
        return new Pen(GetBrush(element, role, alpha), thickness);
    }

    public static Pen GetPen(FrameworkElement element, string key, byte alpha, double thickness, Color fallback)
    {
        return new Pen(GetBrush(element, key, alpha, fallback), thickness);
    }

    public static Color GetColor(FrameworkElement element, PlotColorRole role)
    {
        var (key, fallback) = GetRoleDefinition(role);
        return GetColor(element, key, fallback);
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
        var track = Color.FromRgb(17, 20, 25);
        var veil = Color.FromRgb(238, 241, 245);
        var low = Blend(track, accent, 0.28);
        var mid = Blend(veil, accent, 0.72);
        var high = Blend(warm, Color.FromRgb(255, 248, 230), 0.28);
        var color = value < 0.72
            ? Blend(low, mid, value / 0.72)
            : Blend(mid, high, (value - 0.72) / 0.28);
        var alpha = (byte)Math.Round(20 + (Math.Pow(value, 0.72) * 198));
        return WithAlpha(color, alpha);
    }

    private static (string Key, Color Fallback) GetRoleDefinition(PlotColorRole role)
    {
        return role switch
        {
            PlotColorRole.Track => ("MeterTrackBrush", Color.FromRgb(17, 20, 25)),
            PlotColorRole.Outline => ("ChromeBorderBrush", Color.FromRgb(58, 66, 77)),
            PlotColorRole.Subtle => ("SubtleTextBrush", Color.FromRgb(174, 183, 194)),
            PlotColorRole.Signal => ("SignalBrush", Color.FromRgb(46, 196, 182)),
            PlotColorRole.SpectrumWarm => ("SpectrumWarmBrush", Color.FromRgb(255, 176, 0)),
            PlotColorRole.Danger => ("DangerBrush", Color.FromRgb(255, 70, 92)),
            PlotColorRole.Text => ("TextBrush", Color.FromRgb(244, 247, 250)),
            PlotColorRole.ControlPressed => ("ControlPressedBrush", Color.FromRgb(53, 65, 77)),
            _ => ("SignalBrush", Color.FromRgb(46, 196, 182))
        };
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

internal enum PlotColorRole
{
    Track,
    Outline,
    Subtle,
    Signal,
    SpectrumWarm,
    Danger,
    Text,
    ControlPressed
}
