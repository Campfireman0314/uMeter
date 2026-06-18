namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class SlidingPowerWindow
{
    private readonly int _capacity;
    private readonly Queue<double> _values;
    private double _sum;

    public SlidingPowerWindow(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Window capacity must be positive.");
        }

        _capacity = capacity;
        _values = new Queue<double>(capacity);
    }

    public bool IsFull => _values.Count == _capacity;

    public double Mean => IsFull ? _sum / _capacity : 0;

    public void Add(double power)
    {
        _values.Enqueue(power);
        _sum += power;

        if (_values.Count > _capacity)
        {
            _sum -= _values.Dequeue();
        }
    }

    public void Reset()
    {
        _values.Clear();
        _sum = 0;
    }
}
