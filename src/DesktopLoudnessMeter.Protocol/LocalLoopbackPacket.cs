namespace DesktopLoudnessMeter.Protocol;

public enum LocalLoopbackPacketType
{
    Register,
    Heartbeat,
    Rename,
    Audio,
    Close,
    Ack
}

public sealed record LocalLoopbackPacket(
    LocalLoopbackPacketType Type,
    string Handle,
    string? DisplayName = null,
    LocalLoopbackSourceKind SourceKind = LocalLoopbackSourceKind.Loopback,
    int? SampleRate = null,
    int? Channels = null,
    long? Sequence = null,
    IReadOnlyList<float>? Samples = null);
