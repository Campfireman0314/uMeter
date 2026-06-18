using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class GridRowResizeThumb : Thumb
{
    public static readonly DependencyProperty TargetRowProperty = DependencyProperty.Register(
        nameof(TargetRow),
        typeof(RowDefinition),
        typeof(GridRowResizeThumb),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ContainerElementProperty = DependencyProperty.Register(
        nameof(ContainerElement),
        typeof(FrameworkElement),
        typeof(GridRowResizeThumb),
        new PropertyMetadata(null));

    public GridRowResizeThumb()
    {
        Cursor = Cursors.SizeNS;
        DragDelta += HandleDragDelta;
    }

    public RowDefinition? TargetRow
    {
        get => (RowDefinition?)GetValue(TargetRowProperty);
        set => SetValue(TargetRowProperty, value);
    }

    public FrameworkElement? ContainerElement
    {
        get => (FrameworkElement?)GetValue(ContainerElementProperty);
        set => SetValue(ContainerElementProperty, value);
    }

    private void HandleDragDelta(object sender, DragDeltaEventArgs args)
    {
        if (TargetRow is not { } row || ContainerElement is not { } container)
        {
            return;
        }

        var currentRowHeight = row.ActualHeight;
        var currentContainerHeight = double.IsNaN(container.Height) ? container.ActualHeight : container.Height;
        if (currentRowHeight <= 0 || currentContainerHeight <= 0)
        {
            return;
        }

        var delta = ClampDelta(args.VerticalChange, currentRowHeight, row.MinHeight, row.MaxHeight);
        delta = ClampDelta(delta, currentContainerHeight, container.MinHeight, container.MaxHeight);
        if (Math.Abs(delta) < 0.1)
        {
            return;
        }

        row.Height = new GridLength(currentRowHeight + delta, GridUnitType.Pixel);
        container.SetCurrentValue(FrameworkElement.HeightProperty, currentContainerHeight + delta);
        args.Handled = true;
    }

    private static double ClampDelta(double delta, double current, double minimum, double maximum)
    {
        var minimumValue = minimum > 0 ? minimum : 0;
        var maximumValue = maximum > 0 && !double.IsInfinity(maximum)
            ? maximum
            : double.PositiveInfinity;

        return Math.Clamp(delta, minimumValue - current, maximumValue - current);
    }
}
