using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostRuntimeState : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostRuntimeState()
    {
        var result = NativeMethods.DlmHostRuntimeStateCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host runtime state could not be initialized (code {result}).");
        }
    }

    public NativeHostRuntimeStateSnapshot Snapshot => ReadSnapshot();

    public string? SelectedSourceKey => Snapshot.SelectedSourceKey;

    public bool ShowHiddenSources
    {
        get => Snapshot.ShowHiddenSources;
        set => SetShowHiddenSources(value);
    }

    public HostPerformanceMode PerformanceMode
    {
        get => Snapshot.PerformanceMode;
        set => SetPerformanceMode(value);
    }

    public void SetSelectedSourceKey(string? sourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateSetSelectedSourceKeyExport(_handle, sourceKey),
            "Native host runtime state selected-source set failed");
    }

    public void ClearSelectedSourceKey()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateClearSelectedSourceKeyExport(_handle),
            "Native host runtime state selected-source clear failed");
    }

    public void ApplySelectedSourcePlan(NativeHostSelectedSourceCommandPlan plan)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!plan.Success)
        {
            return;
        }

        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateApplySelectedSourcePlanExport(
                _handle,
                plan.ShouldClearSelected ? 1 : 0,
                plan.ShouldSetSelected ? 1 : 0,
                plan.SelectedSourceKey),
            "Native host runtime state selected-source plan application failed");
    }

    public void ApplyShowHiddenPlan(NativeHostGlobalSettingsCommandPlan plan)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateApplyShowHiddenPlanExport(
                _handle,
                plan.ShowHiddenSources ? 1 : 0),
            "Native host runtime state show-hidden plan application failed");
    }

    public void ApplyPerformanceModePlan(NativeHostRuntimeServiceCommandPlan plan)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!plan.Success)
        {
            return;
        }

        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateApplyPerformanceModePlanExport(_handle, (int)plan.HostMode),
            "Native host runtime state performance-mode plan application failed");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostRuntimeStateDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void SetShowHiddenSources(bool showHiddenSources)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateSetShowHiddenSourcesExport(_handle, showHiddenSources ? 1 : 0),
            "Native host runtime state show-hidden set failed");
    }

    private void SetPerformanceMode(HostPerformanceMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateSetPerformanceModeExport(_handle, (int)mode),
            "Native host runtime state performance-mode set failed");
    }

    private NativeHostRuntimeStateSnapshot ReadSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateGetSnapshotExport(_handle, out var snapshot),
            "Native host runtime state snapshot read failed");

        return new NativeHostRuntimeStateSnapshot(
            SelectedSourceKey: snapshot.HasSelectedSourceKey == 0
                ? null
                : CopySelectedSourceKey(snapshot.SelectedSourceKeyUtf8ByteCount),
            ShowHiddenSources: snapshot.ShowHiddenSources != 0,
            PerformanceMode: (HostPerformanceMode)snapshot.HostPerformanceMode);
    }

    private string CopySelectedSourceKey(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostRuntimeStateCopySelectedSourceKeyExport(
                _handle,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host runtime state selected-source copy failed");
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
    private struct NativeRuntimeStateSnapshot
    {
        public int HasSelectedSourceKey;
        public int SelectedSourceKeyUtf8ByteCount;
        public int ShowHiddenSources;
        public int HostPerformanceMode;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostRuntimeStateDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateSetSelectedSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateSetSelectedSourceKeyExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateClearSelectedSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateClearSelectedSourceKeyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateSetShowHiddenSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateSetShowHiddenSourcesExport(IntPtr handle, int showHiddenSources);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateSetPerformanceModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateSetPerformanceModeExport(IntPtr handle, int hostPerformanceMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateApplySelectedSourcePlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateApplySelectedSourcePlanExport(
            IntPtr handle,
            int shouldClearSelected,
            int shouldSetSelected,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateApplyShowHiddenPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateApplyShowHiddenPlanExport(IntPtr handle, int showHiddenSources);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateApplyPerformanceModePlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateApplyPerformanceModePlanExport(IntPtr handle, int hostPerformanceMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateGetSnapshotExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateGetSnapshotExport(
            IntPtr handle,
            out NativeRuntimeStateSnapshot snapshot);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRuntimeStateCopySelectedSourceKeyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRuntimeStateCopySelectedSourceKeyExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostRuntimeStateSnapshot(
    string? SelectedSourceKey,
    bool ShowHiddenSources,
    HostPerformanceMode PerformanceMode);
