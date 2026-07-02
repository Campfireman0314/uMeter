#include "DlmHostSnapshotChromeProjection.h"

#include <algorithm>
#include <cstring>
#include <memory>
#include <string>
#include <vector>

namespace
{
    constexpr int SettingsStringThemeId = 0;
    constexpr int SettingsStringEventOutputFolder = 1;
    constexpr int SettingsStringDefaultDetailLayoutFile = 2;
    constexpr int SourceStringSourceKey = 0;
    constexpr int SourceStringAlias = 1;
    constexpr int SourceStringInlineCollisionSourceKey = 2;

    struct GlobalSettings
    {
        int schemaVersion = 0;
        bool hasThemeId = false;
        std::string themeId;
        bool hasMaxRecentEventEntries = false;
        int maxRecentEventEntries = 0;
        bool hasEventOutputFolder = false;
        std::string eventOutputFolder;
        bool loadDefaultDetailLayoutFromFile = false;
        bool hasDefaultDetailLayoutFile = false;
        std::string defaultDetailLayoutFile;
        bool showHiddenSources = false;
    };

    struct SourceSettingsRow
    {
        std::string sourceKey;
        bool hasAlias = false;
        std::string alias;
        bool isHidden = false;
        bool hasOrderIndex = false;
        int orderIndex = 0;
        bool hasTruePeakOversampling = false;
        int truePeakOversampling = 0;
        bool hasWaveformDisplayMode = false;
        int waveformDisplayMode = 0;
        bool hasSpectrumDisplayMode = false;
        int spectrumDisplayMode = 0;
        bool hasInlineStereoVisible = false;
        bool inlineStereoVisible = false;
        bool hasInlineWaveformVisible = false;
        bool inlineWaveformVisible = false;
        bool hasInlineSpectrumVisible = false;
        bool inlineSpectrumVisible = false;
        bool hasInlineLoudnessClockVisible = false;
        bool inlineLoudnessClockVisible = false;
        bool hasInlineCollisionVisible = false;
        bool inlineCollisionVisible = false;
        bool hasInlineCollisionSourceKey = false;
        std::string inlineCollisionSourceKey;
        bool hasCollisionLevelFloorDb = false;
        double collisionLevelFloorDb = 0.0;
        bool hasCollisionProminenceThresholdDb = false;
        double collisionProminenceThresholdDb = 0.0;
        bool hasCollisionRelativeThreshold = false;
        double collisionRelativeThreshold = 0.0;
        bool hasCollisionFrequencyScale = false;
        int collisionFrequencyScale = 0;
        bool hasSpectrumDisplayFloorDb = false;
        double spectrumDisplayFloorDb = 0.0;
        bool hasSpectrumDisplayCeilingDb = false;
        double spectrumDisplayCeilingDb = 0.0;
        bool hasLoudnessClockMinimumDb = false;
        double loudnessClockMinimumDb = 0.0;
        bool hasLoudnessClockMaximumDb = false;
        double loudnessClockMaximumDb = 0.0;
        bool hasLoudnessClockWarningDb = false;
        double loudnessClockWarningDb = 0.0;
        bool hasLoudnessClockCycleSeconds = false;
        double loudnessClockCycleSeconds = 0.0;
    };

    struct CommandAvailability
    {
        bool canBackToGlobal = false;
        bool canResetAll = false;
        bool canForgetSelectedSource = false;
        bool canMoveSelectedSourceUp = false;
        bool canMoveSelectedSourceDown = false;
    };

    struct DlmHostSnapshotChromeProjectionHandle
    {
        GlobalSettings settings;
        std::vector<SourceSettingsRow> inputs;
        std::vector<SourceSettingsRow> projectedSources;
        CommandAvailability commands;
    };

    DlmHostSnapshotChromeProjectionHandle* AsProjection(void* handle)
    {
        return static_cast<DlmHostSnapshotChromeProjectionHandle*>(handle);
    }

    bool HasText(const char* value)
    {
        return value != nullptr;
    }

    std::string ToString(const char* value)
    {
        return value == nullptr ? std::string() : std::string(value);
    }

    int CopyStringToBuffer(const std::string& value, char* buffer, int capacity, int* actualByteCount)
    {
        if (buffer == nullptr || actualByteCount == nullptr || capacity < 0)
        {
            return -2;
        }

        const int count = static_cast<int>(value.size());
        if (capacity < count)
        {
            *actualByteCount = count;
            return -5;
        }

        if (count > 0)
        {
            std::memcpy(buffer, value.data(), static_cast<size_t>(count));
        }

        *actualByteCount = count;
        return 0;
    }

    const SourceSettingsRow* SourceAt(DlmHostSnapshotChromeProjectionHandle* projection, int index)
    {
        if (projection == nullptr || index < 0 || index >= static_cast<int>(projection->projectedSources.size()))
        {
            return nullptr;
        }

        return &projection->projectedSources[static_cast<size_t>(index)];
    }

    void FillSourceRow(const SourceSettingsRow& source, DlmHostSnapshotChromeSourceSettingsRow& row)
    {
        row = {};
        row.sourceKeyUtf8ByteCount = static_cast<int>(source.sourceKey.size());
        row.hasAlias = source.hasAlias ? 1 : 0;
        row.aliasUtf8ByteCount = static_cast<int>(source.alias.size());
        row.isHidden = source.isHidden ? 1 : 0;
        row.hasOrderIndex = source.hasOrderIndex ? 1 : 0;
        row.orderIndex = source.orderIndex;
        row.hasTruePeakOversampling = source.hasTruePeakOversampling ? 1 : 0;
        row.truePeakOversampling = source.truePeakOversampling;
        row.hasWaveformDisplayMode = source.hasWaveformDisplayMode ? 1 : 0;
        row.waveformDisplayMode = source.waveformDisplayMode;
        row.hasSpectrumDisplayMode = source.hasSpectrumDisplayMode ? 1 : 0;
        row.spectrumDisplayMode = source.spectrumDisplayMode;
        row.hasInlineStereoVisible = source.hasInlineStereoVisible ? 1 : 0;
        row.inlineStereoVisible = source.inlineStereoVisible ? 1 : 0;
        row.hasInlineWaveformVisible = source.hasInlineWaveformVisible ? 1 : 0;
        row.inlineWaveformVisible = source.inlineWaveformVisible ? 1 : 0;
        row.hasInlineSpectrumVisible = source.hasInlineSpectrumVisible ? 1 : 0;
        row.inlineSpectrumVisible = source.inlineSpectrumVisible ? 1 : 0;
        row.hasInlineLoudnessClockVisible = source.hasInlineLoudnessClockVisible ? 1 : 0;
        row.inlineLoudnessClockVisible = source.inlineLoudnessClockVisible ? 1 : 0;
        row.hasInlineCollisionVisible = source.hasInlineCollisionVisible ? 1 : 0;
        row.inlineCollisionVisible = source.inlineCollisionVisible ? 1 : 0;
        row.hasInlineCollisionSourceKey = source.hasInlineCollisionSourceKey ? 1 : 0;
        row.inlineCollisionSourceKeyUtf8ByteCount = static_cast<int>(source.inlineCollisionSourceKey.size());
        row.hasCollisionLevelFloorDb = source.hasCollisionLevelFloorDb ? 1 : 0;
        row.collisionLevelFloorDb = source.collisionLevelFloorDb;
        row.hasCollisionProminenceThresholdDb = source.hasCollisionProminenceThresholdDb ? 1 : 0;
        row.collisionProminenceThresholdDb = source.collisionProminenceThresholdDb;
        row.hasCollisionRelativeThreshold = source.hasCollisionRelativeThreshold ? 1 : 0;
        row.collisionRelativeThreshold = source.collisionRelativeThreshold;
        row.hasCollisionFrequencyScale = source.hasCollisionFrequencyScale ? 1 : 0;
        row.collisionFrequencyScale = source.collisionFrequencyScale;
        row.hasSpectrumDisplayFloorDb = source.hasSpectrumDisplayFloorDb ? 1 : 0;
        row.spectrumDisplayFloorDb = source.spectrumDisplayFloorDb;
        row.hasSpectrumDisplayCeilingDb = source.hasSpectrumDisplayCeilingDb ? 1 : 0;
        row.spectrumDisplayCeilingDb = source.spectrumDisplayCeilingDb;
        row.hasLoudnessClockMinimumDb = source.hasLoudnessClockMinimumDb ? 1 : 0;
        row.loudnessClockMinimumDb = source.loudnessClockMinimumDb;
        row.hasLoudnessClockMaximumDb = source.hasLoudnessClockMaximumDb ? 1 : 0;
        row.loudnessClockMaximumDb = source.loudnessClockMaximumDb;
        row.hasLoudnessClockWarningDb = source.hasLoudnessClockWarningDb ? 1 : 0;
        row.loudnessClockWarningDb = source.loudnessClockWarningDb;
        row.hasLoudnessClockCycleSeconds = source.hasLoudnessClockCycleSeconds ? 1 : 0;
        row.loudnessClockCycleSeconds = source.loudnessClockCycleSeconds;
    }
}

int DlmHostSnapshotChromeProjectionCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSnapshotChromeProjectionHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSnapshotChromeProjectionDestroy(void* handle)
{
    delete AsProjection(handle);
}

int DlmHostSnapshotChromeProjectionClear(void* handle)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    projection->settings = {};
    projection->inputs.clear();
    projection->projectedSources.clear();
    projection->commands = {};
    return 0;
}

int DlmHostSnapshotChromeProjectionSetGlobalSettings(
    void* handle,
    int schemaVersion,
    const char* themeId,
    int hasMaxRecentEventEntries,
    int maxRecentEventEntries,
    const char* eventOutputFolder,
    int loadDefaultDetailLayoutFromFile,
    const char* defaultDetailLayoutFile,
    int showHiddenSources)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        projection->settings.schemaVersion = schemaVersion;
        projection->settings.hasThemeId = HasText(themeId);
        projection->settings.themeId = ToString(themeId);
        projection->settings.hasMaxRecentEventEntries = hasMaxRecentEventEntries != 0;
        projection->settings.maxRecentEventEntries = maxRecentEventEntries;
        projection->settings.hasEventOutputFolder = HasText(eventOutputFolder);
        projection->settings.eventOutputFolder = ToString(eventOutputFolder);
        projection->settings.loadDefaultDetailLayoutFromFile = loadDefaultDetailLayoutFromFile != 0;
        projection->settings.hasDefaultDetailLayoutFile = HasText(defaultDetailLayoutFile);
        projection->settings.defaultDetailLayoutFile = ToString(defaultDetailLayoutFile);
        projection->settings.showHiddenSources = showHiddenSources != 0;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSnapshotChromeProjectionAddSourceSettings(
    void* handle,
    const char* sourceKey,
    const char* alias,
    int isHidden,
    int hasOrderIndex,
    int orderIndex,
    int hasTruePeakOversampling,
    int truePeakOversampling,
    int hasWaveformDisplayMode,
    int waveformDisplayMode,
    int hasSpectrumDisplayMode,
    int spectrumDisplayMode,
    int hasInlineStereoVisible,
    int inlineStereoVisible,
    int hasInlineWaveformVisible,
    int inlineWaveformVisible,
    int hasInlineSpectrumVisible,
    int inlineSpectrumVisible,
    int hasInlineLoudnessClockVisible,
    int inlineLoudnessClockVisible,
    int hasInlineCollisionVisible,
    int inlineCollisionVisible,
    const char* inlineCollisionSourceKey,
    int hasCollisionLevelFloorDb,
    double collisionLevelFloorDb,
    int hasCollisionProminenceThresholdDb,
    double collisionProminenceThresholdDb,
    int hasCollisionRelativeThreshold,
    double collisionRelativeThreshold,
    int hasCollisionFrequencyScale,
    int collisionFrequencyScale,
    int hasSpectrumDisplayFloorDb,
    double spectrumDisplayFloorDb,
    int hasSpectrumDisplayCeilingDb,
    double spectrumDisplayCeilingDb,
    int hasLoudnessClockMinimumDb,
    double loudnessClockMinimumDb,
    int hasLoudnessClockMaximumDb,
    double loudnessClockMaximumDb,
    int hasLoudnessClockWarningDb,
    double loudnessClockWarningDb,
    int hasLoudnessClockCycleSeconds,
    double loudnessClockCycleSeconds)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || sourceKey == nullptr)
    {
        return -2;
    }

    try
    {
        SourceSettingsRow row;
        row.sourceKey = sourceKey;
        row.hasAlias = HasText(alias);
        row.alias = ToString(alias);
        row.isHidden = isHidden != 0;
        row.hasOrderIndex = hasOrderIndex != 0;
        row.orderIndex = orderIndex;
        row.hasTruePeakOversampling = hasTruePeakOversampling != 0;
        row.truePeakOversampling = truePeakOversampling;
        row.hasWaveformDisplayMode = hasWaveformDisplayMode != 0;
        row.waveformDisplayMode = waveformDisplayMode;
        row.hasSpectrumDisplayMode = hasSpectrumDisplayMode != 0;
        row.spectrumDisplayMode = spectrumDisplayMode;
        row.hasInlineStereoVisible = hasInlineStereoVisible != 0;
        row.inlineStereoVisible = inlineStereoVisible != 0;
        row.hasInlineWaveformVisible = hasInlineWaveformVisible != 0;
        row.inlineWaveformVisible = inlineWaveformVisible != 0;
        row.hasInlineSpectrumVisible = hasInlineSpectrumVisible != 0;
        row.inlineSpectrumVisible = inlineSpectrumVisible != 0;
        row.hasInlineLoudnessClockVisible = hasInlineLoudnessClockVisible != 0;
        row.inlineLoudnessClockVisible = inlineLoudnessClockVisible != 0;
        row.hasInlineCollisionVisible = hasInlineCollisionVisible != 0;
        row.inlineCollisionVisible = inlineCollisionVisible != 0;
        row.hasInlineCollisionSourceKey = HasText(inlineCollisionSourceKey);
        row.inlineCollisionSourceKey = ToString(inlineCollisionSourceKey);
        row.hasCollisionLevelFloorDb = hasCollisionLevelFloorDb != 0;
        row.collisionLevelFloorDb = collisionLevelFloorDb;
        row.hasCollisionProminenceThresholdDb = hasCollisionProminenceThresholdDb != 0;
        row.collisionProminenceThresholdDb = collisionProminenceThresholdDb;
        row.hasCollisionRelativeThreshold = hasCollisionRelativeThreshold != 0;
        row.collisionRelativeThreshold = collisionRelativeThreshold;
        row.hasCollisionFrequencyScale = hasCollisionFrequencyScale != 0;
        row.collisionFrequencyScale = collisionFrequencyScale;
        row.hasSpectrumDisplayFloorDb = hasSpectrumDisplayFloorDb != 0;
        row.spectrumDisplayFloorDb = spectrumDisplayFloorDb;
        row.hasSpectrumDisplayCeilingDb = hasSpectrumDisplayCeilingDb != 0;
        row.spectrumDisplayCeilingDb = spectrumDisplayCeilingDb;
        row.hasLoudnessClockMinimumDb = hasLoudnessClockMinimumDb != 0;
        row.loudnessClockMinimumDb = loudnessClockMinimumDb;
        row.hasLoudnessClockMaximumDb = hasLoudnessClockMaximumDb != 0;
        row.loudnessClockMaximumDb = loudnessClockMaximumDb;
        row.hasLoudnessClockWarningDb = hasLoudnessClockWarningDb != 0;
        row.loudnessClockWarningDb = loudnessClockWarningDb;
        row.hasLoudnessClockCycleSeconds = hasLoudnessClockCycleSeconds != 0;
        row.loudnessClockCycleSeconds = loudnessClockCycleSeconds;
        projection->inputs.push_back(row);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSnapshotChromeProjectionProject(void* handle, int hasSelectedSourceIndex, int selectedSourceIndex, int sourceCount)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        projection->projectedSources = projection->inputs;
        std::sort(projection->projectedSources.begin(), projection->projectedSources.end(), [](const SourceSettingsRow& left, const SourceSettingsRow& right)
        {
            return left.sourceKey < right.sourceKey;
        });

        const bool hasSelection = hasSelectedSourceIndex != 0
            && selectedSourceIndex >= 0
            && selectedSourceIndex < sourceCount;
        projection->commands.canBackToGlobal = hasSelection;
        projection->commands.canResetAll = sourceCount > 0;
        projection->commands.canForgetSelectedSource = hasSelection;
        projection->commands.canMoveSelectedSourceUp = hasSelection && selectedSourceIndex > 0;
        projection->commands.canMoveSelectedSourceDown = hasSelection && selectedSourceIndex < sourceCount - 1;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSnapshotChromeProjectionGetSettings(void* handle, DlmHostSnapshotChromeSettings* settings)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || settings == nullptr)
    {
        return -2;
    }

    *settings = {};
    settings->schemaVersion = projection->settings.schemaVersion;
    settings->hasThemeId = projection->settings.hasThemeId ? 1 : 0;
    settings->hasMaxRecentEventEntries = projection->settings.hasMaxRecentEventEntries ? 1 : 0;
    settings->maxRecentEventEntries = projection->settings.maxRecentEventEntries;
    settings->hasEventOutputFolder = projection->settings.hasEventOutputFolder ? 1 : 0;
    settings->loadDefaultDetailLayoutFromFile = projection->settings.loadDefaultDetailLayoutFromFile ? 1 : 0;
    settings->hasDefaultDetailLayoutFile = projection->settings.hasDefaultDetailLayoutFile ? 1 : 0;
    settings->showHiddenSources = projection->settings.showHiddenSources ? 1 : 0;
    settings->themeIdUtf8ByteCount = static_cast<int>(projection->settings.themeId.size());
    settings->eventOutputFolderUtf8ByteCount = static_cast<int>(projection->settings.eventOutputFolder.size());
    settings->defaultDetailLayoutFileUtf8ByteCount = static_cast<int>(projection->settings.defaultDetailLayoutFile.size());
    return 0;
}

int DlmHostSnapshotChromeProjectionGetSourceSettingsCount(void* handle, int* count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(projection->projectedSources.size());
    return 0;
}

int DlmHostSnapshotChromeProjectionGetSourceSettingsAt(
    void* handle,
    int index,
    DlmHostSnapshotChromeSourceSettingsRow* row)
{
    auto* source = SourceAt(AsProjection(handle), index);
    if (source == nullptr || row == nullptr)
    {
        return -2;
    }

    FillSourceRow(*source, *row);
    return 0;
}

int DlmHostSnapshotChromeProjectionGetCommandAvailability(
    void* handle,
    DlmHostSnapshotChromeCommandAvailability* commands)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || commands == nullptr)
    {
        return -2;
    }

    *commands = {};
    commands->canBackToGlobal = projection->commands.canBackToGlobal ? 1 : 0;
    commands->canResetAll = projection->commands.canResetAll ? 1 : 0;
    commands->canForgetSelectedSource = projection->commands.canForgetSelectedSource ? 1 : 0;
    commands->canMoveSelectedSourceUp = projection->commands.canMoveSelectedSourceUp ? 1 : 0;
    commands->canMoveSelectedSourceDown = projection->commands.canMoveSelectedSourceDown ? 1 : 0;
    return 0;
}

int DlmHostSnapshotChromeProjectionCopySettingsString(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case SettingsStringThemeId:
        return CopyStringToBuffer(projection->settings.themeId, buffer, capacity, actualByteCount);
    case SettingsStringEventOutputFolder:
        return CopyStringToBuffer(projection->settings.eventOutputFolder, buffer, capacity, actualByteCount);
    case SettingsStringDefaultDetailLayoutFile:
        return CopyStringToBuffer(projection->settings.defaultDetailLayoutFile, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}

int DlmHostSnapshotChromeProjectionCopySourceSettingsStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* source = SourceAt(AsProjection(handle), index);
    if (source == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case SourceStringSourceKey:
        return CopyStringToBuffer(source->sourceKey, buffer, capacity, actualByteCount);
    case SourceStringAlias:
        return CopyStringToBuffer(source->alias, buffer, capacity, actualByteCount);
    case SourceStringInlineCollisionSourceKey:
        return CopyStringToBuffer(source->inlineCollisionSourceKey, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}
