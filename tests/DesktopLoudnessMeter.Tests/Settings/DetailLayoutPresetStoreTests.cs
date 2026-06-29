using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Settings;

public sealed class DetailLayoutPresetStoreTests
{
    [Fact]
    public void PresetStoreRoundTripsTypedDetailLayout()
    {
        var path = TemporaryJsonPath();
        try
        {
            var preset = new DetailLayoutPreset
            {
                WaveformDisplayMode = WaveformDisplayMode.Channels,
                SpectrumDisplayMode = SpectrumDisplayMode.Channels,
                StereoVisible = false,
                WaveformVisible = true,
                SpectrumVisible = true,
                LoudnessClockVisible = true,
                CollisionVisible = true,
                CollisionSourceKey = " plugin:keys ",
                CollisionLevelFloorDb = -84,
                CollisionProminenceThresholdDb = 9,
                CollisionRelativeThreshold = 0.25,
                CollisionFrequencyScale = CollisionFrequencyScale.Mel,
                SpectrumDisplayFloorDb = -150,
                SpectrumDisplayCeilingDb = -12,
                LoudnessClockMinimumDb = -54,
                LoudnessClockMaximumDb = -3,
                LoudnessClockWarningDb = -14,
                LoudnessClockCycleSeconds = 45,
                Stereo = new StereoWorkspaceSettings { ScaleRangeDb = 48 },
                Modules =
                [
                    new DetailModuleLayoutSettings { Kind = DetailModuleKind.Spectrum, OrderIndex = 0, Height = 360 },
                    new DetailModuleLayoutSettings { Kind = DetailModuleKind.Stereo, OrderIndex = 1, Height = 300 },
                    new DetailModuleLayoutSettings { Kind = DetailModuleKind.Waveform, OrderIndex = 2, Height = 280 },
                    new DetailModuleLayoutSettings { Kind = DetailModuleKind.LoudnessClock, OrderIndex = 3, Height = 260 }
                ]
            };

            Assert.True(DetailLayoutPresetStore.Save(path, preset, out var saveError), saveError);
            Assert.True(DetailLayoutPresetStore.TryLoad(path, out var loaded, out var loadError), loadError);

            Assert.Equal(WaveformDisplayMode.Channels, loaded.WaveformDisplayMode);
            Assert.Equal(SpectrumDisplayMode.Channels, loaded.SpectrumDisplayMode);
            Assert.False(loaded.StereoVisible);
            Assert.True(loaded.WaveformVisible);
            Assert.True(loaded.SpectrumVisible);
            Assert.True(loaded.LoudnessClockVisible);
            Assert.True(loaded.CollisionVisible);
            Assert.Equal("plugin:keys", loaded.CollisionSourceKey);
            Assert.Equal(-84, loaded.CollisionLevelFloorDb);
            Assert.Equal(9, loaded.CollisionProminenceThresholdDb);
            Assert.Equal(0.25, loaded.CollisionRelativeThreshold);
            Assert.Equal(CollisionFrequencyScale.Mel, loaded.CollisionFrequencyScale);
            Assert.Equal(-150, loaded.SpectrumDisplayFloorDb);
            Assert.Equal(-12, loaded.SpectrumDisplayCeilingDb);
            Assert.Equal(-54, loaded.LoudnessClockMinimumDb);
            Assert.Equal(-3, loaded.LoudnessClockMaximumDb);
            Assert.Equal(-14, loaded.LoudnessClockWarningDb);
            Assert.Equal(45, loaded.LoudnessClockCycleSeconds);
            Assert.Equal(48, loaded.Stereo.ScaleRangeDb);
            Assert.Equal(
                [DetailModuleKind.Spectrum, DetailModuleKind.Stereo, DetailModuleKind.Waveform, DetailModuleKind.LoudnessClock, DetailModuleKind.Collision],
                loaded.Modules.Select(module => module.Kind).ToArray());
            Assert.Equal(360, loaded.Modules[0].Height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PresetStoreRejectsWrongSchema()
    {
        var path = TemporaryJsonPath();
        try
        {
            File.WriteAllText(path, """{"Kind":"something-else","SchemaVersion":1}""");

            Assert.False(DetailLayoutPresetStore.TryLoad(path, out _, out var error));
            Assert.Contains("supported uMeter detail layout", error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TemporaryJsonPath()
    {
        return Path.Combine(Path.GetTempPath(), $"umeter-detail-layout-{Guid.NewGuid():N}.json");
    }
}
