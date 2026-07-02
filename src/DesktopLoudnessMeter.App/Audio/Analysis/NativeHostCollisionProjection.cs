using System.Runtime.InteropServices;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostCollisionProjection : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostCollisionProjection()
    {
        var result = NativeMethods.DlmHostCollisionProjectionCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host collision projection could not be initialized (code {result}).");
        }
    }

    public static IReadOnlyList<FrequencyCollisionBand> Project(
        IReadOnlyList<double>? focus,
        IEnumerable<IReadOnlyList<double>?> comparisons,
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold,
        CollisionFrequencyScale frequencyScale)
    {
        using var projection = new NativeHostCollisionProjection();
        return projection.ProjectBands(focus, comparisons, levelFloorDb, prominenceThresholdDb, relativeThreshold, frequencyScale);
    }

    public IReadOnlyList<FrequencyCollisionBand> ProjectBands(
        IReadOnlyList<double>? focus,
        IEnumerable<IReadOnlyList<double>?> comparisons,
        double levelFloorDb,
        double prominenceThresholdDb,
        double relativeThreshold,
        CollisionFrequencyScale frequencyScale)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostCollisionProjectionClearExport(_handle), "Native collision projection clear failed");
        SetFocusSpectrum(focus);

        foreach (var comparison in comparisons)
        {
            AddComparisonSpectrum(comparison);
        }

        ThrowIfFailed(
            NativeMethods.DlmHostCollisionProjectionProjectExport(
                _handle,
                levelFloorDb,
                prominenceThresholdDb,
                relativeThreshold,
                (int)frequencyScale),
            "Native collision projection failed");
        ThrowIfFailed(
            NativeMethods.DlmHostCollisionProjectionGetBandCountExport(_handle, out var count),
            "Native collision band count read failed");

        var bands = new FrequencyCollisionBand[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostCollisionProjectionGetBandAtExport(_handle, index, out var band),
                "Native collision band read failed");
            bands[index] = new FrequencyCollisionBand(band.StartRatio, band.EndRatio, band.Intensity);
        }

        return bands;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostCollisionProjectionDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void SetFocusSpectrum(IReadOnlyList<double>? spectrum)
    {
        var values = ToArray(spectrum);
        ThrowIfFailed(
            NativeMethods.DlmHostCollisionProjectionSetFocusSpectrumExport(_handle, values, values.Length),
            "Native collision focus spectrum set failed");
    }

    private void AddComparisonSpectrum(IReadOnlyList<double>? spectrum)
    {
        var values = ToArray(spectrum);
        ThrowIfFailed(
            NativeMethods.DlmHostCollisionProjectionAddComparisonSpectrumExport(_handle, values, values.Length),
            "Native collision comparison spectrum add failed");
    }

    private static double[] ToArray(IReadOnlyList<double>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        if (values is double[] array)
        {
            return array;
        }

        var copy = new double[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return copy;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBand
    {
        public double StartRatio;
        public double EndRatio;
        public double Intensity;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostCollisionProjectionDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionSetFocusSpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionSetFocusSpectrumExport(
            IntPtr handle,
            [In] double[] values,
            int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionAddComparisonSpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionAddComparisonSpectrumExport(
            IntPtr handle,
            [In] double[] values,
            int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionProjectExport(
            IntPtr handle,
            double levelFloorDb,
            double prominenceThresholdDb,
            double relativeThreshold,
            int frequencyScale);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionGetBandCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionGetBandCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostCollisionProjectionGetBandAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostCollisionProjectionGetBandAtExport(IntPtr handle, int index, out NativeBand band);
    }
}
