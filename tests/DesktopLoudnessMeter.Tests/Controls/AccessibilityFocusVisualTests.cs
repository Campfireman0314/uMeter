using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class AccessibilityFocusVisualTests
{
    [Fact]
    public void FocusRingUsesTheThemedAccentBrush()
    {
        StaControlHost.Run(() =>
        {
            var slider = new DbRangeSlider();
            var themed = Color.FromRgb(12, 200, 120);
            slider.Resources["AccentBrush"] = new SolidColorBrush(themed);

            var brush = Assert.IsType<SolidColorBrush>(
                AccessibilityFocusVisuals.ResolveFocusBrush(slider, highContrast: false));

            Assert.Equal(themed, brush.Color);
        });
    }

    [Fact]
    public void FocusRingFallsBackToSystemHighlightColoursInHighContrast()
    {
        StaControlHost.Run(() =>
        {
            var slider = new DbRangeSlider();
            slider.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(12, 200, 120));

            var brush = AccessibilityFocusVisuals.ResolveFocusBrush(slider, highContrast: true);

            Assert.Equal(SystemColors.HighlightBrush.Color, ((SolidColorBrush)brush).Color);
        });
    }

    [Fact]
    public void HighContrastFocusRingIsDrawnWithSystemHighlightColour()
    {
        StaControlHost.Run(() =>
        {
            var slider = new DbRangeSlider { Width = 48, Height = 24 };
            StaControlHost.Arrange(slider, 48, 24);
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                AccessibilityFocusVisuals.DrawFocusRing(
                    slider,
                    context,
                    new Rect(0, 0, 48, 24),
                    highContrast: true);
            }

            var bitmap = new RenderTargetBitmap(48, 24, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var pixels = new byte[48 * 24 * 4];
            bitmap.CopyPixels(pixels, 48 * 4, 0);

            var index = ((12 * 48) + 1) * 4;
            var expected = SystemColors.HighlightBrush.Color;
            Assert.Equal(expected.B, pixels[index]);
            Assert.Equal(expected.G, pixels[index + 1]);
            Assert.Equal(expected.R, pixels[index + 2]);
            Assert.Equal(255, pixels[index + 3]);
        });
    }

    [Fact]
    public void RangeSliderDrawsKeyboardFocusRingOnlyWhileFocused()
    {
        StaControlHost.Run(() =>
        {
            var slider = new DbRangeSlider
            {
                Minimum = -120,
                Maximum = 0,
                Step = 1,
                LowerValue = -60,
                UpperValue = -10
            };
            AssertFocusRingAppearsOnFocus(slider);
        });
    }

    [Fact]
    public void ValueSliderDrawsKeyboardFocusRingOnlyWhileFocused()
    {
        StaControlHost.Run(() =>
        {
            var slider = new EditableValueSlider
            {
                Minimum = 0,
                Maximum = 100,
                Step = 1,
                Value = 40
            };
            AssertFocusRingAppearsOnFocus(slider);
        });
    }

    private static void AssertFocusRingAppearsOnFocus(FrameworkElement slider)
    {
        StaControlHost.Arrange(slider);
        using var host = StaControlHost.CreateFocusHost(slider);
        var width = (int)Math.Ceiling(slider.ActualWidth);
        var ringRow = (int)Math.Ceiling(slider.ActualHeight) - 3;

        var unfocused = StaControlHost.RenderPixels(slider);
        Assert.False(StaControlHost.IsOpaque(unfocused, width, 1, 20));
        Assert.False(StaControlHost.IsOpaque(unfocused, width, 1, ringRow));

        Assert.True(slider.Focus());
        Assert.True(slider.IsKeyboardFocusWithin);
        slider.UpdateLayout();

        var focused = StaControlHost.RenderPixels(slider);
        Assert.True(StaControlHost.IsOpaque(focused, width, 1, 20));
        Assert.True(StaControlHost.IsOpaque(focused, width, 1, ringRow));
    }
}
