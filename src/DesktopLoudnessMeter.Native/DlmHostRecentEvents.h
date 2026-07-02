#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostRecentEventRow
    {
        int kind;
        int severity;
        int timeTextUtf8ByteCount;
        int titleUtf8ByteCount;
        int detailUtf8ByteCount;
    };
}

int DlmHostRecentEventsCreate(void** handle);
void DlmHostRecentEventsDestroy(void* handle);
int DlmHostRecentEventsClearCurrentSources(void* handle);
int DlmHostRecentEventsAddLiveSource(void* handle, const char* sourceKey);
int DlmHostRecentEventsAddVisibleSource(void* handle, const char* sourceKey, const char* displayTitle);
int DlmHostRecentEventsReconcile(void* handle, const char* timeText, int maxRecentEventEntries);
int DlmHostRecentEventsGetCount(void* handle, int* count);
int DlmHostRecentEventsGetAt(void* handle, int index, DlmHostRecentEventRow* row);
int DlmHostRecentEventsCopyStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
