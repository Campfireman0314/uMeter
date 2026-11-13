using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// Integrated loudness must stay exact (BS.1770 two-stage gating over every completed block) while
/// repeated queries that see no new completed block reuse the previous answer. The gate seam is used
/// here because the public analyzer cannot isolate a single completed block boundary.
/// </summary>
public sealed class IntegratedLoudnessCacheTests
{
    // Worked examples of the BS.1770 formula -0.691 + 10*log10(power) for a single surviving block.
    private const double HalfPowerLufs = -3.701300;
    private const double QuarterPowerLufs = -6.711600;

    [Fact]
    public void RepeatedQueriesReuseTheCachedResultUntilANewBlockCompletes()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 4, hopFrames: 4);

        AddFrames(gate, power: 0.5, frames: 4);

        var first = gate.CalculateLufs();
        Assert.Equal(HalfPowerLufs, first!.Value, precision: 6);
        Assert.Equal(1, gate.EvaluationCount);

        // An incomplete hop does not complete a new gate block, so the answer is reused verbatim.
        AddFrames(gate, power: 0.5, frames: 1);
        var reused = gate.CalculateLufs();

        Assert.Equal(first.Value, reused!.Value, precision: 12);
        Assert.Equal(1, gate.EvaluationCount);

        // The remaining frames of the hop complete a new block, which invalidates the cached result.
        AddFrames(gate, power: 0.5, frames: 3);
        var afterNewBlock = gate.CalculateLufs();

        Assert.Equal(2, gate.EvaluationCount);
        Assert.Equal(first.Value, afterNewBlock!.Value, precision: 12);
    }

    [Fact]
    public void NewBlocksKeepTheTwoStageGateExactInsteadOfAccumulating()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 4, hopFrames: 4);

        AddFrames(gate, power: 0.5, frames: 4);
        AddFrames(gate, power: 0.0005, frames: 4);
        AddFrames(gate, power: 0.0005, frames: 4);

        // The two quiet blocks sit more than 10 LU below the gated mean, so they are excluded and the
        // reading stays exactly at the loud block instead of drifting toward the average of all blocks.
        var gated = gate.CalculateLufs()!.Value;

        Assert.Equal(HalfPowerLufs, gated, precision: 6);
        Assert.Equal(1, gate.EvaluationCount);
        Assert.Equal(gated, gate.CalculateLufs()!.Value, precision: 12);
        Assert.Equal(1, gate.EvaluationCount);
    }

    [Fact]
    public void ResetInvalidatesACachedValueAndCachesTheUnavailableResult()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 4, hopFrames: 4);

        AddFrames(gate, power: 0.5, frames: 4);
        Assert.Equal(HalfPowerLufs, gate.CalculateLufs()!.Value, precision: 6);
        Assert.Equal(1, gate.EvaluationCount);

        gate.Reset();

        // The post-reset answer is unavailable, and that unavailable answer is what gets cached.
        Assert.Null(gate.CalculateLufs());
        Assert.Equal(2, gate.EvaluationCount);
        Assert.Null(gate.CalculateLufs());
        Assert.Equal(2, gate.EvaluationCount);

        // New input after reset must invalidate the cached "unavailable" answer.
        AddFrames(gate, power: 0.25, frames: 4);
        var restored = gate.CalculateLufs();

        Assert.Equal(3, gate.EvaluationCount);
        Assert.Equal(QuarterPowerLufs, restored!.Value, precision: 6);
    }

    [Fact]
    public void BlocksBelowTheAbsoluteGateStayUnavailableAcrossRepeatedQueries()
    {
        var gate = new IntegratedLoudnessGate(blockFrames: 4, hopFrames: 4);

        // 1e-9 power is far below the -70 LUFS absolute gate, so nothing survives gating.
        AddFrames(gate, power: 1e-9, frames: 4);

        Assert.Null(gate.CalculateLufs());
        Assert.Null(gate.CalculateLufs());
        Assert.Equal(1, gate.EvaluationCount);
    }

    [Fact]
    public void AnalyzerIntegratedLoudnessIsExactBetweenGateBlockCompletions()
    {
        const int sampleRate = 48000;
        using var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, 2, 4));

        // 2 s is an exact multiple of the 100 ms integrated gate hop, so no hop is left partially open.
        analyzer.Process(SteadySine(sampleRate, seconds: 2, amplitude: 0.5, startFrame: 0));
        var established = Assert.IsType<double>(analyzer.Snapshot.IntegratedLufs);

        // Four 960-frame blocks stay inside the 100 ms (4800 frame) hop, so no gate block completes.
        var shortBlock = SteadySine(sampleRate, seconds: 0.02, amplitude: 0.5, startFrame: 2 * sampleRate);
        for (var query = 0; query < 4; query++)
        {
            analyzer.Process(shortBlock);
            Assert.Equal(established, analyzer.Snapshot.IntegratedLufs!.Value, precision: 12);
        }

        // A louder second of audio completes new blocks, so the reading must move immediately.
        analyzer.Process(SteadySine(sampleRate, seconds: 1, amplitude: 0.9, startFrame: 2 * sampleRate));
        Assert.True(
            analyzer.Snapshot.IntegratedLufs!.Value > established,
            $"expected integrated loudness to rise above {established:0.0000} LUFS but measured {analyzer.Snapshot.IntegratedLufs:0.0000} LUFS");
    }

    private static void AddFrames(IntegratedLoudnessGate gate, double power, int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            gate.Add(power);
        }
    }

    private static float[] SteadySine(int sampleRate, double seconds, double amplitude, int startFrame)
    {
        var frames = (int)Math.Round(seconds * sampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * 1000 * (startFrame + frame) / sampleRate) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }
}
