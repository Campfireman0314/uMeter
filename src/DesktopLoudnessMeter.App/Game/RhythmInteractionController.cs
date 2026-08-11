namespace DesktopLoudnessMeter.App.Game;

public enum RhythmTimingGrade
{
    None,
    Good,
    Perfect
}

public readonly record struct RhythmInteractionFrame(
    double Groove,
    int PhraseStep,
    double TempoBpm,
    double TempoConfidence,
    double PredictedBeatPhase,
    RhythmTimingGrade TimingGrade);

public sealed class RhythmInteractionController
{
    internal const double TimingWindowSeconds = 0.14;
    internal const double PerfectWindowSeconds = 0.065;
    private const double TimingFeedbackSeconds = 0.65;
    private const double GrooveDecayPerSecond = 0.025;
    private readonly Queue<double> _pendingActionTimes = new();
    private readonly Queue<double> _beatIntervals = new();
    private double _lastUpdateTime = double.NaN;
    private double _lastBeatTime = double.NaN;
    private double _timingFeedbackRemaining;
    private bool _currentBeatConsumed;
    private double _groove;
    private int _phraseStep;
    private double _tempoBpm;
    private double _tempoConfidence;
    private RhythmTimingGrade _timingGrade;

    public RhythmInteractionFrame Update(double timestampSeconds, bool isBeat)
    {
        timestampSeconds = NormalizeTimestamp(timestampSeconds);
        AdvanceTime(timestampSeconds);
        if (isBeat)
        {
            RegisterBeat(timestampSeconds);
        }
        else
        {
            RemoveExpiredActions(timestampSeconds);
        }

        return CurrentFrame;
    }

    public RhythmInteractionFrame RegisterAction(double timestampSeconds)
    {
        timestampSeconds = NormalizeTimestamp(timestampSeconds);
        AdvanceTime(timestampSeconds);

        if (!_currentBeatConsumed
            && double.IsFinite(_lastBeatTime)
            && timestampSeconds - _lastBeatTime is >= 0 and <= TimingWindowSeconds)
        {
            ScoreAction(timestampSeconds - _lastBeatTime);
            _currentBeatConsumed = true;
        }
        else
        {
            _pendingActionTimes.Enqueue(timestampSeconds);
            while (_pendingActionTimes.Count > 8)
            {
                _pendingActionTimes.Dequeue();
            }
        }

        return CurrentFrame;
    }

    public void Reset()
    {
        _pendingActionTimes.Clear();
        _beatIntervals.Clear();
        _lastUpdateTime = double.NaN;
        _lastBeatTime = double.NaN;
        _timingFeedbackRemaining = 0;
        _currentBeatConsumed = false;
        _groove = 0;
        _phraseStep = 0;
        _tempoBpm = 0;
        _tempoConfidence = 0;
        _timingGrade = RhythmTimingGrade.None;
    }

    private RhythmInteractionFrame CurrentFrame => new(
        Math.Clamp(_groove, 0, 1),
        _phraseStep,
        _tempoBpm,
        Math.Clamp(_tempoConfidence, 0, 1),
        CalculatePredictedBeatPhase(),
        _timingFeedbackRemaining > 0 ? _timingGrade : RhythmTimingGrade.None);

    private double CalculatePredictedBeatPhase()
    {
        if (_tempoConfidence < 0.72
            || _tempoBpm <= 0
            || !double.IsFinite(_lastBeatTime)
            || !double.IsFinite(_lastUpdateTime))
        {
            return 0;
        }

        var interval = 60 / _tempoBpm;
        var elapsed = Math.Max(0, _lastUpdateTime - _lastBeatTime);
        return Math.Clamp((elapsed / interval) % 1, 0, 1);
    }

    private void RegisterBeat(double timestampSeconds)
    {
        if (double.IsFinite(_lastBeatTime))
        {
            var interval = timestampSeconds - _lastBeatTime;
            if (interval is >= 0.25 and <= 1.5)
            {
                _beatIntervals.Enqueue(interval);
                while (_beatIntervals.Count > 8)
                {
                    _beatIntervals.Dequeue();
                }

                UpdateTempoEstimate();
            }
        }

        _lastBeatTime = timestampSeconds;
        _currentBeatConsumed = false;
        _phraseStep = (_phraseStep % 4) + 1;

        var candidate = double.NaN;
        while (_pendingActionTimes.Count > 0)
        {
            var actionTime = _pendingActionTimes.Dequeue();
            var distance = timestampSeconds - actionTime;
            if (distance is >= 0 and <= TimingWindowSeconds)
            {
                candidate = actionTime;
            }
        }

        if (double.IsFinite(candidate))
        {
            ScoreAction(timestampSeconds - candidate);
            _currentBeatConsumed = true;
        }
    }

    private void ScoreAction(double distanceSeconds)
    {
        _timingGrade = distanceSeconds <= PerfectWindowSeconds
            ? RhythmTimingGrade.Perfect
            : RhythmTimingGrade.Good;
        _timingFeedbackRemaining = TimingFeedbackSeconds;
        _groove = Math.Clamp(_groove + (_timingGrade == RhythmTimingGrade.Perfect ? 0.2 : 0.12), 0, 1);
    }

    private void UpdateTempoEstimate()
    {
        if (_beatIntervals.Count < 3)
        {
            _tempoBpm = 0;
            _tempoConfidence = 0;
            return;
        }

        var intervals = _beatIntervals.OrderBy(value => value).ToArray();
        var median = Median(intervals);
        var deviations = intervals
            .Select(value => Math.Abs(value - median))
            .OrderBy(value => value)
            .ToArray();
        var medianDeviation = Median(deviations);
        var consistency = 1 - Math.Clamp(medianDeviation / Math.Max(0.001, median * 0.12), 0, 1);
        var sampleConfidence = Math.Min(1, intervals.Length / 5d);
        _tempoBpm = Math.Clamp(60 / median, 40, 240);
        _tempoConfidence = consistency * sampleConfidence;
    }

    private void AdvanceTime(double timestampSeconds)
    {
        if (double.IsFinite(_lastUpdateTime))
        {
            var delta = Math.Clamp(timestampSeconds - _lastUpdateTime, 0, 1);
            _groove = Math.Max(0, _groove - (delta * GrooveDecayPerSecond));
            _timingFeedbackRemaining = Math.Max(0, _timingFeedbackRemaining - delta);
        }

        _lastUpdateTime = timestampSeconds;
    }

    private void RemoveExpiredActions(double timestampSeconds)
    {
        while (_pendingActionTimes.Count > 0
               && timestampSeconds - _pendingActionTimes.Peek() > TimingWindowSeconds)
        {
            _pendingActionTimes.Dequeue();
        }
    }

    private double NormalizeTimestamp(double timestampSeconds)
    {
        if (!double.IsFinite(timestampSeconds))
        {
            return double.IsFinite(_lastUpdateTime) ? _lastUpdateTime : 0;
        }

        return double.IsFinite(_lastUpdateTime)
            ? Math.Max(_lastUpdateTime, timestampSeconds)
            : Math.Max(0, timestampSeconds);
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var middle = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2
            : values[middle];
    }
}
