using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSelectedDetailSnapshotPlannerTests
{
    [Fact]
    public void ProjectsSelectedSourceModulesDefaultsAndCollisionBands()
    {
        var plan = NativeHostSelectedDetailSnapshotPlanner.Project(
            [
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:lead", "Lead", [0.0, 0.42, 0.72, 0.18]),
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:keys", "Keys", [0.0, 0.43, 0.18, 0.08])
            ],
            [
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Spectrum, 0, 360),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Waveform, 1, 240)
            ],
            new NativeHostDetailModuleVisibility(
                Stereo: null,
                Collision: true,
                LoudnessClock: true,
                Waveform: true,
                Spectrum: true),
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: null,
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.True(plan.HasSelectedSource);
        Assert.Equal(0, plan.SelectedSourceIndex);
        Assert.Equal(
            [DetailModuleKind.Spectrum, DetailModuleKind.Waveform, DetailModuleKind.Stereo, DetailModuleKind.Collision, DetailModuleKind.LoudnessClock],
            plan.Modules.Select(module => module.Kind).ToArray());
        Assert.Equal("Spectrum", plan.Modules[0].Title);
        Assert.Equal(360, plan.Modules.Single(module => module.Kind == DetailModuleKind.Spectrum).Height);
        Assert.True(plan.IsWaveformVisible);
        Assert.True(plan.IsSpectrumVisible);
        Assert.True(plan.IsStereoVisible);
        Assert.True(plan.IsLoudnessClockVisible);
        Assert.True(plan.IsCollisionVisible);
        Assert.Null(plan.Collision.ComparisonSourceKey);
        Assert.Null(plan.Collision.ComparisonDisplayName);
        Assert.Equal(-120, plan.Collision.LevelFloorDb);
        Assert.Equal(6, plan.Collision.ProminenceThresholdDb);
        Assert.Equal(0.18, plan.Collision.RelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Logarithmic, plan.Collision.FrequencyScale);
        Assert.Contains(plan.Collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
    }

    [Fact]
    public void MatchedComparisonUsesOnlyThatSourceAndPreservesSettings()
    {
        var plan = NativeHostSelectedDetailSnapshotPlanner.Project(
            [
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:lead", "Lead", [0.0, 0.42, 0.72, 0.18]),
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:keys", "Keys", [0.0, 0.43, 0.18, 0.08]),
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:pad", "Pad", [0.0, 0.0, 0.0, 0.0])
            ],
            [],
            NativeHostDetailModuleVisibility.Default,
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: "plugin:keys",
            levelFloorDb: -120,
            prominenceThresholdDb: 0,
            relativeThreshold: 0.25,
            frequencyScale: CollisionFrequencyScale.Mel);

        Assert.Equal("plugin:keys", plan.Collision.ComparisonSourceKey);
        Assert.Equal("Keys", plan.Collision.ComparisonDisplayName);
        Assert.Equal(-120, plan.Collision.LevelFloorDb);
        Assert.Equal(0, plan.Collision.ProminenceThresholdDb);
        Assert.Equal(0.25, plan.Collision.RelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, plan.Collision.FrequencyScale);
        Assert.Contains(plan.Collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
    }

    [Fact]
    public void MissingComparisonPreservesRequestedKeyAndUsesAllOtherSources()
    {
        var plan = NativeHostSelectedDetailSnapshotPlanner.Project(
            [
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:lead", "Lead", [0.0, 0.42, 0.72, 0.18]),
                new NativeHostSelectedDetailSnapshotSourceInput("plugin:keys", "Keys", [0.0, 0.43, 0.18, 0.08])
            ],
            [],
            new NativeHostDetailModuleVisibility(Collision: true),
            selectedSourceKey: "plugin:lead",
            comparisonSourceKey: "plugin:missing",
            levelFloorDb: -120,
            prominenceThresholdDb: 0,
            relativeThreshold: 0.25,
            frequencyScale: CollisionFrequencyScale.Logarithmic);

        Assert.Equal("plugin:missing", plan.Collision.ComparisonSourceKey);
        Assert.Null(plan.Collision.ComparisonDisplayName);
        Assert.Contains(plan.Collision.Bands, band => band.StartRatio < band.EndRatio && band.Intensity > 0);
    }

    [Fact]
    public void MissingSelectedSourceReturnsNoSelectionAndNoBands()
    {
        var plan = NativeHostSelectedDetailSnapshotPlanner.Project(
            [new NativeHostSelectedDetailSnapshotSourceInput("plugin:lead", "Lead", [0.5, 0.5])],
            [],
            NativeHostDetailModuleVisibility.Default,
            selectedSourceKey: "plugin:missing",
            comparisonSourceKey: null,
            levelFloorDb: null,
            prominenceThresholdDb: null,
            relativeThreshold: null,
            frequencyScale: null);

        Assert.False(plan.HasSelectedSource);
        Assert.Null(plan.SelectedSourceIndex);
        Assert.Empty(plan.Collision.Bands);
    }
}
