using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class VerticalResizeThumb : Thumb
{
    public static readonly DependencyProperty TargetElementProperty = DependencyProperty.Register(
        nameof(TargetElement),
        typeof(FrameworkElement),
        typeof(VerticalResizeThumb),
        new PropertyMetadata(null));

    public VerticalResizeThumb()
    {
        Cursor = Cursors.SizeNS;
        DragDelta += HandleDragDelta;
    }

    public FrameworkElement? TargetElement
    {
        get => (FrameworkElement?)GetValue(TargetElementProperty);
        set => SetValue(TargetElementProperty, value);
    }

    private void HandleDragDelta(object sender, DragDeltaEventArgs args)
    {
        if (TargetElement is not { } target)
        {
            return;
        }

        var currentHeight = double.IsNaN(target.Height) ? target.ActualHeight : target.Height;
        var minimumHeight = target.MinHeight > 0 ? target.MinHeight : 90;
        var maximumHeight = target.MaxHeight > 0 && !double.IsInfinity(target.MaxHeight) ? target.MaxHeight : 520;
        target.Height = Math.Clamp(currentHeight + args.VerticalChange, minimumHeight, maximumHeight);
    }
}
