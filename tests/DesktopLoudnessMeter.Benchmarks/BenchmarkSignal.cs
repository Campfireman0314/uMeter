namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Deterministic multi-tone stereo signal shared by every scenario so the before/after runs feed
/// byte-identical input into the analyzer.
/// </summary>
internal static class BenchmarkSignal
{
    public static float[] CreateStereoSignal(int frameCount, int sampleRate, int channels)
    {
        var samples = new float[frameCount * channels];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var t = (double)frame / sampleRate;
            var left =
                (0.58 * Math.Sin((2 * Math.PI * 997 * t) + 0.2)) +
                (0.24 * Math.Sin((2 * Math.PI * 6173 * t) + 1.1)) +
                (0.09 * Math.Sin((2 * Math.PI * 13007 * t) + 0.5));
            var right =
                (0.52 * Math.Sin((2 * Math.PI * 1009 * t) + 0.7)) +
                (0.28 * Math.Sin((2 * Math.PI * 5303 * t) + 1.8)) +
                (0.11 * Math.Sin((2 * Math.PI * 11941 * t) + 0.1));

            samples[frame * channels] = (float)Math.Clamp(left, -0.98, 0.98);
            if (channels > 1)
            {
                samples[(frame * channels) + 1] = (float)Math.Clamp(right, -0.98, 0.98);
            }
        }

        return samples;
    }
}
