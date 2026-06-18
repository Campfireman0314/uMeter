namespace DesktopLoudnessMeter.SenderProbe;

public static class SineWaveGenerator
{
    public static float[] Generate(
        int frames,
        int channels,
        int sampleRate,
        double frequencyHz,
        float amplitude,
        ref double phase)
    {
        if (frames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frames), "Frame count must be positive.");
        }

        if (channels is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be 1 or 2.");
        }

        var samples = new float[checked(frames * channels)];
        var phaseStep = (Math.PI * 2 * frequencyHz) / sampleRate;
        for (var frame = 0; frame < frames; frame++)
        {
            var sample = (float)(Math.Sin(phase) * amplitude);
            for (var channel = 0; channel < channels; channel++)
            {
                samples[(frame * channels) + channel] = sample;
            }

            phase += phaseStep;
            if (phase >= Math.PI * 2)
            {
                phase -= Math.PI * 2;
            }
        }

        return samples;
    }
}
