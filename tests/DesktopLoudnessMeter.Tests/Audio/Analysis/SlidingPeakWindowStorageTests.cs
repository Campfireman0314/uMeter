using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// The stereo true-peak windows hold one oversampled peak per display frame and are read on every
/// snapshot refresh. Retention must stay at the configured capacity and reading the peak must not
/// allocate, because the query happens far more often than the appends.
/// </summary>
public sealed class SlidingPeakWindowStorageTests
{
    [Fact]
    public void AppendingBeyondCapacityDoesNotGrowTheRetainedWindow()
    {
        const int capacity = 8;
        var window = new SlidingPeakWindow(capacity);
        for (var index = 0; index < capacity; index++)
        {
            window.Add(0.1 * (index + 1));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            window.Add(index / 2_000d);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void PeakIsTheMaximumOfTheRetainedTail()
    {
        const int capacity = 4;
        var window = new SlidingPeakWindow(capacity);

        double[] feed = [0.25, 0.75, 0.5, 0.125, 0.375, 0.0625, 0.875, 0.5];
        var history = new List<double>(feed.Length);

        foreach (var value in feed)
        {
            window.Add(value);
            history.Add(value);

            var retained = history.Skip(Math.Max(0, history.Count - capacity)).ToArray();
            var expected = retained.Max();

            Assert.True(window.HasValues);
            Assert.Equal(expected, window.Peak);
        }
    }

    [Fact]
    public void PeakQueriesDoNotAllocateOnceTheWindowIsFull()
    {
        var window = new SlidingPeakWindow(capacity: 16);
        for (var index = 0; index < 16; index++)
        {
            window.Add(1d / (index + 1));
        }

        _ = window.Peak;

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            _ = window.Peak;
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void ResetClearsValuesAndPeak()
    {
        var window = new SlidingPeakWindow(capacity: 2);
        window.Add(0.5);
        Assert.True(window.HasValues);
        Assert.Equal(0.5, window.Peak);

        window.Reset();

        Assert.False(window.HasValues);
        Assert.Equal(0d, window.Peak);

        window.Add(0.25);
        window.Add(0.125);
        window.Add(0.0625);
        Assert.Equal(0.125, window.Peak);
    }
}
