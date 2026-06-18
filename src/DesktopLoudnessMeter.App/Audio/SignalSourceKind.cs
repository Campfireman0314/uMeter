namespace DesktopLoudnessMeter.App.Audio;

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
        return kind switch
        {
            SignalSourceKind.WasapiSession => "WASAPI",
            SignalSourceKind.LocalLoopback => "Loopback",
            SignalSourceKind.Plugin => "Plugin",
            _ => "Unknown"
        };
    }
}
