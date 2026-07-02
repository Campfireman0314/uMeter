using System.Buffers.Binary;
using System.Text.Json;

namespace DesktopLoudnessMeter.Protocol;

public static class LocalLoopbackProtocol
{
    public const string ProtocolVersion = "desktop-loudness-meter.loopback.v1";

    public static class PacketTypes
    {
        public const string Register = "register";
        public const string Heartbeat = "heartbeat";
        public const string Rename = "rename";
        public const string Audio = "audio";
        public const string Close = "close";
        public const string Ack = "ack";
    }

    public static class SourceKinds
    {
        public const string Loopback = "loopback";
        public const string LocalLoopback = "local-loopback";
        public const string Plugin = "plugin";
    }

    public static class Fields
    {
        public const string Protocol = "protocol";
        public const string Type = "type";
        public const string Handle = "handle";
        public const string DisplayName = "displayName";
        public const string SourceKind = "sourceKind";
        public const string SampleRate = "sampleRate";
        public const string Channels = "channels";
        public const string Sequence = "sequence";
        public const string SamplesBase64 = "samplesBase64";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] EncodeRegister(
        string handle,
        string displayName,
        LocalLoopbackSourceKind sourceKind,
        int sampleRate,
        int channels)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Register,
            Handle = handle,
            DisplayName = displayName,
            SourceKind = ToWireSourceKind(sourceKind),
            SampleRate = sampleRate,
            Channels = channels
        });
    }

    public static byte[] EncodeHeartbeat(string handle)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Heartbeat,
            Handle = handle
        });
    }

    public static byte[] EncodeRename(string handle, string displayName)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Rename,
            Handle = handle,
            DisplayName = displayName
        });
    }

    public static byte[] EncodeAudio(string handle, long sequence, IReadOnlyList<float> samples)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Audio,
            Handle = handle,
            Sequence = sequence,
            SamplesBase64 = EncodeSamples(samples)
        });
    }

    public static byte[] EncodeClose(string handle)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Close,
            Handle = handle
        });
    }

    public static byte[] EncodeAck(string handle)
    {
        return Encode(new WirePacket
        {
            Type = PacketTypes.Ack,
            Handle = handle
        });
    }

    public static bool TryDecode(ReadOnlySpan<byte> bytes, out LocalLoopbackPacket packet)
    {
        packet = default!;
        try
        {
            var wire = JsonSerializer.Deserialize<WirePacket>(bytes, JsonOptions);
            if (wire is null || !TryConvert(wire, out packet))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException or OverflowException)
        {
            return false;
        }
    }

    public static bool IsValidHandle(string? handle)
    {
        return !string.IsNullOrWhiteSpace(handle)
            && handle.Length <= 128
            && handle.All(character => !char.IsControl(character));
    }

    private static byte[] Encode(WirePacket packet)
    {
        packet.Protocol = ProtocolVersion;
        return JsonSerializer.SerializeToUtf8Bytes(packet, JsonOptions);
    }

    private static bool TryConvert(WirePacket wire, out LocalLoopbackPacket packet)
    {
        packet = default!;
        if (!string.Equals(wire.Protocol, ProtocolVersion, StringComparison.Ordinal)
            || !IsValidHandle(wire.Handle))
        {
            return false;
        }

        if (!TryParseType(wire.Type, out var type))
        {
            return false;
        }

        var sourceKind = ParseSourceKind(wire.SourceKind);
        return type switch
        {
            LocalLoopbackPacketType.Register => TryCreateRegister(wire, sourceKind, out packet),
            LocalLoopbackPacketType.Rename => TryCreateRename(wire, sourceKind, out packet),
            LocalLoopbackPacketType.Audio => TryCreateAudio(wire, sourceKind, out packet),
            LocalLoopbackPacketType.Heartbeat or LocalLoopbackPacketType.Close or LocalLoopbackPacketType.Ack => TryCreateSimple(wire, type, sourceKind, out packet),
            _ => false
        };
    }

    private static bool TryCreateRegister(WirePacket wire, LocalLoopbackSourceKind sourceKind, out LocalLoopbackPacket packet)
    {
        packet = default!;
        if (string.IsNullOrWhiteSpace(wire.DisplayName)
            || !IsSupportedSampleRate(wire.SampleRate)
            || !IsSupportedChannelCount(wire.Channels))
        {
            return false;
        }

        packet = new LocalLoopbackPacket(
            LocalLoopbackPacketType.Register,
            wire.Handle!,
            wire.DisplayName.Trim(),
            sourceKind,
            wire.SampleRate,
            wire.Channels);
        return true;
    }

    private static bool TryCreateRename(WirePacket wire, LocalLoopbackSourceKind sourceKind, out LocalLoopbackPacket packet)
    {
        packet = default!;
        if (string.IsNullOrWhiteSpace(wire.DisplayName))
        {
            return false;
        }

        packet = new LocalLoopbackPacket(
            LocalLoopbackPacketType.Rename,
            wire.Handle!,
            wire.DisplayName.Trim(),
            sourceKind);
        return true;
    }

    private static bool TryCreateAudio(WirePacket wire, LocalLoopbackSourceKind sourceKind, out LocalLoopbackPacket packet)
    {
        packet = default!;
        if (string.IsNullOrWhiteSpace(wire.SamplesBase64)
            || !TryDecodeSamples(wire.SamplesBase64, out var samples))
        {
            return false;
        }

        packet = new LocalLoopbackPacket(
            LocalLoopbackPacketType.Audio,
            wire.Handle!,
            SourceKind: sourceKind,
            Sequence: wire.Sequence,
            Samples: samples);
        return true;
    }

    private static bool TryCreateSimple(WirePacket wire, LocalLoopbackPacketType type, LocalLoopbackSourceKind sourceKind, out LocalLoopbackPacket packet)
    {
        packet = new LocalLoopbackPacket(type, wire.Handle!, SourceKind: sourceKind);
        return true;
    }

    private static bool TryParseType(string? value, out LocalLoopbackPacketType type)
    {
        type = value?.Trim().ToLowerInvariant() switch
        {
            PacketTypes.Register => LocalLoopbackPacketType.Register,
            PacketTypes.Heartbeat => LocalLoopbackPacketType.Heartbeat,
            PacketTypes.Rename => LocalLoopbackPacketType.Rename,
            PacketTypes.Audio => LocalLoopbackPacketType.Audio,
            PacketTypes.Close => LocalLoopbackPacketType.Close,
            PacketTypes.Ack => LocalLoopbackPacketType.Ack,
            _ => default
        };
        return value is not null
            && (type != default || string.Equals(value.Trim(), PacketTypes.Register, StringComparison.OrdinalIgnoreCase));
    }

    private static LocalLoopbackSourceKind ParseSourceKind(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            SourceKinds.Plugin => LocalLoopbackSourceKind.Plugin,
            SourceKinds.Loopback or SourceKinds.LocalLoopback => LocalLoopbackSourceKind.Loopback,
            _ => LocalLoopbackSourceKind.Loopback
        };
    }

    private static string ToWireSourceKind(LocalLoopbackSourceKind sourceKind)
    {
        return sourceKind == LocalLoopbackSourceKind.Plugin ? SourceKinds.Plugin : SourceKinds.Loopback;
    }

    private static bool IsSupportedSampleRate(int? sampleRate)
    {
        return sampleRate is >= 8000 and <= 384000;
    }

    private static bool IsSupportedChannelCount(int? channels)
    {
        return channels is 1 or 2;
    }

    private static string EncodeSamples(IReadOnlyList<float> samples)
    {
        var bytes = new byte[checked(samples.Count * sizeof(float))];
        for (var index = 0; index < samples.Count; index++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(index * sizeof(float), sizeof(float)), samples[index]);
        }

        return Convert.ToBase64String(bytes);
    }

    private static bool TryDecodeSamples(string value, out IReadOnlyList<float> samples)
    {
        samples = [];
        var bytes = Convert.FromBase64String(value);
        if (bytes.Length == 0 || bytes.Length % sizeof(float) != 0)
        {
            return false;
        }

        var decoded = new float[bytes.Length / sizeof(float)];
        for (var index = 0; index < decoded.Length; index++)
        {
            decoded[index] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(index * sizeof(float), sizeof(float)));
        }

        samples = decoded;
        return true;
    }

    private sealed class WirePacket
    {
        public string? Protocol { get; set; }

        public string? Type { get; set; }

        public string? Handle { get; set; }

        public string? DisplayName { get; set; }

        public string? SourceKind { get; set; }

        public int? SampleRate { get; set; }

        public int? Channels { get; set; }

        public long? Sequence { get; set; }

        public string? SamplesBase64 { get; set; }
    }
}
