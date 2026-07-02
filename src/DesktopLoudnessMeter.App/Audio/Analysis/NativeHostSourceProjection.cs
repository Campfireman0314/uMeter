using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSourceProjection : IDisposable
{
    private const int StringFieldSourceKey = 0;
    private const int StringFieldSourceKindLabel = 1;
    private const int StringFieldDisplayName = 2;
    private const int StringFieldAlias = 3;
    private const int StringFieldEffectiveDisplayName = 4;
    private const int StringFieldDisplayTitle = 5;
    private const int StringFieldSourceIdentityText = 6;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSourceProjection()
    {
        var result = NativeMethods.DlmHostSourceProjectionCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host source projection could not be initialized (code {result}).");
        }
    }

    public static NativeHostSourceProjectionResult Project(
        IReadOnlyList<SignalSourceSnapshot> sources,
        Func<string, HostSourceWorkspaceState?> workspaceStateForSource,
        bool showHiddenSources,
        string? selectedSourceKey)
    {
        using var projection = new NativeHostSourceProjection();
        return projection.ProjectSources(sources, workspaceStateForSource, showHiddenSources, selectedSourceKey);
    }

    public NativeHostSourceProjectionResult ProjectSources(
        IReadOnlyList<SignalSourceSnapshot> sources,
        Func<string, HostSourceWorkspaceState?> workspaceStateForSource,
        bool showHiddenSources,
        string? selectedSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostSourceProjectionClearExport(_handle), "Native host source projection clear failed");
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            var workspaceState = workspaceStateForSource(source.SourceKey) ?? new HostSourceWorkspaceState();
            var result = NativeMethods.DlmHostSourceProjectionAddSourceExport(
                _handle,
                source.SourceKey,
                (int)source.SourceKind,
                source.ProcessId is null ? 0 : 1,
                source.ProcessId ?? 0,
                source.DisplayName,
                workspaceState.Alias,
                workspaceState.IsHidden ? 1 : 0,
                workspaceState.OrderIndex is null ? 0 : 1,
                workspaceState.OrderIndex ?? 0,
                (int)(workspaceState.TruePeakOversampling ?? TruePeakOversampling.FourX),
                (int)workspaceState.WaveformDisplayMode,
                (int)workspaceState.SpectrumDisplayMode);
            ThrowIfFailed(result, "Native host source projection add-source failed");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostSourceProjectionProjectExport(
                _handle,
                showHiddenSources ? 1 : 0,
                selectedSourceKey),
            "Native host source projection project failed");
        ThrowIfFailed(
            NativeMethods.DlmHostSourceProjectionGetProjectedCountExport(_handle, out var count),
            "Native host source projection count failed");
        ThrowIfFailed(
            NativeMethods.DlmHostSourceProjectionIsSelectedSourceVisibleExport(_handle, out var selectedVisible),
            "Native host source projection selected-state failed");

        var rows = new NativeHostSourceProjectionRow[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSourceProjectionGetProjectedAtExport(_handle, index, out var row),
                "Native host source projection row failed");
            rows[index] = new NativeHostSourceProjectionRow(
                row.OriginalSourceIndex,
                CopyString(index, StringFieldSourceKey, row.SourceKeyUtf8ByteCount),
                ToSourceKind(row.SourceKind),
                CopyString(index, StringFieldSourceKindLabel, row.SourceKindLabelUtf8ByteCount),
                row.HasProcessId == 0 ? null : row.ProcessId,
                CopyString(index, StringFieldDisplayName, row.DisplayNameUtf8ByteCount),
                ToNullableString(CopyString(index, StringFieldAlias, row.AliasUtf8ByteCount)),
                CopyString(index, StringFieldEffectiveDisplayName, row.EffectiveDisplayNameUtf8ByteCount),
                CopyString(index, StringFieldDisplayTitle, row.DisplayTitleUtf8ByteCount),
                CopyString(index, StringFieldSourceIdentityText, row.SourceIdentityTextUtf8ByteCount),
                row.IsHidden != 0,
                row.HasOrderIndex == 0 ? null : row.OrderIndex,
                (TruePeakOversampling)row.TruePeakOversampling,
                (WaveformDisplayMode)row.WaveformDisplayMode,
                (SpectrumDisplayMode)row.SpectrumDisplayMode);
        }

        return new NativeHostSourceProjectionResult(rows, selectedVisible != 0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.DlmHostSourceProjectionDestroyExport(_handle);
        _handle = IntPtr.Zero;
        _disposed = true;
    }

    private string CopyString(int index, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSourceProjectionCopyProjectedStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native host source projection string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static string? ToNullableString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static SignalSourceKind ToSourceKind(int value)
    {
        return Enum.IsDefined(typeof(SignalSourceKind), value)
            ? (SignalSourceKind)value
            : SignalSourceKind.Unknown;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeProjectionRow
    {
        public int OriginalSourceIndex;
        public int SourceKind;
        public int ProcessId;
        public int HasProcessId;
        public int IsHidden;
        public int OrderIndex;
        public int HasOrderIndex;
        public int TruePeakOversampling;
        public int WaveformDisplayMode;
        public int SpectrumDisplayMode;
        public int SourceKeyUtf8ByteCount;
        public int SourceKindLabelUtf8ByteCount;
        public int DisplayNameUtf8ByteCount;
        public int AliasUtf8ByteCount;
        public int EffectiveDisplayNameUtf8ByteCount;
        public int DisplayTitleUtf8ByteCount;
        public int SourceIdentityTextUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSourceProjectionDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int sourceKind,
            int hasProcessId,
            int processId,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayName,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? alias,
            int isHidden,
            int hasOrderIndex,
            int orderIndex,
            int truePeakOversampling,
            int waveformDisplayMode,
            int spectrumDisplayMode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionProjectExport(
            IntPtr handle,
            int showHiddenSources,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionGetProjectedCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionGetProjectedCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionIsSelectedSourceVisibleExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionIsSelectedSourceVisibleExport(IntPtr handle, out int isVisible);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionGetProjectedAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionGetProjectedAtExport(IntPtr handle, int index, out NativeProjectionRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourceProjectionCopyProjectedStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourceProjectionCopyProjectedStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSourceProjectionResult(
    IReadOnlyList<NativeHostSourceProjectionRow> Rows,
    bool IsSelectedSourceVisible);

internal sealed record NativeHostSourceProjectionRow(
    int OriginalSourceIndex,
    string SourceKey,
    SignalSourceKind SourceKind,
    string SourceKindLabel,
    int? ProcessId,
    string DisplayName,
    string? Alias,
    string EffectiveDisplayName,
    string DisplayTitle,
    string SourceIdentityText,
    bool IsHidden,
    int? OrderIndex,
    TruePeakOversampling TruePeakOversampling,
    WaveformDisplayMode WaveformDisplayMode,
    SpectrumDisplayMode SpectrumDisplayMode);
