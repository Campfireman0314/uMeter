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

constexpr double connectedAckTimeoutMs = 2500.0;
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
    juce::String currentHandle;
    {
        const juce::ScopedLock lock (stateLock);
        currentHandle = currentConfig.handle;
    }

    const auto nextHandle = LoopbackProtocol::isValidHandle (config.handle) ? config.handle : juce::String();
    const auto isSameHandleRestart = active.load (std::memory_order_acquire)
        && nextHandle.isNotEmpty()
        && currentHandle == nextHandle;

    stopInternal (! isSameHandleRestart);

    const juce::ScopedLock lock (stateLock);
    currentConfig = config;
    currentConfig.handle = nextHandle.isNotEmpty() ? nextHandle : LoopbackProtocol::createHandle();
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
    lastAckReceivedMs.store (0.0, std::memory_order_release);

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
    stopInternal (true);
}

void LoopbackSender::stopInternal (bool sendClose)
{
    sendCloseOnStop.store (sendClose, std::memory_order_release);
    if (! active.exchange (false, std::memory_order_acq_rel))
    {
        sendCloseOnStop.store (true, std::memory_order_release);
        return;
    }

    signalThreadShouldExit();
    notify();
    stopThread (1000);
    sendCloseOnStop.store (true, std::memory_order_release);
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

LoopbackSenderConnectionState LoopbackSender::getConnectionState() const noexcept
{
    if (! active.load (std::memory_order_acquire))
    {
        return LoopbackSenderConnectionState::Stopped;
    }

    const auto nowMs = juce::Time::getMillisecondCounterHiRes();
    const auto lastAckMs = lastAckReceivedMs.load (std::memory_order_acquire);
    if (lastAckMs <= 0.0)
    {
        return LoopbackSenderConnectionState::WaitingForReceiver;
    }

    return nowMs - lastAckMs <= connectedAckTimeoutMs
        ? LoopbackSenderConnectionState::Connected
        : LoopbackSenderConnectionState::NoRecentAck;
}

juce::String LoopbackSender::getConnectionStatusText() const
{
    switch (getConnectionState())
    {
        case LoopbackSenderConnectionState::Stopped:
            return "Sender stopped";
        case LoopbackSenderConnectionState::Connected:
            return "Connected to uMeter";
        case LoopbackSenderConnectionState::NoRecentAck:
            return "Sending, no recent reply";
        case LoopbackSenderConnectionState::WaitingForReceiver:
        default:
            return "Waiting for uMeter";
    }
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

        pollForAck (socket, configSnapshot.handle);

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

        pollForAck (socket, configSnapshot.handle);

        wait (5);
    }

    if (sendCloseOnStop.load (std::memory_order_acquire))
    {
        sendPacket (socket, LoopbackProtocol::encodeClose (configSnapshot.handle), configSnapshot.port);
    }

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

void LoopbackSender::pollForAck (juce::DatagramSocket& socket, const juce::String& handle) noexcept
{
    char buffer[2048] {};
    for (auto attempt = 0; attempt < 8; ++attempt)
    {
        if (socket.waitUntilReady (true, 0) != 1)
        {
            return;
        }

        juce::String senderAddress;
        int senderPort = 0;
        const auto bytesRead = socket.read (buffer, sizeof (buffer), false, senderAddress, senderPort);
        if (bytesRead <= 0)
        {
            return;
        }

        if (LoopbackProtocol::isAckForHandle (buffer, bytesRead, handle))
        {
            lastAckReceivedMs.store (juce::Time::getMillisecondCounterHiRes(), std::memory_order_release);
        }
    }
}
}
