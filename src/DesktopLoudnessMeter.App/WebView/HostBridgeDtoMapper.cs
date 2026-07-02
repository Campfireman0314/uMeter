using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.WebView;

public static class HostBridgeDtoMapper
{
    public static HostSnapshotDto ToDto(HostSnapshot snapshot)
    {
        return new HostSnapshotDto(
            snapshot.Sources.Select(ToDto).ToArray(),
            snapshot.SelectedDetail is null ? null : ToDto(snapshot.SelectedDetail),
            ToDto(snapshot.Settings),
            snapshot.RecentEvents.Select(ToDto).ToArray(),
            snapshot.StatusText,
            ToDto(snapshot.Commands));
    }

    public static HostCommandResultDto ToDto(HostCommandResult result)
    {
        return new HostCommandResultDto(
            FormatEnum(result.Command.Kind),
            result.Success,
            ToDto(result.Snapshot),
            result.Error is null ? null : ToDto(result.Error));
    }

    private static HostSourceSnapshotDto ToDto(HostSourceSnapshot source)
    {
        return new HostSourceSnapshotDto(
            source.SourceKey,
            FormatEnum(source.SourceKind),
            source.SourceKindLabel,
            source.ProcessId,
            source.DisplayName,
            source.Alias,
            source.EffectiveDisplayName,
            source.DisplayTitle,
            source.SourceIdentityText,
            source.IsHidden,
            source.OrderIndex,
            source.IsInstanceMissing,
            SanitizeFiniteSingle(source.Peak),
            SanitizeFiniteDouble(source.PeakDb, 0),
            source.PeakDbText,
            source.State,
            FormatEnum(source.TruePeakOversampling),
            FormatEnum(source.WaveformDisplayMode),
            FormatEnum(source.SpectrumDisplayMode),
            SanitizeNullableDouble(source.MomentaryLufs),
            SanitizeNullableDouble(source.ShortTermLufs),
            SanitizeNullableDouble(source.IntegratedLufs),
            SanitizeNullableDouble(source.TruePeakDb),
            SanitizeNullableDouble(source.Rms100MsDb),
            SanitizeNullableDouble(source.StereoImageX),
            SanitizeNullableDouble(source.StereoImageY),
            source.StereoImagePoints?.Select(ToDto).Where(point => point is not null).Cast<StereoImagePointDto>().ToArray(),
            SanitizeNullableDouble(source.Correlation),
            SanitizeDoubleList(source.WaveformSamples),
            SanitizeNestedDoubleList(source.ChannelWaveformSamples),
            SanitizeDoubleList(source.SpectrumBuckets),
            SanitizeNestedDoubleList(source.ChannelSpectrumBuckets),
            source.PlotFrameVersion);
    }

    private static StereoImagePointDto? ToDto(StereoImagePoint point)
    {
        return !double.IsFinite(point.Angle) || !double.IsFinite(point.Radius)
            ? null
            : new StereoImagePointDto(point.Angle, point.Radius);
    }

    private static HostDetailSnapshotDto ToDto(HostDetailSnapshot detail)
    {
        return new HostDetailSnapshotDto(
            ToDto(detail.Source),
            detail.Modules.Select(ToDto).ToArray(),
            detail.IsWaveformVisible,
            detail.IsSpectrumVisible,
            detail.IsStereoVisible,
            detail.IsLoudnessClockVisible,
            detail.IsCollisionVisible,
            detail.Collision is null ? null : ToDto(detail.Collision));
    }

    private static HostDetailModuleSnapshotDto ToDto(HostDetailModuleSnapshot module)
    {
        return new HostDetailModuleSnapshotDto(
            FormatEnum(module.Kind),
            module.Title,
            module.OrderIndex,
            module.IsVisible,
            SanitizeFiniteDouble(module.Height, 0));
    }

    private static HostFrequencyCollisionSnapshotDto ToDto(HostFrequencyCollisionSnapshot collision)
    {
        return new HostFrequencyCollisionSnapshotDto(
            collision.ComparisonSourceKey,
            collision.ComparisonDisplayName,
            SanitizeFiniteDouble(collision.LevelFloorDb, 0),
            SanitizeFiniteDouble(collision.ProminenceThresholdDb, 0),
            SanitizeFiniteDouble(collision.RelativeThreshold, 0),
            FormatEnum(collision.FrequencyScale),
            collision.Bands.Select(ToDto).Where(band => band is not null).Cast<FrequencyCollisionBandDto>().ToArray());
    }

    private static FrequencyCollisionBandDto? ToDto(FrequencyCollisionBand band)
    {
        return !double.IsFinite(band.StartRatio)
            || !double.IsFinite(band.EndRatio)
            || !double.IsFinite(band.Intensity)
            ? null
            : new FrequencyCollisionBandDto(
                band.StartRatio,
                band.EndRatio,
                band.Intensity);
    }

    private static HostRecentEventSnapshotDto ToDto(HostRecentEventSnapshot recentEvent)
    {
        return new HostRecentEventSnapshotDto(
            recentEvent.TimeText,
            FormatEnum(recentEvent.Kind),
            recentEvent.Title,
            recentEvent.Detail,
            FormatEnum(recentEvent.Severity));
    }

    private static HostSettingsSnapshotDto ToDto(HostSettingsSnapshot settings)
    {
        return new HostSettingsSnapshotDto(
            settings.SchemaVersion,
            settings.ThemeId,
            settings.MaxRecentEventEntries,
            settings.EventOutputFolder,
            settings.LoadDefaultDetailLayoutFromFile,
            settings.DefaultDetailLayoutFile,
            settings.ShowHiddenSources,
            settings.Sources.Select(ToDto).ToArray());
    }

    private static HostSourceSettingsSnapshotDto ToDto(HostSourceSettingsSnapshot source)
    {
        return new HostSourceSettingsSnapshotDto(
            source.SourceKey,
            source.Alias,
            source.IsHidden,
            source.OrderIndex,
            FormatNullableEnum(source.TruePeakOversampling),
            FormatNullableEnum(source.WaveformDisplayMode),
            FormatNullableEnum(source.SpectrumDisplayMode),
            source.InlineStereoVisible,
            source.InlineWaveformVisible,
            source.InlineSpectrumVisible,
            source.InlineLoudnessClockVisible,
            source.InlineCollisionVisible,
            source.InlineCollisionSourceKey,
            SanitizeNullableDouble(source.CollisionLevelFloorDb),
            SanitizeNullableDouble(source.CollisionProminenceThresholdDb),
            SanitizeNullableDouble(source.CollisionRelativeThreshold),
            FormatNullableEnum(source.CollisionFrequencyScale),
            SanitizeNullableDouble(source.SpectrumDisplayFloorDb),
            SanitizeNullableDouble(source.SpectrumDisplayCeilingDb),
            SanitizeNullableDouble(source.LoudnessClockMinimumDb),
            SanitizeNullableDouble(source.LoudnessClockMaximumDb),
            SanitizeNullableDouble(source.LoudnessClockWarningDb),
            SanitizeNullableDouble(source.LoudnessClockCycleSeconds));
    }

    private static HostCommandAvailabilityDto ToDto(HostCommandAvailability availability)
    {
        return new HostCommandAvailabilityDto(
            availability.CanBackToGlobal,
            availability.CanResetAll,
            availability.CanForgetSelectedSource,
            availability.CanMoveSelectedSourceUp,
            availability.CanMoveSelectedSourceDown);
    }

    private static HostCommandErrorDto ToDto(HostCommandError error)
    {
        return new HostCommandErrorDto(
            FormatEnum(error.Code),
            error.Message,
            error.TargetSourceKey,
            error.Detail);
    }

    private static string? FormatNullableEnum<T>(T? value)
        where T : struct, Enum
    {
        return value is { } enumValue ? FormatEnum(enumValue) : null;
    }

    private static string FormatEnum<T>(T value)
        where T : struct, Enum
    {
        var text = value.ToString();
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : char.ToLowerInvariant(text[0]) + text[1..];
    }

    private static float SanitizeFiniteSingle(float value)
    {
        return float.IsFinite(value) ? value : 0f;
    }

    private static double SanitizeFiniteDouble(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }

    private static double? SanitizeNullableDouble(double? value)
    {
        return value is double number && double.IsFinite(number) ? number : null;
    }

    private static IReadOnlyList<double>? SanitizeDoubleList(IReadOnlyList<double>? values)
    {
        if (values is null)
        {
            return null;
        }

        var sanitized = new List<double>(values.Count);
        foreach (var value in values)
        {
            if (double.IsFinite(value))
            {
                sanitized.Add(value);
            }
        }

        return sanitized;
    }

    private static IReadOnlyList<IReadOnlyList<double>>? SanitizeNestedDoubleList(IReadOnlyList<IReadOnlyList<double>>? values)
    {
        if (values is null)
        {
            return null;
        }

        var sanitized = new List<IReadOnlyList<double>>(values.Count);
        foreach (var channel in values)
        {
            sanitized.Add(SanitizeDoubleList(channel) ?? Array.Empty<double>());
        }

        return sanitized;
    }
}
