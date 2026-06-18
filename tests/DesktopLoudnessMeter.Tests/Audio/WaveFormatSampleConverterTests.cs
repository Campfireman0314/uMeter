using DesktopLoudnessMeter.App.Audio;
using NAudio.Wave;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class WaveFormatSampleConverterTests
{
    [Fact]
    public void ConvertsIeeeFloat32Buffer()
    {
        var bytes = new byte[sizeof(float) * 4];
        Buffer.BlockCopy(new[] { -1f, -0.25f, 0.25f, 1f }, 0, bytes, 0, bytes.Length);

        var samples = WaveFormatSampleConverter.ToInterleavedFloatSamples(
            bytes,
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2),
            frameCount: 2);

        Assert.Equal([-1f, -0.25f, 0.25f, 1f], samples);
    }

    [Fact]
    public void ConvertsPcm16Buffer()
    {
        var source = new short[] { short.MinValue, -16384, 16384, short.MaxValue };
        var bytes = new byte[source.Length * sizeof(short)];
        Buffer.BlockCopy(source, 0, bytes, 0, bytes.Length);

        var samples = WaveFormatSampleConverter.ToInterleavedFloatSamples(
            bytes,
            new WaveFormat(48000, 16, 2),
            frameCount: 2);

        Assert.Equal(-1f, samples[0], precision: 4);
        Assert.Equal(-0.5f, samples[1], precision: 4);
        Assert.Equal(0.5f, samples[2], precision: 4);
        Assert.InRange(samples[3], 0.999f, 1f);
    }
}
