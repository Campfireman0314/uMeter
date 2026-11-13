namespace DesktopLoudnessMeter.App.Audio.Analysis;

/// <summary>
/// ITU-R BS.1770-4 K-weighting: a high-shelf pre-filter followed by the RLB high-pass.
///
/// BS.1770-4 prints a single coefficient set, valid for 48 kHz. That set is a bilinear
/// transform of a rate-independent analog prototype, so the prototype (not the printed
/// integers) is what defines the weighting curve. Evaluating the prototype at the configured
/// input rate keeps the calibration that the published 48 kHz set establishes; running the
/// fixed 48 kHz coefficients at another rate instead shifts the whole curve in frequency.
/// The derived 48 kHz coefficients reproduce the published set to within double rounding.
/// </summary>
internal sealed class KWeightingFilter
{
    // ITU-R BS.1770-4 analog prototype parameters that the published 48 kHz coefficient
    // set was derived from: shelf centre frequency, shelf gain, shelf Q, then the RLB
    // high-pass corner frequency and Q.
    private const double ShelfFrequencyHz = 1681.974450955533;
    private const double ShelfGainDb = 3.999843853973347;
    private const double ShelfQ = 0.7071752369554196;
    private const double ShelfMidGainExponent = 0.4996667741545416;
    private const double HighPassFrequencyHz = 38.13547087602444;
    private const double HighPassQ = 0.5003270373238773;

    // Keeps the prewarped design frequency finite and below Nyquist for very low but still
    // positive sample rates, so the bilinear transform always yields a stable biquad.
    private const double MaximumNormalizedDesignFrequency = 0.49;

    private readonly BiquadFilter _preFilter;
    private readonly BiquadFilter _rlbFilter;

    public KWeightingFilter(int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        }

        _preFilter = new BiquadFilter(DesignShelf(sampleRate));
        _rlbFilter = new BiquadFilter(DesignHighPass(sampleRate));
    }

    internal BiquadCoefficients PreFilterCoefficients => _preFilter.Coefficients;

    internal BiquadCoefficients HighPassFilterCoefficients => _rlbFilter.Coefficients;

    public double Process(double sample)
    {
        return _rlbFilter.Process(_preFilter.Process(sample));
    }

    public void Reset()
    {
        _preFilter.Reset();
        _rlbFilter.Reset();
    }

    private static BiquadCoefficients DesignShelf(int sampleRate)
    {
        var k = PrewarpedK(ShelfFrequencyHz, sampleRate);
        var highGain = Math.Pow(10, ShelfGainDb / 20);
        var midGain = Math.Pow(highGain, ShelfMidGainExponent);
        var normalization = 1 + (k / ShelfQ) + (k * k);

        return new BiquadCoefficients(
            B0: (highGain + (midGain * k / ShelfQ) + (k * k)) / normalization,
            B1: 2 * ((k * k) - highGain) / normalization,
            B2: (highGain - (midGain * k / ShelfQ) + (k * k)) / normalization,
            A1: 2 * ((k * k) - 1) / normalization,
            A2: (1 - (k / ShelfQ) + (k * k)) / normalization);
    }

    private static BiquadCoefficients DesignHighPass(int sampleRate)
    {
        var k = PrewarpedK(HighPassFrequencyHz, sampleRate);
        var normalization = 1 + (k / HighPassQ) + (k * k);

        return new BiquadCoefficients(
            B0: 1,
            B1: -2,
            B2: 1,
            A1: 2 * ((k * k) - 1) / normalization,
            A2: (1 - (k / HighPassQ) + (k * k)) / normalization);
    }

    private static double PrewarpedK(double frequencyHz, int sampleRate)
    {
        var normalizedFrequency = Math.Min(frequencyHz / sampleRate, MaximumNormalizedDesignFrequency);
        return Math.Tan(Math.PI * normalizedFrequency);
    }

    internal readonly record struct BiquadCoefficients(double B0, double B1, double B2, double A1, double A2);

    private sealed class BiquadFilter
    {
        private double _x1;
        private double _x2;
        private double _y1;
        private double _y2;

        public BiquadFilter(BiquadCoefficients coefficients)
        {
            Coefficients = coefficients;
        }

        public BiquadCoefficients Coefficients { get; }

        public double Process(double input)
        {
            var coefficients = Coefficients;
            var output = (coefficients.B0 * input)
                + (coefficients.B1 * _x1)
                + (coefficients.B2 * _x2)
                - (coefficients.A1 * _y1)
                - (coefficients.A2 * _y2);
            _x2 = _x1;
            _x1 = input;
            _y2 = _y1;
            _y1 = output;
            return output;
        }

        public void Reset()
        {
            _x1 = 0;
            _x2 = 0;
            _y1 = 0;
            _y2 = 0;
        }
    }
}
