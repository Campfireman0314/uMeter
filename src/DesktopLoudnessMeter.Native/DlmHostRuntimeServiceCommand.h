#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostRuntimeServiceCommandPlan
    {
        int status;
        int shouldResetAll;
        int shouldResetSource;
        int sourceKeyUtf8ByteCount;
        int sourceKind;
        int hasProcessId;
        int processId;
        int hostPerformanceMode;
        int analysisPerformanceMode;
    };
}

int DlmHostRuntimeServiceCommandCreate(void** handle);
void DlmHostRuntimeServiceCommandDestroy(void* handle);
int DlmHostRuntimeServiceCommandClearSources(void* handle);
int DlmHostRuntimeServiceCommandAddSource(void* handle, const char* sourceKey);
int DlmHostRuntimeServiceCommandPlanResetAll(void* handle);
int DlmHostRuntimeServiceCommandPlanResetSource(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId);
int DlmHostRuntimeServiceCommandPlanSetPerformanceMode(void* handle, int hostPerformanceMode);
int DlmHostRuntimeServiceCommandGetPlan(void* handle, DlmHostRuntimeServiceCommandPlan* plan);
int DlmHostRuntimeServiceCommandCopySourceKey(void* handle, char* buffer, int capacity, int* actualByteCount);
