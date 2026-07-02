using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSelectedDetailShellPlanner : IDisposable
{
    private const int StringFieldComparisonSourceKey = 0;
    private const int StringFieldComparisonDisplayTitle = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSelectedDetailShellPlanner()
    {
        var result = NativeMethods.DlmHostSelectedDetailShellCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host selected-detail shell planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSelectedDetailShellPlan Project(
        IReadOnlyList<NativeHostSelectedDetailShellSourceInput> sources,
        IReadOnlyList<NativeHostSelectedDetailShellModuleInput> modules,
        string? selectedSourceKey,
        string? comparisonSourceKey,
        double? levelFloorDb,
        double? prominenceThresholdDb,
        double? relativeThreshold,
        CollisionFrequencyScale? frequencyScale)
    {
        using var planner = new NativeHostSelectedDetailShellPlanner();
        return planner.ProjectShell(
            sources,
            modules,
            selectedSourceKey,
            comparisonSourceKey,
            levelFloorDb,
            prominenceThresholdDb,
            relativeThreshold,
            frequencyScale);
    }

    public NativeHostSelectedDetailShellPlan ProjectShell(
        IReadOnlyList<NativeHostSelectedDetailShellSourceInput> sources,
        IReadOnlyList<NativeHostSelectedDetailShellModuleInput> modules,
        string? selectedSourceKey,
        string? comparisonSourceKey,
        double? levelFloorDb,
        double? prominenceThresholdDb,
        double? relativeThreshold,
        CollisionFrequencyScale? frequencyScale)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostSelectedDetailShellClearExport(_handle), "Native selected-detail shell clear failed");

        foreach (var source in sources)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailShellAddSourceExport(_handle, source.SourceKey, source.DisplayTitle),
                "Native selected-detail shell add-source failed");
        }

        foreach (var module in modules)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailShellAddModuleExport(_handle, (int)module.Kind, module.IsVisible ? 1 : 0),
                "Native selected-detail shell add-module failed");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailShellProjectExport(
                _handle,
                selectedSourceKey,
                comparisonSourceKey,
                levelFloorDb.HasValue ? 1 : 0,
                levelFloorDb.GetValueOrDefault(),
                prominenceThresholdDb.HasValue ? 1 : 0,
                prominenceThresholdDb.GetValueOrDefault(),
                relativeThreshold.HasValue ? 1 : 0,
                relativeThreshold.GetValueOrDefault(),
                frequencyScale.HasValue ? 1 : 0,
                (int)(frequencyScale ?? CollisionFrequencyScale.Logarithmic)),
            "Native selected-detail shell project failed");

        return ReadPlan();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostSelectedDetailShellDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private NativeHostSelectedDetailShellPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailShellGetPlanExport(_handle, out var plan),
            "Native selected-detail shell plan read failed");
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailShellGetComparisonIndexCountExport(_handle, out var comparisonIndexCount),
            "Native selected-detail shell comparison count read failed");

        var comparisonIndexes = new int[comparisonIndexCount];
        for (var index = 0; index < comparisonIndexCount; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailShellGetComparisonIndexAtExport(_handle, index, out comparisonIndexes[index]),
                "Native selected-detail shell comparison index read failed");
        }

        return new NativeHostSelectedDetailShellPlan(
            plan.HasSelectedSource != 0,
            plan.HasSelectedSource == 0 ? null : plan.SelectedSourceIndex,
            plan.IsWaveformVisible != 0,
            plan.IsSpectrumVisible != 0,
            plan.IsStereoVisible != 0,
            plan.IsLoudnessClockVisible != 0,
            plan.IsCollisionVisible != 0,
            plan.ComparisonSourceKeyUtf8ByteCount <= 0 ? null : CopyString(StringFieldComparisonSourceKey, plan.ComparisonSourceKeyUtf8ByteCount),
            plan.ComparisonDisplayTitleUtf8ByteCount <= 0 ? null : CopyString(StringFieldComparisonDisplayTitle, plan.ComparisonDisplayTitleUtf8ByteCount),
            plan.LevelFloorDb,
            plan.ProminenceThresholdDb,
            plan.RelativeThreshold,
            ToFrequencyScale(plan.FrequencyScale),
            comparisonIndexes);
    }

    private string CopyString(int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailShellCopyStringExport(_handle, field, buffer, buffer.Length, out var actualCount),
            "Native selected-detail shell string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static CollisionFrequencyScale ToFrequencyScale(int value)
    {
        return Enum.IsDefined(typeof(CollisionFrequencyScale), value)
            ? (CollisionFrequencyScale)value
            : CollisionFrequencyScale.Logarithmic;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSelectedDetailShellPlan
    {
        public int HasSelectedSource;
        public int SelectedSourceIndex;
        public int IsWaveformVisible;
        public int IsSpectrumVisible;
        public int IsStereoVisible;
        public int IsLoudnessClockVisible;
        public int IsCollisionVisible;
        public int ComparisonSourceKeyUtf8ByteCount;
        public int ComparisonDisplayTitleUtf8ByteCount;
        public double LevelFloorDb;
        public double ProminenceThresholdDb;
        public double RelativeThreshold;
        public int FrequencyScale;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSelectedDetailShellDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayTitle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellAddModuleExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellAddModuleExport(
            IntPtr handle,
            int kind,
            int isVisible);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellProjectExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? comparisonSourceKey,
            int hasLevelFloorDb,
            double levelFloorDb,
            int hasProminenceThresholdDb,
            double prominenceThresholdDb,
            int hasRelativeThreshold,
            double relativeThreshold,
            int hasFrequencyScale,
            int frequencyScale);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellGetPlanExport(
            IntPtr handle,
            out NativeSelectedDetailShellPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellGetComparisonIndexCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellGetComparisonIndexCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellGetComparisonIndexAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellGetComparisonIndexAtExport(IntPtr handle, int index, out int sourceIndex);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailShellCopyStringExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailShellCopyStringExport(
            IntPtr handle,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSelectedDetailShellSourceInput(string SourceKey, string DisplayTitle);

internal sealed record NativeHostSelectedDetailShellModuleInput(DetailModuleKind Kind, bool IsVisible);

internal sealed record NativeHostSelectedDetailShellPlan(
    bool HasSelectedSource,
    int? SelectedSourceIndex,
    bool IsWaveformVisible,
    bool IsSpectrumVisible,
    bool IsStereoVisible,
    bool IsLoudnessClockVisible,
    bool IsCollisionVisible,
    string? ComparisonSourceKey,
    string? ComparisonDisplayName,
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    CollisionFrequencyScale FrequencyScale,
    IReadOnlyList<int> ComparisonSourceIndexes);
