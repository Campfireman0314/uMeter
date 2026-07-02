using System.Text.Json;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.WebView;

public sealed class HostBridgeController
{
    private const double SpectrumMinimumDb = -200;
    private const double SpectrumMaximumDb = 0;
    private const double SpectrumMinimumSpanDb = 6;
    private const double LoudnessClockMinimumDb = -120;
    private const double LoudnessClockMaximumDb = 0;
    private const double LoudnessClockMinimumSpanDb = 6;
    private const double LoudnessClockMinimumCycleSeconds = 10;
    private const double LoudnessClockMaximumCycleSeconds = 600;
    private const double CollisionLevelMinimumDb = -200;
    private const double CollisionLevelMaximumDb = 0;
    private const double CollisionProminenceMinimumDb = 0;
    private const double CollisionProminenceMaximumDb = 24;
    private const double CollisionRelativeMinimum = 0.02;
    private const double CollisionRelativeMaximum = 0.8;
    private static readonly Dictionary<string, string> SupportedThemeIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["midnight"] = "midnight",
        ["milk-white"] = "milk-white",
        ["forest-green"] = "forest-green",
        ["pink-dream"] = "pink-dream"
    };

    private readonly DesktopMeterHostController _hostController;
    private readonly Action? _openNativeUi;
    private readonly Func<HostCommand, bool>? _handleShellCommand;
    private readonly Func<IReadOnlyList<HostRecentEventSnapshot>>? _recentEventsProvider;

    public HostBridgeController(
        DesktopMeterHostController hostController,
        Action? openNativeUi = null,
        Func<HostCommand, bool>? handleShellCommand = null,
        Func<IReadOnlyList<HostRecentEventSnapshot>>? recentEventsProvider = null)
    {
        _hostController = hostController;
        _openNativeUi = openNativeUi;
        _handleShellCommand = handleShellCommand;
        _recentEventsProvider = recentEventsProvider;
    }

    public HostBridgeResponse Handle(HostBridgeRequest request)
    {
        var id = NormalizeId(request.Id);
        if (request.Version != 1)
        {
            return HostBridgeResponse.ForError(id, HostBridgeError.InvalidRequest("Unsupported HostBridge version."));
        }

        return request.Kind switch
        {
            "snapshot" => HostBridgeResponse.ForSnapshot(id, ToDto(_hostController.Snapshot)),
            "command" => ExecuteCommand(id, request),
            _ => HostBridgeResponse.ForError(id, HostBridgeError.InvalidRequest("Unknown request kind."))
        };
    }

    public string HandleJson(string json)
    {
        HostBridgeRequest? request;
        try
        {
            request = HostBridgeJson.DeserializeRequest(json);
        }
        catch (JsonException)
        {
            return HostBridgeJson.Serialize(HostBridgeResponse.ForError("unknown", HostBridgeError.InvalidRequest("Request body is not valid JSON.")));
        }

        if (request is null)
        {
            return HostBridgeJson.Serialize(HostBridgeResponse.ForError("unknown", HostBridgeError.InvalidRequest("Request body is empty.")));
        }

        return HostBridgeJson.Serialize(Handle(request));
    }

    private HostBridgeResponse ExecuteCommand(string id, HostBridgeRequest request)
    {
        var command = ToHostCommand(request);
        if (command is null)
        {
            return HostBridgeResponse.ForError(id, HostBridgeError.InvalidRequest("Unknown or invalid command."));
        }

        if (command is OpenNativeUiHostCommand)
        {
            if (_openNativeUi is null)
            {
                return HostBridgeResponse.ForError(id, HostBridgeError.InvalidRequest("Native UI entrypoint is unavailable."));
            }

            _openNativeUi();
            return HostBridgeResponse.ForCommandResult(
                id,
                ToDto(HostCommandResult.Ok(command, _hostController.Snapshot)));
        }

        if (command is OpenGlobalSettingsHostCommand
            or BeginWindowDragHostCommand
            or ToggleWindowStateHostCommand
            or HideToTrayHostCommand
            or CloseWindowHostCommand
            or SetAlwaysOnTopHostCommand
            or SetDisplayModeHostCommand
            or SetTruePeakEnabledHostCommand
            or SaveDetailLayoutHostCommand
            or LoadDetailLayoutHostCommand
            or OpenStereoSettingsHostCommand
            or OpenPopupHostCommand)
        {
            if (_handleShellCommand is null || !_handleShellCommand(command))
            {
                return HostBridgeResponse.ForError(id, HostBridgeError.InvalidRequest("Shell command entrypoint is unavailable."));
            }

            return HostBridgeResponse.ForCommandResult(
                id,
                ToDto(HostCommandResult.Ok(command, _hostController.Snapshot)));
        }

        var result = _hostController.Execute(command);
        return HostBridgeResponse.ForCommandResult(id, ToDto(result));
    }

    private HostSnapshot SnapshotForBridge(HostSnapshot snapshot)
    {
        return _recentEventsProvider is null
            ? snapshot
            : snapshot with { RecentEvents = _recentEventsProvider() };
    }

    private HostSnapshotDto ToDto(HostSnapshot snapshot)
    {
        return HostBridgeDtoMapper.ToDto(SnapshotForBridge(snapshot));
    }

    private HostCommandResultDto ToDto(HostCommandResult result)
    {
        return HostBridgeDtoMapper.ToDto(result with { Snapshot = SnapshotForBridge(result.Snapshot) });
    }

    private static HostCommand? ToHostCommand(HostBridgeRequest request)
    {
        var payload = request.Payload;
        return request.Command switch
        {
            "refresh" => new RefreshHostCommand(),
            "openNativeUi" => new OpenNativeUiHostCommand(),
            "openGlobalSettings" => new OpenGlobalSettingsHostCommand(),
            "beginWindowDrag" => new BeginWindowDragHostCommand(),
            "toggleWindowState" => new ToggleWindowStateHostCommand(),
            "hideToTray" => new HideToTrayHostCommand(),
            "closeWindow" => new CloseWindowHostCommand(),
            "setAlwaysOnTop" when payload?.IsAlwaysOnTop is { } isAlwaysOnTop => new SetAlwaysOnTopHostCommand(isAlwaysOnTop),
            "setDisplayMode" => CreateDisplayModeCommand(payload),
            "setTruePeakEnabled" when payload?.TruePeakEnabled is { } truePeakEnabled => new SetTruePeakEnabledHostCommand(truePeakEnabled),
            "saveDetailLayout" => new SaveDetailLayoutHostCommand(),
            "loadDetailLayout" => new LoadDetailLayoutHostCommand(),
            "openStereoSettings" => new OpenStereoSettingsHostCommand(),
            "backToGlobal" => new BackToGlobalHostCommand(),
            "resetAll" => new ResetAllHostCommand(),
            "selectDetail" => CreateSelectDetailCommand(payload),
            "openPopup" => CreateOpenPopupCommand(payload),
            "resetSource" => CreateResetSourceCommand(payload),
            "hideSource" => CreateHideSourceCommand(payload),
            "restoreSource" => CreateRestoreSourceCommand(payload),
            "moveSource" => CreateMoveSourceCommand(payload),
            "forgetSource" => CreateForgetSourceCommand(payload),
            "setShowHiddenSources" when payload?.ShowHiddenSources is { } showHidden => new SetShowHiddenSourcesHostCommand(showHidden),
            "setTheme" => CreateThemeCommand(payload),
            "setPerformanceMode" => CreatePerformanceModeCommand(payload),
            "setRecentEventLimit" => CreateRecentEventLimitCommand(payload),
            "setEventOutputFolder" => CreateEventOutputFolderCommand(payload),
            "setTruePeakOversampling" => CreateTruePeakOversamplingCommand(payload),
            "setWaveformDisplayMode" => CreateWaveformDisplayModeCommand(payload),
            "setSpectrumDisplayMode" => CreateSpectrumDisplayModeCommand(payload),
            "setDetailModuleVisibility" => CreateDetailModuleVisibilityCommand(payload),
            "setDetailModuleHeight" => CreateDetailModuleHeightCommand(payload),
            "moveDetailModule" => CreateMoveDetailModuleCommand(payload),
            "setSpectrumDisplayRange" => CreateSpectrumDisplayRangeCommand(payload),
            "setLoudnessClockSettings" => CreateLoudnessClockSettingsCommand(payload),
            "setCollisionSettings" => CreateCollisionSettingsCommand(payload),
            _ => null
        };
    }

    private static HostCommand? CreateSelectDetailCommand(HostBridgeRequestPayload? payload)
    {
        var sourceKey = payload?.SourceKey;
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        return new SelectDetailHostCommand(sourceKey);
    }

    private static HostCommand? CreateOpenPopupCommand(HostBridgeRequestPayload? payload)
    {
        var sourceKey = payload?.SourceKey;
        return string.IsNullOrWhiteSpace(sourceKey) ? null : new OpenPopupHostCommand(sourceKey);
    }

    private static HostCommand? CreateDisplayModeCommand(HostBridgeRequestPayload? payload)
    {
        return TryParseCamelEnum(payload?.DisplayMode, out MeterDisplayMode displayMode)
            ? new SetDisplayModeHostCommand(displayMode)
            : null;
    }

    private static HostCommand? CreateResetSourceCommand(HostBridgeRequestPayload? payload)
    {
        return TryReadSourceIdentity(payload, out var sourceKey, out var sourceKind, out var processId)
            ? new ResetSourceHostCommand(sourceKey, sourceKind, processId)
            : null;
    }

    private static HostCommand? CreateHideSourceCommand(HostBridgeRequestPayload? payload)
    {
        var sourceKey = payload?.SourceKey;
        return string.IsNullOrWhiteSpace(sourceKey) ? null : new HideSourceHostCommand(sourceKey);
    }

    private static HostCommand? CreateRestoreSourceCommand(HostBridgeRequestPayload? payload)
    {
        var sourceKey = payload?.SourceKey;
        return string.IsNullOrWhiteSpace(sourceKey) ? null : new RestoreSourceHostCommand(sourceKey);
    }

    private static HostCommand? CreateMoveSourceCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !TryParseCamelEnum(payload.MoveDirection, out HostSourceMoveDirection direction))
        {
            return null;
        }

        return new MoveSourceHostCommand(payload.SourceKey, direction);
    }

    private static HostCommand? CreateForgetSourceCommand(HostBridgeRequestPayload? payload)
    {
        var sourceKey = payload?.SourceKey;
        return string.IsNullOrWhiteSpace(sourceKey) ? null : new ForgetSourceHostCommand(sourceKey);
    }

    private static HostCommand? CreateThemeCommand(HostBridgeRequestPayload? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var themeId = string.IsNullOrWhiteSpace(payload.ThemeId) ? null : payload.ThemeId.Trim();
        if (themeId is not null && !SupportedThemeIds.TryGetValue(themeId, out themeId))
        {
            return null;
        }

        return new SetThemeHostCommand(themeId);
    }

    private static HostCommand? CreatePerformanceModeCommand(HostBridgeRequestPayload? payload)
    {
        return TryParseCamelEnum(payload?.PerformanceMode, out HostPerformanceMode mode)
            ? new SetPerformanceModeHostCommand(mode)
            : null;
    }

    private static HostCommand? CreateRecentEventLimitCommand(HostBridgeRequestPayload? payload)
    {
        if (payload?.MaxRecentEventEntries is not { } limit || limit < 1 || limit > 200)
        {
            return null;
        }

        return new SetRecentEventLimitHostCommand(limit);
    }

    private static HostCommand? CreateEventOutputFolderCommand(HostBridgeRequestPayload? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new SetEventOutputFolderHostCommand(payload.EventOutputFolder);
    }

    private static HostCommand? CreateTruePeakOversamplingCommand(HostBridgeRequestPayload? payload)
    {
        if (!TryReadSourceIdentity(payload, out var sourceKey, out var sourceKind, out var processId)
            || !TryParseCamelEnum(payload?.TruePeakOversampling, out TruePeakOversampling oversampling))
        {
            return null;
        }

        return new SetTruePeakOversamplingHostCommand(sourceKey, sourceKind, processId, oversampling);
    }

    private static HostCommand? CreateWaveformDisplayModeCommand(HostBridgeRequestPayload? payload)
    {
        if (!TryReadSourceIdentity(payload, out var sourceKey, out var sourceKind, out var processId)
            || !TryParseCamelEnum(payload?.WaveformDisplayMode, out WaveformDisplayMode displayMode))
        {
            return null;
        }

        return new SetWaveformDisplayModeHostCommand(sourceKey, sourceKind, processId, displayMode);
    }

    private static HostCommand? CreateSpectrumDisplayModeCommand(HostBridgeRequestPayload? payload)
    {
        if (!TryReadSourceIdentity(payload, out var sourceKey, out var sourceKind, out var processId)
            || !TryParseCamelEnum(payload?.SpectrumDisplayMode, out SpectrumDisplayMode displayMode))
        {
            return null;
        }

        return new SetSpectrumDisplayModeHostCommand(sourceKey, sourceKind, processId, displayMode);
    }

    private static HostCommand? CreateDetailModuleVisibilityCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !TryParseCamelEnum(payload.ModuleKind, out DetailModuleKind moduleKind)
            || payload.IsVisible is not { } isVisible)
        {
            return null;
        }

        return new SetDetailModuleVisibilityHostCommand(payload.SourceKey, moduleKind, isVisible);
    }

    private static HostCommand? CreateDetailModuleHeightCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !TryParseCamelEnum(payload.ModuleKind, out DetailModuleKind moduleKind)
            || !IsFinite(payload.ModuleHeight, out var height))
        {
            return null;
        }

        return new SetDetailModuleHeightHostCommand(payload.SourceKey, moduleKind, height);
    }

    private static HostCommand? CreateMoveDetailModuleCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !TryParseCamelEnum(payload.ModuleKind, out DetailModuleKind moduleKind)
            || !TryParseCamelEnum(payload.TargetModuleKind, out DetailModuleKind targetModuleKind)
            || payload.InsertAfter is not { } insertAfter)
        {
            return null;
        }

        return new MoveDetailModuleHostCommand(payload.SourceKey, moduleKind, targetModuleKind, insertAfter);
    }

    private static HostCommand? CreateSpectrumDisplayRangeCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !IsFinite(payload.SpectrumDisplayFloorDb, out var floorDb)
            || !IsFinite(payload.SpectrumDisplayCeilingDb, out var ceilingDb)
            || !IsValidRange(floorDb, ceilingDb, SpectrumMinimumDb, SpectrumMaximumDb, SpectrumMinimumSpanDb))
        {
            return null;
        }

        return new SetSpectrumDisplayRangeHostCommand(payload.SourceKey, floorDb, ceilingDb);
    }

    private static HostCommand? CreateLoudnessClockSettingsCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !IsFinite(payload.LoudnessClockMinimumDb, out var minimumDb)
            || !IsFinite(payload.LoudnessClockMaximumDb, out var maximumDb)
            || !IsFinite(payload.LoudnessClockWarningDb, out var warningDb)
            || !IsFinite(payload.LoudnessClockCycleSeconds, out var cycleSeconds)
            || !IsValidRange(minimumDb, maximumDb, LoudnessClockMinimumDb, LoudnessClockMaximumDb, LoudnessClockMinimumSpanDb)
            || warningDb < minimumDb
            || warningDb > maximumDb
            || cycleSeconds < LoudnessClockMinimumCycleSeconds
            || cycleSeconds > LoudnessClockMaximumCycleSeconds)
        {
            return null;
        }

        return new SetLoudnessClockSettingsHostCommand(
            payload.SourceKey,
            minimumDb,
            maximumDb,
            warningDb,
            cycleSeconds);
    }

    private static HostCommand? CreateCollisionSettingsCommand(HostBridgeRequestPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.SourceKey)
            || !IsFinite(payload.CollisionLevelFloorDb, out var levelFloorDb)
            || !IsFinite(payload.CollisionProminenceThresholdDb, out var prominenceThresholdDb)
            || !IsFinite(payload.CollisionRelativeThreshold, out var relativeThreshold)
            || !TryParseCamelEnum(payload.CollisionFrequencyScale, out CollisionFrequencyScale frequencyScale)
            || levelFloorDb < CollisionLevelMinimumDb
            || levelFloorDb > CollisionLevelMaximumDb
            || prominenceThresholdDb < CollisionProminenceMinimumDb
            || prominenceThresholdDb > CollisionProminenceMaximumDb
            || relativeThreshold < CollisionRelativeMinimum
            || relativeThreshold > CollisionRelativeMaximum)
        {
            return null;
        }

        return new SetCollisionSettingsHostCommand(
            payload.SourceKey,
            levelFloorDb,
            prominenceThresholdDb,
            relativeThreshold,
            frequencyScale,
            payload.InlineCollisionSourceKey);
    }

    private static bool TryReadSourceIdentity(
        HostBridgeRequestPayload? payload,
        out string sourceKey,
        out SignalSourceKind sourceKind,
        out int? processId)
    {
        sourceKey = payload?.SourceKey ?? string.Empty;
        sourceKind = default;
        processId = payload?.ProcessId;
        if (string.IsNullOrWhiteSpace(sourceKey)
            || !TryParseCamelEnum(payload?.SourceKind, out sourceKind))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseCamelEnum<T>(string? value, out T result)
        where T : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, ignoreCase: true, out result)
            && Enum.IsDefined(result);
    }

    private static bool IsFinite(double? value, out double result)
    {
        result = value ?? 0;
        return value is double number && double.IsFinite(number);
    }

    private static bool IsValidRange(double lower, double upper, double minimum, double maximum, double minimumSpan)
    {
        return lower >= minimum
            && lower <= maximum
            && upper >= minimum
            && upper <= maximum
            && upper - lower >= minimumSpan;
    }

    private static string NormalizeId(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? "unknown" : id;
    }
}
