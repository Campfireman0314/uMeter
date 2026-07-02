using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSourceTargetCommandPlanner : IDisposable
{
    private const int StatusSuccess = 0;
    private const int StatusSourceNotFound = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSourceTargetCommandPlanner()
    {
        var result = NativeMethods.DlmHostSourceTargetCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host source target planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSourceTargetCommandPlan Plan(
        IReadOnlyList<string> sourceKeys,
        string sourceKey)
    {
        using var planner = new NativeHostSourceTargetCommandPlanner();
        return planner.Target(sourceKeys, sourceKey);
    }

    public NativeHostSourceTargetCommandPlan Target(
        IReadOnlyList<string> sourceKeys,
        string sourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSourceTargetCommandPlanTargetExport(_handle, sourceKey),
            "Native host source target planning failed");
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
            NativeMethods.DlmHostSourceTargetCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResetSources(IReadOnlyList<string> sourceKeys)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourceTargetCommandClearSourcesExport(_handle),
            "Native host source target planner clear failed");
        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSourceTargetCommandAddSourceExport(_handle, sourceKey),
                "Native host source target planner add-source failed");
        }
    }

    private NativeHostSourceTargetCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourceTargetCommandGetPlanExport(_handle, out var plan),
            "Native host source target planner plan read failed");
        if (plan.Status == StatusSourceNotFound)
        {
            return NativeHostSourceTargetCommandPlan.NotFound;
        }

        if (plan.Status != StatusSuccess)
        {
            throw new InvalidOperationException($"Native host source target planner returned unknown status {plan.Status}.");
        }

        return new NativeHostSourceTargetCommandPlan(
            Success: true,
            SourceKey: plan.SourceKeyUtf8ByteCount <= 0
                ? null
                : CopySourceKey(plan.SourceKeyUtf8ByteCount));
    }

    private string CopySourceKey(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSourceTargetCommandCopySourceKeyExport(
                _handle,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host source target key copy failed");
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
    private struct NativeSourceTargetCommandPlan
    {
        public int Status;
        public int SourceKeyUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSourceTargetCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandClearSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandClearSourcesExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandPlanTargetExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandPlanTargetExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandGetPlanExport(
            IntPtr handle,
            out NativeSourceTargetCommandPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceTargetCommandCopySourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceTargetCommandCopySourceKeyExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSourceTargetCommandPlan(bool Success, string? SourceKey)
{
    public static NativeHostSourceTargetCommandPlan NotFound { get; } = new(
        Success: false,
        SourceKey: null);
}
