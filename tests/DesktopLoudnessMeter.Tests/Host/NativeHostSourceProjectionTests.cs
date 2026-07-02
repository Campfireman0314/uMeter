using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostSourceProjectionTests
{
    [Fact]
    public void ProjectsAllSourceKindsWithWorkspaceDisplayState()
    {
        var sources = new[]
        {
            Source("pid:42", "Chrome", SignalSourceKind.WasapiSession, processId: 42),
            Source("loopback:system", "System Mix", SignalSourceKind.LocalLoopback),
            Source("plugin:lead", "Lead", SignalSourceKind.Plugin)
        };
        var workspace = new Dictionary<string, HostSourceWorkspaceState>(StringComparer.Ordinal)
        {
            ["plugin:lead"] = new(
                Alias: "  Lead Bus  ",
                IsHidden: false,
                OrderIndex: 2,
                TruePeakOversampling: TruePeakOversampling.EightX,
                WaveformDisplayMode: WaveformDisplayMode.Channels,
                SpectrumDisplayMode: SpectrumDisplayMode.Channels)
        };

        var projection = NativeHostSourceProjection.Project(
            sources,
            sourceKey => workspace.GetValueOrDefault(sourceKey),
            showHiddenSources: false,
            selectedSourceKey: "plugin:lead");

        Assert.True(projection.IsSelectedSourceVisible);
        var plugin = projection.Rows.Single(row => row.SourceKey == "plugin:lead");
        Assert.Equal(2, plugin.OriginalSourceIndex);
        Assert.Equal("Plugin", plugin.SourceKindLabel);
        Assert.Equal("Lead Bus", plugin.Alias);
        Assert.Equal("Lead Bus", plugin.EffectiveDisplayName);
        Assert.Equal("Lead Bus", plugin.DisplayTitle);
        Assert.Equal("Plugin source - lead", plugin.SourceIdentityText);
        Assert.Equal(2, plugin.OrderIndex);
        Assert.Equal(TruePeakOversampling.EightX, plugin.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, plugin.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, plugin.SpectrumDisplayMode);

        var wasapi = projection.Rows.Single(row => row.SourceKey == "pid:42");
        Assert.Equal("WASAPI", wasapi.SourceKindLabel);
        Assert.Equal("WASAPI source - PID 42", wasapi.SourceIdentityText);
        Assert.Equal(TruePeakOversampling.FourX, wasapi.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Overall, wasapi.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Overall, wasapi.SpectrumDisplayMode);
    }

    [Fact]
    public void FiltersHiddenRowsAndReportsSelectedVisibility()
    {
        var sources = new[]
        {
            Source("plugin:hidden", "Hidden", SignalSourceKind.Plugin),
            Source("plugin:visible", "Visible", SignalSourceKind.Plugin)
        };
        var workspace = new Dictionary<string, HostSourceWorkspaceState>(StringComparer.Ordinal)
        {
            ["plugin:hidden"] = new(IsHidden: true)
        };

        var hidden = NativeHostSourceProjection.Project(
            sources,
            sourceKey => workspace.GetValueOrDefault(sourceKey),
            showHiddenSources: false,
            selectedSourceKey: "plugin:hidden");
        var shown = NativeHostSourceProjection.Project(
            sources,
            sourceKey => workspace.GetValueOrDefault(sourceKey),
            showHiddenSources: true,
            selectedSourceKey: "plugin:hidden");

        Assert.False(hidden.IsSelectedSourceVisible);
        Assert.DoesNotContain(hidden.Rows, row => row.SourceKey == "plugin:hidden");
        Assert.True(shown.IsSelectedSourceVisible);
        Assert.True(shown.Rows.Single(row => row.SourceKey == "plugin:hidden").IsHidden);
    }

    [Fact]
    public void OrdersExplicitRowsBeforeFallbackKindNameAndKey()
    {
        var sources = new[]
        {
            Source("pid:9", "Zoom", SignalSourceKind.WasapiSession, processId: 9),
            Source("plugin:z", "Zed", SignalSourceKind.Plugin),
            Source("loopback:a", "Alpha", SignalSourceKind.LocalLoopback),
            Source("plugin:a", "Alpha", SignalSourceKind.Plugin),
            Source("plugin:ordered", "Later Name", SignalSourceKind.Plugin)
        };
        var workspace = new Dictionary<string, HostSourceWorkspaceState>(StringComparer.Ordinal)
        {
            ["plugin:ordered"] = new(OrderIndex: 0)
        };

        var projection = NativeHostSourceProjection.Project(
            sources,
            sourceKey => workspace.GetValueOrDefault(sourceKey),
            showHiddenSources: false,
            selectedSourceKey: null);

        Assert.Equal(
            ["plugin:ordered", "plugin:a", "plugin:z", "loopback:a", "pid:9"],
            projection.Rows.Select(row => row.SourceKey).ToArray());
    }

    [Fact]
    public void DisambiguatesDuplicateEffectiveDisplayNames()
    {
        var sources = new[]
        {
            Source("plugin:lead-a", "Lead", SignalSourceKind.Plugin),
            Source("loopback:lead-b", "Lead", SignalSourceKind.LocalLoopback),
            Source("pid:12", "Lead", SignalSourceKind.WasapiSession, processId: 12)
        };

        var projection = NativeHostSourceProjection.Project(
            sources,
            _ => null,
            showHiddenSources: false,
            selectedSourceKey: null);

        Assert.Equal("Lead - Plugin lead-a", projection.Rows.Single(row => row.SourceKey == "plugin:lead-a").DisplayTitle);
        Assert.Equal("Lead - Loopback lead-b", projection.Rows.Single(row => row.SourceKey == "loopback:lead-b").DisplayTitle);
        Assert.Equal("Lead - WASAPI PID 12", projection.Rows.Single(row => row.SourceKey == "pid:12").DisplayTitle);
    }

    [Fact]
    public void CompactsLongSourceIdentifiers()
    {
        var longKey = "plugin:this-source-identifier-is-longer-than-twenty-four-characters";
        var source = Source(longKey, "Long", SignalSourceKind.Plugin);

        var projection = NativeHostSourceProjection.Project(
            [source],
            _ => null,
            showHiddenSources: false,
            selectedSourceKey: null);

        var row = Assert.Single(projection.Rows);
        Assert.Equal("Plugin source - this-source-identifie...", row.SourceIdentityText);
    }

    private static SignalSourceSnapshot Source(
        string key,
        string name,
        SignalSourceKind kind,
        int? processId = null)
    {
        return new SignalSourceSnapshot(
            key,
            kind,
            processId,
            name,
            0.5f,
            PeakMath.ToDecibels(0.5f),
            "-6.0 dB",
            "Active");
    }
}
