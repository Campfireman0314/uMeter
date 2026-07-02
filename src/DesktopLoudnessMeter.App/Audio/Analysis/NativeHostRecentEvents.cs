using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostRecentEvents : IDisposable
{
    private const int StringFieldTimeText = 0;
    private const int StringFieldTitle = 1;
    private const int StringFieldDetail = 2;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostRecentEvents()
    {
        var result = NativeMethods.DlmHostRecentEventsCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host recent events could not be initialized (code {result}).");
        }
    }

    ~NativeHostRecentEvents()
    {
        Dispose();
    }

    public void ClearCurrentSources()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostRecentEventsClearCurrentSourcesExport(_handle), "Native host recent events clear failed");
    }

    public void AddLiveSource(string sourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRecentEventsAddLiveSourceExport(_handle, sourceKey),
            "Native host recent events add-live failed");
    }

    public void AddVisibleSource(string sourceKey, string displayTitle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRecentEventsAddVisibleSourceExport(_handle, sourceKey, displayTitle),
            "Native host recent events add-visible failed");
    }

    public void Reconcile(string timeText, int maxRecentEventEntries)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRecentEventsReconcileExport(_handle, timeText, maxRecentEventEntries),
            "Native host recent events reconcile failed");
    }

    public IReadOnlyList<HostRecentEventSnapshot> GetEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostRecentEventsGetCountExport(_handle, out var count),
            "Native host recent events count failed");
        var recent = new HostRecentEventSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostRecentEventsGetAtExport(_handle, index, out var row),
                "Native host recent events row failed");
            recent[index] = new HostRecentEventSnapshot(
                CopyString(index, StringFieldTimeText, row.TimeTextUtf8ByteCount),
                ToRecentEventKind(row.Kind),
                CopyString(index, StringFieldTitle, row.TitleUtf8ByteCount),
                CopyString(index, StringFieldDetail, row.DetailUtf8ByteCount),
                ToReadoutSeverity(row.Severity));
        }

        return recent;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostRecentEventsDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private string CopyString(int index, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostRecentEventsCopyStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native host recent events string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static RecentEventKind ToRecentEventKind(int value)
    {
        return Enum.IsDefined(typeof(RecentEventKind), value)
            ? (RecentEventKind)value
            : RecentEventKind.Source;
    }

    private static ReadoutSeverity ToReadoutSeverity(int value)
    {
        return Enum.IsDefined(typeof(ReadoutSeverity), value)
            ? (ReadoutSeverity)value
            : ReadoutSeverity.Normal;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRecentEventRow
    {
        public int Kind;
        public int Severity;
        public int TimeTextUtf8ByteCount;
        public int TitleUtf8ByteCount;
        public int DetailUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostRecentEventsDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsClearCurrentSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsClearCurrentSourcesExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsAddLiveSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsAddLiveSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsAddVisibleSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsAddVisibleSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayTitle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsReconcileExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsReconcileExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string timeText,
            int maxRecentEventEntries);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsGetCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsGetCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsGetAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsGetAtExport(IntPtr handle, int index, out NativeRecentEventRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostRecentEventsCopyStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostRecentEventsCopyStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}
