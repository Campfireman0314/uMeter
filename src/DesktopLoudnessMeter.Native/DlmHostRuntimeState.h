#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostRuntimeStateSnapshot
    {
        int hasSelectedSourceKey;
        int selectedSourceKeyUtf8ByteCount;
        int showHiddenSources;
        int hostPerformanceMode;
    };
}

int DlmHostRuntimeStateCreate(void** handle);
void DlmHostRuntimeStateDestroy(void* handle);
int DlmHostRuntimeStateSetSelectedSourceKey(void* handle, const char* sourceKey);
int DlmHostRuntimeStateClearSelectedSourceKey(void* handle);
int DlmHostRuntimeStateSetShowHiddenSources(void* handle, int showHiddenSources);
int DlmHostRuntimeStateSetPerformanceMode(void* handle, int hostPerformanceMode);
int DlmHostRuntimeStateApplySelectedSourcePlan(
    void* handle,
    int shouldClearSelected,
    int shouldSetSelected,
    const char* selectedSourceKey);
int DlmHostRuntimeStateApplyShowHiddenPlan(void* handle, int showHiddenSources);
int DlmHostRuntimeStateApplyPerformanceModePlan(void* handle, int hostPerformanceMode);
int DlmHostRuntimeStateGetSnapshot(void* handle, DlmHostRuntimeStateSnapshot* snapshot);
int DlmHostRuntimeStateCopySelectedSourceKey(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount);
