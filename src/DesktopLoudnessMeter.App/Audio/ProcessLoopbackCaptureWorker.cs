using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DesktopLoudnessMeter.App.Audio;

internal sealed class ProcessLoopbackCaptureWorker : IProcessLoopbackCaptureWorker
{
    private const int SampleRate = 48000;
    private const int Channels = 2;
    private const int BufferMilliseconds = 50;
    private readonly int _processId;
    private readonly object _lock = new();
    private int _truePeakOversampling = 4;
    private bool _channelWaveformEnabled;
    private bool _channelSpectrumEnabled;
    private PcmAnalysisPerformanceMode _performanceMode = PcmAnalysisPerformanceMode.Normal;
    private PcmMetricAnalyzer _analyzer = CreateAnalyzer(4, channelWaveformEnabled: false, channelSpectrumEnabled: false, PcmAnalysisPerformanceMode.Normal);
    private Thread? _captureThread;
    private volatile bool _stopRequested;
    private volatile bool _isFaulted;
    private AudioClient? _audioClient;
    private EventWaitHandle? _frameEvent;
    private WaveFormat _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

    public ProcessLoopbackCaptureWorker(int processId)
    {
        _processId = processId;
    }

    public PcmMetricsSnapshot? Snapshot
    {
        get
        {
            lock (_lock)
            {
                return _analyzer.Snapshot;
            }
        }
    }

    public bool IsFaulted => _isFaulted;

    public void Start()
    {
        if (!ProcessLoopbackInterop.IsSupported)
        {
            throw new PlatformNotSupportedException("Windows process loopback can only be attempted on Windows 10 build 19041 or later.");
        }

        if (_captureThread is not null)
        {
            return;
        }

        _captureThread = new Thread(CaptureThread)
        {
            IsBackground = true,
            Name = $"Process loopback {_processId}"
        };
        _captureThread.Start();
    }

    public void Dispose()
    {
        _stopRequested = true;
        _frameEvent?.Set();
        _captureThread?.Join(TimeSpan.FromSeconds(2));
        lock (_lock)
        {
            _analyzer.Dispose();
        }

        _audioClient?.Dispose();
        _frameEvent?.Dispose();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _analyzer.Reset();
        }
    }

    public void SetTruePeakOversampling(int oversampling)
    {
        lock (_lock)
        {
            if (_truePeakOversampling == oversampling)
            {
                return;
            }

            var previousAnalyzer = _analyzer;
            _analyzer = CreateAnalyzer(oversampling, _channelWaveformEnabled, _channelSpectrumEnabled, _performanceMode);
            previousAnalyzer.Dispose();
            _truePeakOversampling = oversampling;
        }
    }

    public void SetChannelWaveformEnabled(bool enabled)
    {
        lock (_lock)
        {
            if (_channelWaveformEnabled == enabled)
            {
                return;
            }

            _channelWaveformEnabled = enabled;
            _analyzer.SetChannelWaveformEnabled(enabled);
        }
    }

    public void SetChannelSpectrumEnabled(bool enabled)
    {
        lock (_lock)
        {
            if (_channelSpectrumEnabled == enabled)
            {
                return;
            }

            _channelSpectrumEnabled = enabled;
            _analyzer.SetChannelSpectrumEnabled(enabled);
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        lock (_lock)
        {
            if (_performanceMode == mode)
            {
                return;
            }

            _performanceMode = mode;
            _analyzer.SetPerformanceMode(mode);
        }
    }

    private void CaptureThread()
    {
        try
        {
            _audioClient = ProcessLoopbackInterop.ActivateAudioClientAsync((uint)_processId).GetAwaiter().GetResult();
            InitializeAudioClient(_audioClient);
            CaptureLoop(_audioClient);
        }
        catch (Exception ex) when (IsRecoverableCaptureFailure(ex))
        {
            _isFaulted = true;
            if (ex is System.IO.FileNotFoundException or PlatformNotSupportedException)
            {
                AppLogger.Info($"Process loopback unavailable for PID {_processId}: {ex.Message}");
            }
            else
            {
                AppLogger.Error($"Process loopback worker failed for PID {_processId}.", ex);
            }
        }
    }

    private void InitializeAudioClient(AudioClient audioClient)
    {
        var flags = AudioClientStreamFlags.Loopback
            | AudioClientStreamFlags.EventCallback
            | AudioClientStreamFlags.AutoConvertPcm
            | AudioClientStreamFlags.SrcDefaultQuality;
        var bufferDuration = BufferMilliseconds * 10_000L;

        audioClient.Initialize(
            AudioClientShareMode.Shared,
            flags,
            bufferDuration,
            0,
            _waveFormat,
            Guid.Empty);

        _frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        audioClient.SetEventHandle(_frameEvent.SafeWaitHandle.DangerousGetHandle());
    }

    private void CaptureLoop(AudioClient audioClient)
    {
        var capture = audioClient.AudioCaptureClient;
        var bytesPerFrame = _waveFormat.BlockAlign;

        audioClient.Start();
        try
        {
            while (!_stopRequested)
            {
                _frameEvent?.WaitOne(BufferMilliseconds * 3);

                var packetSize = capture.GetNextPacketSize();
                while (packetSize > 0)
                {
                    ReadPacket(capture, bytesPerFrame);
                    packetSize = capture.GetNextPacketSize();
                }
            }
        }
        finally
        {
            audioClient.Stop();
            audioClient.Reset();
        }
    }

    private void ReadPacket(AudioCaptureClient capture, int bytesPerFrame)
    {
        var buffer = capture.GetBuffer(out var framesRead, out var flags, out _, out _);
        try
        {
            if (framesRead <= 0)
            {
                return;
            }

            var samples = (flags & AudioClientBufferFlags.Silent) != 0
                ? new float[framesRead * _waveFormat.Channels]
                : CopyAndConvert(buffer, framesRead, bytesPerFrame);

            lock (_lock)
            {
                _analyzer.Process(samples);
            }
        }
        finally
        {
            capture.ReleaseBuffer(framesRead);
        }
    }

    private float[] CopyAndConvert(IntPtr buffer, int framesRead, int bytesPerFrame)
    {
        var bytes = new byte[framesRead * bytesPerFrame];
        System.Runtime.InteropServices.Marshal.Copy(buffer, bytes, 0, bytes.Length);
        return WaveFormatSampleConverter.ToInterleavedFloatSamples(bytes, _waveFormat, framesRead);
    }

    private static bool IsRecoverableCaptureFailure(Exception ex)
    {
        return ex is InvalidOperationException
            or PlatformNotSupportedException
            or UnauthorizedAccessException
            or System.IO.FileNotFoundException
            or System.Runtime.InteropServices.COMException
            or NAudio.MmException
            or NotSupportedException;
    }

    private static PcmMetricAnalyzer CreateAnalyzer(
        int truePeakOversampling,
        bool channelWaveformEnabled,
        bool channelSpectrumEnabled,
        PcmAnalysisPerformanceMode performanceMode)
    {
        var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(SampleRate, Channels, truePeakOversampling));
        analyzer.SetChannelWaveformEnabled(channelWaveformEnabled);
        analyzer.SetChannelSpectrumEnabled(channelSpectrumEnabled);
        analyzer.SetPerformanceMode(performanceMode);
        return analyzer;
    }
}
