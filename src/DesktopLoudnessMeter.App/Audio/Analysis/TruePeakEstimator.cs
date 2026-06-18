namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class TruePeakEstimator
{
    private const int KernelRadius = 16;
    private const int BufferLength = (KernelRadius * 2) + 2;
    private readonly int _channels;
    private readonly int _oversampling;
    private readonly int? _windowFrames;
    private readonly double[][] _buffers;
    private readonly double[][] _kernels;
    private readonly int[] _bufferCounts;
    private readonly Queue<double> _recentFramePeaks = [];
    private double _truePeak;
    private long _sampleCount;

    public TruePeakEstimator(int channels, int oversampling, int? windowFrames = null)
    {
        if (channels is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Only mono and stereo PCM are supported.");
        }

        if (oversampling is not (2 or 4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(oversampling), "Oversampling must be 2x, 4x, or 8x.");
        }

        if (windowFrames is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowFrames), "Window frame count must be positive.");
        }

        _channels = channels;
        _oversampling = oversampling;
        _windowFrames = windowFrames;
        _buffers = Enumerable.Range(0, channels)
            .Select(_ => new double[BufferLength])
            .ToArray();
        _kernels = CreateKernels(oversampling);
        _bufferCounts = new int[channels];
    }

    public bool HasSamples => _sampleCount > 0;

    public double TruePeak => _truePeak;

    public void Add(ReadOnlySpan<double> samples)
    {
        Add(samples, Span<double>.Empty);
    }

    public int Add(ReadOnlySpan<double> samples, Span<double> oversampledMonoSamples)
    {
        return Add(samples, oversampledMonoSamples, Span<double>.Empty);
    }

    public int Add(
        ReadOnlySpan<double> samples,
        Span<double> oversampledMonoSamples,
        Span<double> oversampledInterleavedSamples)
    {
        var framePeak = 0d;
        for (var channel = 0; channel < _channels; channel++)
        {
            var sample = samples[channel];
            framePeak = Math.Max(framePeak, Math.Abs(sample));
            var buffer = _buffers[channel];
            buffer[_bufferCounts[channel]++] = sample;
        }

        var oversampledCount = 0;
        if (_bufferCounts[0] == BufferLength)
        {
            oversampledCount = EvaluateBufferedSegment(
                framePeak,
                oversampledMonoSamples,
                oversampledInterleavedSamples,
                out framePeak);
            for (var channel = 0; channel < _channels; channel++)
            {
                var buffer = _buffers[channel];
                Array.Copy(buffer, 1, buffer, 0, BufferLength - 1);
                _bufferCounts[channel] = BufferLength - 1;
            }
        }

        UpdatePeak(framePeak);
        _sampleCount++;
        return oversampledCount;
    }

    public void Reset()
    {
        for (var index = 0; index < _buffers.Length; index++)
        {
            Array.Clear(_buffers[index]);
            _bufferCounts[index] = 0;
        }

        _recentFramePeaks.Clear();
        _truePeak = 0;
        _sampleCount = 0;
    }

    private int EvaluateBufferedSegment(
        double rawFramePeak,
        Span<double> oversampledMonoSamples,
        Span<double> oversampledInterleavedSamples,
        out double peak)
    {
        peak = rawFramePeak;
        var writableCount = Math.Min(_oversampling, oversampledMonoSamples.Length);
        var writableChannelFrameCount = Math.Min(_oversampling, oversampledInterleavedSamples.Length / _channels);
        for (var step = 0; step < _oversampling; step++)
        {
            var mono = 0d;
            for (var channel = 0; channel < _channels; channel++)
            {
                var reconstructed = Reconstruct(_buffers[channel], _kernels[step]);
                mono += reconstructed;
                if (step < writableChannelFrameCount)
                {
                    oversampledInterleavedSamples[(step * _channels) + channel] = reconstructed;
                }

                if (step > 0)
                {
                    peak = Math.Max(peak, Math.Abs(reconstructed));
                }
            }

            if (step < writableCount)
            {
                oversampledMonoSamples[step] = mono / _channels;
            }
        }

        return writableCount;
    }

    private static double Reconstruct(double[] samples, double[] kernel)
    {
        var reconstructed = 0d;
        for (var index = 0; index < samples.Length; index++)
        {
            reconstructed += samples[index] * kernel[index];
        }

        return reconstructed;
    }

    private static double[][] CreateKernels(int oversampling)
    {
        var kernels = new double[oversampling][];
        for (var step = 0; step < oversampling; step++)
        {
            var position = KernelRadius + ((double)step / oversampling);
            var kernel = new double[BufferLength];
            for (var index = 0; index < kernel.Length; index++)
            {
                var distance = position - index;
                kernel[index] = Math.Abs(distance) > KernelRadius
                    ? 0
                    : Sinc(distance) * Hann(distance);
            }

            kernels[step] = kernel;
        }

        return kernels;
    }

    private void UpdatePeak(double amplitude)
    {
        if (_windowFrames is not int windowFrames)
        {
            if (amplitude > _truePeak)
            {
                _truePeak = amplitude;
            }

            return;
        }

        _recentFramePeaks.Enqueue(amplitude);
        var removedPeak = 0d;
        while (_recentFramePeaks.Count > windowFrames)
        {
            removedPeak = Math.Max(removedPeak, _recentFramePeaks.Dequeue());
        }

        if (amplitude >= _truePeak)
        {
            _truePeak = amplitude;
        }
        else if (removedPeak >= _truePeak)
        {
            _truePeak = _recentFramePeaks.Count == 0 ? 0 : _recentFramePeaks.Max();
        }
    }

    private static double Sinc(double value)
    {
        if (Math.Abs(value) < 1e-12)
        {
            return 1;
        }

        var angle = Math.PI * value;
        return Math.Sin(angle) / angle;
    }

    private static double Hann(double distance)
    {
        return 0.5 + (0.5 * Math.Cos(Math.PI * distance / KernelRadius));
    }
}
