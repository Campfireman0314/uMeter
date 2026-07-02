using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSourceTargetCommandPlannerTests
{
    [Fact]
    public void PlanReturnsTargetForKnownSource()
    {
        var plan = NativeHostSourceTargetCommandPlanner.Plan(
            ["plugin:lead", "plugin:keys"],
            "plugin:lead");

        Assert.True(plan.Success);
        Assert.Equal("plugin:lead", plan.SourceKey);
    }

    [Fact]
    public void PlanMissingReturnsUnsuccessfulPlan()
    {
        var plan = NativeHostSourceTargetCommandPlanner.Plan(
            ["plugin:lead"],
            "plugin:missing");

        Assert.False(plan.Success);
        Assert.Null(plan.SourceKey);
    }

    [Fact]
    public void PlanBlankReturnsUnsuccessfulPlan()
    {
        var plan = NativeHostSourceTargetCommandPlanner.Plan(
            ["plugin:lead"],
            string.Empty);

        Assert.False(plan.Success);
        Assert.Null(plan.SourceKey);
    }
}
