namespace DesktopLoudnessMeter.SenderProbe;

public static class SenderProbeProgram
{
    public static async Task<int> MainAsync(string[] args)
    {
        try
        {
            if (args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)))
            {
                PrintUsage();
                return 0;
            }

            var options = SenderProbeOptions.Parse(args);
            Console.WriteLine($"uMeter sender probe: {options.DisplayName}");
            await SenderProbeRunner.RunAsync(options).ConfigureAwait(false);
            Console.WriteLine("Sender probe finished.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
uMeter sender probe

Usage:
  dotnet run --project .\src\DesktopLoudnessMeter.SenderProbe -- [options]

Options:
  --name <text>           Source display name. Default: Sender Probe
  --handle <id>           Stable sender handle. Default: persisted local probe handle
  --sample-rate <hz>      Sample rate. Default: 48000
  --channels <1|2>        Channel count. Default: 2
  --seconds <value>       Duration in seconds. Default: 10
  --frequency <hz>        Sine frequency. Default: 440
  --amplitude <0..1>      Linear amplitude. Default: 0.25
  --frames <count>        Frames per packet. Default: 480
  --rename-after <text>   Rename to this display name halfway through the run
  --port <number>         App UDP loopback port. Default: 38173
""");
    }
}
