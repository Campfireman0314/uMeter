#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostSourceProjectionRow
    {
        int originalSourceIndex;
        int sourceKind;
        int processId;
        int hasProcessId;
        int isHidden;
        int orderIndex;
        int hasOrderIndex;
        int truePeakOversampling;
        int waveformDisplayMode;
        int spectrumDisplayMode;
        int sourceKeyUtf8ByteCount;
        int sourceKindLabelUtf8ByteCount;
        int displayNameUtf8ByteCount;
        int aliasUtf8ByteCount;
        int effectiveDisplayNameUtf8ByteCount;
        int displayTitleUtf8ByteCount;
        int sourceIdentityTextUtf8ByteCount;
    };
}

int DlmHostSourceProjectionCreate(void** handle);
void DlmHostSourceProjectionDestroy(void* handle);
int DlmHostSourceProjectionClear(void* handle);
int DlmHostSourceProjectionAddSource(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId,
    const char* displayName,
    const char* alias,
    int isHidden,
    int hasOrderIndex,
    int orderIndex,
    int truePeakOversampling,
    int waveformDisplayMode,
    int spectrumDisplayMode);
int DlmHostSourceProjectionProject(void* handle, int showHiddenSources, const char* selectedSourceKey);
int DlmHostSourceProjectionGetProjectedCount(void* handle, int* count);
int DlmHostSourceProjectionIsSelectedSourceVisible(void* handle, int* isVisible);
int DlmHostSourceProjectionGetProjectedAt(void* handle, int index, DlmHostSourceProjectionRow* row);
int DlmHostSourceProjectionCopyProjectedStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
