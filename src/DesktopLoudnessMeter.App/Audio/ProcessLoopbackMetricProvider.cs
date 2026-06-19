using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Diagnostics;

namespace DesktopLoudnessMeter.App.Audio;

public sealed class ProcessLoopbackMetricProvider : IProcessMetricProvider
{
    private static readonly TimeSpan DefaultFailureBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultStaleWorkerRetention = TimeSpan.FromSeconds(5);
    private readonly IProcessLoopbackCaptureWorkerFactory _workerFactory;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _failureBackoff;
    private readonly TimeSpan _staleWorkerRetention;
    private readonly Dictionary<int, IProcessLoopbackCaptureWorker> _workers = [];
    private readonly Dictionary<int, DateTimeOffset> _retryAfter = [];
    private readonly Dictionary<int, DateTimeOffset> _lastVisibleAt = [];
    private readonly Dictionary<int, int> _truePeakOversamplingByProcess = [];
    private readonly Dictionary<int, bool> _channelWaveformEnabledByProcess = [];
    private readonly Dictionary<int, bool> _channelSpectrumEnabledByProcess = [];
    private PcmAnalysisPerformanceMode _performanceMode = PcmAnalysisPerformanceMode.Normal;

    public ProcessLoopbackMetricProvider()
        : this(new ProcessLoopbackCaptureWorkerFactory(), () => DateTimeOffset.Now)
    {
    }

    public ProcessLoopbackMetricProvider(
        IProcessLoopbackCaptureWorkerFactory workerFactory,
        Func<DateTimeOffset> clock,
        TimeSpan? failureBackoff = null,
        TimeSpan? staleWorkerRetention = null)
    {
        _workerFactory = workerFactory;
        _clock = clock;
        _failureBackoff = failureBackoff ?? DefaultFailureBackoff;
        _staleWorkerRetention = staleWorkerRetention ?? DefaultStaleWorkerRetention;
    }

    public void TrackProcesses(IReadOnlyCollection<int> processIds)
    {
        var visible = processIds
            .Where(processId => processId > 0)
            .Distinct()
            .ToHashSet();
        var now = _clock();

        foreach (var processId in visible)
        {
            _lastVisibleAt[processId] = now;
        }

        foreach (var processId in _workers.Keys.ToArray())
        {
            if (!visible.Contains(processId))
            {
                if (!ShouldRetainStaleWorker(processId, now))
                {
                    StopWorker(processId);
                }
            }
            else if (_workers[processId].IsFaulted)
            {
                StopWorker(processId);
                _retryAfter[processId] = now + _failureBackoff;
            }
        }

        foreach (var processId in visible)
        {
            if (_workers.ContainsKey(processId) || IsBackedOff(processId))
            {
                continue;
            }

            try
            {
                var worker = _workerFactory.Create(processId);
                worker.SetTruePeakOversampling(GetTruePeakOversampling(processId));
                worker.SetChannelWaveformEnabled(GetChannelWaveformEnabled(processId));
                worker.SetChannelSpectrumEnabled(GetChannelSpectrumEnabled(processId));
                worker.SetPerformanceMode(_performanceMode);
                worker.Start();
                _workers[processId] = worker;
            }
            catch (Exception ex) when (IsRecoverableStartFailure(ex))
            {
                AppLogger.Error($"Process loopback start failed for PID {processId}.", ex);
                _retryAfter[processId] = now + _failureBackoff;
            }
        }
    }

    public PcmMetricsSnapshot? GetMetricsForProcess(int processId)
    {
        return _workers.TryGetValue(processId, out var worker) && !worker.IsFaulted
            ? worker.Snapshot
            : null;
    }

    public void ResetProcess(int processId)
    {
        if (_workers.TryGetValue(processId, out var worker) && !worker.IsFaulted)
        {
            worker.Reset();
        }
    }

    public void ResetAll()
    {
        foreach (var worker in _workers.Values.Where(worker => !worker.IsFaulted))
        {
            worker.Reset();
        }
    }

    public void SetTruePeakOversampling(int processId, int oversampling)
    {
        ValidateTruePeakOversampling(oversampling);
        if (processId <= 0)
        {
            return;
        }

        _truePeakOversamplingByProcess[processId] = oversampling;
        if (_workers.TryGetValue(processId, out var worker) && !worker.IsFaulted)
        {
            worker.SetTruePeakOversampling(oversampling);
        }
    }

    public void SetChannelWaveformEnabled(int processId, bool enabled)
    {
        if (processId <= 0)
        {
            return;
        }

        if (_channelWaveformEnabledByProcess.TryGetValue(processId, out var current) && current == enabled)
        {
            return;
        }

        if (!enabled && !_channelWaveformEnabledByProcess.ContainsKey(processId))
        {
            return;
        }

        _channelWaveformEnabledByProcess[processId] = enabled;
        if (_workers.TryGetValue(processId, out var worker) && !worker.IsFaulted)
        {
            worker.SetChannelWaveformEnabled(enabled);
        }
    }

    public void SetChannelSpectrumEnabled(int processId, bool enabled)
    {
        if (processId <= 0)
        {
            return;
        }

        if (_channelSpectrumEnabledByProcess.TryGetValue(processId, out var current) && current == enabled)
        {
            return;
        }

        if (!enabled && !_channelSpectrumEnabledByProcess.ContainsKey(processId))
        {
            return;
        }

        _channelSpectrumEnabledByProcess[processId] = enabled;
        if (_workers.TryGetValue(processId, out var worker) && !worker.IsFaulted)
        {
            worker.SetChannelSpectrumEnabled(enabled);
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        if (_performanceMode == mode)
        {
            return;
        }

        _performanceMode = mode;
        foreach (var worker in _workers.Values.Where(worker => !worker.IsFaulted))
        {
            worker.SetPerformanceMode(mode);
        }
    }

    public void Dispose()
    {
        foreach (var processId in _workers.Keys.ToArray())
        {
            StopWorker(processId);
        }
    }

    private bool IsBackedOff(int processId)
    {
        if (!_retryAfter.TryGetValue(processId, out var retryAfter))
        {
            return false;
        }

        if (_clock() < retryAfter)
        {
            return true;
        }

        _retryAfter.Remove(processId);
        return false;
    }

    private void StopWorker(int processId)
    {
        if (!_workers.Remove(processId, out var worker))
        {
            return;
        }

        worker.Dispose();
        _lastVisibleAt.Remove(processId);
    }

    private bool ShouldRetainStaleWorker(int processId, DateTimeOffset now)
    {
        return _staleWorkerRetention > TimeSpan.Zero
            && _lastVisibleAt.TryGetValue(processId, out var lastVisibleAt)
            && now - lastVisibleAt <= _staleWorkerRetention;
    }

    private static bool IsRecoverableStartFailure(Exception ex)
    {
        return ex is UnauthorizedAccessException
            or InvalidOperationException
            or System.IO.FileNotFoundException
            or PlatformNotSupportedException
            or System.Runtime.InteropServices.COMException
            or NAudio.MmException;
    }

    private int GetTruePeakOversampling(int processId)
    {
        return _truePeakOversamplingByProcess.GetValueOrDefault(processId, 4);
    }

    private bool GetChannelWaveformEnabled(int processId)
    {
        return _channelWaveformEnabledByProcess.GetValueOrDefault(processId);
    }

    private bool GetChannelSpectrumEnabled(int processId)
    {
        return _channelSpectrumEnabledByProcess.GetValueOrDefault(processId);
    }

    private static void ValidateTruePeakOversampling(int oversampling)
    {
        if (oversampling is not (2 or 4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(oversampling), "True-peak oversampling must be 2x, 4x, or 8x.");
        }
    }
}

public interface IProcessLoopbackCaptureWorkerFactory
{
    IProcessLoopbackCaptureWorker Create(int processId);
}

public interface IProcessLoopbackCaptureWorker : IDisposable
{
    PcmMetricsSnapshot? Snapshot { get; }

    bool IsFaulted { get; }

    void Start();

    void Reset();

    void SetTruePeakOversampling(int oversampling);

    void SetChannelWaveformEnabled(bool enabled);

    void SetChannelSpectrumEnabled(bool enabled);

    void SetPerformanceMode(PcmAnalysisPerformanceMode mode);
}

internal sealed class ProcessLoopbackCaptureWorkerFactory : IProcessLoopbackCaptureWorkerFactory
{
    public IProcessLoopbackCaptureWorker Create(int processId)
    {
        return new ProcessLoopbackCaptureWorker(processId);
    }
}
