using System.Net;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.SenderKit;

public sealed class LoopbackSenderOptions
{
    public const int DefaultPort = 38173;
    private const int MaxEstimatedUdpPayloadBytes = 48_000;

    public LoopbackSenderOptions(
        string handle,
        string displayName,
        LocalLoopbackSourceKind sourceKind,
        int sampleRate,
        int channels,
        int port = DefaultPort)
    {
        if (!LocalLoopbackProtocol.IsValidHandle(handle))
        {
            throw new ArgumentException("Sender handle must be non-empty, 128 characters or fewer, and contain no control characters.", nameof(handle));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name must be non-empty.", nameof(displayName));
        }

        if (sampleRate is < 8000 or > 384000)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be between 8000 and 384000 Hz.");
        }

        if (channels is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Only mono and stereo sender probes are supported.");
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "UDP port must be between 1 and 65535.");
        }

        Handle = handle.Trim();
        DisplayName = displayName.Trim();
        SourceKind = sourceKind;
        SampleRate = sampleRate;
        Channels = channels;
        Port = port;
    }

    public string Handle { get; }

    public string DisplayName { get; internal set; }

    public LocalLoopbackSourceKind SourceKind { get; }

    public int SampleRate { get; }

    public int Channels { get; }

    public int Port { get; }

    public IPAddress Address { get; init; } = IPAddress.Loopback;

    public int FramesPerPacket { get; init; } = 480;

    public int BufferedPacketCapacity { get; init; } = 8;

    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(1);

    internal int MaxSamplesPerPacket => checked(FramesPerPacket * Channels);

    internal void ValidateRuntimeOptions()
    {
        if (!IPAddress.IsLoopback(Address))
        {
            throw new ArgumentException("Sender address must be a loopback address.", nameof(Address));
        }

        if (FramesPerPacket <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FramesPerPacket), "Frames per packet must be positive.");
        }

        if (EstimateV1AudioPacketBytes() > MaxEstimatedUdpPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FramesPerPacket),
                $"Frames per packet is too large for the v1 UDP JSON transport. Keep the estimated packet below {MaxEstimatedUdpPayloadBytes} bytes.");
        }

        if (BufferedPacketCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BufferedPacketCapacity), "Buffered packet capacity must be positive.");
        }

        if (HeartbeatInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(HeartbeatInterval), "Heartbeat interval must be positive.");
        }
    }

    private int EstimateV1AudioPacketBytes()
    {
        var rawSampleBytes = checked(MaxSamplesPerPacket * sizeof(float));
        var base64Bytes = ((rawSampleBytes + 2) / 3) * 4;
        return base64Bytes + 256;
    }
}
