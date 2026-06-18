using System.Buffers.Binary;
using NAudio.Wave;

namespace DesktopLoudnessMeter.App.Audio;

public static class WaveFormatSampleConverter
{
    public static float[] ToInterleavedFloatSamples(ReadOnlySpan<byte> bytes, WaveFormat waveFormat, int frameCount)
    {
        if (frameCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count cannot be negative.");
        }

        var sampleCount = checked(frameCount * waveFormat.Channels);
        var samples = new float[sampleCount];
        if (sampleCount == 0)
        {
            return samples;
        }

        if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat && waveFormat.BitsPerSample == 32)
        {
            ConvertIeeeFloat32(bytes, samples);
            return samples;
        }

        if (waveFormat.Encoding == WaveFormatEncoding.Pcm && waveFormat.BitsPerSample == 16)
        {
            ConvertPcm16(bytes, samples);
            return samples;
        }

        throw new NotSupportedException($"Unsupported capture format: {waveFormat.Encoding}, {waveFormat.BitsPerSample} bit.");
    }

    private static void ConvertIeeeFloat32(ReadOnlySpan<byte> bytes, Span<float> samples)
    {
        var requiredBytes = samples.Length * sizeof(float);
        if (bytes.Length < requiredBytes)
        {
            throw new ArgumentException("Buffer is smaller than the requested frame count.", nameof(bytes));
        }

        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = BitConverter.ToSingle(bytes[(index * sizeof(float))..]);
        }
    }

    private static void ConvertPcm16(ReadOnlySpan<byte> bytes, Span<float> samples)
    {
        var requiredBytes = samples.Length * sizeof(short);
        if (bytes.Length < requiredBytes)
        {
            throw new ArgumentException("Buffer is smaller than the requested frame count.", nameof(bytes));
        }

        for (var index = 0; index < samples.Length; index++)
        {
            var value = BinaryPrimitives.ReadInt16LittleEndian(bytes[(index * sizeof(short))..]);
            samples[index] = value < 0
                ? value / 32768f
                : value / 32767f;
        }
    }
}
