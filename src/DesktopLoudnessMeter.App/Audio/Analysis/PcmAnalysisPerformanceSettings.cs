namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed record PcmAnalysisPerformanceSettings(
    PcmAnalysisPerformanceMode Mode,
    int SpectrumRefreshIntervalFrames,
    int SpectrumAnalysisSamples,
    int WaveformRefreshIntervalFrames,
    int StereoImageRefreshIntervalFrames = 1024)
{
    public static PcmAnalysisPerformanceSettings Normal { get; } = new(
        PcmAnalysisPerformanceMode.Normal,
        SpectrumRefreshIntervalFrames: 1024,
        SpectrumAnalysisSamples: 16384,
        WaveformRefreshIntervalFrames: 2048,
        StereoImageRefreshIntervalFrames: 1024);

    public static PcmAnalysisPerformanceSettings Eco { get; } = new(
        PcmAnalysisPerformanceMode.Eco,
        SpectrumRefreshIntervalFrames: 4096,
        SpectrumAnalysisSamples: 8192,
        WaveformRefreshIntervalFrames: 8192,
        StereoImageRefreshIntervalFrames: 4096);
}
