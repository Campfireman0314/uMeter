using System.Windows;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Themes;

public static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        var resources = application.Resources;
        SetBrush(resources, "AppChromeBrush", theme.AppChrome);
        SetBrush(resources, "PanelBrush", theme.Panel);
        SetBrush(resources, "PanelAltBrush", theme.PanelAlt);
        SetBrush(resources, "AccentBrush", theme.Accent);
        SetBrush(resources, "WarmBrush", theme.Warm);
        SetBrush(resources, "TextBrush", theme.Text);
        SetBrush(resources, "SubtleTextBrush", theme.SubtleText);
        SetBrush(resources, "ControlBrush", theme.Control);
        SetBrush(resources, "ControlHoverBrush", theme.ControlHover);
        SetBrush(resources, "ControlPressedBrush", theme.ControlPressed);
        SetBrush(resources, "ControlBorderBrush", theme.ControlBorder);
        SetBrush(resources, "DisabledTextBrush", theme.DisabledText);
        SetBrush(resources, "ScrollRailBrush", theme.ScrollRail);
        SetBrush(resources, "ChromeBorderBrush", theme.ChromeBorder);
        SetBrush(resources, "SelectionBrush", theme.Selection);
        SetBrush(resources, "MeterTrackBrush", theme.MeterTrack);
        SetBrush(resources, "SignalBrush", theme.Signal);
        SetBrush(resources, "SpectrumWarmBrush", theme.SpectrumWarm);
        InvalidateOpenWindows(application);
    }

    private static void SetBrush(ResourceDictionary resources, string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
    }

    private static void InvalidateOpenWindows(Application application)
    {
        foreach (Window window in application.Windows)
        {
            InvalidateVisualTree(window);
        }
    }

    private static void InvalidateVisualTree(DependencyObject dependencyObject)
    {
        if (dependencyObject is UIElement element)
        {
            element.InvalidateVisual();
        }

        var children = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (var index = 0; index < children; index++)
        {
            InvalidateVisualTree(VisualTreeHelper.GetChild(dependencyObject, index));
        }
    }
}
