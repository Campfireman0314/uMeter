namespace DesktopLoudnessMeter.App.Audio;

public interface IAudioSessionMeterService
    : ISignalSourceMeterService
{
    IReadOnlyList<AudioSessionSnapshot> GetCurrentSessions();

    IReadOnlyList<SignalSourceSnapshot> ISignalSourceMeterService.GetCurrentSources()
    {
        return GetCurrentSessions();
    }
}

public interface IAudioSessionResetService
    : ISignalSourceResetService
{
    void ResetSession(string sessionKey, int? processId);

    void ResetAllSessions();

    void ISignalSourceResetService.ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
    {
        ResetSession(sourceKey, processId);
    }

    void ISignalSourceResetService.ResetAllSources()
    {
        ResetAllSessions();
    }
}

public interface IAudioSessionAnalysisSettingsService
    : ISignalSourceAnalysisSettingsService
{
    void SetTruePeakOversampling(string sessionKey, int? processId, int oversampling);

    void SetChannelWaveformEnabled(string sessionKey, int? processId, bool enabled);

    void SetChannelSpectrumEnabled(string sessionKey, int? processId, bool enabled);

    void ISignalSourceAnalysisSettingsService.SetTruePeakOversampling(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        int oversampling)
    {
        SetTruePeakOversampling(sourceKey, processId, oversampling);
    }

    void ISignalSourceAnalysisSettingsService.SetChannelWaveformEnabled(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        bool enabled)
    {
        SetChannelWaveformEnabled(sourceKey, processId, enabled);
    }

    void ISignalSourceAnalysisSettingsService.SetChannelSpectrumEnabled(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        bool enabled)
    {
        SetChannelSpectrumEnabled(sourceKey, processId, enabled);
    }
}
