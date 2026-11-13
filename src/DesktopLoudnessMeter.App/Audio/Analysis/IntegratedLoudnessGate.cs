namespace DesktopLoudnessMeter.App.Audio.Analysis;

/// <summary>
/// BS.1770 integrated-loudness gating over completed 400 ms blocks. The exact two-pass result is cached
/// between completed blocks, and the second pass is answered from an exact-value aggregate index instead
/// of walking every retained block: the absolute-gated sum and count are accumulated chronologically as
/// blocks complete (so the preliminary mean, and therefore the relative gate, is bit-identical to the
/// original scan), while the relative-gated sum and count come from the index. No level is quantized and
/// no history is truncated.
/// </summary>
internal sealed class IntegratedLoudnessGate
{
    private const double AbsoluteGateLufs = -70;
    private const double RelativeGateLu = -10;
    private static readonly double AbsoluteGatePower = PcmMetricAnalyzer.LufsToMeanSquare(AbsoluteGateLufs);
    private readonly SlidingPowerWindow _window;
    private readonly PowerAggregateIndex _index = new();
    private readonly int _hopFrames;
    private int _framesSinceHop;
    private double _absoluteGatedSum;
    private int _absoluteGatedCount;
    private long _blockVersion;
    private long _cachedVersion = -1;
    private double? _cachedLufs;
    private int _lastEvaluationNodeVisits;

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

        _window = new SlidingPowerWindow(blockFrames);
        _hopFrames = hopFrames;
        _framesSinceHop = 0;
    }

    public void Add(double power)
    {
        _window.Add(power);
        _framesSinceHop++;

        if (_window.IsFull && _framesSinceHop >= _hopFrames)
        {
            var blockPower = _window.Mean;
            _index.Add(blockPower);
            if (blockPower > AbsoluteGatePower)
            {
                _absoluteGatedSum += blockPower;
                _absoluteGatedCount++;
            }

            _framesSinceHop = 0;
            _blockVersion++;
        }
    }

    /// <summary>
    /// Number of times the BS.1770 gating pass actually ran. Test instrumentation only: production code
    /// never reads it, and it is the observable that proves repeated queries reuse a cached answer.
    /// </summary>
    internal int EvaluationCount { get; private set; }

    /// <summary>
    /// Number of retained-index nodes the last evaluation touched. Test instrumentation only: production
    /// code never reads it, and it is the observable that proves a completed block no longer rescans the
    /// whole retained history.
    /// </summary>
    internal int LastEvaluationNodeVisits => _lastEvaluationNodeVisits;

    public double? CalculateLufs()
    {
        // The retained block set only changes when a block completes or the gate is reset, so the exact
        // two-pass result is reused verbatim in between. A cached "unavailable" result is cached too.
        if (_cachedVersion == _blockVersion)
        {
            return _cachedLufs;
        }

        var lufs = Evaluate();
        _cachedLufs = lufs;
        _cachedVersion = _blockVersion;
        return lufs;
    }

    private double? Evaluate()
    {
        EvaluationCount++;
        if (_absoluteGatedCount == 0)
        {
            _lastEvaluationNodeVisits = 0;
            return null;
        }

        var preliminaryMean = _absoluteGatedSum / _absoluteGatedCount;
        var relativeGate = PcmMetricAnalyzer.MeanSquareToLufs(preliminaryMean) + RelativeGateLu;
        var relativeGatePower = PcmMetricAnalyzer.LufsToMeanSquare(relativeGate);
        var threshold = relativeGatePower > AbsoluteGatePower ? relativeGatePower : AbsoluteGatePower;

        _index.SumAbove(threshold, out var gatedSum, out var gatedCount);
        _lastEvaluationNodeVisits = _index.LastQueryNodeVisits;

        return gatedCount == 0
            ? null
            : PcmMetricAnalyzer.MeanSquareToLufs(gatedSum / gatedCount);
    }

    public void Reset()
    {
        _window.Reset();
        _index.Reset();
        _absoluteGatedSum = 0;
        _absoluteGatedCount = 0;
        _framesSinceHop = 0;
        _blockVersion++;
        _cachedLufs = null;
        _cachedVersion = -1;
        _lastEvaluationNodeVisits = 0;
    }
}
