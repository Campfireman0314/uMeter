#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSnapshotChromeSettings
    {
        int schemaVersion;
        int hasThemeId;
        int hasMaxRecentEventEntries;
        int maxRecentEventEntries;
        int hasEventOutputFolder;
        int loadDefaultDetailLayoutFromFile;
        int hasDefaultDetailLayoutFile;
        int showHiddenSources;
        int themeIdUtf8ByteCount;
        int eventOutputFolderUtf8ByteCount;
        int defaultDetailLayoutFileUtf8ByteCount;
    };

    struct DlmHostSnapshotChromeSourceSettingsRow
    {
        int sourceKeyUtf8ByteCount;
        int hasAlias;
        int aliasUtf8ByteCount;
        int isHidden;
        int hasOrderIndex;
        int orderIndex;
        int hasTruePeakOversampling;
        int truePeakOversampling;
        int hasWaveformDisplayMode;
        int waveformDisplayMode;
        int hasSpectrumDisplayMode;
        int spectrumDisplayMode;
        int hasInlineStereoVisible;
        int inlineStereoVisible;
        int hasInlineWaveformVisible;
        int inlineWaveformVisible;
        int hasInlineSpectrumVisible;
        int inlineSpectrumVisible;
        int hasInlineLoudnessClockVisible;
        int inlineLoudnessClockVisible;
        int hasInlineCollisionVisible;
        int inlineCollisionVisible;
        int hasInlineCollisionSourceKey;
        int inlineCollisionSourceKeyUtf8ByteCount;
        int hasCollisionLevelFloorDb;
        double collisionLevelFloorDb;
        int hasCollisionProminenceThresholdDb;
        double collisionProminenceThresholdDb;
        int hasCollisionRelativeThreshold;
        double collisionRelativeThreshold;
        int hasCollisionFrequencyScale;
        int collisionFrequencyScale;
        int hasSpectrumDisplayFloorDb;
        double spectrumDisplayFloorDb;
        int hasSpectrumDisplayCeilingDb;
        double spectrumDisplayCeilingDb;
        int hasLoudnessClockMinimumDb;
        double loudnessClockMinimumDb;
        int hasLoudnessClockMaximumDb;
        double loudnessClockMaximumDb;
        int hasLoudnessClockWarningDb;
        double loudnessClockWarningDb;
        int hasLoudnessClockCycleSeconds;
        double loudnessClockCycleSeconds;
    };

    struct DlmHostSnapshotChromeCommandAvailability
    {
        int canBackToGlobal;
        int canResetAll;
        int canForgetSelectedSource;
        int canMoveSelectedSourceUp;
        int canMoveSelectedSourceDown;
    };
}

int DlmHostSnapshotChromeProjectionCreate(void** handle);
void DlmHostSnapshotChromeProjectionDestroy(void* handle);
int DlmHostSnapshotChromeProjectionClear(void* handle);
int DlmHostSnapshotChromeProjectionSetGlobalSettings(
    void* handle,
    int schemaVersion,
    const char* themeId,
    int hasMaxRecentEventEntries,
    int maxRecentEventEntries,
    const char* eventOutputFolder,
    int loadDefaultDetailLayoutFromFile,
    const char* defaultDetailLayoutFile,
    int showHiddenSources);
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
    double loudnessClockCycleSeconds);
int DlmHostSnapshotChromeProjectionProject(void* handle, int hasSelectedSourceIndex, int selectedSourceIndex, int sourceCount);
int DlmHostSnapshotChromeProjectionGetSettings(void* handle, DlmHostSnapshotChromeSettings* settings);
int DlmHostSnapshotChromeProjectionGetSourceSettingsCount(void* handle, int* count);
int DlmHostSnapshotChromeProjectionGetSourceSettingsAt(void* handle, int index, DlmHostSnapshotChromeSourceSettingsRow* row);
int DlmHostSnapshotChromeProjectionGetCommandAvailability(void* handle, DlmHostSnapshotChromeCommandAvailability* commands);
int DlmHostSnapshotChromeProjectionCopySettingsString(void* handle, int field, char* buffer, int capacity, int* actualByteCount);
int DlmHostSnapshotChromeProjectionCopySourceSettingsStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
