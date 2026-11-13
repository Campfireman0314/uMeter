using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class DbRangeSliderAutomationTests
{
    [Fact]
    public void ExposesOneRangeValueProviderPerEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var peer = EnsurePeer(slider);

            var children = peer.GetChildren();

            Assert.NotNull(children);
            Assert.Equal(2, children!.Count);
            Assert.Contains("Lower", children[0].GetName(), StringComparison.Ordinal);
            Assert.Contains("Upper", children[1].GetName(), StringComparison.Ordinal);
            Assert.NotEqual(children[0].GetName(), children[1].GetName());
        });
    }

    [Fact]
    public void EndpointProvidersReportTheirOwnAllowedBounds()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            Assert.Equal(-120, lower.Minimum);
            Assert.Equal(-11, lower.Maximum);
            Assert.Equal(-60, lower.Value);
            Assert.False(lower.IsReadOnly);

            Assert.Equal(-59, upper.Minimum);
            Assert.Equal(0, upper.Maximum);
            Assert.Equal(-10, upper.Value);
            Assert.False(upper.IsReadOnly);

            Assert.Equal(slider.Step, lower.SmallChange);
            Assert.Equal(slider.Step, upper.SmallChange);
        });
    }

    [Fact]
    public void EndpointProvidersMoveOnlyTheirOwnEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            lower.SetValue(-42);
            Assert.Equal(-42, slider.LowerValue);
            Assert.Equal(-10, slider.UpperValue);

            upper.SetValue(-8);
            Assert.Equal(-8, slider.UpperValue);
            Assert.Equal(-42, slider.LowerValue);
        });
    }

    [Fact]
    public void EndpointProvidersRejectValuesOutsideTheirBounds()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            Assert.Throws<ArgumentOutOfRangeException>(() => lower.SetValue(-5));
            Assert.Throws<ArgumentOutOfRangeException>(() => upper.SetValue(-200));
        });
    }

    [Fact]
    public void DisabledSliderReportsDisabledProvidersAndRejectsChanges()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var peer = EnsurePeer(slider);
            var children = peer.GetChildren()!;
            var lower = (IRangeValueProvider)children[0].GetPattern(PatternInterface.RangeValue)!;

            slider.IsEnabled = false;

            Assert.False(children[0].IsEnabled());
            Assert.False(children[1].IsEnabled());
            Assert.Throws<ElementNotEnabledException>(() => lower.SetValue(-40));
            Assert.Equal(-60, slider.LowerValue);
        });
    }

    [Fact]
    public void ProviderChangesKeepTwoWayBindingsAlive()
    {
        StaControlHost.Run(() =>
        {
            var source = new RangeSliderBindingSource { LowerLimit = -60, UpperLimit = -10 };
            var slider = new DbRangeSlider
            {
                Minimum = -120,
                Maximum = 0,
                Step = 1
            };
            BindingOperations.SetBinding(
                slider,
                DbRangeSlider.LowerValueProperty,
                new Binding(nameof(RangeSliderBindingSource.LowerLimit))
                {
                    Source = source,
                    Mode = BindingMode.TwoWay
                });
            BindingOperations.SetBinding(
                slider,
                DbRangeSlider.UpperValueProperty,
                new Binding(nameof(RangeSliderBindingSource.UpperLimit))
                {
                    Source = source,
                    Mode = BindingMode.TwoWay
                });

            var (lower, upper) = RangeValueProviders(slider);
            lower.SetValue(-70);
            upper.SetValue(-20);

            Assert.Equal(-70, source.LowerLimit);
            Assert.Equal(-20, source.UpperLimit);
        });
    }

    [Fact]
    public void ProvidersPublishValueChangesForRealUpdates()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var peer = EnsurePeer(slider);
            var children = peer.GetChildren()!;
            var lowerPeer = Assert.IsType<RangeSliderEndpointAutomationPeer>(children[0]);
            var lower = (IRangeValueProvider)children[0].GetPattern(PatternInterface.RangeValue)!;

            lower.SetValue(-42);

            Assert.True(lowerPeer.RaisedValueChangeCount >= 1);
            Assert.Equal(-42, lower.Value);

            var before = lowerPeer.RaisedValueChangeCount;
            StaControlHost.RaiseKeyDown(slider, Key.Right);

            Assert.True(lowerPeer.RaisedValueChangeCount > before);
            Assert.Equal(-41, lower.Value);
        });
    }

    [Fact]
    public void SwitchingTheActiveEndpointAnnouncesTheActiveEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var peer = EnsurePeer(slider);
            var children = peer.GetChildren()!;
            var lowerPeer = Assert.IsType<RangeSliderEndpointAutomationPeer>(children[0]);
            var upperPeer = Assert.IsType<RangeSliderEndpointAutomationPeer>(children[1]);

            StaControlHost.RaiseKeyDown(slider, Key.Space);
            Assert.True(upperPeer.RaisedFocusNotificationCount >= 1);
            Assert.Equal(0, lowerPeer.RaisedFocusNotificationCount);

            StaControlHost.RaiseKeyDown(slider, Key.Space);
            Assert.True(lowerPeer.RaisedFocusNotificationCount >= 1);
        });
    }

    [Fact]
    public void SliderPeerCarriesTheConfiguredAccessibleNameAndGroupRole()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            System.Windows.Automation.AutomationProperties.SetName(slider, "Range");
            var peer = EnsurePeer(slider);

            Assert.Equal("Range", peer.GetName());
            Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
            Assert.Equal(AutomationControlType.Slider, peer.GetChildren()![0].GetAutomationControlType());
        });
    }

    [Fact]
    public void SliderPeerNameFollowsTheDisplayedLanguage()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            System.Windows.Automation.AutomationProperties.SetName(slider, "Range");
            DesktopLoudnessMeter.App.Localization.LocalizationManager.LocalizeTree(
                slider,
                DesktopLoudnessMeter.App.Localization.LocalizationManager.ChineseLanguageCode);

            Assert.Equal("范围", EnsurePeer(slider).GetName());
        });
    }

    [Fact]
    public void EndpointProvidersExposeOrderedBoundingRectangles()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            StaControlHost.Arrange(slider);
            using var host = StaControlHost.CreateFocusHost(slider);
            var children = EnsurePeer(slider).GetChildren()!;

            var lower = children[0].GetBoundingRectangle();
            var upper = children[1].GetBoundingRectangle();

            Assert.False(lower.IsEmpty);
            Assert.False(upper.IsEmpty);
            Assert.True(lower.Left < upper.Left);
            Assert.Equal(lower.Width, upper.Width, 1);
            Assert.Equal(28, lower.Width, 1);
        });
    }

    [Fact]
    public void EndpointPeersRedirectAutomationFocusToTheSliderAndSelectTheirEndpoint()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            StaControlHost.Arrange(slider);
            using var host = StaControlHost.CreateFocusHost(slider);
            var children = EnsurePeer(slider).GetChildren()!;
            var lowerPeer = children[0];
            var upperPeer = children[1];
            Assert.True(upperPeer.IsKeyboardFocusable());

            upperPeer.SetFocus();

            Assert.True(slider.IsKeyboardFocused);
            Assert.Equal(RangeSliderEndpoint.Upper, slider.ActiveEndpoint);
            Assert.True(upperPeer.HasKeyboardFocus());
            Assert.False(lowerPeer.HasKeyboardFocus());

            StaControlHost.RaiseKeyDown(slider, Key.Up);
            Assert.Equal(-9, slider.UpperValue);
            Assert.Equal(-60, slider.LowerValue);

            lowerPeer.SetFocus();

            Assert.True(slider.IsKeyboardFocused);
            Assert.Equal(RangeSliderEndpoint.Lower, slider.ActiveEndpoint);
            Assert.True(lowerPeer.HasKeyboardFocus());
            Assert.False(upperPeer.HasKeyboardFocus());

            StaControlHost.RaiseKeyDown(slider, Key.Up);
            Assert.Equal(-59, slider.LowerValue);
            Assert.Equal(-9, slider.UpperValue);
        });
    }

    [Fact]
    public void ShrinkingMaximumKeepsEndpointProvidersInsideTheirAdvertisedBounds()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            slider.Maximum = -80;

            Assert.Equal(-81, slider.LowerValue);
            Assert.Equal(-80, slider.UpperValue);
            AssertAdvertisedBoundsAgree(lower);
            AssertAdvertisedBoundsAgree(upper);
            Assert.Equal(-81, lower.Value);
            Assert.Equal(-81, lower.Maximum);
        });
    }

    [Fact]
    public void RaisingMinimumPullsTheUpperEndpointUpWhenThePairWouldCross()
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            slider.Minimum = -5;

            Assert.Equal(-5, slider.LowerValue);
            Assert.Equal(-4, slider.UpperValue);
            AssertAdvertisedBoundsAgree(lower);
            AssertAdvertisedBoundsAgree(upper);
        });
    }

    [Theory]
    [InlineData(-80.4)]
    [InlineData(-80.6)]
    public void FractionalMaximumKeepsEndpointsInsideTheirRanges(double maximum)
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            slider.Maximum = maximum;

            Assert.Equal(maximum, slider.UpperValue);
            Assert.True(slider.UpperValue - slider.LowerValue >= slider.Step);
            AssertAdvertisedBoundsAgree(lower);
            AssertAdvertisedBoundsAgree(upper);
            upper.SetValue(upper.Value);
            Assert.Equal(maximum, slider.UpperValue);
        });
    }

    [Theory]
    [InlineData(-5.4)]
    [InlineData(-5.6)]
    public void FractionalMinimumKeepsEndpointsInsideTheirRanges(double minimum)
    {
        StaControlHost.Run(() =>
        {
            var slider = CreateRangeSlider();
            var (lower, upper) = RangeValueProviders(slider);

            slider.Minimum = minimum;

            Assert.Equal(minimum, slider.LowerValue);
            Assert.True(slider.UpperValue - slider.LowerValue >= slider.Step);
            AssertAdvertisedBoundsAgree(lower);
            AssertAdvertisedBoundsAgree(upper);
            lower.SetValue(lower.Value);
            Assert.Equal(minimum, slider.LowerValue);
        });
    }

    private static void AssertAdvertisedBoundsAgree(IRangeValueProvider provider)
    {
        Assert.True(provider.Minimum <= provider.Value, $"value {provider.Value} below minimum {provider.Minimum}");
        Assert.True(provider.Value <= provider.Maximum, $"value {provider.Value} above maximum {provider.Maximum}");
        Assert.True(provider.Minimum <= provider.Maximum, $"inverted advertised range {provider.Minimum}..{provider.Maximum}");
    }

    private static AutomationPeer EnsurePeer(DbRangeSlider slider)
    {
        StaControlHost.Arrange(slider);
        return UIElementAutomationPeer.CreatePeerForElement(slider)
            ?? new DbRangeSliderAutomationPeer(slider);
    }

    private static (IRangeValueProvider Lower, IRangeValueProvider Upper) RangeValueProviders(DbRangeSlider slider)
    {
        var children = EnsurePeer(slider).GetChildren();
        Assert.NotNull(children);
        Assert.Equal(2, children!.Count);
        var lower = Assert.IsAssignableFrom<IRangeValueProvider>(children[0].GetPattern(PatternInterface.RangeValue));
        var upper = Assert.IsAssignableFrom<IRangeValueProvider>(children[1].GetPattern(PatternInterface.RangeValue));
        return (lower, upper);
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

    public sealed class RangeSliderBindingSource
    {
        public double LowerLimit { get; set; }

        public double UpperLimit { get; set; }
    }
}
