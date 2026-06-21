#pragma once

#include <juce_core/juce_core.h>

#include <cstdint>

namespace dlm
{
class LoopbackProtocol final
{
public:
    static constexpr const char* protocolVersion = "desktop-loudness-meter.loopback.v1";
    static constexpr int defaultPort = 38173;

    static juce::String createHandle();
    static bool isValidHandle (const juce::String& handle);

    static juce::MemoryBlock encodeRegister (const juce::String& handle,
                                             const juce::String& displayName,
                                             int sampleRate,
                                             int channels);

    static juce::MemoryBlock encodeHeartbeat (const juce::String& handle);
    static juce::MemoryBlock encodeRename (const juce::String& handle, const juce::String& displayName);
    static juce::MemoryBlock encodeAudio (const juce::String& handle, std::int64_t sequence, const float* samples, int sampleCount);
    static juce::MemoryBlock encodeClose (const juce::String& handle);
    static juce::MemoryBlock encodeAck (const juce::String& handle);
    static bool isAckForHandle (const void* data, int sizeInBytes, const juce::String& handle);
};
}
