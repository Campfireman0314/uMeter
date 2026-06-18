#include "LoopbackSender.h"

#include "LoopbackProtocol.h"
#include "PluginState.h"

#include <algorithm>

namespace
{
int sanitizeChannelCount (int channels)
{
    return juce::jlimit (1, 2, channels);
}

int sanitizeSampleRate (int sampleRate)
{
    return juce::jlimit (8000, 384000, sampleRate);
}

int sanitizeHeartbeatInterval (int intervalMs)
{
    return std::max (intervalMs, 10);
}

int sanitizeRegisterInterval (int intervalMs)
{
    return std::max (intervalMs, 100);
}

juce::String normalizeDisplayName (const juce::String& displayName)
{
    const auto trimmed = displayName.trim();
    return trimmed.isEmpty() ? dlm::PluginState::defaultDisplayName : trimmed;
}
}

namespace dlm
{
LoopbackSender::LoopbackSender()
    : Thread ("uMeter Loopback Sender")
{
}

LoopbackSender::~LoopbackSender()
{
    stop();
}

void LoopbackSender::start (const LoopbackSenderConfig& config)
{
    stop();

    const juce::ScopedLock lock (stateLock);
    currentConfig = config;
    currentConfig.handle = LoopbackProtocol::isValidHandle (config.handle) ? config.handle : LoopbackProtocol::createHandle();
    currentConfig.displayName = normalizeDisplayName (config.displayName);
    currentConfig.sampleRate = sanitizeSampleRate (config.sampleRate);
    currentConfig.channels = sanitizeChannelCount (config.channels);
    currentConfig.port = config.port <= 0 ? LoopbackProtocol::defaultPort : config.port;
    currentConfig.heartbeatIntervalMs = sanitizeHeartbeatInterval (config.heartbeatIntervalMs);
    currentConfig.registerIntervalMs = sanitizeRegisterInterval (config.registerIntervalMs);
    currentConfig.queueCapacity = std::max<std::size_t> (config.queueCapacity, 1);
    currentConfig.maxSamplesPerPacket = std::max (config.maxSamplesPerPacket, currentConfig.channels);
    pendingRename = {};
    renamePending.store (false, std::memory_order_release);

    audioQueue = std::make_unique<RealtimeAudioQueue> (currentConfig.queueCapacity, currentConfig.maxSamplesPerPacket);

    if (startThread())
    {
        active.store (true, std::memory_order_release);
    }
    else
    {
        audioQueue.reset();
    }
}

void LoopbackSender::stop()
{
    if (! active.exchange (false, std::memory_order_acq_rel))
    {
        return;
    }

    signalThreadShouldExit();
    notify();
    stopThread (1000);
}

void LoopbackSender::rename (const juce::String& displayName)
{
    const auto normalized = normalizeDisplayName (displayName);

    {
        const juce::ScopedLock lock (stateLock);
        currentConfig.displayName = normalized;
        pendingRename = normalized;
    }

    if (active.load (std::memory_order_acquire))
    {
        renamePending.store (true, std::memory_order_release);
        notify();
    }
}

bool LoopbackSender::tryEnqueue (const juce::AudioBuffer<float>& buffer, int channelCount) noexcept
{
    if (! active.load (std::memory_order_acquire) || audioQueue == nullptr)
    {
        return false;
    }

    const auto accepted = audioQueue->tryPush (buffer, channelCount);
    if (accepted)
    {
        notify();
    }

    return accepted;
}

bool LoopbackSender::isActive() const noexcept
{
    return active.load (std::memory_order_acquire);
}

std::uint64_t LoopbackSender::getDroppedPacketCount() const noexcept
{
    return audioQueue == nullptr ? 0 : audioQueue->getDroppedPacketCount();
}

void LoopbackSender::run()
{
    LoopbackSenderConfig configSnapshot;
    {
        const juce::ScopedLock lock (stateLock);
        configSnapshot = currentConfig;
    }

    juce::DatagramSocket socket;
    socket.bindToPort (0, "127.0.0.1");

    sendPacket (socket, LoopbackProtocol::encodeRegister (configSnapshot.handle,
                                                          configSnapshot.displayName,
                                                          configSnapshot.sampleRate,
                                                          configSnapshot.channels),
                configSnapshot.port);

    auto lastHeartbeatMs = juce::Time::getMillisecondCounterHiRes();
    auto lastRegisterMs = lastHeartbeatMs;
    QueuedAudioPacket packet;

    while (! threadShouldExit())
    {
        while (audioQueue != nullptr && audioQueue->tryPop (packet))
        {
            sendPacket (socket, LoopbackProtocol::encodeAudio (configSnapshot.handle,
                                                               packet.sequence,
                                                               packet.samples.data(),
                                                               static_cast<int> (packet.samples.size())),
                        configSnapshot.port);
        }

        if (renamePending.exchange (false, std::memory_order_acq_rel))
        {
            juce::String renameValue;
            {
                const juce::ScopedLock lock (stateLock);
                renameValue = pendingRename;
            }

            configSnapshot.displayName = renameValue;
            sendPacket (socket, LoopbackProtocol::encodeRename (configSnapshot.handle, renameValue),
                        configSnapshot.port);
        }

        const auto nowMs = juce::Time::getMillisecondCounterHiRes();
        if (nowMs - lastRegisterMs >= configSnapshot.registerIntervalMs)
        {
            sendPacket (socket, LoopbackProtocol::encodeRegister (configSnapshot.handle,
                                                                  configSnapshot.displayName,
                                                                  configSnapshot.sampleRate,
                                                                  configSnapshot.channels),
                        configSnapshot.port);
            lastRegisterMs = nowMs;
        }

        if (nowMs - lastHeartbeatMs >= configSnapshot.heartbeatIntervalMs)
        {
            sendPacket (socket, LoopbackProtocol::encodeHeartbeat (configSnapshot.handle), configSnapshot.port);
            lastHeartbeatMs = nowMs;
        }

        wait (5);
    }

    sendPacket (socket, LoopbackProtocol::encodeClose (configSnapshot.handle), configSnapshot.port);
    socket.shutdown();
}

void LoopbackSender::sendPacket (juce::DatagramSocket& socket, const juce::MemoryBlock& packet, int port) const
{
    if (packet.getSize() == 0)
    {
        return;
    }

    socket.write ("127.0.0.1",
                  port,
                  packet.getData(),
                  static_cast<int> (packet.getSize()));
}
}
