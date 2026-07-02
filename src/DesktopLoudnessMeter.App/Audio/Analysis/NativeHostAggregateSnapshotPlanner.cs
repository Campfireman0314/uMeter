using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostAggregateSnapshotPlanner : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostAggregateSnapshotPlanner()
    {
        var result = NativeMethods.DlmHostAggregateSnapshotCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host aggregate snapshot planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostAggregateSnapshotPlan Project(IReadOnlyList<string> sourceKeys, string? selectedSourceKey)
    {
        using var planner = new NativeHostAggregateSnapshotPlanner();
        return planner.ProjectSnapshot(sourceKeys, selectedSourceKey);
    }

    public NativeHostAggregateSnapshotPlan ProjectSnapshot(IReadOnlyList<string> sourceKeys, string? selectedSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostAggregateSnapshotClearExport(_handle), "Native aggregate snapshot clear failed");

        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostAggregateSnapshotAddSourceExport(_handle, sourceKey),
                "Native aggregate snapshot add-source failed");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostAggregateSnapshotProjectExport(_handle, selectedSourceKey),
            "Native aggregate snapshot project failed");

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
            NativeMethods.DlmHostAggregateSnapshotDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private NativeHostAggregateSnapshotPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostAggregateSnapshotGetPlanExport(_handle, out var plan),
            "Native aggregate snapshot plan read failed");

        return new NativeHostAggregateSnapshotPlan(
            plan.HasSelectedSource != 0,
            plan.HasSelectedSource == 0 ? null : plan.SelectedSourceIndex,
            plan.IncludeSelectedDetail != 0,
            CopyStatusText(plan.StatusTextUtf8ByteCount));
    }

    private string CopyStatusText(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostAggregateSnapshotCopyStatusTextExport(_handle, buffer, buffer.Length, out var actualCount),
            "Native aggregate snapshot status text copy failed");
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
    private struct NativeAggregateSnapshotPlan
    {
        public int HasSelectedSource;
        public int SelectedSourceIndex;
        public int IncludeSelectedDetail;
        public int StatusTextUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostAggregateSnapshotDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotProjectExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotGetPlanExport(
            IntPtr handle,
            out NativeAggregateSnapshotPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostAggregateSnapshotCopyStatusTextExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostAggregateSnapshotCopyStatusTextExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostAggregateSnapshotPlan(
    bool HasSelectedSource,
    int? SelectedSourceIndex,
    bool IncludeSelectedDetail,
    string StatusText);
