using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostDetailModuleProjectionTests
{
    [Fact]
    public void ProjectsOrderedRowsWithDefaultsAndDisplayState()
    {
        var projection = NativeHostDetailModuleProjection.Project(
            [
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Spectrum, 0, 360),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Waveform, 1, 240)
            ],
            new NativeHostDetailModuleVisibility(
                Stereo: null,
                Collision: true,
                LoudnessClock: true,
                Waveform: true,
                Spectrum: true));

        Assert.Equal(
            [DetailModuleKind.Spectrum, DetailModuleKind.Waveform, DetailModuleKind.Stereo, DetailModuleKind.Collision, DetailModuleKind.LoudnessClock],
            projection.Rows.Select(row => row.Kind).ToArray());
        Assert.Equal("Spectrum", projection.Rows[0].Title);
        Assert.Equal("Waveform", projection.Rows[1].Title);
        Assert.Equal("Stereo image", projection.Rows[2].Title);
        Assert.Equal("Frequency collision", projection.Rows[3].Title);
        Assert.Equal("Clock", projection.Rows[4].Title);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.Stereo).IsVisible);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.Collision).IsVisible);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.LoudnessClock).IsVisible);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.Waveform).IsVisible);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.Spectrum).IsVisible);
        Assert.Equal(360, projection.Rows.Single(row => row.Kind == DetailModuleKind.Spectrum).Height);
        Assert.Equal(240, projection.Rows.Single(row => row.Kind == DetailModuleKind.Waveform).Height);
    }

    [Fact]
    public void NormalizesDuplicateInvalidAndOutOfRangeRows()
    {
        var projection = NativeHostDetailModuleProjection.Project(
            [
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Spectrum, 0, 360),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Spectrum, 1, 240),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Stereo, 2, 1),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Collision, 3, 999),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.LoudnessClock, 4, double.PositiveInfinity),
                new NativeHostDetailModuleLayoutInput((DetailModuleKind)99, 5, 5000)
            ],
            NativeHostDetailModuleVisibility.Default);

        Assert.Equal(5, projection.Rows.Count);
        Assert.Equal(
            [DetailModuleKind.Spectrum, DetailModuleKind.Stereo, DetailModuleKind.Collision, DetailModuleKind.LoudnessClock, DetailModuleKind.Waveform],
            projection.Rows.Select(row => row.Kind).ToArray());
        Assert.Equal(360, projection.Rows.Single(row => row.Kind == DetailModuleKind.Spectrum).Height);
        Assert.Equal(320, projection.Rows.Single(row => row.Kind == DetailModuleKind.Stereo).Height);
        Assert.Equal(112, projection.Rows.Single(row => row.Kind == DetailModuleKind.Collision).Height);
        Assert.Equal(260, projection.Rows.Single(row => row.Kind == DetailModuleKind.LoudnessClock).Height);
        Assert.Equal(220, projection.Rows.Single(row => row.Kind == DetailModuleKind.Waveform).Height);
        Assert.True(projection.Rows.Single(row => row.Kind == DetailModuleKind.Stereo).IsVisible);
        Assert.False(projection.Rows.Single(row => row.Kind == DetailModuleKind.Collision).IsVisible);
        Assert.False(projection.Rows.Single(row => row.Kind == DetailModuleKind.LoudnessClock).IsVisible);
        Assert.False(projection.Rows.Single(row => row.Kind == DetailModuleKind.Waveform).IsVisible);
        Assert.False(projection.Rows.Single(row => row.Kind == DetailModuleKind.Spectrum).IsVisible);
    }

    [Fact]
    public void PreservesInputOrderForEqualOrderIndexes()
    {
        var projection = NativeHostDetailModuleProjection.Project(
            [
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Waveform, 0, 240),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.Spectrum, 0, 360),
                new NativeHostDetailModuleLayoutInput(DetailModuleKind.LoudnessClock, 0, 260)
            ],
            NativeHostDetailModuleVisibility.Default);

        Assert.Equal(
            [DetailModuleKind.Waveform, DetailModuleKind.Spectrum, DetailModuleKind.LoudnessClock, DetailModuleKind.Stereo, DetailModuleKind.Collision],
            projection.Rows.Select(row => row.Kind).ToArray());
    }
}
