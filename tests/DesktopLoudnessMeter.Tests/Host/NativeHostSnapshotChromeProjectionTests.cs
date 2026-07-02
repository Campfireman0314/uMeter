using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSnapshotChromeProjectionTests
{
    [Fact]
    public void ProjectsGlobalSettingsFields()
    {
        var workspace = new UserWorkspaceSettings
        {
            SchemaVersion = 0,
            ThemeId = "studio-dark",
            MaxRecentEventEntries = 42,
            EventOutputFolder = "C:\\Events",
            LoadDefaultDetailLayoutFromFile = true,
            DefaultDetailLayoutFile = "C:\\Layouts\\default.json"
        };

        var projection = NativeHostSnapshotChromeProjection.Project(
            workspace,
            showHiddenSources: true,
            selectedSourceIndex: null,
            sourceCount: 0);

        Assert.Equal(UserWorkspaceSettings.CurrentSchemaVersion, projection.Settings.SchemaVersion);
        Assert.Equal("studio-dark", projection.Settings.ThemeId);
        Assert.Equal(42, projection.Settings.MaxRecentEventEntries);
        Assert.Equal("C:\\Events", projection.Settings.EventOutputFolder);
        Assert.True(projection.Settings.LoadDefaultDetailLayoutFromFile);
        Assert.Equal("C:\\Layouts\\default.json", projection.Settings.DefaultDetailLayoutFile);
        Assert.True(projection.Settings.ShowHiddenSources);
        Assert.Empty(projection.Settings.Sources);
    }

    [Fact]
    public void ProjectsSourceRowsInOrdinalKeyOrderAndPreservesNullableFields()
    {
        var workspace = new UserWorkspaceSettings();
        var zed = workspace.GetOrCreateSource("plugin:z");
        zed.Alias = "Zed Bus";
        zed.IsHidden = true;
        zed.OrderIndex = 2;
        zed.TruePeakOversampling = TruePeakOversampling.EightX;
        zed.WaveformDisplayMode = WaveformDisplayMode.Channels;
        zed.SpectrumDisplayMode = SpectrumDisplayMode.Channels;
        zed.InlineStereoVisible = false;
        zed.InlineWaveformVisible = true;
        zed.InlineSpectrumVisible = false;
        zed.InlineLoudnessClockVisible = true;
        zed.InlineCollisionVisible = true;
        zed.InlineCollisionSourceKey = "plugin:a";
        zed.CollisionLevelFloorDb = -84;
        zed.CollisionProminenceThresholdDb = 9;
        zed.CollisionRelativeThreshold = 0.25;
        zed.CollisionFrequencyScale = CollisionFrequencyScale.Mel;
        zed.SpectrumDisplayFloorDb = -96;
        zed.SpectrumDisplayCeilingDb = -6;
        zed.LoudnessClockMinimumDb = -60;
        zed.LoudnessClockMaximumDb = 0;
        zed.LoudnessClockWarningDb = -12;
        zed.LoudnessClockCycleSeconds = 120;
        workspace.GetOrCreateSource("plugin:a");

        var projection = NativeHostSnapshotChromeProjection.Project(
            workspace,
            showHiddenSources: false,
            selectedSourceIndex: null,
            sourceCount: 0);

        Assert.Equal(["plugin:a", "plugin:z"], projection.Settings.Sources.Select(source => source.SourceKey).ToArray());

        var empty = projection.Settings.Sources[0];
        Assert.Null(empty.Alias);
        Assert.False(empty.IsHidden);
        Assert.Null(empty.OrderIndex);
        Assert.Null(empty.TruePeakOversampling);
        Assert.Null(empty.WaveformDisplayMode);
        Assert.Null(empty.SpectrumDisplayMode);
        Assert.Null(empty.InlineStereoVisible);
        Assert.Null(empty.CollisionLevelFloorDb);

        var source = projection.Settings.Sources[1];
        Assert.Equal("Zed Bus", source.Alias);
        Assert.True(source.IsHidden);
        Assert.Equal(2, source.OrderIndex);
        Assert.Equal(TruePeakOversampling.EightX, source.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, source.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, source.SpectrumDisplayMode);
        Assert.False(source.InlineStereoVisible);
        Assert.True(source.InlineWaveformVisible);
        Assert.False(source.InlineSpectrumVisible);
        Assert.True(source.InlineLoudnessClockVisible);
        Assert.True(source.InlineCollisionVisible);
        Assert.Equal("plugin:a", source.InlineCollisionSourceKey);
        Assert.Equal(-84, source.CollisionLevelFloorDb);
        Assert.Equal(9, source.CollisionProminenceThresholdDb);
        Assert.Equal(0.25, source.CollisionRelativeThreshold);
        Assert.Equal(CollisionFrequencyScale.Mel, source.CollisionFrequencyScale);
        Assert.Equal(-96, source.SpectrumDisplayFloorDb);
        Assert.Equal(-6, source.SpectrumDisplayCeilingDb);
        Assert.Equal(-60, source.LoudnessClockMinimumDb);
        Assert.Equal(0, source.LoudnessClockMaximumDb);
        Assert.Equal(-12, source.LoudnessClockWarningDb);
        Assert.Equal(120, source.LoudnessClockCycleSeconds);
    }

    [Theory]
    [InlineData(null, 0, false, false, false, false, false)]
    [InlineData(null, 2, false, true, false, false, false)]
    [InlineData(0, 3, true, true, true, false, true)]
    [InlineData(1, 3, true, true, true, true, true)]
    [InlineData(2, 3, true, true, true, true, false)]
    [InlineData(3, 3, false, true, false, false, false)]
    public void ProjectsCommandAvailability(
        int? selectedSourceIndex,
        int sourceCount,
        bool canBack,
        bool canResetAll,
        bool canForget,
        bool canMoveUp,
        bool canMoveDown)
    {
        var projection = NativeHostSnapshotChromeProjection.Project(
            new UserWorkspaceSettings(),
            showHiddenSources: false,
            selectedSourceIndex,
            sourceCount);

        Assert.Equal(canBack, projection.Commands.CanBackToGlobal);
        Assert.Equal(canResetAll, projection.Commands.CanResetAll);
        Assert.Equal(canForget, projection.Commands.CanForgetSelectedSource);
        Assert.Equal(canMoveUp, projection.Commands.CanMoveSelectedSourceUp);
        Assert.Equal(canMoveDown, projection.Commands.CanMoveSelectedSourceDown);
    }
}
