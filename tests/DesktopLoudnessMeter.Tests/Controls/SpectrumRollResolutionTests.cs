using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumRollResolutionTests
{
    [Fact]
    public void ResolutionGrowsWithViewportWithinBounds()
    {
        var compact = SpectrumRollResolution.ForPlotSize(160, 80, sourceBucketCount: 320);
        var roomy = SpectrumRollResolution.ForPlotSize(900, 360, sourceBucketCount: 320);
        var huge = SpectrumRollResolution.ForPlotSize(5000, 5000, sourceBucketCount: 320);

        Assert.True(compact.HistoryColumns < roomy.HistoryColumns);
        Assert.True(compact.FrequencyRows < roomy.FrequencyRows);
        Assert.Equal(SpectrumRollResolution.MaximumHistoryColumns, huge.HistoryColumns);
        Assert.Equal(320, huge.FrequencyRows);
    }

    [Fact]
    public void InvalidViewportFallsBackToMinimumResolution()
    {
        var resolution = SpectrumRollResolution.ForPlotSize(double.NaN, -10, sourceBucketCount: 320);

        Assert.Equal(SpectrumRollResolution.MinimumHistoryColumns, resolution.HistoryColumns);
        Assert.Equal(SpectrumRollResolution.MinimumFrequencyRows, resolution.FrequencyRows);
    }
}
