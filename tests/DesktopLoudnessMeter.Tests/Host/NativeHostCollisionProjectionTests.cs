using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostCollisionProjectionTests
{
    [Fact]
    public void EmptyInputsReturnNoBands()
    {
        Assert.Empty(Project([], [[0.5, 0.5]], CollisionFrequencyScale.Logarithmic));
        Assert.Empty(Project([0.5, 0.5], [], CollisionFrequencyScale.Logarithmic));
        Assert.Empty(Project([0.5, 0.5], [null, []], CollisionFrequencyScale.Logarithmic));
    }

    [Fact]
    public void MatchesManagedProjectionForSelectedComparison()
    {
        var focus = new[] { 0.0, 0.42, 0.72, 0.18 };
        IReadOnlyList<double>[] comparisons =
        [
            [0.0, 0.43, 0.18, 0.08]
        ];

        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -120, 0, 0.25, CollisionFrequencyScale.Mel),
            Project(focus, comparisons, CollisionFrequencyScale.Mel, levelFloorDb: -120, prominenceThresholdDb: 0, relativeThreshold: 0.25));
    }

    [Fact]
    public void MatchesManagedProjectionForUnequalBucketCounts()
    {
        var focus = new[] { 0.7, 0.7, 0.1, 0.9, 0.9 };
        IReadOnlyList<double>[] comparisons =
        [
            [0.68, 0.69, 0.02]
        ];

        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -120, 0, 0.2, CollisionFrequencyScale.Logarithmic),
            Project(focus, comparisons, CollisionFrequencyScale.Logarithmic, levelFloorDb: -120, prominenceThresholdDb: 0, relativeThreshold: 0.2));
    }

    [Fact]
    public void MatchesManagedProjectionForMultiComparisonStrongestIntensity()
    {
        var focus = new[] { 0.8, 0.1, 0.75, 0.74, 0.05 };
        IReadOnlyList<double>[] comparisons =
        [
            [0.78, 0.0, 0.10, 0.08, 0.0],
            [0.20, 0.0, 0.74, 0.73, 0.0]
        ];

        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -120, 0, 0.12, CollisionFrequencyScale.Logarithmic),
            Project(focus, comparisons, CollisionFrequencyScale.Logarithmic, levelFloorDb: -120, prominenceThresholdDb: 0, relativeThreshold: 0.12));
    }

    [Fact]
    public void MatchesManagedProjectionForProminenceAndFloorClamps()
    {
        var focus = new[] { 0.05, 0.25, 0.90, 0.25, 0.05, 0.20, 0.82, 0.20, 0.05 };
        IReadOnlyList<double>[] comparisons =
        [
            [0.04, 0.22, 0.88, 0.20, 0.04, 0.18, 0.80, 0.18, 0.04]
        ];

        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -18, 12, 0.10, CollisionFrequencyScale.Mel),
            Project(focus, comparisons, CollisionFrequencyScale.Mel, levelFloorDb: -18, prominenceThresholdDb: 12, relativeThreshold: 0.10));
    }

    [Fact]
    public void ProducesDifferentScaleRatiosForMelAndLogarithmicModes()
    {
        var focus = new[] { 0.8, 0.8, 0.0, 0.7 };
        IReadOnlyList<double>[] comparisons =
        [
            [0.79, 0.78, 0.0, 0.69]
        ];

        var mel = Project(focus, comparisons, CollisionFrequencyScale.Mel, levelFloorDb: -120, prominenceThresholdDb: 0, relativeThreshold: 0.2);
        var log = Project(focus, comparisons, CollisionFrequencyScale.Logarithmic, levelFloorDb: -120, prominenceThresholdDb: 0, relativeThreshold: 0.2);

        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -120, 0, 0.2, CollisionFrequencyScale.Mel),
            mel);
        AssertBandsEqual(
            FrequencyCollisionAnalyzer.AnalyzeSpectra(focus, comparisons, -120, 0, 0.2, CollisionFrequencyScale.Logarithmic),
            log);
        Assert.NotEqual(mel.Select(band => band.StartRatio).ToArray(), log.Select(band => band.StartRatio).ToArray());
    }

    private static IReadOnlyList<FrequencyCollisionBand> Project(
        IReadOnlyList<double>? focus,
        IEnumerable<IReadOnlyList<double>?> comparisons,
        CollisionFrequencyScale scale,
        double levelFloorDb = -120,
        double prominenceThresholdDb = 0,
        double relativeThreshold = 0.18)
    {
        return NativeHostCollisionProjection.Project(
            focus,
            comparisons,
            levelFloorDb,
            prominenceThresholdDb,
            relativeThreshold,
            scale);
    }

    private static void AssertBandsEqual(
        IReadOnlyList<FrequencyCollisionBand> expected,
        IReadOnlyList<FrequencyCollisionBand> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].StartRatio, actual[index].StartRatio, precision: 10);
            Assert.Equal(expected[index].EndRatio, actual[index].EndRatio, precision: 10);
            Assert.Equal(expected[index].Intensity, actual[index].Intensity, precision: 10);
        }
    }
}
