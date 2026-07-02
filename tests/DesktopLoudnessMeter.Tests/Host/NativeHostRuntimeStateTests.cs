using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostRuntimeStateTests
{
    [Fact]
    public void SelectedSourceKeyRoundTripsAndClears()
    {
        using var state = new NativeHostRuntimeState();

        state.SetSelectedSourceKey("plugin:lead");
        var selected = state.Snapshot;
        state.ClearSelectedSourceKey();
        var cleared = state.Snapshot;

        Assert.Equal("plugin:lead", selected.SelectedSourceKey);
        Assert.Null(cleared.SelectedSourceKey);
    }

    [Fact]
    public void ShowHiddenSourcesRoundTrips()
    {
        using var state = new NativeHostRuntimeState();

        state.ShowHiddenSources = true;
        var shown = state.Snapshot;
        state.ShowHiddenSources = false;
        var hidden = state.Snapshot;

        Assert.True(shown.ShowHiddenSources);
        Assert.False(hidden.ShowHiddenSources);
    }

    [Fact]
    public void PerformanceModeRoundTrips()
    {
        using var state = new NativeHostRuntimeState();

        state.PerformanceMode = HostPerformanceMode.Eco;
        var eco = state.Snapshot;
        state.PerformanceMode = HostPerformanceMode.Normal;
        var normal = state.Snapshot;

        Assert.Equal(HostPerformanceMode.Eco, eco.PerformanceMode);
        Assert.Equal(HostPerformanceMode.Normal, normal.PerformanceMode);
    }

    [Fact]
    public void AppliesSelectedSourcePlannerResults()
    {
        using var state = new NativeHostRuntimeState();

        state.ApplySelectedSourcePlan(NativeHostSelectedSourceCommandPlanner.PlanSelect(["plugin:lead"], "plugin:lead"));
        var selected = state.Snapshot;
        state.ApplySelectedSourcePlan(NativeHostSelectedSourceCommandPlanner.PlanBackToGlobal());
        var cleared = state.Snapshot;

        Assert.Equal("plugin:lead", selected.SelectedSourceKey);
        Assert.Null(cleared.SelectedSourceKey);
    }

    [Fact]
    public void AppliesShowHiddenAndPerformancePlannerResults()
    {
        using var state = new NativeHostRuntimeState();

        state.ApplyShowHiddenPlan(NativeHostGlobalSettingsCommandPlanner.PlanSetShowHiddenSources(true));
        state.ApplyPerformanceModePlan(NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(HostPerformanceMode.Eco));
        var snapshot = state.Snapshot;

        Assert.True(snapshot.ShowHiddenSources);
        Assert.Equal(HostPerformanceMode.Eco, snapshot.PerformanceMode);
    }
}
