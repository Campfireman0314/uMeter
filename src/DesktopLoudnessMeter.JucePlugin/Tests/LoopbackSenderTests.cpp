#include "LoopbackSender.h"

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

juce::MemoryBlock readPacket (juce::DatagramSocket& socket, int timeoutMs)
{
    expect (socket.waitUntilReady (true, timeoutMs) == 1, "timed out waiting for sender packet");

    char buffer[8192] {};
    const auto bytesRead = socket.read (buffer, sizeof (buffer), false);
    expect (bytesRead > 0, "sender packet read failed");

    return juce::MemoryBlock (buffer, static_cast<std::size_t> (bytesRead));
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
}
}

void runLoopbackSenderTests()
{
    senderEmitsLifecyclePacketsFromWorker();
}
