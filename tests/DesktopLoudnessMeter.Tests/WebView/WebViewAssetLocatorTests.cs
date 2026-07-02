using DesktopLoudnessMeter.App.WebView;

namespace DesktopLoudnessMeter.Tests.WebView;

public sealed class WebViewAssetLocatorTests
{
    [Fact]
    public void ResolveStartupUriPrefersDevServerEnvironmentUrl()
    {
        var locator = new WebViewAssetLocator(
            @"C:\uMeter",
            name => name == "UMETER_WEBVIEW_DEV_URL" ? "http://127.0.0.1:5173/" : null);

        var uri = locator.ResolveStartupUri();

        Assert.Equal("http://127.0.0.1:5173/", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveStartupUriFallsBackToPackagedWebIndex()
    {
        var locator = new WebViewAssetLocator(
            @"C:\uMeter",
            _ => null);

        var uri = locator.ResolveStartupUri();

        Assert.Equal("https://app.umeter.local/index.html", uri.AbsoluteUri);
        Assert.Equal(@"C:\uMeter\Web", locator.ResolveWebRootDirectory());
        Assert.Equal("app.umeter.local", locator.ResolveVirtualHostName());
    }

    [Fact]
    public void WebViewShellUsesFramelessResizableWindowChrome()
    {
        var xaml = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "DesktopLoudnessMeter.App",
            "WebView",
            "WebViewShellWindow.xaml"));

        Assert.Contains("WindowStyle=\"None\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ResizeMode=\"CanResize\"", xaml, StringComparison.Ordinal);
        Assert.Contains("GlassFrameThickness=\"0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("UseAeroCaptionButtons=\"False\"", xaml, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "desktop-loudness-meter.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
