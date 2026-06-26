namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed record PcmAnalysisPerformanceSettings(
    PcmAnalysisPerformanceMode Mode,
    int SpectrumRefreshIntervalFrames,
    int SpectrumAnalysisSamples,
    int WaveformRefreshIntervalFrames)
{
    public static PcmAnalysisPerformanceSettings Normal { get; } = new(
        PcmAnalysisPerformanceMode.Normal,
        SpectrumRefreshIntervalFrames: 1024,
        SpectrumAnalysisSamples: 16384,
        WaveformRefreshIntervalFrames: 2048);

    public static PcmAnalysisPerformanceSettings Eco { get; } = new(
        PcmAnalysisPerformanceMode.Eco,
        SpectrumRefreshIntervalFrames: 4096,
        SpectrumAnalysisSamples: 8192,
        WaveformRefreshIntervalFrames: 8192);
}
