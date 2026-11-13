using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio.Analysis;

/// <summary>
/// Storage behavior of the scalar sliding windows the analyzer feeds one frame at a time. A full window
/// must keep exactly its configured number of values: the oldest sample is evicted, the retention never
/// grows, and the retained arithmetic still describes the most recent window. The allocation assertion
/// is deterministic (a preallocated full window must not allocate while appending); wall-clock time is
/// never used as an oracle.
/// </summary>
public sealed class SlidingPowerWindowStorageTests
{
    [Fact]
    public void AppendingBeyondCapacityDoesNotGrowTheRetainedWindow()
    {
        const int capacity = 8;
        var window = new SlidingPowerWindow(capacity);

        // A full window is the steady state while audio keeps arriving.
        for (var index = 0; index < capacity; index++)
        {
            window.Add(1d / (index + 1));
        }

        Assert.True(window.IsFull);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            window.Add(0.5);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0, allocatedBytes);
        Assert.True(window.IsFull);
    }

    [Fact]
    public void MeanFollowsTheMostRecentCapacityValues()
    {
        const int capacity = 4;
        var window = new SlidingPowerWindow(capacity);

        // Every value and every partial sum here is exactly representable, so the expected mean can be
        // compared digit for digit instead of with a tolerance.
        double[] feed = [1.0, 2.0, 4.0, 8.0, 16.0, 0.5, 0.25, 3.0, 0.125];
        var history = new List<double>(feed.Length);

        foreach (var value in feed)
        {
            window.Add(value);
            history.Add(value);

            if (history.Count < capacity)
            {
                Assert.False(window.IsFull);
                Assert.Equal(0d, window.Mean);
                continue;
            }

            var retained = history.Skip(history.Count - capacity).ToArray();
            var expected = 0d;
            foreach (var retainedValue in retained)
            {
                expected += retainedValue;
            }

            Assert.True(window.IsFull);
            Assert.Equal(expected / capacity, window.Mean);
        }
    }

    [Fact]
    public void ResetDiscardsRetainedValuesAndRefillsFromEmpty()
    {
        var window = new SlidingPowerWindow(capacity: 2);
        window.Add(1.0);
        window.Add(2.0);
        Assert.Equal(1.5, window.Mean);

        window.Reset();

        Assert.False(window.IsFull);
        Assert.Equal(0d, window.Mean);

        window.Add(4.0);
        Assert.False(window.IsFull);
        window.Add(8.0);
        Assert.True(window.IsFull);
        Assert.Equal(6d, window.Mean);
    }
}
