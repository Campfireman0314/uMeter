using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostChartSettingsCommandPlannerTests
{
    [Fact]
    public void PlanSetTruePeakOversamplingReturnsServiceValue()
    {
        var plan = NativeHostChartSettingsCommandPlanner.PlanSetTruePeakOversampling(TruePeakOversampling.EightX);

        Assert.Equal(TruePeakOversampling.EightX, plan.Oversampling);
        Assert.Equal(8, plan.ServiceOversampling);
    }

    [Fact]
    public void PlanSetDisplayModesReturnChannelEnabledFlags()
    {
        var waveformChannels = NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(WaveformDisplayMode.Channels);
        var waveformOverall = NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(WaveformDisplayMode.Overall);
        var spectrumChannels = NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(SpectrumDisplayMode.Channels);
        var spectrumOverall = NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(SpectrumDisplayMode.Overall);

        Assert.Equal(WaveformDisplayMode.Channels, waveformChannels.DisplayMode);
        Assert.True(waveformChannels.ChannelEnabled);
        Assert.False(waveformOverall.ChannelEnabled);
        Assert.Equal(SpectrumDisplayMode.Channels, spectrumChannels.DisplayMode);
        Assert.True(spectrumChannels.ChannelEnabled);
        Assert.False(spectrumOverall.ChannelEnabled);
    }

    [Fact]
    public void PlanSetDetailModuleVisibilityReturnsTarget()
    {
        var plan = NativeHostChartSettingsCommandPlanner.PlanSetDetailModuleVisibility(DetailModuleKind.Collision, true);

        Assert.Equal(DetailModuleKind.Collision, plan.ModuleKind);
        Assert.True(plan.IsVisible);
    }

    [Fact]
    public void PlanSetSpectrumDisplayRangePreservesValues()
    {
        var plan = NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayRange(-84, -12);

        Assert.Equal(-84, plan.FloorDb);
        Assert.Equal(-12, plan.CeilingDb);
    }

    [Fact]
    public void PlanSetLoudnessClockSettingsPreservesValues()
    {
        var plan = NativeHostChartSettingsCommandPlanner.PlanSetLoudnessClockSettings(-72, -12, -18, 90);

        Assert.Equal(-72, plan.MinimumDb);
        Assert.Equal(-12, plan.MaximumDb);
        Assert.Equal(-18, plan.WarningDb);
        Assert.Equal(90, plan.CycleSeconds);
    }

    [Fact]
    public void PlanSetCollisionSettingsTrimsAndClearsComparisonSourceKey()
    {
        var trimmed = NativeHostChartSettingsCommandPlanner.PlanSetCollisionSettings(
            -96,
            9,
            0.25,
            CollisionFrequencyScale.Mel,
            "\u3000plugin:keys\u00A0");
        var cleared = NativeHostChartSettingsCommandPlanner.PlanSetCollisionSettings(
            -96,
            9,
            0.25,
            CollisionFrequencyScale.Logarithmic,
            "\u3000\u00A0");

        Assert.Equal(-96, trimmed.LevelFloorDb);
        Assert.Equal(9, trimmed.ProminenceThresholdDb);
        Assert.Equal(0.25, trimmed.RelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, trimmed.FrequencyScale);
        Assert.Equal("plugin:keys", trimmed.ComparisonSourceKey);
        Assert.Null(cleared.ComparisonSourceKey);
        Assert.Equal(CollisionFrequencyScale.Logarithmic, cleared.FrequencyScale);
    }
}
