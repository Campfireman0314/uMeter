using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.Tests.Controls;

public sealed class LoudnessClockPointMapperTests
{
    [Fact]
    public void BelowMinimumLoudnessIsNotMapped()
    {
        var point = LoudnessClockPointMapper.Map(
            loudnessDb: -61,
            minimumDb: -60,
            maximumDb: 0,
            innerRadius: 20,
            outerRadius: 100,
            elapsedSeconds: 0,
            cycleDurationSeconds: 30);

        Assert.Null(point);
    }

    [Fact]
    public void MinimumMaximumAndIntermediateLoudnessMapToExpectedRadii()
    {
        var minimum = LoudnessClockPointMapper.Map(-60, -60, 0, 20, 100, 0, 30);
        var middle = LoudnessClockPointMapper.Map(-30, -60, 0, 20, 100, 0, 30);
        var maximum = LoudnessClockPointMapper.Map(3, -60, 0, 20, 100, 0, 30);

        Assert.NotNull(minimum);
        Assert.NotNull(middle);
        Assert.NotNull(maximum);
        Assert.Equal(20, minimum.Value.Radius, precision: 6);
        Assert.Equal(60, middle.Value.Radius, precision: 6);
        Assert.Equal(100, maximum.Value.Radius, precision: 6);
    }

    [Fact]
    public void AnglesAdvanceClockwiseByDefault()
    {
        var top = LoudnessClockPointMapper.Map(-30, -60, 0, 20, 100, 0, 4);
        var right = LoudnessClockPointMapper.Map(-30, -60, 0, 20, 100, 1, 4);

        Assert.NotNull(top);
        Assert.NotNull(right);
        Assert.Equal(-Math.PI / 2, top.Value.AngleRadians, precision: 6);
        Assert.Equal(0, right.Value.AngleRadians, precision: 6);
    }

    [Fact]
    public void AnglesUseStableElapsedCyclePhaseAfterWrap()
    {
        var top = LoudnessClockPointMapper.Map(
            -30,
            -60,
            0,
            20,
            100,
            elapsedSeconds: 0,
            cycleDurationSeconds: 30);
        var wrappedTop = LoudnessClockPointMapper.Map(
            -30,
            -60,
            0,
            20,
            100,
            elapsedSeconds: 30,
            cycleDurationSeconds: 30);
        var wrappedRight = LoudnessClockPointMapper.Map(
            -30,
            -60,
            0,
            20,
            100,
            elapsedSeconds: 37.5,
            cycleDurationSeconds: 30);

        Assert.NotNull(top);
        Assert.NotNull(wrappedTop);
        Assert.NotNull(wrappedRight);
        Assert.Equal(top.Value.AngleRadians, wrappedTop.Value.AngleRadians, precision: 6);
        Assert.Equal(0, wrappedRight.Value.AngleRadians, precision: 6);
    }

    [Fact]
    public void SamplesAtOrAboveWarningLevelAreClassified()
    {
        var normal = LoudnessClockPointMapper.Map(-18, -60, 0, 20, 100, 0, 30, warningDb: -12);
        var warning = LoudnessClockPointMapper.Map(-12, -60, 0, 20, 100, 0, 30, warningDb: -12);

        Assert.NotNull(normal);
        Assert.NotNull(warning);
        Assert.False(normal.Value.IsWarning);
        Assert.True(warning.Value.IsWarning);
    }

    [Fact]
    public void SamplesAboveMaximumAreClampedAndClassifiedAsOverMaximum()
    {
        var point = LoudnessClockPointMapper.Map(-1, -32, -4, 20, 100, 0, 30, warningDb: -12);

        Assert.NotNull(point);
        Assert.Equal(100, point.Value.Radius, precision: 6);
        Assert.True(point.Value.IsWarning);
        Assert.True(point.Value.IsOverMaximum);
    }
}
