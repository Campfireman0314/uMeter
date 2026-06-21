using System.Runtime.InteropServices;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeRfRapidTruePeakEstimator : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeRfRapidTruePeakEstimator(int channels, int oversampling, int? windowFrames = null)
    {
        if (NativeMethods.DlmRfRapidTruePeakCreate(channels, oversampling, windowFrames ?? 0, out _handle) != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("RfRapid true-peak estimator could not be initialized.");
        }

        Channels = channels;
        Oversampling = oversampling;
    }

    public int Channels { get; }

    public int Oversampling { get; }

    public double TruePeak { get; private set; }

    public int Process(
        double[] interleavedSamples,
        int frameCount,
        double[] oversampledMonoSamples,
        double[] oversampledInterleavedSamples,
        double[] framePeaks,
        int[] frameOutputCounts)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(interleavedSamples);
        ArgumentNullException.ThrowIfNull(oversampledMonoSamples);
        ArgumentNullException.ThrowIfNull(oversampledInterleavedSamples);
        ArgumentNullException.ThrowIfNull(framePeaks);
        ArgumentNullException.ThrowIfNull(frameOutputCounts);

        var outputCapacityFrames = Math.Min(
            oversampledMonoSamples.Length,
            oversampledInterleavedSamples.Length / Channels);
        var result = NativeMethods.DlmRfRapidTruePeakProcessF64(
            _handle,
            interleavedSamples,
            frameCount,
            oversampledMonoSamples,
            oversampledInterleavedSamples,
            framePeaks,
            frameOutputCounts,
            outputCapacityFrames,
            out var outputFrameCount,
            out var truePeak);
        if (result != 0)
        {
            throw new InvalidOperationException($"RfRapid true-peak processing failed with code {result}.");
        }

        TruePeak = truePeak;
        return outputFrameCount;
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (NativeMethods.DlmRfRapidTruePeakReset(_handle) != 0)
        {
            throw new InvalidOperationException("RfRapid true-peak reset failed.");
        }

        TruePeak = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.DlmRfRapidTruePeakDestroy(_handle);
        _handle = IntPtr.Zero;
        _disposed = true;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmRfRapidTruePeakCreate(
            int channels,
            int oversampling,
            int windowFrames,
            out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmRfRapidTruePeakDestroy(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmRfRapidTruePeakReset(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmRfRapidTruePeakProcessF64(
            IntPtr handle,
            [In] double[] interleavedSamples,
            int frameCount,
            [Out] double[] oversampledMonoSamples,
            [Out] double[] oversampledInterleavedSamples,
            [Out] double[] framePeaks,
            [Out] int[] frameOutputCounts,
            int outputCapacityFrames,
            out int outputFrameCount,
            out double truePeak);
    }
}
