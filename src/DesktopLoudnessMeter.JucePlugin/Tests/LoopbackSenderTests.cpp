#include "LoopbackSender.h"
#include "LoopbackProtocol.h"

#include <juce_audio_basics/juce_audio_basics.h>
#include <juce_core/juce_core.h>

#include <stdexcept>

namespace
{
void expect (bool condition, const char* message)
{
    if (! condition)
    {
        throw std::runtime_error (message);
    }
}

juce::String memoryToString (const juce::MemoryBlock& block)
{
    return juce::String::fromUTF8 (static_cast<const char*> (block.getData()), static_cast<int> (block.getSize()));
}

juce::var parsePacket (const juce::MemoryBlock& block)
{
    auto parsed = juce::JSON::parse (memoryToString (block));
    expect (! parsed.isVoid(), "received packet should parse as JSON");
    return parsed;
}

juce::String property (const juce::var& packet, const char* name)
{
    auto* object = packet.getDynamicObject();
    expect (object != nullptr, "packet should be a JSON object");
    return object->getProperty (name).toString();
}

struct ReceivedPacket
{
    juce::MemoryBlock block;
    juce::String senderAddress;
    int senderPort = 0;
};

ReceivedPacket readPacketWithSender (juce::DatagramSocket& socket, int timeoutMs)
{
    expect (socket.waitUntilReady (true, timeoutMs) == 1, "timed out waiting for sender packet");

    char buffer[8192] {};
    juce::String senderAddress;
    int senderPort = 0;
    const auto bytesRead = socket.read (buffer, sizeof (buffer), false, senderAddress, senderPort);
    expect (bytesRead > 0, "sender packet read failed");

    return { juce::MemoryBlock (buffer, static_cast<std::size_t> (bytesRead)), senderAddress, senderPort };
}

juce::MemoryBlock readPacket (juce::DatagramSocket& socket, int timeoutMs)
{
    return readPacketWithSender (socket, timeoutMs).block;
}

juce::var readUntilType (juce::DatagramSocket& socket, const char* expectedType, int attempts)
{
    for (auto attempt = 0; attempt < attempts; ++attempt)
    {
        auto packet = parsePacket (readPacket (socket, 1000));
        if (property (packet, "type") == expectedType)
        {
            return packet;
        }
    }

    throw std::runtime_error ("expected packet type did not arrive");
}

ReceivedPacket readPacketWithSenderUntilType (juce::DatagramSocket& socket, const char* expectedType, int attempts)
{
    for (auto attempt = 0; attempt < attempts; ++attempt)
    {
        auto received = readPacketWithSender (socket, 1000);
        auto packet = parsePacket (received.block);
        if (property (packet, "type") == expectedType)
        {
            return received;
        }
    }

    throw std::runtime_error ("expected packet type did not arrive");
}

juce::var readUntilRegisterWithoutClose (juce::DatagramSocket& socket, int attempts)
{
    for (auto attempt = 0; attempt < attempts; ++attempt)
    {
        auto packet = parsePacket (readPacket (socket, 1000));
        const auto type = property (packet, "type");
        expect (type != "close", "same-handle sender restart must not emit close before register");
        if (type == "register")
        {
            return packet;
        }
    }

    throw std::runtime_error ("expected register packet did not arrive");
}

void waitUntilConnected (const dlm::LoopbackSender& sender)
{
    for (auto attempt = 0; attempt < 100; ++attempt)
    {
        if (sender.getConnectionState() == dlm::LoopbackSenderConnectionState::Connected)
        {
            return;
        }

        juce::Thread::sleep (10);
    }

    throw std::runtime_error ("sender did not observe receiver ack");
}

juce::AudioBuffer<float> makeAudioBlock()
{
    juce::AudioBuffer<float> buffer (2, 2);
    buffer.setSample (0, 0, 0.25f);
    buffer.setSample (1, 0, -0.25f);
    buffer.setSample (0, 1, 0.50f);
    buffer.setSample (1, 1, -0.50f);
    return buffer;
}

void senderEmitsLifecyclePacketsFromWorker()
{
    juce::DatagramSocket receiver;
    expect (receiver.bindToPort (0, "127.0.0.1"), "test UDP receiver should bind");

    dlm::LoopbackSender sender;
    dlm::LoopbackSenderConfig config;
    config.handle = "juce-vst3-sender-test";
    config.displayName = "Sender Test";
    config.sampleRate = 48000;
    config.channels = 2;
    config.port = receiver.getBoundPort();
    config.heartbeatIntervalMs = 25;
    config.registerIntervalMs = 25;
    config.queueCapacity = 4;
    config.maxSamplesPerPacket = 16;

    sender.start (config);

    const auto registration = readUntilType (receiver, "register", 3);
    expect (property (registration, "sourceKind") == "plugin", "sender should register as plugin source");
    expect (property (registration, "handle") == config.handle, "sender register handle mismatch");

    const auto heartbeat = readUntilType (receiver, "heartbeat", 5);
    expect (property (heartbeat, "handle") == config.handle, "sender heartbeat handle mismatch");

    auto block = makeAudioBlock();
    expect (sender.tryEnqueue (block, 2), "sender should enqueue small audio block");

    const auto audio = readUntilType (receiver, "audio", 5);
    expect (property (audio, "samplesBase64").isNotEmpty(), "audio packet should include samples");

    sender.rename ("Renamed Sender Test");

    const auto rename = readUntilType (receiver, "rename", 5);
    expect (property (rename, "displayName") == "Renamed Sender Test", "sender rename display name mismatch");
    expect (property (rename, "handle") == config.handle, "sender rename handle mismatch");

    const auto repeatedRegistration = readUntilType (receiver, "register", 5);
    expect (property (repeatedRegistration, "displayName") == "Renamed Sender Test", "repeated register should use latest display name");
    expect (property (repeatedRegistration, "handle") == config.handle, "repeated register handle mismatch");

    sender.stop();

    const auto close = readUntilType (receiver, "close", 5);
    expect (property (close, "handle") == config.handle, "sender close handle mismatch");
    expect (sender.getConnectionState() == dlm::LoopbackSenderConnectionState::Stopped, "sender should report stopped after stop");
}

void senderReportsConnectedAfterReceiverAck()
{
    juce::DatagramSocket receiver;
    expect (receiver.bindToPort (0, "127.0.0.1"), "test UDP receiver should bind");

    dlm::LoopbackSender sender;
    dlm::LoopbackSenderConfig config;
    config.handle = "juce-vst3-ack-test";
    config.displayName = "Ack Test";
    config.sampleRate = 48000;
    config.channels = 2;
    config.port = receiver.getBoundPort();
    config.heartbeatIntervalMs = 25;
    config.registerIntervalMs = 25;

    sender.start (config);
    expect (sender.getConnectionState() == dlm::LoopbackSenderConnectionState::WaitingForReceiver, "sender should wait before first ack");

    const auto registration = readPacketWithSenderUntilType (receiver, "register", 3);
    const auto ack = dlm::LoopbackProtocol::encodeAck (config.handle);
    expect (receiver.write (registration.senderAddress,
                            registration.senderPort,
                            ack.getData(),
                            static_cast<int> (ack.getSize())) > 0,
            "test receiver should send ack");

    waitUntilConnected (sender);
    expect (sender.getConnectionStatusText() == "Connected to uMeter", "sender connected status text mismatch");

    sender.stop();
}

void sameHandleRestartDoesNotEmitCloseBeforeReregister()
{
    juce::DatagramSocket receiver;
    expect (receiver.bindToPort (0, "127.0.0.1"), "test UDP receiver should bind");

    dlm::LoopbackSender sender;
    dlm::LoopbackSenderConfig config;
    config.handle = "juce-vst3-reconfigure-test";
    config.displayName = "Reconfigure Test";
    config.sampleRate = 48000;
    config.channels = 2;
    config.port = receiver.getBoundPort();
    config.heartbeatIntervalMs = 25;
    config.registerIntervalMs = 25;

    sender.start (config);

    const auto initialRegistration = readUntilType (receiver, "register", 3);
    expect (property (initialRegistration, "handle") == config.handle, "initial register handle mismatch");

    auto restartedConfig = config;
    restartedConfig.sampleRate = 44100;
    restartedConfig.channels = 1;
    sender.start (restartedConfig);

    const auto restartedRegistration = readUntilRegisterWithoutClose (receiver, 5);
    expect (property (restartedRegistration, "handle") == config.handle, "restarted register handle mismatch");
    expect (property (restartedRegistration, "sampleRate") == "44100", "restarted register sample rate mismatch");
    expect (property (restartedRegistration, "channels") == "1", "restarted register channel count mismatch");

    sender.stop();

    const auto close = readUntilType (receiver, "close", 5);
    expect (property (close, "handle") == config.handle, "explicit stop close handle mismatch");
}
}

void runLoopbackSenderTests()
{
    senderEmitsLifecyclePacketsFromWorker();
    senderReportsConnectedAfterReceiverAck();
    sameHandleRestartDoesNotEmitCloseBeforeReregister();
}
