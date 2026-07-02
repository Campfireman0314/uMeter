#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSourceTargetCommandPlan
    {
        int status;
        int sourceKeyUtf8ByteCount;
    };
}

int DlmHostSourceTargetCommandCreate(void** handle);
void DlmHostSourceTargetCommandDestroy(void* handle);
int DlmHostSourceTargetCommandClearSources(void* handle);
int DlmHostSourceTargetCommandAddSource(void* handle, const char* sourceKey);
int DlmHostSourceTargetCommandPlanTarget(void* handle, const char* sourceKey);
int DlmHostSourceTargetCommandGetPlan(void* handle, DlmHostSourceTargetCommandPlan* plan);
int DlmHostSourceTargetCommandCopySourceKey(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount);
