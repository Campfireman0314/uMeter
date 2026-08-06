using System.Diagnostics;
using DesktopLoudnessMeter.App.Audio.Analysis;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace DesktopLoudnessMeter.App.Audio;

public sealed class WasapiSessionMeterService :
    IAudioSessionMeterService,
    IAudioSessionResetService,
    IAudioSessionAnalysisSettingsService,
    IDisposable
{
    private const float AudiblePeakFloor = 0.000001f;
    private const double AudibleMetricFloorDb = -90;
    private readonly MMDeviceEnumerator _deviceEnumerator = new();
    private readonly Dictionary<int, string?> _processNameCache = [];
    private readonly IProcessMetricProvider? _processMetricProvider;
    private readonly Func<IReadOnlyList<AudioSessionSnapshot>>? _snapshotSource;
    private readonly int _currentProcessId;

    public WasapiSessionMeterService()
        : this(new ProcessLoopbackMetricProvider())
    {
    }

    public WasapiSessionMeterService(IProcessMetricProvider? processMetricProvider)
        : this(processMetricProvider, Environment.ProcessId)
    {
    }

    private WasapiSessionMeterService(IProcessMetricProvider? processMetricProvider, int currentProcessId)
    {
        _processMetricProvider = processMetricProvider;
        _currentProcessId = currentProcessId;
    }

    public WasapiSessionMeterService(
        IProcessMetricProvider? processMetricProvider,
        Func<IReadOnlyList<AudioSessionSnapshot>> snapshotSource)
        : this(processMetricProvider, snapshotSource, Environment.ProcessId)
    {
    }

    public WasapiSessionMeterService(
        IProcessMetricProvider? processMetricProvider,
        Func<IReadOnlyList<AudioSessionSnapshot>> snapshotSource,
        int currentProcessId)
    {
        _processMetricProvider = processMetricProvider;
        _snapshotSource = snapshotSource;
        _currentProcessId = currentProcessId;
    }

    public IReadOnlyList<AudioSessionSnapshot> GetCurrentSessions()
    {
        try
        {
            if (_snapshotSource is not null)
            {
                return AddProcessMetrics(NormalizeRows(_snapshotSource()));
            }

            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = device.AudioSessionManager.Sessions;
            var snapshots = new List<AudioSessionSnapshot>(sessions.Count);

            for (var index = 0; index < sessions.Count; index++)
            {
                using var session = sessions[index];
                if (session.State == AudioSessionState.AudioSessionStateExpired)
                {
                    continue;
                }

                var processId = NormalizeProcessId(session.GetProcessID);
                var peak = PeakMath.ClampPeak(session.AudioMeterInformation.MasterPeakValue);
                var key = ResolveSessionKey(session, processId, index);

                snapshots.Add(new AudioSessionSnapshot(
                    key,
                    processId,
                    ResolveDisplayName(session, processId),
                    peak,
                    PeakMath.ToDecibels(peak),
                    PeakMath.FormatDb(peak),
                    session.State.ToString()));
            }

            return AddProcessMetrics(NormalizeRows(snapshots));
        }
        catch (Exception ex) when (IsRecoverableAudioFailure(ex))
        {
            return [];
        }
    }

    public void Dispose()
    {
        _processMetricProvider?.Dispose();
        _deviceEnumerator.Dispose();
    }

    public void ResetSession(string sessionKey, int? processId)
    {
        if (processId is not int value || value == _currentProcessId)
        {
            return;
        }

        _processMetricProvider?.ResetProcess(value);
    }

    public void ResetAllSessions()
    {
        _processMetricProvider?.ResetAll();
    }

    public void SetTruePeakOversampling(string sessionKey, int? processId, int oversampling)
    {
        if (processId is not int value || value == _currentProcessId)
        {
            return;
        }

        _processMetricProvider?.SetTruePeakOversampling(value, oversampling);
    }

    public void SetChannelSpectrumEnabled(string sessionKey, int? processId, bool enabled)
    {
        if (processId is not int value || value == _currentProcessId)
        {
            return;
        }

        _processMetricProvider?.SetChannelSpectrumEnabled(value, enabled);
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        _processMetricProvider?.SetPerformanceMode(mode);
    }

    public void SetChannelWaveformEnabled(string sessionKey, int? processId, bool enabled)
    {
        if (processId is not int value || value == _currentProcessId)
        {
            return;
        }

        _processMetricProvider?.SetChannelWaveformEnabled(value, enabled);
    }

    private IReadOnlyList<AudioSessionSnapshot> AddProcessMetrics(IReadOnlyList<AudioSessionSnapshot> snapshots)
    {
        if (_processMetricProvider is null)
        {
            return snapshots.Where(snapshot => IsVisibleAudioRow(snapshot, metrics: null)).ToArray();
        }

        var processIds = snapshots
            .Where(snapshot => snapshot.ProcessId is not null)
            .Select(snapshot => snapshot.ProcessId!.Value)
            .Distinct()
            .ToArray();

        try
        {
            _processMetricProvider.TrackProcesses(processIds);
        }
        catch (Exception ex) when (IsRecoverableAudioFailure(ex))
        {
            return snapshots.Where(snapshot => IsVisibleAudioRow(snapshot, metrics: null)).ToArray();
        }

        return snapshots
            .Select(snapshot =>
            {
                var metrics = snapshot.ProcessId is int processId
                    ? TryGetMetrics(processId)
                    : null;
                return (Snapshot: snapshot, Metrics: metrics);
            })
            .Where(item => IsVisibleAudioRow(item.Snapshot, item.Metrics))
            .Select(item => MergeMetrics(item.Snapshot, item.Metrics))
            .ToArray();
    }

    private PcmMetricsSnapshot? TryGetMetrics(int processId)
    {
        try
        {
            return _processMetricProvider?.GetMetricsForProcess(processId);
        }
        catch (Exception ex) when (IsRecoverableAudioFailure(ex))
        {
            return null;
        }
    }

    private static AudioSessionSnapshot MergeMetrics(AudioSessionSnapshot snapshot, PcmMetricsSnapshot? metrics)
    {
        return metrics is null
            ? snapshot
            : snapshot with
            {
                MomentaryLufs = metrics.MomentaryLufs,
                ShortTermLufs = metrics.ShortTermLufs,
                IntegratedLufs = metrics.IntegratedLufs,
                TruePeakDb = metrics.TruePeakDb,
                Rms100MsDb = metrics.Rms100MsDb,
                StereoImageX = metrics.StereoImageX,
                StereoImageY = metrics.StereoImageY,
                StereoImagePoints = metrics.StereoImagePoints,
                Correlation = metrics.Correlation,
                WaveformSamples = metrics.WaveformSamples,
                ChannelWaveformSamples = metrics.ChannelWaveformSamples,
                SpectrumBuckets = metrics.SpectrumBuckets,
                ChannelSpectrumBuckets = metrics.ChannelSpectrumBuckets,
                PlotFrameVersion = metrics.PlotFrameVersion,
                MidMetrics = metrics.MidMetrics,
                SideMetrics = metrics.SideMetrics
            };
    }

    private IReadOnlyList<AudioSessionSnapshot> NormalizeRows(IReadOnlyList<AudioSessionSnapshot> snapshots)
    {
        return snapshots
            .Where(snapshot => snapshot.ProcessId != _currentProcessId)
            .GroupBy(GetProcessRowKey, StringComparer.Ordinal)
            .Select(CollapseProcessRows)
            .OrderByDescending(session => session.Peak)
            .ThenBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private bool IsVisibleAudioRow(AudioSessionSnapshot snapshot, PcmMetricsSnapshot? metrics)
    {
        if (snapshot.ProcessId == _currentProcessId)
        {
            return false;
        }

        if (snapshot.ProcessId is not null && metrics is not null)
        {
            return HasCurrentAudiblePcm(metrics);
        }

        return snapshot.Peak > AudiblePeakFloor || HasAudibleAnalyzerMetrics(snapshot);
    }

    private static bool HasCurrentAudiblePcm(PcmMetricsSnapshot metrics)
    {
        return IsAudibleDb(metrics.TruePeakDb) || IsAudibleDb(metrics.Rms100MsDb);
    }

    private static string GetProcessRowKey(AudioSessionSnapshot snapshot)
    {
        return snapshot.ProcessId is int processId
            ? $"pid:{processId}"
            : snapshot.SessionKey;
    }

    private static AudioSessionSnapshot CollapseProcessRows(IGrouping<string, AudioSessionSnapshot> group)
    {
        var rows = group.ToArray();
        var loudest = rows
            .OrderByDescending(snapshot => snapshot.Peak)
            .ThenBy(snapshot => snapshot.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .First();
        var peak = rows.Max(snapshot => snapshot.Peak);
        var state = rows.Any(snapshot => snapshot.State.Contains("Active", StringComparison.OrdinalIgnoreCase))
            ? "AudioSessionStateActive"
            : loudest.State;

        return loudest with
        {
            SourceKey = group.Key,
            Peak = peak,
            PeakDb = PeakMath.ToDecibels(peak),
            PeakDbText = PeakMath.FormatDb(peak),
            State = state
        };
    }

    private static bool HasAudibleAnalyzerMetrics(AudioSessionSnapshot snapshot)
    {
        return IsAudibleDb(snapshot.TruePeakDb)
            || IsAudibleDb(snapshot.Rms100MsDb)
            || IsAudibleDb(snapshot.MomentaryLufs)
            || IsAudibleDb(snapshot.ShortTermLufs)
            || IsAudibleDb(snapshot.IntegratedLufs);
    }

    private static bool IsAudibleDb(double? value)
    {
        return value is double metric
            && double.IsFinite(metric)
            && metric > AudibleMetricFloorDb;
    }

    private static int? NormalizeProcessId(uint processId)
    {
        return processId == 0 || processId > int.MaxValue ? null : (int)processId;
    }

    private static string ResolveSessionKey(AudioSessionControl session, int? processId, int index)
    {
        if (!string.IsNullOrWhiteSpace(session.GetSessionInstanceIdentifier))
        {
            return session.GetSessionInstanceIdentifier;
        }

        if (!string.IsNullOrWhiteSpace(session.GetSessionIdentifier))
        {
            return session.GetSessionIdentifier;
        }

        return processId is null ? $"session:{index}" : $"pid:{processId.Value}";
    }

    private string ResolveDisplayName(AudioSessionControl session, int? processId)
    {
        var processName = ResolveProcessName(processId);
        if (!string.IsNullOrWhiteSpace(processName))
        {
            return processName;
        }

        if (session.IsSystemSoundsSession)
        {
            return "System Sounds";
        }

        if (!string.IsNullOrWhiteSpace(session.DisplayName))
        {
            return session.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(session.GetSessionIdentifier))
        {
            return ShortenIdentifier(session.GetSessionIdentifier);
        }

        return "Unknown session";
    }

    private string? ResolveProcessName(int? processId)
    {
        if (processId is null)
        {
            return null;
        }

        if (_processNameCache.TryGetValue(processId.Value, out var cached))
        {
            return cached;
        }

        try
        {
            using var process = Process.GetProcessById(processId.Value);
            var description = TryGetMainModuleDescription(process);
            if (!string.IsNullOrWhiteSpace(description))
            {
                _processNameCache[processId.Value] = description;
                return description;
            }

            _processNameCache[processId.Value] = process.ProcessName;
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            _processNameCache.Remove(processId.Value);
            return null;
        }
        catch (InvalidOperationException)
        {
            _processNameCache.Remove(processId.Value);
            return null;
        }
    }

    private static string? TryGetMainModuleDescription(Process process)
    {
        try
        {
            return process.MainModule?.FileVersionInfo.FileDescription;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static string ShortenIdentifier(string identifier)
    {
        var trimmed = identifier.Trim();
        var lastSlash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        return lastSlash >= 0 && lastSlash < trimmed.Length - 1
            ? trimmed[(lastSlash + 1)..]
            : trimmed;
    }

    private static bool IsRecoverableAudioFailure(Exception ex)
    {
        return ex is UnauthorizedAccessException
            or InvalidOperationException
            or System.IO.FileNotFoundException
            or NAudio.MmException
            or System.Runtime.InteropServices.COMException;
    }
}
