using System.Text.Json;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.WebView;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.WebView;

public sealed class HostBridgeControllerTests
{
    [Fact]
    public void SnapshotRequestReturnsCurrentHostSnapshot()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-1","kind":"snapshot"}
            """);

        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("snapshot", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("Waiting for audio sources", response.RootElement.GetProperty("snapshot").GetProperty("statusText").GetString());
    }

    [Fact]
    public void SnapshotRequestUsesConfiguredRecentEventsProvider()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(
            host,
            recentEventsProvider: () =>
            [
                new HostRecentEventSnapshot(
                    "12:00:01",
                    RecentEventKind.Loudness,
                    "Loudness jump",
                    "Lead rose 12.3 LUFS to -15.5 LUFS",
                    ReadoutSeverity.Warm)
            ]);

        var response = Send(bridge, """
            {"version":1,"id":"request-recent-provider","kind":"snapshot"}
            """);

        var recentEvents = response.RootElement.GetProperty("snapshot").GetProperty("recentEvents");
        var recent = Assert.Single(recentEvents.EnumerateArray());
        Assert.Equal("loudness", recent.GetProperty("kind").GetString());
        Assert.Equal("Loudness jump", recent.GetProperty("title").GetString());
    }

    [Fact]
    public void RefreshCommandExecutesThroughHostController()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-2","kind":"command","command":"refresh"}
            """);

        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("commandResult", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("refresh", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("plugin:lead", response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("sources")[0].GetProperty("sourceKey").GetString());
    }

    [Fact]
    public void RefreshCommandResultUsesConfiguredRecentEventsProvider()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        var bridge = new HostBridgeController(
            host,
            recentEventsProvider: () =>
            [
                new HostRecentEventSnapshot(
                    "12:00:02",
                    RecentEventKind.TruePeak,
                    "True peak max",
                    "Lead reached +1.0 dBTP",
                    ReadoutSeverity.Over)
            ]);

        var response = Send(bridge, """
            {"version":1,"id":"request-refresh-recent-provider","kind":"command","command":"refresh"}
            """);

        var recentEvents = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("recentEvents");
        var recent = Assert.Single(recentEvents.EnumerateArray());
        Assert.Equal("truePeak", recent.GetProperty("kind").GetString());
        Assert.Equal("True peak max", recent.GetProperty("title").GetString());
    }

    [Fact]
    public void OpenNativeUiCommandInvokesConfiguredEntrypoint()
    {
        var host = CreateHost();
        var opened = false;
        var bridge = new HostBridgeController(host, openNativeUi: () => opened = true);

        var response = Send(bridge, """
            {"version":1,"id":"request-native-ui","kind":"command","command":"openNativeUi"}
            """);

        Assert.True(opened);
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("commandResult", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("openNativeUi", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
    }

    [Fact]
    public void OpenNativeUiCommandReturnsBridgeErrorWhenEntrypointIsUnavailable()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-native-ui-missing","kind":"command","command":"openNativeUi"}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("error", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void ShellWindowCommandsInvokeConfiguredEntrypoint()
    {
        var host = CreateHost();
        var commands = new List<HostCommandKind>();
        var bridge = new HostBridgeController(host, handleShellCommand: command =>
        {
            commands.Add(command.Kind);
            return true;
        });

        var settings = Send(bridge, """
            {"version":1,"id":"request-shell-1","kind":"command","command":"openGlobalSettings"}
            """);
        var beginDrag = Send(bridge, """
            {"version":1,"id":"request-shell-drag","kind":"command","command":"beginWindowDrag"}
            """);
        var toggleState = Send(bridge, """
            {"version":1,"id":"request-shell-toggle","kind":"command","command":"toggleWindowState"}
            """);
        var tray = Send(bridge, """
            {"version":1,"id":"request-shell-2","kind":"command","command":"hideToTray"}
            """);
        var close = Send(bridge, """
            {"version":1,"id":"request-shell-3","kind":"command","command":"closeWindow"}
            """);
        var topmost = Send(bridge, """
            {"version":1,"id":"request-shell-4","kind":"command","command":"setAlwaysOnTop","payload":{"isAlwaysOnTop":false}}
            """);
        var displayMode = Send(bridge, """
            {"version":1,"id":"request-shell-5","kind":"command","command":"setDisplayMode","payload":{"displayMode":"lufs"}}
            """);
        var truePeak = Send(bridge, """
            {"version":1,"id":"request-shell-6","kind":"command","command":"setTruePeakEnabled","payload":{"truePeakEnabled":true}}
            """);
        var saveLayout = Send(bridge, """
            {"version":1,"id":"request-shell-7","kind":"command","command":"saveDetailLayout"}
            """);
        var loadLayout = Send(bridge, """
            {"version":1,"id":"request-shell-8","kind":"command","command":"loadDetailLayout"}
            """);
        var stereoSettings = Send(bridge, """
            {"version":1,"id":"request-shell-9","kind":"command","command":"openStereoSettings"}
            """);

        Assert.Equal(
            [
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
                HostCommandKind.OpenStereoSettings
            ],
            commands);
        Assert.Equal("openGlobalSettings", settings.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("beginWindowDrag", beginDrag.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("toggleWindowState", toggleState.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("hideToTray", tray.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("closeWindow", close.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("setAlwaysOnTop", topmost.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("setDisplayMode", displayMode.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("setTruePeakEnabled", truePeak.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("saveDetailLayout", saveLayout.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("loadDetailLayout", loadLayout.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("openStereoSettings", stereoSettings.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.True(settings.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(beginDrag.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(toggleState.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(tray.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(close.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(topmost.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(displayMode.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(truePeak.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(saveLayout.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(loadLayout.RootElement.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public void ShellWindowCommandReturnsBridgeErrorWhenEntrypointIsUnavailable()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-shell-missing","kind":"command","command":"hideToTray"}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("error", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void DetailCommandsRoundTripSelectedDetailState()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var selected = Send(bridge, """
            {"version":1,"id":"request-3","kind":"command","command":"selectDetail","payload":{"sourceKey":"plugin:lead"}}
            """);
        var global = Send(bridge, """
            {"version":1,"id":"request-4","kind":"command","command":"backToGlobal"}
            """);

        Assert.Equal("plugin:lead", selected.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("selectedDetail").GetProperty("source").GetProperty("sourceKey").GetString());
        var globalSnapshot = global.RootElement.GetProperty("result").GetProperty("snapshot");
        Assert.False(globalSnapshot.TryGetProperty("selectedDetail", out var selectedDetail) && selectedDetail.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public void OpenPopupCommandRoundTripsThroughShellEntrypoint()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        OpenPopupHostCommand? opened = null;
        var bridge = new HostBridgeController(host, handleShellCommand: command =>
        {
            opened = Assert.IsType<OpenPopupHostCommand>(command);
            return true;
        });

        var response = Send(bridge, """
            {"version":1,"id":"request-popup-1","kind":"command","command":"openPopup","payload":{"sourceKey":"plugin:lead"}}
            """);

        var snapshot = response.RootElement.GetProperty("result").GetProperty("snapshot");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("openPopup", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("plugin:lead", opened?.SourceKey);
        Assert.False(snapshot.TryGetProperty("selectedDetail", out var selectedDetail) && selectedDetail.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public void ShowHiddenCommandRebuildsSafeSnapshotFromLastSources()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:hidden").IsHidden = true;
        var host = CreateHost([Source("plugin:hidden", "Hidden", SignalSourceKind.Plugin)], workspace);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-5","kind":"command","command":"setShowHiddenSources","payload":{"showHiddenSources":true}}
            """);

        var snapshot = response.RootElement.GetProperty("result").GetProperty("snapshot");
        Assert.True(snapshot.GetProperty("settings").GetProperty("showHiddenSources").GetBoolean());
        Assert.Equal("plugin:hidden", snapshot.GetProperty("sources")[0].GetProperty("sourceKey").GetString());
    }

    [Fact]
    public void NativeMainWindowCommandsRoundTripThroughHostBridge()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        Send(bridge, """
            {"version":1,"id":"request-native-main-1","kind":"command","command":"setPerformanceMode","payload":{"performanceMode":"eco"}}
            """);
        var truePeak = Send(bridge, """
            {"version":1,"id":"request-native-main-2","kind":"command","command":"setTruePeakOversampling","payload":{"sourceKey":"plugin:lead","sourceKind":"plugin","truePeakOversampling":"eightX"}}
            """);
        var resetSource = Send(bridge, """
            {"version":1,"id":"request-native-main-3","kind":"command","command":"resetSource","payload":{"sourceKey":"plugin:lead","sourceKind":"plugin"}}
            """);
        var hidden = Send(bridge, """
            {"version":1,"id":"request-native-main-4","kind":"command","command":"hideSource","payload":{"sourceKey":"plugin:lead"}}
            """);

        Assert.True(truePeak.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("eightX", truePeak.RootElement
            .GetProperty("result")
            .GetProperty("snapshot")
            .GetProperty("sources")[0]
            .GetProperty("truePeakOversampling")
            .GetString());
        Assert.True(resetSource.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("resetSource", resetSource.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.True(hidden.RootElement.GetProperty("ok").GetBoolean());
        Assert.Empty(hidden.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public void SourceVisibilityAndOrderCommandsRoundTripThroughHostBridge()
    {
        var workspace = new UserWorkspaceSettings();
        var host = CreateHost(
            [
                Source("plugin:lead", "Lead", SignalSourceKind.Plugin),
                Source("plugin:keys", "Keys", SignalSourceKind.Plugin)
            ],
            workspace);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var shown = Send(bridge, """
            {"version":1,"id":"request-source-action-0","kind":"command","command":"setShowHiddenSources","payload":{"showHiddenSources":true}}
            """);
        var hidden = Send(bridge, """
            {"version":1,"id":"request-source-action-1","kind":"command","command":"hideSource","payload":{"sourceKey":"plugin:lead"}}
            """);
        var restored = Send(bridge, """
            {"version":1,"id":"request-source-action-2","kind":"command","command":"restoreSource","payload":{"sourceKey":"plugin:lead"}}
            """);
        var moved = Send(bridge, """
            {"version":1,"id":"request-source-action-3","kind":"command","command":"moveSource","payload":{"sourceKey":"plugin:keys","moveDirection":"up"}}
            """);

        Assert.True(shown.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(hidden.RootElement.GetProperty("ok").GetBoolean());
        var hiddenLead = hidden.RootElement
            .GetProperty("result")
            .GetProperty("snapshot")
            .GetProperty("sources")
            .EnumerateArray()
            .Single(source => source.GetProperty("sourceKey").GetString() == "plugin:lead");
        Assert.True(hiddenLead.GetProperty("isHidden").GetBoolean());
        Assert.True(restored.RootElement.GetProperty("ok").GetBoolean());
        var restoredLead = restored.RootElement
            .GetProperty("result")
            .GetProperty("snapshot")
            .GetProperty("sources")
            .EnumerateArray()
            .Single(source => source.GetProperty("sourceKey").GetString() == "plugin:lead");
        Assert.False(restoredLead.GetProperty("isHidden").GetBoolean());
        Assert.True(moved.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("plugin:keys", moved.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("sources")[0].GetProperty("sourceKey").GetString());
    }

    [Fact]
    public void ForgetSourceCommandRoundTripsThroughHostBridge()
    {
        var workspace = new UserWorkspaceSettings();
        workspace.GetOrCreateSource("plugin:lead").Alias = "Lead Bus";
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)], workspace);
        host.Refresh();
        var bridge = new HostBridgeController(host);
        Send(bridge, """
            {"version":1,"id":"request-forget-1","kind":"command","command":"selectDetail","payload":{"sourceKey":"plugin:lead"}}
            """);

        var response = Send(bridge, """
            {"version":1,"id":"request-forget-2","kind":"command","command":"forgetSource","payload":{"sourceKey":"plugin:lead"}}
            """);

        var snapshot = response.RootElement.GetProperty("result").GetProperty("snapshot");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("forgetSource", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.False(snapshot.TryGetProperty("selectedDetail", out var selectedDetail) && selectedDetail.ValueKind != JsonValueKind.Null);
        Assert.Empty(snapshot.GetProperty("settings").GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public void ChartDisplayModeCommandRoundTripsThroughHostBridge()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-chart-1","kind":"command","command":"setWaveformDisplayMode","payload":{"sourceKey":"plugin:lead","sourceKind":"plugin","waveformDisplayMode":"channels"}}
            """);

        var source = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("sources")[0];
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("setWaveformDisplayMode", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("channels", source.GetProperty("waveformDisplayMode").GetString());
    }

    [Fact]
    public void DetailModuleVisibilityCommandUpdatesSelectedDetailSnapshot()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);
        Send(bridge, """
            {"version":1,"id":"request-chart-2","kind":"command","command":"selectDetail","payload":{"sourceKey":"plugin:lead"}}
            """);

        var response = Send(bridge, """
            {"version":1,"id":"request-chart-3","kind":"command","command":"setDetailModuleVisibility","payload":{"sourceKey":"plugin:lead","moduleKind":"waveform","isVisible":true}}
            """);

        var detail = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("selectedDetail");
        var waveform = detail.GetProperty("modules").EnumerateArray().Single(module => module.GetProperty("kind").GetString() == "waveform");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(detail.GetProperty("isWaveformVisible").GetBoolean());
        Assert.True(waveform.GetProperty("isVisible").GetBoolean());
    }

    [Fact]
    public void DetailModuleHeightCommandUpdatesSelectedDetailSnapshot()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);
        Send(bridge, """
            {"version":1,"id":"request-chart-4","kind":"command","command":"selectDetail","payload":{"sourceKey":"plugin:lead"}}
            """);

        var response = Send(bridge, """
            {"version":1,"id":"request-chart-5","kind":"command","command":"setDetailModuleHeight","payload":{"sourceKey":"plugin:lead","moduleKind":"spectrum","moduleHeight":333}}
            """);

        var detail = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("selectedDetail");
        var spectrum = detail.GetProperty("modules").EnumerateArray().Single(module => module.GetProperty("kind").GetString() == "spectrum");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("setDetailModuleHeight", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal(333, spectrum.GetProperty("height").GetDouble());
    }

    [Fact]
    public void MoveDetailModuleCommandUpdatesSelectedDetailSnapshot()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);
        Send(bridge, """
            {"version":1,"id":"request-chart-6","kind":"command","command":"selectDetail","payload":{"sourceKey":"plugin:lead"}}
            """);

        var response = Send(bridge, """
            {"version":1,"id":"request-chart-7","kind":"command","command":"moveDetailModule","payload":{"sourceKey":"plugin:lead","moduleKind":"waveform","targetModuleKind":"spectrum","insertAfter":true}}
            """);

        var detail = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("selectedDetail");
        var kinds = detail.GetProperty("modules").EnumerateArray().Select(module => module.GetProperty("kind").GetString()).ToArray();
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("moveDetailModule", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal(["stereo", "collision", "loudnessClock", "spectrum", "waveform"], kinds);
    }

    [Fact]
    public void InvalidChartCommandPayloadReturnsTypedError()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-chart-4","kind":"command","command":"setWaveformDisplayMode","payload":{"sourceKey":"plugin:lead","sourceKind":"plugin","waveformDisplayMode":"sideways"}}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("error", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("overall", host.Snapshot.Sources[0].WaveformDisplayMode.ToString().ToLowerInvariant());
    }

    [Fact]
    public void ChartSettingsCommandsRoundTripWorkspaceProjection()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        Send(bridge, """
            {"version":1,"id":"request-chart-5","kind":"command","command":"setSpectrumDisplayRange","payload":{"sourceKey":"plugin:lead","spectrumDisplayFloorDb":-84,"spectrumDisplayCeilingDb":-12}}
            """);
        Send(bridge, """
            {"version":1,"id":"request-chart-6","kind":"command","command":"setLoudnessClockSettings","payload":{"sourceKey":"plugin:lead","loudnessClockMinimumDb":-42,"loudnessClockMaximumDb":-6,"loudnessClockWarningDb":-12,"loudnessClockCycleSeconds":60}}
            """);
        var response = Send(bridge, """
            {"version":1,"id":"request-chart-7","kind":"command","command":"setCollisionSettings","payload":{"sourceKey":"plugin:lead","collisionLevelFloorDb":-96,"collisionProminenceThresholdDb":9,"collisionRelativeThreshold":0.25,"collisionFrequencyScale":"mel","inlineCollisionSourceKey":"plugin:keys"}}
            """);

        var sourceSettings = response.RootElement
            .GetProperty("result")
            .GetProperty("snapshot")
            .GetProperty("settings")
            .GetProperty("sources")[0];
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(-84, sourceSettings.GetProperty("spectrumDisplayFloorDb").GetDouble());
        Assert.Equal(-12, sourceSettings.GetProperty("spectrumDisplayCeilingDb").GetDouble());
        Assert.Equal(-42, sourceSettings.GetProperty("loudnessClockMinimumDb").GetDouble());
        Assert.Equal(-6, sourceSettings.GetProperty("loudnessClockMaximumDb").GetDouble());
        Assert.Equal(-12, sourceSettings.GetProperty("loudnessClockWarningDb").GetDouble());
        Assert.Equal(60, sourceSettings.GetProperty("loudnessClockCycleSeconds").GetDouble());
        Assert.Equal(-96, sourceSettings.GetProperty("collisionLevelFloorDb").GetDouble());
        Assert.Equal(9, sourceSettings.GetProperty("collisionProminenceThresholdDb").GetDouble());
        Assert.Equal(0.25, sourceSettings.GetProperty("collisionRelativeThreshold").GetDouble());
        Assert.Equal("mel", sourceSettings.GetProperty("collisionFrequencyScale").GetString());
        Assert.Equal("plugin:keys", sourceSettings.GetProperty("inlineCollisionSourceKey").GetString());
    }

    [Fact]
    public void GlobalSettingsCommandsRoundTripWorkspaceProjection()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        Send(bridge, """
            {"version":1,"id":"request-settings-1","kind":"command","command":"setTheme","payload":{"themeId":"forest-green"}}
            """);
        Send(bridge, """
            {"version":1,"id":"request-settings-2","kind":"command","command":"setRecentEventLimit","payload":{"maxRecentEventEntries":12}}
            """);
        var response = Send(bridge, """
            {"version":1,"id":"request-settings-3","kind":"command","command":"setEventOutputFolder","payload":{"eventOutputFolder":"  C:\\uMeter Events  "}}
            """);

        var settings = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("settings");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("setEventOutputFolder", response.RootElement.GetProperty("result").GetProperty("commandKind").GetString());
        Assert.Equal("forest-green", settings.GetProperty("themeId").GetString());
        Assert.Equal(12, settings.GetProperty("maxRecentEventEntries").GetInt32());
        Assert.Equal("C:\\uMeter Events", settings.GetProperty("eventOutputFolder").GetString());
    }

    [Fact]
    public void EventOutputFolderBlankPayloadClearsWorkspaceProjection()
    {
        var workspace = new UserWorkspaceSettings { EventOutputFolder = "C:\\uMeter Events" };
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)], workspace);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-settings-blank","kind":"command","command":"setEventOutputFolder","payload":{"eventOutputFolder":"   "}}
            """);

        var settings = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("settings");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.False(settings.TryGetProperty("eventOutputFolder", out var eventOutputFolder)
            && eventOutputFolder.ValueKind != JsonValueKind.Null);
        Assert.Null(host.Snapshot.Settings.EventOutputFolder);
    }

    [Fact]
    public void ThemeCommandNormalizesKnownThemeIdCasing()
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-settings-theme-case","kind":"command","command":"setTheme","payload":{"themeId":"Forest-Green"}}
            """);

        var settings = response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("settings");
        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("forest-green", settings.GetProperty("themeId").GetString());
        Assert.Equal("forest-green", host.Snapshot.Settings.ThemeId);
    }

    [Theory]
    [InlineData("setTheme", "{\"themeId\":\"not-a-theme\"}")]
    [InlineData("setTheme", null)]
    [InlineData("setRecentEventLimit", "{\"maxRecentEventEntries\":0}")]
    [InlineData("setRecentEventLimit", "{\"maxRecentEventEntries\":201}")]
    [InlineData("setRecentEventLimit", null)]
    [InlineData("setEventOutputFolder", null)]
    public void InvalidGlobalSettingsPayloadReturnsTypedError(string command, string? payloadJson)
    {
        var workspace = new UserWorkspaceSettings
        {
            ThemeId = "midnight",
            MaxRecentEventEntries = 6,
            EventOutputFolder = "C:\\uMeter Events"
        };
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)], workspace);
        host.Refresh();
        var bridge = new HostBridgeController(host);
        var payloadClause = payloadJson is null ? string.Empty : $@",""payload"":{payloadJson}";

        var response = Send(bridge, $$"""
            {"version":1,"id":"request-settings-invalid","kind":"command","command":"{{command}}"{{payloadClause}}}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("error", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("midnight", host.Snapshot.Settings.ThemeId);
        Assert.Equal(6, host.Snapshot.Settings.MaxRecentEventEntries);
        Assert.Equal("C:\\uMeter Events", host.Snapshot.Settings.EventOutputFolder);
    }

    [Theory]
    [InlineData("setSpectrumDisplayRange", "{\"sourceKey\":\"plugin:lead\",\"spectrumDisplayFloorDb\":-12,\"spectrumDisplayCeilingDb\":-84}")]
    [InlineData("setLoudnessClockSettings", "{\"sourceKey\":\"plugin:lead\",\"loudnessClockMinimumDb\":-6,\"loudnessClockMaximumDb\":-42,\"loudnessClockWarningDb\":-12,\"loudnessClockCycleSeconds\":60}")]
    [InlineData("setCollisionSettings", "{\"sourceKey\":\"plugin:lead\",\"collisionLevelFloorDb\":-96,\"collisionProminenceThresholdDb\":9,\"collisionRelativeThreshold\":1.2,\"collisionFrequencyScale\":\"mel\"}")]
    public void OutOfRangeChartSettingsPayloadReturnsTypedError(string command, string payloadJson)
    {
        var host = CreateHost([Source("plugin:lead", "Lead", SignalSourceKind.Plugin)]);
        host.Refresh();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, $$"""
            {"version":1,"id":"request-chart-range","kind":"command","command":"{{command}}","payload":{{payloadJson}}}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Empty(host.Snapshot.Settings.Sources);
    }

    [Fact]
    public void InvalidRequestReturnsTypedErrorWithoutChangingSnapshot()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-6","kind":"command","command":"doesNotExist"}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("error", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", response.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Waiting for audio sources", host.Snapshot.StatusText);
    }

    [Fact]
    public void HostCommandFailureReturnsCommandResultWithSafeSnapshot()
    {
        var host = CreateHost();
        var bridge = new HostBridgeController(host);

        var response = Send(bridge, """
            {"version":1,"id":"request-7","kind":"command","command":"selectDetail","payload":{"sourceKey":"missing"}}
            """);

        Assert.False(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("commandResult", response.RootElement.GetProperty("kind").GetString());
        Assert.Equal("sourceNotFound", response.RootElement.GetProperty("result").GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Waiting for audio sources", response.RootElement.GetProperty("result").GetProperty("snapshot").GetProperty("statusText").GetString());
    }

    private static JsonDocument Send(HostBridgeController bridge, string json)
    {
        return JsonDocument.Parse(bridge.HandleJson(json));
    }

    private static DesktopMeterHostController CreateHost(
        IReadOnlyList<SignalSourceSnapshot>? sources = null,
        UserWorkspaceSettings? workspace = null)
    {
        return new DesktopMeterHostController(new FakeSignalSourceService(sources ?? []), workspace);
    }

    private static SignalSourceSnapshot Source(string key, string name, SignalSourceKind kind)
    {
        return new SignalSourceSnapshot(key, kind, null, name, 0.5f, -6, "-6.0 dB", "Active");
    }

    private sealed class FakeSignalSourceService : ISignalSourceMeterService
    {
        private readonly IReadOnlyList<SignalSourceSnapshot> _sources;

        public FakeSignalSourceService(IReadOnlyList<SignalSourceSnapshot> sources)
        {
            _sources = sources;
        }

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return _sources;
        }
    }
}
