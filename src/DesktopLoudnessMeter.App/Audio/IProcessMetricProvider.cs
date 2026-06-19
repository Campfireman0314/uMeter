using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Audio;

public interface IProcessMetricProvider : IDisposable
{
    void TrackProcesses(IReadOnlyCollection<int> processIds);

    PcmMetricsSnapshot? GetMetricsForProcess(int processId);

    void ResetProcess(int processId);

    void ResetAll();

    void SetTruePeakOversampling(int processId, int oversampling);

    void SetChannelWaveformEnabled(int processId, bool enabled);

    void SetChannelSpectrumEnabled(int processId, bool enabled);

    void SetPerformanceMode(PcmAnalysisPerformanceMode mode);
}
