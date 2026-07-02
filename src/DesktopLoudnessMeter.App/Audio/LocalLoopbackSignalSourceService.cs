using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.App.Audio;

public sealed class LocalLoopbackSignalSourceService :
    ISignalSourceMeterService,
    ISignalSourceResetService,
    ISignalSourceAnalysisSettingsService,
    IDisposable
{
    public static readonly TimeSpan DefaultSourceTimeout = TimeSpan.FromSeconds(3);
    private const string SourceKeyPrefix = "loopback:";
    private readonly object _lock = new();
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _sourceTimeout;
    private readonly NativeSourceIngestCore _core = new();
    private bool _isDisposed;

    public LocalLoopbackSignalSourceService()
        : this(() => DateTimeOffset.Now)
    {
    }

    public LocalLoopbackSignalSourceService(Func<DateTimeOffset> clock, TimeSpan? sourceTimeout = null)
    {
        _clock = clock;
        _sourceTimeout = sourceTimeout ?? DefaultSourceTimeout;
    }

    public static string BuildSourceKey(string handle)
    {
        return $"{SourceKeyPrefix}{handle}";
    }

    public void Ingest(LocalLoopbackPacket packet)
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            var now = _clock();
            RemoveExpiredSources(now);
            switch (packet.Type)
            {
                case LocalLoopbackPacketType.Register:
                    Register(packet, now);
                    break;
                case LocalLoopbackPacketType.Heartbeat:
                    _core.Heartbeat(packet.Handle, ToMilliseconds(now));
                    break;
                case LocalLoopbackPacketType.Rename:
                    _core.Rename(packet.Handle, NormalizeDisplayName(packet.DisplayName, packet.Handle), ToMilliseconds(now));
                    break;
                case LocalLoopbackPacketType.Audio:
                    ProcessAudio(packet, now);
                    break;
                case LocalLoopbackPacketType.Close:
                    _core.Close(packet.Handle);
                    break;
                case LocalLoopbackPacketType.Ack:
                    break;
            }
        }
    }

    public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
    {
        lock (_lock)
        {
            RemoveExpiredSources(_clock());
            return _core.GetSnapshots()
                .OrderBy(source => source.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(source => source.Handle, StringComparer.Ordinal)
                .Select(CreateSnapshot)
                .ToArray();
        }
    }

    public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
    {
        if (!TryGetHandle(sourceKey, out var handle))
        {
            return;
        }

        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.ResetSource(handle);
            }
        }
    }

    public void ResetAllSources()
    {
        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.ResetAll();
            }
        }
    }

    public void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling)
    {
        ValidateTruePeakOversampling(oversampling);
        if (!TryGetHandle(sourceKey, out var handle))
        {
            return;
        }

        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.SetTruePeakOversampling(handle, oversampling);
            }
        }
    }

    public void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
    {
        if (!TryGetHandle(sourceKey, out var handle))
        {
            return;
        }

        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.SetChannelWaveformEnabled(handle, enabled);
            }
        }
    }

    public void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
    {
        if (!TryGetHandle(sourceKey, out var handle))
        {
            return;
        }

        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.SetChannelSpectrumEnabled(handle, enabled);
            }
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        lock (_lock)
        {
            if (!_isDisposed)
            {
                _core.SetPerformanceMode(mode);
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _core.Dispose();
        }
    }

    private void Register(LocalLoopbackPacket packet, DateTimeOffset timestamp)
    {
        if (packet.SampleRate is not int sampleRate || packet.Channels is not int channels)
        {
            return;
        }

        _core.Register(
            packet.Handle,
            NormalizeDisplayName(packet.DisplayName, packet.Handle),
            (int)NormalizeSourceKind(packet.SourceKind),
            sampleRate,
            channels,
            ToMilliseconds(timestamp));
    }

    private void ProcessAudio(LocalLoopbackPacket packet, DateTimeOffset timestamp)
    {
        if (packet.Samples is null || packet.Samples.Count == 0)
        {
            return;
        }

        _core.Process(packet.Handle, packet.Samples, ToMilliseconds(timestamp));
    }

    private SignalSourceSnapshot CreateSnapshot(NativeSourceIngestCore.NativeSourceSnapshot source)
    {
        var metrics = source.Metrics;
        var sourceKind = ToSignalSourceKind(source.SourceKind);
        return new SignalSourceSnapshot(
            BuildSourceKey(source.Handle),
            sourceKind,
            null,
            source.DisplayName,
            source.Peak,
            PeakMath.ToDecibels(source.Peak),
            PeakMath.FormatDb(source.Peak),
            source.Peak > 0 ? "Active" : "Registered",
            metrics.MomentaryLufs,
            metrics.ShortTermLufs,
            metrics.IntegratedLufs,
            metrics.TruePeakDb,
            metrics.Rms100MsDb,
            metrics.StereoImageX,
            metrics.StereoImageY,
            metrics.StereoImagePoints,
            metrics.Correlation,
            metrics.WaveformSamples,
            metrics.ChannelWaveformSamples,
            metrics.SpectrumBuckets,
            metrics.ChannelSpectrumBuckets,
            metrics.PlotFrameVersion);
    }

    private void RemoveExpiredSources(DateTimeOffset now)
    {
        if (_sourceTimeout <= TimeSpan.Zero)
        {
            return;
        }

        _core.RemoveExpired(ToMilliseconds(now), (long)_sourceTimeout.TotalMilliseconds);
    }

    private static bool TryGetHandle(string sourceKey, out string handle)
    {
        handle = string.Empty;
        if (!sourceKey.StartsWith(SourceKeyPrefix, StringComparison.Ordinal) || sourceKey.Length == SourceKeyPrefix.Length)
        {
            return false;
        }

        handle = sourceKey[SourceKeyPrefix.Length..];
        return true;
    }

    private static SignalSourceKind NormalizeSourceKind(LocalLoopbackSourceKind sourceKind)
    {
        return sourceKind == LocalLoopbackSourceKind.Plugin ? SignalSourceKind.Plugin : SignalSourceKind.LocalLoopback;
    }

    private static SignalSourceKind ToSignalSourceKind(int sourceKind)
    {
        return Enum.IsDefined(typeof(SignalSourceKind), sourceKind)
            ? (SignalSourceKind)sourceKind
            : SignalSourceKind.Unknown;
    }

    private static string NormalizeDisplayName(string? displayName, string handle)
    {
        return string.IsNullOrWhiteSpace(displayName) ? handle : displayName.Trim();
    }

    private static void ValidateTruePeakOversampling(int oversampling)
    {
        if (oversampling is not (2 or 4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(oversampling), "True-peak oversampling must be 2x, 4x, or 8x.");
        }
    }

    private static long ToMilliseconds(DateTimeOffset timestamp)
    {
        return timestamp.ToUnixTimeMilliseconds();
    }
}
