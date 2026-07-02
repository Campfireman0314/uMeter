using System.Text.Json;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Settings;

public sealed class UserWorkspaceSettingsSchemaTests
{
    [Fact]
    public void NewWorkspaceUsesCurrentSchemaVersion()
    {
        var settings = new UserWorkspaceSettings();

        Assert.Equal(UserWorkspaceSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void LoadFromFileUpgradesLegacyWorkspaceWithoutDroppingPreferences()
    {
        var path = TemporaryJsonPath();
        try
        {
            File.WriteAllText(path, """
                {
                  "ThemeId": "studio-dark",
                  "MaxRecentEventEntries": 999,
                  "EventOutputFolder": "  C:\\Events  ",
                  "LoadDefaultDetailLayoutFromFile": true,
                  "DefaultDetailLayoutFile": "  C:\\Layouts\\default.json  ",
                  "Sources": {
                    "": { "Alias": "ignored" },
                    "plugin:lead": {
                      "Alias": "  Lead Bus  ",
                      "IsHidden": true,
                      "OrderIndex": 2,
                      "TruePeakOversampling": "EightX",
                      "WaveformDisplayMode": "Channels",
                      "SpectrumDisplayMode": "Channels",
                      "InlineCollisionSourceKey": " plugin:keys ",
                      "CollisionAbsoluteThresholdDb": -84,
                      "LoudnessClockMinimumDb": -53.4,
                      "LoudnessClockMaximumDb": -2.6,
                      "LoudnessClockWarningDb": -14.2,
                      "LoudnessClockCycleSeconds": 44.6,
                      "InlineStereo": {
                        "ScaleRangeDb": 480
                      },
                      "InlineDetailModules": [
                        { "Kind": "Spectrum", "OrderIndex": 0, "Height": 360 },
                        { "Kind": "Collision", "OrderIndex": 1, "Height": 420 }
                      ]
                    },
                    "plugin:bad-order": {
                      "Alias": "   ",
                      "OrderIndex": -1
                    }
                  }
                }
                """);

            var settings = UserWorkspaceSettings.LoadFromFile(path);

            Assert.Equal(UserWorkspaceSettings.CurrentSchemaVersion, settings.SchemaVersion);
            Assert.Equal(200, settings.MaxRecentEventEntries);
            Assert.Equal("C:\\Events", settings.EventOutputFolder);
            Assert.Equal("C:\\Layouts\\default.json", settings.DefaultDetailLayoutFile);
            Assert.False(settings.Sources.ContainsKey(string.Empty));

            var lead = settings.GetSource("plugin:lead");
            Assert.NotNull(lead);
            Assert.Equal("Lead Bus", lead.Alias);
            Assert.True(lead.IsHidden);
            Assert.Equal(2, lead.OrderIndex);
            Assert.Equal(TruePeakOversampling.EightX, lead.TruePeakOversampling);
            Assert.Equal(WaveformDisplayMode.Channels, lead.WaveformDisplayMode);
            Assert.Equal(SpectrumDisplayMode.Channels, lead.SpectrumDisplayMode);
            Assert.Equal("plugin:keys", lead.InlineCollisionSourceKey);
            Assert.Equal(-84, lead.CollisionLevelFloorDb);
            Assert.Null(lead.CollisionAbsoluteThresholdDb);
            Assert.Equal(-53, lead.LoudnessClockMinimumDb);
            Assert.Equal(-3, lead.LoudnessClockMaximumDb);
            Assert.Equal(-14, lead.LoudnessClockWarningDb);
            Assert.Equal(45, lead.LoudnessClockCycleSeconds);
            Assert.Equal(96, lead.InlineStereo!.ScaleRangeDb);
            Assert.Equal(112, lead.InlineDetailModules!.Single(module => module.Kind == DetailModuleKind.Collision).Height);

            var badOrder = settings.GetSource("plugin:bad-order");
            Assert.NotNull(badOrder);
            Assert.Null(badOrder.Alias);
            Assert.Null(badOrder.OrderIndex);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveToFileWritesCurrentSchemaVersionAndDropsLegacyCollisionField()
    {
        var path = TemporaryJsonPath();
        try
        {
            var settings = new UserWorkspaceSettings
            {
                SchemaVersion = 0,
                ThemeId = "studio-dark"
            };
            var source = settings.GetOrCreateSource("plugin:lead");
            source.Alias = "Lead Bus";
            source.CollisionAbsoluteThresholdDb = -90;
            source.WaveformDisplayMode = WaveformDisplayMode.Channels;

            settings.SaveToFile(path);

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            Assert.Equal(UserWorkspaceSettings.CurrentSchemaVersion, root.GetProperty("SchemaVersion").GetInt32());
            Assert.Equal("studio-dark", root.GetProperty("ThemeId").GetString());
            var lead = root.GetProperty("Sources").GetProperty("plugin:lead");
            Assert.Equal("Lead Bus", lead.GetProperty("Alias").GetString());
            Assert.Equal("Channels", lead.GetProperty("WaveformDisplayMode").GetString());
            Assert.Equal(-90, lead.GetProperty("CollisionLevelFloorDb").GetDouble());
            Assert.Equal(JsonValueKind.Null, lead.GetProperty("CollisionAbsoluteThresholdDb").ValueKind);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TemporaryJsonPath()
    {
        return Path.Combine(Path.GetTempPath(), $"umeter-workspace-{Guid.NewGuid():N}.json");
    }
}
