using System.Windows;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Controls;

internal static class SpectrumFrequencyHoverMapper
{
    public static double? Map(Point position, Rect plot, double viewportMinimumMel, double viewportMelSpan)
    {
        if (plot.Width <= 0
            || plot.Height <= 0
            || !double.IsFinite(viewportMinimumMel)
            || !double.IsFinite(viewportMelSpan)
            || viewportMelSpan <= 0
            || position.X < plot.Left
            || position.X > plot.Right
            || position.Y < plot.Top
            || position.Y > plot.Bottom)
        {
            return null;
        }

        var ratio = (plot.Bottom - position.Y) / plot.Height;
        var mel = viewportMinimumMel + (viewportMelSpan * ratio);
        return Math.Clamp(
            SpectrumScale.MelToHertz(mel),
            SpectrumScale.MinimumFrequencyHz,
            SpectrumScale.MaximumFrequencyHz);
    }
}
