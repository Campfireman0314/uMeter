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
    private readonly Dictionary<string, SourceState> _sourcesByHandle = [];
    private readonly Dictionary<string, int> _truePeakOversamplingByHandle = [];
    private readonly Dictionary<string, bool> _channelWaveformEnabledByHandle = [];
    private readonly Dictionary<string, bool> _channelSpectrumEnabledByHandle = [];
    private PcmAnalysisPerformanceMode _performanceMode = PcmAnalysisPerformanceMode.Normal;
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

            RemoveExpiredSources(_clock());
            switch (packet.Type)
            {
                case LocalLoopbackPacketType.Register:
                    Register(packet);
                    break;
                case LocalLoopbackPacketType.Heartbeat:
                    Touch(packet.Handle);
                    break;
                case LocalLoopbackPacketType.Rename:
                    Rename(packet);
                    break;
                case LocalLoopbackPacketType.Audio:
                    ProcessAudio(packet);
                    break;
                case LocalLoopbackPacketType.Close:
                    _sourcesByHandle.Remove(packet.Handle);
                    break;
            }
        }
    }

    public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
    {
        lock (_lock)
        {
            RemoveExpiredSources(_clock());
            return _sourcesByHandle.Values
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
            if (_sourcesByHandle.TryGetValue(handle, out var source))
            {
                source.Reset();
            }
        }
    }

    public void ResetAllSources()
    {
        lock (_lock)
        {
            foreach (var source in _sourcesByHandle.Values)
            {
                source.Reset();
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
            _truePeakOversamplingByHandle[handle] = oversampling;
            if (_sourcesByHandle.TryGetValue(handle, out var source))
            {
                source.SetTruePeakOversampling(oversampling);
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
            _channelWaveformEnabledByHandle[handle] = enabled;
            if (_sourcesByHandle.TryGetValue(handle, out var source))
            {
                source.SetChannelWaveformEnabled(enabled);
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
            _channelSpectrumEnabledByHandle[handle] = enabled;
            if (_sourcesByHandle.TryGetValue(handle, out var source))
            {
                source.SetChannelSpectrumEnabled(enabled);
            }
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
            foreach (var source in _sourcesByHandle.Values)
            {
                source.SetPerformanceMode(mode);
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _isDisposed = true;
            _sourcesByHandle.Clear();
        }
    }

    private void Register(LocalLoopbackPacket packet)
    {
        if (packet.SampleRate is not int sampleRate || packet.Channels is not int channels)
        {
            return;
        }

        if (_sourcesByHandle.TryGetValue(packet.Handle, out var source)
            && source.SampleRate == sampleRate
            && source.Channels == channels)
        {
            source.DisplayName = NormalizeDisplayName(packet.DisplayName, packet.Handle);
            source.SourceKind = NormalizeSourceKind(packet.SourceKind);
            source.Touch(_clock());
            return;
        }

        _sourcesByHandle[packet.Handle] = new SourceState(
            packet.Handle,
            NormalizeDisplayName(packet.DisplayName, packet.Handle),
            NormalizeSourceKind(packet.SourceKind),
            sampleRate,
            channels,
            GetTruePeakOversampling(packet.Handle),
            GetChannelWaveformEnabled(packet.Handle),
            GetChannelSpectrumEnabled(packet.Handle),
            _performanceMode,
            _clock());
    }

    private void Touch(string handle)
    {
        if (_sourcesByHandle.TryGetValue(handle, out var source))
        {
            source.Touch(_clock());
        }
    }

    private void Rename(LocalLoopbackPacket packet)
    {
        if (_sourcesByHandle.TryGetValue(packet.Handle, out var source))
        {
            source.DisplayName = NormalizeDisplayName(packet.DisplayName, packet.Handle);
            source.Touch(_clock());
        }
    }

    private void ProcessAudio(LocalLoopbackPacket packet)
    {
        if (!_sourcesByHandle.TryGetValue(packet.Handle, out var source)
            || packet.Samples is null
            || packet.Samples.Count == 0
            || packet.Samples.Count % source.Channels != 0)
        {
            return;
        }

        source.Process(packet.Samples);
        source.Touch(_clock());
    }

    private SignalSourceSnapshot CreateSnapshot(SourceState source)
    {
        var metrics = source.Snapshot;
        return new SignalSourceSnapshot(
            BuildSourceKey(source.Handle),
            source.SourceKind,
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

        foreach (var source in _sourcesByHandle.Values.Where(source => now - source.LastSeenAt > _sourceTimeout).ToArray())
        {
            _sourcesByHandle.Remove(source.Handle);
        }
    }

    private int GetTruePeakOversampling(string handle)
    {
        return _truePeakOversamplingByHandle.GetValueOrDefault(handle, 4);
    }

    private bool GetChannelWaveformEnabled(string handle)
    {
        return _channelWaveformEnabledByHandle.GetValueOrDefault(handle);
    }

    private bool GetChannelSpectrumEnabled(string handle)
    {
        return _channelSpectrumEnabledByHandle.GetValueOrDefault(handle);
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

    private sealed class SourceState
    {
        private PcmMetricAnalyzer _analyzer;
        private int _truePeakOversampling;
        private bool _channelWaveformEnabled;
        private bool _channelSpectrumEnabled;
        private PcmAnalysisPerformanceMode _performanceMode;

        public SourceState(
            string handle,
            string displayName,
            SignalSourceKind sourceKind,
            int sampleRate,
            int channels,
            int truePeakOversampling,
            bool channelWaveformEnabled,
            bool channelSpectrumEnabled,
            PcmAnalysisPerformanceMode performanceMode,
            DateTimeOffset lastSeenAt)
        {
            Handle = handle;
            DisplayName = displayName;
            SourceKind = sourceKind;
            SampleRate = sampleRate;
            Channels = channels;
            _truePeakOversampling = truePeakOversampling;
            _channelWaveformEnabled = channelWaveformEnabled;
            _channelSpectrumEnabled = channelSpectrumEnabled;
            _performanceMode = performanceMode;
            _analyzer = CreateAnalyzer(sampleRate, channels, truePeakOversampling, channelWaveformEnabled, channelSpectrumEnabled, performanceMode);
            LastSeenAt = lastSeenAt;
        }

        public string Handle { get; }

        public string DisplayName { get; set; }

        public SignalSourceKind SourceKind { get; set; }

        public int SampleRate { get; }

        public int Channels { get; }

        public DateTimeOffset LastSeenAt { get; private set; }

        public float Peak { get; private set; }

        public PcmMetricsSnapshot Snapshot => _analyzer.Snapshot;

        public void Touch(DateTimeOffset timestamp)
        {
            LastSeenAt = timestamp;
        }

        public void Process(IReadOnlyList<float> samples)
        {
            var sampleArray = samples as float[] ?? samples.ToArray();
            Peak = CalculatePeak(sampleArray);
            _analyzer.Process(sampleArray);
        }

        public void Reset()
        {
            Peak = 0;
            _analyzer.Reset();
        }

        public void SetTruePeakOversampling(int oversampling)
        {
            if (_truePeakOversampling == oversampling)
            {
                return;
            }

            _truePeakOversampling = oversampling;
            Peak = 0;
            _analyzer = CreateAnalyzer(SampleRate, Channels, oversampling, _channelWaveformEnabled, _channelSpectrumEnabled, _performanceMode);
        }

        public void SetChannelWaveformEnabled(bool enabled)
        {
            _channelWaveformEnabled = enabled;
            _analyzer.SetChannelWaveformEnabled(enabled);
        }

        public void SetChannelSpectrumEnabled(bool enabled)
        {
            _channelSpectrumEnabled = enabled;
            _analyzer.SetChannelSpectrumEnabled(enabled);
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            if (_performanceMode == mode)
            {
                return;
            }

            _performanceMode = mode;
            _analyzer.SetPerformanceMode(mode);
        }

        private static PcmMetricAnalyzer CreateAnalyzer(
            int sampleRate,
            int channels,
            int truePeakOversampling,
            bool channelWaveformEnabled,
            bool channelSpectrumEnabled,
            PcmAnalysisPerformanceMode performanceMode)
        {
            var analyzer = new PcmMetricAnalyzer(new PcmMetricAnalyzerOptions(sampleRate, channels, truePeakOversampling));
            analyzer.SetChannelWaveformEnabled(channelWaveformEnabled);
            analyzer.SetChannelSpectrumEnabled(channelSpectrumEnabled);
            analyzer.SetPerformanceMode(performanceMode);
            return analyzer;
        }

        private static float CalculatePeak(IReadOnlyList<float> samples)
        {
            var peak = 0f;
            foreach (var sample in samples)
            {
                if (!float.IsFinite(sample))
                {
                    continue;
                }

                peak = Math.Max(peak, Math.Abs(sample));
            }

            return PeakMath.ClampPeak(peak);
        }
    }
}
