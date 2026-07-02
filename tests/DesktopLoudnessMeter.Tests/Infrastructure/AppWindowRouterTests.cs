using DesktopLoudnessMeter.App.Infrastructure;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class AppWindowRouterTests
{
    [Fact]
    public void StartupTrayAndSecondaryActivationRestoreWebViewPrimary()
    {
        var calls = new List<string>();
        var router = new AppWindowRouter(
            showWebViewPrimary: () => calls.Add("webview"),
            showNativeUi: () => calls.Add("native"));

        router.ShowStartupWindow();
        router.RestoreFromTray();
        router.RestoreFromSecondaryActivation();

        Assert.Equal(["webview", "webview", "webview"], calls);
    }

    [Fact]
    public void ExplicitFallbackRequestOpensWpfFallbackOnly()
    {
        var calls = new List<string>();
        var router = new AppWindowRouter(
            showWebViewPrimary: () => calls.Add("webview"),
            showNativeUi: () => calls.Add("native"));

        router.OpenNativeUi();

        Assert.Equal(["native"], calls);
    }
}
