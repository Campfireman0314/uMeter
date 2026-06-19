using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class SpectrumRollView : FrameworkElement
{
    private const int MaximumHistoryColumns = 240;
    private const double PreferredPlotHeight = 140;
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
    private static readonly double[] MajorFrequencies = [20d, 100d, 1000d, 4000d, 12000d, 22000d];
    private static readonly double[] MinorFrequencies = [31.5d, 50d, 63d, 250d, 500d, 2000d, 8000d, 16000d];
    private WriteableBitmap? _bitmap;
    private int[] _pixels = [];
    private double[] _levels = [];
    private int _bitmapBucketCount;
    private int _filledColumns;
    private Color _bitmapAccent;
    private Color _bitmapWarm;
    private bool _isPanning;
    private Point _panStart;
    private double _panStartCenterMel;
    private double _panStartMelSpan;
    private double _viewportCenterMel = (MinimumMel + MaximumMel) / 2;
    private double _viewportMelSpan = DomainMelSpan;

    public SpectrumRollView()
    {
        Cursor = Cursors.SizeNS;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);
        SnapsToDevicePixels = true;
        Loaded += (_, _) => RecolorBitmapIfNeeded();
    }

    public static readonly DependencyProperty SpectrumBucketsProperty = DependencyProperty.Register(
        nameof(SpectrumBuckets),
        typeof(IReadOnlyList<double>),
        typeof(SpectrumRollView),
        new FrameworkPropertyMetadata(null, SpectrumBucketsChanged));

    public IReadOnlyList<double>? SpectrumBuckets
    {
        get => (IReadOnlyList<double>?)GetValue(SpectrumBucketsProperty);
        set => SetValue(SpectrumBucketsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        return new Size(width, 160);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        RecolorBitmapIfNeeded();

        var surfaceBrush = ThemedDrawing.GetBrush(this, "MeterTrackBrush", Color.FromRgb(17, 20, 25));
        var outlinePen = ThemedDrawing.GetPen(this, "ChromeBorderBrush", 1, Color.FromRgb(58, 66, 77));
        drawingContext.DrawRoundedRectangle(surfaceBrush, outlinePen, new Rect(0, 0, width, height), 6, 6);

        var plot = GetPlotRect(width, height);
        NormalizeViewport(plot);
        DrawFrequencyScale(drawingContext, plot);
        DrawTimeScale(drawingContext, plot);
        DrawSpectrumRoll(drawingContext, plot);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ClickCount >= 2)
        {
            ResetViewport();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _isPanning = true;
        _panStart = e.GetPosition(this);
        var viewport = GetEffectiveViewport(GetPlotRect());
        _panStartCenterMel = viewport.Center;
        _panStartMelSpan = viewport.Span;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isPanning)
        {
            return;
        }

        var deltaY = e.GetPosition(this).Y - _panStart.Y;
        var plot = GetPlotRect();
        var melDelta = deltaY * _panStartMelSpan / plot.Height;
        SetViewport(_panStartCenterMel + melDelta, _viewportMelSpan, plot);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var plot = GetPlotRect();
        var viewport = GetEffectiveViewport(plot);
        var mouseY = Math.Clamp(e.GetPosition(this).Y, plot.Top, plot.Bottom);
        var anchorRatio = (plot.Bottom - mouseY) / plot.Height;
        var anchorMel = viewport.Minimum + (viewport.Span * anchorRatio);
        var zoom = Math.Pow(1.16, e.Delta / 120.0);
        var newSpan = Math.Clamp(viewport.Span / zoom, MinimumMelViewportSpan, DomainMelSpan);
        var effectiveNewSpan = Math.Min(newSpan, GetLayoutLimitedSpan(plot));
        var newCenter = anchorMel + (effectiveNewSpan * (0.5 - anchorRatio));
        SetViewport(newCenter, newSpan, plot);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _isPanning = false;
    }

    private void DrawFrequencyScale(DrawingContext drawingContext, Rect plot)
    {
        foreach (var frequency in MinorFrequencies)
        {
            var y = FrequencyToY(plot, frequency);
            if (!IsVisibleY(plot, y))
            {
                continue;
            }

            drawingContext.DrawLine(GridPen(), new Point(plot.Left, y), new Point(plot.Right, y));
            drawingContext.DrawLine(TickPen(), new Point(plot.Left - 3, y), new Point(plot.Left, y));
        }

        foreach (var frequency in MajorFrequencies)
        {
            var y = FrequencyToY(plot, frequency);
            if (!IsVisibleY(plot, y))
            {
                continue;
            }

            drawingContext.DrawLine(GridPen(), new Point(plot.Left, y), new Point(plot.Right, y));
            drawingContext.DrawLine(TickPen(), new Point(plot.Left - 6, y), new Point(plot.Left, y));
            DrawLabel(drawingContext, FormatFrequency(frequency), new Point(7, y - 7));
        }
    }

    private void DrawTimeScale(DrawingContext drawingContext, Rect plot)
    {
        for (var index = 1; index < 4; index++)
        {
            var x = plot.Left + (plot.Width * index / 4);
            drawingContext.DrawLine(GridPen(), new Point(x, plot.Top), new Point(x, plot.Bottom));
            drawingContext.DrawLine(TickPen(), new Point(x, plot.Bottom), new Point(x, plot.Bottom + 5));
        }
    }

    private void DrawSpectrumRoll(DrawingContext drawingContext, Rect plot)
    {
        if (_bitmap is null || _filledColumns == 0)
        {
            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, "SubtleTextBrush", 28, Color.FromRgb(174, 183, 194)),
                null,
                plot,
                4,
                4);
            return;
        }

        var top = MelToY(plot, MaximumMel);
        var bottom = MelToY(plot, MinimumMel);
        drawingContext.PushClip(new RectangleGeometry(plot));
        drawingContext.DrawImage(_bitmap, new Rect(plot.Left, top, plot.Width, bottom - top));
        drawingContext.Pop();
    }

    private int LevelPixel(double value)
    {
        return ColorToBgra(ThemedDrawing.SpectrumPixel(CurrentAccent(), CurrentWarm(), value));
    }

    private static int ColorToBgra(Color color)
    {
        return color.B | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }

    private static string FormatFrequency(double frequency)
    {
        return frequency >= 1000 ? $"{frequency / 1000:0.#}k" : $"{frequency:0}";
    }

    private double FrequencyToY(Rect plot, double frequency)
    {
        return MelToY(plot, SpectrumScale.HertzToMel(Math.Clamp(frequency, SpectrumScale.MinimumFrequencyHz, SpectrumScale.MaximumFrequencyHz)));
    }

    private double MelToY(Rect plot, double mel)
    {
        var viewport = GetEffectiveViewport(plot);
        var ratio = (mel - viewport.Minimum) / viewport.Span;
        return plot.Bottom - (plot.Height * ratio);
    }

    private Rect GetPlotRect()
    {
        return GetPlotRect(ActualWidth, ActualHeight);
    }

    private static Rect GetPlotRect(double width, double height)
    {
        return new Rect(42, 9, Math.Max(1, width - 52), Math.Max(1, height - 20));
    }

    private static bool IsVisibleY(Rect plot, double y)
    {
        return y >= plot.Top && y <= plot.Bottom;
    }

    private static double DomainMelSpan => MaximumMel - MinimumMel;

    private static double MinimumMelViewportSpan => DomainMelSpan / 32;

    private void ResetViewport()
    {
        _viewportCenterMel = (MinimumMel + MaximumMel) / 2;
        _viewportMelSpan = DomainMelSpan;
    }

    private void NormalizeViewport(Rect plot)
    {
        SetViewport(_viewportCenterMel, _viewportMelSpan, plot);
    }

    private void SetViewport(double centerMel, double spanMel, Rect plot)
    {
        _viewportMelSpan = Math.Clamp(spanMel, MinimumMelViewportSpan, DomainMelSpan);
        var effectiveSpan = Math.Min(_viewportMelSpan, GetLayoutLimitedSpan(plot));
        _viewportCenterMel = ClampCenter(centerMel, effectiveSpan);
    }

    private Viewport GetEffectiveViewport(Rect plot)
    {
        var span = Math.Min(Math.Clamp(_viewportMelSpan, MinimumMelViewportSpan, DomainMelSpan), GetLayoutLimitedSpan(plot));
        var center = ClampCenter(_viewportCenterMel, span);
        return new Viewport(center - (span / 2), span);
    }

    private static double GetLayoutLimitedSpan(Rect plot)
    {
        var ratio = Math.Clamp(plot.Height / PreferredPlotHeight, MinimumMelViewportSpan / DomainMelSpan, 1);
        return Math.Clamp(DomainMelSpan * ratio, MinimumMelViewportSpan, DomainMelSpan);
    }

    private static double ClampCenter(double center, double span)
    {
        var halfSpan = span / 2;
        return Math.Clamp(center, MinimumMel + halfSpan, MaximumMel - halfSpan);
    }

    private readonly record struct Viewport(double Minimum, double Span)
    {
        public double Center => Minimum + (Span / 2);
    }

    private void DrawLabel(DrawingContext drawingContext, string text, Point origin)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            ThemedDrawing.GetBrush(this, "SubtleTextBrush", 180, Color.FromRgb(174, 183, 194)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        drawingContext.DrawText(formatted, origin);
    }

    private static void SpectrumBucketsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not SpectrumRollView view)
        {
            return;
        }

        if (args.NewValue is not IReadOnlyList<double> buckets || buckets.Count == 0)
        {
            view.ClearSpectrum();
            return;
        }

        view.AppendSpectrumColumn(buckets);
    }

    private void AppendSpectrumColumn(IReadOnlyList<double> buckets)
    {
        EnsureBitmap(buckets.Count);
        ShiftBitmapLeft();
        WriteRightmostColumn(buckets);
        _filledColumns = Math.Min(MaximumHistoryColumns, _filledColumns + 1);
        _bitmap?.WritePixels(
            new Int32Rect(0, 0, MaximumHistoryColumns, _bitmapBucketCount),
            _pixels,
            MaximumHistoryColumns * sizeof(int),
            0);
        InvalidateVisual();
    }

    private void EnsureBitmap(int bucketCount)
    {
        if (_bitmap is not null && _bitmapBucketCount == bucketCount)
        {
            return;
        }

        _bitmapBucketCount = bucketCount;
        _filledColumns = 0;
        _pixels = new int[MaximumHistoryColumns * bucketCount];
        _levels = new double[MaximumHistoryColumns * bucketCount];
        _bitmap = new WriteableBitmap(
            MaximumHistoryColumns,
            bucketCount,
            96,
            96,
            PixelFormats.Bgra32,
            null);
        _bitmapAccent = CurrentAccent();
        _bitmapWarm = CurrentWarm();
    }

    private void ShiftBitmapLeft()
    {
        if (_bitmapBucketCount == 0 || _filledColumns == 0)
        {
            return;
        }

        for (var row = 0; row < _bitmapBucketCount; row++)
        {
            var rowOffset = row * MaximumHistoryColumns;
            Array.Copy(_pixels, rowOffset + 1, _pixels, rowOffset, MaximumHistoryColumns - 1);
            Array.Copy(_levels, rowOffset + 1, _levels, rowOffset, MaximumHistoryColumns - 1);
        }
    }

    private void WriteRightmostColumn(IReadOnlyList<double> buckets)
    {
        var x = MaximumHistoryColumns - 1;
        for (var row = 0; row < _bitmapBucketCount; row++)
        {
            _pixels[(row * MaximumHistoryColumns) + x] = 0;
            _levels[(row * MaximumHistoryColumns) + x] = 0;
        }

        for (var bucket = 0; bucket < buckets.Count; bucket++)
        {
            var value = Math.Clamp(buckets[bucket], 0, 1);
            if (value <= 0)
            {
                continue;
            }

            var row = _bitmapBucketCount - 1 - bucket;
            var index = (row * MaximumHistoryColumns) + x;
            _levels[index] = value;
            _pixels[index] = LevelPixel(value);
        }
    }

    private void ClearSpectrum()
    {
        if (_pixels.Length > 0)
        {
            Array.Clear(_pixels);
            Array.Clear(_levels);
            _bitmap?.WritePixels(
                new Int32Rect(0, 0, MaximumHistoryColumns, _bitmapBucketCount),
                _pixels,
                MaximumHistoryColumns * sizeof(int),
                0);
        }

        _filledColumns = 0;
        InvalidateVisual();
    }

    private Pen GridPen()
    {
        return ThemedDrawing.GetPen(this, "SubtleTextBrush", 42, 1, Color.FromRgb(174, 183, 194));
    }

    private Pen TickPen()
    {
        return ThemedDrawing.GetPen(this, "SubtleTextBrush", 95, 1, Color.FromRgb(174, 183, 194));
    }

    private Color CurrentAccent()
    {
        return ThemedDrawing.GetColor(this, "SignalBrush", Color.FromRgb(46, 196, 182));
    }

    private Color CurrentWarm()
    {
        return ThemedDrawing.GetColor(this, "SpectrumWarmBrush", Color.FromRgb(255, 176, 0));
    }

    private void RecolorBitmapIfNeeded()
    {
        if (_bitmap is null || _pixels.Length == 0 || _levels.Length == 0)
        {
            return;
        }

        var accent = CurrentAccent();
        var warm = CurrentWarm();
        if (accent == _bitmapAccent && warm == _bitmapWarm)
        {
            return;
        }

        _bitmapAccent = accent;
        _bitmapWarm = warm;
        for (var index = 0; index < _pixels.Length; index++)
        {
            var value = _levels[index];
            _pixels[index] = value > 0 ? LevelPixel(value) : 0;
        }

        _bitmap.WritePixels(
            new Int32Rect(0, 0, MaximumHistoryColumns, _bitmapBucketCount),
            _pixels,
            MaximumHistoryColumns * sizeof(int),
            0);
    }
}
