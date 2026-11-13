using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class EditableValueSliderAccessibilityTests
{
    [Fact]
    public void ArrowKeysAdjustTheValueInSteps()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();

            StaControlHost.RaiseKeyDown(slider, Key.Right);
            Assert.Equal(41, slider.Value);

            StaControlHost.RaiseKeyDown(slider, Key.Up);
            Assert.Equal(42, slider.Value);

            StaControlHost.RaiseKeyDown(slider, Key.Left);
            StaControlHost.RaiseKeyDown(slider, Key.Down);
            Assert.Equal(40, slider.Value);
        });
    }

    [Fact]
    public void HomeAndEndStayInsideTheBounds()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();

            StaControlHost.RaiseKeyDown(slider, Key.Home);
            Assert.Equal(0, slider.Value);

            StaControlHost.RaiseKeyDown(slider, Key.End);
            Assert.Equal(100, slider.Value);
        });
    }

    [Fact]
    public void EnterAndF2BothStartNumericEntry()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.Enter);
            Assert.Equal(Visibility.Visible, editor.Visibility);
            StaControlHost.RaiseKeyDown(editor, Key.Escape);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            Assert.Equal(Visibility.Visible, editor.Visibility);
            Assert.Equal("40", editor.Text);
        });
    }

    [Fact]
    public void EscapeCancelsNumericEntryWithoutChangingTheValue()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            editor.Text = "88";
            StaControlHost.RaiseKeyDown(editor, Key.Escape);

            Assert.Equal(40, slider.Value);
            Assert.Equal(Visibility.Collapsed, editor.Visibility);
        });
    }

    [Fact]
    public void KeysArrivingWhileTypingDoNotMoveTheSlider()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var editor = StaControlHost.FindEditor(slider);

            StaControlHost.RaiseKeyDown(slider, Key.F2);
            StaControlHost.RaiseKeyDown(slider, Key.Right);
            StaControlHost.RaiseKeyDown(slider, Key.Home);
            StaControlHost.RaiseKeyDown(slider, Key.End);
            StaControlHost.RaiseKeyDown(slider, Key.Left);

            Assert.Equal(40, slider.Value);
        });
    }

    [Fact]
    public void ArrowKeysAreHandledButTabIsLeftForFocusNavigation()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();

            Assert.True(StaControlHost.RaiseKeyDown(slider, Key.Right).Handled);
            Assert.True(StaControlHost.RaiseKeyDown(slider, Key.End).Handled);
            Assert.False(StaControlHost.RaiseKeyDown(slider, Key.Tab).Handled);
        });
    }

    [Fact]
    public void DisabledSliderIgnoresKeyboardInput()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            slider.IsEnabled = false;

            StaControlHost.RaiseKeyDown(slider, Key.Right);
            StaControlHost.RaiseKeyDown(slider, Key.End);
            StaControlHost.RaiseKeyDown(slider, Key.F2);

            Assert.Equal(40, slider.Value);
            Assert.Equal(Visibility.Collapsed, StaControlHost.FindEditor(slider).Visibility);
        });
    }

    [Fact]
    public void AutomationPeerExposesTheRangeValuePattern()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            AutomationProperties.SetName(slider, "Level floor");

            var peer = EnsurePeer(slider);
            Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
            Assert.Equal("Level floor", peer.GetName());

            var provider = Assert.IsAssignableFrom<IRangeValueProvider>(
                peer.GetPattern(PatternInterface.RangeValue));
            Assert.Equal(0, provider.Minimum);
            Assert.Equal(100, provider.Maximum);
            Assert.Equal(40, provider.Value);
            Assert.Equal(1, provider.SmallChange);
            Assert.False(provider.IsReadOnly);
            Assert.True(peer.IsEnabled());
        });
    }

    [Fact]
    public void AutomationPeerRejectsOutOfRangeAndDisabledChanges()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var peer = EnsurePeer(slider);
            var provider = Assert.IsAssignableFrom<IRangeValueProvider>(
                peer.GetPattern(PatternInterface.RangeValue));

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetValue(140));

            slider.IsEnabled = false;

            Assert.False(peer.IsEnabled());
            Assert.Throws<ElementNotEnabledException>(() => provider.SetValue(50));
            Assert.Equal(40, slider.Value);
        });
    }

    [Fact]
    public void AutomationPeerChangesKeepTwoWayBindingsAlive()
    {
        StaControlHost.Run(() =>
        {
            var source = new EditableSliderBindingSource { Threshold = 40 };
            var slider = new EditableValueSlider { Minimum = 0, Maximum = 100, Step = 1 };
            BindingOperations.SetBinding(
                slider,
                EditableValueSlider.ValueProperty,
                new Binding(nameof(EditableSliderBindingSource.Threshold))
                {
                    Source = source,
                    Mode = BindingMode.TwoWay
                });

            var provider = Assert.IsAssignableFrom<IRangeValueProvider>(
                EnsurePeer(slider).GetPattern(PatternInterface.RangeValue));
            provider.SetValue(64);

            Assert.Equal(64, source.Threshold);
            Assert.Equal(64, provider.Value);
        });
    }

    [Fact]
    public void AutomationPeerPublishesValueChangesForRealUpdates()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var peer = Assert.IsType<EditableValueSliderAutomationPeer>(EnsurePeer(slider));
            var provider = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;

            provider.SetValue(64);
            Assert.True(peer.RaisedValueChangeCount >= 1);
            Assert.Equal(64, provider.Value);

            var before = peer.RaisedValueChangeCount;
            StaControlHost.RaiseKeyDown(slider, Key.Right);

            Assert.True(peer.RaisedValueChangeCount > before);
            Assert.Equal(65, provider.Value);
        });
    }

    [Theory]
    [InlineData(0.4, 100.6)]
    [InlineData(0.6, 100.4)]
    public void FractionalBoundsRemainReachableByAutomationAndKeyboard(double minimum, double maximum)
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateSlider();
            var provider = (IRangeValueProvider)EnsurePeer(slider).GetPattern(PatternInterface.RangeValue)!;
            slider.Minimum = minimum;
            slider.Maximum = maximum;

            provider.SetValue(maximum);
            Assert.Equal(maximum, provider.Value);
            Assert.InRange(provider.Value, provider.Minimum, provider.Maximum);
            provider.SetValue(provider.Value);
            Assert.Equal(maximum, provider.Value);

            StaControlHost.RaiseKeyDown(slider, Key.Home);
            Assert.Equal(minimum, provider.Value);
            StaControlHost.RaiseKeyDown(slider, Key.End);
            Assert.Equal(maximum, provider.Value);
        });
    }

    private static AutomationPeer EnsurePeer(EditableValueSlider slider)
    {
        StaControlHost.Arrange(slider, height: 48);
        return UIElementAutomationPeer.CreatePeerForElement(slider)
            ?? new EditableValueSliderAutomationPeer(slider);
    }

    private static EditableValueSlider CreateSlider()
    {
        return new EditableValueSlider
        {
            Minimum = 0,
            Maximum = 100,
            Step = 1,
            Value = 40,
            DisplayFormat = "0"
        };
    }

    public sealed class EditableSliderBindingSource
    {
        public double Threshold { get; set; }
    }
}
