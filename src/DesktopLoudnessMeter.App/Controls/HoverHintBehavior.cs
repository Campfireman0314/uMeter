using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopLoudnessMeter.App.Controls;

public static class HoverHintBehavior
{
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(650);

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(HoverHintBehavior),
        new PropertyMetadata(null, OnTextChanged));

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(HoverHintBehavior),
        new PropertyMetadata(true, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(HoverHintState),
        typeof(HoverHintBehavior));

    public static string? GetText(DependencyObject element)
    {
        return (string?)element.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject element, string? value)
    {
        element.SetValue(TextProperty, value);
    }

    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not FrameworkElement element)
        {
            return;
        }

        if (args.NewValue is string text && !string.IsNullOrWhiteSpace(text))
        {
            EnsureState(element);
        }
        else
        {
            RemoveState(element);
        }
    }

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is FrameworkElement element
            && element.GetValue(StateProperty) is HoverHintState state
            && args.NewValue is false)
        {
            state.Stop();
        }
    }

    private static void EnsureState(FrameworkElement element)
    {
        if (element.GetValue(StateProperty) is HoverHintState)
        {
            return;
        }

        var state = new HoverHintState(element);
        element.SetValue(StateProperty, state);
        element.MouseEnter += state.OnMouseEnter;
        element.MouseMove += state.OnMouseMove;
        element.MouseLeave += state.OnMouseLeave;
        element.Unloaded += state.OnUnloaded;
    }

    private static void RemoveState(FrameworkElement element)
    {
        if (element.GetValue(StateProperty) is not HoverHintState state)
        {
            return;
        }

        state.Detach();
        element.ClearValue(StateProperty);
    }

    private sealed class HoverHintState
    {
        private readonly FrameworkElement _element;
        private readonly DispatcherTimer _timer;
        private AdornerLayer? _adornerLayer;
        private HoverHintAdorner? _adorner;

        public HoverHintState(FrameworkElement element)
        {
            _element = element;
            _timer = new DispatcherTimer(DispatcherPriority.Background, element.Dispatcher)
            {
                Interval = HoverDelay
            };
            _timer.Tick += OnTimerTick;
        }

        public void OnMouseEnter(object sender, MouseEventArgs args)
        {
            Restart();
        }

        public void OnMouseMove(object sender, MouseEventArgs args)
        {
            CloseHint();
            Restart();
        }

        public void OnMouseLeave(object sender, MouseEventArgs args)
        {
            Stop();
        }

        public void OnUnloaded(object sender, RoutedEventArgs args)
        {
            Stop();
        }

        public void Stop()
        {
            _timer.Stop();
            CloseHint();
        }

        public void Detach()
        {
            Stop();
            _timer.Tick -= OnTimerTick;
            _element.MouseEnter -= OnMouseEnter;
            _element.MouseMove -= OnMouseMove;
            _element.MouseLeave -= OnMouseLeave;
            _element.Unloaded -= OnUnloaded;
        }

        private void Restart()
        {
            _timer.Stop();
            if (!GetIsEnabled(_element) || string.IsNullOrWhiteSpace(GetText(_element)))
            {
                return;
            }

            _timer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs args)
        {
            _timer.Stop();
            if (!_element.IsMouseOver || !GetIsEnabled(_element) || GetText(_element) is not { } text)
            {
                return;
            }

            _adornerLayer = AdornerLayer.GetAdornerLayer(_element);
            if (_adornerLayer is null)
            {
                return;
            }

            _adorner = new HoverHintAdorner(_element, text);
            _adornerLayer.Add(_adorner);
        }

        private void CloseHint()
        {
            if (_adorner is null)
            {
                return;
            }

            _adornerLayer?.Remove(_adorner);
            _adorner = null;
            _adornerLayer = null;
        }
    }

    private sealed class HoverHintAdorner : Adorner
    {
        private readonly Border _content;

        public HoverHintAdorner(UIElement adornedElement, string text)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            var textBlock = new TextBlock
            {
                MaxWidth = 360,
                FontSize = 11,
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            _content = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Child = textBlock
            };
            _content.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
            _content.SetResourceReference(Border.BorderBrushProperty, "ControlBorderBrush");
            AddVisualChild(_content);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            return index == 0 ? _content : throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return AdornedElement.RenderSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _content.Arrange(new Rect(
                new Point(0, AdornedElement.RenderSize.Height + 4),
                _content.DesiredSize));
            return finalSize;
        }
    }
}
