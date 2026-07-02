using DesktopLoudnessMeter.Protocol;
using System.Text.Json;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class LocalLoopbackProtocolTests
{
    [Fact]
    public void RegisterPacketRoundTrips()
    {
        var bytes = LocalLoopbackProtocol.EncodeRegister(
            "plugin-1",
            "Lead Vocal",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2);

        Assert.True(LocalLoopbackProtocol.TryDecode(bytes, out var packet));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;

        Assert.Equal(LocalLoopbackPacketType.Register, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal("Lead Vocal", packet.DisplayName);
        Assert.Equal(LocalLoopbackSourceKind.Plugin, packet.SourceKind);
        Assert.Equal(48000, packet.SampleRate);
        Assert.Equal(2, packet.Channels);
        Assert.Equal(LocalLoopbackProtocol.ProtocolVersion, root.GetProperty("protocol").GetString());
        Assert.Equal("register", root.GetProperty("type").GetString());
        Assert.Equal("plugin-1", root.GetProperty("handle").GetString());
        Assert.Equal("Lead Vocal", root.GetProperty("displayName").GetString());
        Assert.Equal("plugin", root.GetProperty("sourceKind").GetString());
        Assert.Equal(48000, root.GetProperty("sampleRate").GetInt32());
        Assert.Equal(2, root.GetProperty("channels").GetInt32());
    }

    [Fact]
    public void AudioPacketRoundTripsFloatSamples()
    {
        var bytes = LocalLoopbackProtocol.EncodeAudio(
            "plugin-1",
            sequence: 7,
            samples: [0.25f, -0.5f]);

        Assert.True(LocalLoopbackProtocol.TryDecode(bytes, out var packet));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;

        Assert.Equal(LocalLoopbackPacketType.Audio, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal(7, packet.Sequence);
        Assert.Equal([0.25f, -0.5f], packet.Samples);
        Assert.Equal(LocalLoopbackProtocol.ProtocolVersion, root.GetProperty("protocol").GetString());
        Assert.Equal("audio", root.GetProperty("type").GetString());
        Assert.Equal("plugin-1", root.GetProperty("handle").GetString());
        Assert.Equal(7, root.GetProperty("sequence").GetInt64());
        Assert.Equal("AACAPgAAAL8=", root.GetProperty("samplesBase64").GetString());
    }

    [Theory]
    [InlineData(LocalLoopbackPacketType.Heartbeat)]
    [InlineData(LocalLoopbackPacketType.Close)]
    [InlineData(LocalLoopbackPacketType.Ack)]
    public void SimpleControlPacketsRoundTrip(LocalLoopbackPacketType type)
    {
        var bytes = type switch
        {
            LocalLoopbackPacketType.Heartbeat => LocalLoopbackProtocol.EncodeHeartbeat("plugin-1"),
            LocalLoopbackPacketType.Close => LocalLoopbackProtocol.EncodeClose("plugin-1"),
            LocalLoopbackPacketType.Ack => LocalLoopbackProtocol.EncodeAck("plugin-1"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        Assert.True(LocalLoopbackProtocol.TryDecode(bytes, out var packet));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;

        Assert.Equal(type, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal(LocalLoopbackProtocol.ProtocolVersion, root.GetProperty("protocol").GetString());
        Assert.Equal(WireType(type), root.GetProperty("type").GetString());
        Assert.Equal("plugin-1", root.GetProperty("handle").GetString());
    }

    [Fact]
    public void RenamePacketRoundTrips()
    {
        var bytes = LocalLoopbackProtocol.EncodeRename("plugin-1", "Renamed Bus");

        Assert.True(LocalLoopbackProtocol.TryDecode(bytes, out var packet));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;

        Assert.Equal(LocalLoopbackPacketType.Rename, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal("Renamed Bus", packet.DisplayName);
        Assert.Equal(LocalLoopbackProtocol.ProtocolVersion, root.GetProperty("protocol").GetString());
        Assert.Equal("rename", root.GetProperty("type").GetString());
        Assert.Equal("plugin-1", root.GetProperty("handle").GetString());
        Assert.Equal("Renamed Bus", root.GetProperty("displayName").GetString());
    }

    [Theory]
    [InlineData("{\"protocol\":\"wrong\",\"type\":\"register\",\"handle\":\"x\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\",\"handle\":\"x\",\"sampleRate\":0,\"channels\":2}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\",\"handle\":\"x\",\"sampleRate\":48000,\"channels\":6}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"audio\",\"handle\":\"x\",\"samplesBase64\":\"AQID\"}")]
    public void InvalidPacketsAreRejected(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        Assert.False(LocalLoopbackProtocol.TryDecode(bytes, out _));
    }

    private static string WireType(LocalLoopbackPacketType type)
    {
        return type switch
        {
            LocalLoopbackPacketType.Register => "register",
            LocalLoopbackPacketType.Heartbeat => "heartbeat",
            LocalLoopbackPacketType.Rename => "rename",
            LocalLoopbackPacketType.Audio => "audio",
            LocalLoopbackPacketType.Close => "close",
            LocalLoopbackPacketType.Ack => "ack",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
