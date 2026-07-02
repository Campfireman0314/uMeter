using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostRuntimeServiceCommandPlannerTests
{
    [Fact]
    public void PlanResetAllReturnsResetTarget()
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanResetAll();

        Assert.True(plan.Success);
        Assert.True(plan.ShouldResetAll);
        Assert.False(plan.ShouldResetSource);
    }

    [Fact]
    public void PlanResetSourceReturnsTargetIdentity()
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            ["plugin:lead", "plugin:keys"],
            "plugin:lead",
            SignalSourceKind.Plugin,
            processId: 42);

        Assert.True(plan.Success);
        Assert.False(plan.ShouldResetAll);
        Assert.True(plan.ShouldResetSource);
        Assert.Equal("plugin:lead", plan.SourceKey);
        Assert.Equal(SignalSourceKind.Plugin, plan.SourceKind);
        Assert.Equal(42, plan.ProcessId);
    }

    [Fact]
    public void PlanResetSourceMissingReturnsUnsuccessfulPlan()
    {
        var plan = NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            ["plugin:lead"],
            "plugin:missing",
            SignalSourceKind.Plugin,
            processId: null);

        Assert.False(plan.Success);
        Assert.False(plan.ShouldResetSource);
        Assert.Null(plan.SourceKey);
    }

    [Fact]
    public void PlanPerformanceModePreservesHostModeAndMapsAnalyzerMode()
    {
        var eco = NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(HostPerformanceMode.Eco);
        var normal = NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(HostPerformanceMode.Normal);
        var invalid = NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode((HostPerformanceMode)42);

        Assert.True(eco.Success);
        Assert.Equal(HostPerformanceMode.Eco, eco.HostMode);
        Assert.Equal(PcmAnalysisPerformanceMode.Eco, eco.AnalysisMode);
        Assert.True(normal.Success);
        Assert.Equal(HostPerformanceMode.Normal, normal.HostMode);
        Assert.Equal(PcmAnalysisPerformanceMode.Normal, normal.AnalysisMode);
        Assert.True(invalid.Success);
        Assert.Equal((HostPerformanceMode)42, invalid.HostMode);
        Assert.Equal(PcmAnalysisPerformanceMode.Normal, invalid.AnalysisMode);
    }
}
