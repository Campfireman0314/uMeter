#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostDetailModuleProjectionRow
    {
        int kind;
        int orderIndex;
        int isVisible;
        double height;
        int titleUtf8ByteCount;
    };
}

int DlmHostDetailModuleProjectionCreate(void** handle);
void DlmHostDetailModuleProjectionDestroy(void* handle);
int DlmHostDetailModuleProjectionClear(void* handle);
int DlmHostDetailModuleProjectionAddLayout(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height);
int DlmHostDetailModuleProjectionProject(
    void* handle,
    int hasStereoVisible,
    int stereoVisible,
    int hasCollisionVisible,
    int collisionVisible,
    int hasLoudnessClockVisible,
    int loudnessClockVisible,
    int hasWaveformVisible,
    int waveformVisible,
    int hasSpectrumVisible,
    int spectrumVisible);
int DlmHostDetailModuleProjectionGetCount(void* handle, int* count);
int DlmHostDetailModuleProjectionGetAt(void* handle, int index, DlmHostDetailModuleProjectionRow* row);
int DlmHostDetailModuleProjectionCopyStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
