using System.IO;

namespace DesktopLoudnessMeter.Tests.WebViewMigration;

public sealed class NativeBoundaryBaselineTests
{
    [Fact]
    public void CurrentReadmeDocumentsOptionalNativeManagedFallback()
    {
        var readme = ReadRepositoryFile("README.md");

        Assert.Contains("optional native RfRapid", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("managed fallback", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("automatically falls back", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CurrentAnalyzerFallsBackWhenNativeTruePeakCannotLoad()
    {
        var analyzer = ReadRepositoryFile("src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs");

        Assert.Contains("NativeRfRapidTruePeakEstimator?", analyzer);
        Assert.Contains("TruePeakEstimator", analyzer);
        Assert.Contains("DllNotFoundException", analyzer);
        Assert.Contains("EntryPointNotFoundException", analyzer);
        Assert.Contains("BadImageFormatException", analyzer);
        Assert.Contains("_nativeTruePeakDisabled = true", analyzer);
        Assert.Contains("return false", analyzer);
    }

    [Fact]
    public void NativeBoundaryBaselineRecordsMainAndReferenceBehavior()
    {
        var baseline = MigrationDocumentReader.Read("native-boundary-baseline.md");

        Assert.Contains("managed fallback", baseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NativePcmAnalyzer", baseline);
        Assert.Contains("mandatory-native-drift", baseline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DesktopLoudnessMeter.Native.dll", baseline);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "desktop-loudness-meter.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
