using System.Net;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.Tests.Audio;

[Collection(LocalLoopbackUdpTestCollection.Name)]
public sealed class LocalLoopbackUdpReceiverTests
{
    [Fact]
    public void RouteReceivedPacketIgnoresInboundAckWithoutAckResponse()
    {
        var routedPackets = new List<LocalLoopbackPacket>();
        var shouldAck = LocalLoopbackUdpReceiver.RouteReceivedPacket(
            IPAddress.Loopback,
            LocalLoopbackProtocol.EncodeAck("plugin-a"),
            routedPackets.Add,
            out var ackHandle);

        Assert.False(shouldAck);
        Assert.Null(ackHandle);
        Assert.Empty(routedPackets);
    }

    [Fact]
    public void RouteReceivedPacketRoutesValidNonAckFromLoopback()
    {
        var routedPackets = new List<LocalLoopbackPacket>();
        var shouldAck = LocalLoopbackUdpReceiver.RouteReceivedPacket(
            IPAddress.Loopback,
            LocalLoopbackProtocol.EncodeRegister(
                "plugin-a",
                "Lead Vocal",
                LocalLoopbackSourceKind.Plugin,
                sampleRate: 48000,
                channels: 2),
            routedPackets.Add,
            out var ackHandle);

        var packet = Assert.Single(routedPackets);
        Assert.True(shouldAck);
        Assert.Equal("plugin-a", ackHandle);
        Assert.Equal(LocalLoopbackPacketType.Register, packet.Type);
        Assert.Equal("plugin-a", packet.Handle);
    }

    [Fact]
    public void RouteReceivedPacketIgnoresNonLoopbackRemote()
    {
        var routedPackets = new List<LocalLoopbackPacket>();
        var shouldAck = LocalLoopbackUdpReceiver.RouteReceivedPacket(
            IPAddress.Parse("192.0.2.10"),
            LocalLoopbackProtocol.EncodeRegister(
                "plugin-a",
                "Lead Vocal",
                LocalLoopbackSourceKind.Plugin,
                sampleRate: 48000,
                channels: 2),
            routedPackets.Add,
            out var ackHandle);

        Assert.False(shouldAck);
        Assert.Null(ackHandle);
        Assert.Empty(routedPackets);
    }

    [Fact]
    public void RouteReceivedPacketIgnoresInvalidPayloadWithoutAckResponse()
    {
        var routedPackets = new List<LocalLoopbackPacket>();
        var shouldAck = LocalLoopbackUdpReceiver.RouteReceivedPacket(
            IPAddress.Loopback,
            System.Text.Encoding.UTF8.GetBytes("{\"protocol\":\"wrong\",\"type\":\"register\",\"handle\":\"plugin-a\"}"),
            routedPackets.Add,
            out var ackHandle);

        Assert.False(shouldAck);
        Assert.Null(ackHandle);
        Assert.Empty(routedPackets);
    }

    [Fact(Skip = "Requires a working UDP loopback stack; this environment drops 127.0.0.1 UDP datagrams.")]
    public async Task ReceiverIngestsDiagnosticSenderPackets()
    {
        using var service = new LocalLoopbackSignalSourceService();
        using var receiver = LocalLoopbackUdpReceiver.Start(service, port: 0);
        using var sender = new LocalLoopbackDiagnosticSender(receiver.Port);

        Assert.Equal(IPAddress.Loopback, receiver.LocalEndPoint.Address);

        await sender.RegisterAsync("plugin-a", "Lead Vocal", SignalSourceKind.Plugin, 48000, 2);
        await WaitUntil(() => service.GetCurrentSources().Count == 1);

        await sender.SendAudioAsync("plugin-a", sequence: 1, samples: StereoTone(4800, 0.5f, -0.5f));
        await WaitUntil(() => service.GetCurrentSources().Single().TruePeakDb is not null);

        await sender.RenameAsync("plugin-a", "Renamed Vocal");
        await WaitUntil(() => service.GetCurrentSources().Single().DisplayName == "Renamed Vocal");

        await sender.HeartbeatAsync("plugin-a");
        Assert.Equal("loopback:plugin-a", service.GetCurrentSources().Single().SourceKey);

        await sender.CloseAsync("plugin-a");
        await WaitUntil(() => service.GetCurrentSources().Count == 0);
    }

    [Fact(Skip = "Requires a working UDP loopback stack; this environment drops 127.0.0.1 UDP datagrams.")]
    public async Task DisposingReceiverStopsPacketIngest()
    {
        using var service = new LocalLoopbackSignalSourceService();
        using var receiver = LocalLoopbackUdpReceiver.Start(service, port: 0);
        using var sender = new LocalLoopbackDiagnosticSender(receiver.Port);

        receiver.Dispose();
        await sender.RegisterAsync("plugin-a", "Lead Vocal", SignalSourceKind.Plugin, 48000, 2);
        await Task.Delay(100);

        Assert.Empty(service.GetCurrentSources());
    }

    [Fact]
    public void TryStartReportsBindFailureWithoutThrowing()
    {
        using var firstService = new LocalLoopbackSignalSourceService();
        using var firstReceiver = LocalLoopbackUdpReceiver.Start(firstService, port: 0);
        using var secondService = new LocalLoopbackSignalSourceService();

        var started = LocalLoopbackUdpReceiver.TryStart(secondService, out var secondReceiver, firstReceiver.Port);

        Assert.False(started);
        Assert.Null(secondReceiver);
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
