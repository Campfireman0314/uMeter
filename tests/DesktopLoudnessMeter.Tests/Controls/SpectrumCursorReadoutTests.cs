using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class SpectrumCursorReadoutTests
{
    [Fact]
    public void RetainedLevelInvertsTheAnalyzerNormalizedScale()
    {
        var readout = SpectrumCursorReadout.Create(
            frequencyHz: 1000,
            sourceBucket: 100,
            historyColumn: 3,
            hasRetainedLevel: true,
            storedLevel: 0.5);

        Assert.Equal(SpectrumCursorLevelState.Measured, readout.LevelState);
        Assert.Equal(-100.0, readout.LevelDbfs, precision: 9);
        Assert.Equal("-100.0 dBFS", readout.LevelText);
        Assert.Equal("1 kHz", readout.FrequencyText);
        Assert.Equal(100, readout.SourceBucket);
        Assert.Equal(3, readout.HistoryColumn);
    }

    [Fact]
    public void MeasuredLevelFormatsOneDecimal()
    {
        var readout = SpectrumCursorReadout.Create(1000, 100, 0, true, 0.123456);

        Assert.Equal(-175.3088, readout.LevelDbfs, precision: 9);
        Assert.Equal("-175.3 dBFS", readout.LevelText);
    }

    [Fact]
    public void ZeroLevelReportsTheAnalyzerFloor()
    {
        var readout = SpectrumCursorReadout.Create(1000, 100, 0, true, 0);

        Assert.Equal(SpectrumCursorLevelState.AtFloor, readout.LevelState);
        Assert.Equal("≤ -200 dBFS", readout.LevelText);
    }

    [Fact]
    public void FullScaleLevelReportsTheAnalyzerCeiling()
    {
        var readout = SpectrumCursorReadout.Create(1000, 100, 0, true, 1);

        Assert.Equal(SpectrumCursorLevelState.AtCeiling, readout.LevelState);
        Assert.Equal("≥ 0 dBFS", readout.LevelText);
    }

    [Theory]
    [InlineData(-0.4)]
    [InlineData(1.4)]
    public void OutOfRangeStoredLevelsStayClampedAsFloorOrCeiling(double storedLevel)
    {
        var readout = SpectrumCursorReadout.Create(1000, 100, 0, true, storedLevel);

        Assert.NotEqual(SpectrumCursorLevelState.Measured, readout.LevelState);
        Assert.DoesNotContain("E-", readout.LevelText, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            readout.LevelState == SpectrumCursorLevelState.AtFloor
            || readout.LevelState == SpectrumCursorLevelState.AtCeiling);
    }

    [Fact]
    public void MissingHistoryHasNoNumericLevel()
    {
        var readout = SpectrumCursorReadout.Create(
            frequencyHz: 440,
            sourceBucket: 100,
            historyColumn: -1,
            hasRetainedLevel: false,
            storedLevel: 0);

        Assert.Equal(SpectrumCursorLevelState.Unavailable, readout.LevelState);
        Assert.True(double.IsNaN(readout.LevelDbfs));
        Assert.Equal("—", readout.LevelText);
        Assert.Equal("440 Hz", readout.FrequencyText);
    }

    [Fact]
    public void NonFiniteStoredLevelHasNoNumericLevel()
    {
        var readout = SpectrumCursorReadout.Create(440, 100, 2, true, double.NaN);

        Assert.Equal(SpectrumCursorLevelState.Unavailable, readout.LevelState);
        Assert.Equal("—", readout.LevelText);
    }

    [Theory]
    [InlineData(20, "20 Hz")]
    [InlineData(999.6, "1000 Hz")]
    [InlineData(1000, "1 kHz")]
    [InlineData(1234.5, "1.23 kHz")]
    [InlineData(22000, "22 kHz")]
    public void FrequencyTextFollowsTheExistingHoverFormat(double frequencyHz, string expected)
    {
        var readout = SpectrumCursorReadout.Create(frequencyHz, 0, 0, true, 0.5);

        Assert.Equal(expected, readout.FrequencyText);
    }
}
