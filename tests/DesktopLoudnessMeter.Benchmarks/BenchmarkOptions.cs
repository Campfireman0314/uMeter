using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Benchmarks;

/// <summary>
/// Command line configuration for the benchmark executable. Every dimension that influences timing
/// or allocation is explicit so a before/after comparison can be reproduced exactly.
/// </summary>
internal sealed record BenchmarkOptions
{
    public const int DefaultSampleRate = 48000;
    public const int DefaultChannels = 2;
    public const int DefaultBlockFrames = 480;
    public const int DefaultWarmupRuns = 1;
    public const int DefaultRepeats = 3;
    public const int DefaultOversampling = 4;
    public static readonly double[] DefaultDurationsSeconds = [10, 60];

    public int SampleRate { get; init; } = DefaultSampleRate;

    public int Channels { get; init; } = DefaultChannels;

    public int BlockFrames { get; init; } = DefaultBlockFrames;

    public int WarmupRuns { get; init; } = DefaultWarmupRuns;

    public int Repeats { get; init; } = DefaultRepeats;

    public int Oversampling { get; init; } = DefaultOversampling;

    public double[] DurationsSeconds { get; init; } = DefaultDurationsSeconds;

    public PcmAnalysisPerformanceMode[] Modes { get; init; } =
        [PcmAnalysisPerformanceMode.Normal, PcmAnalysisPerformanceMode.Eco];

    public bool RunTruePeak { get; init; } = true;

    public bool RunAnalyzer { get; init; } = true;

    public static BenchmarkOptions Parse(string[] args)
    {
        var options = new BenchmarkOptions();
        foreach (var argument in args)
        {
            if (argument is "--analyzer-only")
            {
                options = options with { RunTruePeak = false };
                continue;
            }

            if (argument is "--true-peak-only")
            {
                options = options with { RunAnalyzer = false };
                continue;
            }

            var separator = argument.IndexOf('=');
            if (!argument.StartsWith("--", StringComparison.Ordinal) || separator < 0)
            {
                throw new ArgumentException(
                    $"Unsupported argument '{argument}'. Use --key=value form, for example --seconds=10,60 --block=480.");
            }

            var key = argument[2..separator];
            var value = argument[(separator + 1)..];
            options = key switch
            {
                "sample-rate" => options with { SampleRate = ParsePositiveInt(key, value) },
                "channels" => options with { Channels = ParsePositiveInt(key, value) },
                "block" => options with { BlockFrames = ParsePositiveInt(key, value) },
                "warmup" => options with { WarmupRuns = ParseNonNegativeInt(key, value) },
                "repeats" => options with { Repeats = ParsePositiveInt(key, value) },
                "oversampling" => options with { Oversampling = ParsePositiveInt(key, value) },
                "seconds" => options with { DurationsSeconds = ParseDurations(value) },
                "mode" => options with { Modes = ParseModes(value) },
                _ => throw new ArgumentException($"Unknown benchmark option '--{key}'.")
            };
        }

        if (options.Channels is < 1 or > 2)
        {
            throw new ArgumentException("--channels must be 1 or 2.");
        }

        if (options.Oversampling is not (2 or 4 or 8))
        {
            throw new ArgumentException("--oversampling must be 2, 4 or 8.");
        }

        return options;
    }

    private static int ParsePositiveInt(string key, string value)
    {
        var parsed = ParseNonNegativeInt(key, value);
        return parsed > 0 ? parsed : throw new ArgumentException($"--{key} must be greater than zero.");
    }

    private static int ParseNonNegativeInt(string key, string value)
    {
        return int.TryParse(value, out var parsed) && parsed >= 0
            ? parsed
            : throw new ArgumentException($"--{key} must be a non-negative integer.");
    }

    private static double[] ParseDurations(string value)
    {
        var durations = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => double.TryParse(item, out var seconds) && seconds > 0
                ? seconds
                : throw new ArgumentException($"--seconds values must be positive numbers, got '{item}'."))
            .ToArray();

        return durations.Length > 0 ? durations : throw new ArgumentException("--seconds needs at least one value.");
    }

    private static PcmAnalysisPerformanceMode[] ParseModes(string value)
    {
        var modes = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => Enum.TryParse<PcmAnalysisPerformanceMode>(item, ignoreCase: true, out var mode)
                ? mode
                : throw new ArgumentException($"--mode accepts Normal or Eco, got '{item}'."))
            .Distinct()
            .ToArray();

        return modes.Length > 0 ? modes : throw new ArgumentException("--mode needs at least one value.");
    }
}
