using System.Windows;

namespace DesktopLoudnessMeter.App;

internal static class DetailPopupPlacement
{
    public static void PositionNearOwner(Window popup, Window? owner)
    {
        if (owner?.IsVisible != true)
        {
            popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        popup.WindowStartupLocation = WindowStartupLocation.Manual;
        var ownerWidth = owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width;
        var ownerHeight = owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height;
        var popupWidth = popup.Width > 0 ? popup.Width : 460;
        var popupHeight = popup.Height > 0 ? popup.Height : 840;
        popup.Left = ClampToVirtualScreen(owner.Left + ((ownerWidth - popupWidth) / 2), popupWidth, isHorizontal: true);
        popup.Top = ClampToVirtualScreen(owner.Top + ((ownerHeight - popupHeight) / 2), popupHeight, isHorizontal: false);
    }

    private static double ClampToVirtualScreen(double value, double length, bool isHorizontal)
    {
        var minimum = isHorizontal ? SystemParameters.VirtualScreenLeft : SystemParameters.VirtualScreenTop;
        var span = isHorizontal ? SystemParameters.VirtualScreenWidth : SystemParameters.VirtualScreenHeight;
        var maximum = Math.Max(minimum, minimum + span - length);
        return Math.Clamp(value, minimum, maximum);
    }
}
