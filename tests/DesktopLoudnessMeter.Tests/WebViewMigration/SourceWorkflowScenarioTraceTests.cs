namespace DesktopLoudnessMeter.Tests.WebViewMigration;

public sealed class SourceWorkflowScenarioTraceTests
{
    [Fact]
    public void FakeSourceIdentitySurvivesDisappearReconnectAndRename()
    {
        var original = FakeSourceScenarioHarness.Plugin("plugin:lead", "Lead");

        var disconnected = original.Disappear();
        var reconnected = disconnected.Reconnect("Lead Vocal");

        Assert.False(disconnected.IsPresent);
        Assert.True(reconnected.IsPresent);
        Assert.Equal(original.SourceKey, reconnected.SourceKey);
        Assert.NotEqual(original.DisplayName, reconnected.DisplayName);
    }

    [Fact]
    public void FakeSourceHarnessModelsExpectedSourceKinds()
    {
        var sources = new[]
        {
            FakeSourceScenarioHarness.Wasapi("wasapi:42", "System Audio", 42),
            FakeSourceScenarioHarness.LocalLoopback("loopback:main", "Local Loopback"),
            FakeSourceScenarioHarness.Plugin("plugin:lead", "Lead")
        };

        Assert.Equal(["wasapi", "local-loopback", "plugin"], sources.Select(source => source.SourceKind).ToArray());
        Assert.Equal(42, sources[0].ProcessId);
        Assert.Null(sources[1].ProcessId);
        Assert.Null(sources[2].ProcessId);
    }

    [Fact]
    public void ScenarioTraceDocumentDefinesSourceWorkflowCases()
    {
        var content = ReadScenarioTraces();

        string[] workflows =
        [
            "refresh",
            "selection",
            "alias edit",
            "hide",
            "restore",
            "reorder",
            "reset",
            "forget",
            "show hidden",
            "reconnect"
        ];

        foreach (var workflow in workflows)
        {
            Assert.Contains(workflow, content, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Initial State", content);
        Assert.Contains("Action", content);
        Assert.Contains("Expected Host State", content);
        Assert.Contains("Expected Persisted State", content);
        Assert.Contains("Expected WebView Projection", content);
    }

    private static string ReadScenarioTraces()
    {
        return MigrationDocumentReader.Read("scenario-traces.md");
    }
}
