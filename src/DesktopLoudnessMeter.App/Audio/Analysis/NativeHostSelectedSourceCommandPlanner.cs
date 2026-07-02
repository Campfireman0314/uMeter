using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSelectedSourceCommandPlanner : IDisposable
{
    private const int StatusSuccess = 0;
    private const int StatusSourceNotFound = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSelectedSourceCommandPlanner()
    {
        var result = NativeMethods.DlmHostSelectedSourceCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host selected source planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSelectedSourceCommandPlan PlanSelect(
        IReadOnlyList<string> sourceKeys,
        string sourceKey)
    {
        using var planner = new NativeHostSelectedSourceCommandPlanner();
        return planner.Select(sourceKeys, sourceKey);
    }

    public static NativeHostSelectedSourceCommandPlan PlanBackToGlobal()
    {
        using var planner = new NativeHostSelectedSourceCommandPlanner();
        return planner.BackToGlobal();
    }

    public static NativeHostSelectedSourceCommandPlan PlanReconcile(
        IReadOnlyList<string> sourceKeys,
        string? selectedSourceKey)
    {
        using var planner = new NativeHostSelectedSourceCommandPlanner();
        return planner.Reconcile(sourceKeys, selectedSourceKey);
    }

    public static NativeHostSelectedSourceCommandPlan PlanHiddenSource(
        string sourceKey,
        bool isHidden,
        bool showHiddenSources,
        string? selectedSourceKey)
    {
        using var planner = new NativeHostSelectedSourceCommandPlanner();
        return planner.HiddenSource(sourceKey, isHidden, showHiddenSources, selectedSourceKey);
    }

    public NativeHostSelectedSourceCommandPlan Select(
        IReadOnlyList<string> sourceKeys,
        string sourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandPlanSelectExport(_handle, sourceKey),
            "Native host selected source select planning failed");
        return ReadPlan();
    }

    public NativeHostSelectedSourceCommandPlan BackToGlobal()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandPlanBackToGlobalExport(_handle),
            "Native host selected source back-to-global planning failed");
        return ReadPlan();
    }

    public NativeHostSelectedSourceCommandPlan Reconcile(
        IReadOnlyList<string> sourceKeys,
        string? selectedSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandPlanReconcileExport(_handle, selectedSourceKey),
            "Native host selected source reconcile planning failed");
        return ReadPlan();
    }

    public NativeHostSelectedSourceCommandPlan HiddenSource(
        string sourceKey,
        bool isHidden,
        bool showHiddenSources,
        string? selectedSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandPlanHiddenSourceExport(
                _handle,
                sourceKey,
                isHidden ? 1 : 0,
                showHiddenSources ? 1 : 0,
                selectedSourceKey),
            "Native host selected source hidden-source planning failed");
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
            NativeMethods.DlmHostSelectedSourceCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResetSources(IReadOnlyList<string> sourceKeys)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandClearSourcesExport(_handle),
            "Native host selected source planner clear failed");
        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedSourceCommandAddSourceExport(_handle, sourceKey),
                "Native host selected source planner add-source failed");
        }
    }

    private NativeHostSelectedSourceCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandGetPlanExport(_handle, out var plan),
            "Native host selected source planner plan read failed");
        if (plan.Status == StatusSourceNotFound)
        {
            return NativeHostSelectedSourceCommandPlan.NotFound;
        }

        if (plan.Status != StatusSuccess)
        {
            throw new InvalidOperationException($"Native host selected source planner returned unknown status {plan.Status}.");
        }

        return new NativeHostSelectedSourceCommandPlan(
            Success: true,
            ShouldClearSelected: plan.ShouldClearSelected != 0,
            ShouldSetSelected: plan.ShouldSetSelected != 0,
            SelectedSourceKey: plan.SelectedSourceKeyUtf8ByteCount <= 0
                ? null
                : CopySelectedSourceKey(plan.SelectedSourceKeyUtf8ByteCount));
    }

    private string CopySelectedSourceKey(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedSourceCommandCopySelectedSourceKeyExport(
                _handle,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host selected source key copy failed");
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
    private struct NativeSelectedSourceCommandPlan
    {
        public int Status;
        public int ShouldClearSelected;
        public int ShouldSetSelected;
        public int SelectedSourceKeyUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSelectedSourceCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandClearSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandClearSourcesExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandPlanSelectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandPlanSelectExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandPlanBackToGlobalExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandPlanBackToGlobalExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandPlanReconcileExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandPlanReconcileExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandPlanHiddenSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandPlanHiddenSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int isHidden,
            int showHiddenSources,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandGetPlanExport(
            IntPtr handle,
            out NativeSelectedSourceCommandPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedSourceCommandCopySelectedSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedSourceCommandCopySelectedSourceKeyExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSelectedSourceCommandPlan(
    bool Success,
    bool ShouldClearSelected,
    bool ShouldSetSelected,
    string? SelectedSourceKey)
{
    public static NativeHostSelectedSourceCommandPlan NotFound { get; } = new(
        Success: false,
        ShouldClearSelected: false,
        ShouldSetSelected: false,
        SelectedSourceKey: null);
}
