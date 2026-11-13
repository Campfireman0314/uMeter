using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// Independent numerical anchors for rate-correct K-weighted loudness.
///
/// Expected values never come from the analyzer or from the production filter:
///
/// 1. <see cref="OneKilohertzStereoReferenceToneReadsMinusTwentyThreeLufs"/> uses the
///    published EBU Tech 3341 reference case: a stereo 1 kHz sine at -23.0 dBFS must
///    read -23.0 LUFS within 0.1 LU. BS.1770 calibration is defined at 1 kHz, so the
///    same published level applies at every supported sample rate.
/// 2. <see cref="SteadyStateMomentaryLufsMatchesIndependentKWeightedReference"/> uses
///    precomputed literals produced offline by a NumPy implementation written from the
///    ITU-R BS.1770-4 analog prototype parameters (high shelf f0 = 1681.974450955533 Hz,
///    G = 3.999843853973347 dB, Q = 0.7071752369554196; RLB high-pass
///    f0 = 38.13547087602444 Hz, Q = 0.5003270373238773) with the prewarped bilinear
///    transform. That implementation evaluates the digital magnitude response on the unit
///    circle and applies the steady-state stereo relation
///    LUFS = -0.691 + 10 * log10(|H(f)|^2 * A^2) for a sine of peak amplitude A duplicated
///    to both channels. Running the same design at 48 kHz reproduces the published
///    BS.1770-4 48 kHz coefficient set, which anchors the derivation to a published source
///    (see <see cref="DerivedFortyEightKilohertzCoefficientsPreservePublishedSet"/>).
///
/// Tolerance: 0.1 LU on level anchors. Legitimate deviation at and above 100 Hz comes from
/// the finite 400 ms observation window (single-tone mean-square ripple of roughly
/// 1 / (2 * pi * f * T), i.e. about 0.017 LU at 100 Hz) plus float32 input rounding; the
/// measured deviation is included in failure messages.
///
/// This is not an ITU or EBU certification claim: the checks cover synthesised steady-state
/// tones, not the full official compliance test suite.
/// </summary>
public sealed class KWeightingRateResponseTests
{
    private const double LevelToleranceLu = 0.1;
    private const double ObservationSeconds = 2.0;

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void OneKilohertzStereoReferenceToneReadsMinusTwentyThreeLufs(int sampleRate)
    {
        var analyzer = Analyzer(sampleRate);
        var referenceTone = Sine(sampleRate, ObservationSeconds, amplitude: Math.Pow(10, -23.0 / 20), frequencyHz: 1000);

        analyzer.Process(referenceTone);

        AssertWithinLufs(-23.0, analyzer.Snapshot.MomentaryLufs, $"{sampleRate} Hz momentary");
        AssertWithinLufs(-23.0, analyzer.Snapshot.IntegratedLufs, $"{sampleRate} Hz integrated");
    }

    [Theory]
    [InlineData(44100, 100, -7.841271)]
    [InlineData(44100, 1000, -6.011138)]
    [InlineData(44100, 2000, -3.635606)]
    [InlineData(44100, 10000, -2.665750)]
    [InlineData(48000, 100, -7.845098)]
    [InlineData(48000, 1000, -6.013896)]
    [InlineData(48000, 2000, -3.640343)]
    [InlineData(48000, 10000, -2.669718)]
    [InlineData(96000, 100, -7.866742)]
    [InlineData(96000, 1000, -6.031199)]
    [InlineData(96000, 2000, -3.665666)]
    [InlineData(96000, 10000, -2.692063)]
    public void SteadyStateMomentaryLufsMatchesIndependentKWeightedReference(
        int sampleRate,
        int frequencyHz,
        double expectedLufs)
    {
        var analyzer = Analyzer(sampleRate);

        analyzer.Process(Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: frequencyHz));

        AssertWithinLufs(
            expectedLufs,
            analyzer.Snapshot.MomentaryLufs,
            $"{sampleRate} Hz @ {frequencyHz} Hz momentary");
    }

    [Fact]
    public void PartitionedProcessingMatchesSingleBlock()
    {
        const int sampleRate = 96000;
        const int framesPerChunk = 37;
        var signal = Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 1000);

        var singleBlock = Analyzer(sampleRate);
        singleBlock.Process(signal);

        var partitioned = Analyzer(sampleRate);
        var frames = signal.Length / 2;
        for (var startFrame = 0; startFrame < frames; startFrame += framesPerChunk)
        {
            var chunkFrames = Math.Min(framesPerChunk, frames - startFrame);
            partitioned.Process(signal.AsSpan(startFrame * 2, chunkFrames * 2));
        }

        Assert.Equal(-6.031199, singleBlock.Snapshot.MomentaryLufs!.Value, LevelToleranceLu);
        Assert.Equal(-6.031199, partitioned.Snapshot.MomentaryLufs!.Value, LevelToleranceLu);
        Assert.Equal(
            singleBlock.Snapshot.MomentaryLufs!.Value,
            partitioned.Snapshot.MomentaryLufs!.Value,
            1e-6);
    }

    [Fact]
    public void PartitionedProcessingMatchesSingleBlockMeasuredLevel()
    {
        const int sampleRate = 44100;
        var signal = Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 2000);

        var partitioned = Analyzer(sampleRate);
        var frames = signal.Length / 2;
        for (var startFrame = 0; startFrame < frames; startFrame += 512)
        {
            var chunkFrames = Math.Min(512, frames - startFrame);
            partitioned.Process(signal.AsSpan(startFrame * 2, chunkFrames * 2));
        }

        AssertWithinLufs(-3.635606, partitioned.Snapshot.MomentaryLufs, "44100 Hz @ 2000 Hz partitioned");
    }

    [Fact]
    public void ResetRestoresRateCorrectMomentaryReading()
    {
        const int sampleRate = 44100;
        var analyzer = Analyzer(sampleRate);
        var signal = Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 1000);

        analyzer.Process(signal);
        var firstReading = analyzer.Snapshot.MomentaryLufs!.Value;

        analyzer.Reset();
        Assert.Null(analyzer.Snapshot.MomentaryLufs);
        Assert.Null(analyzer.Snapshot.IntegratedLufs);

        analyzer.Process(signal);

        AssertWithinLufs(-6.011138, analyzer.Snapshot.MomentaryLufs, "44100 Hz @ 1000 Hz after reset");
        Assert.Equal(firstReading, analyzer.Snapshot.MomentaryLufs!.Value, 1e-9);
    }

    /// <summary>
    /// Worked example that ties the rate-dependent design to a published source: the
    /// ITU-R BS.1770-4 48 kHz coefficient set. This is checked directly because the public
    /// analyzer seam cannot expose individual biquad coefficients.
    /// </summary>
    [Fact]
    public void DerivedFortyEightKilohertzCoefficientsPreservePublishedSet()
    {
        var filter = new KWeightingFilter(48000);
        var shelf = filter.PreFilterCoefficients;
        var highPass = filter.HighPassFilterCoefficients;

        Assert.Equal(1.53512485958697, shelf.B0, 1e-12);
        Assert.Equal(-2.69169618940638, shelf.B1, 1e-12);
        Assert.Equal(1.19839281085285, shelf.B2, 1e-12);
        Assert.Equal(-1.69065929318241, shelf.A1, 1e-12);
        Assert.Equal(0.73248077421585, shelf.A2, 1e-12);

        Assert.Equal(1.0, highPass.B0, 1e-12);
        Assert.Equal(-2.0, highPass.B1, 1e-12);
        Assert.Equal(1.0, highPass.B2, 1e-12);
        Assert.Equal(-1.99004745483398, highPass.A1, 1e-12);
        Assert.Equal(0.99007225036621, highPass.A2, 1e-12);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(96000)]
    public void DerivedNonFortyEightKilohertzCoefficientsStayStable(int sampleRate)
    {
        var filter = new KWeightingFilter(sampleRate);

        foreach (var coefficients in new[] { filter.PreFilterCoefficients, filter.HighPassFilterCoefficients })
        {
            // Poles of a canonic biquad: z^2 + a1 z + a2 == 0 must sit inside the unit circle.
            var discriminant = (coefficients.A1 * coefficients.A1) - (4 * coefficients.A2);
            Assert.True(discriminant <= 0, $"unexpected real poles at {sampleRate} Hz");
            Assert.InRange(coefficients.A2, 0, 1);
        }
    }

    /// <summary>
    /// Channel semantics at a non-48 kHz rate. These relations are analytic, not measured from
    /// the analyzer: BS.1770 sums the weighted channel powers, the mid/side split keeps the
    /// side channel at full sine amplitude, and a mono stream contributes one channel instead
    /// of two, so each case differs from the duplicated-stereo anchor only by the exact
    /// channel-count factor 10 * log10(2).
    /// </summary>
    [Fact]
    public void ChannelSemanticsMatchAnalyticChannelCountFactorAtNinetySixKilohertz()
    {
        const int sampleRate = 96000;
        const double duplicatedStereoAnchor = -6.031199;
        var singleChannelFactor = 10 * Math.Log10(2);

        var duplicated = Analyzer(sampleRate);
        duplicated.Process(Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 1000));

        var antiPhase = Analyzer(sampleRate);
        antiPhase.Process(Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 1000, invertRight: true));

        var mono = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, 1, 4));
        mono.Process(ToMono(Sine(sampleRate, ObservationSeconds, amplitude: 0.5, frequencyHz: 1000)));

        AssertWithinLufs(duplicatedStereoAnchor, duplicated.Snapshot.MomentaryLufs, "96 kHz duplicated stereo");
        AssertWithinLufs(duplicatedStereoAnchor, antiPhase.Snapshot.MomentaryLufs, "96 kHz anti-phase overall");
        AssertWithinLufs(
            duplicatedStereoAnchor - singleChannelFactor,
            antiPhase.Snapshot.SideMetrics!.MomentaryLufs,
            "96 kHz anti-phase side");
        Assert.Null(antiPhase.Snapshot.MidMetrics!.MomentaryLufs);
        AssertWithinLufs(
            duplicatedStereoAnchor - singleChannelFactor,
            mono.Snapshot.MomentaryLufs,
            "96 kHz mono");
    }

    private static PcmMetricAnalyzer Analyzer(int sampleRate)
    {
        return new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, 2, 4));
    }

    private static void AssertWithinLufs(double expected, double? actual, string context)
    {
        Assert.NotNull(actual);
        var value = actual.Value;
        Assert.True(
            Math.Abs(value - expected) <= LevelToleranceLu,
            $"{context}: expected {expected:0.000} LUFS +/- {LevelToleranceLu} LU but measured {value:0.000} LUFS (delta {value - expected:+0.000;-0.000} LU).");
    }

    private static float[] Sine(
        int sampleRate,
        double seconds,
        double amplitude,
        double frequencyHz,
        int startFrame = 0,
        bool invertRight = false)
    {
        var frames = (int)Math.Round(seconds * sampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * frequencyHz * (startFrame + frame) / sampleRate) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = invertRight ? -value : value;
        }

        return samples;
    }

    private static float[] ToMono(float[] interleavedStereo)
    {
        var mono = new float[interleavedStereo.Length / 2];
        for (var frame = 0; frame < mono.Length; frame++)
        {
            mono[frame] = interleavedStereo[frame * 2];
        }

        return mono;
    }
}
