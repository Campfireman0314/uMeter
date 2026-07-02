namespace DesktopLoudnessMeter.Tests.WebViewMigration;

public sealed class BuildPackagingBaselineTests
{
    [Fact]
    public void ScenarioTracesRecordBuildAndAssetBaselines()
    {
        var traces = MigrationDocumentReader.Read("scenario-traces.md");

        string[] expectedTerms =
        [
            "restore",
            "build",
            "test",
            "publish",
            "development asset",
            "packaged asset",
            "missing asset"
        ];

        foreach (var term in expectedTerms)
        {
            Assert.Contains(term, traces, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void NativeBoundaryBaselineRecordsPackagingDecisionStates()
    {
        var baseline = MigrationDocumentReader.Read("native-boundary-baseline.md");

        Assert.Contains("preserved", baseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("intentional", baseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fatal", baseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("05-packaging-native-boundary-and-retirement", baseline);
    }
}
