using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Settings;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostDetailModuleProjection : IDisposable
{
    private const int StringFieldTitle = 0;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostDetailModuleProjection()
    {
        var result = NativeMethods.DlmHostDetailModuleProjectionCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host detail module projection could not be initialized (code {result}).");
        }
    }

    public static NativeHostDetailModuleProjectionResult Project(
        IReadOnlyList<NativeHostDetailModuleLayoutInput> layouts,
        NativeHostDetailModuleVisibility visibility)
    {
        using var projection = new NativeHostDetailModuleProjection();
        return projection.ProjectModules(layouts, visibility);
    }

    public NativeHostDetailModuleProjectionResult ProjectModules(
        IReadOnlyList<NativeHostDetailModuleLayoutInput> layouts,
        NativeHostDetailModuleVisibility visibility)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostDetailModuleProjectionClearExport(_handle), "Native detail module projection clear failed");

        foreach (var layout in layouts)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostDetailModuleProjectionAddLayoutExport(
                    _handle,
                    (int)layout.Kind,
                    layout.OrderIndex,
                    layout.Height is null ? 0 : 1,
                    layout.Height ?? 0),
                "Native detail module projection add-layout failed");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostDetailModuleProjectionProjectExport(
                _handle,
                visibility.Stereo is null ? 0 : 1,
                visibility.Stereo == true ? 1 : 0,
                visibility.Collision is null ? 0 : 1,
                visibility.Collision == true ? 1 : 0,
                visibility.LoudnessClock is null ? 0 : 1,
                visibility.LoudnessClock == true ? 1 : 0,
                visibility.Waveform is null ? 0 : 1,
                visibility.Waveform == true ? 1 : 0,
                visibility.Spectrum is null ? 0 : 1,
                visibility.Spectrum == true ? 1 : 0),
            "Native detail module projection project failed");

        ThrowIfFailed(
            NativeMethods.DlmHostDetailModuleProjectionGetCountExport(_handle, out var count),
            "Native detail module projection count failed");
        var rows = new NativeHostDetailModuleProjectionRow[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostDetailModuleProjectionGetAtExport(_handle, index, out var row),
                "Native detail module projection row failed");
            rows[index] = new NativeHostDetailModuleProjectionRow(
                ToDetailModuleKind(row.Kind),
                CopyString(index, StringFieldTitle, row.TitleUtf8ByteCount),
                row.OrderIndex,
                row.IsVisible != 0,
                row.Height);
        }

        return new NativeHostDetailModuleProjectionResult(rows);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostDetailModuleProjectionDestroyExport(_handle);
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
            NativeMethods.DlmHostDetailModuleProjectionCopyStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native detail module projection string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static DetailModuleKind ToDetailModuleKind(int value)
    {
        return Enum.IsDefined(typeof(DetailModuleKind), value)
            ? (DetailModuleKind)value
            : DetailModuleKind.Stereo;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeProjectionRow
    {
        public int Kind;
        public int OrderIndex;
        public int IsVisible;
        public double Height;
        public int TitleUtf8ByteCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostDetailModuleProjectionDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionAddLayoutExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionAddLayoutExport(
            IntPtr handle,
            int kind,
            int orderIndex,
            int hasHeight,
            double height);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionProjectExport(
            IntPtr handle,
            int hasStereoVisible,
            int stereoVisible,
            int hasCollisionVisible,
            int collisionVisible,
            int hasLoudnessClockVisible,
            int loudnessClockVisible,
            int hasWaveformVisible,
            int waveformVisible,
            int hasSpectrumVisible,
            int spectrumVisible);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionGetCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionGetCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionGetAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionGetAtExport(IntPtr handle, int index, out NativeProjectionRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostDetailModuleProjectionCopyStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostDetailModuleProjectionCopyStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostDetailModuleVisibility(
    bool? Stereo = null,
    bool? Collision = null,
    bool? LoudnessClock = null,
    bool? Waveform = null,
    bool? Spectrum = null)
{
    public static NativeHostDetailModuleVisibility Default { get; } = new();
}

internal sealed record NativeHostDetailModuleLayoutInput(
    DetailModuleKind Kind,
    int OrderIndex,
    double? Height);

internal sealed record NativeHostDetailModuleProjectionResult(
    IReadOnlyList<NativeHostDetailModuleProjectionRow> Rows);

internal sealed record NativeHostDetailModuleProjectionRow(
    DetailModuleKind Kind,
    string Title,
    int OrderIndex,
    bool IsVisible,
    double Height);
