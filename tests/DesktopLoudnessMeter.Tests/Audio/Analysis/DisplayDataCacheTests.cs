using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// Display data may follow the existing frame-based performance cadence, but every published view (Lissajous
/// points, waveform, and spectrum) must be a stable snapshot, must refresh once the cadence elapses, and must
/// never freeze the numeric metrics that share the same window.
/// </summary>
public sealed class DisplayDataCacheTests
{
    private const int SampleRate = 48000;
    private const int Capacity = 4800;

    [Fact]
    public void StereoImagePointsReuseThePublishedViewUntilTheRefreshIntervalElapses()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.1, right: 0.1);
        var initial = window.StereoImagePoints!;

        AddFrames(window, 128, left: 0.8, right: 0.8);
        var reused = window.StereoImagePoints!;

        Assert.Same(initial, reused);

        AddFrames(window, 1024, left: 0.8, right: 0.8);
        var refreshed = window.StereoImagePoints!;

        Assert.NotSame(initial, refreshed);
        Assert.NotEqual(initial.ToArray(), refreshed.ToArray());
    }

    [Fact]
    public void EcoModeLengthensTheStereoImagePointRefreshInterval()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.1, right: 0.1);
        var normalInitial = window.StereoImagePoints!;

        AddFrames(window, 128, left: 0.8, right: 0.8);
        Assert.Same(normalInitial, window.StereoImagePoints);

        // Switching performance mode invalidates the published view immediately.
        window.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);
        var ecoInitial = window.StereoImagePoints!;
        Assert.NotSame(normalInitial, ecoInitial);

        AddFrames(window, 1024, left: 0.8, right: 0.8);
        Assert.Same(ecoInitial, window.StereoImagePoints);

        AddFrames(window, 8192, left: -0.4, right: -0.4);
        var refreshed = window.StereoImagePoints!;

        Assert.NotSame(ecoInitial, refreshed);
        Assert.NotEqual(ecoInitial.ToArray(), refreshed.ToArray());
    }

    [Fact]
    public void ResetClearsStereoImagePointsAndPublishesAFreshView()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.1, right: 0.1);
        var beforeReset = window.StereoImagePoints!;
        Assert.NotEmpty(beforeReset);

        window.Reset();

        Assert.Null(window.StereoImagePoints);

        AddFrames(window, Capacity, left: 0, right: 0.6);
        var afterReset = window.StereoImagePoints!;

        Assert.NotSame(beforeReset, afterReset);
        Assert.NotEqual(beforeReset.ToArray(), afterReset.ToArray());
        Assert.All(afterReset, point => Assert.True(point.Angle > 0.99, $"expected hard-right angle, got {point.Angle:0.000}"));
    }

    [Fact]
    public void PublishedStereoImagePointsAreNeverMutatedByLaterRefreshes()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.2, right: 0);
        var published = window.StereoImagePoints!;
        var publishedCopy = published.ToArray();

        AddFrames(window, 4096, left: -0.7, right: 0);

        var latest = window.StereoImagePoints!;
        Assert.NotSame(published, latest);
        Assert.Equal(publishedCopy, published.ToArray());
        Assert.All(latest, point => Assert.True(point.Angle < -0.99, $"expected hard-left angle, got {point.Angle:0.000}"));
    }

    [Fact]
    public void NumericStereoMetricsStayCurrentWhileThePointCloudIsCached()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.5, right: 0);
        var cachedPoints = window.StereoImagePoints!;
        var hardLeftX = window.StereoImageX!.Value;

        Assert.Equal(-1, hardLeftX, precision: 9);

        // 960 frames of hard-right material leave the 4800 frame window 3840:960 mixed. The point cloud is
        // still cached, but the numeric stereo image must already reflect the new input:
        // leftRms = sqrt(960/4800) = 0.447214, rightRms = sqrt(240/4800) = 0.223607, X = -1/3.
        AddFrames(window, 960, left: 0, right: 0.5);

        Assert.Same(cachedPoints, window.StereoImagePoints);
        Assert.Equal(-1d / 3, window.StereoImageX!.Value, precision: 6);
        Assert.Equal(0, window.Correlation!.Value, precision: 12);
    }

    [Fact]
    public void AnalyzerPublishesStereoImagePointsForNewInputAfterReset()
    {
        using var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, 2, 4));

        analyzer.Process(HardPanned(seconds: 0.2, left: 0.8, right: 0));
        var leftPoints = analyzer.Snapshot.StereoImagePoints!;
        Assert.NotEmpty(leftPoints);
        Assert.All(leftPoints, point => Assert.True(point.Angle < -0.99, $"expected hard-left angle, got {point.Angle:0.000}"));

        analyzer.Reset();

        Assert.Null(analyzer.Snapshot.StereoImagePoints);
        Assert.Null(analyzer.Snapshot.StereoImageX);

        analyzer.Process(HardPanned(seconds: 0.2, left: 0, right: 0.8));
        var rightPoints = analyzer.Snapshot.StereoImagePoints!;

        Assert.NotSame(leftPoints, rightPoints);
        Assert.NotEmpty(rightPoints);
        Assert.All(rightPoints, point => Assert.True(point.Angle > 0.99, $"expected hard-right angle, got {point.Angle:0.000}"));
    }

    [Fact]
    public void PublishedWaveformViewsAreStableSnapshots()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.1, right: 0.1);
        var waveform = window.WaveformSamples!;
        var channelWaveform = window.ChannelWaveformSamples!;
        var waveformSnapshot = waveform.ToArray();
        var channelSnapshot = channelWaveform.Select(channel => channel.ToArray()).ToArray();
        Assert.NotEmpty(waveformSnapshot);

        // Two further refreshes exceed the inherited two-buffer pool, so a pooled view would be overwritten.
        var refreshed = waveform;
        for (var refresh = 0; refresh < 2; refresh++)
        {
            AddFrames(window, 2048, left: 0.9, right: 0.9);
            var latest = window.WaveformSamples!;
            Assert.NotSame(refreshed, latest);
            refreshed = latest;
        }

        Assert.Equal(waveformSnapshot, waveform.ToArray());
        Assert.Equal(channelSnapshot.Length, channelWaveform.Count);
        for (var channel = 0; channel < channelWaveform.Count; channel++)
        {
            Assert.Equal(channelSnapshot[channel], channelWaveform[channel].ToArray());
        }
    }

    [Fact]
    public void PublishedSpectrumViewsAreStableSnapshots()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.1, right: 0.1);
        var spectrum = window.SpectrumBuckets!;
        var channelSpectrum = window.ChannelSpectrumBuckets!;
        var spectrumSnapshot = spectrum.ToArray();
        var channelSnapshot = channelSpectrum.Select(channel => channel.ToArray()).ToArray();
        Assert.Equal(SpectrumScale.BucketCount, spectrumSnapshot.Length);

        var refreshed = spectrum;
        for (var refresh = 0; refresh < 2; refresh++)
        {
            AddFrames(window, 1024, left: 0.9, right: -0.9);
            var latest = window.SpectrumBuckets!;
            Assert.NotSame(refreshed, latest);
            refreshed = latest;
        }

        Assert.Equal(spectrumSnapshot, spectrum.ToArray());
        Assert.Equal(channelSnapshot.Length, channelSpectrum.Count);
        for (var channel = 0; channel < channelSpectrum.Count; channel++)
        {
            Assert.Equal(channelSnapshot[channel], channelSpectrum[channel].ToArray());
        }
    }

    [Fact]
    public void ResetLeavesPreviouslyPublishedDisplayViewsIntact()
    {
        var window = CreateWindow();

        AddFrames(window, Capacity, left: 0.4, right: 0.2);
        var waveform = window.WaveformSamples!;
        var channelWaveform = window.ChannelWaveformSamples!;
        var spectrum = window.SpectrumBuckets!;
        var channelSpectrum = window.ChannelSpectrumBuckets!;
        var points = window.StereoImagePoints!;
        var waveformSnapshot = waveform.ToArray();
        var channelWaveformSnapshot = channelWaveform.Select(channel => channel.ToArray()).ToArray();
        var spectrumSnapshot = spectrum.ToArray();
        var channelSpectrumSnapshot = channelSpectrum.Select(channel => channel.ToArray()).ToArray();
        var pointsSnapshot = points.ToArray();

        window.Reset();

        Assert.Null(window.WaveformSamples);
        Assert.Null(window.ChannelWaveformSamples);
        Assert.Null(window.SpectrumBuckets);
        Assert.Null(window.ChannelSpectrumBuckets);
        Assert.Null(window.StereoImagePoints);

        Assert.Equal(waveformSnapshot, waveform.ToArray());
        Assert.Equal(spectrumSnapshot, spectrum.ToArray());
        Assert.Equal(pointsSnapshot, points.ToArray());
        for (var channel = 0; channel < channelWaveform.Count; channel++)
        {
            Assert.Equal(channelWaveformSnapshot[channel], channelWaveform[channel].ToArray());
            Assert.Equal(channelSpectrumSnapshot[channel], channelSpectrum[channel].ToArray());
        }
    }

    private static RecentSampleWindow CreateWindow()
    {
        return new RecentSampleWindow(channels: 2, SampleRate, oversampling: 4, capacityFrames: Capacity);
    }

    private static void AddFrames(RecentSampleWindow window, int frames, double left, double right)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            window.Add([left, right], [left, right, left, right]);
        }
    }

    private static float[] HardPanned(double seconds, double left, double right)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var phase = Math.Sin(2 * Math.PI * 1000 * frame / SampleRate);
            samples[frame * 2] = (float)(phase * left);
            samples[(frame * 2) + 1] = (float)(phase * right);
        }

        return samples;
    }
}
