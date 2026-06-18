namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal static class SpectrumScale
{
    public const int BucketCount = 320;
    public const double MinimumFrequencyHz = 20;
    public const double MaximumFrequencyHz = 22000;

    public static double HertzToMel(double hertz)
    {
        return 2595 * Math.Log10(1 + (hertz / 700));
    }

    public static double MelToHertz(double mel)
    {
        return 700 * (Math.Pow(10, mel / 2595) - 1);
    }
}
