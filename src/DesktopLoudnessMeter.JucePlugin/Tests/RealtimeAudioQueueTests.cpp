#include "RealtimeAudioQueue.h"

#include <juce_audio_basics/juce_audio_basics.h>

#include <cmath>
#include <stdexcept>
#include <vector>

namespace
{
void expect (bool condition, const char* message)
{
    if (! condition)
    {
        throw std::runtime_error (message);
    }
}

void expectNear (float actual, float expected, const char* message)
{
    if (std::abs (actual - expected) > 0.000001f)
    {
        throw std::runtime_error (message);
    }
}

juce::AudioBuffer<float> makeStereoBuffer()
{
    juce::AudioBuffer<float> buffer (2, 3);
    buffer.setSample (0, 0, 0.10f);
    buffer.setSample (1, 0, -0.10f);
    buffer.setSample (0, 1, 0.20f);
    buffer.setSample (1, 1, -0.20f);
    buffer.setSample (0, 2, 0.30f);
    buffer.setSample (1, 2, -0.30f);
    return buffer;
}

void audioBuffersAreCopiedAsInterleavedPackets()
{
    dlm::RealtimeAudioQueue queue (4, 16);
    auto buffer = makeStereoBuffer();

    expect (queue.tryPush (buffer, 2), "first enqueue should succeed");

    dlm::QueuedAudioPacket packet;
    expect (queue.tryPop (packet), "queued packet should pop");
    expect (packet.sequence == 1, "first packet sequence should be 1");
    expect (packet.samples.size() == 6, "interleaved sample count mismatch");
    expectNear (packet.samples[0], 0.10f, "sample 0 mismatch");
    expectNear (packet.samples[1], -0.10f, "sample 1 mismatch");
    expectNear (packet.samples[2], 0.20f, "sample 2 mismatch");
    expectNear (packet.samples[3], -0.20f, "sample 3 mismatch");
    expectNear (packet.samples[4], 0.30f, "sample 4 mismatch");
    expectNear (packet.samples[5], -0.30f, "sample 5 mismatch");
}

void fullQueuesDropInsteadOfBlocking()
{
    dlm::RealtimeAudioQueue queue (1, 16);
    auto buffer = makeStereoBuffer();

    expect (queue.tryPush (buffer, 2), "first enqueue should fill the queue");
    expect (! queue.tryPush (buffer, 2), "second enqueue should drop");
    expect (queue.getDroppedPacketCount() == 1, "drop count should increment");
}

void oversizedBlocksAreRejected()
{
    dlm::RealtimeAudioQueue queue (4, 4);
    auto buffer = makeStereoBuffer();

    expect (! queue.tryPush (buffer, 2), "oversized enqueue should fail");
    expect (queue.getDroppedPacketCount() == 1, "oversized block should increment drop count");
}
}

void runRealtimeAudioQueueTests()
{
    audioBuffersAreCopiedAsInterleavedPackets();
    fullQueuesDropInsteadOfBlocking();
    oversizedBlocksAreRejected();
}
