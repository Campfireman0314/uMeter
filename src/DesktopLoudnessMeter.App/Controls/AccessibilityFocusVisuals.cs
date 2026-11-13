using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// Draws the keyboard focus ring used by the custom-drawn sliders. Normal themes
/// use the accent brush from the active theme; system high contrast takes over
/// with the user's highlight colors so the ring stays visible.
/// </summary>
internal static class AccessibilityFocusVisuals
{
    internal const double RingThickness = 2;
    internal const double RingInset = 1;
    internal const double RingCornerRadius = 6;

    internal static Brush ResolveFocusBrush(FrameworkElement element)
    {
        return ResolveFocusBrush(element, SystemParameters.HighContrast);
    }

    internal static Brush ResolveFocusBrush(FrameworkElement element, bool highContrast)
    {
        if (highContrast)
        {
            return SystemColors.HighlightBrush;
        }

        return ThemedDrawing.GetBrush(element, "AccentBrush", Color.FromRgb(46, 196, 182));
    }

    internal static void DrawFocusRing(FrameworkElement element, DrawingContext drawingContext, Rect bounds)
    {
        DrawFocusRing(element, drawingContext, bounds, SystemParameters.HighContrast);
    }

    internal static void DrawFocusRing(
        FrameworkElement element,
        DrawingContext drawingContext,
        Rect bounds,
        bool highContrast)
    {
        if (bounds.Width <= RingInset * 2 || bounds.Height <= RingInset * 2)
        {
            return;
        }

        var ring = new Rect(
            bounds.Left + RingInset,
            bounds.Top + RingInset,
            bounds.Width - (RingInset * 2),
            bounds.Height - (RingInset * 2));
        var pen = new Pen(ResolveFocusBrush(element, highContrast), RingThickness);
        drawingContext.DrawRoundedRectangle(null, pen, ring, RingCornerRadius, RingCornerRadius);
    }
}
