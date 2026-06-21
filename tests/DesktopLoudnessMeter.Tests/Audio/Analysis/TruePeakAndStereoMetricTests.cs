using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class TruePeakAndStereoMetricTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void RmsUsesMostRecentHundredMilliseconds()
    {
        var analyzer = Analyzer();

        analyzer.Process(Sine(seconds: 0.1, leftAmplitude: 0.5, rightAmplitude: 0.5));
        analyzer.Process(Sine(seconds: 0.1, leftAmplitude: 0.25, rightAmplitude: 0.25, startFrame: SampleRate / 10));

        var expectedRmsDb = 20 * Math.Log10(0.25 / Math.Sqrt(2));
        Assert.Equal(expectedRmsDb, analyzer.Snapshot.Rms100MsDb!.Value, precision: 1);
    }

    [Fact]
    public void CorrelationReportsMonoAndAntiPhase()
    {
        var mono = Analyzer();
        mono.Process(Sine(seconds: 0.2, leftAmplitude: 0.5, rightAmplitude: 0.5));

        var antiPhase = Analyzer();
        antiPhase.Process(Sine(seconds: 0.2, leftAmplitude: 0.5, rightAmplitude: -0.5));

        Assert.Equal(1, mono.Snapshot.Correlation!.Value, precision: 2);
        Assert.Equal(-1, antiPhase.Snapshot.Correlation!.Value, precision: 2);
    }

    [Fact]
    public void StereoImagePointsTowardHardPannedSide()
    {
        var left = Analyzer();
        left.Process(Sine(seconds: 0.2, leftAmplitude: 0.5, rightAmplitude: 0));

        var right = Analyzer();
        right.Process(Sine(seconds: 0.2, leftAmplitude: 0, rightAmplitude: 0.5));

        Assert.InRange(left.Snapshot.StereoImageX!.Value, -1, -0.9);
        Assert.InRange(right.Snapshot.StereoImageX!.Value, 0.9, 1);
        Assert.InRange(left.Snapshot.StereoImageY!.Value, -1, 1);
        Assert.InRange(right.Snapshot.StereoImageY!.Value, -1, 1);
    }

    [Fact]
    public void StereoImageIncludesRecentLissajousSamplePoints()
    {
        var mono = Analyzer();
        mono.Process(Sine(seconds: 0.2, leftAmplitude: 0.8, rightAmplitude: 0.8));

        var left = Analyzer();
        left.Process(Sine(seconds: 0.2, leftAmplitude: 0.8, rightAmplitude: 0));

        var right = Analyzer();
        right.Process(Sine(seconds: 0.2, leftAmplitude: 0, rightAmplitude: 0.8));

        var monoPoints = mono.Snapshot.StereoImagePoints!;
        var leftPoints = left.Snapshot.StereoImagePoints!;
        var rightPoints = right.Snapshot.StereoImagePoints!;

        Assert.NotEmpty(monoPoints);
        Assert.All(monoPoints, point => Assert.InRange(Math.Abs(point.Angle), 0, 0.001));
        Assert.Contains(monoPoints, point => point.Radius > 0.5);
        Assert.Contains(leftPoints, point => point.Angle < -0.99 && point.Radius > 0.5);
        Assert.Contains(rightPoints, point => point.Angle > 0.99 && point.Radius > 0.5);
        Assert.All(monoPoints.Concat(leftPoints).Concat(rightPoints), point => Assert.InRange(point.Radius, 0, 1));
    }

    [Fact]
    public void TruePeakFollowsRecentSignalWindow()
    {
        var analyzer = Analyzer();

        analyzer.Process(Sine(seconds: 0.1, leftAmplitude: 0.9, rightAmplitude: 0.9));
        analyzer.Process(Sine(seconds: 0.2, leftAmplitude: 0.1, rightAmplitude: 0.1, startFrame: SampleRate / 10));

        var truePeakAmplitude = Math.Pow(10, analyzer.Snapshot.TruePeakDb!.Value / 20);
        Assert.InRange(truePeakAmplitude, 0.08, 0.16);
    }

    [Fact]
    public void TruePeakOversamplingCanExceedDiscreteSamplePeak()
    {
        var samples = Sine(
            seconds: 0.1,
            leftAmplitude: 0.9,
            rightAmplitude: 0.9,
            frequency: SampleRate / 4d,
            phaseRadians: Math.PI / 4);
        var discretePeak = samples.Max(sample => Math.Abs(sample));
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 8));

        analyzer.Process(samples);

        var truePeakAmplitude = Math.Pow(10, analyzer.Snapshot.TruePeakDb!.Value / 20);
        Assert.True(truePeakAmplitude > discretePeak + 0.05);
        Assert.InRange(truePeakAmplitude, 0.85, 0.95);
    }

    [Fact]
    public void TruePeakOversamplingRatioChangesMeasuredInterSamplePeak()
    {
        var samples = Sine(
            seconds: 0.1,
            leftAmplitude: 0.9,
            rightAmplitude: 0.9,
            frequency: SampleRate / 4d,
            phaseRadians: 3 * Math.PI / 8);
        var twoX = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 2));
        var eightX = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 8));

        twoX.Process(samples);
        eightX.Process(samples);

        var twoXAmplitude = Math.Pow(10, twoX.Snapshot.TruePeakDb!.Value / 20);
        var eightXAmplitude = Math.Pow(10, eightX.Snapshot.TruePeakDb!.Value / 20);
        Assert.True(eightXAmplitude > twoXAmplitude + 0.02, $"2x={twoXAmplitude:0.000}, 8x={eightXAmplitude:0.000}");
    }

    [Fact]
    public void NativeRfRapidTruePeakMatchesManagedEstimatorWhenAvailable()
    {
        using var native = TryCreateNativeTruePeakEstimator(channels: 2, oversampling: 8, windowFrames: SampleRate / 10);
        if (native is null)
        {
            return;
        }

        var samples = Sine(
            seconds: 0.12,
            leftAmplitude: 0.9,
            rightAmplitude: 0.8,
            frequency: SampleRate / 4d,
            phaseRadians: 3 * Math.PI / 8);
        var managed = new TruePeakEstimator(channels: 2, oversampling: 8, windowFrames: SampleRate / 10);
        Span<double> frame = stackalloc double[2];
        Span<double> managedMono = stackalloc double[8];
        Span<double> managedInterleaved = stackalloc double[16];

        var nativeInput = new double[samples.Length];
        for (var index = 0; index < samples.Length; index++)
        {
            nativeInput[index] = samples[index];
        }

        var frameCount = samples.Length / 2;
        var nativeMono = new double[frameCount * 8];
        var nativeInterleaved = new double[frameCount * 8 * 2];
        var nativeFramePeaks = new double[frameCount];
        var nativeFrameOutputCounts = new int[frameCount];

        native.Process(nativeInput, frameCount, nativeMono, nativeInterleaved, nativeFramePeaks, nativeFrameOutputCounts);

        for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex += 2)
        {
            frame[0] = samples[sampleIndex];
            frame[1] = samples[sampleIndex + 1];
            var frameIndex = sampleIndex / 2;
            var managedCount = managed.Add(frame, managedMono, managedInterleaved);
            var nativeOffset = frameIndex * 8;

            Assert.Equal(managedCount, nativeFrameOutputCounts[frameIndex]);
            for (var index = 0; index < managedCount; index++)
            {
                Assert.Equal(managedMono[index], nativeMono[nativeOffset + index], precision: 12);
            }
        }

        Assert.Equal(managed.TruePeak, native.TruePeak, precision: 12);
    }

    [Fact]
    public void AnalyzerExposesWaveformAndSpectrumRollData()
    {
        var analyzer = Analyzer();

        analyzer.Process(Sine(seconds: 0.2, leftAmplitude: 0.7, rightAmplitude: 0.7, frequency: 1000));

        var waveform = analyzer.Snapshot.WaveformSamples!;
        var spectrum = analyzer.Snapshot.SpectrumBuckets!;
        Assert.NotEmpty(waveform);
        Assert.All(waveform, sample => Assert.InRange(sample, -1, 1));
        Assert.Equal(SpectrumScale.BucketCount, spectrum.Count);
        Assert.All(spectrum, bucket => Assert.InRange(bucket, 0, 1));
        Assert.Contains(spectrum, bucket => bucket > 0);
    }

    [Fact]
    public void AnalyzerCanExposePerChannelSpectrumRollDataWhenRequested()
    {
        var analyzer = Analyzer();
        analyzer.SetChannelSpectrumEnabled(true);

        analyzer.Process(DualSine(seconds: 0.2, leftFrequency: 1000, rightFrequency: 4000));

        var channels = analyzer.Snapshot.ChannelSpectrumBuckets!;
        Assert.Equal(2, channels.Count);
        Assert.Equal(SpectrumScale.BucketCount, channels[0].Count);
        Assert.Equal(SpectrumScale.BucketCount, channels[1].Count);
        Assert.Contains(channels[0], bucket => bucket > 0);
        Assert.Contains(channels[1], bucket => bucket > 0);
        Assert.NotEqual(channels[0], channels[1]);
    }

    [Fact]
    public void AnalyzerSkipsPerChannelSpectrumByDefault()
    {
        var analyzer = Analyzer();

        analyzer.Process(DualSine(seconds: 0.2, leftFrequency: 1000, rightFrequency: 4000));

        Assert.Null(analyzer.Snapshot.ChannelSpectrumBuckets);
    }

    [Fact]
    public void SpectrumScaleCoversAudibleRange()
    {
        Assert.Equal(20, SpectrumScale.MinimumFrequencyHz);
        Assert.Equal(22000, SpectrumScale.MaximumFrequencyHz);
    }

    [Fact]
    public void WaveformRollUsesOversampledDisplayBuffer()
    {
        var samples = Sine(seconds: 0.003, leftAmplitude: 0.7, rightAmplitude: 0.7, frequency: 7000);
        var twoX = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 2));
        var eightX = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 8));

        twoX.Process(samples);
        eightX.Process(samples);

        Assert.True(eightX.Snapshot.WaveformSamples!.Count > twoX.Snapshot.WaveformSamples!.Count * 2);
    }

    [Fact]
    public void AnalyzerCanExposePerChannelWaveformRollDataWhenRequested()
    {
        var analyzer = Analyzer();
        analyzer.SetChannelWaveformEnabled(true);

        analyzer.Process(DualSine(seconds: 0.003, leftFrequency: 1000, rightFrequency: 4000));

        var channels = analyzer.Snapshot.ChannelWaveformSamples!;
        Assert.Equal(2, channels.Count);
        Assert.NotEmpty(channels[0]);
        Assert.NotEmpty(channels[1]);
        Assert.All(channels[0].Concat(channels[1]), sample => Assert.InRange(sample, -1, 1));
        Assert.NotEqual(channels[0], channels[1]);
    }

    [Fact]
    public void AnalyzerSkipsPerChannelWaveformByDefault()
    {
        var analyzer = Analyzer();

        analyzer.Process(DualSine(seconds: 0.003, leftFrequency: 1000, rightFrequency: 4000));

        Assert.Null(analyzer.Snapshot.ChannelWaveformSamples);
    }

    private static PcmMetricAnalyzer Analyzer()
    {
        return new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 4));
    }

    private static NativeRfRapidTruePeakEstimator? TryCreateNativeTruePeakEstimator(
        int channels,
        int oversampling,
        int windowFrames)
    {
        try
        {
            return new NativeRfRapidTruePeakEstimator(channels, oversampling, windowFrames);
        }
        catch (Exception ex) when (ex is DllNotFoundException
            or EntryPointNotFoundException
            or BadImageFormatException
            or InvalidOperationException
            or System.Runtime.InteropServices.SEHException)
        {
            return null;
        }
    }

    private static float[] Sine(
        double seconds,
        double leftAmplitude,
        double rightAmplitude,
        int startFrame = 0,
        double frequency = 1000,
        double phaseRadians = 0)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var phase = (2 * Math.PI * frequency * (startFrame + frame) / SampleRate) + phaseRadians;
            samples[frame * 2] = (float)(Math.Sin(phase) * leftAmplitude);
            samples[(frame * 2) + 1] = (float)(Math.Sin(phase) * rightAmplitude);
        }

        return samples;
    }

    private static float[] DualSine(
        double seconds,
        double leftFrequency,
        double rightFrequency,
        double amplitude = 0.7)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            samples[frame * 2] = (float)(Math.Sin(2 * Math.PI * leftFrequency * frame / SampleRate) * amplitude);
            samples[(frame * 2) + 1] = (float)(Math.Sin(2 * Math.PI * rightFrequency * frame / SampleRate) * amplitude);
        }

        return samples;
    }
}
