using System.Globalization;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using DesktopLoudnessMeter.App.Controls;
using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.Tests.Localization;

public sealed class LocalizationManagerTests
{
    [Theory]
    [InlineData("zh-CN", LocalizationManager.ChineseLanguageCode)]
    [InlineData("zh-TW", LocalizationManager.ChineseLanguageCode)]
    [InlineData("ja-JP", LocalizationManager.JapaneseLanguageCode)]
    [InlineData("ru-RU", LocalizationManager.RussianLanguageCode)]
    [InlineData("en-US", LocalizationManager.EnglishLanguageCode)]
    [InlineData("de-DE", LocalizationManager.EnglishLanguageCode)]
    public void AutomaticLanguageMapsSupportedCulturesAndFallsBackToEnglish(
        string cultureName,
        string expectedLanguageCode)
    {
        Assert.Equal(
            expectedLanguageCode,
            LocalizationManager.ResolveLanguageCode(
                LocalizationManager.AutomaticLanguageCode,
                CultureInfo.GetCultureInfo(cultureName)));
    }

    [Fact]
    public void ExplicitLanguageOverridesSystemCulture()
    {
        Assert.Equal(
            LocalizationManager.RussianLanguageCode,
            LocalizationManager.ResolveLanguageCode(
                LocalizationManager.RussianLanguageCode,
                CultureInfo.GetCultureInfo("ja-JP")));
    }

    [Fact]
    public void UnknownPreferenceReturnsToAutomaticSelection()
    {
        Assert.Equal(
            LocalizationManager.AutomaticLanguageCode,
            LocalizationManager.NormalizePreferenceCode("de"));
    }

    [Fact]
    public void EveryCatalogEntryHasAllFourTranslations()
    {
        Assert.NotEmpty(LocalizationManager.SourceTexts);
        foreach (var sourceText in LocalizationManager.SourceTexts)
        {
            var translations = LocalizationManager.GetTranslations(sourceText);
            Assert.Equal(4, translations.Count);
            Assert.All(translations, translation => Assert.False(string.IsNullOrWhiteSpace(translation)));
        }
    }

    [Fact]
    public void CoreControlsTranslateInEverySupportedLanguage()
    {
        Assert.Equal("全局设置", LocalizationManager.Translate("Global Settings", LocalizationManager.ChineseLanguageCode));
        Assert.Equal("全般設定", LocalizationManager.Translate("Global Settings", LocalizationManager.JapaneseLanguageCode));
        Assert.Equal("Общие настройки", LocalizationManager.Translate("Global Settings", LocalizationManager.RussianLanguageCode));
    }

    [Theory]
    [InlineData(LocalizationManager.EnglishLanguageCode, "4x oversampling")]
    [InlineData(LocalizationManager.ChineseLanguageCode, "4 倍过采样")]
    [InlineData(LocalizationManager.JapaneseLanguageCode, "4 倍オーバーサンプリング")]
    [InlineData(LocalizationManager.RussianLanguageCode, "4× оверсэмплинг")]
    public void OversamplingMultiplierIsFullyLocalized(string languageCode, string expected)
    {
        var format = LocalizationManager.Translate("{0}x oversampling", languageCode);

        Assert.Equal(expected, string.Format(CultureInfo.InvariantCulture, format, 4));
    }

    [Fact]
    public void PopupContentIsIncludedInLocalizationTraversal()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var label = new TextBlock { Text = "Scale" };
                var correlationMeter = new CorrelationMeter { Label = "Correlation" };
                var popupContent = new StackPanel();
                popupContent.Children.Add(label);
                popupContent.Children.Add(correlationMeter);
                var popup = new Popup { Child = popupContent };
                var root = new Grid();
                root.Children.Add(popup);

                LocalizationManager.LocalizeTree(root, LocalizationManager.ChineseLanguageCode);

                Assert.Equal("刻度", label.Text);
                Assert.Equal("相关度", correlationMeter.Label);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    [Fact]
    public void DetailSurfaceTextLiteralsAreCatalogued()
    {
        var repositoryRoot = FindRepositoryRoot();
        var intentionalSymbols = new HashSet<string>(StringComparer.Ordinal)
        {
            "20 Hz - 22 kHz",
            "L",
            "R",
            "uMeter",
            "v",
            "X"
        };
        var uncovered = new List<string>();

        foreach (var relativePath in new[]
        {
            "src/DesktopLoudnessMeter.App/MainWindow.xaml",
            "src/DesktopLoudnessMeter.App/DetailWindow.xaml",
            "src/DesktopLoudnessMeter.App/StereoImageSettingsWindow.xaml"
        })
        {
            var document = XDocument.Load(Path.Combine(repositoryRoot, relativePath));
            foreach (var attribute in document.Descendants().Attributes())
            {
                if (attribute.Name.LocalName is not ("Text" or "Content" or "Header" or "ToolTip" or "Title" or "Label")
                    || attribute.Value.StartsWith('{')
                    || !attribute.Value.Any(char.IsLetter)
                    || intentionalSymbols.Contains(attribute.Value)
                    || LocalizationManager.SourceTexts.Contains(attribute.Value))
                {
                    continue;
                }

                uncovered.Add($"{relativePath}: {attribute.Name.LocalName}=\"{attribute.Value}\"");
            }
        }

        Assert.Empty(uncovered);
    }

    [Fact]
    public void ComputedDetailTextFormatsAreCatalogued()
    {
        var computedSources = new[]
        {
            "{0} source",
            "{0} source - {1}",
            "{0} source - PID {1}",
            "{0} to {1} dB",
            "{0} to {1} LUFS",
            "Active",
            "Inactive",
            "n/a",
            "max {0}"
        };

        Assert.All(computedSources, source => Assert.Contains(source, LocalizationManager.SourceTexts));
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
