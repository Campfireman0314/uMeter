using System.Windows;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumFrequencyHoverMapperTests
{
    [Fact]
    public void HoverYMapsToViewportFrequency()
    {
        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
        var frequency = SpectrumFrequencyHoverMapper.Map(
            new Point(60, 50),
            new Rect(40, 0, 200, 100),
            minimumMel,
            maximumMel - minimumMel);

        Assert.NotNull(frequency);
        Assert.InRange(frequency.Value, 3200, 3500);
    }

    [Fact]
    public void HoverOutsidePlotIsIgnored()
    {
        var frequency = SpectrumFrequencyHoverMapper.Map(
            new Point(10, 50),
            new Rect(40, 0, 200, 100),
            0,
            1000);

        Assert.Null(frequency);
    }
}
