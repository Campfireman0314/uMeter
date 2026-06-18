#pragma once

#include "RealtimeAudioQueue.h"

#include <juce_core/juce_core.h>

#include <atomic>
#include <memory>

namespace dlm
{
struct LoopbackSenderConfig
{
    juce::String handle;
    juce::String displayName;
    int sampleRate = 48000;
    int channels = 2;
    int port = 38173;
    int heartbeatIntervalMs = 1000;
    int registerIntervalMs = 1000;
    std::size_t queueCapacity = 64;
    int maxSamplesPerPacket = 8192;
};

class LoopbackSender final : private juce::Thread
{
public:
    LoopbackSender();
    ~LoopbackSender() override;

    void start (const LoopbackSenderConfig& config);
    void stop();
    void rename (const juce::String& displayName);

    bool tryEnqueue (const juce::AudioBuffer<float>& buffer, int channelCount) noexcept;
    bool isActive() const noexcept;
    std::uint64_t getDroppedPacketCount() const noexcept;

private:
    void run() override;
    void sendPacket (juce::DatagramSocket& socket, const juce::MemoryBlock& packet, int port) const;

    mutable juce::CriticalSection stateLock;
    LoopbackSenderConfig currentConfig;
    juce::String pendingRename;
    std::unique_ptr<RealtimeAudioQueue> audioQueue;
    std::atomic<bool> active { false };
    std::atomic<bool> renamePending { false };
};
}
