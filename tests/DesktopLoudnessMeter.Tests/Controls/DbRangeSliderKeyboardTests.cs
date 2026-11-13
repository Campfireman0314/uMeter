using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class DbRangeSliderKeyboardTests
{
    [Fact]
    public void ArrowKeysMoveTheActiveLowerEndpointOnly()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();

            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);

            StaControlHost.RaiseKeyDown(slider, Key.Right);
            Assert.Equal(-59, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);

            StaControlHost.RaiseKeyDown(slider, Key.Up);
            Assert.Equal(-58, slider.LowerValue);

            StaControlHost.RaiseKeyDown(slider, Key.Left);
            StaControlHost.RaiseKeyDown(slider, Key.Down);
            Assert.Equal(-60, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);
        });
    }

    [Fact]
    public void SpaceSwitchesTheActiveEndpointToTheUpperValue()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();

            StaControlHost.RaiseKeyDown(slider, Key.Space);

            Assert.Equal(RangeSliderEndpoint.Upper, slider.ActiveEndpoint);

            StaControlHost.RaiseKeyDown(slider, Key.Up);
            Assert.Equal(-9, slider.UpperValue);
            Assert.Equal(-60, slider.LowerValue);

            StaControlHost.RaiseKeyDown(slider, Key.Space);
            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);
        });
    }

    [Fact]
    public void HomeAndEndStayInsideTheEndpointSeparation()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();

            StaControlHost.RaiseKeyDown(slider, Key.Home);
            Assert.Equal(-120, slider.LowerValue);

            StaControlHost.RaiseKeyDown(slider, Key.End);
            Assert.Equal(-11, slider.LowerValue);
            Assert.True(slider.LowerValue < slider.UpperValue);

            StaControlHost.RaiseKeyDown(slider, Key.Space);
            StaControlHost.RaiseKeyDown(slider, Key.Home);
            Assert.Equal(-10, slider.UpperValue);
            Assert.True(slider.LowerValue < slider.UpperValue);

            StaControlHost.RaiseKeyDown(slider, Key.End);
            Assert.Equal(0, slider.UpperValue);
            Assert.Equal(-11, slider.LowerValue);
        });
    }

    [Fact]
    public void EnterStartsNumericEntryForTheActiveEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.Space);
            StaControlHost.RaiseKeyDown(slider, Key.Enter);

            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal("-10", editor.Text);

            editor.Text = "-12";
            StaControlHost.RaiseKeyDown(editor, Key.Enter);

            Assert.Equal(-12, slider.UpperValue);
            Assert.Equal(-60, slider.LowerValue);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);
        });
    }

    [Fact]
    public void F2StartsNumericEntryForTheActiveEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);

            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);

            editor.Text = "-78";
            StaControlHost.RaiseKeyDown(editor, Key.Enter);

            Assert.Equal(-78, slider.LowerValue);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);
        });
    }

    [Fact]
    public void NumericEntryKeepsTheEndpointsSeparated()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            editor.Text = "-5";
            StaControlHost.RaiseKeyDown(editor, Key.Enter);

            Assert.Equal(-11, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);
        });
    }

    [Fact]
    public void EscapeCancelsNumericEntryWithoutChangingTheValue()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            editor.Text = "-100";
            StaControlHost.RaiseKeyDown(editor, Key.Escape);

            Assert.Equal(-60, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);
        });
    }

    [Fact]
    public void ArrowKeysAreHandledButTabIsLeftForFocusNavigation()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();

            Assert.True(StaControlHost.RaiseKeyDown(slider, Key.Right).Handled);
            Assert.True(StaControlHost.RaiseKeyDown(slider, Key.Home).Handled);
            Assert.False(StaControlHost.RaiseKeyDown(slider, Key.Tab).Handled);
            Assert.False(StaControlHost.RaiseKeyDown(slider, Key.LeftShift).Handled);
        });
    }

    [Fact]
    public void KeyClicksArrivingWhileTypingDoNotMoveTheSlider()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            Assert.Equal(Visibility.Visible, editor.Visibility);

            StaControlHost.RaiseKeyDown(slider, Key.Right);
            StaControlHost.RaiseKeyDown(slider, Key.Left);
            StaControlHost.RaiseKeyDown(slider, Key.Home);
            StaControlHost.RaiseKeyDown(slider, Key.End);
            StaControlHost.RaiseKeyDown(slider, Key.Space);

            Assert.Equal(-60, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);
            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);
        });
    }

    [Fact]
    public void DisabledRangeSliderIgnoresKeyboardInput()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            slider.IsEnabled = false;

            StaControlHost.RaiseKeyDown(slider, Key.Right);
            StaControlHost.RaiseKeyDown(slider, Key.End);
            StaControlHost.RaiseKeyDown(slider, Key.Space);
            StaControlHost.RaiseKeyDown(slider, Key.F2);

            Assert.Equal(-60, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);
            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);
            Assert.Equal(
                Visibility.Collapsed,
                StaControlHost.FindEditor(slider).Visibility);
        });
    }

    private static DbRangeSlider CreateRangeSlider()
    {
        return new DbRangeSlider
        {
            Minimum = -120,
            Maximum = 0,
            Step = 1,
            LowerValue = -60,
            UpperValue = -10
        };
    }
}
