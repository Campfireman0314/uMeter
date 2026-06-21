using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class SpectrumRollView : FrameworkElement
{
    private const int MaximumHistoryColumns = 240;
    private const double PreferredPlotHeight = 140;
    private const double AnalyzerFloorDb = -200;
    private const double AnalyzerCeilingDb = 0;
    private const double AnalyzerRangeDb = AnalyzerCeilingDb - AnalyzerFloorDb;
    private const double DefaultDisplayFloorDb = -120;
    private const double MinimumDisplayRangeDb = 1;
    private const double SoftFloorKneeDb = 9;
    private const double SoftFloorMaximumDisplayValue = 0.16;
    private const double VerticalSmoothingWeight = 0.18;
    private const int PendingBitmapLockHResult = unchecked((int)0x88982F0D);
    private static readonly double MinimumMel = SpectrumScale.HertzToMel(SpectrumScale.MinimumFrequencyHz);
    private static readonly double MaximumMel = SpectrumScale.HertzToMel(SpectrumScale.MaximumFrequencyHz);
    private static readonly double[] MajorFrequencies = [20d, 100d, 1000d, 4000d, 12000d, 22000d];
    private static readonly double[] MinorFrequencies = [31.5d, 50d, 63d, 250d, 500d, 2000d, 8000d, 16000d];
    private WriteableBitmap? _bitmap;
    private int[] _pixels = [];
    private int[] _columnPixels = [];
    private double[] _levels = [];
    private int _bitmapBucketCount;
    private int _filledColumns;
    private int _nextColumnIndex;
    private IReadOnlyList<double>? _pendingSpectrumBuckets;
    private bool _bitmapNeedsFullWrite;
    private bool _isBitmapWriteRetryQueued;
    private readonly DeferredRenderUpdate _spectrumUpdate;
    private readonly PlotBackgroundCache _backgroundCache = new();
    private Color _bitmapAccent;
    private Color _bitmapWarm;
    private double _bitmapDisplayFloorDb = DefaultDisplayFloorDb;
    private double _bitmapDisplayCeilingDb = AnalyzerCeilingDb;
    private bool _isPanning;
    private Point _panStart;
    private double _panStartCenterMel;
    private double _panStartMelSpan;
    private double _viewportCenterMel = (MinimumMel + MaximumMel) / 2;
    private double _viewportMelSpan = DomainMelSpan;

    public SpectrumRollView()
    {
        _spectrumUpdate = new DeferredRenderUpdate(this, CanApplySpectrumUpdate, ApplyPendingSpectrumUpdate);
        Cursor = Cursors.SizeNS;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);
        SnapsToDevicePixels = true;
        Loaded += (_, _) =>
        {
            RecolorBitmapIfNeeded();
            FlushPendingBitmapWriteIfReady();
            ApplyPendingSpectrumUpdateIfReady();
        };
        IsVisibleChanged += (_, _) =>
        {
            FlushPendingBitmapWriteIfReady();
            ApplyPendingSpectrumUpdateIfReady();
        };
        SizeChanged += (_, _) =>
        {
            FlushPendingBitmapWriteIfReady();
            ApplyPendingSpectrumUpdateIfReady();
        };
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

    public static readonly DependencyProperty PlotTickProperty = DependencyProperty.Register(
        nameof(PlotTick),
        typeof(long),
        typeof(SpectrumRollView),
        new FrameworkPropertyMetadata(0L, PlotTickChanged));

    public long PlotTick
    {
        get => (long)GetValue(PlotTickProperty);
        set => SetValue(PlotTickProperty, value);
    }

    public static readonly DependencyProperty DisplayFloorDbProperty = DependencyProperty.Register(
        nameof(DisplayFloorDb),
        typeof(double),
        typeof(SpectrumRollView),
        new FrameworkPropertyMetadata(DefaultDisplayFloorDb, DisplayRangeChanged));

    public double DisplayFloorDb
    {
        get => (double)GetValue(DisplayFloorDbProperty);
        set => SetValue(DisplayFloorDbProperty, value);
    }

    public static readonly DependencyProperty DisplayCeilingDbProperty = DependencyProperty.Register(
        nameof(DisplayCeilingDb),
        typeof(double),
        typeof(SpectrumRollView),
        new FrameworkPropertyMetadata(AnalyzerCeilingDb, DisplayRangeChanged));

    public double DisplayCeilingDb
    {
        get => (double)GetValue(DisplayCeilingDbProperty);
        set => SetValue(DisplayCeilingDbProperty, value);
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

        var plot = GetPlotRect(width, height);
        NormalizeViewport(plot);
        DrawBackground(drawingContext, width, height, plot);
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
        InvalidateBackground();
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
        InvalidateBackground();
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

    private void DrawBackground(DrawingContext drawingContext, double width, double height, Rect plot)
    {
        var viewport = GetEffectiveViewport(plot);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var trackColor = CurrentTrack();
        var outlineColor = CurrentOutline();
        var subtleColor = CurrentSubtle();

        drawingContext.DrawDrawing(_backgroundCache.GetOrCreate(
            width,
            height,
            pixelsPerDip,
            viewport.Minimum,
            viewport.Span,
            trackColor,
            outlineColor,
            subtleColor,
            () => CreateBackgroundDrawing(width, height, plot)));
    }

    private DrawingGroup CreateBackgroundDrawing(double width, double height, Rect plot)
    {
        var group = new DrawingGroup();
        using (var drawingContext = group.Open())
        {
            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, PlotColorRole.Track),
                ThemedDrawing.GetPen(this, PlotColorRole.Outline, 1),
                new Rect(0, 0, width, height),
                6,
                6);
            DrawFrequencyScale(drawingContext, plot);
            DrawTimeScale(drawingContext, plot);
        }

        if (group.CanFreeze)
        {
            group.Freeze();
        }

        return group;
    }

    private void DrawSpectrumRoll(DrawingContext drawingContext, Rect plot)
    {
        if (_bitmap is null || _filledColumns == 0)
        {
            drawingContext.DrawRoundedRectangle(
                ThemedDrawing.GetBrush(this, PlotColorRole.Subtle, 28),
                null,
                plot,
                4,
                4);
            return;
        }

        var top = MelToY(plot, MaximumMel);
        var bottom = MelToY(plot, MinimumMel);
        drawingContext.PushClip(new RectangleGeometry(plot));
        var destination = new Rect(plot.Left, top, plot.Width, bottom - top);
        if (_filledColumns < MaximumHistoryColumns)
        {
            var width = destination.Width * _filledColumns / MaximumHistoryColumns;
            DrawBitmapSegment(
                drawingContext,
                0,
                _filledColumns,
                new Rect(destination.Right - width, destination.Top, width, destination.Height));
        }
        else if (_nextColumnIndex == 0)
        {
            drawingContext.DrawImage(_bitmap, destination);
        }
        else
        {
            var firstWidth = MaximumHistoryColumns - _nextColumnIndex;
            var firstDestinationWidth = destination.Width * firstWidth / MaximumHistoryColumns;
            DrawBitmapSegment(
                drawingContext,
                _nextColumnIndex,
                firstWidth,
                new Rect(destination.Left, destination.Top, firstDestinationWidth, destination.Height));
            DrawBitmapSegment(
                drawingContext,
                0,
                _nextColumnIndex,
                new Rect(destination.Left + firstDestinationWidth, destination.Top, destination.Width - firstDestinationWidth, destination.Height));
        }

        drawingContext.Pop();
    }

    private void DrawBitmapSegment(DrawingContext drawingContext, int sourceX, int sourceWidth, Rect destination)
    {
        if (_bitmap is null || sourceWidth <= 0 || destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        drawingContext.DrawImage(
            new CroppedBitmap(_bitmap, new Int32Rect(sourceX, 0, sourceWidth, _bitmapBucketCount)),
            destination);
    }

    private static double DisplayValue(double normalizedLevel, DisplayTransform transform)
    {
        if (normalizedLevel <= 0)
        {
            return 0;
        }

        var level = Math.Clamp(normalizedLevel, 0, 1);
        if (level <= transform.FloorLevel)
        {
            if (level <= transform.SoftFloorLevel)
            {
                return 0;
            }

            var softRatio = (level - transform.SoftFloorLevel) / (transform.FloorLevel - transform.SoftFloorLevel);
            return Math.Clamp(softRatio * softRatio * SoftFloorMaximumDisplayValue, 0, SoftFloorMaximumDisplayValue);
        }

        var visibleRatio = Math.Clamp((level - transform.FloorLevel) / transform.LevelRange, 0, 1);
        return Math.Clamp(SoftFloorMaximumDisplayValue + ((1 - SoftFloorMaximumDisplayValue) * visibleRatio), 0, 1);
    }

    private static int LevelPixel(double value, DisplayTransform transform, Color accent, Color warm)
    {
        var displayValue = DisplayValue(value, transform);
        return displayValue > 0
            ? ColorToBgra(ThemedDrawing.SpectrumPixel(accent, warm, displayValue))
            : 0;
    }

    private (double FloorDb, double CeilingDb) DisplayRange()
    {
        var floorDb = double.IsFinite(DisplayFloorDb) ? DisplayFloorDb : AnalyzerFloorDb;
        var ceilingDb = double.IsFinite(DisplayCeilingDb) ? DisplayCeilingDb : AnalyzerCeilingDb;
        floorDb = Math.Clamp(floorDb, AnalyzerFloorDb, AnalyzerCeilingDb - MinimumDisplayRangeDb);
        ceilingDb = Math.Clamp(ceilingDb, floorDb + MinimumDisplayRangeDb, AnalyzerCeilingDb);
        return (floorDb, ceilingDb);
    }

    private static DisplayTransform CreateDisplayTransform(double floorDb, double ceilingDb)
    {
        var floorLevel = Math.Clamp((floorDb - AnalyzerFloorDb) / AnalyzerRangeDb, 0, 1);
        var ceilingLevel = Math.Clamp((ceilingDb - AnalyzerFloorDb) / AnalyzerRangeDb, floorLevel + double.Epsilon, 1);
        var softFloorLevel = Math.Clamp(((floorDb - SoftFloorKneeDb) - AnalyzerFloorDb) / AnalyzerRangeDb, 0, floorLevel);
        return new DisplayTransform(softFloorLevel, floorLevel, ceilingLevel - floorLevel);
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
            ThemedDrawing.GetBrush(this, PlotColorRole.Subtle, 180),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        drawingContext.DrawText(formatted, origin);
    }

    private static void SpectrumBucketsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not SpectrumRollView view)
        {
            return;
        }

        view.ScheduleSpectrumUpdate(args.NewValue as IReadOnlyList<double>);
    }

    private static void PlotTickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is SpectrumRollView view)
        {
            view.ScheduleSpectrumUpdate(view.SpectrumBuckets);
        }
    }

    private static void DisplayRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not SpectrumRollView view)
        {
            return;
        }

        view.RecolorBitmapIfNeeded();
        view.InvalidateVisual();
    }

    private void ScheduleSpectrumUpdate(IReadOnlyList<double>? buckets)
    {
        _pendingSpectrumBuckets = buckets is { Count: > 0 } ? buckets : null;
        _spectrumUpdate.Request();
    }

    private void ApplyPendingSpectrumUpdateIfReady()
    {
        _spectrumUpdate.TryApply();
    }

    private void ApplyPendingSpectrumUpdate()
    {
        if (_pendingSpectrumBuckets is not { Count: > 0 } buckets)
        {
            ClearSpectrum();
            return;
        }

        AppendSpectrumColumn(buckets);
    }

    private bool CanApplySpectrumUpdate()
    {
        return IsLoaded
            && IsVisible
            && Visibility == Visibility.Visible
            && ActualWidth > 0
            && ActualHeight > 0;
    }

    private void AppendSpectrumColumn(IReadOnlyList<double> buckets)
    {
        EnsureBitmap(buckets.Count);
        var column = _nextColumnIndex;
        WriteColumn(column, buckets);
        _filledColumns = Math.Min(MaximumHistoryColumns, _filledColumns + 1);
        _nextColumnIndex = (_nextColumnIndex + 1) % MaximumHistoryColumns;

        if (_bitmapNeedsFullWrite)
        {
            FlushPendingBitmapWriteIfReady();
        }
        else if (!TryWriteBitmap(
            new Int32Rect(column, 0, 1, _bitmapBucketCount),
            _columnPixels,
            sizeof(int),
            0))
        {
            MarkBitmapWritePending();
        }

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
        _nextColumnIndex = 0;
        _pixels = new int[MaximumHistoryColumns * bucketCount];
        _columnPixels = new int[bucketCount];
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
        (_bitmapDisplayFloorDb, _bitmapDisplayCeilingDb) = DisplayRange();
        _bitmapNeedsFullWrite = false;
    }

    private void WriteColumn(int x, IReadOnlyList<double> buckets)
    {
        var (floorDb, ceilingDb) = DisplayRange();
        var transform = CreateDisplayTransform(floorDb, ceilingDb);
        var accent = CurrentAccent();
        var warm = CurrentWarm();

        for (var row = 0; row < _bitmapBucketCount; row++)
        {
            _pixels[(row * MaximumHistoryColumns) + x] = 0;
            _levels[(row * MaximumHistoryColumns) + x] = 0;
            _columnPixels[row] = 0;
        }

        for (var bucket = 0; bucket < buckets.Count; bucket++)
        {
            var value = Math.Clamp(buckets[bucket], 0, 1);
            var row = _bitmapBucketCount - 1 - bucket;
            var index = (row * MaximumHistoryColumns) + x;
            var pixel = LevelPixel(SmoothedColumnValue(buckets, bucket), transform, accent, warm);
            _levels[index] = value;
            _pixels[index] = pixel;
            _columnPixels[row] = pixel;
        }

        FillIsolatedColumnGaps(x);
    }

    private static double SmoothedColumnValue(IReadOnlyList<double> buckets, int bucket)
    {
        var value = Math.Clamp(buckets[bucket], 0, 1);
        var previous = bucket > 0 ? Math.Clamp(buckets[bucket - 1], 0, 1) : value;
        var next = bucket < buckets.Count - 1 ? Math.Clamp(buckets[bucket + 1], 0, 1) : value;
        var neighborAverage = (previous + next) * 0.5;
        return Math.Clamp((value * (1 - VerticalSmoothingWeight)) + (neighborAverage * VerticalSmoothingWeight), 0, 1);
    }

    private void FillIsolatedColumnGaps(int x)
    {
        for (var row = 1; row < _bitmapBucketCount - 1; row++)
        {
            if (_columnPixels[row] != 0)
            {
                continue;
            }

            var upper = _columnPixels[row - 1];
            var lower = _columnPixels[row + 1];
            if (upper == 0 || lower == 0)
            {
                continue;
            }

            var fill = BlendBgra(upper, lower);
            _columnPixels[row] = fill;
            _pixels[(row * MaximumHistoryColumns) + x] = fill;
        }
    }

    private static int BlendBgra(int first, int second)
    {
        var blue = (((first & 0xFF) + (second & 0xFF)) / 2) & 0xFF;
        var green = (((first >> 8 & 0xFF) + (second >> 8 & 0xFF)) / 2) & 0xFF;
        var red = (((first >> 16 & 0xFF) + (second >> 16 & 0xFF)) / 2) & 0xFF;
        var alpha = (((first >> 24 & 0xFF) + (second >> 24 & 0xFF)) / 2) & 0xFF;
        return blue | (green << 8) | (red << 16) | (alpha << 24);
    }

    private void ClearSpectrum()
    {
        if (_pixels.Length > 0)
        {
            Array.Clear(_pixels);
            Array.Clear(_levels);
            if (!TryWriteFullBitmap())
            {
                MarkBitmapWritePending();
            }
        }

        _filledColumns = 0;
        _nextColumnIndex = 0;
        InvalidateVisual();
    }

    private Pen GridPen()
    {
        return ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 42, 1);
    }

    private Pen TickPen()
    {
        return ThemedDrawing.GetPen(this, PlotColorRole.Subtle, 95, 1);
    }

    private Color CurrentAccent()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Signal);
    }

    private Color CurrentWarm()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.SpectrumWarm);
    }

    private Color CurrentTrack()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Track);
    }

    private Color CurrentOutline()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Outline);
    }

    private Color CurrentSubtle()
    {
        return ThemedDrawing.GetColor(this, PlotColorRole.Subtle);
    }

    private void InvalidateBackground()
    {
        _backgroundCache.Invalidate();
    }

    private static bool AreClose(double first, double second)
    {
        return Math.Abs(first - second) < 0.5;
    }

    private void RecolorBitmapIfNeeded()
    {
        if (_bitmap is null || _pixels.Length == 0 || _levels.Length == 0)
        {
            return;
        }

        var accent = CurrentAccent();
        var warm = CurrentWarm();
        var (floorDb, ceilingDb) = DisplayRange();
        if (accent == _bitmapAccent
            && warm == _bitmapWarm
            && AreClose(floorDb, _bitmapDisplayFloorDb)
            && AreClose(ceilingDb, _bitmapDisplayCeilingDb))
        {
            return;
        }

        _bitmapAccent = accent;
        _bitmapWarm = warm;
        _bitmapDisplayFloorDb = floorDb;
        _bitmapDisplayCeilingDb = ceilingDb;
        var transform = CreateDisplayTransform(floorDb, ceilingDb);
        for (var row = 0; row < _bitmapBucketCount; row++)
        {
            for (var column = 0; column < MaximumHistoryColumns; column++)
            {
                var index = (row * MaximumHistoryColumns) + column;
                var value = SmoothedStoredValue(row, column);
                _pixels[index] = LevelPixel(value, transform, accent, warm);
            }
        }

        if (!TryWriteFullBitmap())
        {
            MarkBitmapWritePending();
        }
    }

    private double SmoothedStoredValue(int row, int column)
    {
        var index = (row * MaximumHistoryColumns) + column;
        var value = _levels[index];
        var previous = row > 0 ? _levels[((row - 1) * MaximumHistoryColumns) + column] : value;
        var next = row < _bitmapBucketCount - 1 ? _levels[((row + 1) * MaximumHistoryColumns) + column] : value;
        var neighborAverage = (previous + next) * 0.5;
        return Math.Clamp((value * (1 - VerticalSmoothingWeight)) + (neighborAverage * VerticalSmoothingWeight), 0, 1);
    }

    private bool TryWriteFullBitmap()
    {
        if (_bitmap is null || _pixels.Length == 0 || _bitmapBucketCount <= 0)
        {
            _bitmapNeedsFullWrite = false;
            return true;
        }

        if (!TryWriteBitmap(
            new Int32Rect(0, 0, MaximumHistoryColumns, _bitmapBucketCount),
            _pixels,
            MaximumHistoryColumns * sizeof(int),
            0))
        {
            return false;
        }

        _bitmapNeedsFullWrite = false;
        return true;
    }

    private bool TryWriteBitmap(Int32Rect rect, Array pixels, int stride, int offset)
    {
        if (_bitmap is null)
        {
            return true;
        }

        try
        {
            _bitmap.WritePixels(rect, pixels, stride, offset);
            return true;
        }
        catch (COMException ex) when (ex.HResult == PendingBitmapLockHResult)
        {
            return false;
        }
    }

    private void MarkBitmapWritePending()
    {
        _bitmapNeedsFullWrite = true;
        QueueBitmapWriteRetry();
    }

    private void FlushPendingBitmapWriteIfReady()
    {
        if (!_bitmapNeedsFullWrite || !CanApplySpectrumUpdate())
        {
            return;
        }

        if (!TryWriteFullBitmap())
        {
            QueueBitmapWriteRetry();
            return;
        }

        InvalidateVisual();
    }

    private void QueueBitmapWriteRetry()
    {
        if (_isBitmapWriteRetryQueued || !CanApplySpectrumUpdate())
        {
            return;
        }

        _isBitmapWriteRetryQueued = true;
        Dispatcher.BeginInvoke(
            (Action)(() =>
            {
                _isBitmapWriteRetryQueued = false;
                FlushPendingBitmapWriteIfReady();
            }),
            DispatcherPriority.ContextIdle);
    }

    private readonly record struct DisplayTransform(double SoftFloorLevel, double FloorLevel, double LevelRange);
}
