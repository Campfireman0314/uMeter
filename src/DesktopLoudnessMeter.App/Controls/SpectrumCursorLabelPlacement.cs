using System.Windows;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// Places the hover label next to the cursor while keeping it inside the plot,
/// including narrow inline/popup/channel plots where the label is wider than the
/// plot. No separate tooltip window or overlay element is involved.
/// </summary>
internal readonly record struct SpectrumCursorLabelPlacement(Rect Label, double TextOriginX, double TextOriginY)
{
    public const double CursorGap = 10;
    public const double EdgeInset = 4;
    public const double VerticalInset = 3;

    public static SpectrumCursorLabelPlacement Place(
        Point hover,
        Rect plot,
        Size textSize,
        Thickness padding)
    {
        var availableWidth = Math.Max(0, plot.Width - (EdgeInset * 2));
        var availableHeight = Math.Max(0, plot.Height - (VerticalInset * 2));
        var width = Math.Min(Math.Max(0, textSize.Width) + padding.Left + padding.Right, availableWidth);
        var height = Math.Min(Math.Max(0, textSize.Height) + padding.Top + padding.Bottom, availableHeight);
        if (width <= 0 || height <= 0)
        {
            return new SpectrumCursorLabelPlacement(new Rect(plot.Left, plot.Top, 0, 0), plot.Left, plot.Top);
        }

        var left = hover.X + CursorGap;
        if (left + width > plot.Right - VerticalInset)
        {
            left = hover.X - width - CursorGap;
        }

        var maximumLeft = Math.Max(plot.Left + EdgeInset, plot.Right - EdgeInset - width);
        left = Math.Clamp(left, plot.Left + EdgeInset, maximumLeft);
        var top = Math.Clamp(
            hover.Y - (height / 2),
            plot.Top + VerticalInset,
            Math.Max(plot.Top + VerticalInset, plot.Bottom - VerticalInset - height));

        var label = new Rect(left, top, width, height);
        return new SpectrumCursorLabelPlacement(
            label,
            label.Left + padding.Left,
            label.Top + padding.Top);
    }
}
