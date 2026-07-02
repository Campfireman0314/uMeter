namespace DesktopLoudnessMeter.Tests.WebViewMigration;

internal static class FakeSourceScenarioHarness
{
    public static FakeSourceScenarioSource Wasapi(string sourceKey, string displayName, int processId)
    {
        return new FakeSourceScenarioSource(sourceKey, "wasapi", displayName, processId, IsPresent: true);
    }

    public static FakeSourceScenarioSource LocalLoopback(string sourceKey, string displayName)
    {
        return new FakeSourceScenarioSource(sourceKey, "local-loopback", displayName, ProcessId: null, IsPresent: true);
    }

    public static FakeSourceScenarioSource Plugin(string sourceKey, string displayName)
    {
        return new FakeSourceScenarioSource(sourceKey, "plugin", displayName, ProcessId: null, IsPresent: true);
    }
}

internal sealed record FakeSourceScenarioSource(
    string SourceKey,
    string SourceKind,
    string DisplayName,
    int? ProcessId,
    bool IsPresent)
{
    public FakeSourceScenarioSource Disappear()
    {
        return this with { IsPresent = false };
    }

    public FakeSourceScenarioSource Reconnect(string displayName)
    {
        return this with
        {
            DisplayName = displayName,
            IsPresent = true
        };
    }
}
