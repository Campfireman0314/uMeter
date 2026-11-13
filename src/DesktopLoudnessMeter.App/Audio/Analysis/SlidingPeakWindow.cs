namespace DesktopLoudnessMeter.App.Audio.Analysis;

/// <summary>
/// Fixed-size window of the newest peak amplitudes. Storage is allocated once at the configured
/// capacity and the oldest value is evicted before a new one is stored, so retention never grows while
/// audio keeps arriving. Reading the peak scans the retained values without allocating, because the peak
/// is queried on every snapshot refresh instead of once per append.
/// </summary>
internal sealed class SlidingPeakWindow
{
    private readonly int _capacity;
    private readonly double[] _values;
    private int _startIndex;
    private int _count;

    public SlidingPeakWindow(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _values = new double[_capacity];
    }

    public bool HasValues => _count > 0;

    public double Peak
    {
        get
        {
            if (_count == 0)
            {
                return 0;
            }

            var peak = _values[_startIndex];
            for (var offset = 1; offset < _count; offset++)
            {
                var index = _startIndex + offset;
                peak = Math.Max(peak, _values[index >= _capacity ? index - _capacity : index]);
            }

            return peak;
        }
    }

    public void Add(double amplitude)
    {
        if (_count == _capacity)
        {
            _values[_startIndex] = amplitude;
            _startIndex = _startIndex + 1 == _capacity ? 0 : _startIndex + 1;
            return;
        }

        _values[(_startIndex + _count) % _capacity] = amplitude;
        _count++;
    }

    public void Reset()
    {
        _startIndex = 0;
        _count = 0;
    }
}
