using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSelectedDetailShellPlannerTests
{
    [Fact]
    public void ResolvesSelectedSourceIndexAndVisibleModules()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [
                new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead"),
                new NativeHostSelectedDetailShellSourceInput("plugin:keys", "Keys")
            ],
            [
                new NativeHostSelectedDetailShellModuleInput(DetailModuleKind.Waveform, true),
                new NativeHostSelectedDetailShellModuleInput(DetailModuleKind.Collision, true),
                new NativeHostSelectedDetailShellModuleInput(DetailModuleKind.Spectrum, false)
            ],
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: null,
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.True(plan.HasSelectedSource);
        Assert.Equal(0, plan.SelectedSourceIndex);
        Assert.True(plan.IsWaveformVisible);
        Assert.True(plan.IsCollisionVisible);
        Assert.False(plan.IsSpectrumVisible);
        Assert.False(plan.IsStereoVisible);
        Assert.False(plan.IsLoudnessClockVisible);
    }

    [Fact]
    public void MissingSelectedSourceReturnsNoSelection()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead")],
            [],
            selectedSourceKey: "plugin:missing",
            comparisonSourceKey: null,
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.False(plan.HasSelectedSource);
        Assert.Null(plan.SelectedSourceIndex);
        Assert.Empty(plan.ComparisonSourceIndexes);
    }

    [Fact]
    public void MatchedComparisonUsesOnlyThatSource()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [
                new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead"),
                new NativeHostSelectedDetailShellSourceInput("plugin:keys", "Keys"),
                new NativeHostSelectedDetailShellSourceInput("plugin:pad", "Pad")
            ],
            [],
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: "plugin:keys",
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.Equal("plugin:keys", plan.ComparisonSourceKey);
        Assert.Equal("Keys", plan.ComparisonDisplayName);
        Assert.Equal([1], plan.ComparisonSourceIndexes);
    }

    [Fact]
    public void MissingComparisonPreservesKeyAndUsesAllOtherSources()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [
                new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead"),
                new NativeHostSelectedDetailShellSourceInput("plugin:keys", "Keys"),
                new NativeHostSelectedDetailShellSourceInput("plugin:pad", "Pad")
            ],
            [],
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: "plugin:missing",
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.Equal("plugin:missing", plan.ComparisonSourceKey);
        Assert.Null(plan.ComparisonDisplayName);
        Assert.Equal([1, 2], plan.ComparisonSourceIndexes);
    }

    [Fact]
    public void BlankComparisonUsesAllOtherSourcesAndDefaultsCollisionSettings()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [
                new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead"),
                new NativeHostSelectedDetailShellSourceInput("plugin:keys", "Keys")
            ],
            [],
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: " ",
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.Null(plan.ComparisonSourceKey);
        Assert.Null(plan.ComparisonDisplayName);
        Assert.Equal([1], plan.ComparisonSourceIndexes);
        Assert.Equal(-120, plan.LevelFloorDb);
        Assert.Equal(6, plan.ProminenceThresholdDb);
        Assert.Equal(0.18, plan.RelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Logarithmic, plan.FrequencyScale);
    }

    [Fact]
    public void PreservesProvidedCollisionSettings()
    {
        var plan = NativeHostSelectedDetailShellPlanner.Project(
            [new NativeHostSelectedDetailShellSourceInput("plugin:lead", "Lead")],
            [],
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: null,
            levelFloorDb: -42,
            prominenceThresholdDb: 9,
            relativeThreshold: 0.25,
            frequencyScale: CollisionFrequencyScale.Mel);

        Assert.Equal(-42, plan.LevelFloorDb);
        Assert.Equal(9, plan.ProminenceThresholdDb);
        Assert.Equal(0.25, plan.RelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, plan.FrequencyScale);
    }
}
