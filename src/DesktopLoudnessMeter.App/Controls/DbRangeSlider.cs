using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class DbRangeSlider : FrameworkElement
{
    private const double DefaultMinimum = -200;
    private const double DefaultMaximum = 0;
    private const double DefaultStep = 1;
    private const double ThumbRadius = 7;
    private const double HaloRadius = 12;
    private const double TrackHeight = 8;
    private const double SelectedTrackHeight = 6;
    private const double HorizontalPadding = HaloRadius + 2;
    private const double PreferredWidth = 264;
    private const double PreferredHeight = 54;
    private DragThumb? _dragThumb;
    private DragThumb? _hotThumb;
    private TextBox? _editor;
    private DragThumb? _editingThumb;
    private bool _isCommittingEditor;
    private readonly RangeSliderEndpointHandle _lowerHandle;
    private readonly RangeSliderEndpointHandle _upperHandle;

    public DbRangeSlider()
    {
        Cursor = Cursors.SizeWE;
        Focusable = true;
        SnapsToDevicePixels = true;
        AddVisualChild(_editor = CreateEditor());
        AddLogicalChild(_editor);
        _lowerHandle = new RangeSliderEndpointHandle(this, RangeSliderEndpoint.Lower);
        _upperHandle = new RangeSliderEndpointHandle(this, RangeSliderEndpoint.Upper);
        AddVisualChild(_lowerHandle);
        AddVisualChild(_upperHandle);
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultMinimum, OnBoundsChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultMaximum, OnBoundsChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(DefaultStep, OnBoundsChanged));

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public static readonly DependencyProperty ActiveEndpointProperty = DependencyProperty.Register(
        nameof(ActiveEndpoint),
        typeof(RangeSliderEndpoint),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(RangeSliderEndpoint.Lower, OnActiveEndpointChanged));

    public RangeSliderEndpoint ActiveEndpoint
    {
        get => (RangeSliderEndpoint)GetValue(ActiveEndpointProperty);
        set => SetValue(ActiveEndpointProperty, value);
    }

    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
        nameof(LowerValue),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(
            DefaultMinimum,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceLowerValue));

    public double LowerValue
    {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
        nameof(UpperValue),
        typeof(double),
        typeof(DbRangeSlider),
        new FrameworkPropertyMetadata(
            DefaultMaximum,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            CoerceUpperValue));

    public double UpperValue
    {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? PreferredWidth : availableSize.Width;
        _editor?.Measure(new Size(72, 26));
        var handleSize = EndpointHandleSize();
        _lowerHandle.Measure(new Size(handleSize, handleSize));
        _upperHandle.Measure(new Size(handleSize, handleSize));
        return new Size(Math.Max(180, width), PreferredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ArrangeEditor();
        ArrangeEndpointHandles(finalSize);
        return finalSize;
    }

    protected override int VisualChildrenCount => _editor is null ? 2 : 3;

    protected override Visual GetVisualChild(int index)
    {
        if (_editor is not null)
        {
            if (index == 0)
            {
                return _editor;
            }

            index--;
        }

        return index switch
        {
            0 => _lowerHandle,
            1 => _upperHandle,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DbRangeSliderAutomationPeer(this);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var track = TrackRect();
        if (track.Width <= 0 || track.Height <= 0)
        {
            return;
        }

        var lowerX = ValueToX(LowerValue);
        var upperX = ValueToX(UpperValue);
        var centerY = track.Top + (track.Height / 2);
        var accent = ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
        var warm = ThemedDrawing.GetColor(this, "SpectrumWarmBrush", Color.FromRgb(255, 176, 0));
        var text = ThemedDrawing.GetBrush(this, "TextBrush", Color.FromRgb(238, 241, 245));
        var subtle = ThemedDrawing.GetBrush(this, "SubtleTextBrush", Color.FromRgb(174, 183, 194));
        var opacity = IsEnabled ? 1.0 : 0.48;
        var isKeyboardActive = IsEnabled && IsKeyboardFocusWithin;

        drawingContext.PushOpacity(opacity);
        DrawTrack(drawingContext, track, lowerX, upperX, accent, warm);
        DrawTicks(drawingContext, track, subtle);
        DrawEndpointLabels(drawingContext, track, subtle);
        DrawThumb(
            drawingContext,
            new Point(lowerX, centerY),
            accent,
            _hotThumb == DragThumb.Lower || _dragThumb == DragThumb.Lower || (isKeyboardActive && ActiveEndpoint == RangeSliderEndpoint.Lower),
            text);
        DrawThumb(
            drawingContext,
            new Point(upperX, centerY),
            warm,
            _hotThumb == DragThumb.Upper || _dragThumb == DragThumb.Upper || (isKeyboardActive && ActiveEndpoint == RangeSliderEndpoint.Upper),
            text);
        drawingContext.Pop();

        if (isKeyboardActive)
        {
            DrawActiveEndpointMarker(drawingContext, new Point(lowerX, centerY), accent, RangeSliderEndpoint.Lower);
            DrawActiveEndpointMarker(drawingContext, new Point(upperX, centerY), warm, RangeSliderEndpoint.Upper);
            AccessibilityFocusVisuals.DrawFocusRing(this, drawingContext, new Rect(RenderSize));
        }
    }

    private void DrawActiveEndpointMarker(DrawingContext drawingContext, Point center, Color accent, RangeSliderEndpoint endpoint)
    {
        if (ActiveEndpoint != endpoint)
        {
            return;
        }

        var marker = new Pen(new SolidColorBrush(ThemedDrawing.WithAlpha(accent, 255)), 2.5)
        {
            DashStyle = new DashStyle([2, 1.6], 0)
        };
        drawingContext.DrawEllipse(null, marker, center, HaloRadius - 1, HaloRadius - 1);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        if (e.ClickCount >= 2)
        {
            BeginEdit(ChooseThumb(e.GetPosition(this)));
            e.Handled = true;
            return;
        }

        EndEdit(commit: true);
        _dragThumb = ChooseThumb(e.GetPosition(this));
        _hotThumb = _dragThumb;
        ActivateEndpoint(_dragThumb == DragThumb.Lower ? RangeSliderEndpoint.Lower : RangeSliderEndpoint.Upper);
        CaptureMouse();
        UpdateDrag(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (_editor is not null && e.NewFocus == _editor)
        {
            return;
        }

        EndEdit(commit: true);
    }

    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusWithinChanged(e);
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);
        if (_dragThumb is not null && IsMouseCaptured)
        {
            UpdateDrag(position);
            e.Handled = true;
            return;
        }

        var nextHotThumb = HitThumb(position);
        if (nextHotThumb != _hotThumb)
        {
            _hotThumb = nextHotThumb;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragThumb is null)
        {
            return;
        }

        _dragThumb = null;
        ReleaseMouseCapture();
        _hotThumb = HitThumb(e.GetPosition(this));
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

        _hotThumb = null;
        InvalidateVisual();
    }

    private void DrawTrack(DrawingContext drawingContext, Rect track, double lowerX, double upperX, Color accent, Color warm)
    {
        drawingContext.DrawRoundedRectangle(
            ThemedDrawing.GetBrush(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25)),
            ThemedDrawing.GetPen(this, "ControlBorderBrush", 1, Color.FromRgb(58, 66, 77)),
            track,
            TrackHeight / 2,
            TrackHeight / 2);

        if (upperX <= lowerX)
        {
            return;
        }

        var selected = new Rect(
            lowerX,
            track.Top + ((track.Height - SelectedTrackHeight) / 2),
            upperX - lowerX,
            SelectedTrackHeight);
        var selectedBrush = new LinearGradientBrush(
            [
                new GradientStop(ThemedDrawing.WithAlpha(accent, 216), 0),
                new GradientStop(ThemedDrawing.WithAlpha(warm, 226), 1)
            ],
            new Point(0, 0),
            new Point(1, 0));
        drawingContext.DrawRoundedRectangle(selectedBrush, null, selected, SelectedTrackHeight / 2, SelectedTrackHeight / 2);
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
        var minimum = Label($"{Minimum:0}", subtle, pixelsPerDip);
        var maximum = Label($"{Maximum:0}", subtle, pixelsPerDip);
        drawingContext.DrawText(minimum, new Point(track.Left, track.Bottom + 12));
        drawingContext.DrawText(maximum, new Point(track.Right - maximum.Width, track.Bottom + 12));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsEnabled || IsValueEditorOpen)
        {
            return;
        }

        var step = EffectiveStep();
        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                NudgeActiveEndpoint(-step);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.Up:
                NudgeActiveEndpoint(step);
                e.Handled = true;
                break;
            case Key.Home:
                MoveActiveEndpointToLimit(lowest: true);
                e.Handled = true;
                break;
            case Key.End:
                MoveActiveEndpointToLimit(lowest: false);
                e.Handled = true;
                break;
            case Key.Space:
                ActivateEndpoint(ActiveEndpoint == RangeSliderEndpoint.Lower
                    ? RangeSliderEndpoint.Upper
                    : RangeSliderEndpoint.Lower);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.F2:
                BeginEdit(ActiveEndpoint);
                e.Handled = true;
                break;
        }
    }

    private bool IsValueEditorOpen => _editingThumb is not null;

    private void NudgeActiveEndpoint(double delta)
    {
        SetEndpointValue(ActiveEndpoint, GetEndpointValue(ActiveEndpoint) + delta);
    }

    private void MoveActiveEndpointToLimit(bool lowest)
    {
        var separation = EffectiveStep();
        if (ActiveEndpoint == RangeSliderEndpoint.Lower)
        {
            SetCurrentValue(
                LowerValueProperty,
                lowest ? Minimum : Math.Clamp(UpperValue - separation, Minimum, Maximum));
        }
        else
        {
            SetCurrentValue(
                UpperValueProperty,
                lowest ? Math.Clamp(LowerValue + separation, Minimum, Maximum) : Maximum);
        }
    }

    internal void ActivateEndpoint(RangeSliderEndpoint endpoint)
    {
        if (ActiveEndpoint == endpoint)
        {
            InvalidateVisual();
            return;
        }

        SetCurrentValue(ActiveEndpointProperty, endpoint);
        InvalidateVisual();
    }

    internal double EffectiveStepValue => EffectiveStep();

    internal double GetEndpointValue(RangeSliderEndpoint endpoint)
    {
        return endpoint == RangeSliderEndpoint.Lower ? LowerValue : UpperValue;
    }

    internal (double Minimum, double Maximum) GetEndpointBounds(RangeSliderEndpoint endpoint)
    {
        var separation = EffectiveStep();
        return endpoint == RangeSliderEndpoint.Lower
            ? (Minimum, Math.Max(Minimum, UpperValue - separation))
            : (Math.Min(Maximum, LowerValue + separation), Maximum);
    }

    internal void SetEndpointValue(RangeSliderEndpoint endpoint, double value)
    {
        // Let dependency-property coercion handle every input path identically, including exact bounds
        // that do not fall on a step. Pre-snapping here would make such endpoints unreachable.
        SetCurrentValue(endpoint == RangeSliderEndpoint.Lower ? LowerValueProperty : UpperValueProperty, value);
    }

    internal IReadOnlyList<AutomationPeer> CreateEndpointPeers()
    {
        return [_lowerHandle.EnsurePeer(), _upperHandle.EnsurePeer()];
    }

    private static double EndpointHandleSize()
    {
        return (HaloRadius + 2) * 2;
    }

    private void ArrangeEndpointHandles(Size finalSize)
    {
        var track = TrackRect(finalSize.Width, finalSize.Height);
        var size = EndpointHandleSize();
        var centerY = track.Top + (track.Height / 2);
        ArrangeEndpointHandle(_lowerHandle, ValueToX(track, LowerValue), centerY, size, finalSize);
        ArrangeEndpointHandle(_upperHandle, ValueToX(track, UpperValue), centerY, size, finalSize);
    }

    private static void ArrangeEndpointHandle(
        RangeSliderEndpointHandle handle,
        double centerX,
        double centerY,
        double size,
        Size finalSize)
    {
        var left = Math.Clamp(centerX - (size / 2), 0, Math.Max(0, finalSize.Width - size));
        var top = Math.Clamp(centerY - (size / 2), 0, Math.Max(0, finalSize.Height - size));
        handle.Arrange(new Rect(left, top, size, size));
    }

    private void SynchronizeEndpointAutomation()
    {
        _lowerHandle.Peer?.Synchronize();
        _upperHandle.Peer?.Synchronize();
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
        return TrackRect(ActualWidth, ActualHeight);
    }

    private Rect TrackRect(double width, double height)
    {
        var trackWidth = Math.Max(1, width - (HorizontalPadding * 2));
        var centerY = Math.Clamp(height * 0.43, HaloRadius + 2, Math.Max(HaloRadius + 2, height - 24));
        return new Rect(HorizontalPadding, centerY - (TrackHeight / 2), trackWidth, TrackHeight);
    }

    private double ValueToX(double value)
    {
        return ValueToX(TrackRect(), value);
    }

    private double ValueToX(Rect track, double value)
    {
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

    private DragThumb ChooseThumb(Point position)
    {
        return HitThumb(position) ?? (Math.Abs(position.X - ValueToX(LowerValue)) <= Math.Abs(position.X - ValueToX(UpperValue))
            ? DragThumb.Lower
            : DragThumb.Upper);
    }

    private DragThumb? HitThumb(Point position)
    {
        var y = TrackRect().Top + (TrackRect().Height / 2);
        var lowerDistance = Distance(position, new Point(ValueToX(LowerValue), y));
        var upperDistance = Distance(position, new Point(ValueToX(UpperValue), y));
        var hitRadius = HaloRadius + 2;
        if (lowerDistance > hitRadius && upperDistance > hitRadius)
        {
            return null;
        }

        return lowerDistance <= upperDistance ? DragThumb.Lower : DragThumb.Upper;
    }

    private void UpdateDrag(Point position)
    {
        if (_dragThumb is null)
        {
            return;
        }

        SetEndpointValue(
            _dragThumb == DragThumb.Lower ? RangeSliderEndpoint.Lower : RangeSliderEndpoint.Upper,
            XToValue(position.X));
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

    private void BeginEdit(DragThumb thumb)
    {
        BeginEdit(thumb == DragThumb.Lower ? RangeSliderEndpoint.Lower : RangeSliderEndpoint.Upper);
    }

    private void BeginEdit(RangeSliderEndpoint endpoint)
    {
        if (_editor is null || !IsEnabled)
        {
            return;
        }

        ActivateEndpoint(endpoint);
        var thumb = endpoint == RangeSliderEndpoint.Lower ? DragThumb.Lower : DragThumb.Upper;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        _dragThumb = null;
        _hotThumb = thumb;
        _editingThumb = thumb;
        _editor.Text = $"{(thumb == DragThumb.Lower ? LowerValue : UpperValue):0}";
        ApplyEditorTheme(_editor);
        _editor.Visibility = Visibility.Visible;
        ArrangeEditor();
        _editor.Focus();
        _editor.SelectAll();
        InvalidateVisual();
    }

    private void EndEdit(bool commit)
    {
        if (_editor is null || _editingThumb is null)
        {
            return;
        }

        _isCommittingEditor = true;
        try
        {
            if (commit && double.TryParse(_editor.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                SetEditedValue(_editingThumb.Value, value);
            }
        }
        finally
        {
            _isCommittingEditor = false;
            _editor.Visibility = Visibility.Collapsed;
            _editingThumb = null;
            InvalidateVisual();
        }
    }

    private void SetEditedValue(DragThumb thumb, double value)
    {
        SetEndpointValue(thumb == DragThumb.Lower ? RangeSliderEndpoint.Lower : RangeSliderEndpoint.Upper, value);
    }

    private void ArrangeEditor()
    {
        if (_editor is null || _editor.Visibility != Visibility.Visible || _editingThumb is null)
        {
            return;
        }

        var track = TrackRect();
        var value = _editingThumb == DragThumb.Lower ? LowerValue : UpperValue;
        var desired = _editor.DesiredSize;
        var width = desired.Width > 0 ? desired.Width : _editor.Width;
        var height = desired.Height > 0 ? desired.Height : _editor.Height;
        var x = Math.Clamp(ValueToX(value) - (width / 2), 0, Math.Max(0, ActualWidth - width));
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

    private double EffectiveStep()
    {
        return double.IsFinite(Step) && Step > 0 ? Step : DefaultStep;
    }

    private static double Distance(Point first, Point second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt((x * x) + (y * y));
    }

    private static void OnBoundsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not DbRangeSlider slider)
        {
            return;
        }

        // Coerce the endpoints in dependency order: a shrunken Maximum (or a raised Minimum) can leave the
        // pair crossing the minimum separation, so re-coerce after both so every advertised endpoint range
        // still contains its own value.
        slider.CoerceValue(UpperValueProperty);
        slider.CoerceValue(LowerValueProperty);
        if (slider.LowerValue + slider.EffectiveStep() > slider.UpperValue)
        {
            slider.CoerceValue(UpperValueProperty);
        }

        slider.ArrangeEditor();
        slider.InvalidateArrange();
        slider.InvalidateVisual();
        slider.SynchronizeEndpointAutomation();
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is DbRangeSlider slider)
        {
            slider.ArrangeEditor();
            slider.InvalidateArrange();
            slider.InvalidateVisual();
            slider.SynchronizeEndpointAutomation();
        }
    }

    private static void OnActiveEndpointChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not DbRangeSlider slider)
        {
            return;
        }

        var activated = (RangeSliderEndpoint)args.NewValue;
        var peer = activated == RangeSliderEndpoint.Lower ? slider._lowerHandle.Peer : slider._upperHandle.Peer;
        peer?.NotifyActivated();
        slider.InvalidateVisual();
    }

    private static object CoerceLowerValue(DependencyObject dependencyObject, object baseValue)
    {
        var slider = (DbRangeSlider)dependencyObject;
        var separation = slider.EffectiveStep();
        var minimum = Math.Min(slider.Minimum, slider.Maximum - separation);
        var maximum = Math.Max(minimum, slider.UpperValue - separation);
        return SnapWithinBounds(AsFinite(baseValue, minimum), minimum, maximum, separation);
    }

    private static object CoerceUpperValue(DependencyObject dependencyObject, object baseValue)
    {
        var slider = (DbRangeSlider)dependencyObject;
        var separation = slider.EffectiveStep();
        var maximum = Math.Max(slider.Maximum, slider.Minimum + separation);
        var minimum = Math.Min(maximum, slider.LowerValue + separation);
        return SnapWithinBounds(AsFinite(baseValue, maximum), minimum, maximum, separation);
    }

    private static double AsFinite(object value, double fallback)
    {
        return value is double number && double.IsFinite(number) ? number : fallback;
    }

    private static double SnapWithinBounds(double value, double minimum, double maximum, double step)
    {
        var clamped = Math.Clamp(value, minimum, maximum);
        if (clamped == minimum || clamped == maximum)
        {
            return clamped;
        }

        var snapped = Math.Round(clamped / step, 0, MidpointRounding.AwayFromZero) * step;
        return Math.Clamp(snapped, minimum, maximum);
    }

    private enum DragThumb
    {
        Lower,
        Upper
    }
}
