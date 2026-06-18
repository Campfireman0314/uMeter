namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class KWeightingFilter
{
    private readonly BiquadFilter _preFilter = new(
        b0: 1.53512485958697,
        b1: -2.69169618940638,
        b2: 1.19839281085285,
        a1: -1.69065929318241,
        a2: 0.73248077421585);

    private readonly BiquadFilter _rlbFilter = new(
        b0: 1,
        b1: -2,
        b2: 1,
        a1: -1.99004745483398,
        a2: 0.99007225036621);

    public double Process(double sample)
    {
        return _rlbFilter.Process(_preFilter.Process(sample));
    }

    public void Reset()
    {
        _preFilter.Reset();
        _rlbFilter.Reset();
    }

    private sealed class BiquadFilter
    {
        private readonly double _b0;
        private readonly double _b1;
        private readonly double _b2;
        private readonly double _a1;
        private readonly double _a2;
        private double _x1;
        private double _x2;
        private double _y1;
        private double _y2;

        public BiquadFilter(double b0, double b1, double b2, double a1, double a2)
        {
            _b0 = b0;
            _b1 = b1;
            _b2 = b2;
            _a1 = a1;
            _a2 = a2;
        }

        public double Process(double input)
        {
            var output = (_b0 * input) + (_b1 * _x1) + (_b2 * _x2) - (_a1 * _y1) - (_a2 * _y2);
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
