using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeSourceIngestCore : IDisposable
{
    private const int SourceStringFieldHandle = 0;
    private const int SourceStringFieldDisplayName = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeSourceIngestCore()
    {
        var result = NativeMethods.DlmSourceIngestCoreCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native source ingest core could not be initialized (code {result}).");
        }
    }

    public void Register(
        string handle,
        string displayName,
        int sourceKind,
        int sampleRate,
        int channels,
        long timestampMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = NativeMethods.DlmSourceIngestCoreRegisterExport(
            _handle,
            handle,
            displayName,
            sourceKind,
            sampleRate,
            channels,
            timestampMilliseconds);
        ThrowIfFailed(result, "Native source register failed");
    }

    public void Heartbeat(string handle, long timestampMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreHeartbeatExport(_handle, handle, timestampMilliseconds),
            "Native source heartbeat failed");
    }

    public void Rename(string handle, string displayName, long timestampMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreRenameExport(_handle, handle, displayName, timestampMilliseconds),
            "Native source rename failed");
    }

    public void Process(string handle, IReadOnlyList<float> samples, long timestampMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var sampleArray = samples as float[] ?? samples.ToArray();
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreProcessF32Export(_handle, handle, sampleArray, sampleArray.Length, timestampMilliseconds),
            "Native source audio process failed");
    }

    public void Close(string handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreCloseExport(_handle, handle),
            "Native source close failed");
    }

    public void RemoveExpired(long nowMilliseconds, long timeoutMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreRemoveExpiredExport(_handle, nowMilliseconds, timeoutMilliseconds),
            "Native source timeout cleanup failed");
    }

    public void ResetSource(string handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreResetSourceExport(_handle, handle),
            "Native source reset failed");
    }

    public void ResetAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreResetAllExport(_handle),
            "Native source reset-all failed");
    }

    public void SetTruePeakOversampling(string handle, int oversampling)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreSetTruePeakOversamplingExport(_handle, handle, oversampling),
            "Native source true-peak oversampling update failed");
    }

    public void SetChannelWaveformEnabled(string handle, bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreSetChannelWaveformEnabledExport(_handle, handle, enabled ? 1 : 0),
            "Native source channel waveform update failed");
    }

    public void SetChannelSpectrumEnabled(string handle, bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreSetChannelSpectrumEnabledExport(_handle, handle, enabled ? 1 : 0),
            "Native source channel spectrum update failed");
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreSetPerformanceModeExport(_handle, mode == PcmAnalysisPerformanceMode.Eco ? 1 : 0),
            "Native source performance mode update failed");
    }

    public IReadOnlyList<NativeSourceSnapshot> GetSnapshots()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreGetSourceCountExport(_handle, out var count),
            "Native source count failed");

        var snapshots = new NativeSourceSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmSourceIngestCoreGetSourceSnapshotAtExport(_handle, index, out var snapshot),
                "Native source snapshot failed");
            var handle = CopyString(index, SourceStringFieldHandle, snapshot.HandleUtf8ByteCount);
            var displayName = CopyString(index, SourceStringFieldDisplayName, snapshot.DisplayNameUtf8ByteCount);
            snapshots[index] = new NativeSourceSnapshot(
                handle,
                displayName,
                snapshot.SourceKind,
                snapshot.SampleRate,
                snapshot.Channels,
                snapshot.LastSeenMilliseconds,
                snapshot.Peak,
                ToMetrics(index, snapshot.Channels, snapshot.Metrics));
        }

        return snapshots;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.DlmSourceIngestCoreDestroyExport(_handle);
        _handle = IntPtr.Zero;
        _disposed = true;
    }

    private PcmMetricsSnapshot ToMetrics(int index, int sourceChannels, NativePcmAnalyzerSnapshot native)
    {
        return new PcmMetricsSnapshot(
            MomentaryLufs: ToNullable(native.HasMomentaryLufs, native.MomentaryLufs),
            ShortTermLufs: ToNullable(native.HasShortTermLufs, native.ShortTermLufs),
            IntegratedLufs: ToNullable(native.HasIntegratedLufs, native.IntegratedLufs),
            TruePeakDb: ToFiniteNullable(native.HasTruePeakDb, native.TruePeakDb),
            Rms100MsDb: ToNullable(native.HasRms100MsDb, native.Rms100MsDb),
            StereoImageX: ToNullable(native.HasStereoImageX, native.StereoImageX),
            StereoImageY: ToNullable(native.HasStereoImageY, native.StereoImageY),
            StereoImagePoints: CopyStereoImagePoints(index, native.StereoImagePointCount),
            Correlation: ToNullable(native.HasCorrelation, native.Correlation),
            WaveformSamples: CopyVector(index, native.WaveformCount, NativeMethods.DlmSourceIngestCoreCopyWaveformAtExport),
            ChannelWaveformSamples: CopyChannelVectors(index, native.ChannelWaveformCount, sourceChannels, NativeMethods.DlmSourceIngestCoreCopyChannelWaveformAtExport),
            SpectrumBuckets: CopyVector(index, native.SpectrumCount, NativeMethods.DlmSourceIngestCoreCopySpectrumAtExport),
            ChannelSpectrumBuckets: CopyChannelVectors(index, native.ChannelSpectrumCount, sourceChannels, NativeMethods.DlmSourceIngestCoreCopyChannelSpectrumAtExport),
            PlotFrameVersion: native.PlotFrameVersion);
    }

    private string CopyString(int index, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreCopySourceStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native source string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private IReadOnlyList<double>? CopyVector(int index, int count, NativeVectorCopy copy)
    {
        if (count <= 0)
        {
            return null;
        }

        var buffer = new double[count];
        ThrowIfFailed(copy(_handle, index, buffer, buffer.Length, out var actualCount), "Native source vector copy failed");
        return actualCount == buffer.Length ? buffer : buffer[..actualCount];
    }

    private IReadOnlyList<IReadOnlyList<double>>? CopyChannelVectors(int index, int count, int sourceChannels, NativeChannelVectorCopy copy)
    {
        if (count <= 0)
        {
            return null;
        }

        var channels = new IReadOnlyList<double>[sourceChannels];
        for (var channel = 0; channel < channels.Length; channel++)
        {
            var buffer = new double[count];
            ThrowIfFailed(copy(_handle, index, channel, buffer, buffer.Length, out var actualCount), "Native source channel vector copy failed");
            channels[channel] = actualCount == buffer.Length ? buffer : buffer[..actualCount];
        }

        return channels;
    }

    private IReadOnlyList<StereoImagePoint>? CopyStereoImagePoints(int index, int count)
    {
        if (count <= 0)
        {
            return null;
        }

        var angles = new double[count];
        var radii = new double[count];
        ThrowIfFailed(
            NativeMethods.DlmSourceIngestCoreCopyStereoImagePointsAtExport(_handle, index, angles, radii, count, out var actualCount),
            "Native source stereo image point copy failed");
        var points = new StereoImagePoint[actualCount];
        for (var pointIndex = 0; pointIndex < actualCount; pointIndex++)
        {
            points[pointIndex] = new StereoImagePoint(angles[pointIndex], radii[pointIndex]);
        }

        return points;
    }

    private static double? ToNullable(int hasValue, double value)
    {
        return hasValue == 0 ? null : value;
    }

    private static double? ToFiniteNullable(int hasValue, double value)
    {
        return hasValue == 0 || !double.IsFinite(value) ? null : value;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    private delegate int NativeVectorCopy(IntPtr handle, int index, double[] buffer, int capacity, out int actualCount);

    private delegate int NativeChannelVectorCopy(IntPtr handle, int index, int channel, double[] buffer, int capacity, out int actualCount);

    public sealed record NativeSourceSnapshot(
        string Handle,
        string DisplayName,
        int SourceKind,
        int SampleRate,
        int Channels,
        long LastSeenMilliseconds,
        float Peak,
        PcmMetricsSnapshot Metrics);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSourceIngestSnapshot
    {
        public int SourceKind;
        public int SampleRate;
        public int Channels;
        public long LastSeenMilliseconds;
        public float Peak;
        public int HandleUtf8ByteCount;
        public int DisplayNameUtf8ByteCount;
        public NativePcmAnalyzerSnapshot Metrics;
    }

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
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmSourceIngestCoreDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreRegisterExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreRegisterExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayName,
            int sourceKind,
            int sampleRate,
            int channels,
            long timestampMilliseconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreHeartbeatExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreHeartbeatExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            long timestampMilliseconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreRenameExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreRenameExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayName,
            long timestampMilliseconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreProcessF32Export", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreProcessF32Export(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            [In] float[] samples,
            int sampleCount,
            long timestampMilliseconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCloseExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCloseExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreRemoveExpiredExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreRemoveExpiredExport(IntPtr handle, long nowMilliseconds, long timeoutMilliseconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreResetSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreResetSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreResetAllExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreResetAllExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreSetTruePeakOversamplingExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreSetTruePeakOversamplingExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            int oversampling);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreSetChannelWaveformEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreSetChannelWaveformEnabledExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreSetChannelSpectrumEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreSetChannelSpectrumEnabledExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreSetPerformanceModeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreSetPerformanceModeExport(IntPtr handle, int mode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreGetSourceCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreGetSourceCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreGetSourceSnapshotAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreGetSourceSnapshotAtExport(IntPtr handle, int index, out NativeSourceIngestSnapshot snapshot);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopySourceStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopySourceStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopyWaveformAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopyWaveformAtExport(IntPtr handle, int index, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopyChannelWaveformAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopyChannelWaveformAtExport(IntPtr handle, int index, int channel, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopySpectrumAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopySpectrumAtExport(IntPtr handle, int index, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopyChannelSpectrumAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopyChannelSpectrumAtExport(IntPtr handle, int index, int channel, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmSourceIngestCoreCopyStereoImagePointsAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmSourceIngestCoreCopyStereoImagePointsAtExport(
            IntPtr handle,
            int index,
            [Out] double[] angleBuffer,
            [Out] double[] radiusBuffer,
            int capacity,
            out int actualCount);
    }
}
