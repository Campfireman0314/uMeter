#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSelectedSourceCommandPlan
    {
        int status;
        int shouldClearSelected;
        int shouldSetSelected;
        int selectedSourceKeyUtf8ByteCount;
    };
}

int DlmHostSelectedSourceCommandCreate(void** handle);
void DlmHostSelectedSourceCommandDestroy(void* handle);
int DlmHostSelectedSourceCommandClearSources(void* handle);
int DlmHostSelectedSourceCommandAddSource(void* handle, const char* sourceKey);
int DlmHostSelectedSourceCommandPlanSelect(void* handle, const char* sourceKey);
int DlmHostSelectedSourceCommandPlanBackToGlobal(void* handle);
int DlmHostSelectedSourceCommandPlanReconcile(void* handle, const char* selectedSourceKey);
int DlmHostSelectedSourceCommandPlanHiddenSource(
    void* handle,
    const char* sourceKey,
    int isHidden,
    int showHiddenSources,
    const char* selectedSourceKey);
int DlmHostSelectedSourceCommandGetPlan(void* handle, DlmHostSelectedSourceCommandPlan* plan);
int DlmHostSelectedSourceCommandCopySelectedSourceKey(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount);
