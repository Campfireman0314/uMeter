using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostAggregateSnapshotPlannerTests
{
    [Fact]
    public void ResolvesSelectedSourceAndIncludesDetail()
    {
        var plan = NativeHostAggregateSnapshotPlanner.Project(
            ["plugin:lead", "plugin:keys"],
            "plugin:keys");

        Assert.True(plan.HasSelectedSource);
        Assert.Equal(1, plan.SelectedSourceIndex);
        Assert.True(plan.IncludeSelectedDetail);
        Assert.Equal("2 sources tracked", plan.StatusText);
    }

    [Fact]
    public void MissingSelectedSourceDisablesDetail()
    {
        var plan = NativeHostAggregateSnapshotPlanner.Project(
            ["plugin:lead"],
            "plugin:missing");

        Assert.False(plan.HasSelectedSource);
        Assert.Null(plan.SelectedSourceIndex);
        Assert.False(plan.IncludeSelectedDetail);
        Assert.Equal("1 source tracked", plan.StatusText);
    }

    [Fact]
    public void BlankSelectedSourceDisablesDetail()
    {
        var plan = NativeHostAggregateSnapshotPlanner.Project(
            ["plugin:lead"],
            " ");

        Assert.False(plan.HasSelectedSource);
        Assert.Null(plan.SelectedSourceIndex);
        Assert.False(plan.IncludeSelectedDetail);
    }

    [Theory]
    [InlineData(0, "No active audio sources")]
    [InlineData(1, "1 source tracked")]
    [InlineData(3, "3 sources tracked")]
    public void FormatsNormalSourceStatusText(int sourceCount, string expected)
    {
        var sourceKeys = Enumerable
            .Range(0, sourceCount)
            .Select(index => $"plugin:{index}")
            .ToArray();

        var plan = NativeHostAggregateSnapshotPlanner.Project(sourceKeys, selectedSourceKey: null);

        Assert.Equal(expected, plan.StatusText);
    }
}
