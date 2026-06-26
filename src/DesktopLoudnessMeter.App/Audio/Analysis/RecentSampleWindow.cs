using System.Collections;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class RecentSampleWindow
{
    private const int MaximumStereoImagePoints = 160;
    private const int MaximumWaveformPoints = 960;
    private const double SpectrumDisplayFloorDb = -200;
    private const double SpectrumDisplayRangeDb = 200;
    private static readonly double RootTwo = Math.Sqrt(2);
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly int _displaySampleRate;
    private readonly int _capacityFrames;
    private readonly Queue<FrameMetrics> _frames;
    private readonly double[] _spectrumSamples;
    private readonly double[][] _channelSpectrumSamples;
    private readonly double[] _displaySamples;
    private readonly double[][] _channelDisplaySamples;
    private readonly SampleBuffer[] _waveformBuffers = [new(MaximumWaveformPoints), new(MaximumWaveformPoints)];
    private readonly SampleBuffer[][] _channelWaveformBuffers;
    private readonly SampleBuffer[] _spectrumBuffers = [new(SpectrumScale.BucketCount), new(SpectrumScale.BucketCount)];
    private readonly SampleBuffer[][] _channelSpectrumBuffers;
    private readonly ISpectrumFft _spectrumFft;
    private int _spectrumStartIndex;
    private int _spectrumSampleCount;
    private int _displayStartIndex;
    private int _displaySampleCount;
    private long _displayVersion;
    private long _displayFrameVersion;
    private long _waveformVersion = -1;
    private long _channelWaveformVersion = -1;
    private long _spectrumVersion = -1;
    private long _channelSpectrumVersion = -1;
    private long _spectrumEligibleFrameVersion = -1;
    private long _channelSpectrumEligibleFrameVersion = -1;
    private int _waveformBufferIndex;
    private int _channelWaveformBufferIndex;
    private int _spectrumBufferIndex;
    private int _channelSpectrumBufferIndex;
    private SampleBuffer? _waveformCache;
    private IReadOnlyList<IReadOnlyList<double>>? _channelWaveformCache;
    private SampleBuffer? _spectrumCache;
    private IReadOnlyList<IReadOnlyList<double>>? _channelSpectrumCache;
    private double[] _fftInput = [];
    private double[] _fftMagnitudes = [];
    private double[] _fftWindow = [];
    private double _fftWindowSum;
    private PcmAnalysisPerformanceSettings _performanceSettings = PcmAnalysisPerformanceSettings.Normal;
    private double _sampleSquareSum;
    private double _leftSquareSum;
    private double _rightSquareSum;
    private double _crossSum;
    private double _midSquareSum;
    private double _sideSquareSum;

    public RecentSampleWindow(int channels, int sampleRate, int oversampling, int capacityFrames)
        : this(channels, sampleRate, oversampling, capacityFrames, SpectrumFftProvider.Create())
    {
    }

    internal RecentSampleWindow(
        int channels,
        int sampleRate,
        int oversampling,
        int capacityFrames,
        ISpectrumFft spectrumFft)
    {
        if (channels is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Only mono and stereo PCM are supported.");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        }

        if (oversampling is not (2 or 4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(oversampling), "Oversampling must be 2x, 4x, or 8x.");
        }

        if (capacityFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityFrames), "Window capacity must be positive.");
        }

        _channels = channels;
        _sampleRate = sampleRate;
        _displaySampleRate = sampleRate * oversampling;
        _capacityFrames = capacityFrames;
        _spectrumFft = spectrumFft;
        _frames = new Queue<FrameMetrics>(capacityFrames);
        _spectrumSamples = new double[capacityFrames];
        _channelSpectrumSamples = Enumerable.Range(0, channels)
            .Select(_ => new double[capacityFrames])
            .ToArray();
        _displaySamples = new double[capacityFrames * oversampling];
        _channelDisplaySamples = Enumerable.Range(0, channels)
            .Select(_ => new double[capacityFrames * oversampling])
            .ToArray();
        _channelWaveformBuffers = Enumerable.Range(0, channels)
            .Select(_ => new[] { new SampleBuffer(MaximumWaveformPoints), new SampleBuffer(MaximumWaveformPoints) })
            .ToArray();
        _channelSpectrumBuffers = Enumerable.Range(0, channels)
            .Select(_ => new[] { new SampleBuffer(SpectrumScale.BucketCount), new SampleBuffer(SpectrumScale.BucketCount) })
            .ToArray();
    }

    public bool IsFull => _frames.Count == _capacityFrames;

    public double Rms
    {
        get
        {
            var sampleCount = _frames.Count * _channels;
            return sampleCount == 0 ? 0 : Math.Sqrt(_sampleSquareSum / sampleCount);
        }
    }

    public double? Correlation
    {
        get
        {
            if (!IsFull || _channels < 2)
            {
                return null;
            }

            var denominator = Math.Sqrt(_leftSquareSum * _rightSquareSum);
            return denominator <= double.Epsilon
                ? 0
                : Math.Clamp(_crossSum / denominator, -1, 1);
        }
    }

    public double? StereoImageX
    {
        get
        {
            if (!IsFull)
            {
                return null;
            }

            var leftRms = Math.Sqrt(_leftSquareSum / _frames.Count);
            var rightRms = Math.Sqrt(_rightSquareSum / _frames.Count);
            var sum = leftRms + rightRms;
            return sum <= double.Epsilon ? 0 : Math.Clamp((rightRms - leftRms) / sum, -1, 1);
        }
    }

    public double? StereoImageY
    {
        get
        {
            if (!IsFull)
            {
                return null;
            }

            var midRms = Math.Sqrt(_midSquareSum / _frames.Count);
            var sideRms = Math.Sqrt(_sideSquareSum / _frames.Count);
            var sum = midRms + sideRms;
            return sum <= double.Epsilon ? 0 : Math.Clamp((midRms - sideRms) / sum, -1, 1);
        }
    }

    public IReadOnlyList<StereoImagePoint>? StereoImagePoints
    {
        get
        {
            if (!IsFull || _channels < 2)
            {
                return null;
            }

            var stride = Math.Max(1, _frames.Count / MaximumStereoImagePoints);
            var points = new List<StereoImagePoint>(Math.Min(MaximumStereoImagePoints, _frames.Count));
            var index = 0;
            foreach (var frame in _frames)
            {
                if (index++ % stride == 0)
                {
                    points.Add(new StereoImagePoint(frame.PolarAngle, frame.PolarRadius));
                }
            }

            return points;
        }
    }

    public IReadOnlyList<double>? WaveformSamples
    {
        get
        {
            if (_displaySampleCount == 0)
            {
                return null;
            }

            if (_waveformVersion == _displayVersion)
            {
                return _waveformCache;
            }

            _waveformBufferIndex ^= 1;
            var buffer = _waveformBuffers[_waveformBufferIndex];
            FillWaveformBuffer(buffer);
            _waveformCache = buffer;
            _waveformVersion = _displayVersion;
            return buffer;
        }
    }

    public IReadOnlyList<IReadOnlyList<double>>? ChannelWaveformSamples
    {
        get
        {
            if (_displaySampleCount == 0)
            {
                return null;
            }

            if (_channelWaveformVersion == _displayVersion)
            {
                return _channelWaveformCache;
            }

            _channelWaveformBufferIndex ^= 1;
            var buffers = new IReadOnlyList<double>[_channels];
            for (var channel = 0; channel < _channels; channel++)
            {
                var buffer = _channelWaveformBuffers[channel][_channelWaveformBufferIndex];
                FillWaveformBuffer(buffer, channel);
                buffers[channel] = buffer;
            }

            _channelWaveformCache = buffers;
            _channelWaveformVersion = _displayVersion;
            return buffers;
        }
    }

    public IReadOnlyList<double>? SpectrumBuckets
    {
        get
        {
            if (_displaySampleCount < 128)
            {
                return null;
            }

            if (_spectrumVersion == _displayVersion)
            {
                return _spectrumCache;
            }

            if (!ShouldRefreshSpectrum(ref _spectrumEligibleFrameVersion, _spectrumCache is not null))
            {
                return _spectrumCache;
            }

            _spectrumBufferIndex ^= 1;
            var buffer = _spectrumBuffers[_spectrumBufferIndex];
            FillSpectrumBuffer(buffer);
            _spectrumCache = buffer;
            _spectrumVersion = _displayVersion;
            return buffer;
        }
    }

    public IReadOnlyList<IReadOnlyList<double>>? ChannelSpectrumBuckets
    {
        get
        {
            if (_displaySampleCount < 128)
            {
                return null;
            }

            if (_channelSpectrumVersion == _displayVersion)
            {
                return _channelSpectrumCache;
            }

            if (!ShouldRefreshSpectrum(ref _channelSpectrumEligibleFrameVersion, _channelSpectrumCache is not null))
            {
                return _channelSpectrumCache;
            }

            _channelSpectrumBufferIndex ^= 1;
            var buffers = new IReadOnlyList<double>[_channels];
            for (var channel = 0; channel < _channels; channel++)
            {
                var buffer = _channelSpectrumBuffers[channel][_channelSpectrumBufferIndex];
                FillSpectrumBuffer(buffer, channel);
                buffers[channel] = buffer;
            }

            _channelSpectrumCache = buffers;
            _channelSpectrumVersion = _displayVersion;
            return buffers;
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        var settings = mode == PcmAnalysisPerformanceMode.Eco
            ? PcmAnalysisPerformanceSettings.Eco
            : PcmAnalysisPerformanceSettings.Normal;

        if (_performanceSettings == settings)
        {
            return;
        }

        _performanceSettings = settings;
        _spectrumVersion = -1;
        _channelSpectrumVersion = -1;
        _spectrumEligibleFrameVersion = -1;
        _channelSpectrumEligibleFrameVersion = -1;
    }

    public void Add(ReadOnlySpan<double> samples)
    {
        Add(samples, ReadOnlySpan<double>.Empty);
    }

    public void Add(ReadOnlySpan<double> samples, ReadOnlySpan<double> oversampledMonoSamples)
    {
        Add(samples, oversampledMonoSamples, ReadOnlySpan<double>.Empty);
    }

    public void Add(
        ReadOnlySpan<double> samples,
        ReadOnlySpan<double> oversampledMonoSamples,
        ReadOnlySpan<double> oversampledInterleavedSamples)
    {
        var left = samples[0];
        var right = _channels == 1 ? samples[0] : samples[1];
        var mid = (left + right) * 0.5;
        var side = (left - right) * 0.5;
        var leftMagnitude = Math.Abs(left);
        var rightMagnitude = Math.Abs(right);
        var magnitudeSum = leftMagnitude + rightMagnitude;
        var polarAngle = _channels == 1 || magnitudeSum <= double.Epsilon
            ? 0
            : Math.Clamp((rightMagnitude - leftMagnitude) / magnitudeSum, -1, 1);
        var polarRadius = Math.Clamp(Math.Sqrt(((left * left) + (right * right)) / 2), 0, 1);
        EnqueueSpectrumSamples(samples);
        EnqueueDisplaySamples(oversampledMonoSamples, oversampledInterleavedSamples);
        _displayFrameVersion++;

        var frame = new FrameMetrics(
            SampleSquares: _channels == 1 ? left * left : (left * left) + (right * right),
            LeftSquare: left * left,
            RightSquare: right * right,
            Cross: left * right,
            MidSquare: mid * mid,
            SideSquare: side * side,
            PolarAngle: polarAngle,
            PolarRadius: polarRadius);

        _frames.Enqueue(frame);
        Add(frame);

        if (_frames.Count > _capacityFrames)
        {
            Subtract(_frames.Dequeue());
        }
    }

    public void Reset()
    {
        _frames.Clear();
        _sampleSquareSum = 0;
        _leftSquareSum = 0;
        _rightSquareSum = 0;
        _crossSum = 0;
        _midSquareSum = 0;
        _sideSquareSum = 0;
        Array.Clear(_spectrumSamples);
        foreach (var samples in _channelSpectrumSamples)
        {
            Array.Clear(samples);
        }

        Array.Clear(_displaySamples);
        foreach (var samples in _channelDisplaySamples)
        {
            Array.Clear(samples);
        }

        _spectrumStartIndex = 0;
        _spectrumSampleCount = 0;
        _displayStartIndex = 0;
        _displaySampleCount = 0;
        _displayVersion++;
        _displayFrameVersion = 0;
        _waveformVersion = -1;
        _channelWaveformVersion = -1;
        _spectrumVersion = -1;
        _channelSpectrumVersion = -1;
        _spectrumEligibleFrameVersion = -1;
        _channelSpectrumEligibleFrameVersion = -1;
        _waveformCache = null;
        _channelWaveformCache = null;
        _spectrumCache = null;
        _channelSpectrumCache = null;
        foreach (var buffer in _waveformBuffers)
        {
            buffer.SetCount(0);
        }

        foreach (var buffer in _spectrumBuffers)
        {
            buffer.SetCount(0);
        }

        foreach (var buffers in _channelWaveformBuffers)
        {
            foreach (var buffer in buffers)
            {
                buffer.SetCount(0);
            }
        }

        foreach (var buffers in _channelSpectrumBuffers)
        {
            foreach (var buffer in buffers)
            {
                buffer.SetCount(0);
            }
        }
    }

    private void Add(FrameMetrics frame)
    {
        _sampleSquareSum += frame.SampleSquares;
        _leftSquareSum += frame.LeftSquare;
        _rightSquareSum += frame.RightSquare;
        _crossSum += frame.Cross;
        _midSquareSum += frame.MidSquare;
        _sideSquareSum += frame.SideSquare;
    }

    private void Subtract(FrameMetrics frame)
    {
        _sampleSquareSum -= frame.SampleSquares;
        _leftSquareSum -= frame.LeftSquare;
        _rightSquareSum -= frame.RightSquare;
        _crossSum -= frame.Cross;
        _midSquareSum -= frame.MidSquare;
        _sideSquareSum -= frame.SideSquare;
    }

    private void EnqueueDisplaySamples(ReadOnlySpan<double> samples, ReadOnlySpan<double> interleavedChannelSamples)
    {
        var channelFrameCount = interleavedChannelSamples.Length / _channels;
        for (var index = 0; index < samples.Length; index++)
        {
            var slot = ReserveDisplaySampleSlot();
            var mono = SanitizeSample(samples[index]);
            _displaySamples[slot] = mono;
            for (var channel = 0; channel < _channels; channel++)
            {
                var channelSample = index < channelFrameCount
                    ? interleavedChannelSamples[(index * _channels) + channel]
                    : mono;
                _channelDisplaySamples[channel][slot] = SanitizeSample(channelSample);
            }

            _displayVersion++;
        }
    }

    private void EnqueueSpectrumSamples(ReadOnlySpan<double> samples)
    {
        var slot = ReserveSpectrumSampleSlot();
        var mono = 0d;
        for (var channel = 0; channel < _channels; channel++)
        {
            var sample = SanitizeSample(samples[channel]);
            _channelSpectrumSamples[channel][slot] = sample;
            mono += sample;
        }

        _spectrumSamples[slot] = mono / _channels;
    }

    private int ReserveSpectrumSampleSlot()
    {
        if (_spectrumSamples.Length == 0)
        {
            return 0;
        }

        if (_spectrumSampleCount < _spectrumSamples.Length)
        {
            var slot = (_spectrumStartIndex + _spectrumSampleCount) % _spectrumSamples.Length;
            _spectrumSampleCount++;
            return slot;
        }

        var overwriteSlot = _spectrumStartIndex;
        _spectrumStartIndex = (_spectrumStartIndex + 1) % _spectrumSamples.Length;
        return overwriteSlot;
    }

    private int ReserveDisplaySampleSlot()
    {
        if (_displaySamples.Length == 0)
        {
            return 0;
        }

        if (_displaySampleCount < _displaySamples.Length)
        {
            var slot = (_displayStartIndex + _displaySampleCount) % _displaySamples.Length;
            _displaySampleCount++;
            return slot;
        }

        var overwriteSlot = _displayStartIndex;
        _displayStartIndex = (_displayStartIndex + 1) % _displaySamples.Length;
        return overwriteSlot;
    }

    private double GetDisplaySample(int offset)
    {
        return _displaySamples[(_displayStartIndex + offset) % _displaySamples.Length];
    }

    private double GetChannelDisplaySample(int channel, int offset)
    {
        return _channelDisplaySamples[channel][(_displayStartIndex + offset) % _displaySamples.Length];
    }

    private double GetSpectrumSample(int offset)
    {
        return _spectrumSamples[(_spectrumStartIndex + offset) % _spectrumSamples.Length];
    }

    private double GetChannelSpectrumSample(int channel, int offset)
    {
        return _channelSpectrumSamples[channel][(_spectrumStartIndex + offset) % _spectrumSamples.Length];
    }

    private void FillWaveformBuffer(SampleBuffer target)
    {
        FillWaveformBuffer(target, sourceChannel: null);
    }

    private void FillWaveformBuffer(SampleBuffer target, int? sourceChannel)
    {
        if (_displaySampleCount <= MaximumWaveformPoints)
        {
            for (var index = 0; index < _displaySampleCount; index++)
            {
                var sample = sourceChannel is int channel
                    ? GetChannelDisplaySample(channel, index)
                    : GetDisplaySample(index);
                target.Values[index] = Math.Clamp(sample, -1.5, 1.5);
            }

            target.SetCount(_displaySampleCount);
            return;
        }

        var bucketCount = MaximumWaveformPoints / 2;
        var outputIndex = 0;
        for (var bucket = 0; bucket < bucketCount; bucket++)
        {
            var start = bucket * _displaySampleCount / bucketCount;
            var end = Math.Max(start + 1, (bucket + 1) * _displaySampleCount / bucketCount);
            var minimumValue = double.PositiveInfinity;
            var maximumValue = double.NegativeInfinity;
            var minimumIndex = start;
            var maximumIndex = start;

            for (var index = start; index < end && index < _displaySampleCount; index++)
            {
                var sample = sourceChannel is int channel
                    ? GetChannelDisplaySample(channel, index)
                    : GetDisplaySample(index);
                var value = Math.Clamp(sample, -1.5, 1.5);
                if (value < minimumValue)
                {
                    minimumValue = value;
                    minimumIndex = index;
                }

                if (value > maximumValue)
                {
                    maximumValue = value;
                    maximumIndex = index;
                }
            }

            if (minimumIndex <= maximumIndex)
            {
                target.Values[outputIndex++] = minimumValue;
                target.Values[outputIndex++] = maximumValue;
            }
            else
            {
                target.Values[outputIndex++] = maximumValue;
                target.Values[outputIndex++] = minimumValue;
            }
        }

        target.SetCount(outputIndex);
    }

    private void FillSpectrumBuffer(SampleBuffer target)
    {
        FillSpectrumBuffer(target, sourceChannel: null);
    }

    private void FillSpectrumBuffer(SampleBuffer target, int? sourceChannel)
    {
        var fftSize = PreviousPowerOfTwo(Math.Min(_spectrumSampleCount, _performanceSettings.SpectrumAnalysisSamples));
        if (fftSize < 128)
        {
            target.SetCount(0);
            return;
        }

        EnsureFftWorkspace(fftSize);

        var start = _spectrumSampleCount - fftSize;
        for (var index = 0; index < fftSize; index++)
        {
            _fftInput[index] = sourceChannel is int channel
                ? GetChannelSpectrumSample(channel, start + index)
                : GetSpectrumSample(start + index);
        }

        _spectrumFft.ComputeMagnitudes(_fftInput, _fftWindow, fftSize, _fftWindowSum, _fftMagnitudes);
        var magnitudeCount = (fftSize / 2) + 1;
        var minimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
        var maximumMel = SpectrumScale.HertzToMel(Math.Min(SpectrumScale.MaximumFrequencyHz, _sampleRate * 0.49));
        for (var bucket = 0; bucket < SpectrumScale.BucketCount; bucket++)
        {
            var ratio = SpectrumScale.BucketCount == 1 ? 0 : (double)bucket / (SpectrumScale.BucketCount - 1);
            var frequency = SpectrumScale.MelToHertz(minimumMel + ((maximumMel - minimumMel) * ratio));
            var binPosition = frequency * fftSize / _sampleRate;
            target.Values[bucket] = MagnitudeToDisplayLevel(InterpolateMagnitude(_fftMagnitudes, magnitudeCount, binPosition));
        }

        target.SetCount(SpectrumScale.BucketCount);
    }

    private bool ShouldRefreshSpectrum(ref long eligibleFrameVersion, bool hasCache)
    {
        if (!hasCache)
        {
            eligibleFrameVersion = _displayFrameVersion;
            return true;
        }

        if (_displayFrameVersion - eligibleFrameVersion < _performanceSettings.SpectrumRefreshIntervalFrames)
        {
            return false;
        }

        eligibleFrameVersion = _displayFrameVersion;
        return true;
    }

    private void EnsureFftWorkspace(int fftSize)
    {
        if (_fftInput.Length == fftSize)
        {
            return;
        }

        _fftInput = new double[fftSize];
        _fftMagnitudes = new double[(fftSize / 2) + 1];
        _fftWindow = new double[fftSize];
        _fftWindowSum = 0;
        for (var index = 0; index < fftSize; index++)
        {
            var window = HannWindow(index, fftSize);
            _fftWindow[index] = window;
            _fftWindowSum += window;
        }
    }

    private static double InterpolateMagnitude(IReadOnlyList<double> magnitudes, int count, double binPosition)
    {
        var lower = (int)Math.Floor(binPosition);
        if (lower < 0)
        {
            return magnitudes[0];
        }

        if (lower >= count - 1)
        {
            return magnitudes[count - 1];
        }

        var fraction = binPosition - lower;
        return magnitudes[lower] + ((magnitudes[lower + 1] - magnitudes[lower]) * fraction);
    }

    private static double MagnitudeToDisplayLevel(double magnitude)
    {
        var decibels = magnitude <= 1e-12 ? SpectrumDisplayFloorDb : 20 * Math.Log10(magnitude);
        return Math.Clamp((decibels - SpectrumDisplayFloorDb) / SpectrumDisplayRangeDb, 0, 1);
    }

    private static double SanitizeSample(double sample)
    {
        return double.IsFinite(sample) ? sample : 0;
    }

    private static int PreviousPowerOfTwo(int value)
    {
        var power = 1;
        while (power * 2 <= value)
        {
            power *= 2;
        }

        return power;
    }

    private static double HannWindow(int index, int length)
    {
        return 0.5 - (0.5 * Math.Cos(2 * Math.PI * index / Math.Max(1, length - 1)));
    }

    private readonly record struct FrameMetrics(
        double SampleSquares,
        double LeftSquare,
        double RightSquare,
        double Cross,
        double MidSquare,
        double SideSquare,
        double PolarAngle,
        double PolarRadius);

    private sealed class SampleBuffer : IReadOnlyList<double>
    {
        public SampleBuffer(int capacity)
        {
            Values = new double[capacity];
        }

        public double[] Values { get; }

        public int Count { get; private set; }

        public double this[int index] => index >= 0 && index < Count
            ? Values[index]
            : throw new ArgumentOutOfRangeException(nameof(index));

        public void SetCount(int count)
        {
            Count = Math.Clamp(count, 0, Values.Length);
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return Values[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
