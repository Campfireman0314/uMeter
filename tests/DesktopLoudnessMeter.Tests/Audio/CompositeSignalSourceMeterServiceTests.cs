using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class CompositeSignalSourceMeterServiceTests
{
    [Fact]
    public void ReturnsSourcesFromAllChildren()
    {
        var first = new FakeSignalSourceService([Source("wasapi:1", SignalSourceKind.WasapiSession)]);
        var second = new FakeSignalSourceService([Source("loopback:1", SignalSourceKind.Plugin)]);
        using var composite = new CompositeSignalSourceMeterService(first, second);

        var sources = composite.GetCurrentSources();

        Assert.Equal(["wasapi:1", "loopback:1"], sources.Select(source => source.SourceKey));
    }

    [Fact]
    public void ResetAndSettingsFanOutToChildren()
    {
        var first = new FakeSignalSourceService([]);
        var second = new FakeSignalSourceService([]);
        using var composite = new CompositeSignalSourceMeterService(first, second);

        composite.ResetSource("loopback:1", SignalSourceKind.Plugin, null);
        composite.ResetAllSources();
        composite.SetTruePeakOversampling("loopback:1", SignalSourceKind.Plugin, null, 8);
        composite.SetChannelWaveformEnabled("loopback:1", SignalSourceKind.Plugin, null, true);
        composite.SetChannelSpectrumEnabled("loopback:1", SignalSourceKind.Plugin, null, true);
        composite.SetPerformanceMode(PcmAnalysisPerformanceMode.Eco);

        Assert.All(new[] { first, second }, service =>
        {
            Assert.Equal([("loopback:1", SignalSourceKind.Plugin, null)], service.ResetRequests);
            Assert.Equal(1, service.ResetAllCount);
            Assert.Equal([("loopback:1", SignalSourceKind.Plugin, null, 8)], service.TruePeakOversamplingRequests);
            Assert.Equal([("loopback:1", SignalSourceKind.Plugin, null, true)], service.ChannelWaveformRequests);
            Assert.Equal([("loopback:1", SignalSourceKind.Plugin, null, true)], service.ChannelSpectrumRequests);
            Assert.Equal([PcmAnalysisPerformanceMode.Eco], service.PerformanceModeRequests);
        });
    }

    [Fact]
    public void DisposeDisposesDisposableChildren()
    {
        var first = new FakeSignalSourceService([]);
        var second = new FakeSignalSourceService([]);
        var composite = new CompositeSignalSourceMeterService(first, second);

        composite.Dispose();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    private static SignalSourceSnapshot Source(string key, SignalSourceKind kind)
    {
        return new SignalSourceSnapshot(
            key,
            kind,
            null,
            key,
            0.5f,
            -6,
            "-6.0 dB",
            "Active");
    }

    private sealed class FakeSignalSourceService :
        ISignalSourceMeterService,
        ISignalSourceResetService,
        ISignalSourceAnalysisSettingsService,
        IDisposable
    {
        private readonly IReadOnlyList<SignalSourceSnapshot> _sources;

        public FakeSignalSourceService(IReadOnlyList<SignalSourceSnapshot> sources)
        {
            _sources = sources;
        }

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId)> ResetRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public int ResetAllCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return _sources;
        }

        public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
        {
            ResetRequests.Add((sourceKey, sourceKind, processId));
        }

        public void ResetAllSources()
        {
            ResetAllCount++;
        }

        public void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling)
        {
            TruePeakOversamplingRequests.Add((sourceKey, sourceKind, processId, oversampling));
        }

        public void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelWaveformRequests.Add((sourceKey, sourceKind, processId, enabled));
        }

        public void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelSpectrumRequests.Add((sourceKey, sourceKind, processId, enabled));
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            PerformanceModeRequests.Add(mode);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
