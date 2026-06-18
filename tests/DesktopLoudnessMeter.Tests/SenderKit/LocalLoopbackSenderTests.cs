using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.Protocol;
using DesktopLoudnessMeter.SenderKit;

namespace DesktopLoudnessMeter.Tests.SenderKit;

public sealed class LocalLoopbackSenderTests
{
    [Fact]
    public async Task SenderCreatesRenamesStreamsAndClosesPluginSource()
    {
        using var service = new LocalLoopbackSignalSourceService();
        using var receiver = LocalLoopbackUdpReceiver.Start(service, port: 0);
        await using var sender = new LocalLoopbackSender(new LoopbackSenderOptions(
            "plugin-probe-a",
            "Probe A",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2,
            receiver.Port)
        {
            FramesPerPacket = 480,
            BufferedPacketCapacity = 4,
            HeartbeatInterval = TimeSpan.FromMilliseconds(100)
        });

        await sender.StartAsync();
        await WaitUntil(() => service.GetCurrentSources().Count == 1);

        Assert.True(sender.TryEnqueueAudio(StereoTone(480, 0.25f, -0.25f)));
        await WaitUntil(() => service.GetCurrentSources().Single().TruePeakDb is not null);

        await sender.RenameAsync("Probe Renamed");
        await WaitUntil(() => service.GetCurrentSources().Single().DisplayName == "Probe Renamed");

        var source = service.GetCurrentSources().Single();
        Assert.Equal("loopback:plugin-probe-a", source.SourceKey);
        Assert.Equal(SignalSourceKind.Plugin, source.SourceKind);

        await sender.DisposeAsync();
        await WaitUntil(() => service.GetCurrentSources().Count == 0);
    }

    [Fact]
    public void AudioHandoffDropsInsteadOfBlockingWhenPoolIsFull()
    {
        var sender = new LocalLoopbackSender(new LoopbackSenderOptions(
            "plugin-probe-b",
            "Probe B",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2,
            port: 38173)
        {
            FramesPerPacket = 2,
            BufferedPacketCapacity = 1
        });

        Assert.True(sender.TryEnqueueAudio(StereoTone(2, 0.1f, -0.1f)));
        Assert.False(sender.TryEnqueueAudio(StereoTone(2, 0.1f, -0.1f)));
        Assert.Equal(1, sender.DroppedAudioPacketCount);
    }

    [Fact]
    public void OversizedFrameChunksAreRejectedForUdpJsonTransport()
    {
        var options = new LoopbackSenderOptions(
            "plugin-probe-large",
            "Probe Large",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2,
            port: 38173)
        {
            FramesPerPacket = 4800
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new LocalLoopbackSender(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\u0001handle")]
    public void InvalidHandlesAreRejected(string handle)
    {
        Assert.Throws<ArgumentException>(() => new LoopbackSenderOptions(
            handle,
            "Probe",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2,
            port: 38173));
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTimeOffset.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(condition());
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
