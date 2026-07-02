using System.IO;

namespace DesktopLoudnessMeter.Tests.WebViewMigration;

internal static class MigrationDocumentReader
{
    public static string Read(string fileName)
    {
        var path = Path.Combine(FindRepositoryRoot(), "docs", "webview-migration", fileName);
        Assert.True(File.Exists(path), $"Missing migration document: {path}");
        return File.ReadAllText(path);
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
