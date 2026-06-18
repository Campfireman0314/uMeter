namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal static class SpectrumFftProvider
{
    public static ISpectrumFft Create()
    {
        return new NativeRfRapidSpectrumFft();
    }
}
