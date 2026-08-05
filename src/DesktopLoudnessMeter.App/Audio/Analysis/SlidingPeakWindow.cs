namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class SlidingPeakWindow
{
    private readonly int _capacity;
    private readonly Queue<double> _values;

    public SlidingPeakWindow(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _values = new Queue<double>(_capacity);
    }

    public bool HasValues => _values.Count > 0;

    public double Peak => _values.Count == 0 ? 0 : _values.Max();

    public void Add(double amplitude)
    {
        _values.Enqueue(amplitude);
        if (_values.Count > _capacity)
        {
            _values.Dequeue();
        }
    }

    public void Reset()
    {
        _values.Clear();
    }
}
