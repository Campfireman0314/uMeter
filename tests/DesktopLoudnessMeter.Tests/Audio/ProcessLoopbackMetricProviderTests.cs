using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class ProcessLoopbackMetricProviderTests
{
    [Fact]
    public void StartsNewWorkersAndStopsStaleWorkers()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(
            factory,
            () => DateTimeOffset.UnixEpoch,
            staleWorkerRetention: TimeSpan.Zero);

        provider.TrackProcesses([1, 2]);
        provider.TrackProcesses([2, 3]);

        Assert.Equal([1, 2, 3], factory.CreatedProcessIds);
        Assert.True(factory.Workers[1].Disposed);
        Assert.False(factory.Workers[2].Disposed);
        Assert.False(factory.Workers[3].Disposed);
    }

    [Fact]
    public void RetainsStaleWorkersForShortSessionDropouts()
    {
        var now = DateTimeOffset.UnixEpoch;
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(
            factory,
            () => now,
            staleWorkerRetention: TimeSpan.FromSeconds(5));

        provider.TrackProcesses([9]);
        provider.TrackProcesses([]);
        Assert.False(factory.Workers[9].Disposed);

        now += TimeSpan.FromSeconds(6);
        provider.TrackProcesses([]);

        Assert.True(factory.Workers[9].Disposed);
    }

    [Fact]
    public void FailedStartsAreBackedOff()
    {
        var now = DateTimeOffset.UnixEpoch;
        var factory = new FakeWorkerFactory { ThrowForProcessId = 7 };
        var provider = new ProcessLoopbackMetricProvider(
            factory,
            () => now,
            failureBackoff: TimeSpan.FromSeconds(10));

        provider.TrackProcesses([7]);
        provider.TrackProcesses([7]);
        now += TimeSpan.FromSeconds(11);
        provider.TrackProcesses([7]);

        Assert.Equal(2, factory.CreateAttempts.Count(processId => processId == 7));
    }

    [Fact]
    public void ReturnsWorkerSnapshotForProcess()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(factory, () => DateTimeOffset.UnixEpoch);

        provider.TrackProcesses([5]);
        factory.Workers[5].Snapshot = new PcmMetricsSnapshot(MomentaryLufs: -14);

        Assert.Equal(-14, provider.GetMetricsForProcess(5)!.MomentaryLufs);
    }

    [Fact]
    public void ResetDelegatesToTrackedWorkers()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(factory, () => DateTimeOffset.UnixEpoch);

        provider.TrackProcesses([5, 6]);
        provider.ResetProcess(5);
        provider.ResetAll();

        Assert.Equal(2, factory.Workers[5].ResetCount);
        Assert.Equal(1, factory.Workers[6].ResetCount);
    }

    [Fact]
    public void TruePeakOversamplingDelegatesToTrackedWorkersAndNewWorkers()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(factory, () => DateTimeOffset.UnixEpoch);

        provider.TrackProcesses([5]);
        provider.SetTruePeakOversampling(5, 8);
        provider.SetTruePeakOversampling(6, 2);
        provider.TrackProcesses([5, 6]);

        Assert.Equal(8, factory.Workers[5].TruePeakOversampling);
        Assert.Equal(2, factory.Workers[6].TruePeakOversampling);
    }

    [Fact]
    public void ChannelSpectrumSettingDelegatesToTrackedWorkersAndNewWorkers()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(factory, () => DateTimeOffset.UnixEpoch);

        provider.TrackProcesses([5]);
        provider.SetChannelSpectrumEnabled(5, true);
        provider.SetChannelSpectrumEnabled(6, true);
        provider.TrackProcesses([5, 6]);

        Assert.True(factory.Workers[5].ChannelSpectrumEnabled);
        Assert.True(factory.Workers[6].ChannelSpectrumEnabled);
    }

    [Fact]
    public void ChannelWaveformSettingDelegatesToTrackedWorkersAndNewWorkers()
    {
        var factory = new FakeWorkerFactory();
        var provider = new ProcessLoopbackMetricProvider(factory, () => DateTimeOffset.UnixEpoch);

        provider.TrackProcesses([5]);
        provider.SetChannelWaveformEnabled(5, true);
        provider.SetChannelWaveformEnabled(6, true);
        provider.TrackProcesses([5, 6]);

        Assert.True(factory.Workers[5].ChannelWaveformEnabled);
        Assert.True(factory.Workers[6].ChannelWaveformEnabled);
    }

    private sealed class FakeWorkerFactory : IProcessLoopbackCaptureWorkerFactory
    {
        public List<int> CreatedProcessIds { get; } = [];

        public List<int> CreateAttempts { get; } = [];

        public Dictionary<int, FakeWorker> Workers { get; } = [];

        public int? ThrowForProcessId { get; init; }

        public IProcessLoopbackCaptureWorker Create(int processId)
        {
            CreateAttempts.Add(processId);
            if (ThrowForProcessId == processId)
            {
                throw new InvalidOperationException("activation failed");
            }

            CreatedProcessIds.Add(processId);
            var worker = new FakeWorker();
            Workers[processId] = worker;
            return worker;
        }
    }

    private sealed class FakeWorker : IProcessLoopbackCaptureWorker
    {
        public PcmMetricsSnapshot? Snapshot { get; set; }

        public bool IsFaulted { get; set; }

        public bool Started { get; private set; }

        public int ResetCount { get; private set; }

        public bool Disposed { get; private set; }

        public void Start()
        {
            Started = true;
        }

        public void Reset()
        {
            ResetCount++;
        }

        public int TruePeakOversampling { get; private set; } = 4;

        public bool ChannelWaveformEnabled { get; private set; }

        public bool ChannelSpectrumEnabled { get; private set; }

        public void SetTruePeakOversampling(int oversampling)
        {
            TruePeakOversampling = oversampling;
        }

        public void SetChannelWaveformEnabled(bool enabled)
        {
            ChannelWaveformEnabled = enabled;
        }

        public void SetChannelSpectrumEnabled(bool enabled)
        {
            ChannelSpectrumEnabled = enabled;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
