using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class HostWorkspaceCommandEffectApplierTests
{
    [Fact]
    public void AppliesGlobalSettingsPlans()
    {
        var workspace = new UserWorkspaceSettings();
        var applier = new HostWorkspaceCommandEffectApplier(workspace);

        applier.ApplyTheme(NativeHostGlobalSettingsCommandPlanner.PlanSetTheme("  studio-green  "));
        applier.ApplyRecentEventLimit(NativeHostGlobalSettingsCommandPlanner.PlanSetRecentEventLimit(999));
        applier.ApplyEventOutputFolder(NativeHostGlobalSettingsCommandPlanner.PlanSetEventOutputFolder("  C:\\uMeter Events  "));

        Assert.Equal("studio-green", workspace.ThemeId);
        Assert.Equal(200, workspace.MaxRecentEventEntries);
        Assert.Equal("C:\\uMeter Events", workspace.EventOutputFolder);
    }

    [Fact]
    public void AppliesSourcePreferenceAndOrderPlans()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:lead");
        workspace.GetOrCreateSource("plugin:keys");
        var applier = new HostWorkspaceCommandEffectApplier(workspace);

        applier.ApplySourceAlias(
            "plugin:lead",
            NativeHostSourcePreferenceCommandPlanner.PlanSetAlias(["plugin:lead"], "plugin:lead", "  Lead Bus  "));
        applier.ApplySourceHidden(
            "plugin:lead",
            NativeHostSourcePreferenceCommandPlanner.PlanSetHidden(["plugin:lead"], "plugin:lead", isHidden: true));
        applier.ApplySourceOrder(
            NativeHostSourceOrderCommandPlanner.PlanMoveToIndex(
                ["plugin:lead", "plugin:keys"],
                "plugin:keys",
                0));

        var lead = workspace.GetSource("plugin:lead");
        var keys = workspace.GetSource("plugin:keys");
        Assert.Equal("Lead Bus", lead?.Alias);
        Assert.True(lead?.IsHidden);
        Assert.Equal(1, lead?.OrderIndex);
        Assert.Equal(0, keys?.OrderIndex);
    }

    [Fact]
    public void AppliesForgetPlanOnlyWhenNativePlanAllowsForget()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:lead").Alias = "Lead Bus";
        var applier = new HostWorkspaceCommandEffectApplier(workspace);

        applier.ApplyForgetSource(
            "plugin:lead",
            NativeHostSourcePreferenceCommandPlanner.PlanForget("plugin:lead", selectedSourceKey: null));

        Assert.Null(workspace.GetSource("plugin:lead"));
    }

    [Fact]
    public void AppliesChartSettingsPlans()
    {
        var workspace = new UserWorkspaceSettings();
        var applier = new HostWorkspaceCommandEffectApplier(workspace);

        applier.ApplyTruePeakOversampling(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetTruePeakOversampling(TruePeakOversampling.EightX));
        applier.ApplyWaveformDisplayMode(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(WaveformDisplayMode.Channels));
        applier.ApplySpectrumDisplayMode(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(SpectrumDisplayMode.Channels));
        applier.ApplyDetailModuleVisibility(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetDetailModuleVisibility(DetailModuleKind.Collision, true));
        applier.ApplySpectrumDisplayRange(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayRange(-84, -12));
        applier.ApplyLoudnessClockSettings(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetLoudnessClockSettings(-72, -12, -18, 90));
        applier.ApplyCollisionSettings(
            "plugin:lead",
            NativeHostChartSettingsCommandPlanner.PlanSetCollisionSettings(
                -96,
                9,
                0.25,
                CollisionFrequencyScale.Mel,
                "  plugin:keys  "));

        var source = workspace.GetSource("plugin:lead");
        Assert.Equal(TruePeakOversampling.EightX, source?.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, source?.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, source?.SpectrumDisplayMode);
        Assert.True(source?.InlineCollisionVisible);
        Assert.Equal(-84, source?.SpectrumDisplayFloorDb);
        Assert.Equal(-12, source?.SpectrumDisplayCeilingDb);
        Assert.Equal(-72, source?.LoudnessClockMinimumDb);
        Assert.Equal(-12, source?.LoudnessClockMaximumDb);
        Assert.Equal(-18, source?.LoudnessClockWarningDb);
        Assert.Equal(90, source?.LoudnessClockCycleSeconds);
        Assert.Equal(-96, source?.CollisionLevelFloorDb);
        Assert.Equal(9, source?.CollisionProminenceThresholdDb);
        Assert.Equal(0.25, source?.CollisionRelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, source?.CollisionFrequencyScale);
        Assert.Equal("plugin:keys", source?.InlineCollisionSourceKey);
    }
}
