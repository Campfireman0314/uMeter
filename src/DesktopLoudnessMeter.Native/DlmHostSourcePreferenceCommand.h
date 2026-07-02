#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSourcePreferenceCommandPlan
    {
        int status;
        int hasAlias;
        int aliasUtf8ByteCount;
        int isHidden;
        int shouldForget;
        int shouldClearSelected;
    };
}

int DlmHostSourcePreferenceCommandCreate(void** handle);
void DlmHostSourcePreferenceCommandDestroy(void* handle);
int DlmHostSourcePreferenceCommandClearSources(void* handle);
int DlmHostSourcePreferenceCommandAddSource(void* handle, const char* sourceKey);
int DlmHostSourcePreferenceCommandPlanSetAlias(void* handle, const char* sourceKey, const char* alias);
int DlmHostSourcePreferenceCommandPlanSetHidden(void* handle, const char* sourceKey, int isHidden);
int DlmHostSourcePreferenceCommandPlanForget(void* handle, const char* sourceKey, const char* selectedSourceKey);
int DlmHostSourcePreferenceCommandGetPlan(void* handle, DlmHostSourcePreferenceCommandPlan* plan);
int DlmHostSourcePreferenceCommandCopyAlias(void* handle, char* buffer, int capacity, int* actualByteCount);
