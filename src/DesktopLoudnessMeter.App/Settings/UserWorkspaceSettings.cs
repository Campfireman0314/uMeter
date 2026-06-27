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

    public bool LoadDefaultDetailLayoutFromFile { get; set; }

    public string? DefaultDetailLayoutFile { get; set; }

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
        settings.DefaultDetailLayoutFile = string.IsNullOrWhiteSpace(settings.DefaultDetailLayoutFile)
            ? null
            : settings.DefaultDetailLayoutFile.Trim();
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
    private const double LoudnessClockSettingsMinimumDb = -120;
    private const double LoudnessClockSettingsMaximumDb = 0;
    private const double LoudnessClockSettingsMinimumSpanDb = 6;
    private const double LoudnessClockSettingsMinimumCycleSeconds = 10;
    private const double LoudnessClockSettingsMaximumCycleSeconds = 600;

    public string? Alias { get; set; }

    public bool IsHidden { get; set; }

    public int? OrderIndex { get; set; }

    public TruePeakOversampling? TruePeakOversampling { get; set; }

    public WaveformDisplayMode? WaveformDisplayMode { get; set; }

    public SpectrumDisplayMode? SpectrumDisplayMode { get; set; }

    public bool? InlineStereoVisible { get; set; }

    public bool? InlineWaveformVisible { get; set; }

    public bool? InlineSpectrumVisible { get; set; }

    public bool? InlineLoudnessClockVisible { get; set; }

    public bool? InlineCollisionVisible { get; set; }

    public bool? PopupStereoVisible { get; set; }

    public bool? PopupWaveformVisible { get; set; }

    public bool? PopupSpectrumVisible { get; set; }

    public bool? PopupLoudnessClockVisible { get; set; }

    public bool? PopupCollisionVisible { get; set; }

    public string? InlineCollisionSourceKey { get; set; }

    public string? PopupCollisionSourceKey { get; set; }

    public double? CollisionLevelFloorDb { get; set; }

    public double? CollisionAbsoluteThresholdDb { get; set; }

    public double? CollisionProminenceThresholdDb { get; set; }

    public double? CollisionRelativeThreshold { get; set; }

    public CollisionFrequencyScale? CollisionFrequencyScale { get; set; }

    public double? SpectrumDisplayFloorDb { get; set; }

    public double? SpectrumDisplayCeilingDb { get; set; }

    public double? LoudnessClockMinimumDb { get; set; }

    public double? LoudnessClockMaximumDb { get; set; }

    public double? LoudnessClockWarningDb { get; set; }

    public double? LoudnessClockCycleSeconds { get; set; }

    public StereoWorkspaceSettings? InlineStereo { get; set; }

    public StereoWorkspaceSettings? PopupStereo { get; set; }

    public List<DetailModuleLayoutSettings>? InlineDetailModules { get; set; }

    public List<DetailModuleLayoutSettings>? PopupDetailModules { get; set; }

    public void Normalize()
    {
        Alias = string.IsNullOrWhiteSpace(Alias) ? null : Alias.Trim();
        OrderIndex = OrderIndex is int orderIndex && orderIndex >= 0 ? orderIndex : null;
        InlineCollisionSourceKey = string.IsNullOrWhiteSpace(InlineCollisionSourceKey) ? null : InlineCollisionSourceKey.Trim();
        PopupCollisionSourceKey = string.IsNullOrWhiteSpace(PopupCollisionSourceKey) ? null : PopupCollisionSourceKey.Trim();
        if (CollisionLevelFloorDb is null
            && CollisionAbsoluteThresholdDb is double legacyLevelFloorDb
            && legacyLevelFloorDb <= 0)
        {
            CollisionLevelFloorDb = legacyLevelFloorDb;
        }

        CollisionAbsoluteThresholdDb = null;
        NormalizeLoudnessClockRange();
        NormalizeLoudnessClockWarning();
        LoudnessClockCycleSeconds = NormalizeLoudnessClockCycleSeconds(LoudnessClockCycleSeconds);
        InlineStereo?.Normalize();
        PopupStereo?.Normalize();
        InlineDetailModules = InlineDetailModules is null
            ? null
            : DetailModuleLayoutNormalizer.Normalize(InlineDetailModules);
        PopupDetailModules = PopupDetailModules is null
            ? null
            : DetailModuleLayoutNormalizer.Normalize(PopupDetailModules);
    }

    private void NormalizeLoudnessClockRange()
    {
        var minimum = NormalizeLoudnessClockDb(LoudnessClockMinimumDb);
        var maximum = NormalizeLoudnessClockDb(LoudnessClockMaximumDb);
        if (minimum is double minimumDb && maximum is double maximumDb)
        {
            minimumDb = Math.Clamp(
                minimumDb,
                LoudnessClockSettingsMinimumDb,
                LoudnessClockSettingsMaximumDb - LoudnessClockSettingsMinimumSpanDb);
            maximumDb = Math.Clamp(
                maximumDb,
                minimumDb + LoudnessClockSettingsMinimumSpanDb,
                LoudnessClockSettingsMaximumDb);
            LoudnessClockMinimumDb = minimumDb;
            LoudnessClockMaximumDb = maximumDb;
            return;
        }

        if (minimum is double onlyMinimumDb)
        {
            LoudnessClockMinimumDb = Math.Clamp(
                onlyMinimumDb,
                LoudnessClockSettingsMinimumDb,
                LoudnessClockSettingsMaximumDb - LoudnessClockSettingsMinimumSpanDb);
        }
        else
        {
            LoudnessClockMinimumDb = null;
        }

        LoudnessClockMaximumDb = maximum is double onlyMaximumDb
            ? Math.Clamp(
                onlyMaximumDb,
                LoudnessClockSettingsMinimumDb + LoudnessClockSettingsMinimumSpanDb,
                LoudnessClockSettingsMaximumDb)
            : null;
    }

    private static double? NormalizeLoudnessClockDb(double? value)
    {
        return value is double db && double.IsFinite(db)
            ? Math.Round(db, 0, MidpointRounding.AwayFromZero)
            : null;
    }

    private void NormalizeLoudnessClockWarning()
    {
        if (NormalizeLoudnessClockDb(LoudnessClockWarningDb) is not double warningDb)
        {
            LoudnessClockWarningDb = null;
            return;
        }

        var lower = LoudnessClockMinimumDb ?? LoudnessClockSettingsMinimumDb;
        var upper = LoudnessClockMaximumDb ?? LoudnessClockSettingsMaximumDb;
        if (upper <= lower)
        {
            upper = lower + LoudnessClockSettingsMinimumSpanDb;
        }

        LoudnessClockWarningDb = Math.Clamp(warningDb, lower, upper);
    }

    private static double? NormalizeLoudnessClockCycleSeconds(double? value)
    {
        return value is double seconds && double.IsFinite(seconds)
            ? Math.Clamp(
                Math.Round(seconds, 0, MidpointRounding.AwayFromZero),
                LoudnessClockSettingsMinimumCycleSeconds,
                LoudnessClockSettingsMaximumCycleSeconds)
            : null;
    }
}

public sealed class DetailModuleLayoutSettings
{
    public DetailModuleKind Kind { get; set; }

    public int OrderIndex { get; set; }

    public double? Height { get; set; }
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
