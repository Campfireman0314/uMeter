namespace DesktopLoudnessMeter.App.Audio;

public static class PeakMath
{
    public static float ClampPeak(float peak)
    {
        if (float.IsNaN(peak) || peak <= 0)
        {
            return 0;
        }

        return peak >= 1 ? 1 : peak;
    }

    public static double ToDecibels(float peak)
    {
        var clamped = ClampPeak(peak);
        if (clamped <= 0)
        {
            return double.NegativeInfinity;
        }

        return 20.0 * Math.Log10(clamped);
    }

    public static string FormatDb(float peak)
    {
        var db = ToDecibels(peak);
        return double.IsNegativeInfinity(db) ? "-inf dB" : $"{db:0.0} dB";
    }
}
