using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml.Linq;
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
        Assert.Contains("gamewindow.baml", resources);
        Assert.Contains("controls/dancingsheepview.baml", resources);
    }

    [Fact]
    public void GameAudioSourceSelectorRendersTheSourceDisplayTitle()
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            repositoryRoot,
            "src/DesktopLoudnessMeter.App/GameWindow.xaml"));
        var selector = Assert.Single(document.Descendants().Where(element =>
            element.Name.LocalName == "ComboBox"
            && element.Attributes().Any(attribute =>
                attribute.Name.LocalName == "ItemsSource"
                && attribute.Value == "{Binding AudioSources}")));

        Assert.Contains(selector.Descendants(), element =>
            element.Name.LocalName == "TextBlock"
            && element.Attributes().Any(attribute =>
                attribute.Name.LocalName == "Text"
                && attribute.Value == "{Binding DisplayTitle}"));
    }

    [Fact]
    public void GameWindowDoesNotApplyGlobalLayerDepthConcealment()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src/DesktopLoudnessMeter.App/GameWindow.xaml"));

        Assert.DoesNotContain("IsTwoOrMoreLayersBelowTop", xaml);
        Assert.DoesNotContain("#FF3A3D3E", xaml);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "desktop-loudness-meter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
