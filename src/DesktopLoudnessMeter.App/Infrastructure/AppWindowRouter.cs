namespace DesktopLoudnessMeter.App.Infrastructure;

internal sealed class AppWindowRouter
{
    private readonly Action _showWebViewPrimary;
    private readonly Action _showNativeUi;

    public AppWindowRouter(Action showWebViewPrimary, Action showNativeUi)
    {
        _showWebViewPrimary = showWebViewPrimary;
        _showNativeUi = showNativeUi;
    }

    public void ShowStartupWindow()
    {
        _showWebViewPrimary();
    }

    public void RestoreFromTray()
    {
        _showWebViewPrimary();
    }

    public void RestoreFromSecondaryActivation()
    {
        _showWebViewPrimary();
    }

    public void OpenNativeUi()
    {
        _showNativeUi();
    }
}
