using AppEntryPoint = DesktopLoudnessMeter.App.App;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class AppTrayMenuTests
{
    [Fact]
    public void NormalTrayMenuExposesWebViewAndNativeUiEntrypoints()
    {
        Assert.Contains(AppEntryPoint.TrayShowMenuText, AppEntryPoint.NormalTrayMenuTexts);
        Assert.Contains(AppEntryPoint.TrayShowNativeUiMenuText, AppEntryPoint.NormalTrayMenuTexts);
        Assert.Contains(AppEntryPoint.TrayPopupMenuText, AppEntryPoint.NormalTrayMenuTexts);
        Assert.Contains(AppEntryPoint.TrayExitMenuText, AppEntryPoint.NormalTrayMenuTexts);
        Assert.DoesNotContain("Open WPF Fallback", AppEntryPoint.NormalTrayMenuTexts);
    }
}
