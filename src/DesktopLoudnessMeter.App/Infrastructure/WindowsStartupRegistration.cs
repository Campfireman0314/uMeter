using Microsoft.Win32;

namespace DesktopLoudnessMeter.App.Infrastructure;

public sealed class WindowsStartupRegistration : IStartupRegistration
{
    internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "uMeter";
    internal const string BackgroundArgument = "--background";

    private readonly Func<string?> _getExecutablePath;

    public WindowsStartupRegistration(Func<string?>? getExecutablePath = null)
    {
        _getExecutablePath = getExecutablePath ?? (() => Environment.ProcessPath);
    }

    public void Configure(bool enabled, bool startHidden)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (!enabled)
        {
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = _getExecutablePath();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Could not resolve the uMeter executable path for Windows startup.");
        }

        key?.SetValue(ValueName, BuildCommand(executablePath, startHidden), RegistryValueKind.String);
    }

    internal static string BuildCommand(string executablePath, bool startHidden)
    {
        var command = $"\"{executablePath.Trim()}\"";
        return startHidden ? $"{command} {BackgroundArgument}" : command;
    }
}
