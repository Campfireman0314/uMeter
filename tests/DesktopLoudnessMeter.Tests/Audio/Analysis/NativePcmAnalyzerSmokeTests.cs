using System.Runtime.InteropServices;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class NativePcmAnalyzerSmokeTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void NativeDllExportsPcmAnalyzerEntrypointsWhenCopied()
    {
        var nativeDll = Path.Combine(AppContext.BaseDirectory, "DesktopLoudnessMeter.Native.dll");
        if (!File.Exists(nativeDll) || !NativeLibrary.TryLoad(nativeDll, out var handle))
        {
            return;
        }

        try
        {
            if (!NativeLibrary.TryGetExport(handle, "DlmPcmAnalyzerCreateExport", out _))
            {
                return;
            }

            Assert.True(NativeLibrary.TryGetExport(handle, "DlmPcmAnalyzerProcessF32Export", out _));
            Assert.True(NativeLibrary.TryGetExport(handle, "DlmPcmAnalyzerResetExport", out _));
            Assert.True(NativeLibrary.TryGetExport(handle, "DlmPcmAnalyzerGetSnapshotExport", out _));
            Assert.True(NativeLibrary.TryGetExport(handle, "DlmPcmAnalyzerDestroyExport", out _));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [Fact]
    public void NativePcmAnalyzerRejectsInvalidOptionsWhenAvailable()
    {
        if (!NativePcmAnalyzerInterop.TryCreate(0, 2, 4, out var handle, out var error))
        {
            Assert.True(error is -3 or int.MinValue);
            return;
        }

        NativePcmAnalyzerInterop.Destroy(handle);
        Assert.Fail("Invalid native analyzer options were accepted.");
    }

    [Fact]
    public void NativePcmAnalyzerResetClearsSnapshotWhenAvailable()
    {
        if (!NativePcmAnalyzerInterop.TryCreate(48000, 2, 4, out var handle, out _))
        {
            return;
        }

        try
        {
            var samples = Enumerable.Range(0, 960)
                .Select(index => (float)(Math.Sin(2 * Math.PI * 1000 * index / 48000) * 0.25))
                .ToArray();
            Assert.Equal(0, NativePcmAnalyzerInterop.Process(handle, samples, samples.Length));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(handle, out var populated));
            Assert.Equal(1, populated.HasTruePeakDb);

            Assert.Equal(0, NativePcmAnalyzerInterop.Reset(handle));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(handle, out var reset));
            Assert.Equal(0, reset.HasTruePeakDb);
            Assert.Equal(0, reset.WaveformCount);
            Assert.Equal(0, reset.PlotFrameVersion);
        }
        finally
        {
            NativePcmAnalyzerInterop.Destroy(handle);
        }
    }

    [Fact]
    public void NativePcmAnalyzerMatchesManagedLoudnessAndRmsForSteadySineWhenAvailable()
    {
        var samples = StereoSine(seconds: 3.2, leftAmplitude: 0.35, rightAmplitude: 0.35);
        if (!TryAnalyzeNative(samples, channels: 2, oversampling: 4, out var native))
        {
            return;
        }

        var managed = AnalyzeManaged(samples, channels: 2, oversampling: 4);

        AssertOptionalClose(managed.MomentaryLufs, native.MomentaryLufs, native.HasMomentaryLufs);
        AssertOptionalClose(managed.ShortTermLufs, native.ShortTermLufs, native.HasShortTermLufs);
        AssertOptionalClose(managed.IntegratedLufs, native.IntegratedLufs, native.HasIntegratedLufs);
        AssertOptionalClose(managed.TruePeakDb, native.TruePeakDb, native.HasTruePeakDb, tolerance: 0.15);
        AssertOptionalClose(managed.Rms100MsDb, native.Rms100MsDb, native.HasRms100MsDb, tolerance: 0.05);
    }

    [Fact]
    public void NativePcmAnalyzerTracksRecentWindowAndStereoMetricsWhenAvailable()
    {
        var initial = StereoSine(seconds: 0.1, leftAmplitude: 0.9, rightAmplitude: 0.9);
        var tail = StereoSine(seconds: 0.2, leftAmplitude: 0.1, rightAmplitude: -0.1, startFrame: SampleRate / 10);
        var samples = initial.Concat(tail).ToArray();

        if (!TryAnalyzeNative(samples, channels: 2, oversampling: 8, out var native))
        {
            return;
        }

        var managed = AnalyzeManaged(samples, channels: 2, oversampling: 8);

        AssertOptionalClose(managed.TruePeakDb, native.TruePeakDb, native.HasTruePeakDb, tolerance: 0.2);
        AssertOptionalClose(managed.Rms100MsDb, native.Rms100MsDb, native.HasRms100MsDb, tolerance: 0.08);
        AssertOptionalClose(managed.Correlation, native.Correlation, native.HasCorrelation, tolerance: 0.02);
        AssertOptionalClose(managed.StereoImageX, native.StereoImageX, native.HasStereoImageX, tolerance: 0.02);
        AssertOptionalClose(managed.StereoImageY, native.StereoImageY, native.HasStereoImageY, tolerance: 0.02);
    }

    [Fact]
    public void NativePcmAnalyzerSanitizesNonFiniteSamplesWhenAvailable()
    {
        var samples = StereoSine(seconds: 0.2, leftAmplitude: 0.25, rightAmplitude: 0.25);
        samples[0] = float.NaN;
        samples[1] = float.PositiveInfinity;
        samples[2] = float.NegativeInfinity;

        if (!TryAnalyzeNative(samples, channels: 2, oversampling: 4, out var native))
        {
            return;
        }

        Assert.Equal(1, native.HasTruePeakDb);
        Assert.Equal(1, native.HasRms100MsDb);
        Assert.True(double.IsFinite(native.TruePeakDb));
        Assert.True(double.IsFinite(native.Rms100MsDb));
        Assert.True(native.HasMomentaryLufs == 0 || double.IsFinite(native.MomentaryLufs));
        Assert.True(native.HasShortTermLufs == 0 || double.IsFinite(native.ShortTermLufs));
        Assert.True(native.HasIntegratedLufs == 0 || double.IsFinite(native.IntegratedLufs));
    }

    [Fact]
    public void NativePcmAnalyzerLeavesStereoOnlyMetricsUnavailableForMonoWhenAvailable()
    {
        var samples = MonoSine(seconds: 0.2, amplitude: 0.4);
        if (!TryAnalyzeNative(samples, channels: 1, oversampling: 4, out var native))
        {
            return;
        }

        Assert.Equal(1, native.HasTruePeakDb);
        Assert.Equal(1, native.HasRms100MsDb);
        Assert.Equal(0, native.HasCorrelation);
        Assert.Equal(1, native.HasStereoImageX);
        Assert.Equal(1, native.HasStereoImageY);
        Assert.Equal(0d, native.StereoImageX, precision: 12);
        Assert.Equal(1d, native.StereoImageY, precision: 12);
    }

    [Fact]
    public void NativePcmAnalyzerExposesWaveformAndSpectrumDataWhenAvailable()
    {
        var samples = StereoSine(seconds: 0.2, leftAmplitude: 0.7, rightAmplitude: 0.7, frequency: 1000);
        if (!NativePcmAnalyzerInterop.TryCreate(SampleRate, 2, 4, out var handle, out _))
        {
            return;
        }

        try
        {
            Assert.Equal(0, NativePcmAnalyzerInterop.Process(handle, samples, samples.Length));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(handle, out var snapshot));
            Assert.True(snapshot.WaveformCount > 0);
            Assert.True(snapshot.SpectrumCount > 0);

            var waveform = new double[snapshot.WaveformCount];
            var spectrum = new double[snapshot.SpectrumCount];
            Assert.Equal(0, NativePcmAnalyzerInterop.CopyWaveform(handle, waveform, waveform.Length, out var waveformCount));
            Assert.Equal(0, NativePcmAnalyzerInterop.CopySpectrum(handle, spectrum, spectrum.Length, out var spectrumCount));

            Assert.Equal(snapshot.WaveformCount, waveformCount);
            Assert.Equal(snapshot.SpectrumCount, spectrumCount);
            Assert.All(waveform, sample => Assert.InRange(sample, -1, 1));
            Assert.All(spectrum, bucket => Assert.InRange(bucket, 0, 1));
            Assert.Contains(spectrum, bucket => bucket > 0);
        }
        finally
        {
            NativePcmAnalyzerInterop.Destroy(handle);
        }
    }

    [Fact]
    public void NativePcmAnalyzerCanExposePerChannelRollDataWhenEnabled()
    {
        var samples = DualSine(seconds: 0.2, leftFrequency: 1000, rightFrequency: 4000);
        if (!NativePcmAnalyzerInterop.TryCreate(SampleRate, 2, 4, out var handle, out _))
        {
            return;
        }

        try
        {
            Assert.Equal(0, NativePcmAnalyzerInterop.SetChannelWaveformEnabled(handle, enabled: 1));
            Assert.Equal(0, NativePcmAnalyzerInterop.SetChannelSpectrumEnabled(handle, enabled: 1));
            Assert.Equal(0, NativePcmAnalyzerInterop.Process(handle, samples, samples.Length));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(handle, out var snapshot));

            Assert.True(snapshot.ChannelWaveformCount > 0);
            Assert.True(snapshot.ChannelSpectrumCount > 0);

            var leftWaveform = new double[snapshot.ChannelWaveformCount];
            var rightWaveform = new double[snapshot.ChannelWaveformCount];
            var leftSpectrum = new double[snapshot.ChannelSpectrumCount];
            var rightSpectrum = new double[snapshot.ChannelSpectrumCount];

            Assert.Equal(0, NativePcmAnalyzerInterop.CopyChannelWaveform(handle, 0, leftWaveform, leftWaveform.Length, out var leftWaveformCount));
            Assert.Equal(0, NativePcmAnalyzerInterop.CopyChannelWaveform(handle, 1, rightWaveform, rightWaveform.Length, out var rightWaveformCount));
            Assert.Equal(0, NativePcmAnalyzerInterop.CopyChannelSpectrum(handle, 0, leftSpectrum, leftSpectrum.Length, out var leftSpectrumCount));
            Assert.Equal(0, NativePcmAnalyzerInterop.CopyChannelSpectrum(handle, 1, rightSpectrum, rightSpectrum.Length, out var rightSpectrumCount));

            Assert.Equal(snapshot.ChannelWaveformCount, leftWaveformCount);
            Assert.Equal(snapshot.ChannelWaveformCount, rightWaveformCount);
            Assert.Equal(snapshot.ChannelSpectrumCount, leftSpectrumCount);
            Assert.Equal(snapshot.ChannelSpectrumCount, rightSpectrumCount);
            Assert.NotEqual(leftWaveform, rightWaveform);
            Assert.NotEqual(leftSpectrum, rightSpectrum);
        }
        finally
        {
            NativePcmAnalyzerInterop.Destroy(handle);
        }
    }

    [Fact]
    public void NativePcmAnalyzerEcoModeReducesSpectrumResolutionWhenAvailable()
    {
        var samples = StereoSine(seconds: 0.2, leftAmplitude: 0.7, rightAmplitude: 0.7, frequency: 1000);
        if (!NativePcmAnalyzerInterop.TryCreate(SampleRate, 2, 4, out var normalHandle, out _))
        {
            return;
        }

        try
        {
            Assert.Equal(0, NativePcmAnalyzerInterop.Process(normalHandle, samples, samples.Length));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(normalHandle, out var normalSnapshot));
            var normalSpectrum = new double[normalSnapshot.SpectrumCount];
            Assert.Equal(0, NativePcmAnalyzerInterop.CopySpectrum(normalHandle, normalSpectrum, normalSpectrum.Length, out _));

            if (!NativePcmAnalyzerInterop.TryCreate(SampleRate, 2, 4, out var ecoHandle, out _))
            {
                return;
            }

            try
            {
                Assert.Equal(0, NativePcmAnalyzerInterop.SetPerformanceMode(ecoHandle, mode: 1));
                Assert.Equal(0, NativePcmAnalyzerInterop.Process(ecoHandle, samples, samples.Length));
                Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(ecoHandle, out var ecoSnapshot));
                var ecoSpectrum = new double[ecoSnapshot.SpectrumCount];
                Assert.Equal(0, NativePcmAnalyzerInterop.CopySpectrum(ecoHandle, ecoSpectrum, ecoSpectrum.Length, out _));

                Assert.Equal(normalSnapshot.SpectrumCount, ecoSnapshot.SpectrumCount);
                Assert.NotEqual(normalSpectrum, ecoSpectrum);
            }
            finally
            {
                NativePcmAnalyzerInterop.Destroy(ecoHandle);
            }
        }
        finally
        {
            NativePcmAnalyzerInterop.Destroy(normalHandle);
        }
    }

    private static bool TryAnalyzeNative(
        float[] samples,
        int channels,
        int oversampling,
        out NativePcmAnalyzerSnapshot snapshot)
    {
        snapshot = default;
        if (!NativePcmAnalyzerInterop.TryCreate(SampleRate, channels, oversampling, out var handle, out _))
        {
            return false;
        }

        try
        {
            Assert.Equal(0, NativePcmAnalyzerInterop.Process(handle, samples, samples.Length));
            Assert.Equal(0, NativePcmAnalyzerInterop.GetSnapshot(handle, out snapshot));
            return true;
        }
        finally
        {
            NativePcmAnalyzerInterop.Destroy(handle);
        }
    }

    private static PcmMetricsSnapshot AnalyzeManaged(float[] samples, int channels, int oversampling)
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, channels, oversampling));
        analyzer.Process(samples);
        return analyzer.Snapshot;
    }

    private static void AssertOptionalClose(
        double? expected,
        double actual,
        int hasActual,
        double tolerance = 0.1)
    {
        if (expected is null)
        {
            Assert.Equal(0, hasActual);
            return;
        }

        Assert.Equal(1, hasActual);
        Assert.InRange(actual, expected.Value - tolerance, expected.Value + tolerance);
    }

    private static float[] StereoSine(
        double seconds,
        double leftAmplitude,
        double rightAmplitude,
        int startFrame = 0,
        double frequency = 1000)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var phase = 2 * Math.PI * frequency * (startFrame + frame) / SampleRate;
            samples[frame * 2] = (float)(Math.Sin(phase) * leftAmplitude);
            samples[(frame * 2) + 1] = (float)(Math.Sin(phase) * rightAmplitude);
        }

        return samples;
    }

    private static float[] MonoSine(double seconds, double amplitude, double frequency = 1000)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            samples[frame] = (float)(Math.Sin(2 * Math.PI * frequency * frame / SampleRate) * amplitude);
        }

        return samples;
    }

    private static float[] DualSine(
        double seconds,
        double leftFrequency,
        double rightFrequency,
        double amplitude = 0.7)
    {
        var frames = (int)Math.Round(seconds * SampleRate);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            samples[frame * 2] = (float)(Math.Sin(2 * Math.PI * leftFrequency * frame / SampleRate) * amplitude);
            samples[(frame * 2) + 1] = (float)(Math.Sin(2 * Math.PI * rightFrequency * frame / SampleRate) * amplitude);
        }

        return samples;
    }

    private static partial class NativePcmAnalyzerInterop
    {
        public static bool TryCreate(int sampleRate, int channels, int oversampling, out IntPtr handle, out int error)
        {
            handle = IntPtr.Zero;
            try
            {
                error = Create(sampleRate, channels, oversampling, out handle);
                return error == 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            {
                error = int.MinValue;
                return false;
            }
        }

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCreateExport", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Create(int sampleRate, int channels, int oversampling, out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Destroy(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerResetExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Reset(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerProcessF32Export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Process(IntPtr handle, [In] float[] samples, int sampleCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerGetSnapshotExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSnapshot(IntPtr handle, out NativePcmAnalyzerSnapshot snapshot);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetChannelWaveformEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetChannelWaveformEnabled(IntPtr handle, int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetChannelSpectrumEnabledExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetChannelSpectrumEnabled(IntPtr handle, int enabled);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerSetPerformanceModeExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetPerformanceMode(IntPtr handle, int mode);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyWaveformExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyWaveform(IntPtr handle, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyChannelWaveformExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyChannelWaveform(IntPtr handle, int channel, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopySpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopySpectrum(IntPtr handle, [Out] double[] buffer, int capacity, out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmPcmAnalyzerCopyChannelSpectrumExport", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyChannelSpectrum(IntPtr handle, int channel, [Out] double[] buffer, int capacity, out int actualCount);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePcmAnalyzerSnapshot
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
}
