using System.Globalization;

namespace DesktopLoudnessMeter.App.Controls;

internal enum SpectrumCursorLevelState
{
    /// <summary>No retained level is drawn at the hovered position.</summary>
    Unavailable,

    /// <summary>A finite level strictly inside the analyzer floor/ceiling range.</summary>
    Measured,

    /// <summary>The retained level sits at or below the analyzer -200 dBFS floor.</summary>
    AtFloor,

    /// <summary>The retained level sits at or above the analyzer 0 dBFS ceiling.</summary>
    AtCeiling
}

/// <summary>
/// One cursor readout: the displayed frequency, the retained source bucket and
/// history column it came from, and the level state of that retained sample.
/// The level comes from normalized source data (-200 dBFS floor, 0 dBFS ceiling)
/// before any display color transform.
/// </summary>
internal readonly record struct SpectrumCursorReadout(
    double FrequencyHz,
    int SourceBucket,
    int HistoryColumn,
    SpectrumCursorLevelState LevelState,
    double LevelDbfs)
{
    private const string UnavailableText = "—";

    public string FrequencyText => FormatFrequency(FrequencyHz);

    public string LevelText => LevelState switch
    {
        SpectrumCursorLevelState.Measured => string.Create(
            CultureInfo.InvariantCulture,
            $"{LevelDbfs:0.0} dBFS"),
        SpectrumCursorLevelState.AtFloor => string.Create(
            CultureInfo.InvariantCulture,
            $"≤ {SpectrumRollColumnRasterizer.AnalyzerFloorDb:0} dBFS"),
        SpectrumCursorLevelState.AtCeiling => string.Create(
            CultureInfo.InvariantCulture,
            $"≥ {SpectrumRollColumnRasterizer.AnalyzerCeilingDb:0} dBFS"),
        _ => UnavailableText
    };

    public static SpectrumCursorReadout Create(
        double frequencyHz,
        int sourceBucket,
        int historyColumn,
        bool hasRetainedLevel,
        double storedLevel)
    {
        if (!hasRetainedLevel || !double.IsFinite(storedLevel))
        {
            return new SpectrumCursorReadout(
                frequencyHz,
                sourceBucket,
                historyColumn,
                SpectrumCursorLevelState.Unavailable,
                double.NaN);
        }

        if (storedLevel <= 0)
        {
            return new SpectrumCursorReadout(
                frequencyHz,
                sourceBucket,
                historyColumn,
                SpectrumCursorLevelState.AtFloor,
                SpectrumRollColumnRasterizer.AnalyzerFloorDb);
        }

        if (storedLevel >= 1)
        {
            return new SpectrumCursorReadout(
                frequencyHz,
                sourceBucket,
                historyColumn,
                SpectrumCursorLevelState.AtCeiling,
                SpectrumRollColumnRasterizer.AnalyzerCeilingDb);
        }

        return new SpectrumCursorReadout(
            frequencyHz,
            sourceBucket,
            historyColumn,
            SpectrumCursorLevelState.Measured,
            SpectrumRollColumnRasterizer.AnalyzerFloorDb
                + (SpectrumRollColumnRasterizer.AnalyzerRangeDb * storedLevel));
    }

    private static string FormatFrequency(double frequency)
    {
        return frequency >= 1000
            ? string.Create(CultureInfo.InvariantCulture, $"{frequency / 1000:0.##} kHz")
            : string.Create(CultureInfo.InvariantCulture, $"{frequency:0} Hz");
    }
}
