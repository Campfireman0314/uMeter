using System.Collections;
using System.Reflection;
using System.Resources;
using AppEntryPoint = DesktopLoudnessMeter.App.App;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class WpfResourcePackagingTests
{
    [Fact]
    public void AppAssemblyEmbedsWindowBamlResources()
    {
        var resources = ReadGeneratedResourceKeys(typeof(AppEntryPoint).Assembly);

        Assert.Contains("app.baml", resources);
        Assert.Contains("mainwindow.baml", resources);
        Assert.Contains("detailwindow.baml", resources);
        Assert.Contains("globalsettingswindow.baml", resources);
        Assert.Contains("stereoimagesettingswindow.baml", resources);
    }

    private static HashSet<string> ReadGeneratedResourceKeys(Assembly assembly)
    {
        var resourceName = Assert.Single(
            assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase)));
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new ResourceReader(stream!);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in reader)
        {
            keys.Add((string)entry.Key);
        }

        return keys;
    }
}
