#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSourceOrderCommandRow
    {
        int sourceKeyUtf8ByteCount;
        int orderIndex;
    };
}

int DlmHostSourceOrderCommandCreate(void** handle);
void DlmHostSourceOrderCommandDestroy(void* handle);
int DlmHostSourceOrderCommandClear(void* handle);
int DlmHostSourceOrderCommandAddSource(void* handle, const char* sourceKey);
int DlmHostSourceOrderCommandPlanMove(void* handle, const char* sourceKey, int direction);
int DlmHostSourceOrderCommandPlanMoveToIndex(void* handle, const char* sourceKey, int targetIndex);
int DlmHostSourceOrderCommandGetStatus(void* handle, int* status);
int DlmHostSourceOrderCommandGetRowCount(void* handle, int* count);
int DlmHostSourceOrderCommandGetRow(void* handle, int index, DlmHostSourceOrderCommandRow* row);
int DlmHostSourceOrderCommandCopyRowSourceKey(
    void* handle,
    int index,
    char* buffer,
    int capacity,
    int* actualByteCount);
