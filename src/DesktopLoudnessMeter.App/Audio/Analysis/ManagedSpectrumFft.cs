namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class ManagedSpectrumFft : ISpectrumFft
{
    private ComplexSample[] _fftBuffer = [];

    public string Name => "Managed radix-2 FFT";

    public void ComputeMagnitudes(
        double[] samples,
        double[] window,
        int fftSize,
        double windowSum,
        double[] magnitudes)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(magnitudes);

        if (fftSize <= 0 || samples.Length < fftSize || window.Length < fftSize)
        {
            throw new ArgumentOutOfRangeException(nameof(fftSize), "FFT input buffers must contain fftSize samples.");
        }

        var magnitudeCount = (fftSize / 2) + 1;
        if (magnitudes.Length < magnitudeCount)
        {
            throw new ArgumentException("Magnitude buffer is too small for the requested FFT size.", nameof(magnitudes));
        }

        EnsureFftBuffer(fftSize);
        for (var index = 0; index < fftSize; index++)
        {
            _fftBuffer[index] = new ComplexSample(samples[index] * window[index], 0);
        }

        FastFourierTransform(_fftBuffer, fftSize);

        var scale = 2 / Math.Max(1, windowSum);
        for (var bin = 0; bin < magnitudeCount; bin++)
        {
            var complex = _fftBuffer[bin];
            magnitudes[bin] = scale * Math.Sqrt((complex.Real * complex.Real) + (complex.Imaginary * complex.Imaginary));
        }
    }

    private void EnsureFftBuffer(int fftSize)
    {
        if (_fftBuffer.Length == fftSize)
        {
            return;
        }

        _fftBuffer = new ComplexSample[fftSize];
    }

    private static void FastFourierTransform(ComplexSample[] samples, int count)
    {
        for (int index = 1, reverse = 0; index < count; index++)
        {
            var bit = count >> 1;
            for (; (reverse & bit) != 0; bit >>= 1)
            {
                reverse ^= bit;
            }

            reverse ^= bit;
            if (index < reverse)
            {
                (samples[index], samples[reverse]) = (samples[reverse], samples[index]);
            }
        }

        for (var length = 2; length <= count; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var unitReal = Math.Cos(angle);
            var unitImaginary = Math.Sin(angle);

            for (var start = 0; start < count; start += length)
            {
                var real = 1d;
                var imaginary = 0d;
                var halfLength = length / 2;
                for (var offset = 0; offset < halfLength; offset++)
                {
                    var even = samples[start + offset];
                    var odd = samples[start + offset + halfLength];
                    var oddReal = (odd.Real * real) - (odd.Imaginary * imaginary);
                    var oddImaginary = (odd.Real * imaginary) + (odd.Imaginary * real);

                    samples[start + offset] = new ComplexSample(even.Real + oddReal, even.Imaginary + oddImaginary);
                    samples[start + offset + halfLength] = new ComplexSample(even.Real - oddReal, even.Imaginary - oddImaginary);

                    var nextReal = (real * unitReal) - (imaginary * unitImaginary);
                    imaginary = (real * unitImaginary) + (imaginary * unitReal);
                    real = nextReal;
                }
            }
        }
    }

    private readonly record struct ComplexSample(double Real, double Imaginary);
}
