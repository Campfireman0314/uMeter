using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class PcmMetricAnalyzerContractTests
{
    [Theory]
    [InlineData(0, 2, 4)]
    [InlineData(48000, 0, 4)]
    [InlineData(48000, 3, 4)]
    [InlineData(48000, 2, 1)]
    [InlineData(48000, 2, 16)]
    public void RejectsInvalidOptions(int sampleRate, int channels, int oversampling)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, channels, oversampling)));
    }

    [Fact]
    public void RejectsUnalignedInterleavedBlocks()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));

        Assert.Throws<ArgumentException>(() => analyzer.Process([0.1f, -0.1f, 0.2f]));
    }

    [Fact]
    public void SnapshotStartsUnavailableAndIndependentOfUiTypes()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));

        var snapshot = analyzer.Snapshot;

        Assert.Null(snapshot.MomentaryLufs);
        Assert.Null(snapshot.ShortTermLufs);
        Assert.Null(snapshot.IntegratedLufs);
        Assert.Null(snapshot.TruePeakDb);
        Assert.Null(snapshot.Rms100MsDb);
        Assert.Null(snapshot.StereoImageX);
        Assert.Null(snapshot.StereoImageY);
        Assert.Null(snapshot.Correlation);
        Assert.Equal("DesktopLoudnessMeter.App.Audio.Analysis", typeof(PcmMetricsSnapshot).Namespace);
    }

    [Fact]
    public void ResetClearsAvailableMetrics()
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(48000, 2, 4));
        var block = Sine(seconds: 1, amplitude: 0.25);

        analyzer.Process(block);
        Assert.NotNull(analyzer.Snapshot.MomentaryLufs);
        Assert.NotNull(analyzer.Snapshot.TruePeakDb);

        analyzer.Reset();

        Assert.Null(analyzer.Snapshot.MomentaryLufs);
        Assert.Null(analyzer.Snapshot.TruePeakDb);
        Assert.Null(analyzer.Snapshot.Rms100MsDb);
    }

    [Fact]
    public void EcoModeThrottlesSpectrumFftRefreshes()
    {
        var normalFft = new CountingSpectrumFft();
        var normalWindow = new RecentSampleWindow(2, 48000, 4, 4800, normalFft);

        ProcessFramesWithSpectrumReads(normalWindow, 4600);

        var ecoFft = new CountingSpectrumFft();
        var ecoWindow = new RecentSampleWindow(2, 48000, 4, 4800, ecoFft);
        ecoWindow.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);

        ProcessFramesWithSpectrumReads(ecoWindow, 4600);

        Assert.True(normalFft.ComputeCount > ecoFft.ComputeCount);
        Assert.Equal(4, normalFft.ComputeCount);
        Assert.Equal(1, ecoFft.ComputeCount);
    }

    [Fact]
    public void EcoModeKeepsSpectrumResolutionBelowNormal()
    {
        var fft = new CountingSpectrumFft();
        var window = new RecentSampleWindow(2, 48000, 4, 4800, fft);
        window.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);

        ProcessFramesWithSpectrumReads(window, 4800);

        Assert.Equal(4096, fft.LastFftSize);
    }

    [Fact]
    public void WaveformRefreshReusesCacheBetweenEligibleFrameIntervals()
    {
        var window = new RecentSampleWindow(2, 48000, 4, 4800);

        AddConstantFramesWithWaveformReads(window, 1, 0.1);
        var initial = window.WaveformSamples!.ToArray();

        AddConstantFramesWithWaveformReads(window, 128, 0.8);
        var reused = window.WaveformSamples!.ToArray();

        Assert.Equal(initial, reused);

        AddConstantFramesWithWaveformReads(window, 2048, -0.4);
        var refreshed = window.WaveformSamples!.ToArray();

        Assert.NotEqual(initial, refreshed);
    }

    [Fact]
    public void EcoModeUsesLongerWaveformRefreshInterval()
    {
        var window = new RecentSampleWindow(2, 48000, 4, 4800);

        AddConstantFramesWithWaveformReads(window, 1, 0.1);
        var normalInitial = window.WaveformSamples!.ToArray();
        AddConstantFramesWithWaveformReads(window, 128, 0.8);
        Assert.Equal(normalInitial, window.WaveformSamples!.ToArray());

        window.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);
        var ecoInitial = window.WaveformSamples!.ToArray();
        Assert.NotEqual(normalInitial, ecoInitial);

        AddConstantFramesWithWaveformReads(window, 1024, 0.8);
        var reused = window.WaveformSamples!.ToArray();

        Assert.Equal(ecoInitial, reused);

        AddConstantFramesWithWaveformReads(window, 8192, -0.4);
        var refreshed = window.WaveformSamples!.ToArray();

        Assert.NotEqual(ecoInitial, refreshed);
    }

    private static float[] Sine(double seconds, double amplitude, double frequency = 1000)
    {
        const int sampleRate = 48000;
        var frames = (int)Math.Round(seconds * sampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * frequency * frame / sampleRate) * amplitude);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }

    private static void ProcessFramesWithSpectrumReads(RecentSampleWindow window, int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            var sample = Math.Sin(2 * Math.PI * 1000 * frame / 48000) * 0.25;
            window.Add([sample, sample], [sample, sample, sample, sample]);
            _ = window.SpectrumBuckets;
        }
    }

    private static void AddConstantFramesWithWaveformReads(RecentSampleWindow window, int frames, double sample)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            window.Add([sample, sample], [sample, sample, sample, sample]);
            _ = window.WaveformSamples;
        }
    }

    private sealed class CountingSpectrumFft : ISpectrumFft
    {
        public string Name => "Counting FFT";

        public int ComputeCount { get; private set; }

        public int LastFftSize { get; private set; }

        public void ComputeMagnitudes(
            double[] samples,
            double[] window,
            int fftSize,
            double windowSum,
            double[] magnitudes)
        {
            ComputeCount++;
            LastFftSize = fftSize;
            Array.Clear(magnitudes);
        }
    }
}
