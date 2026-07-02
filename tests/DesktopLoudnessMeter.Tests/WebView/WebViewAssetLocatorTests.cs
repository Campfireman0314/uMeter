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
}
