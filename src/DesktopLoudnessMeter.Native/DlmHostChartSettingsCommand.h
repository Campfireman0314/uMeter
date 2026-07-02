#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostChartSettingsCommandPlan
    {
        int truePeakOversampling;
        int truePeakServiceOversampling;
        int waveformDisplayMode;
        int waveformChannelEnabled;
        int spectrumDisplayMode;
        int spectrumChannelEnabled;
        int detailModuleKind;
        int detailModuleVisible;
        double spectrumDisplayFloorDb;
        double spectrumDisplayCeilingDb;
        double loudnessClockMinimumDb;
        double loudnessClockMaximumDb;
        double loudnessClockWarningDb;
        double loudnessClockCycleSeconds;
        double collisionLevelFloorDb;
        double collisionProminenceThresholdDb;
        double collisionRelativeThreshold;
        int collisionFrequencyScale;
        int hasCollisionComparisonSourceKey;
        int collisionComparisonSourceKeyUtf8ByteCount;
    };
}

int DlmHostChartSettingsCommandCreate(void** handle);
void DlmHostChartSettingsCommandDestroy(void* handle);
int DlmHostChartSettingsCommandPlanSetTruePeakOversampling(void* handle, int oversampling);
int DlmHostChartSettingsCommandPlanSetWaveformDisplayMode(void* handle, int displayMode);
int DlmHostChartSettingsCommandPlanSetSpectrumDisplayMode(void* handle, int displayMode);
int DlmHostChartSettingsCommandPlanSetDetailModuleVisibility(void* handle, int moduleKind, int isVisible);
int DlmHostChartSettingsCommandPlanSetSpectrumDisplayRange(void* handle, double floorDb, double ceilingDb);
int DlmHostChartSettingsCommandPlanSetLoudnessClockSettings(
    void* handle,
    double minimumDb,
    double maximumDb,
    double warningDb,
    double cycleSeconds);
int DlmHostChartSettingsCommandPlanSetCollisionSettings(
    void* handle,
    double levelFloorDb,
    double prominenceThresholdDb,
    double relativeThreshold,
    int frequencyScale,
    const char* comparisonSourceKey);
int DlmHostChartSettingsCommandGetPlan(void* handle, DlmHostChartSettingsCommandPlan* plan);
int DlmHostChartSettingsCommandCopyCollisionComparisonSourceKey(void* handle, char* buffer, int capacity, int* actualByteCount);
