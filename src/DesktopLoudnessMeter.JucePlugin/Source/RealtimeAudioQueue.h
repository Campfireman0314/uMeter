#pragma once

#include <juce_audio_basics/juce_audio_basics.h>

#include <atomic>
#include <cstdint>
#include <vector>

namespace dlm
{
struct QueuedAudioPacket
{
    std::int64_t sequence = 0;
    std::vector<float> samples;
};

class RealtimeAudioQueue final
{
public:
    RealtimeAudioQueue (std::size_t capacity, int maxSamplesPerPacket);

    bool tryPush (const juce::AudioBuffer<float>& buffer, int channelCount) noexcept;
    bool tryPop (QueuedAudioPacket& packet);
    void clear() noexcept;

    std::uint64_t getDroppedPacketCount() const noexcept;
    int getMaxSamplesPerPacket() const noexcept;

private:
    struct Slot
    {
        std::int64_t sequence = 0;
        int sampleCount = 0;
        std::vector<float> samples;
    };

    std::vector<Slot> slots;
    int maxSamples = 0;
    std::atomic<std::size_t> writeIndex { 0 };
    std::atomic<std::size_t> readIndex { 0 };
    std::atomic<std::int64_t> nextSequence { 0 };
    std::atomic<std::uint64_t> droppedPackets { 0 };
};
}
