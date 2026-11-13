using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// Integrated loudness over long histories must stay the exact BS.1770 two-pass computation while the
/// work per completed block stops growing with the retained history. The oracle in this file is a
/// separate, deliberately naive two-pass implementation written from the published formula: it shares no
/// production code, so reordering inside an indexed aggregate can only agree with it if the arithmetic
/// stays inside the documented 1e-10 LU budget.
/// </summary>
public sealed class IntegratedLoudnessGateScalingTests
{
    private const double LufsOffset = -0.691;
    private const double AbsoluteGateLufs = -70;
    private const double RelativeGateLu = -10;
    private const double NumericBudgetLu = 1e-10;
    private static readonly double AbsoluteGatePower = Math.Pow(10, (AbsoluteGateLufs - LufsOffset) / 10);

    [Fact]
    public void VariedDeterministicHistoriesMatchTheTwoPassReferenceAtEveryStep()
    {
        foreach (var seed in new uint[] { 1u, 7u, 2027u, 65535u })
        {
            var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
            var history = new List<double>();

            foreach (var power in VariedPowers(seed, count: 300))
            {
                gate.Add(power);
                history.Add(power);

                AssertWithinBudget(ReferenceLufs(history), gate.CalculateLufs(), $"seed {seed}");
            }
        }
    }

    [Fact]
    public void AscendingDescendingAndShiftedHistoriesMatchTheTwoPassReferenceAtEveryStep()
    {
        var histories = new List<(string Name, double[] Powers)>
        {
            // Geometric sweeps cross both gates without landing on a threshold by construction, which a
            // linear sweep can do by coincidence.
            ("ascending", GeometricPowers(count: 90, first: 1e-9, ratio: 1.2)),
            ("descending", GeometricPowers(count: 90, first: 1e-9, ratio: 1.2).Reverse().ToArray()),
            ("quiet-then-loud", Enumerable.Range(0, 240)
                .Select(index => index < 120 ? 1e-9 : 0.05 + (index * 1e-4))
                .ToArray()),
            ("loud-then-quiet", Enumerable.Range(0, 240)
                .Select(index => index < 120 ? 0.05 + (index * 1e-4) : 1e-9)
                .ToArray()),
            ("duplicate-heavy", Enumerable.Range(0, 240)
                .Select(index => (index % 7) switch
                {
                    0 => 0.5,
                    1 => 0.25,
                    2 => 1e-9,
                    3 => 0.125,
                    4 => 0.0625,
                    5 => 0.03125,
                    _ => 1e-8
                })
                .ToArray())
        };

        foreach (var (name, powers) in histories)
        {
            var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
            var history = new List<double>(powers.Length);
            foreach (var power in powers)
            {
                gate.Add(power);
                history.Add(power);
                AssertWithinBudget(ReferenceLufs(history), gate.CalculateLufs(), name);
            }
        }
    }

    [Fact]
    public void BlocksExactlyAtTheAbsoluteGateAreExcludedAndTheNextRepresentablePowerIsIncluded()
    {
        // A history of equal block powers keeps the sliding-window arithmetic exact, so the strict
        // comparison is tested at the true gate value instead of at a value that drifted off it.
        Assert.Null(Measure(Enumerable.Repeat(AbsoluteGatePower, 5).ToArray()));

        var oneStepBelow = Math.BitDecrement(AbsoluteGatePower);
        Assert.Null(Measure(Enumerable.Repeat(oneStepBelow, 5).ToArray()));

        var oneStepAbove = Math.BitIncrement(AbsoluteGatePower);
        var aboveResult = Measure(Enumerable.Repeat(oneStepAbove, 5).ToArray());
        AssertWithinBudget(ToLufs(oneStepAbove), aboveResult, "one step above the absolute gate");
        AssertWithinBudget(ReferenceLufs(Enumerable.Repeat(oneStepAbove, 5).ToArray()), aboveResult, "reference");

        // The gate value itself must not contribute: with two blocks sitting exactly on the gate, the
        // reading is the loud block alone. Counting the gate blocks would move the reading by many LU.
        var mixed = Measure([AbsoluteGatePower, AbsoluteGatePower, 0.25]);
        AssertWithinBudget(ToLufs(0.25), mixed, "gate blocks must not contribute");
        Assert.True(
            Math.Abs(ToLufs(0.25) - ToLufs((0.25 + (2 * AbsoluteGatePower)) / 3)) > 1e-3,
            "the fixture must separate the strict and inclusive gating results");
    }

    [Fact]
    public void BlocksOnEitherSideOfTheRelativeGateMatchTheTwoPassReference()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
        var history = new List<double>();
        foreach (var power in new[] { 0.4, 0.4, 0.4, 0.4, 0.2, 0.05, 0.0125 })
        {
            gate.Add(power);
            history.Add(power);
        }

        // Every step adds a block just below and just above the relative gate the previous query implied.
        // The offsets are far larger than the sliding-window rounding, so the comparison outcome is
        // determined by the gate formula and not by accumulated floating-point noise.
        var sawRelativeGating = false;
        for (var step = 0; step < 12; step++)
        {
            var relativeGatePower = ReferenceRelativeGatePower(history);
            foreach (var power in new[]
            {
                relativeGatePower * (1 - 1e-6),
                relativeGatePower * (1 + 1e-6),
                relativeGatePower * (1 - 1e-3),
                relativeGatePower * (1 + 1e-3)
            })
            {
                gate.Add(power);
                history.Add(power);
                AssertWithinBudget(ReferenceLufs(history), gate.CalculateLufs(), $"relative gate step {step}");
                var gatedCount = ReferenceGatedCount(history);
                sawRelativeGating |= gatedCount < history.Count;
            }
        }

        // Sanity check that this history really is decided by second-stage gating and not by an
        // ungated mean: at least one query excluded blocks that the absolute gate kept.
        Assert.True(sawRelativeGating, "the fixture must exercise relative gating rather than an ungated mean");
    }

    [Fact]
    public void BlockExactlyAtTheRelativeGateIsExcluded()
    {
        // These two powers sum to exactly 2, giving preliminary mean 1 and a -10 LU relative threshold
        // of exactly 0.1. The quieter block must be excluded, leaving only the known 1.9-power result.
        double[] powers = [0.1, 1.9];
        Assert.Equal(0.1, ReferenceRelativeGatePower(powers));
        Assert.Equal(1, ReferenceGatedCount(powers));

        AssertWithinBudget(2.096536009528289, Measure(powers), "exact relative-threshold equality");
    }

    [Fact]
    public void IndexedThresholdEqualityExcludesEveryDuplicateAtThatPower()
    {
        var index = new PowerAggregateIndex();
        index.Add(0.1);
        index.Add(1.9);
        index.Add(0.1);

        index.SumAbove(0.1, out var sum, out var count);
        Assert.Equal(1, count);
        Assert.Equal(1.9, sum);

        index.SumAbove(Math.BitDecrement(0.1), out sum, out count);
        Assert.Equal(3, count);
        Assert.Equal(2.1, sum, precision: 14);
    }

    [Fact]
    public void ResetDropsTheIndexedHistoryAndBehavesLikeAFreshGate()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
        foreach (var power in VariedPowers(seed: 99u, count: 800))
        {
            gate.Add(power);
        }

        Assert.NotNull(gate.CalculateLufs());

        gate.Reset();

        Assert.Null(gate.CalculateLufs());

        var fresh = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
        var restored = VariedPowers(seed: 1234u, count: 200);
        foreach (var power in restored)
        {
            gate.Add(power);
            fresh.Add(power);
        }

        AssertWithinBudget(fresh.CalculateLufs(), gate.CalculateLufs(), "after reset");
        AssertWithinBudget(ReferenceLufs(restored), gate.CalculateLufs(), "after reset reference");
    }

    [Fact]
    public void RepeatedQueriesBetweenCompletedBlocksReuseTheIndexedAnswer()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 4, hopFrames: 4);
        foreach (var power in VariedPowers(seed: 5u, count: 120))
        {
            gate.Add(power);
            gate.Add(power);
            gate.Add(power);
            gate.Add(power);
        }

        var evaluationsAfterHistory = gate.EvaluationCount;
        var measured = gate.CalculateLufs();
        var evaluationsAfterFirstQuery = gate.EvaluationCount;

        for (var query = 0; query < 10; query++)
        {
            Assert.Equal(measured!.Value, gate.CalculateLufs()!.Value);
        }

        Assert.Equal(evaluationsAfterHistory + 1, evaluationsAfterFirstQuery);
        Assert.Equal(evaluationsAfterFirstQuery, gate.EvaluationCount);
    }

    [Fact]
    public void CompletedBlocksDoNotRescanTheWholeRetainedHistory()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
        var history = new List<double>();

        foreach (var power in VariedPowers(seed: 4242u, count: 500))
        {
            gate.Add(power);
            history.Add(power);
        }

        AssertWithinBudget(ReferenceLufs(history), gate.CalculateLufs(), "first measurement");
        var visitsAt500 = gate.LastEvaluationNodeVisits;
        var evaluatedCount = history.Count;

        foreach (var power in VariedPowers(seed: 4243u, count: 4_000))
        {
            gate.Add(power);
            history.Add(power);
        }

        AssertWithinBudget(ReferenceLufs(history), gate.CalculateLufs(), "long measurement");
        var visitsAt4500 = gate.LastEvaluationNodeVisits;

        // A rescan of the retained history touches at least one entry per block (evaluatedCount then
        // 4500). An exact indexed evaluation touches a logarithmic number of nodes, so the counts stay
        // far below the history size and barely move when the history grows by nine times.
        Assert.True(visitsAt500 > 0, "the evaluation must report the index work it performed");
        Assert.True(
            visitsAt500 < evaluatedCount / 4,
            $"{evaluatedCount} blocks should not need {visitsAt500} node visits");
        Assert.True(
            visitsAt4500 < 4_500 / 4,
            $"4500 blocks should not need {visitsAt4500} node visits");
        Assert.True(
            visitsAt4500 <= visitsAt500 * 4,
            $"index work grew from {visitsAt500} to {visitsAt4500} visits, which is not logarithmic");
    }

    [Fact]
    public void AnalyzerIntegratedLoudnessIsPartitionIndependentForALongVariedSignal()
    {
        const int sampleRate = 48_000;
        var frames = sampleRate * 4;
        var signal = BuildSignal(frames, sampleRate);

        using var singleBlock = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, 2, 4));
        singleBlock.Process(signal);
        var singleBlockLufs = singleBlock.Snapshot.IntegratedLufs;

        using var streamed = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, 2, 4));
        for (var frameOffset = 0; frameOffset < frames; frameOffset += 480)
        {
            var blockFrames = Math.Min(480, frames - frameOffset);
            streamed.Process(signal.AsSpan(frameOffset * 2, blockFrames * 2));
        }

        Assert.NotNull(singleBlockLufs);
        Assert.Equal(singleBlockLufs!.Value, streamed.Snapshot.IntegratedLufs!.Value, precision: 10);

        // A duplicated mono signal has an empty side component. The Mid gate measures the mean of the two
        // channels, i.e. half the stereo power of the overall gate, so it reads 10*log10(2) LU lower; the
        // indexed gate must preserve exactly that per-channel relationship.
        Assert.Equal(
            singleBlockLufs.Value - (10 * Math.Log10(2)),
            streamed.Snapshot.MidMetrics!.IntegratedLufs!.Value,
            precision: 10);
        Assert.Null(streamed.Snapshot.SideMetrics!.IntegratedLufs);
    }

    private static double? Measure(double[] blockPowers)
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 1, hopFrames: 1);
        foreach (var power in blockPowers)
        {
            gate.Add(power);
        }

        return gate.CalculateLufs();
    }

    private static float[] BuildSignal(int frameCount, int sampleRate)
    {
        var samples = new float[frameCount * 2];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var seconds = frame / (double)sampleRate;
            // 2 s of loud programme followed by a 28 dB quieter section, so the relative gate has to
            // exclude the quiet blocks and the Mid/Side gates see a varied history.
            var amplitude = seconds < 2 ? 0.5 : 0.02;
            var value = (float)(Math.Sin(2 * Math.PI * 500 * seconds) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }

    private static double[] GeometricPowers(int count, double first, double ratio)
    {
        var powers = new double[count];
        var value = first;
        for (var index = 0; index < count; index++)
        {
            powers[index] = value;
            value *= ratio;
        }

        return powers;
    }

    private static void AssertWithinBudget(double? expected, double? actual, string context)
    {
        if (expected is null || actual is null)
        {
            Assert.True(
                expected is null && actual is null,
                $"{context}: expected {(expected is null ? "null" : expected.Value.ToString("R"))} " +
                $"but measured {(actual is null ? "null" : actual.Value.ToString("R"))}");
            return;
        }

        var difference = Math.Abs(expected.Value - actual.Value);
        Assert.True(
            difference <= NumericBudgetLu,
            $"{context}: expected {expected.Value:R} LUFS but measured {actual.Value:R} LUFS ({difference:R} LU)");
    }

    /// <summary>
    /// Straightforward two-pass BS.1770 gating over the chronological block list: the first pass applies
    /// the absolute gate, the second applies the absolute and the relative gate. Deliberately naive.
    /// </summary>
    private static double? ReferenceLufs(IReadOnlyList<double> blockPowers)
    {
        var absoluteGated = AbsoluteGatedSumAndCount(blockPowers, out var absoluteCount);
        if (absoluteCount == 0)
        {
            return null;
        }

        var relativeGatePower = ToPower(ToLufs(absoluteGated / absoluteCount) + RelativeGateLu);
        var gatedSum = 0d;
        var gatedCount = 0;
        for (var index = 0; index < blockPowers.Count; index++)
        {
            var power = blockPowers[index];
            if (power <= AbsoluteGatePower || power <= relativeGatePower)
            {
                continue;
            }

            gatedSum += power;
            gatedCount++;
        }

        return gatedCount == 0 ? null : ToLufs(gatedSum / gatedCount);
    }

    private static double ReferenceRelativeGatePower(IReadOnlyList<double> blockPowers)
    {
        var absoluteGated = AbsoluteGatedSumAndCount(blockPowers, out var absoluteCount);
        return absoluteCount == 0
            ? AbsoluteGatePower
            : ToPower(ToLufs(absoluteGated / absoluteCount) + RelativeGateLu);
    }

    private static int ReferenceGatedCount(IReadOnlyList<double> blockPowers)
    {
        var absoluteGated = AbsoluteGatedSumAndCount(blockPowers, out var absoluteCount);
        if (absoluteCount == 0)
        {
            return 0;
        }

        var relativeGatePower = ToPower(ToLufs(absoluteGated / absoluteCount) + RelativeGateLu);
        var gatedCount = 0;
        for (var index = 0; index < blockPowers.Count; index++)
        {
            var power = blockPowers[index];
            if (power <= AbsoluteGatePower || power <= relativeGatePower)
            {
                continue;
            }

            gatedCount++;
        }

        return gatedCount;
    }

    private static double AbsoluteGatedSumAndCount(IReadOnlyList<double> blockPowers, out int count)
    {
        var sum = 0d;
        count = 0;
        for (var index = 0; index < blockPowers.Count; index++)
        {
            if (blockPowers[index] <= AbsoluteGatePower)
            {
                continue;
            }

            sum += blockPowers[index];
            count++;
        }

        return sum;
    }

    private static double ToLufs(double meanSquare)
    {
        return meanSquare <= 0 ? double.NegativeInfinity : LufsOffset + (10 * Math.Log10(meanSquare));
    }

    private static double ToPower(double lufs)
    {
        return Math.Pow(10, (lufs - LufsOffset) / 10);
    }

    /// <summary>
    /// Deterministic mixed history: loud programme material, quiet sections below the absolute gate,
    /// repeated powers, and values on both sides of the absolute gate.
    /// </summary>
    private static double[] VariedPowers(uint seed, int count)
    {
        var powers = new double[count];
        var state = seed == 0 ? 1u : seed;
        for (var index = 0; index < count; index++)
        {
            state = unchecked((state * 1664525u) + 1013904223u);
            var unit = (state >> 8) / (double)(1 << 24);
            var section = (index / 40) % 5;
            powers[index] = section switch
            {
                0 => 0.02 + (unit * 0.4),
                1 => 1e-9 * (1 + unit),
                2 => index % 3 == 0 ? 0.125 : 0.125 + (unit * 1e-3),
                3 => AbsoluteGatePower * (unit < 0.5 ? 0.5 : 1.5),
                _ => 0.005 + (Math.Round(unit, 2) * 0.2)
            };
        }

        return powers;
    }
}
