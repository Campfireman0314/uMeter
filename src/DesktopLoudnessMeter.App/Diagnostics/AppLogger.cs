using System.Diagnostics;
using System.IO;

namespace DesktopLoudnessMeter.App.Diagnostics;

public static class AppLogger
{
    private static readonly object Sync = new();

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "uMeter");

    public static string LogPath { get; } = Path.Combine(LogDirectory, "umeter.log");

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    [Conditional("DEBUG")]
    public static void DebugInfo(string message)
    {
        Write("DEBUG", message);
    }

    public static void Error(string message, Exception exception)
    {
        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    private static void Write(string level, string message)
    {
        System.Diagnostics.Debug.WriteLine($"uMeter [{level}] {message}");

        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    LogPath,
                    $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}",
                    System.Text.Encoding.UTF8);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine($"uMeter log write failed: {ex.Message}");
        }
    }
}
