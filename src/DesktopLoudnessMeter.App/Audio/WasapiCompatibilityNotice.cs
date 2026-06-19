namespace DesktopLoudnessMeter.App.Audio;

internal static class WasapiCompatibilityNotice
{
    private static readonly Version MinimumAttemptVersion = new(10, 0, ProcessLoopbackInterop.MinimumAttemptBuild);
    private static readonly Version DocumentedSupportVersion = new(10, 0, ProcessLoopbackInterop.DocumentedSupportBuild);

    public static string? Create(Version osVersion)
    {
        if (osVersion >= DocumentedSupportVersion)
        {
            return null;
        }

        var versionText = osVersion.Build > 0
            ? $"{osVersion.Major}.{osVersion.Minor}.{osVersion.Build}"
            : $"{osVersion.Major}.{osVersion.Minor}";

        if (osVersion >= MinimumAttemptVersion)
        {
            return $"Advanced WASAPI meters are experimental on Windows {versionText}: uMeter will try LUFS, true peak, waveform, spectrum, and stereo detail, but Microsoft documents process-loopback support on Windows 10 build {ProcessLoopbackInterop.DocumentedSupportBuild} or later. Peak rows may still work.";
        }

        return $"Advanced WASAPI meters are unavailable on Windows {versionText}: LUFS, true peak, waveform, spectrum, and stereo detail require Windows 10 build {ProcessLoopbackInterop.MinimumAttemptBuild} or later to attempt. Peak rows may still work.";
    }
}
