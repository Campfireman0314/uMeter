using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostChartSettingsCommandPlanner : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostChartSettingsCommandPlanner()
    {
        var result = NativeMethods.DlmHostChartSettingsCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host chart settings planner could not be initialized (code {result}).");
        }
    }

    public static NativeTruePeakOversamplingPlan PlanSetTruePeakOversampling(TruePeakOversampling oversampling)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanTruePeakOversampling(oversampling);
    }

    public static NativeWaveformDisplayModePlan PlanSetWaveformDisplayMode(WaveformDisplayMode displayMode)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanWaveformDisplayMode(displayMode);
    }

    public static NativeSpectrumDisplayModePlan PlanSetSpectrumDisplayMode(SpectrumDisplayMode displayMode)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanSpectrumDisplayMode(displayMode);
    }

    public static NativeDetailModuleVisibilityPlan PlanSetDetailModuleVisibility(DetailModuleKind moduleKind, bool isVisible)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanDetailModuleVisibility(moduleKind, isVisible);
    }

    public static NativeSpectrumDisplayRangePlan PlanSetSpectrumDisplayRange(double floorDb, double ceilingDb)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanSpectrumDisplayRange(floorDb, ceilingDb);
    }

    public static NativeLoudnessClockSettingsPlan PlanSetLoudnessClockSettings(
        double minimumDb,
        double maximumDb,
        double warningDb,
        double cycleSeconds)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanLoudnessClockSettings(minimumDb, maximumDb, warningDb, cycleSeconds);
    }

    public static NativeCollisionSettingsPlan PlanSetCollisionSettings(
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold,
        CollisionFrequencyScale frequencyScale,
        string? comparisonSourceKey)
    {
        using var planner = new NativeHostChartSettingsCommandPlanner();
        return planner.PlanCollisionSettings(levelFloorDb, prominenceThresholdDb, relativeThreshold, frequencyScale, comparisonSourceKey);
    }

    public NativeTruePeakOversamplingPlan PlanTruePeakOversampling(TruePeakOversampling oversampling)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetTruePeakOversamplingExport(_handle, (int)oversampling),
            "Native host chart settings true peak planning failed");
        var plan = ReadPlan();
        return new NativeTruePeakOversamplingPlan(
            (TruePeakOversampling)plan.TruePeakOversampling,
            plan.TruePeakServiceOversampling);
    }

    public NativeWaveformDisplayModePlan PlanWaveformDisplayMode(WaveformDisplayMode displayMode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetWaveformDisplayModeExport(_handle, (int)displayMode),
            "Native host chart settings waveform planning failed");
        var plan = ReadPlan();
        return new NativeWaveformDisplayModePlan(
            (WaveformDisplayMode)plan.WaveformDisplayMode,
            plan.WaveformChannelEnabled != 0);
    }

    public NativeSpectrumDisplayModePlan PlanSpectrumDisplayMode(SpectrumDisplayMode displayMode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetSpectrumDisplayModeExport(_handle, (int)displayMode),
            "Native host chart settings spectrum planning failed");
        var plan = ReadPlan();
        return new NativeSpectrumDisplayModePlan(
            (SpectrumDisplayMode)plan.SpectrumDisplayMode,
            plan.SpectrumChannelEnabled != 0);
    }

    public NativeDetailModuleVisibilityPlan PlanDetailModuleVisibility(DetailModuleKind moduleKind, bool isVisible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetDetailModuleVisibilityExport(_handle, (int)moduleKind, isVisible ? 1 : 0),
            "Native host chart settings detail module visibility planning failed");
        var plan = ReadPlan();
        return new NativeDetailModuleVisibilityPlan(
            (DetailModuleKind)plan.DetailModuleKind,
            plan.DetailModuleVisible != 0);
    }

    public NativeSpectrumDisplayRangePlan PlanSpectrumDisplayRange(double floorDb, double ceilingDb)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetSpectrumDisplayRangeExport(_handle, floorDb, ceilingDb),
            "Native host chart settings spectrum range planning failed");
        var plan = ReadPlan();
        return new NativeSpectrumDisplayRangePlan(plan.SpectrumDisplayFloorDb, plan.SpectrumDisplayCeilingDb);
    }

    public NativeLoudnessClockSettingsPlan PlanLoudnessClockSettings(
        double minimumDb,
        double maximumDb,
        double warningDb,
        double cycleSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetLoudnessClockSettingsExport(
                _handle,
                minimumDb,
                maximumDb,
                warningDb,
                cycleSeconds),
            "Native host chart settings loudness clock planning failed");
        var plan = ReadPlan();
        return new NativeLoudnessClockSettingsPlan(
            plan.LoudnessClockMinimumDb,
            plan.LoudnessClockMaximumDb,
            plan.LoudnessClockWarningDb,
            plan.LoudnessClockCycleSeconds);
    }

    public NativeCollisionSettingsPlan PlanCollisionSettings(
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold,
        CollisionFrequencyScale frequencyScale,
        string? comparisonSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandPlanSetCollisionSettingsExport(
                _handle,
                levelFloorDb,
                prominenceThresholdDb,
                relativeThreshold,
                (int)frequencyScale,
                comparisonSourceKey),
            "Native host chart settings collision planning failed");
        var plan = ReadPlan();
        return new NativeCollisionSettingsPlan(
            plan.CollisionLevelFloorDb,
            plan.CollisionProminenceThresholdDb,
            plan.CollisionRelativeThreshold,
            (CollisionFrequencyScale)plan.CollisionFrequencyScale,
            plan.HasCollisionComparisonSourceKey == 0
                ? null
                : CopyCollisionComparisonSourceKey(plan.CollisionComparisonSourceKeyUtf8ByteCount));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostChartSettingsCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private NativeChartSettingsPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandGetPlanExport(_handle, out var plan),
            "Native host chart settings plan read failed");
        return plan;
    }

    private string CopyCollisionComparisonSourceKey(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostChartSettingsCommandCopyCollisionComparisonSourceKeyExport(_handle, buffer, buffer.Length, out var actualCount),
            "Native host chart settings collision comparison source key copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeChartSettingsPlan
    {
        public int TruePeakOversampling;
        public int TruePeakServiceOversampling;
        public int WaveformDisplayMode;
        public int WaveformChannelEnabled;
        public int SpectrumDisplayMode;
        public int SpectrumChannelEnabled;
        public int DetailModuleKind;
        public int DetailModuleVisible;
        public double SpectrumDisplayFloorDb;
        public double SpectrumDisplayCeilingDb;
        public double LoudnessClockMinimumDb;
        public double LoudnessClockMaximumDb;
        public double LoudnessClockWarningDb;
        public double LoudnessClockCycleSeconds;
        public double CollisionLevelFloorDb;
        public double CollisionProminenceThresholdDb;
        public double CollisionRelativeThreshold;
        public int CollisionFrequencyScale;
        public int HasCollisionComparisonSourceKey;
        public int CollisionComparisonSourceKeyUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostChartSettingsCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetTruePeakOversamplingExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetTruePeakOversamplingExport(IntPtr handle, int oversampling);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetWaveformDisplayModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetWaveformDisplayModeExport(IntPtr handle, int displayMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetSpectrumDisplayModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetSpectrumDisplayModeExport(IntPtr handle, int displayMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetDetailModuleVisibilityExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetDetailModuleVisibilityExport(IntPtr handle, int moduleKind, int isVisible);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetSpectrumDisplayRangeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetSpectrumDisplayRangeExport(IntPtr handle, double floorDb, double ceilingDb);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetLoudnessClockSettingsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetLoudnessClockSettingsExport(
            IntPtr handle,
            double minimumDb,
            double maximumDb,
            double warningDb,
            double cycleSeconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandPlanSetCollisionSettingsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandPlanSetCollisionSettingsExport(
            IntPtr handle,
            double levelFloorDb,
            double prominenceThresholdDb,
            double relativeThreshold,
            int frequencyScale,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? comparisonSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandGetPlanExport(IntPtr handle, out NativeChartSettingsPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostChartSettingsCommandCopyCollisionComparisonSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostChartSettingsCommandCopyCollisionComparisonSourceKeyExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeTruePeakOversamplingPlan(TruePeakOversampling Oversampling, int ServiceOversampling);

internal sealed record NativeWaveformDisplayModePlan(WaveformDisplayMode DisplayMode, bool ChannelEnabled);

internal sealed record NativeSpectrumDisplayModePlan(SpectrumDisplayMode DisplayMode, bool ChannelEnabled);

internal sealed record NativeDetailModuleVisibilityPlan(DetailModuleKind ModuleKind, bool IsVisible);

internal sealed record NativeSpectrumDisplayRangePlan(double FloorDb, double CeilingDb);

internal sealed record NativeLoudnessClockSettingsPlan(
    double MinimumDb,
    double MaximumDb,
    double WarningDb,
    double CycleSeconds);

internal sealed record NativeCollisionSettingsPlan(
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    CollisionFrequencyScale FrequencyScale,
    string? ComparisonSourceKey);
