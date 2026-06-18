namespace DesktopLoudnessMeter.App.Audio;

public interface ISignalSourceMeterService
{
    IReadOnlyList<SignalSourceSnapshot> GetCurrentSources();
}

public interface ISignalSourceResetService
{
    void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId);

    void ResetAllSources();
}

public interface ISignalSourceAnalysisSettingsService
{
    void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling);

    void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled);

    void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled);
}
