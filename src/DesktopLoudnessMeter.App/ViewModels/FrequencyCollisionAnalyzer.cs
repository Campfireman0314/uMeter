namespace DesktopLoudnessMeter.App.ViewModels;

internal static class FrequencyCollisionAnalyzer
{
    private const double DisplayFloorDb = -96;
    private const double DisplayRangeDb = 96;
    private const double MinimumAudibleDisplayLevel = 0.04;

    public static IReadOnlyList<FrequencyCollisionBand> Analyze(
        IReadOnlyList<AudioSessionMeterViewModel> sessions,
        string focusSourceKey,
        string selectedComparisonSourceKey,
        bool compareAllComparisons,
        double absoluteThresholdDb,
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
        AnalyzeFocus(focus, comparisons, bucketCount, absoluteThresholdDb, relativeThreshold, collisions);
        return CompactBands(collisions);
    }

    private static void AnalyzeFocus(
        AudioSessionMeterViewModel focus,
        IEnumerable<AudioSessionMeterViewModel> comparisons,
        int bucketCount,
        double absoluteThresholdDb,
        double relativeThreshold,
        double[] collisions)
    {
        foreach (var comparison in comparisons)
        {
            AnalyzePair(
                focus.SpectrumBuckets,
                comparison.SpectrumBuckets,
                bucketCount,
                absoluteThresholdDb,
                relativeThreshold,
                collisions);
        }
    }

    private static void AnalyzePair(
        IReadOnlyList<double> first,
        IReadOnlyList<double> second,
        int bucketCount,
        double absoluteThresholdDb,
        double relativeThreshold,
        double[] collisions)
    {
        var safeAbsoluteThresholdDb = Math.Max(absoluteThresholdDb, 0.001);
        var safeRelativeThreshold = Math.Max(relativeThreshold, 0.001);

        for (var index = 0; index < bucketCount; index++)
        {
            var firstLevel = Math.Clamp(first[index], 0, 1);
            var secondLevel = Math.Clamp(second[index], 0, 1);
            var stronger = Math.Max(firstLevel, secondLevel);
            if (stronger < MinimumAudibleDisplayLevel)
            {
                continue;
            }

            var displayGap = Math.Abs(firstLevel - secondLevel);
            var absoluteGapDb = Math.Abs(DisplayLevelToDb(firstLevel) - DisplayLevelToDb(secondLevel));
            var relativeGap = displayGap / Math.Max(stronger, MinimumAudibleDisplayLevel);
            if (absoluteGapDb > absoluteThresholdDb || relativeGap > relativeThreshold)
            {
                continue;
            }

            var absoluteFit = 1 - (absoluteGapDb / safeAbsoluteThresholdDb);
            var relativeFit = 1 - (relativeGap / safeRelativeThreshold);
            var intensity = Math.Clamp(Math.Min(absoluteFit, relativeFit) * stronger, 0, 1);
            collisions[index] = Math.Max(collisions[index], intensity);
        }
    }

    private static double DisplayLevelToDb(double displayLevel)
    {
        return DisplayFloorDb + (Math.Clamp(displayLevel, 0, 1) * DisplayRangeDb);
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
