using System.Text.Json;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;
using DesktopLoudnessMeter.App.WebView;

namespace DesktopLoudnessMeter.Tests.WebView;

public sealed class HostBridgeDtoMapperTests
{
    [Fact]
    public void SnapshotResponseSerializesStableJsonEnvelope()
    {
        var snapshot = HostSnapshot.Empty with
        {
            Settings = HostSettingsSnapshot.FromWorkspace(new DesktopLoudnessMeter.App.Settings.UserWorkspaceSettings()),
            Sources =
            [
                HostSourceSnapshot.FromSignalSource(new SignalSourceSnapshot(
                    "plugin:lead",
                    SignalSourceKind.Plugin,
                    null,
                    "Lead",
                    0.5f,
                    -6,
                    "-6.0 dB",
                    "Active"))
            ],
            StatusText = "1 source tracked"
        };

        var response = HostBridgeResponse.ForSnapshot("request-1", HostBridgeDtoMapper.ToDto(snapshot));
        var json = HostBridgeJson.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("request-1", root.GetProperty("id").GetString());
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal("snapshot", root.GetProperty("kind").GetString());
        Assert.Equal("1 source tracked", root.GetProperty("snapshot").GetProperty("statusText").GetString());
        Assert.Equal(DesktopLoudnessMeter.App.Settings.UserWorkspaceSettings.CurrentSchemaVersion, root.GetProperty("snapshot").GetProperty("settings").GetProperty("schemaVersion").GetInt32());
        Assert.Equal("plugin:lead", root.GetProperty("snapshot").GetProperty("sources")[0].GetProperty("sourceKey").GetString());
    }

    [Fact]
    public void CommandResultResponseCarriesTypedError()
    {
        var command = new SelectDetailHostCommand("missing");
        var result = HostCommandResult.Fail(
            command,
            HostSnapshot.Empty,
            HostCommandError.NotFound("missing", "Source was not found."));

        var response = HostBridgeResponse.ForCommandResult("request-2", HostBridgeDtoMapper.ToDto(result));
        var json = HostBridgeJson.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.False(root.GetProperty("result").GetProperty("success").GetBoolean());
        Assert.Equal("sourceNotFound", root.GetProperty("result").GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("missing", root.GetProperty("result").GetProperty("error").GetProperty("targetSourceKey").GetString());
    }

    [Fact]
    public void SelectedDetailSerializesChartModules()
    {
        var source = HostSourceSnapshot.FromSignalSource(new SignalSourceSnapshot(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            "Lead",
            0.5f,
            -6,
            "-6.0 dB",
            "Active"));
        var snapshot = HostSnapshot.Empty with
        {
            Sources = [source],
            SelectedDetail = new HostDetailSnapshot(
                source,
                [
                    new HostDetailModuleSnapshot(DetailModuleKind.Spectrum, "Spectrum", 0, true, 360),
                    new HostDetailModuleSnapshot(DetailModuleKind.Waveform, "Waveform", 1, true, 240)
                ],
                IsWaveformVisible: true,
                IsSpectrumVisible: true,
                IsStereoVisible: true,
                IsLoudnessClockVisible: false,
                IsCollisionVisible: true,
                Collision: new HostFrequencyCollisionSnapshot(
                    "plugin:keys",
                    "mel",
                    -120,
                    6,
                    0.18,
                    CollisionFrequencyScale.Mel,
                    [new FrequencyCollisionBand(0.25, 0.5, 0.8)]))
        };

        var response = HostBridgeResponse.ForSnapshot("request-4", HostBridgeDtoMapper.ToDto(snapshot));
        var json = HostBridgeJson.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var modules = document.RootElement
            .GetProperty("snapshot")
            .GetProperty("selectedDetail")
            .GetProperty("modules");

        Assert.Equal("spectrum", modules[0].GetProperty("kind").GetString());
        Assert.Equal(360, modules[0].GetProperty("height").GetDouble());
        Assert.Equal("waveform", modules[1].GetProperty("kind").GetString());
        Assert.True(document.RootElement.GetProperty("snapshot").GetProperty("selectedDetail").GetProperty("isWaveformVisible").GetBoolean());
        var collision = document.RootElement.GetProperty("snapshot").GetProperty("selectedDetail").GetProperty("collision");
        Assert.Equal("plugin:keys", collision.GetProperty("comparisonSourceKey").GetString());
        Assert.Equal("mel", collision.GetProperty("frequencyScale").GetString());
        Assert.Equal(0.8, collision.GetProperty("bands")[0].GetProperty("intensity").GetDouble());
    }

    [Fact]
    public void ErrorResponseUsesTypedBridgeError()
    {
        var response = HostBridgeResponse.ForError(
            "request-3",
            HostBridgeError.InvalidRequest("Unknown command."));

        var json = HostBridgeJson.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.Equal("error", root.GetProperty("kind").GetString());
        Assert.Equal("invalidRequest", root.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("Unknown command.", root.GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public void SnapshotResponseSanitizesNonFiniteMetricsBeforeSerialization()
    {
        var snapshot = HostSnapshot.Empty with
        {
            Settings = HostSettingsSnapshot.FromWorkspace(new DesktopLoudnessMeter.App.Settings.UserWorkspaceSettings()),
            Sources =
            [
                HostSourceSnapshot.FromSignalSource(new SignalSourceSnapshot(
                    "plugin:silent",
                    SignalSourceKind.Plugin,
                    null,
                    "Silent",
                    float.NaN,
                    double.NegativeInfinity,
                    "-inf dB",
                    "Idle",
                    MomentaryLufs: double.NaN,
                    ShortTermLufs: double.PositiveInfinity,
                    IntegratedLufs: double.NegativeInfinity,
                    TruePeakDb: double.NaN,
                    Rms100MsDb: double.PositiveInfinity,
                    StereoImageX: double.NaN,
                    StereoImageY: double.NegativeInfinity,
                    StereoImagePoints:
                    [
                        new StereoImagePoint(0.1, 0.2),
                        new StereoImagePoint(double.NaN, 0.4)
                    ],
                    Correlation: double.PositiveInfinity,
                    WaveformSamples: [0.1, double.NaN, -0.2],
                    ChannelWaveformSamples: [[double.NegativeInfinity, 0.3]],
                    SpectrumBuckets: [0.25, double.PositiveInfinity],
                    ChannelSpectrumBuckets: [[double.NaN, 0.5]],
                    PlotFrameVersion: 7))
            ]
        };

        var response = HostBridgeResponse.ForSnapshot("request-non-finite", HostBridgeDtoMapper.ToDto(snapshot));
        var json = HostBridgeJson.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var source = document.RootElement.GetProperty("snapshot").GetProperty("sources")[0];

        Assert.Equal(0, source.GetProperty("peak").GetDouble());
        Assert.Equal(0, source.GetProperty("peakDb").GetDouble());
        Assert.False(source.TryGetProperty("momentaryLufs", out _));
        Assert.False(source.TryGetProperty("shortTermLufs", out _));
        Assert.False(source.TryGetProperty("integratedLufs", out _));
        Assert.False(source.TryGetProperty("truePeakDb", out _));
        Assert.False(source.TryGetProperty("rms100MsDb", out _));
        Assert.False(source.TryGetProperty("stereoImageX", out _));
        Assert.False(source.TryGetProperty("stereoImageY", out _));
        Assert.False(source.TryGetProperty("correlation", out _));
        Assert.Equal(1, source.GetProperty("stereoImagePoints").GetArrayLength());
        Assert.Equal(2, source.GetProperty("waveformSamples").GetArrayLength());
        Assert.Equal(1, source.GetProperty("channelWaveformSamples")[0].GetArrayLength());
        Assert.Equal(1, source.GetProperty("spectrumBuckets").GetArrayLength());
        Assert.Equal(1, source.GetProperty("channelSpectrumBuckets")[0].GetArrayLength());
    }
}
