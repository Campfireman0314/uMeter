using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSourceOrderCommandPlannerTests
{
    [Fact]
    public void PlanMoveReordersMiddleRowsAndClampsEdges()
    {
        var movedUp = NativeHostSourceOrderCommandPlanner.PlanMove(
            ["plugin:a", "plugin:b", "plugin:c"],
            "plugin:b",
            HostSourceMoveDirection.Up);
        var clamped = NativeHostSourceOrderCommandPlanner.PlanMove(
            ["plugin:a", "plugin:b", "plugin:c"],
            "plugin:a",
            HostSourceMoveDirection.Up);

        Assert.True(movedUp.Success);
        Assert.Equal(
            [("plugin:b", 0), ("plugin:a", 1), ("plugin:c", 2)],
            movedUp.Rows.Select(row => (row.SourceKey, row.OrderIndex)).ToArray());
        Assert.True(clamped.Success);
        Assert.Equal(
            [("plugin:a", 0), ("plugin:b", 1), ("plugin:c", 2)],
            clamped.Rows.Select(row => (row.SourceKey, row.OrderIndex)).ToArray());
    }

    [Fact]
    public void PlanMoveToIndexClampsTargetIndex()
    {
        var negative = NativeHostSourceOrderCommandPlanner.PlanMoveToIndex(
            ["plugin:a", "plugin:b", "plugin:c"],
            "plugin:c",
            -12);
        var oversized = NativeHostSourceOrderCommandPlanner.PlanMoveToIndex(
            ["plugin:a", "plugin:b", "plugin:c"],
            "plugin:a",
            99);

        Assert.True(negative.Success);
        Assert.Equal(
            [("plugin:c", 0), ("plugin:a", 1), ("plugin:b", 2)],
            negative.Rows.Select(row => (row.SourceKey, row.OrderIndex)).ToArray());
        Assert.True(oversized.Success);
        Assert.Equal(
            [("plugin:b", 0), ("plugin:c", 1), ("plugin:a", 2)],
            oversized.Rows.Select(row => (row.SourceKey, row.OrderIndex)).ToArray());
    }

    [Fact]
    public void MissingSourceReturnsUnsuccessfulPlan()
    {
        var plan = NativeHostSourceOrderCommandPlanner.PlanMoveToIndex(
            ["plugin:a", "plugin:b"],
            "plugin:missing",
            0);

        Assert.False(plan.Success);
        Assert.Empty(plan.Rows);
    }
}
