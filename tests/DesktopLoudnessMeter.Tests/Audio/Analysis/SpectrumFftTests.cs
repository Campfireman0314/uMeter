using DesktopLoudnessMeter.App.Audio.Analysis;
using System.Runtime.InteropServices;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

public sealed class SpectrumFftTests
{
    [Fact]
    public void ManagedFftFindsDominantSineBin()
    {
        var magnitudes = ComputeBinMagnitudes(new ManagedSpectrumFft());

        AssertDominantBin(magnitudes, expectedBin: 8);
        Assert.InRange(magnitudes[8], 0.99, 1.01);
    }

    [Fact]
    public void NativeProviderComputesMagnitudesWithFallbackAvailable()
    {
        var magnitudes = ComputeBinMagnitudes(new NativeRfRapidSpectrumFft());

        AssertDominantBin(magnitudes, expectedBin: 8);
        Assert.InRange(magnitudes[8], 0.99, 1.01);
    }

    [Fact]
    public void NativeDllExportsRfRapidEntrypointsWhenCopied()
    {
        var nativeDll = Path.Combine(AppContext.BaseDirectory, "DesktopLoudnessMeter.Native.dll");
        if (!File.Exists(nativeDll))
        {
            return;
        }

        Assert.True(NativeLibrary.TryLoad(nativeDll, out var handle));
        try
        {
            Assert.True(NativeLibrary.TryGetExport(handle, "DlmRfRapidInit", out _));
            Assert.True(NativeLibrary.TryGetExport(handle, "DlmRfRapidSpectrumF64", out _));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    private static double[] ComputeBinMagnitudes(ISpectrumFft fft)
    {
        const int fftSize = 1024;
        const int sineBin = 8;
        var samples = new double[fftSize];
        var window = new double[fftSize];
        var magnitudes = new double[(fftSize / 2) + 1];

        for (var index = 0; index < fftSize; index++)
        {
            samples[index] = Math.Sin(2 * Math.PI * sineBin * index / fftSize);
            window[index] = 1;
        }

        fft.ComputeMagnitudes(samples, window, fftSize, windowSum: fftSize, magnitudes);
        return magnitudes;
    }

    private static void AssertDominantBin(double[] magnitudes, int expectedBin)
    {
        var dominantBin = 0;
        for (var index = 1; index < magnitudes.Length; index++)
        {
            if (magnitudes[index] > magnitudes[dominantBin])
            {
                dominantBin = index;
            }
        }

        Assert.Equal(expectedBin, dominantBin);
    }
}
