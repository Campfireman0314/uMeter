namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class IntegratedLoudnessGate
{
    private const double AbsoluteGateLufs = -70;
    private const double RelativeGateLu = -10;
    private readonly int _blockFrames;
    private readonly int _hopFrames;
    private readonly Queue<double> _window;
    private readonly List<double> _blockPowers = [];
    private double _windowSum;
    private int _framesSinceHop;

    public IntegratedLoudnessGate(int blockFrames, int hopFrames)
    {
        if (blockFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockFrames), "Integrated loudness block size must be positive.");
        }

        if (hopFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopFrames), "Integrated loudness hop size must be positive.");
        }

        _blockFrames = blockFrames;
        _hopFrames = hopFrames;
        _window = new Queue<double>(blockFrames);
    }

    public void Add(double power)
    {
        _window.Enqueue(power);
        _windowSum += power;
        _framesSinceHop++;

        if (_window.Count > _blockFrames)
        {
            _windowSum -= _window.Dequeue();
        }

        if (_window.Count == _blockFrames && _framesSinceHop >= _hopFrames)
        {
            _blockPowers.Add(_windowSum / _blockFrames);
            _framesSinceHop = 0;
        }
    }

    public double? CalculateLufs()
    {
        var absoluteGatePower = PcmMetricAnalyzer.LufsToMeanSquare(AbsoluteGateLufs);
        var absoluteGatedSum = 0d;
        var absoluteGatedCount = 0;
        foreach (var power in _blockPowers)
        {
            if (power <= absoluteGatePower)
            {
                continue;
            }

            absoluteGatedSum += power;
            absoluteGatedCount++;
        }

        if (absoluteGatedCount == 0)
        {
            return null;
        }

        var preliminaryMean = absoluteGatedSum / absoluteGatedCount;
        var relativeGate = PcmMetricAnalyzer.MeanSquareToLufs(preliminaryMean) + RelativeGateLu;
        var relativeGatePower = PcmMetricAnalyzer.LufsToMeanSquare(relativeGate);
        var gatedSum = 0d;
        var gatedCount = 0;
        foreach (var power in _blockPowers)
        {
            if (power <= absoluteGatePower || power <= relativeGatePower)
            {
                continue;
            }

            gatedSum += power;
            gatedCount++;
        }

        return gatedCount == 0
            ? null
            : PcmMetricAnalyzer.MeanSquareToLufs(gatedSum / gatedCount);
    }

    public void Reset()
    {
        _window.Clear();
        _blockPowers.Clear();
        _windowSum = 0;
        _framesSinceHop = 0;
    }
}
