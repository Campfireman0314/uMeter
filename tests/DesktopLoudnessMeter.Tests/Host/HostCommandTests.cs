using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class HostCommandTests
{
    [Fact]
    public void SourceCommandsExposeStableTargetsAndPayloads()
    {
        HostCommand[] commands =
        [
            new SelectDetailHostCommand("plugin:lead"),
            new OpenPopupHostCommand("plugin:lead"),
            new ResetSourceHostCommand("plugin:lead", SignalSourceKind.Plugin, null),
            new SetSourceAliasHostCommand("plugin:lead", "Lead Bus"),
            new HideSourceHostCommand("plugin:lead"),
            new RestoreSourceHostCommand("plugin:lead"),
            new MoveSourceHostCommand("plugin:lead", HostSourceMoveDirection.Up),
            new MoveSourceToIndexHostCommand("plugin:lead", 2),
            new ForgetSourceHostCommand("plugin:lead"),
            new SetTruePeakOversamplingHostCommand("plugin:lead", SignalSourceKind.Plugin, null, TruePeakOversampling.EightX),
            new SetWaveformDisplayModeHostCommand("plugin:lead", SignalSourceKind.Plugin, null, WaveformDisplayMode.Channels),
            new SetSpectrumDisplayModeHostCommand("plugin:lead", SignalSourceKind.Plugin, null, SpectrumDisplayMode.Channels),
            new SetDetailModuleVisibilityHostCommand("plugin:lead", DetailModuleKind.Waveform, true)
        ];

        Assert.All(commands, command => Assert.Equal(HostCommandScope.Source, command.Scope));
        Assert.All(commands, command => Assert.Equal("plugin:lead", command.TargetSourceKey));
        Assert.Equal(HostCommandKind.OpenPopup, commands[1].Kind);
        Assert.Equal(HostCommandKind.SetSourceAlias, commands[3].Kind);
        Assert.Equal(HostCommandKind.SetTruePeakOversampling, commands[9].Kind);
        Assert.Equal(HostCommandKind.SetDetailModuleVisibility, commands[^1].Kind);
    }

    [Fact]
    public void GlobalCommandsExposeWorkflowOperations()
    {
        HostCommand[] commands =
        [
            new RefreshHostCommand(),
            new OpenNativeUiHostCommand(),
            new OpenGlobalSettingsHostCommand(),
            new BeginWindowDragHostCommand(),
            new ToggleWindowStateHostCommand(),
            new HideToTrayHostCommand(),
            new CloseWindowHostCommand(),
            new SetAlwaysOnTopHostCommand(false),
            new SetDisplayModeHostCommand(MeterDisplayMode.Lufs),
            new SetTruePeakEnabledHostCommand(true),
            new SaveDetailLayoutHostCommand(),
            new LoadDetailLayoutHostCommand(),
            new OpenStereoSettingsHostCommand(),
            new BackToGlobalHostCommand(),
            new ResetAllHostCommand(),
            new SetThemeHostCommand("studio-dark"),
            new SetPerformanceModeHostCommand(HostPerformanceMode.Eco),
            new SetRecentEventLimitHostCommand(20),
            new SetEventOutputFolderHostCommand("C:\\Events")
        ];

        Assert.All(commands, command => Assert.Equal(HostCommandScope.Global, command.Scope));
        Assert.Equal(
            [
                HostCommandKind.Refresh,
                HostCommandKind.OpenNativeUi,
                HostCommandKind.OpenGlobalSettings,
                HostCommandKind.BeginWindowDrag,
                HostCommandKind.ToggleWindowState,
                HostCommandKind.HideToTray,
                HostCommandKind.CloseWindow,
                HostCommandKind.SetAlwaysOnTop,
                HostCommandKind.SetDisplayMode,
                HostCommandKind.SetTruePeakEnabled,
                HostCommandKind.SaveDetailLayout,
                HostCommandKind.LoadDetailLayout,
                HostCommandKind.OpenStereoSettings,
                HostCommandKind.BackToGlobal,
                HostCommandKind.ResetAll,
                HostCommandKind.SetTheme,
                HostCommandKind.SetPerformanceMode,
                HostCommandKind.SetRecentEventLimit,
                HostCommandKind.SetEventOutputFolder
            ],
            commands.Select(command => command.Kind).ToArray());
    }

    [Fact]
    public void ResultsCarrySnapshotAndTypedErrorWithoutThrowing()
    {
        var snapshot = HostSnapshot.Empty;
        var success = HostCommandResult.Ok(new RefreshHostCommand(), snapshot);
        var error = HostCommandResult.Fail(
            new ResetSourceHostCommand("missing", SignalSourceKind.Plugin, null),
            snapshot,
            HostCommandError.NotFound("missing", "Source was not found."));

        Assert.True(success.Success);
        Assert.Null(success.Error);
        Assert.Same(snapshot, success.Snapshot);

        Assert.False(error.Success);
        Assert.Equal(HostCommandErrorCode.SourceNotFound, error.Error?.Code);
        Assert.Equal("missing", error.Error?.TargetSourceKey);
        Assert.Same(snapshot, error.Snapshot);
    }
}
