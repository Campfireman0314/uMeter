#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostAggregateSnapshotPlan
    {
        int hasSelectedSource;
        int selectedSourceIndex;
        int includeSelectedDetail;
        int statusTextUtf8ByteCount;
    };
}

int DlmHostAggregateSnapshotCreate(void** handle);
void DlmHostAggregateSnapshotDestroy(void* handle);
int DlmHostAggregateSnapshotClear(void* handle);
int DlmHostAggregateSnapshotAddSource(void* handle, const char* sourceKey);
int DlmHostAggregateSnapshotProject(void* handle, const char* selectedSourceKey);
int DlmHostAggregateSnapshotGetPlan(void* handle, DlmHostAggregateSnapshotPlan* plan);
int DlmHostAggregateSnapshotCopyStatusText(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount);
