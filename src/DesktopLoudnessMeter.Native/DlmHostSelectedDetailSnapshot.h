#pragma once

#include "DlmHostCollisionProjection.h"
#include "DlmHostDetailModuleProjection.h"
#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSelectedDetailSnapshotPlan
    {
        int hasSelectedSource;
        int selectedSourceIndex;
        int isWaveformVisible;
        int isSpectrumVisible;
        int isStereoVisible;
        int isLoudnessClockVisible;
        int isCollisionVisible;
        int comparisonSourceKeyUtf8ByteCount;
        int comparisonDisplayTitleUtf8ByteCount;
        double levelFloorDb;
        double prominenceThresholdDb;
        double relativeThreshold;
        int frequencyScale;
    };
}

int DlmHostSelectedDetailSnapshotCreate(void** handle);
void DlmHostSelectedDetailSnapshotDestroy(void* handle);
int DlmHostSelectedDetailSnapshotClear(void* handle);
int DlmHostSelectedDetailSnapshotAddSource(
    void* handle,
    const char* sourceKey,
    const char* displayTitle,
    const double* spectrumValues,
    int spectrumCount);
int DlmHostSelectedDetailSnapshotAddModuleLayout(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height);
int DlmHostSelectedDetailSnapshotProject(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasStereoVisible,
    int stereoVisible,
    int hasCollisionVisible,
    int collisionVisible,
    int hasLoudnessClockVisible,
    int loudnessClockVisible,
    int hasWaveformVisible,
    int waveformVisible,
    int hasSpectrumVisible,
    int spectrumVisible,
    int hasLevelFloorDb,
    double levelFloorDb,
    int hasProminenceThresholdDb,
    double prominenceThresholdDb,
    int hasRelativeThreshold,
    double relativeThreshold,
    int hasFrequencyScale,
    int frequencyScale);
int DlmHostSelectedDetailSnapshotGetPlan(void* handle, DlmHostSelectedDetailSnapshotPlan* plan);
int DlmHostSelectedDetailSnapshotGetModuleCount(void* handle, int* count);
int DlmHostSelectedDetailSnapshotGetModuleAt(void* handle, int index, DlmHostDetailModuleProjectionRow* row);
int DlmHostSelectedDetailSnapshotCopyModuleStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
int DlmHostSelectedDetailSnapshotGetBandCount(void* handle, int* count);
int DlmHostSelectedDetailSnapshotGetBandAt(void* handle, int index, DlmHostCollisionBand* band);
int DlmHostSelectedDetailSnapshotCopyString(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
