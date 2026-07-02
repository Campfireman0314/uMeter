using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSourcePreferenceCommandPlannerTests
{
    [Fact]
    public void PlanSetAliasTrimsAliasAndClearsBlankAlias()
    {
        var trimmed = NativeHostSourcePreferenceCommandPlanner.PlanSetAlias(
            ["plugin:lead"],
            "plugin:lead",
            "\u3000Lead Bus\u00A0");
        var cleared = NativeHostSourcePreferenceCommandPlanner.PlanSetAlias(
            ["plugin:lead"],
            "plugin:lead",
            "\u3000\u00A0");

        Assert.True(trimmed.Success);
        Assert.Equal("Lead Bus", trimmed.Alias);
        Assert.False(trimmed.ShouldForget);
        Assert.True(cleared.Success);
        Assert.Null(cleared.Alias);
    }

    [Fact]
    public void PlanSetHiddenReturnsTargetState()
    {
        var hidden = NativeHostSourcePreferenceCommandPlanner.PlanSetHidden(
            ["plugin:lead"],
            "plugin:lead",
            isHidden: true);
        var restored = NativeHostSourcePreferenceCommandPlanner.PlanSetHidden(
            ["plugin:lead"],
            "plugin:lead",
            isHidden: false);

        Assert.True(hidden.Success);
        Assert.True(hidden.IsHidden);
        Assert.True(restored.Success);
        Assert.False(restored.IsHidden);
    }

    [Fact]
    public void MissingSourceReturnsUnsuccessfulPlan()
    {
        var alias = NativeHostSourcePreferenceCommandPlanner.PlanSetAlias(
            ["plugin:lead"],
            "plugin:missing",
            "Lead Bus");
        var hidden = NativeHostSourcePreferenceCommandPlanner.PlanSetHidden(
            ["plugin:lead"],
            "plugin:missing",
            isHidden: true);

        Assert.False(alias.Success);
        Assert.False(hidden.Success);
    }

    [Fact]
    public void PlanForgetFlagsSelectedSourceClearing()
    {
        var selected = NativeHostSourcePreferenceCommandPlanner.PlanForget(
            "plugin:lead",
            selectedSourceKey: "plugin:lead");
        var notSelected = NativeHostSourcePreferenceCommandPlanner.PlanForget(
            "plugin:lead",
            selectedSourceKey: "plugin:other");

        Assert.True(selected.Success);
        Assert.True(selected.ShouldForget);
        Assert.True(selected.ShouldClearSelected);
        Assert.True(notSelected.Success);
        Assert.True(notSelected.ShouldForget);
        Assert.False(notSelected.ShouldClearSelected);
    }
}
