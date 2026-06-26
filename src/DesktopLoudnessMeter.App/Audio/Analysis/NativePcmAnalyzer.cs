using System.Runtime.InteropServices;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativePcmAnalyzer : IDisposable
{
    private readonly int _channels;
    private IntPtr _handle;
    private bool _disposed;

    public NativePcmAnalyzer(PcmMetricAnalyzerOptions options)
    {
        _channels = options.Channels;
        var result = NativeMethods.DlmPcmAnalyzerCreateExport(
            options.SampleRate,
            options.Channels,
            options.TruePeakOversampling,
            out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native PCM analyzer could not be initialized (code {result}).");
        }
    }

    public NativePcmAnalyzerSnapshot Snapshot
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var result = NativeMethods.DlmPcmAnalyzerGetSnapshotExport(_handle, out var snapshot);
            if (result != 0)
            {
                throw new InvalidOperationException($"Native PCM snapshot failed with code {result}.");
            }

            return snapshot;
        }
    }

    public void Process(ReadOnlySpan<float> interleavedSamples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var samples = interleavedSamples.ToArray();
        var result = NativeMethods.DlmPcmAnalyzerProcessF32Export(_handle, samples, samples.Length);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM analyzer process failed with code {result}.");
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = NativeMethods.DlmPcmAnalyzerResetExport(_handle);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM analyzer reset failed with code {result}.");
        }
    }

    public void SetChannelWaveformEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = NativeMethods.DlmPcmAnalyzerSetChannelWaveformEnabledExport(_handle, enabled ? 1 : 0);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM channel waveform setting failed with code {result}.");
        }
    }

    public void SetChannelSpectrumEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = NativeMethods.DlmPcmAnalyzerSetChannelSpectrumEnabledExport(_handle, enabled ? 1 : 0);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM channel spectrum setting failed with code {result}.");
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = NativeMethods.DlmPcmAnalyzerSetPerformanceModeExport(_handle, mode == PcmAnalysisPerformanceMode.Eco ? 1 : 0);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM performance mode update failed with code {result}.");
        }
    }

    public IReadOnlyList<double>? CopyWaveform(int count)
    {
        return CopyVector(count, NativeMethods.DlmPcmAnalyzerCopyWaveformExport);
    }

    public IReadOnlyList<IReadOnlyList<double>>? CopyChannelWaveform(int count)
    {
        return CopyChannelVectors(count, NativeMethods.DlmPcmAnalyzerCopyChannelWaveformExport);
    }

    public IReadOnlyList<double>? CopySpectrum(int count)
    {
        return CopyVector(count, NativeMethods.DlmPcmAnalyzerCopySpectrumExport);
    }

    public IReadOnlyList<IReadOnlyList<double>>? CopyChannelSpectrum(int count)
    {
        return CopyChannelVectors(count, NativeMethods.DlmPcmAnalyzerCopyChannelSpectrumExport);
    }

    public IReadOnlyList<StereoImagePoint>? CopyStereoImagePoints(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count <= 0)
        {
            return null;
        }

        var angles = new double[count];
        var radii = new double[count];
        var result = NativeMethods.DlmPcmAnalyzerCopyStereoImagePointsExport(_handle, angles, radii, count, out var actualCount);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM stereo image point copy failed with code {result}.");
        }

        var points = new StereoImagePoint[actualCount];
        for (var index = 0; index < actualCount; index++)
        {
            points[index] = new StereoImagePoint(angles[index], radii[index]);
        }

        return points;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.DlmPcmAnalyzerDestroyExport(_handle);
        _handle = IntPtr.Zero;
        _disposed = true;
    }

    private IReadOnlyList<double>? CopyVector(
        int count,
        NativeVectorCopy copy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count <= 0)
        {
            return null;
        }

        var buffer = new double[count];
        var result = copy(_handle, buffer, buffer.Length, out var actualCount);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native PCM vector copy failed with code {result}.");
        }

        return actualCount == buffer.Length ? buffer : buffer[..actualCount];
    }

    private IReadOnlyList<IReadOnlyList<double>>? CopyChannelVectors(
        int count,
        NativeChannelVectorCopy copy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count <= 0)
        {
            return null;
        }

        var channels = new IReadOnlyList<double>[_channels];
        for (var channel = 0; channel < _channels; channel++)
        {
            var buffer = new double[count];
            var result = copy(_handle, channel, buffer, buffer.Length, out var actualCount);
            if (result != 0)
            {
                throw new InvalidOperationException($"Native PCM channel vector copy failed with code {result}.");
            }

            channels[channel] = actualCount == buffer.Length ? buffer : buffer[..actualCount];
        }

        return channels;
    }

    private delegate int NativeVectorCopy(IntPtr handle, double[] buffer, int capacity, out int actualCount);

    private delegate int NativeChannelVectorCopy(IntPtr handle, int channel, double[] buffer, int capacity, out int actualCount);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePcmAnalyzerSnapshot
    {
        public int HasMomentaryLufs;
        public double MomentaryLufs;
        public int HasShortTermLufs;
        public double ShortTermLufs;
        public int HasIntegratedLufs;
        public double IntegratedLufs;
        public int HasTruePeakDb;
        public double TruePeakDb;
        public int HasRms100MsDb;
        public double Rms100MsDb;
        public int HasStereoImageX;
        public double StereoImageX;
        public int HasStereoImageY;
        public double StereoImageY;
        public int HasCorrelation;
        public double Correlation;
        public int WaveformCount;
        public int ChannelWaveformCount;
        public int SpectrumCount;
        public int ChannelSpectrumCount;
        public int StereoImagePointCount;
        public long PlotFrameVersion;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCreateExport(int sampleRate, int channels, int oversampling, out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmPcmAnalyzerDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerResetExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerResetExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerProcessF32Export", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerProcessF32Export(IntPtr handle, [In] float[] samples, int sampleCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerGetSnapshotExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerGetSnapshotExport(IntPtr handle, out NativePcmAnalyzerSnapshot snapshot);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetChannelWaveformEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerSetChannelWaveformEnabledExport(IntPtr handle, int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetChannelSpectrumEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerSetChannelSpectrumEnabledExport(IntPtr handle, int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetPerformanceModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerSetPerformanceModeExport(IntPtr handle, int mode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyWaveformExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCopyWaveformExport(IntPtr handle, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyChannelWaveformExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCopyChannelWaveformExport(IntPtr handle, int channel, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopySpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCopySpectrumExport(IntPtr handle, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyChannelSpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCopyChannelSpectrumExport(IntPtr handle, int channel, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyStereoImagePointsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmPcmAnalyzerCopyStereoImagePointsExport(
            IntPtr handle,
            [Out] double[] angleBuffer,
            [Out] double[] radiusBuffer,
            int capacity,
            out int actualCount);
    }
}
