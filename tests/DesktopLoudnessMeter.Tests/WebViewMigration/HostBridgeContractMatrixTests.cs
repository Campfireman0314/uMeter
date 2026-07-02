namespace DesktopLoudnessMeter.Tests.WebViewMigration;

public sealed class HostBridgeContractMatrixTests
{
    [Fact]
    public void ContractMatrixContainsRequiredAuditColumns()
    {
        var content = ReadMigrationDocument("hostbridge-contract-matrix.md");

        string[] requiredColumns =
        [
            "Command",
            "Payload Fields",
            "Result Shape",
            "Frontend Caller",
            "Mock Implementation",
            "C# Bridge Handler",
            "Host Execution Path",
            "Test Coverage",
            "Status"
        ];

        foreach (var column in requiredColumns)
        {
            Assert.Contains(column, content);
        }
    }

    [Fact]
    public void ContractMatrixRecordsKnownAliasMismatchAsFailure()
    {
        var content = ReadMigrationDocument("hostbridge-contract-matrix.md");

        Assert.Contains("setSourceAlias", content);
        Assert.Contains("contract-failure", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mockHostBridge.ts", content);
        Assert.Contains("HostBridgeController.cs", content);
    }

    [Fact]
    public void StateOwnershipMapDefinesAllowedOwnershipKinds()
    {
        var content = ReadMigrationDocument("state-ownership-map.md");

        Assert.Contains("host-owned", content);
        Assert.Contains("WebView-derived", content);
        Assert.Contains("persisted-workspace-owned", content);
        Assert.Contains("transient-view-owned", content);
    }

    private static string ReadMigrationDocument(string fileName)
    {
        return MigrationDocumentReader.Read(fileName);
    }
}
