namespace DesktopLoudnessMeter.Tests.WebViewMigration;

public sealed class DetailSettingsScenarioTraceTests
{
    [Fact]
    public void ScenarioTraceDocumentDefinesDetailSettingsCases()
    {
        var content = ReadScenarioTraces();

        string[] settings =
        [
            "true peak",
            "waveform mode",
            "spectrum mode",
            "collision visibility",
            "detail module order",
            "loudness-clock settings"
        ];

        foreach (var setting in settings)
        {
            Assert.Contains(setting, content, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("global", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("per-source persisted", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("runtime-only", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reset behavior", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScenarioTraceDocumentDefinesRenderingGateCases()
    {
        var content = ReadScenarioTraces();

        Assert.Contains("hidden waveform", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hidden spectrum", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hidden loudness-clock", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("active gate", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadScenarioTraces()
    {
        return MigrationDocumentReader.Read("scenario-traces.md");
    }
}
