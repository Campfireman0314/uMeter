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
    private readonly FrameMetrics[] _frameSlots;
    private int _frameStartIndex;
    private int _frameCount;
    private readonly double[] _spectrumSamples;
    private readonly double[][] _channelSpectrumSamples;
    private readonly double[] _displaySamples;
    private readonly double[][] _channelDisplaySamples;
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
    private long _waveformEligibleFrameVersion = -1;
    private long _channelWaveformEligibleFrameVersion = -1;
    private long _spectrumEligibleFrameVersion = -1;
    private long _channelSpectrumEligibleFrameVersion = -1;
    private long _stereoImageVersion = -1;
    private long _stereoImageEligibleFrameVersion = -1;
    private IReadOnlyList<double>? _waveformCache;
    private IReadOnlyList<IReadOnlyList<double>>? _channelWaveformCache;
    private IReadOnlyList<double>? _spectrumCache;
    private IReadOnlyList<IReadOnlyList<double>>? _channelSpectrumCache;
    private IReadOnlyList<StereoImagePoint>? _stereoImageCache;
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
        _frameSlots = new FrameMetrics[capacityFrames];
        _spectrumSamples = new double[capacityFrames];
        _channelSpectrumSamples = Enumerable.Range(0, channels)
            .Select(_ => new double[capacityFrames])
            .ToArray();
        _displaySamples = new double[capacityFrames * oversampling];
        _channelDisplaySamples = Enumerable.Range(0, channels)
            .Select(_ => new double[capacityFrames * oversampling])
            .ToArray();
    }

    public bool IsFull => _frameCount == _capacityFrames;

    public double Rms
    {
        get
        {
            var sampleCount = _frameCount * _channels;
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

            var leftRms = Math.Sqrt(_leftSquareSum / _frameCount);
            var rightRms = Math.Sqrt(_rightSquareSum / _frameCount);
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

            var midRms = Math.Sqrt(_midSquareSum / _frameCount);
            var sideRms = Math.Sqrt(_sideSquareSum / _frameCount);
            var sum = midRms + sideRms;
            return sum <= double.Epsilon ? 0 : Math.Clamp((midRms - sideRms) / sum, -1, 1);
        }
    }

    /// <summary>
    /// Lissajous point cloud for the recent window. This is display data, so it is rebuilt on the same
    /// frame-based cadence as the waveform and spectrum rolls instead of on every processed block, and each
    /// published view is an independent read-only snapshot that later refreshes never mutate.
    /// </summary>
    public IReadOnlyList<StereoImagePoint>? StereoImagePoints
    {
        get
        {
            if (!IsFull || _channels < 2)
            {
                return null;
            }

            if (_stereoImageVersion == _displayFrameVersion)
            {
                return _stereoImageCache;
            }

            if (!ShouldRefresh(_performanceSettings.StereoImageRefreshIntervalFrames, ref _stereoImageEligibleFrameVersion, _stereoImageCache is not null))
            {
                _stereoImageVersion = _displayFrameVersion;
                return _stereoImageCache;
            }

            _stereoImageCache = BuildStereoImagePoints();
            _stereoImageVersion = _displayFrameVersion;
            return _stereoImageCache;
        }
    }

    private IReadOnlyList<StereoImagePoint> BuildStereoImagePoints()
    {
        // Frames live in a ring buffer, so only the sampled frames are touched: the stride keeps the cloud at
        // or below MaximumStereoImagePoints without walking every retained frame.
        var stride = Math.Max(1, _frameCount / MaximumStereoImagePoints);
        var points = new StereoImagePoint[(_frameCount + stride - 1) / stride];
        var output = 0;
        for (var offset = 0; offset < _frameCount; offset += stride)
        {
            var frame = _frameSlots[(_frameStartIndex + offset) % _capacityFrames];
            points[output++] = new StereoImagePoint(frame.PolarAngle, frame.PolarRadius);
        }

        return Array.AsReadOnly(points);
    }

    /// <summary>
    /// Downsampled waveform for display. Every published view is a read-only snapshot: a consumer that keeps
    /// the reference keeps the samples it was given, even after later refreshes or a reset.
    /// </summary>
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

            if (!ShouldRefreshWaveform(ref _waveformEligibleFrameVersion, _waveformCache is not null))
            {
                _waveformVersion = _displayVersion;
                return _waveformCache;
            }

            _waveformCache = Array.AsReadOnly(BuildWaveform(sourceChannel: null));
            _waveformVersion = _displayVersion;
            return _waveformCache;
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

            if (!ShouldRefreshWaveform(ref _channelWaveformEligibleFrameVersion, _channelWaveformCache is not null))
            {
                _channelWaveformVersion = _displayVersion;
                return _channelWaveformCache;
            }

            var buffers = new IReadOnlyList<double>[_channels];
            for (var channel = 0; channel < _channels; channel++)
            {
                buffers[channel] = Array.AsReadOnly(BuildWaveform(channel));
            }

            _channelWaveformCache = Array.AsReadOnly(buffers);
            _channelWaveformVersion = _displayVersion;
            return _channelWaveformCache;
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

            _spectrumCache = BuildSpectrum(sourceChannel: null);
            _spectrumVersion = _displayVersion;
            return _spectrumCache;
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

            var buffers = new IReadOnlyList<double>[_channels];
            for (var channel = 0; channel < _channels; channel++)
            {
                buffers[channel] = BuildSpectrum(channel);
            }

            _channelSpectrumCache = Array.AsReadOnly(buffers);
            _channelSpectrumVersion = _displayVersion;
            return _channelSpectrumCache;
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
        _waveformVersion = -1;
        _channelWaveformVersion = -1;
        _spectrumVersion = -1;
        _channelSpectrumVersion = -1;
        _waveformEligibleFrameVersion = -1;
        _channelWaveformEligibleFrameVersion = -1;
        _spectrumEligibleFrameVersion = -1;
        _channelSpectrumEligibleFrameVersion = -1;
        _stereoImageVersion = -1;
        _stereoImageEligibleFrameVersion = -1;
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

        AddFrame(frame);
    }

    private void AddFrame(FrameMetrics frame)
    {
        if (_frameCount == _capacityFrames)
        {
            Subtract(_frameSlots[_frameStartIndex]);
            _frameStartIndex = (_frameStartIndex + 1) % _capacityFrames;
            _frameCount--;
        }

        _frameSlots[(_frameStartIndex + _frameCount) % _capacityFrames] = frame;
        Add(frame);
        _frameCount++;
    }

    public void Reset()
    {
        Array.Clear(_frameSlots);
        _frameStartIndex = 0;
        _frameCount = 0;
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
        _waveformEligibleFrameVersion = -1;
        _channelWaveformEligibleFrameVersion = -1;
        _spectrumEligibleFrameVersion = -1;
        _channelSpectrumEligibleFrameVersion = -1;
        _stereoImageVersion = -1;
        _stereoImageEligibleFrameVersion = -1;
        _waveformCache = null;
        _channelWaveformCache = null;
        _spectrumCache = null;
        _channelSpectrumCache = null;
        _stereoImageCache = null;
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

    /// <summary>
    /// Builds one waveform view into a freshly allocated array, so the returned view is never written again.
    /// </summary>
    private double[] BuildWaveform(int? sourceChannel)
    {
        if (_displaySampleCount <= MaximumWaveformPoints)
        {
            var samples = new double[_displaySampleCount];
            for (var index = 0; index < samples.Length; index++)
            {
                samples[index] = Math.Clamp(DisplaySample(sourceChannel, index), -1.5, 1.5);
            }

            return samples;
        }

        var bucketCount = MaximumWaveformPoints / 2;
        var values = new double[bucketCount * 2];
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
                var value = Math.Clamp(DisplaySample(sourceChannel, index), -1.5, 1.5);
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
                values[outputIndex++] = minimumValue;
                values[outputIndex++] = maximumValue;
            }
            else
            {
                values[outputIndex++] = maximumValue;
                values[outputIndex++] = minimumValue;
            }
        }

        return outputIndex == values.Length ? values : values[..outputIndex];
    }

    private double DisplaySample(int? sourceChannel, int index)
    {
        return sourceChannel is int channel
            ? GetChannelDisplaySample(channel, index)
            : GetDisplaySample(index);
    }

    /// <summary>
    /// Builds one spectrum view into a freshly allocated array, so the returned view is never written again.
    /// </summary>
    private IReadOnlyList<double> BuildSpectrum(int? sourceChannel)
    {
        var fftSize = PreviousPowerOfTwo(Math.Min(_spectrumSampleCount, _performanceSettings.SpectrumAnalysisSamples));
        if (fftSize < 128)
        {
            return Array.Empty<double>();
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
        var values = new double[SpectrumScale.BucketCount];
        for (var bucket = 0; bucket < SpectrumScale.BucketCount; bucket++)
        {
            var ratio = SpectrumScale.BucketCount == 1 ? 0 : (double)bucket / (SpectrumScale.BucketCount - 1);
            var frequency = SpectrumScale.MelToHertz(minimumMel + ((maximumMel - minimumMel) * ratio));
            var binPosition = frequency * fftSize / _sampleRate;
            values[bucket] = MagnitudeToDisplayLevel(InterpolateMagnitude(_fftMagnitudes, magnitudeCount, binPosition));
        }

        return Array.AsReadOnly(values);
    }

    private bool ShouldRefreshSpectrum(ref long eligibleFrameVersion, bool hasCache)
    {
        return ShouldRefresh(_performanceSettings.SpectrumRefreshIntervalFrames, ref eligibleFrameVersion, hasCache);
    }

    private bool ShouldRefreshWaveform(ref long eligibleFrameVersion, bool hasCache)
    {
        return ShouldRefresh(_performanceSettings.WaveformRefreshIntervalFrames, ref eligibleFrameVersion, hasCache);
    }

    /// <summary>
    /// Frame-based display cadence shared by the waveform, spectrum, and stereo-image rolls: display data is
    /// rebuilt at most once per interval, and the first read after a reset or mode change always rebuilds.
    /// </summary>
    private bool ShouldRefresh(int intervalFrames, ref long eligibleFrameVersion, bool hasCache)
    {
        if (!hasCache || eligibleFrameVersion < 0)
        {
            eligibleFrameVersion = _displayFrameVersion;
            return true;
        }

        if (_displayFrameVersion - eligibleFrameVersion < intervalFrames)
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
}
