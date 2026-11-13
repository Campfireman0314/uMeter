using System.Runtime.InteropServices;
using DesktopLoudnessMeter.Benchmarks;

BenchmarkOptions options;
try
{
    options = BenchmarkOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

Console.WriteLine("uMeter analyzer benchmark");
Console.WriteLine(
    $"Environment: {RuntimeInformation.FrameworkDescription}, OS {RuntimeInformation.OSDescription}, " +
    $"process {(Environment.Is64BitProcess ? "x64" : "x86")}, {Environment.ProcessorCount} logical CPU(s)");
Console.WriteLine(
    $"Started (UTC): {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}  " +
    $"build: {(Environment.GetEnvironmentVariable("CONFIGURATION") ?? "see csproj")}");

// Reported before any section so an analyzer-only run still says whether the numbers came from the
// native backend or the managed fallback.
TruePeakBenchmark.TryCreateNativeProbe(out var nativeBackend);
Console.WriteLine($"Native true-peak backend: {nativeBackend}");
Console.WriteLine();

if (options.RunAnalyzer)
{
    AnalyzerBenchmark.Run(options);
}

if (options.RunGate)
{
    GateBenchmark.Run(options);
}

if (options.RunTruePeak)
{
    TruePeakBenchmark.Run(options.SampleRate, options.Channels, nativeBackend);
}

Console.WriteLine("Done.");
return 0;
