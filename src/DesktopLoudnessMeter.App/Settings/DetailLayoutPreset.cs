using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Settings;

public sealed class DetailLayoutPreset
{
    public const string PresetKind = "uMeter.detailLayout";
    public const int CurrentSchemaVersion = 1;
    private const double CollisionLevelMinimumDb = -200;
    private const double DefaultCollisionLevelFloorDb = -120;
    private const double DefaultCollisionProminenceThresholdDb = 6;
    private const double DefaultCollisionRelativeThreshold = 0.18;
    private const double SpectrumDisplayMinimumDb = -200;
    private const double SpectrumDisplayMaximumDb = 0;
    private const double SpectrumDisplayDefaultFloorDb = -96;
    private const double SpectrumDisplayDefaultCeilingDb = -6;
    private const double SpectrumDisplayMinimumSpanDb = 6;
    private const double LoudnessClockMinimumLimitDb = -120;
    private const double LoudnessClockMaximumLimitDb = 0;
    private const double LoudnessClockDefaultMinimumDb = -32;
    private const double LoudnessClockDefaultMaximumDb = -4;
    private const double LoudnessClockDefaultWarningDb = -12;
    private const double LoudnessClockMinimumSpanDb = 6;
    private const double LoudnessClockMinimumCycleSeconds = 10;
    private const double LoudnessClockMaximumCycleSeconds = 600;
    private const double LoudnessClockDefaultCycleSeconds = 30;

    public string Kind { get; set; } = PresetKind;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public WaveformDisplayMode WaveformDisplayMode { get; set; } = WaveformDisplayMode.Overall;

    public SpectrumDisplayMode SpectrumDisplayMode { get; set; } = SpectrumDisplayMode.Overall;

    public bool StereoVisible { get; set; } = true;

    public bool WaveformVisible { get; set; }

    public bool SpectrumVisible { get; set; }

    public bool LoudnessClockVisible { get; set; }

    public bool CollisionVisible { get; set; }

    public string? CollisionSourceKey { get; set; }

    public double CollisionLevelFloorDb { get; set; } = DefaultCollisionLevelFloorDb;

    public double CollisionProminenceThresholdDb { get; set; } = DefaultCollisionProminenceThresholdDb;

    public double CollisionRelativeThreshold { get; set; } = DefaultCollisionRelativeThreshold;

    public CollisionFrequencyScale CollisionFrequencyScale { get; set; } = CollisionFrequencyScale.Logarithmic;

    public double SpectrumDisplayFloorDb { get; set; } = SpectrumDisplayDefaultFloorDb;

    public double SpectrumDisplayCeilingDb { get; set; } = SpectrumDisplayDefaultCeilingDb;

    public double LoudnessClockMinimumDb { get; set; } = LoudnessClockDefaultMinimumDb;

    public double LoudnessClockMaximumDb { get; set; } = LoudnessClockDefaultMaximumDb;

    public double LoudnessClockWarningDb { get; set; } = LoudnessClockDefaultWarningDb;

    public double LoudnessClockCycleSeconds { get; set; } = LoudnessClockDefaultCycleSeconds;

    public StereoWorkspaceSettings Stereo { get; set; } = new();

    public List<DetailModuleLayoutSettings> Modules { get; set; } = DetailModuleLayoutNormalizer.DefaultLayouts().ToList();

    public void Normalize()
    {
        Kind = PresetKind;
        SchemaVersion = CurrentSchemaVersion;
        CollisionSourceKey = string.IsNullOrWhiteSpace(CollisionSourceKey) ? null : CollisionSourceKey.Trim();
        CollisionLevelFloorDb = Math.Clamp(RoundDisplayDb(CollisionLevelFloorDb, DefaultCollisionLevelFloorDb), CollisionLevelMinimumDb, SpectrumDisplayMaximumDb);
        CollisionProminenceThresholdDb = Math.Clamp(RoundDisplayDb(CollisionProminenceThresholdDb, DefaultCollisionProminenceThresholdDb), 0, 24);
        CollisionRelativeThreshold = double.IsFinite(CollisionRelativeThreshold)
            ? Math.Clamp(CollisionRelativeThreshold, 0.02, 0.8)
            : DefaultCollisionRelativeThreshold;

        SpectrumDisplayFloorDb = Math.Clamp(
            RoundDisplayDb(SpectrumDisplayFloorDb, SpectrumDisplayDefaultFloorDb),
            SpectrumDisplayMinimumDb,
            SpectrumDisplayCeilingDb - SpectrumDisplayMinimumSpanDb);
        SpectrumDisplayCeilingDb = Math.Clamp(
            RoundDisplayDb(SpectrumDisplayCeilingDb, SpectrumDisplayDefaultCeilingDb),
            SpectrumDisplayFloorDb + SpectrumDisplayMinimumSpanDb,
            SpectrumDisplayMaximumDb);

        LoudnessClockMinimumDb = Math.Clamp(
            RoundDisplayDb(LoudnessClockMinimumDb, LoudnessClockDefaultMinimumDb),
            LoudnessClockMinimumLimitDb,
            LoudnessClockMaximumDb - LoudnessClockMinimumSpanDb);
        LoudnessClockMaximumDb = Math.Clamp(
            RoundDisplayDb(LoudnessClockMaximumDb, LoudnessClockDefaultMaximumDb),
            LoudnessClockMinimumDb + LoudnessClockMinimumSpanDb,
            LoudnessClockMaximumLimitDb);
        LoudnessClockWarningDb = Math.Clamp(
            RoundDisplayDb(LoudnessClockWarningDb, LoudnessClockDefaultWarningDb),
            LoudnessClockMinimumDb,
            LoudnessClockMaximumDb);
        LoudnessClockCycleSeconds = Math.Clamp(
            RoundDisplayDb(LoudnessClockCycleSeconds, LoudnessClockDefaultCycleSeconds),
            LoudnessClockMinimumCycleSeconds,
            LoudnessClockMaximumCycleSeconds);
        Stereo ??= new StereoWorkspaceSettings();
        Stereo.Normalize();
        Modules = DetailModuleLayoutNormalizer.Normalize(Modules);
    }

    internal bool Validate(out string error)
    {
        if (!string.Equals(Kind, PresetKind, StringComparison.Ordinal))
        {
            error = "The file is not a uMeter detail layout preset.";
            return false;
        }

        if (SchemaVersion != CurrentSchemaVersion)
        {
            error = $"Unsupported detail layout schema version {SchemaVersion}.";
            return false;
        }

        if (!Enum.IsDefined(WaveformDisplayMode)
            || !Enum.IsDefined(SpectrumDisplayMode)
            || !Enum.IsDefined(CollisionFrequencyScale))
        {
            error = "The file contains an unsupported display mode.";
            return false;
        }

        if (Modules is not null
            && Modules.Any(module =>
                !Enum.IsDefined(module.Kind)
                || module.Height is double height && !double.IsFinite(height)))
        {
            error = "The file contains an unsupported detail module layout.";
            return false;
        }

        if (!IsFinite(
            CollisionLevelFloorDb,
            CollisionProminenceThresholdDb,
            CollisionRelativeThreshold,
            SpectrumDisplayFloorDb,
            SpectrumDisplayCeilingDb,
            LoudnessClockMinimumDb,
            LoudnessClockMaximumDb,
            LoudnessClockWarningDb,
            LoudnessClockCycleSeconds))
        {
            error = "The file contains a non-finite numeric value.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public StereoWorkspaceSettings CloneStereo()
    {
        return new StereoWorkspaceSettings
        {
            IsAutoScale = Stereo.IsAutoScale,
            ScaleRangeDb = Stereo.ScaleRangeDb,
            ManualCeilingDb = Stereo.ManualCeilingDb,
            AutoScaleFloorDb = Stereo.AutoScaleFloorDb,
            AutoScaleSpeedDbPerSecond = Stereo.AutoScaleSpeedDbPerSecond,
            TracePersistenceSeconds = Stereo.TracePersistenceSeconds
        };
    }

    private static bool IsFinite(params double[] values)
    {
        return values.All(double.IsFinite);
    }

    private static double RoundDisplayDb(double value, double fallback)
    {
        return double.IsFinite(value)
            ? Math.Round(value, 0, MidpointRounding.AwayFromZero)
            : fallback;
    }
}

internal static class DetailLayoutPresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static bool Save(string? path, DetailLayoutPreset preset, out string error)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Choose a layout preset file first.";
            return false;
        }

        try
        {
            preset.Normalize();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(preset, JsonOptions));
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            error = ex.Message;
            AppLogger.Error("Failed to save detail layout preset.", ex);
            return false;
        }
    }

    public static bool TryLoad(string? path, out DetailLayoutPreset preset, out string error)
    {
        preset = new DetailLayoutPreset();
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Choose a layout preset file first.";
            return false;
        }

        try
        {
            if (!File.Exists(path))
            {
                error = "The selected layout preset file does not exist.";
                return false;
            }

            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetString(document.RootElement, nameof(DetailLayoutPreset.Kind), out var kind)
                || !string.Equals(kind, DetailLayoutPreset.PresetKind, StringComparison.Ordinal)
                || !TryGetInt32(document.RootElement, nameof(DetailLayoutPreset.SchemaVersion), out var schemaVersion)
                || schemaVersion != DetailLayoutPreset.CurrentSchemaVersion)
            {
                error = "The file is not a supported uMeter detail layout preset.";
                return false;
            }

            var loaded = JsonSerializer.Deserialize<DetailLayoutPreset>(json, JsonOptions);
            if (loaded is null)
            {
                error = "The layout preset file is empty or invalid.";
                return false;
            }

            if (!loaded.Validate(out error))
            {
                error = string.IsNullOrWhiteSpace(error) ? "The layout preset file is empty or invalid." : error;
                return false;
            }

            loaded.Normalize();
            preset = loaded;
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            error = ex.Message;
            AppLogger.Error("Failed to load detail layout preset.", ex);
            return false;
        }
    }

    public static bool TryLoadDefault(UserWorkspaceSettings workspaceSettings, out DetailLayoutPreset preset, out string error)
    {
        preset = new DetailLayoutPreset();
        error = string.Empty;
        if (!workspaceSettings.LoadDefaultDetailLayoutFromFile)
        {
            return false;
        }

        if (TryLoad(workspaceSettings.DefaultDetailLayoutFile, out preset, out error))
        {
            return true;
        }

        AppLogger.Info($"Default detail layout preset ignored: {error}");
        return false;
    }

    private static bool TryGetString(JsonElement root, string propertyName, out string value)
    {
        if (TryGetProperty(root, propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetInt32(JsonElement root, string propertyName, out int value)
    {
        if (TryGetProperty(root, propertyName, out var property) && property.TryGetInt32(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property)
    {
        foreach (var candidate in root.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
