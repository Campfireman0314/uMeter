using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostGlobalSettingsCommandPlannerTests
{
    [Fact]
    public void PlanSetThemeTrimsThemeAndClearsBlankTheme()
    {
        var trimmed = NativeHostGlobalSettingsCommandPlanner.PlanSetTheme("\u3000custom-theme\u00A0");
        var cleared = NativeHostGlobalSettingsCommandPlanner.PlanSetTheme("\u3000\u00A0");

        Assert.Equal("custom-theme", trimmed.ThemeId);
        Assert.Null(cleared.ThemeId);
    }

    [Fact]
    public void PlanSetThemeDoesNotEnforceWebBridgeWhitelist()
    {
        var plan = NativeHostGlobalSettingsCommandPlanner.PlanSetTheme("  studio-green  ");

        Assert.Equal("studio-green", plan.ThemeId);
    }

    [Fact]
    public void PlanSetRecentEventLimitClampsToSupportedRange()
    {
        var low = NativeHostGlobalSettingsCommandPlanner.PlanSetRecentEventLimit(-10);
        var preserved = NativeHostGlobalSettingsCommandPlanner.PlanSetRecentEventLimit(42);
        var high = NativeHostGlobalSettingsCommandPlanner.PlanSetRecentEventLimit(999);

        Assert.Equal(1, low.MaxRecentEventEntries);
        Assert.Equal(42, preserved.MaxRecentEventEntries);
        Assert.Equal(200, high.MaxRecentEventEntries);
    }

    [Fact]
    public void PlanSetEventOutputFolderTrimsFolderAndClearsBlankFolder()
    {
        var trimmed = NativeHostGlobalSettingsCommandPlanner.PlanSetEventOutputFolder("\u3000C:\\uMeter Events\u00A0");
        var cleared = NativeHostGlobalSettingsCommandPlanner.PlanSetEventOutputFolder("\u3000\u00A0");

        Assert.Equal("C:\\uMeter Events", trimmed.EventOutputFolder);
        Assert.Null(cleared.EventOutputFolder);
    }

    [Fact]
    public void PlanSetShowHiddenSourcesReturnsTargetState()
    {
        var shown = NativeHostGlobalSettingsCommandPlanner.PlanSetShowHiddenSources(true);
        var hidden = NativeHostGlobalSettingsCommandPlanner.PlanSetShowHiddenSources(false);

        Assert.True(shown.ShowHiddenSources);
        Assert.False(hidden.ShowHiddenSources);
    }
}
