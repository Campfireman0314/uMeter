namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal interface ISpectrumFft
{
    string Name { get; }

    void ComputeMagnitudes(
        double[] samples,
        double[] window,
        int fftSize,
        double windowSum,
        double[] magnitudes);
}
