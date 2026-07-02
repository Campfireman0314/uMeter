using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostRuntimeServiceCommandPlanner : IDisposable
{
    private const int StatusSuccess = 0;
    private const int StatusSourceNotFound = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostRuntimeServiceCommandPlanner()
    {
        var result = NativeMethods.DlmHostRuntimeServiceCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host runtime service planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostRuntimeServiceCommandPlan PlanResetAll()
    {
        using var planner = new NativeHostRuntimeServiceCommandPlanner();
        return planner.PlanAllSourcesReset();
    }

    public static NativeHostRuntimeServiceCommandPlan PlanResetSource(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId)
    {
        using var planner = new NativeHostRuntimeServiceCommandPlanner();
        return planner.PlanSourceReset(sourceKeys, sourceKey, sourceKind, processId);
    }

    public static NativeHostRuntimeServiceCommandPlan PlanSetPerformanceMode(HostPerformanceMode mode)
    {
        using var planner = new NativeHostRuntimeServiceCommandPlanner();
        return planner.PlanPerformanceMode(mode);
    }

    public NativeHostRuntimeServiceCommandPlan PlanAllSourcesReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandPlanResetAllExport(_handle),
            "Native host runtime service reset-all planning failed");
        return ReadPlan();
    }

    public NativeHostRuntimeServiceCommandPlan PlanSourceReset(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandPlanResetSourceExport(
                _handle,
                sourceKey,
                (int)sourceKind,
                processId.HasValue ? 1 : 0,
                processId.GetValueOrDefault()),
            "Native host runtime service reset-source planning failed");
        return ReadPlan();
    }

    public NativeHostRuntimeServiceCommandPlan PlanPerformanceMode(HostPerformanceMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandPlanSetPerformanceModeExport(_handle, (int)mode),
            "Native host runtime service performance-mode planning failed");
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
            NativeMethods.DlmHostRuntimeServiceCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResetSources(IReadOnlyList<string> sourceKeys)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandClearSourcesExport(_handle),
            "Native host runtime service planner clear failed");
        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostRuntimeServiceCommandAddSourceExport(_handle, sourceKey),
                "Native host runtime service planner add-source failed");
        }
    }

    private NativeHostRuntimeServiceCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandGetPlanExport(_handle, out var plan),
            "Native host runtime service planner plan read failed");
        if (plan.Status == StatusSourceNotFound)
        {
            return NativeHostRuntimeServiceCommandPlan.NotFound;
        }

        if (plan.Status != StatusSuccess)
        {
            throw new InvalidOperationException($"Native host runtime service planner returned unknown status {plan.Status}.");
        }

        return new NativeHostRuntimeServiceCommandPlan(
            Success: true,
            ShouldResetAll: plan.ShouldResetAll != 0,
            ShouldResetSource: plan.ShouldResetSource != 0,
            SourceKey: plan.SourceKeyUtf8ByteCount <= 0 ? null : CopySourceKey(plan.SourceKeyUtf8ByteCount),
            SourceKind: (SignalSourceKind)plan.SourceKind,
            ProcessId: plan.HasProcessId == 0 ? null : plan.ProcessId,
            HostMode: (HostPerformanceMode)plan.HostPerformanceMode,
            AnalysisMode: (PcmAnalysisPerformanceMode)plan.AnalysisPerformanceMode);
    }

    private string CopySourceKey(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeServiceCommandCopySourceKeyExport(
                _handle,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host runtime service source-key copy failed");
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
    private struct NativeRuntimeServiceCommandPlan
    {
        public int Status;
        public int ShouldResetAll;
        public int ShouldResetSource;
        public int SourceKeyUtf8ByteCount;
        public int SourceKind;
        public int HasProcessId;
        public int ProcessId;
        public int HostPerformanceMode;
        public int AnalysisPerformanceMode;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostRuntimeServiceCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandClearSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandClearSourcesExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandPlanResetAllExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandPlanResetAllExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandPlanResetSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandPlanResetSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int sourceKind,
            int hasProcessId,
            int processId);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandPlanSetPerformanceModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandPlanSetPerformanceModeExport(
            IntPtr handle,
            int hostPerformanceMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandGetPlanExport(
            IntPtr handle,
            out NativeRuntimeServiceCommandPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeServiceCommandCopySourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeServiceCommandCopySourceKeyExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostRuntimeServiceCommandPlan(
    bool Success,
    bool ShouldResetAll,
    bool ShouldResetSource,
    string? SourceKey,
    SignalSourceKind SourceKind,
    int? ProcessId,
    HostPerformanceMode HostMode,
    PcmAnalysisPerformanceMode AnalysisMode)
{
    public static NativeHostRuntimeServiceCommandPlan NotFound { get; } = new(
        Success: false,
        ShouldResetAll: false,
        ShouldResetSource: false,
        SourceKey: null,
        SourceKind: SignalSourceKind.Unknown,
        ProcessId: null,
        HostMode: HostPerformanceMode.Normal,
        AnalysisMode: PcmAnalysisPerformanceMode.Normal);
}
