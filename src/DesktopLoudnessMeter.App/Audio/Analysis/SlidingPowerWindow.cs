namespace DesktopLoudnessMeter.App.Audio.Analysis;

/// <summary>
/// Fixed-size sliding mean of consecutive frame powers. The retained storage is allocated once at the
/// configured capacity: once the window is full the oldest value is evicted as the new sample arrives, so
/// appending never grows the buffer. The running sum keeps the historical update order (add the new
/// value, then subtract the evicted one) so the published mean is unchanged by the storage change.
/// </summary>
internal sealed class SlidingPowerWindow
{
    private readonly int _capacity;
    private readonly double[] _values;
    private int _startIndex;
    private int _count;
    private double _sum;

    public SlidingPowerWindow(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Window capacity must be positive.");
        }

        _capacity = capacity;
        _values = new double[capacity];
    }

    public bool IsFull => _count == _capacity;

    public double Mean => IsFull ? _sum / _capacity : 0;

    public void Add(double power)
    {
        if (_count == _capacity)
        {
            _sum += power;
            _sum -= _values[_startIndex];
            _values[_startIndex] = power;
            _startIndex = _startIndex + 1 == _capacity ? 0 : _startIndex + 1;
            return;
        }

        _values[(_startIndex + _count) % _capacity] = power;
        _count++;
        _sum += power;
    }

    public void Reset()
    {
        _startIndex = 0;
        _count = 0;
        _sum = 0;
    }
}
