using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

namespace DesktopLoudnessMeter.Tests.Controls;

/// <summary>
/// Focus contracts for the themed chrome styles in App.xaml. The static checks
/// prove the shipped markup declares the treatment; the live checks parse that
/// same markup and verify a real control shows the ring while keyboard focused.
/// </summary>
public sealed class ThemedControlFocusContractTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly string[] ThemedStyleKeys =
    [
        "Button",
        "DetailModuleToggleButton",
        "CheckBox",
        "ComboBox",
        "SignalMeasurementModeComboBox"
    ];

    [Fact]
    public void ThemedCoreControlsDrawAFocusRingFromTheThemeAccentBrush()
    {
        var document = LoadAppResources();

        foreach (var styleKey in ThemedStyleKeys)
        {
            var style = FindStyle(document, styleKey);
            var ring = Assert.Single(
                style.Descendants(Presentation + "Border")
                    .Where(element => (string?)element.Attribute(Xaml + "Name") == "FocusRing"));

            Assert.Equal("{DynamicResource AccentBrush}", (string?)ring.Attribute("BorderBrush"));
            Assert.Equal("0", (string?)ring.Attribute("Opacity"));

            var focusedTrigger = style.Descendants(Presentation + "Trigger")
                .Single(trigger => (string?)trigger.Attribute("Property") == "IsKeyboardFocused");
            Assert.Contains(
                focusedTrigger.Elements(Presentation + "Setter"),
                setter => (string?)setter.Attribute("TargetName") == "FocusRing"
                    && (string?)setter.Attribute("Property") == "Opacity"
                    && (string?)setter.Attribute("Value") == "1");
        }
    }

    [Fact]
    public void ThemedCoreControlsKeepTheSystemFocusVisualForHighContrast()
    {
        var document = LoadAppResources();

        foreach (var styleKey in ThemedStyleKeys)
        {
            var style = FindStyle(document, styleKey);
            Assert.Contains(
                style.Elements(Presentation + "Setter"),
                setter => (string?)setter.Attribute("Property") == "FocusVisualStyle"
                    && ((string?)setter.Attribute("Value"))?.Contains("SystemParameters.FocusVisualStyleKey", StringComparison.Ordinal) == true);
        }
    }

    [Fact]
    public void ThemedCoreControlsShowTheFocusRingOnlyWhileKeyboardFocused()
    {
        var document = LoadAppResources();

        StaControlHost.Run(() =>
        {
            foreach (var styleKey in ThemedStyleKeys)
            {
                var style = ParseStyle(document, FindStyle(document, styleKey));
                var control = CreateControl(styleKey);
                control.Resources["LocalizedEnumConverter"] =
                    new DesktopLoudnessMeter.App.Localization.LocalizedEnumConverter();
                control.Style = style;
                control.Width = 120;
                control.Height = 28;

                StaControlHost.Arrange(control, 120, 28);
                using var host = StaControlHost.CreateFocusHost(control);
                var ring = FindFocusRing(control);
                Assert.Equal(0d, ring.Opacity);

                Assert.True(control.Focus());
                control.UpdateLayout();

                ring = FindFocusRing(control);
                Assert.Equal(1d, ring.Opacity);
                Assert.Equal(2d, ring.BorderThickness.Left);
                Assert.NotNull(control.FocusVisualStyle);
            }
        });
    }

    private static Control CreateControl(string styleKey)
    {
        return styleKey switch
        {
            "Button" => new Button { Content = "Reset" },
            "DetailModuleToggleButton" => new ToggleButton(),
            "CheckBox" => new CheckBox { Content = "Eco mode" },
            _ => new ComboBox()
        };
    }

    private static Border FindFocusRing(DependencyObject root)
    {
        var result = FindVisualChild<Border>(root, "FocusRing");
        Assert.NotNull(result);
        return result!;
    }

    private static T? FindVisualChild<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match && match.Name == name)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static Style ParseStyle(XDocument document, XElement style)
    {
        var appAssembly = typeof(DesktopLoudnessMeter.App.Localization.LocalizedEnumConverter).Assembly.GetName().Name;
        var declarations = string.Join(
            " ",
            document.Root!.Attributes()
                .Where(attribute => attribute.IsNamespaceDeclaration)
                .Select(attribute => attribute.Name.NamespaceName.Length == 0
                    ? $"xmlns=\"{QualifyAssembly(attribute.Value, appAssembly)}\""
                    : $"xmlns:{attribute.Name.LocalName}=\"{QualifyAssembly(attribute.Value, appAssembly)}\""));
        var dictionaryMarkup =
            $"<ResourceDictionary {declarations}>" +
            "<localization:LocalizedEnumConverter x:Key=\"LocalizedEnumConverter\" />" +
            style.ToString(SaveOptions.DisableFormatting) +
            "</ResourceDictionary>";

        var dictionary = Assert.IsType<ResourceDictionary>(XamlReader.Parse(dictionaryMarkup));
        return Assert.Single(dictionary.Values.OfType<Style>());
    }

    private static string QualifyAssembly(string namespaceUri, string? appAssembly)
    {
        return namespaceUri.StartsWith("clr-namespace:", StringComparison.Ordinal)
            && !namespaceUri.Contains(";assembly=", StringComparison.Ordinal)
            && appAssembly is not null
                ? $"{namespaceUri};assembly={appAssembly}"
                : namespaceUri;
    }

    private static XElement FindStyle(XDocument document, string key)
    {
        var styles = document.Descendants(Presentation + "Style").ToList();
        var style = key is "Button" or "CheckBox" or "ComboBox"
            ? styles.FirstOrDefault(element =>
                (string?)element.Attribute("TargetType") == $"{{x:Type {key}}}" && element.Attribute(Xaml + "Key") is null)
            : styles.FirstOrDefault(element => (string?)element.Attribute(Xaml + "Key") == key);

        Assert.NotNull(style);
        return style!;
    }

    private static XDocument LoadAppResources()
    {
        var repositoryRoot = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(repositoryRoot, "src/DesktopLoudnessMeter.App/App.xaml"));
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
