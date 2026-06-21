namespace DesktopLoudnessMeter.App.ViewModels;

internal static class FrequencyCollisionAnalyzer
{
    private const double DisplayFloorDb = -200;
    private const double DisplayRangeDb = 200;
    private const int MaximumProminenceRadius = 8;
    private const double ComparisonTolerance = 1e-9;

    public static IReadOnlyList<FrequencyCollisionBand> Analyze(
        IReadOnlyList<AudioSessionMeterViewModel> sessions,
        string focusSourceKey,
        string selectedComparisonSourceKey,
        bool compareAllComparisons,
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold)
    {
        var active = sessions
            .Where(session => !session.IsInstanceMissing && session.SpectrumBuckets.Count > 0)
            .ToArray();
        var focus = active.FirstOrDefault(session =>
            string.Equals(session.SourceKey, focusSourceKey, StringComparison.Ordinal));
        if (focus is null)
        {
            return Array.Empty<FrequencyCollisionBand>();
        }

        var comparisons = compareAllComparisons
            ? active.Where(session => !ReferenceEquals(session, focus)).ToArray()
            : active.Where(session =>
                !ReferenceEquals(session, focus)
                && string.Equals(session.SourceKey, selectedComparisonSourceKey, StringComparison.Ordinal)).ToArray();
        if (comparisons.Length == 0)
        {
            return Array.Empty<FrequencyCollisionBand>();
        }

        var bucketCount = Math.Min(focus.SpectrumBuckets.Count, comparisons.Min(session => session.SpectrumBuckets.Count));
        if (bucketCount <= 0)
        {
            return Array.Empty<FrequencyCollisionBand>();
        }

        var collisions = new double[bucketCount];
        AnalyzeFocus(focus, comparisons, bucketCount, levelFloorDb, prominenceThresholdDb, relativeThreshold, collisions);
        return CompactBands(collisions);
    }

    private static void AnalyzeFocus(
        AudioSessionMeterViewModel focus,
        IEnumerable<AudioSessionMeterViewModel> comparisons,
        int bucketCount,
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold,
        double[] collisions)
    {
        var safeLevelFloorDb = Math.Clamp(levelFloorDb, DisplayFloorDb, 0);
        var safeLevelFloor = DbToDisplayLevel(safeLevelFloorDb);
        var safeProminenceThresholdDb = Math.Clamp(prominenceThresholdDb, 0, 48);
        var safeRelativeThreshold = Math.Max(relativeThreshold, 0.001);
        var focusProminence = CalculateProminenceScores(
            focus.SpectrumBuckets,
            bucketCount,
            safeLevelFloor,
            safeProminenceThresholdDb);

        foreach (var comparison in comparisons)
        {
            var comparisonProminence = CalculateProminenceScores(
                comparison.SpectrumBuckets,
                bucketCount,
                safeLevelFloor,
                safeProminenceThresholdDb);
            AnalyzePair(
                focus.SpectrumBuckets,
                comparison.SpectrumBuckets,
                bucketCount,
                safeLevelFloor,
                safeRelativeThreshold,
                focusProminence,
                comparisonProminence,
                collisions);
        }
    }

    private static void AnalyzePair(
        IReadOnlyList<double> first,
        IReadOnlyList<double> second,
        int bucketCount,
        double safeLevelFloor,
        double safeRelativeThreshold,
        IReadOnlyList<double> firstProminence,
        IReadOnlyList<double> secondProminence,
        double[] collisions)
    {
        for (var index = 0; index < bucketCount; index++)
        {
            var firstLevel = Math.Clamp(first[index], 0, 1);
            var secondLevel = Math.Clamp(second[index], 0, 1);
            var stronger = Math.Max(firstLevel, secondLevel);
            var weaker = Math.Min(firstLevel, secondLevel);
            if (weaker + ComparisonTolerance < safeLevelFloor)
            {
                continue;
            }

            var prominenceFit = Math.Min(firstProminence[index], secondProminence[index]);
            if (prominenceFit <= 0)
            {
                continue;
            }

            var displayGap = Math.Abs(firstLevel - secondLevel);
            var relativeGap = displayGap / Math.Max(stronger, safeLevelFloor);
            if (relativeGap > safeRelativeThreshold + ComparisonTolerance)
            {
                continue;
            }

            var relativeFit = 1 - (relativeGap / safeRelativeThreshold);
            var intensity = Math.Clamp(relativeFit * prominenceFit * stronger, 0, 1);
            collisions[index] = Math.Max(collisions[index], intensity);
        }
    }

    private static double[] CalculateProminenceScores(
        IReadOnlyList<double> levels,
        int bucketCount,
        double safeLevelFloor,
        double prominenceThresholdDb)
    {
        var scores = new double[bucketCount];
        if (bucketCount < 3 || prominenceThresholdDb <= 0)
        {
            Array.Fill(scores, 1);
            return scores;
        }

        var radius = GetProminenceRadius(bucketCount);
        for (var index = 0; index < bucketCount; index++)
        {
            var level = Math.Clamp(levels[index], 0, 1);
            if (level + ComparisonTolerance < safeLevelFloor)
            {
                continue;
            }

            var localBackground = EstimateLocalBackground(levels, bucketCount, index, radius);
            var prominenceDb = (level - localBackground) * DisplayRangeDb;
            if (prominenceDb + ComparisonTolerance < prominenceThresholdDb)
            {
                continue;
            }

            scores[index] = Math.Clamp(prominenceDb / (prominenceThresholdDb * 2), 0, 1);
        }

        return scores;
    }

    private static int GetProminenceRadius(int bucketCount)
    {
        return Math.Clamp(bucketCount / 32, 1, MaximumProminenceRadius);
    }

    private static double EstimateLocalBackground(
        IReadOnlyList<double> levels,
        int bucketCount,
        int centerIndex,
        int radius)
    {
        var start = Math.Max(0, centerIndex - radius);
        var end = Math.Min(bucketCount - 1, centerIndex + radius);
        var sum = 0d;
        var count = 0;

        for (var index = start; index <= end; index++)
        {
            if (index == centerIndex)
            {
                continue;
            }

            sum += Math.Clamp(levels[index], 0, 1);
            count++;
        }

        return count == 0 ? 0 : sum / count;
    }

    private static double DbToDisplayLevel(double decibels)
    {
        return Math.Clamp((decibels - DisplayFloorDb) / DisplayRangeDb, 0, 1);
    }

    private static IReadOnlyList<FrequencyCollisionBand> CompactBands(IReadOnlyList<double> collisions)
    {
        List<FrequencyCollisionBand>? bands = null;
        var start = -1;
        var strongest = 0d;

        for (var index = 0; index <= collisions.Count; index++)
        {
            var value = index < collisions.Count ? collisions[index] : 0;
            if (value > 0)
            {
                if (start < 0)
                {
                    start = index;
                    strongest = value;
                }
                else
                {
                    strongest = Math.Max(strongest, value);
                }

                continue;
            }

            if (start < 0)
            {
                continue;
            }

            bands ??= [];
            bands.Add(new FrequencyCollisionBand(
                (double)start / collisions.Count,
                (double)index / collisions.Count,
                Math.Clamp(strongest, 0, 1)));
            start = -1;
            strongest = 0;
        }

        return bands is null ? Array.Empty<FrequencyCollisionBand>() : bands;
    }
}
