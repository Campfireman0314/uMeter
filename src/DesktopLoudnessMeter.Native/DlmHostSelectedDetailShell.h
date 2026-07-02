#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSelectedDetailShellPlan
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

int DlmHostSelectedDetailShellCreate(void** handle);
void DlmHostSelectedDetailShellDestroy(void* handle);
int DlmHostSelectedDetailShellClear(void* handle);
int DlmHostSelectedDetailShellAddSource(void* handle, const char* sourceKey, const char* displayTitle);
int DlmHostSelectedDetailShellAddModule(void* handle, int kind, int isVisible);
int DlmHostSelectedDetailShellProject(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasLevelFloorDb,
    double levelFloorDb,
    int hasProminenceThresholdDb,
    double prominenceThresholdDb,
    int hasRelativeThreshold,
    double relativeThreshold,
    int hasFrequencyScale,
    int frequencyScale);
int DlmHostSelectedDetailShellGetPlan(void* handle, DlmHostSelectedDetailShellPlan* plan);
int DlmHostSelectedDetailShellGetComparisonIndexCount(void* handle, int* count);
int DlmHostSelectedDetailShellGetComparisonIndexAt(void* handle, int index, int* sourceIndex);
int DlmHostSelectedDetailShellCopyString(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
