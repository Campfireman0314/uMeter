#include "RealtimeAudioQueue.h"

#include <algorithm>

namespace dlm
{
RealtimeAudioQueue::RealtimeAudioQueue (std::size_t capacity, int maxSamplesPerPacket)
    : slots (std::max<std::size_t> (capacity, 1)),
      maxSamples (std::max (maxSamplesPerPacket, 1))
{
    for (auto& slot : slots)
    {
        slot.samples.resize (static_cast<std::size_t> (maxSamples));
    }
}

bool RealtimeAudioQueue::tryPush (const juce::AudioBuffer<float>& buffer, int channelCount) noexcept
{
    if (channelCount <= 0 || channelCount > buffer.getNumChannels() || buffer.getNumSamples() <= 0)
    {
        droppedPackets.fetch_add (1, std::memory_order_relaxed);
        return false;
    }

    const auto sampleCount = buffer.getNumSamples() * channelCount;
    if (sampleCount > maxSamples)
    {
        droppedPackets.fetch_add (1, std::memory_order_relaxed);
        return false;
    }

    const auto write = writeIndex.load (std::memory_order_relaxed);
    const auto read = readIndex.load (std::memory_order_acquire);

    if (write - read >= slots.size())
    {
        droppedPackets.fetch_add (1, std::memory_order_relaxed);
        return false;
    }

    auto& slot = slots[write % slots.size()];
    auto output = slot.samples.data();

    for (auto sample = 0; sample < buffer.getNumSamples(); ++sample)
    {
        for (auto channel = 0; channel < channelCount; ++channel)
        {
            *output++ = buffer.getSample (channel, sample);
        }
    }

    slot.sampleCount = sampleCount;
    slot.sequence = nextSequence.fetch_add (1, std::memory_order_relaxed) + 1;

    writeIndex.store (write + 1, std::memory_order_release);
    return true;
}

bool RealtimeAudioQueue::tryPop (QueuedAudioPacket& packet)
{
    const auto read = readIndex.load (std::memory_order_relaxed);
    const auto write = writeIndex.load (std::memory_order_acquire);

    if (read == write)
    {
        return false;
    }

    auto& slot = slots[read % slots.size()];
    packet.sequence = slot.sequence;
    packet.samples.assign (slot.samples.begin(), slot.samples.begin() + slot.sampleCount);

    readIndex.store (read + 1, std::memory_order_release);
    return true;
}

void RealtimeAudioQueue::clear() noexcept
{
    readIndex.store (writeIndex.load (std::memory_order_acquire), std::memory_order_release);
}

std::uint64_t RealtimeAudioQueue::getDroppedPacketCount() const noexcept
{
    return droppedPackets.load (std::memory_order_relaxed);
}

int RealtimeAudioQueue::getMaxSamplesPerPacket() const noexcept
{
    return maxSamples;
}
}
