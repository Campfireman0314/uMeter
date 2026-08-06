namespace DesktopLoudnessMeter.App.Audio;

using DesktopLoudnessMeter.App.Localization;

public enum SignalSourceKind
{
    Unknown,
    WasapiSession,
    LocalLoopback,
    Plugin
}

public static class SignalSourceKindExtensions
{
    public static string ToDisplayLabel(this SignalSourceKind kind)
    {
        var label = kind switch
        {
            SignalSourceKind.WasapiSession => "WASAPI",
            SignalSourceKind.LocalLoopback => "Loopback",
            SignalSourceKind.Plugin => "Plugin",
            _ => "Unknown"
        };
        return LocalizationManager.Translate(label);
    }
}
