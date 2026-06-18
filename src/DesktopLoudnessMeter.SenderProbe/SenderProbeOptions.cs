using System.Globalization;
using DesktopLoudnessMeter.SenderKit;

namespace DesktopLoudnessMeter.SenderProbe;

public sealed record SenderProbeOptions
{
    public string DisplayName { get; init; } = "Sender Probe";

    public string? Handle { get; init; }

    public int SampleRate { get; init; } = 48000;

    public int Channels { get; init; } = 2;

    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(10);

    public double FrequencyHz { get; init; } = 440;

    public float Amplitude { get; init; } = 0.25f;

    public int FramesPerPacket { get; init; } = 480;

    public int BufferedPackets { get; init; } = 8;

    public string? RenameAfter { get; init; }

    public int Port { get; init; } = LoopbackSenderOptions.DefaultPort;

    public string IdentityPath { get; init; } = DefaultIdentityPath();

    public static SenderProbeOptions Parse(params string[] args)
    {
        var options = new SenderProbeOptions();
        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            options = option switch
            {
                "--name" => options with { DisplayName = ReadString(args, ref index, option) },
                "--handle" => options with { Handle = ReadString(args, ref index, option) },
                "--sample-rate" => options with { SampleRate = ReadInt(args, ref index, option) },
                "--channels" => options with { Channels = ReadInt(args, ref index, option) },
                "--seconds" => options with { Duration = TimeSpan.FromSeconds(ReadDouble(args, ref index, option)) },
                "--frequency" => options with { FrequencyHz = ReadDouble(args, ref index, option) },
                "--amplitude" => options with { Amplitude = (float)ReadDouble(args, ref index, option) },
                "--frames" => options with { FramesPerPacket = ReadInt(args, ref index, option) },
                "--buffered-packets" => options with { BufferedPackets = ReadInt(args, ref index, option) },
                "--rename-after" => options with { RenameAfter = ReadString(args, ref index, option) },
                "--port" => options with { Port = ReadInt(args, ref index, option) },
                "--identity" => options with { IdentityPath = ReadString(args, ref index, option) },
                _ => throw new ArgumentException($"Unknown sender probe option: {option}")
            };
        }

        options.Validate();
        return options;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new ArgumentException("Display name must be non-empty.");
        }

        if (SampleRate is < 8000 or > 384000)
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRate), "Sample rate must be between 8000 and 384000 Hz.");
        }

        if (Channels is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(Channels), "Channels must be 1 or 2.");
        }

        if (Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Duration), "Duration must be positive.");
        }

        if (FrequencyHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHz), "Frequency must be positive.");
        }

        if (Amplitude is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Amplitude), "Amplitude must be between 0 and 1.");
        }

        if (FramesPerPacket <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FramesPerPacket), "Frames per packet must be positive.");
        }

        if (BufferedPackets <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BufferedPackets), "Buffered packet count must be positive.");
        }
    }

    private static string ReadString(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        return args[index];
    }

    private static int ReadInt(string[] args, ref int index, string option)
    {
        var value = ReadString(args, ref index, option);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"{option} requires an integer value.");
    }

    private static double ReadDouble(string[] args, ref int index, string option)
    {
        var value = ReadString(args, ref index, option);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"{option} requires a numeric value.");
    }

    private static string DefaultIdentityPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "uMeter", "sender-probe.identity");
    }
}
