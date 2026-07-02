using System.Text;
using System.Text.Json;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class NativeLoopbackProtocolTests
{
    [Fact]
    public void RegisterPacketDecodesThroughNativeProtocol()
    {
        var bytes = LocalLoopbackProtocol.EncodeRegister(
            "plugin-1",
            "Lead Vocal",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2);

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));

        Assert.Equal(LocalLoopbackPacketType.Register, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal("Lead Vocal", packet.DisplayName);
        Assert.Equal(LocalLoopbackSourceKind.Plugin, packet.SourceKind);
        Assert.Equal(48000, packet.SampleRate);
        Assert.Equal(2, packet.Channels);
    }

    [Fact]
    public void EscapedNonBmpDisplayNameDecodesThroughNativeProtocol()
    {
        var bytes = LocalLoopbackProtocol.EncodeRegister(
            "plugin-1",
            "Mix 🎧",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2);

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));

        Assert.Equal("Mix 🎧", packet.DisplayName);
    }

    [Fact]
    public void NonAsciiHandleUsesCharacterLimitThroughNativeProtocol()
    {
        var handle = new string('源', 128);
        var bytes = LocalLoopbackProtocol.EncodeRegister(
            handle,
            "Source",
            LocalLoopbackSourceKind.Plugin,
            sampleRate: 48000,
            channels: 2);

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));
        Assert.Equal(handle, packet.Handle);
    }

    [Fact]
    public void AudioPacketDecodesLittleEndianFloatSamplesThroughNativeProtocol()
    {
        var bytes = LocalLoopbackProtocol.EncodeAudio(
            "plugin-1",
            sequence: 7,
            samples: [0.25f, -0.5f]);

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));

        Assert.Equal(LocalLoopbackPacketType.Audio, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal(7, packet.Sequence);
        Assert.Equal([0.25f, -0.5f], packet.Samples);
    }

    [Theory]
    [InlineData(LocalLoopbackPacketType.Heartbeat)]
    [InlineData(LocalLoopbackPacketType.Close)]
    [InlineData(LocalLoopbackPacketType.Ack)]
    public void SimpleControlPacketsDecodeThroughNativeProtocol(LocalLoopbackPacketType type)
    {
        var bytes = type switch
        {
            LocalLoopbackPacketType.Heartbeat => LocalLoopbackProtocol.EncodeHeartbeat("plugin-1"),
            LocalLoopbackPacketType.Close => LocalLoopbackProtocol.EncodeClose("plugin-1"),
            LocalLoopbackPacketType.Ack => LocalLoopbackProtocol.EncodeAck("plugin-1"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));

        Assert.Equal(type, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
    }

    [Fact]
    public void RenamePacketDecodesThroughNativeProtocol()
    {
        var bytes = LocalLoopbackProtocol.EncodeRename("plugin-1", "Renamed Bus");

        Assert.True(NativeLoopbackProtocol.TryDecode(bytes, out var packet));

        Assert.Equal(LocalLoopbackPacketType.Rename, packet.Type);
        Assert.Equal("plugin-1", packet.Handle);
        Assert.Equal("Renamed Bus", packet.DisplayName);
    }

    [Theory]
    [InlineData("{\"protocol\":\"wrong\",\"type\":\"register\",\"handle\":\"x\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\",\"handle\":\"x\",\"sampleRate\":0,\"channels\":2}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"register\",\"handle\":\"x\",\"sampleRate\":48000,\"channels\":6}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"audio\",\"handle\":\"x\",\"samplesBase64\":\"AQID\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"audio\",\"handle\":\"x\",\"samplesBase64\":\"\"}")]
    [InlineData("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"rename\",\"handle\":\"x\",\"displayName\":\"   \"}")]
    public void InvalidPacketsAreRejectedByNativeProtocol(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        Assert.False(NativeLoopbackProtocol.TryDecode(bytes, out _));
    }

    [Fact]
    public void AckEncodingUsesProtocolV1WireShape()
    {
        var bytes = NativeLoopbackProtocol.EncodeAck("plugin-1");
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;

        Assert.Equal(LocalLoopbackProtocol.ProtocolVersion, root.GetProperty("protocol").GetString());
        Assert.Equal("ack", root.GetProperty("type").GetString());
        Assert.Equal("plugin-1", root.GetProperty("handle").GetString());
    }
}
