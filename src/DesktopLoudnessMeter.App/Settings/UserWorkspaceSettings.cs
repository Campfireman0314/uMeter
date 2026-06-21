using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Settings;

public sealed class UserWorkspaceSettings
{
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private bool _saveToDisk;

    public UserWorkspaceSettings()
    {
    }

    private UserWorkspaceSettings(bool saveToDisk)
    {
        _saveToDisk = saveToDisk;
    }

    public string? ThemeId { get; set; }

    public int? MaxRecentEventEntries { get; set; }

    public string? EventOutputFolder { get; set; }

    public Dictionary<string, SourceWorkspaceSettings> Sources { get; set; } = new(StringComparer.Ordinal);

    public static UserWorkspaceSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new UserWorkspaceSettings(saveToDisk: true);
            }

            var json = File.ReadAllText(path);
            var settings = Normalize(JsonSerializer.Deserialize<UserWorkspaceSettings>(json, JsonOptions));
            settings._saveToDisk = true;
            return settings;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to load user workspace settings.", ex);
            return new UserWorkspaceSettings(saveToDisk: true);
        }
    }

    public void Save()
    {
        if (!_saveToDisk)
        {
            return;
        }

        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(Normalize(this), JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save user workspace settings.", ex);
        }
    }

    public SourceWorkspaceSettings? GetSource(string sourceKey)
    {
        return string.IsNullOrWhiteSpace(sourceKey)
            ? null
            : Sources.GetValueOrDefault(sourceKey);
    }

    public SourceWorkspaceSettings GetOrCreateSource(string sourceKey)
    {
        if (!Sources.TryGetValue(sourceKey, out var settings))
        {
            settings = new SourceWorkspaceSettings();
            Sources[sourceKey] = settings;
        }

        return settings;
    }

    public void ForgetSource(string sourceKey)
    {
        Sources.Remove(sourceKey);
    }

    private static UserWorkspaceSettings Normalize(UserWorkspaceSettings? settings)
    {
        settings ??= new UserWorkspaceSettings();
        settings.MaxRecentEventEntries = settings.MaxRecentEventEntries is int maxEntries
            ? Math.Clamp(maxEntries, 1, 200)
            : null;
        settings.EventOutputFolder = string.IsNullOrWhiteSpace(settings.EventOutputFolder)
            ? null
            : settings.EventOutputFolder.Trim();
        settings.Sources = settings.Sources
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value ?? new SourceWorkspaceSettings(),
                StringComparer.Ordinal);

        foreach (var source in settings.Sources.Values)
        {
            source.Normalize();
        }

        return settings;
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "uMeter",
            SettingsFileName);
    }
}

public sealed class SourceWorkspaceSettings
{
    public string? Alias { get; set; }

    public TruePeakOversampling? TruePeakOversampling { get; set; }

    public WaveformDisplayMode? WaveformDisplayMode { get; set; }

    public SpectrumDisplayMode? SpectrumDisplayMode { get; set; }

    public bool? InlineWaveformVisible { get; set; }

    public bool? InlineSpectrumVisible { get; set; }

    public bool? InlineCollisionVisible { get; set; }

    public bool? PopupWaveformVisible { get; set; }

    public bool? PopupSpectrumVisible { get; set; }

    public bool? PopupCollisionVisible { get; set; }

    public string? InlineCollisionSourceKey { get; set; }

    public string? PopupCollisionSourceKey { get; set; }

    public double? CollisionLevelFloorDb { get; set; }

    public double? CollisionAbsoluteThresholdDb { get; set; }

    public double? CollisionProminenceThresholdDb { get; set; }

    public double? CollisionRelativeThreshold { get; set; }

    public double? SpectrumDisplayFloorDb { get; set; }

    public double? SpectrumDisplayCeilingDb { get; set; }

    public StereoWorkspaceSettings? InlineStereo { get; set; }

    public StereoWorkspaceSettings? PopupStereo { get; set; }

    public void Normalize()
    {
        Alias = string.IsNullOrWhiteSpace(Alias) ? null : Alias.Trim();
        InlineCollisionSourceKey = string.IsNullOrWhiteSpace(InlineCollisionSourceKey) ? null : InlineCollisionSourceKey.Trim();
        PopupCollisionSourceKey = string.IsNullOrWhiteSpace(PopupCollisionSourceKey) ? null : PopupCollisionSourceKey.Trim();
        if (CollisionLevelFloorDb is null
            && CollisionAbsoluteThresholdDb is double legacyLevelFloorDb
            && legacyLevelFloorDb <= 0)
        {
            CollisionLevelFloorDb = legacyLevelFloorDb;
        }

        CollisionAbsoluteThresholdDb = null;
        InlineStereo?.Normalize();
        PopupStereo?.Normalize();
    }
}

public sealed class StereoWorkspaceSettings
{
    public bool IsAutoScale { get; set; } = StereoImageDisplaySettings.DefaultAutoScale;

    public double ScaleRangeDb { get; set; } = StereoImageDisplaySettings.DefaultScaleRangeDb;

    public double ManualCeilingDb { get; set; } = StereoImageDisplaySettings.DefaultManualCeilingDb;

    public double AutoScaleFloorDb { get; set; } = StereoImageDisplaySettings.DefaultAutoScaleFloorDb;

    public double AutoScaleSpeedDbPerSecond { get; set; } = StereoImageDisplaySettings.DefaultAutoScaleSpeedDbPerSecond;

    public double TracePersistenceSeconds { get; set; } = StereoImageDisplaySettings.DefaultTracePersistenceSeconds;

    public static StereoWorkspaceSettings FromSettings(StereoImageDisplaySettings settings)
    {
        return new StereoWorkspaceSettings
        {
            IsAutoScale = settings.IsAutoScale,
            ScaleRangeDb = settings.ScaleRangeDb,
            ManualCeilingDb = settings.ManualCeilingDb,
            AutoScaleFloorDb = settings.AutoScaleFloorDb,
            AutoScaleSpeedDbPerSecond = settings.AutoScaleSpeedDbPerSecond,
            TracePersistenceSeconds = settings.TracePersistenceSeconds
        };
    }

    public void ApplyTo(StereoImageDisplaySettings settings)
    {
        settings.IsAutoScale = IsAutoScale;
        settings.ScaleRangeDb = ScaleRangeDb;
        settings.ManualCeilingDb = ManualCeilingDb;
        settings.AutoScaleFloorDb = AutoScaleFloorDb;
        settings.AutoScaleSpeedDbPerSecond = AutoScaleSpeedDbPerSecond;
        settings.TracePersistenceSeconds = TracePersistenceSeconds;
    }

    public void Normalize()
    {
        ScaleRangeDb = Math.Clamp(ScaleRangeDb, 12, 96);
        ManualCeilingDb = Math.Clamp(ManualCeilingDb, -60, 12);
        AutoScaleFloorDb = Math.Clamp(AutoScaleFloorDb, -72, 0);
        AutoScaleSpeedDbPerSecond = Math.Clamp(AutoScaleSpeedDbPerSecond, 1, 24);
        TracePersistenceSeconds = Math.Clamp(TracePersistenceSeconds, 0.02, 0.25);
    }
}
