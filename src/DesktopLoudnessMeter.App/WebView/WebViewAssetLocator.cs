using System.IO;

namespace DesktopLoudnessMeter.App.WebView;

public sealed class WebViewAssetLocator
{
    private const string DevServerEnvironmentVariable = "UMETER_WEBVIEW_DEV_URL";
    private const string VirtualHostName = "app.umeter.local";
    private readonly string _baseDirectory;
    private readonly Func<string, string?> _getEnvironmentVariable;

    public WebViewAssetLocator(
        string? baseDirectory = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : baseDirectory;
        _getEnvironmentVariable = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
    }

    public Uri ResolveStartupUri()
    {
        var devUrl = _getEnvironmentVariable(DevServerEnvironmentVariable);
        if (Uri.TryCreate(devUrl, UriKind.Absolute, out var devUri)
            && (devUri.Scheme == Uri.UriSchemeHttp || devUri.Scheme == Uri.UriSchemeHttps))
        {
            return devUri;
        }

        return new Uri($"https://{VirtualHostName}/index.html");
    }

    public string ResolveWebRootDirectory()
    {
        return Path.Combine(_baseDirectory, "Web");
    }

    public string ResolveVirtualHostName()
    {
        return VirtualHostName;
    }
}
