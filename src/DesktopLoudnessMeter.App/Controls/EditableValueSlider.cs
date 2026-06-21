using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class EditableValueSlider : FrameworkElement
{
    private const double DefaultMinimum = 0;
    private const double DefaultMaximum = 1;
    private const double DefaultStep = 0.01;
    private const double ThumbRadius = 7;
    private const double HaloRadius = 12;
    private const double TrackHeight = 8;
    private const double SelectedTrackHeight = 6;
    private const double HorizontalPadding = HaloRadius + 2;
    private const double PreferredWidth = 264;
    private const double PreferredHeight = 48;
    private bool _isDragging;
    private bool _isHot;
    private bool _isCommittingEditor;
    private readonly TextBox _editor;

    public EditableValueSlider()
    {
        Cursor = Cursors.SizeWE;
        Focusable = true;
        SnapsToDevicePixels = true;
        AddVisualChild(_editor = CreateEditor());
        AddLogicalChild(_editor);
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(DefaultMinimum, OnBoundsChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(DefaultMaximum, OnBoundsChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(DefaultStep, OnBoundsChanged));

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(
            DefaultMinimum,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceValue));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty DisplayScaleProperty = DependencyProperty.Register(
        nameof(DisplayScale),
        typeof(double),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(1d, OnValueChanged));

    public double DisplayScale
    {
        get => (double)GetValue(DisplayScaleProperty);
        set => SetValue(DisplayScaleProperty, value);
    }

    public static readonly DependencyProperty DisplayFormatProperty = DependencyProperty.Register(
        nameof(DisplayFormat),
        typeof(string),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata("0", OnValueChanged));

    public string DisplayFormat
    {
        get => (string)GetValue(DisplayFormatProperty);
        set => SetValue(DisplayFormatProperty, value);
    }

    public static readonly DependencyProperty DisplaySuffixProperty = DependencyProperty.Register(
        nameof(DisplaySuffix),
        typeof(string),
        typeof(EditableValueSlider),
        new FrameworkPropertyMetadata(string.Empty, OnValueChanged));

    public string DisplaySuffix
    {
        get => (string)GetValue(DisplaySuffixProperty);
        set => SetValue(DisplaySuffixProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? PreferredWidth : availableSize.Width;
        _editor.Measure(new Size(72, 26));
        return new Size(Math.Max(180, width), PreferredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ArrangeEditor();
        return finalSize;
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index)
    {
        return index == 0 ? _editor : throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var track = TrackRect();
        if (track.Width <= 0 || track.Height <= 0)
        {
            return;
        }

        var valueX = ValueToX(Value);
        var centerY = track.Top + (track.Height / 2);
        var accent = ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
        var text = ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(238, 241, 245));
        var subtle = ThemedDrawing.GetBrush(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var opacity = IsEnabled ? 1.0 : 0.48;

        drawingContext.PushOpacity(opacity);
        DrawTrack(drawingContext, track, valueX, accent);
        DrawTicks(drawingContext, track, subtle);
        DrawEndpointLabels(drawingContext, track, subtle);
        DrawThumb(drawingContext, new Point(valueX, centerY), accent, _isHot || _isDragging, text);
        drawingContext.Pop();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        if (e.ClickCount >= 2)
        {
            BeginEdit();
            e.Handled = true;
            return;
        }

        EndEdit(commit: true);
        _isDragging = true;
        _isHot = true;
        CaptureMouse();
        UpdateValue(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);
        if (_isDragging && IsMouseCaptured)
        {
            UpdateValue(position);
            e.Handled = true;
            return;
        }

        var nextHot = HitThumb(position);
        if (nextHot != _isHot)
        {
            _isHot = nextHot;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        ReleaseMouseCapture();
        _isHot = HitThumb(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (IsMouseCaptured)
        {
            return;
        }

        _isHot = false;
        InvalidateVisual();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (e.NewFocus == _editor)
        {
            return;
        }

        EndEdit(commit: true);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Left or Key.Down)
        {
            SetEditedValue(Value - EffectiveStep());
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.Up)
        {
            SetEditedValue(Value + EffectiveStep());
            e.Handled = true;
        }
        else if (e.Key == Key.Home)
        {
            SetEditedValue(Minimum);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            SetEditedValue(Maximum);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            BeginEdit();
            e.Handled = true;
        }
    }

    private void DrawTrack(DrawingContext drawingContext, Rect track, double valueX, Color accent)
    {
        drawingContext.DrawRoundedRectangle(
            ThemedDrawing.GetBrush(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25)),
            ThemedDrawing.GetPen(this, "ControlBorderBrush", 1, Color.FromRgb(58, 66, 77)),
            track,
            TrackHeight / 2,
            TrackHeight / 2);

        var selected = new Rect(
            track.Left,
            track.Top + ((track.Height - SelectedTrackHeight) / 2),
            Math.Max(0, valueX - track.Left),
            SelectedTrackHeight);
        if (selected.Width <= 0)
        {
            return;
        }

        drawingContext.DrawRoundedRectangle(
            new SolidColorBrush(ThemedDrawing.WithAlpha(accent, 216)),
            null,
            selected,
            SelectedTrackHeight / 2,
            SelectedTrackHeight / 2);
    }

    private void DrawTicks(DrawingContext drawingContext, Rect track, Brush subtle)
    {
        var pen = new Pen(subtle, 1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var tickValues = new[] { Minimum, Minimum + ((Maximum - Minimum) * 0.25), Minimum + ((Maximum - Minimum) * 0.5), Minimum + ((Maximum - Minimum) * 0.75), Maximum };
        foreach (var value in tickValues)
        {
            var x = ValueToX(value);
            drawingContext.DrawLine(pen, new Point(x, track.Bottom + 4), new Point(x, track.Bottom + 9));
        }
    }

    private void DrawEndpointLabels(DrawingContext drawingContext, Rect track, Brush subtle)
    {
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var minimum = Label(FormatDisplay(Minimum), subtle, pixelsPerDip);
        var maximum = Label(FormatDisplay(Maximum), subtle, pixelsPerDip);
        drawingContext.DrawText(minimum, new Point(track.Left, track.Bottom + 12));
        drawingContext.DrawText(maximum, new Point(track.Right - maximum.Width, track.Bottom + 12));
    }

    private void DrawThumb(DrawingContext drawingContext, Point center, Color accent, bool isHot, Brush fill)
    {
        var haloAlpha = isHot ? (byte)58 : (byte)34;
        var strokeAlpha = isHot ? (byte)246 : (byte)210;
        var haloBrush = new SolidColorBrush(ThemedDrawing.WithAlpha(accent, haloAlpha));
        var stroke = new Pen(new SolidColorBrush(ThemedDrawing.WithAlpha(accent, strokeAlpha)), 2);
        drawingContext.DrawEllipse(haloBrush, null, center, HaloRadius, HaloRadius);
        drawingContext.DrawEllipse(fill, stroke, center, ThumbRadius, ThumbRadius);
        drawingContext.DrawEllipse(new SolidColorBrush(ThemedDrawing.WithAlpha(accent, 46)), null, center, ThumbRadius - 3, ThumbRadius - 3);
    }

    private FormattedText Label(string text, Brush brush, double pixelsPerDip)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            brush,
            pixelsPerDip);
    }

    private Rect TrackRect()
    {
        var width = Math.Max(1, ActualWidth - (HorizontalPadding * 2));
        var centerY = Math.Clamp(ActualHeight * 0.43, HaloRadius + 2, Math.Max(HaloRadius + 2, ActualHeight - 24));
        return new Rect(HorizontalPadding, centerY - (TrackHeight / 2), width, TrackHeight);
    }

    private double ValueToX(double value)
    {
        var track = TrackRect();
        var span = Math.Max(0.0001, Maximum - Minimum);
        var ratio = Math.Clamp((value - Minimum) / span, 0, 1);
        return track.Left + (track.Width * ratio);
    }

    private double XToValue(double x)
    {
        var track = TrackRect();
        var span = Math.Max(0.0001, Maximum - Minimum);
        var ratio = Math.Clamp((x - track.Left) / track.Width, 0, 1);
        return Minimum + (span * ratio);
    }

    private bool HitThumb(Point position)
    {
        var track = TrackRect();
        var center = new Point(ValueToX(Value), track.Top + (track.Height / 2));
        var distance = Math.Sqrt(Math.Pow(position.X - center.X, 2) + Math.Pow(position.Y - center.Y, 2));
        return distance <= HaloRadius + 2;
    }

    private void UpdateValue(Point position)
    {
        SetEditedValue(XToValue(position.X));
    }

    private TextBox CreateEditor()
    {
        var editor = new TextBox
        {
            Visibility = Visibility.Collapsed,
            Width = 72,
            Height = 26,
            Padding = new Thickness(6, 2, 6, 2),
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1),
            FontSize = 11
        };
        editor.KeyDown += EditorOnKeyDown;
        editor.LostKeyboardFocus += (_, _) =>
        {
            if (!_isCommittingEditor)
            {
                EndEdit(commit: true);
            }
        };
        return editor;
    }

    private void BeginEdit()
    {
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        _isDragging = false;
        _isHot = true;
        _editor.Text = FormatEditorValue(Value);
        ApplyEditorTheme(_editor);
        _editor.Visibility = Visibility.Visible;
        ArrangeEditor();
        _editor.Focus();
        _editor.SelectAll();
        InvalidateVisual();
    }

    private void EndEdit(bool commit)
    {
        if (_editor.Visibility != Visibility.Visible)
        {
            return;
        }

        _isCommittingEditor = true;
        try
        {
            if (commit && TryParseEditorText(_editor.Text, out var value))
            {
                SetEditedValue(value);
            }
        }
        finally
        {
            _isCommittingEditor = false;
            _editor.Visibility = Visibility.Collapsed;
            InvalidateVisual();
        }
    }

    private void SetEditedValue(double value)
    {
        SetCurrentValue(ValueProperty, Math.Clamp(Snap(value, EffectiveStep()), Minimum, Maximum));
    }

    private void ArrangeEditor()
    {
        if (_editor.Visibility != Visibility.Visible)
        {
            return;
        }

        var track = TrackRect();
        var desired = _editor.DesiredSize;
        var width = desired.Width > 0 ? desired.Width : _editor.Width;
        var height = desired.Height > 0 ? desired.Height : _editor.Height;
        var x = Math.Clamp(ValueToX(Value) - (width / 2), 0, Math.Max(0, ActualWidth - width));
        var y = Math.Max(0, track.Top - height - 6);
        _editor.Arrange(new Rect(x, y, width, height));
    }

    private void ApplyEditorTheme(TextBox editor)
    {
        editor.Background = ThemedDrawing.GetBrush(this, "ControlPressedBrush", Color.FromRgb(53, 65, 77));
        editor.Foreground = ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(238, 241, 245));
        editor.BorderBrush = ThemedDrawing.GetBrush(this, "AccentBrush", Color.FromRgb(46, 196, 182));
    }

    private void EditorOnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            EndEdit(commit: true);
            Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            EndEdit(commit: false);
            Focus();
            e.Handled = true;
        }
    }

    private string FormatDisplay(double value)
    {
        var scaled = value * EffectiveDisplayScale();
        return scaled.ToString(DisplayFormat, CultureInfo.InvariantCulture) + DisplaySuffix;
    }

    private string FormatEditorValue(double value)
    {
        return (value * EffectiveDisplayScale()).ToString(DisplayFormat, CultureInfo.InvariantCulture);
    }

    private bool TryParseEditorText(string text, out double value)
    {
        var suffix = DisplaySuffix;
        var trimmed = text.Trim();
        var hadSuffix = false;
        if (!string.IsNullOrWhiteSpace(suffix) && trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^suffix.Length].Trim();
            hadSuffix = true;
        }

        if (trimmed.EndsWith("%", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1].Trim();
            hadSuffix = true;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        var scale = EffectiveDisplayScale();
        if (scale != 1 && (hadSuffix || value > Maximum || value < Minimum))
        {
            value /= scale;
        }

        return true;
    }

    private double EffectiveDisplayScale()
    {
        return double.IsFinite(DisplayScale) && DisplayScale != 0 ? DisplayScale : 1;
    }

    private double EffectiveStep()
    {
        return double.IsFinite(Step) && Step > 0 ? Step : DefaultStep;
    }

    private static void OnBoundsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not EditableValueSlider slider)
        {
            return;
        }

        slider.CoerceValue(ValueProperty);
        slider.ArrangeEditor();
        slider.InvalidateVisual();
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is EditableValueSlider slider)
        {
            slider.ArrangeEditor();
            slider.InvalidateVisual();
        }
    }

    private static object CoerceValue(DependencyObject dependencyObject, object baseValue)
    {
        var slider = (EditableValueSlider)dependencyObject;
        var minimum = Math.Min(slider.Minimum, slider.Maximum);
        var maximum = Math.Max(slider.Minimum, slider.Maximum);
        return Snap(Math.Clamp(AsFinite(baseValue, minimum), minimum, maximum), slider.EffectiveStep());
    }

    private static double AsFinite(object value, double fallback)
    {
        return value is double number && double.IsFinite(number) ? number : fallback;
    }

    private static double Snap(double value, double step)
    {
        return Math.Round(value / step, 0, MidpointRounding.AwayFromZero) * step;
    }
}
