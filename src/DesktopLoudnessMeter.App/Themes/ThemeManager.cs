using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

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
        var visited = new HashSet<DependencyObject>();
        foreach (Window window in application.Windows)
        {
            InvalidateVisualTree(window, visited);
        }
    }

    private static void InvalidateVisualTree(DependencyObject dependencyObject, ISet<DependencyObject> visited)
    {
        if (!visited.Add(dependencyObject))
        {
            return;
        }

        RefreshResourceBackedProperties(dependencyObject);

        if (dependencyObject is UIElement element)
        {
            element.InvalidateVisual();
        }

        if (dependencyObject is Visual or System.Windows.Media.Media3D.Visual3D)
        {
            var children = VisualTreeHelper.GetChildrenCount(dependencyObject);
            for (var index = 0; index < children; index++)
            {
                InvalidateVisualTree(VisualTreeHelper.GetChild(dependencyObject, index), visited);
            }
        }

        foreach (var child in LogicalTreeHelper.GetChildren(dependencyObject).OfType<DependencyObject>())
        {
            InvalidateVisualTree(child, visited);
        }

        if (dependencyObject is Popup { Child: { } popupChild })
        {
            InvalidateVisualTree(popupChild, visited);
        }
    }

    private static void RefreshResourceBackedProperties(DependencyObject dependencyObject)
    {
        if (dependencyObject is Control control)
        {
            control.InvalidateProperty(Control.BackgroundProperty);
            control.InvalidateProperty(Control.ForegroundProperty);
            control.InvalidateProperty(Control.BorderBrushProperty);
        }

        if (dependencyObject is TextBlock textBlock)
        {
            textBlock.InvalidateProperty(TextBlock.ForegroundProperty);
        }

        if (dependencyObject is Border border)
        {
            border.InvalidateProperty(Border.BackgroundProperty);
            border.InvalidateProperty(Border.BorderBrushProperty);
        }

        if (dependencyObject is Panel panel)
        {
            panel.InvalidateProperty(Panel.BackgroundProperty);
        }

        if (dependencyObject is Shape shape)
        {
            shape.InvalidateProperty(Shape.FillProperty);
            shape.InvalidateProperty(Shape.StrokeProperty);
        }
    }
}
