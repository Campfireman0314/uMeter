namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed record SignalLevelMetrics(
    double? MomentaryLufs = null,
    double? ShortTermLufs = null,
    double? IntegratedLufs = null,
    double? TruePeakDb = null,
    double? Rms100MsDb = null);
