using System.Xml.Linq;
using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.Tests.Controls;

/// <summary>
/// Accessible-name contracts for the inline detail surface (MainWindow.xaml) and
/// the popup detail surface (DetailWindow.xaml). Both surfaces must expose the
/// same meaningful automation names so keyboard and screen-reader users get the
/// same information in either view.
/// </summary>
public sealed class AccessibleNameContractTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly string[] DiagramSettingsNames =
    [
        "Clock settings",
        "Waveform settings",
        "Spectrum settings",
        "Stereo image scale settings",
        "Collision settings"
    ];

    private static readonly string[] MetricModeNames =
    [
        "Momentary loudness measurement mode",
        "Short-term loudness measurement mode",
        "Integrated loudness measurement mode",
        "True-peak level measurement mode",
        "100 ms RMS level measurement mode"
    ];

    private static readonly string[] SliderNames =
    [
        "Range",
        "Warning",
        "Cycle",
        "Level floor",
        "Prominence",
        "Match tolerance"
    ];

    [Fact]
    public void IconOnlyChromeButtonsCarryTheSameNameAsTheirTooltip()
    {
        foreach (var window in new[] { "MainWindow.xaml", "DetailWindow.xaml", "GlobalSettingsWindow.xaml" })
        {
            foreach (var button in LoadWindow(window).Descendants(Presentation + "Button"))
            {
                var style = (string?)button.Attribute("Style");
                if (style?.Contains("ChromeIconButton", StringComparison.Ordinal) != true)
                {
                    continue;
                }

                var name = AutomationName(button);
                Assert.False(
                    string.IsNullOrWhiteSpace(name),
                    $"{window}: icon-only button needs AutomationProperties.Name.");

                var tooltip = (string?)button.Attribute("ToolTip");
                if (!string.IsNullOrEmpty(tooltip) && !tooltip.StartsWith('{'))
                {
                    Assert.Equal(tooltip, name);
                }
            }
        }
    }

    [Fact]
    public void BothDetailSurfacesNameTheDiagramSettingsCommands()
    {
        foreach (var window in new[] { "MainWindow.xaml", "DetailWindow.xaml" })
        {
            var names = AutomationNames(LoadWindow(window));
            foreach (var expected in DiagramSettingsNames)
            {
                Assert.Contains(expected, names);
            }
        }
    }

    [Fact]
    public void BothDetailSurfacesNameTheCompactMetricSelectors()
    {
        foreach (var window in new[] { "MainWindow.xaml", "DetailWindow.xaml" })
        {
            var document = LoadWindow(window);
            var names = AutomationNames(document);
            foreach (var expected in MetricModeNames)
            {
                Assert.Contains(expected, names);
            }

            var compactSelectors = document.Descendants(Presentation + "ComboBox")
                .Where(element => ((string?)element.Attribute("Style"))?.Contains("SignalMeasurementModeComboBox", StringComparison.Ordinal) == true)
                .ToList();
            Assert.Equal(MetricModeNames.Length, compactSelectors.Count);
            Assert.All(compactSelectors, selector => Assert.False(string.IsNullOrWhiteSpace(AutomationName(selector))));
        }
    }

    [Fact]
    public void BothDetailSurfacesNameTheSettingsSliders()
    {
        foreach (var window in new[] { "MainWindow.xaml", "DetailWindow.xaml" })
        {
            var document = LoadWindow(window);
            var names = AutomationNames(document);
            foreach (var expected in SliderNames)
            {
                Assert.Contains(expected, names);
            }

            var sliders = document.Descendants()
                .Where(element => element.Name.LocalName is "EditableValueSlider" or "DbRangeSlider")
                .ToList();
            Assert.NotEmpty(sliders);
            Assert.All(sliders, slider => Assert.False(
                string.IsNullOrWhiteSpace(AutomationName(slider)),
                $"{window}: slider {slider.Name.LocalName} needs AutomationProperties.Name."));
        }
    }

    [Fact]
    public void EveryAccessibleNameIsLocalizable()
    {
        var uncovered = new List<string>();
        foreach (var window in new[] { "MainWindow.xaml", "DetailWindow.xaml", "GlobalSettingsWindow.xaml" })
        {
            foreach (var element in LoadWindow(window).Descendants())
            {
                var name = AutomationName(element);
                if (string.IsNullOrWhiteSpace(name)
                    || name.StartsWith('{')
                    || LocalizationManager.SourceTexts.Contains(name))
                {
                    continue;
                }

                uncovered.Add($"{window}: {name}");
            }
        }

        Assert.Empty(uncovered);
    }

    private static string? AutomationName(XElement element)
    {
        return element.Attributes()
            .Where(attribute => attribute.Name.LocalName == "AutomationProperties.Name")
            .Select(attribute => attribute.Value)
            .FirstOrDefault();
    }

    private static List<string> AutomationNames(XDocument document)
    {
        return document.Descendants()
            .Select(AutomationName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();
    }

    private static XDocument LoadWindow(string fileName)
    {
        var repositoryRoot = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(repositoryRoot, "src/DesktopLoudnessMeter.App", fileName));
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
