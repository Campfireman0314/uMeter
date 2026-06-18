using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App;

internal static class WindowDragBehavior
{
    public static void HandleHeaderMouseLeftButtonDown(Window window, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleWindowState(window);
            e.Handled = true;
            return;
        }

        if (window.WindowState == WindowState.Maximized)
        {
            RestoreForDrag(window, e);
        }

        try
        {
            window.DragMove();
            e.Handled = true;
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if Windows consumes the mouse capture during snap/restore.
        }
    }

    private static void ToggleWindowState(Window window)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private static void RestoreForDrag(Window window, MouseEventArgs e)
    {
        var mouseInWindow = e.GetPosition(window);
        var mouseOnScreen = ToDeviceIndependentPoint(window, window.PointToScreen(mouseInWindow));
        var restoreBounds = window.RestoreBounds;
        var restoredWidth = Math.Max(window.MinWidth, restoreBounds.Width);
        var restoredHeight = Math.Max(window.MinHeight, restoreBounds.Height);
        var cursorRatio = window.ActualWidth > 0
            ? Math.Clamp(mouseInWindow.X / window.ActualWidth, 0.08, 0.92)
            : 0.5;

        window.WindowState = WindowState.Normal;
        window.Width = restoredWidth;
        window.Height = restoredHeight;
        window.Left = mouseOnScreen.X - (restoredWidth * cursorRatio);
        window.Top = mouseOnScreen.Y - Math.Min(mouseInWindow.Y, 32);
    }

    private static Point ToDeviceIndependentPoint(Window window, Point screenPoint)
    {
        var source = PresentationSource.FromVisual(window);
        return source?.CompositionTarget is { } target
            ? target.TransformFromDevice.Transform(screenPoint)
            : screenPoint;
    }
}
