using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class WasapiSessionMeterServiceTests
{
    [Fact]
    public void MergesAvailableProcessMetricsIntoSnapshots()
    {
        var provider = new FakeProcessMetricProvider();
        provider.Metrics[42] = new PcmMetricsSnapshot(
            MomentaryLufs: -18.2,
            ShortTermLufs: -19.4,
            IntegratedLufs: -20.1,
            TruePeakDb: -1.2,
            Rms100MsDb: -22.5,
            StereoImageX: 0.25,
            StereoImageY: 0.75,
            Correlation: 0.85,
            WaveformSamples: [0.1, -0.1],
            ChannelWaveformSamples: [[0.2, -0.2], [0.4, -0.4]],
            SpectrumBuckets: [0.25, 0.75],
            ChannelSpectrumBuckets: [[0.1, 0.2], [0.3, 0.4]]);
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42), Snapshot("b", null)],
            currentProcessId: 9000);

        var snapshots = service.GetCurrentSessions();

        var processBacked = snapshots.Single(snapshot => snapshot.SessionKey == "pid:42");
        Assert.Equal(-18.2, processBacked.MomentaryLufs);
        Assert.Equal(-19.4, processBacked.ShortTermLufs);
        Assert.Equal(-20.1, processBacked.IntegratedLufs);
        Assert.Equal(-1.2, processBacked.TruePeakDb);
        Assert.Equal(-22.5, processBacked.Rms100MsDb);
        Assert.Equal(0.25, processBacked.StereoImageX);
        Assert.Equal(0.75, processBacked.StereoImageY);
        Assert.Equal(0.85, processBacked.Correlation);
        Assert.Equal([0.1, -0.1], processBacked.WaveformSamples);
        Assert.Equal([0.2, -0.2], processBacked.ChannelWaveformSamples![0]);
        Assert.Equal([0.4, -0.4], processBacked.ChannelWaveformSamples![1]);
        Assert.Equal([0.25, 0.75], processBacked.SpectrumBuckets);
        Assert.Equal([0.1, 0.2], processBacked.ChannelSpectrumBuckets![0]);
        Assert.Equal([0.3, 0.4], processBacked.ChannelSpectrumBuckets![1]);

        var pidless = snapshots.Single(snapshot => snapshot.SessionKey == "b");
        Assert.Null(pidless.MomentaryLufs);
        Assert.Equal([42], provider.TrackedProcessSets.Single());
    }

    [Fact]
    public void ExposesWasapiRowsThroughSignalSourceService()
    {
        var service = new WasapiSessionMeterService(
            new FakeProcessMetricProvider(),
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);
        ISignalSourceMeterService sourceService = service;

        var source = Assert.Single(sourceService.GetCurrentSources());

        Assert.Equal("pid:42", source.SourceKey);
        Assert.Equal(SignalSourceKind.WasapiSession, source.SourceKind);
        Assert.Equal("WASAPI", source.SourceKindLabel);
        Assert.Equal("pid:42", ((AudioSessionSnapshot)source).SessionKey);
    }

    [Fact]
    public void ProviderFailuresDoNotEscapeRefresh()
    {
        var provider = new FakeProcessMetricProvider { ThrowOnTrack = true };
        var service = new WasapiSessionMeterService(provider, () => [Snapshot("a", 42)], currentProcessId: 9000);

        var snapshot = Assert.Single(service.GetCurrentSessions());

        Assert.Null(snapshot.MomentaryLufs);
        Assert.Equal("pid:42", snapshot.SessionKey);
    }

    [Fact]
    public void CollapsesDuplicateProcessRowsAndOmitsSelfAndSilentRows()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () =>
            [
                Snapshot("first", 42, peak: 0.1f, peakText: "-20.0 dB"),
                Snapshot("second", 42, peak: 0.5f, peakText: "-6.0 dB"),
                Snapshot("self", 9000, peak: 0.9f),
                Snapshot("silent-loopback", 77, peak: 0, state: "AudioSessionStateActive")
            ],
            currentProcessId: 9000);

        var snapshot = Assert.Single(service.GetCurrentSessions());

        Assert.Equal("pid:42", snapshot.SessionKey);
        Assert.Equal(42, snapshot.ProcessId);
        Assert.Equal(0.5f, snapshot.Peak);
        Assert.Equal("-6.0 dB", snapshot.PeakDbText);
        Assert.Equal([42], provider.TrackedProcessSets.Single());
    }

    [Fact]
    public void ResetDelegatesToProcessMetricProvider()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42), Snapshot("b", 84)],
            currentProcessId: 9000);

        service.ResetSession("pid:42", 42);
        service.ResetAllSessions();

        Assert.Equal([42], provider.ResetProcessIds);
        Assert.Equal(1, provider.ResetAllCount);
    }

    [Fact]
    public void ResetDelegatesThroughSignalSourceInterface()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42), Snapshot("b", 84)],
            currentProcessId: 9000);
        ISignalSourceResetService resetService = service;

        resetService.ResetSource("pid:42", SignalSourceKind.WasapiSession, 42);
        resetService.ResetAllSources();

        Assert.Equal([42], provider.ResetProcessIds);
        Assert.Equal(1, provider.ResetAllCount);
    }

    [Fact]
    public void TruePeakOversamplingDelegatesToProcessMetricProvider()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);

        service.SetTruePeakOversampling("pid:42", 42, 8);
        service.SetTruePeakOversampling("self", 9000, 2);

        Assert.Equal([(42, 8)], provider.TruePeakOversamplingRequests);
    }

    [Fact]
    public void AnalysisSettingsDelegateThroughSignalSourceInterface()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);
        ISignalSourceAnalysisSettingsService settingsService = service;

        settingsService.SetTruePeakOversampling("pid:42", SignalSourceKind.WasapiSession, 42, 8);
        settingsService.SetChannelWaveformEnabled("pid:42", SignalSourceKind.WasapiSession, 42, true);
        settingsService.SetChannelSpectrumEnabled("pid:42", SignalSourceKind.WasapiSession, 42, true);

        Assert.Equal([(42, 8)], provider.TruePeakOversamplingRequests);
        Assert.Equal([(42, true)], provider.ChannelWaveformRequests);
        Assert.Equal([(42, true)], provider.ChannelSpectrumRequests);
    }

    [Fact]
    public void ChannelSpectrumSettingDelegatesToProcessMetricProvider()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);

        service.SetChannelSpectrumEnabled("pid:42", 42, true);
        service.SetChannelSpectrumEnabled("self", 9000, true);

        Assert.Equal([(42, true)], provider.ChannelSpectrumRequests);
    }

    [Fact]
    public void ChannelWaveformSettingDelegatesToProcessMetricProvider()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);

        service.SetChannelWaveformEnabled("pid:42", 42, true);
        service.SetChannelWaveformEnabled("self", 9000, true);

        Assert.Equal([(42, true)], provider.ChannelWaveformRequests);
    }

    [Fact]
    public void PerformanceModeDelegatesToProcessMetricProvider()
    {
        var provider = new FakeProcessMetricProvider();
        var service = new WasapiSessionMeterService(
            provider,
            () => [Snapshot("a", 42)],
            currentProcessId: 9000);

        service.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);

        Assert.Equal([PcmAnalysisPerformanceMode.Eco], provider.PerformanceModeRequests);
    }

    private static AudioSessionSnapshot Snapshot(
        string key,
        int? processId,
        float peak = 0.5f,
        string peakText = "-6.0 dB",
        string state = "Active")
    {
        return new AudioSessionSnapshot(
            key,
            processId,
            key,
            peak,
            PeakMath.ToDecibels(peak),
            peakText,
            state);
    }

    private sealed class FakeProcessMetricProvider : IProcessMetricProvider
    {
        public Dictionary<int, PcmMetricsSnapshot> Metrics { get; } = [];

        public List<int[]> TrackedProcessSets { get; } = [];

        public List<int> ResetProcessIds { get; } = [];

        public List<(int ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(int ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(int ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public int ResetAllCount { get; private set; }

        public bool ThrowOnTrack { get; init; }

        public void TrackProcesses(IReadOnlyCollection<int> processIds)
        {
            if (ThrowOnTrack)
            {
                throw new InvalidOperationException("loopback unavailable");
            }

            TrackedProcessSets.Add(processIds.Order().ToArray());
        }

        public PcmMetricsSnapshot? GetMetricsForProcess(int processId)
        {
            return Metrics.GetValueOrDefault(processId);
        }

        public void ResetProcess(int processId)
        {
            ResetProcessIds.Add(processId);
        }

        public void ResetAll()
        {
            ResetAllCount++;
        }

        public void SetTruePeakOversampling(int processId, int oversampling)
        {
            TruePeakOversamplingRequests.Add((processId, oversampling));
        }

        public void SetChannelWaveformEnabled(int processId, bool enabled)
        {
            ChannelWaveformRequests.Add((processId, enabled));
        }

        public void SetChannelSpectrumEnabled(int processId, bool enabled)
        {
            ChannelSpectrumRequests.Add((processId, enabled));
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            PerformanceModeRequests.Add(mode);
        }

        public void Dispose()
        {
        }
    }
}
