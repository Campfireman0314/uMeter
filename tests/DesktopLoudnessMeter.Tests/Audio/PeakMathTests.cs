using DesktopLoudnessMeter.App.Audio;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class PeakMathTests
{
    [Theory]
    [InlineData(0.0, "-inf dB")]
    [InlineData(-0.25, "-inf dB")]
    public void FormatDbTreatsSilenceAndInvalidValuesAsNegativeInfinity(double peak, string expected)
    {
        Assert.Equal(expected, PeakMath.FormatDb((float)peak));
    }

    [Theory]
    [InlineData(1.0, 0.0)]
    [InlineData(0.5, -6.0206)]
    [InlineData(0.25, -12.0412)]
    public void ToDecibelsConvertsNominalPeaks(double peak, double expectedDb)
    {
        Assert.Equal(expectedDb, PeakMath.ToDecibels((float)peak), precision: 3);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(2.0, 1.0)]
    public void ClampPeakKeepsMeterValuesWithinDisplayRange(double peak, double expected)
    {
        Assert.Equal((float)expected, PeakMath.ClampPeak((float)peak));
    }
}
