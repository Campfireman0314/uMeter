using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSelectedSourceCommandPlannerTests
{
    [Fact]
    public void PlanSelectReturnsSelectedSourceForKnownSource()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanSelect(
            ["plugin:lead", "plugin:keys"],
            "plugin:lead");

        Assert.True(plan.Success);
        Assert.True(plan.ShouldSetSelected);
        Assert.False(plan.ShouldClearSelected);
        Assert.Equal("plugin:lead", plan.SelectedSourceKey);
    }

    [Fact]
    public void PlanSelectMissingReturnsUnsuccessfulPlan()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanSelect(
            ["plugin:lead"],
            "plugin:missing");

        Assert.False(plan.Success);
        Assert.False(plan.ShouldSetSelected);
        Assert.False(plan.ShouldClearSelected);
        Assert.Null(plan.SelectedSourceKey);
    }

    [Fact]
    public void PlanBackToGlobalClearsSelectedSource()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanBackToGlobal();

        Assert.True(plan.Success);
        Assert.True(plan.ShouldClearSelected);
        Assert.False(plan.ShouldSetSelected);
    }

    [Fact]
    public void PlanReconcileKeepsVisibleSelectedSource()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanReconcile(
            ["plugin:lead", "plugin:keys"],
            "plugin:lead");

        Assert.True(plan.Success);
        Assert.False(plan.ShouldClearSelected);
        Assert.False(plan.ShouldSetSelected);
    }

    [Fact]
    public void PlanReconcileClearsMissingSelectedSource()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanReconcile(
            ["plugin:keys"],
            "plugin:lead");

        Assert.True(plan.Success);
        Assert.True(plan.ShouldClearSelected);
        Assert.False(plan.ShouldSetSelected);
    }

    [Fact]
    public void PlanHiddenSourceClearsSelectedWhenHiddenRowsAreNotShown()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanHiddenSource(
            "plugin:lead",
            isHidden: true,
            showHiddenSources: false,
            selectedSourceKey: "plugin:lead");

        Assert.True(plan.Success);
        Assert.True(plan.ShouldClearSelected);
    }

    [Fact]
    public void PlanHiddenSourceKeepsSelectedWhenHiddenRowsAreShown()
    {
        var plan = NativeHostSelectedSourceCommandPlanner.PlanHiddenSource(
            "plugin:lead",
            isHidden: true,
            showHiddenSources: true,
            selectedSourceKey: "plugin:lead");

        Assert.True(plan.Success);
        Assert.False(plan.ShouldClearSelected);
    }
}
