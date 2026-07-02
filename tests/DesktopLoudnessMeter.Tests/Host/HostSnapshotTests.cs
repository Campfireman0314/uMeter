using System.Reflection;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class HostSnapshotTests
{
    [Fact]
    public void SourceProjectionKeepsIdentityDisplayStateAndAnalyzerMetrics()
    {
        var source = new SignalSourceSnapshot(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            "Lead",
            0.5f,
            -6,
            "-6.0 dB",
            "Active",
            MomentaryLufs: -18.2,
            ShortTermLufs: -17.1,
            IntegratedLufs: -19.3,
            TruePeakDb: -0.7,
            Rms100MsDb: -21.4,
            StereoImageX: 0.2,
            StereoImageY: 0.8,
            StereoImagePoints: [new StereoImagePoint(0.1, 0.2)],
            Correlation: 0.75,
            WaveformSamples: [0.1, -0.1],
            ChannelWaveformSamples: [[0.2], [-0.2]],
            SpectrumBuckets: [0.3, 0.7],
            ChannelSpectrumBuckets: [[0.4], [0.6]],
            PlotFrameVersion: 42);

        var projected = HostSourceSnapshot.FromSignalSource(
            source,
            new HostSourceWorkspaceState(
                Alias: "Lead Bus",
                IsHidden: true,
                OrderIndex: 3,
                TruePeakOversampling: TruePeakOversampling.EightX,
                WaveformDisplayMode: WaveformDisplayMode.Channels,
                SpectrumDisplayMode: SpectrumDisplayMode.Channels),
            isInstanceMissing: true,
            hasDisplayNameCollision: true);

        Assert.Equal("plugin:lead", projected.SourceKey);
        Assert.Equal(SignalSourceKind.Plugin, projected.SourceKind);
        Assert.Equal("Plugin", projected.SourceKindLabel);
        Assert.Equal("Lead", projected.DisplayName);
        Assert.Equal("Lead Bus", projected.Alias);
        Assert.Equal("Lead Bus", projected.EffectiveDisplayName);
        Assert.Equal("Lead Bus - Plugin lead", projected.DisplayTitle);
        Assert.True(projected.IsHidden);
        Assert.Equal(3, projected.OrderIndex);
        Assert.True(projected.IsInstanceMissing);
        Assert.Equal(TruePeakOversampling.EightX, projected.TruePeakOversampling);
        Assert.Equal(WaveformDisplayMode.Channels, projected.WaveformDisplayMode);
        Assert.Equal(SpectrumDisplayMode.Channels, projected.SpectrumDisplayMode);
        Assert.Equal(-18.2, projected.MomentaryLufs);
        Assert.Equal(-0.7, projected.TruePeakDb);
        Assert.Equal([0.1, -0.1], projected.WaveformSamples);
        Assert.Equal([0.4], projected.ChannelSpectrumBuckets![0]);
        Assert.Equal(42, projected.PlotFrameVersion);
    }

    [Fact]
    public void SettingsProjectionReadsWorkspaceSchemaVersion()
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
        var source = workspace.GetOrCreateSource("plugin:lead");
        source.Alias = "Lead Bus";
        source.IsHidden = true;
        source.OrderIndex = 2;
        source.TruePeakOversampling = TruePeakOversampling.EightX;
        source.WaveformDisplayMode = WaveformDisplayMode.Channels;
        source.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        var projected = HostSettingsSnapshot.FromWorkspace(workspace);

        Assert.Equal(UserWorkspaceSettings.CurrentSchemaVersion, projected.SchemaVersion);
        Assert.Equal("studio-dark", projected.ThemeId);
        Assert.Equal(42, projected.MaxRecentEventEntries);
        Assert.Equal("C:\\Events", projected.EventOutputFolder);
        Assert.True(projected.LoadDefaultDetailLayoutFromFile);
        Assert.Equal("C:\\Layouts\\default.json", projected.DefaultDetailLayoutFile);

        var sourceProjection = Assert.Single(projected.Sources);
        Assert.Equal("plugin:lead", sourceProjection.SourceKey);
        Assert.Equal("Lead Bus", sourceProjection.Alias);
        Assert.True(sourceProjection.IsHidden);
        Assert.Equal(2, sourceProjection.OrderIndex);
        Assert.Equal(TruePeakOversampling.EightX, sourceProjection.TruePeakOversampling);
    }

    [Fact]
    public void HostContractPublicPropertiesDoNotExposeWpfOnlyTypes()
    {
        var forbiddenTypeNames = new[]
        {
            "System.Windows.Input.ICommand",
            "System.Collections.ObjectModel.ObservableCollection",
            "System.Windows.Threading.DispatcherTimer"
        };
        var hostTypes = typeof(HostSnapshot).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "DesktopLoudnessMeter.App.Host")
            .ToArray();

        foreach (var type in hostTypes)
        {
            var exposedTypes = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.PropertyType.FullName)
                .Concat(type
                    .GetConstructors()
                    .SelectMany(ctor => ctor.GetParameters())
                    .Select(parameter => parameter.ParameterType.FullName));

            Assert.DoesNotContain(exposedTypes, name => forbiddenTypeNames.Contains(name));
        }
    }
}
