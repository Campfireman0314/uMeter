#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmLoopbackProtocolPacket
    {
        int packetType;
        int sourceKind;
        int sampleRate;
        int channels;
        long long sequence;
        int handleUtf8ByteCount;
        int displayNameUtf8ByteCount;
        int sampleCount;
    };
}

int DlmLoopbackProtocolDecode(
    const unsigned char* bytes,
    int byteCount,
    void** packetHandle,
    DlmLoopbackProtocolPacket* packet);
void DlmLoopbackProtocolDestroyPacket(void* packetHandle);
int DlmLoopbackProtocolCopyString(
    void* packetHandle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount);
int DlmLoopbackProtocolCopySamples(
    void* packetHandle,
    float* buffer,
    int capacity,
    int* actualCount);
int DlmLoopbackProtocolEncodeAck(
    const char* sourceHandle,
    unsigned char* buffer,
    int capacity,
    int* actualByteCount);
