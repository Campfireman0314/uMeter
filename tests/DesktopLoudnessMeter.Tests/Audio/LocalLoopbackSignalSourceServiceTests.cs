using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class LocalLoopbackSignalSourceServiceTests
{
    private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    [Fact]
    public void RegisterRenameAndDuplicateNamesPreserveHandleIdentity()
    {
        var service = CreateService();

        service.Ingest(Register("plugin-a", "Shared", LocalLoopbackSourceKind.Plugin));
        service.Ingest(Register("plugin-b", "Shared", LocalLoopbackSourceKind.Plugin));
        service.Ingest(new LocalLoopbackPacket(LocalLoopbackPacketType.Rename, "plugin-a", "Lead Vocal"));

        var sources = service.GetCurrentSources().OrderBy(source => source.SourceKey).ToArray();

        Assert.Equal(2, sources.Length);
        Assert.Equal("loopback:plugin-a", sources[0].SourceKey);
        Assert.Equal("Lead Vocal", sources[0].DisplayName);
        Assert.Equal(SignalSourceKind.Plugin, sources[0].SourceKind);
        Assert.Equal("loopback:plugin-b", sources[1].SourceKey);
        Assert.Equal("Shared", sources[1].DisplayName);
    }

    [Fact]
    public void AudioFeedsAnalyzerAndSettingsControlChannelMetrics()
    {
        var service = CreateService();
        var sourceKey = LocalLoopbackSignalSourceService.BuildSourceKey("plugin-a");
        service.Ingest(Register("plugin-a", "Lead Vocal", LocalLoopbackSourceKind.Plugin));
        service.SetChannelWaveformEnabled(sourceKey, SignalSourceKind.Plugin, null, true);
        service.SetChannelSpectrumEnabled(sourceKey, SignalSourceKind.Plugin, null, true);
        service.SetTruePeakOversampling(sourceKey, SignalSourceKind.Plugin, null, 8);

        service.Ingest(Audio("plugin-a", StereoTone(4800, 0.5f, -0.5f)));

        var source = Assert.Single(service.GetCurrentSources());
        Assert.Equal(0.5f, source.Peak);
        Assert.Equal("-6.0 dB", source.PeakDbText);
        Assert.NotNull(source.TruePeakDb);
        Assert.NotNull(source.Rms100MsDb);
        Assert.NotEmpty(source.WaveformSamples!);
        Assert.NotEmpty(source.SpectrumBuckets!);
        Assert.NotNull(source.ChannelWaveformSamples);
        Assert.NotNull(source.ChannelSpectrumBuckets);

        service.ResetSource(sourceKey, SignalSourceKind.Plugin, null);
        source = Assert.Single(service.GetCurrentSources());

        Assert.Null(source.TruePeakDb);
        Assert.Null(source.Rms100MsDb);

        service.Ingest(Audio("plugin-a", StereoTone(4800, 0.25f, -0.25f)));
        Assert.NotNull(Assert.Single(service.GetCurrentSources()).TruePeakDb);

        service.ResetAllSources();
        Assert.Null(Assert.Single(service.GetCurrentSources()).TruePeakDb);
    }

    [Fact]
    public void HeartbeatTimeoutCloseAndReregisterControlVisibility()
    {
        var service = CreateService(timeout: TimeSpan.FromSeconds(1));
        service.Ingest(Register("plugin-a", "Lead Vocal", LocalLoopbackSourceKind.Plugin));

        _now += TimeSpan.FromMilliseconds(900);
        service.Ingest(new LocalLoopbackPacket(LocalLoopbackPacketType.Heartbeat, "plugin-a"));
        Assert.Single(service.GetCurrentSources());

        _now += TimeSpan.FromMilliseconds(900);
        service.Ingest(Audio("plugin-a", StereoTone(64, 0.1f, -0.1f)));
        Assert.Single(service.GetCurrentSources());

        _now += TimeSpan.FromMilliseconds(1100);
        Assert.Empty(service.GetCurrentSources());

        service.Ingest(Register("plugin-a", "Lead Vocal Again", LocalLoopbackSourceKind.Plugin));
        Assert.Equal("loopback:plugin-a", Assert.Single(service.GetCurrentSources()).SourceKey);

        service.Ingest(new LocalLoopbackPacket(LocalLoopbackPacketType.Close, "plugin-a"));
        Assert.Empty(service.GetCurrentSources());
    }

    private LocalLoopbackSignalSourceService CreateService(TimeSpan? timeout = null)
    {
        return new LocalLoopbackSignalSourceService(() => _now, timeout);
    }

    private static LocalLoopbackPacket Register(string handle, string name, LocalLoopbackSourceKind kind)
    {
        return new LocalLoopbackPacket(
            LocalLoopbackPacketType.Register,
            handle,
            name,
            kind,
            SampleRate: 48000,
            Channels: 2);
    }

    private static LocalLoopbackPacket Audio(string handle, IReadOnlyList<float> samples)
    {
        return new LocalLoopbackPacket(
            LocalLoopbackPacketType.Audio,
            handle,
            Sequence: 1,
            Samples: samples);
    }

    private static float[] StereoTone(int frames, float left, float right)
    {
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            samples[frame * 2] = left;
            samples[(frame * 2) + 1] = right;
        }

        return samples;
    }
}
