using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSourceOrderCommandPlanner : IDisposable
{
    private const int StatusSuccess = 0;
    private const int StatusSourceNotFound = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSourceOrderCommandPlanner()
    {
        var result = NativeMethods.DlmHostSourceOrderCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host source order planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSourceOrderCommandPlan PlanMove(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        HostSourceMoveDirection direction)
    {
        using var planner = new NativeHostSourceOrderCommandPlanner();
        return planner.PlanMoveSource(sourceKeys, sourceKey, direction);
    }

    public static NativeHostSourceOrderCommandPlan PlanMoveToIndex(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        int targetIndex)
    {
        using var planner = new NativeHostSourceOrderCommandPlanner();
        return planner.PlanMoveSourceToIndex(sourceKeys, sourceKey, targetIndex);
    }

    public NativeHostSourceOrderCommandPlan PlanMoveSource(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        HostSourceMoveDirection direction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        var nativeDirection = direction == HostSourceMoveDirection.Up ? -1 : 1;
        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandPlanMoveExport(_handle, sourceKey, nativeDirection),
            "Native host source order move planning failed");
        return ReadPlan();
    }

    public NativeHostSourceOrderCommandPlan PlanMoveSourceToIndex(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        int targetIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandPlanMoveToIndexExport(_handle, sourceKey, targetIndex),
            "Native host source order move-to-index planning failed");
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
            NativeMethods.DlmHostSourceOrderCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResetSources(IReadOnlyList<string> sourceKeys)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandClearExport(_handle),
            "Native host source order planner clear failed");
        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSourceOrderCommandAddSourceExport(_handle, sourceKey),
                "Native host source order planner add-source failed");
        }
    }

    private NativeHostSourceOrderCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandGetStatusExport(_handle, out var status),
            "Native host source order planner status read failed");
        if (status == StatusSourceNotFound)
        {
            return new NativeHostSourceOrderCommandPlan(false, []);
        }

        if (status != StatusSuccess)
        {
            throw new InvalidOperationException($"Native host source order planner returned unknown status {status}.");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandGetRowCountExport(_handle, out var count),
            "Native host source order planner count read failed");
        var rows = new NativeHostSourceOrderCommandRow[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSourceOrderCommandGetRowExport(_handle, index, out var row),
                "Native host source order planner row read failed");
            rows[index] = new NativeHostSourceOrderCommandRow(
                CopySourceKey(index, row.SourceKeyUtf8ByteCount),
                row.OrderIndex);
        }

        return new NativeHostSourceOrderCommandPlan(true, rows);
    }

    private string CopySourceKey(int index, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSourceOrderCommandCopyRowSourceKeyExport(
                _handle,
                index,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host source order planner source-key copy failed");
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
    private struct NativeOrderRow
    {
        public int SourceKeyUtf8ByteCount;
        public int OrderIndex;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSourceOrderCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandPlanMoveExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandPlanMoveExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int direction);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandPlanMoveToIndexExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandPlanMoveToIndexExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int targetIndex);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandGetStatusExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandGetStatusExport(IntPtr handle, out int status);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandGetRowCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandGetRowCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandGetRowExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandGetRowExport(IntPtr handle, int index, out NativeOrderRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceOrderCommandCopyRowSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceOrderCommandCopyRowSourceKeyExport(
            IntPtr handle,
            int index,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSourceOrderCommandPlan(
    bool Success,
    IReadOnlyList<NativeHostSourceOrderCommandRow> Rows);

internal sealed record NativeHostSourceOrderCommandRow(
    string SourceKey,
    int OrderIndex);
