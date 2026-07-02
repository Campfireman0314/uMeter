#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmSourceIngestSnapshot
    {
        int sourceKind;
        int sampleRate;
        int channels;
        long long lastSeenMilliseconds;
        float peak;
        int handleUtf8ByteCount;
        int displayNameUtf8ByteCount;
        DlmPcmAnalyzerSnapshot metrics;
    };
}

int DlmSourceIngestCoreCreate(void** handle);
void DlmSourceIngestCoreDestroy(void* handle);
int DlmSourceIngestCoreRegister(
    void* handle,
    const char* sourceHandle,
    const char* displayName,
    int sourceKind,
    int sampleRate,
    int channels,
    long long timestampMilliseconds);
int DlmSourceIngestCoreHeartbeat(void* handle, const char* sourceHandle, long long timestampMilliseconds);
int DlmSourceIngestCoreRename(void* handle, const char* sourceHandle, const char* displayName, long long timestampMilliseconds);
int DlmSourceIngestCoreProcessF32(
    void* handle,
    const char* sourceHandle,
    const float* interleavedSamples,
    int sampleCount,
    long long timestampMilliseconds);
int DlmSourceIngestCoreClose(void* handle, const char* sourceHandle);
int DlmSourceIngestCoreRemoveExpired(void* handle, long long nowMilliseconds, long long timeoutMilliseconds);
int DlmSourceIngestCoreResetSource(void* handle, const char* sourceHandle);
int DlmSourceIngestCoreResetAll(void* handle);
int DlmSourceIngestCoreSetTruePeakOversampling(void* handle, const char* sourceHandle, int oversampling);
int DlmSourceIngestCoreSetChannelWaveformEnabled(void* handle, const char* sourceHandle, int enabled);
int DlmSourceIngestCoreSetChannelSpectrumEnabled(void* handle, const char* sourceHandle, int enabled);
int DlmSourceIngestCoreSetPerformanceMode(void* handle, int mode);
int DlmSourceIngestCoreGetSourceCount(void* handle, int* count);
int DlmSourceIngestCoreGetSourceSnapshotAt(void* handle, int index, DlmSourceIngestSnapshot* snapshot);
int DlmSourceIngestCoreCopySourceStringAt(void* handle, int index, int field, char* buffer, int capacity, int* actualByteCount);
int DlmSourceIngestCoreCopyWaveformAt(void* handle, int index, double* buffer, int capacity, int* actualCount);
int DlmSourceIngestCoreCopyChannelWaveformAt(void* handle, int index, int channel, double* buffer, int capacity, int* actualCount);
int DlmSourceIngestCoreCopySpectrumAt(void* handle, int index, double* buffer, int capacity, int* actualCount);
int DlmSourceIngestCoreCopyChannelSpectrumAt(void* handle, int index, int channel, double* buffer, int capacity, int* actualCount);
int DlmSourceIngestCoreCopyStereoImagePointsAt(
    void* handle,
    int index,
    double* angleBuffer,
    double* radiusBuffer,
    int capacity,
    int* actualCount);
