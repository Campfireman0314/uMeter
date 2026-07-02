#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostCollisionBand
    {
        double startRatio;
        double endRatio;
        double intensity;
    };
}

int DlmHostCollisionProjectionCreate(void** handle);
void DlmHostCollisionProjectionDestroy(void* handle);
int DlmHostCollisionProjectionClear(void* handle);
int DlmHostCollisionProjectionSetFocusSpectrum(void* handle, const double* values, int count);
int DlmHostCollisionProjectionAddComparisonSpectrum(void* handle, const double* values, int count);
int DlmHostCollisionProjectionProject(
    void* handle,
    double levelFloorDb,
    double prominenceThresholdDb,
    double relativeThreshold,
    int frequencyScale);
int DlmHostCollisionProjectionGetBandCount(void* handle, int* count);
int DlmHostCollisionProjectionGetBandAt(void* handle, int index, DlmHostCollisionBand* band);
