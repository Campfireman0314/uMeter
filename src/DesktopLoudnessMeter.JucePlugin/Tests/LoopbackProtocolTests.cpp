#include "LoopbackProtocol.h"

#include <juce_core/juce_core.h>

#include <cmath>
#include <cstdint>
#include <cstring>
#include <stdexcept>
#include <string>
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

juce::String toString (const juce::MemoryBlock& block)
{
    return juce::String::fromUTF8 (static_cast<const char*> (block.getData()), static_cast<int> (block.getSize()));
}

juce::var parseJson (const juce::MemoryBlock& block)
{
    auto result = juce::JSON::parse (toString (block));
    expect (! result.isVoid(), "packet JSON did not parse");
    return result;
}

juce::String property (const juce::var& value, const char* name)
{
    auto* object = value.getDynamicObject();
    expect (object != nullptr, "packet JSON is not an object");
    return object->getProperty (name).toString();
}

int intProperty (const juce::var& value, const char* name)
{
    auto* object = value.getDynamicObject();
    expect (object != nullptr, "packet JSON is not an object");
    return static_cast<int> (object->getProperty (name));
}

juce::MemoryBlock utf8Block (const juce::String& text)
{
    juce::MemoryBlock block;
    const auto* utf8 = text.toRawUTF8();
    block.append (utf8, static_cast<std::size_t> (std::strlen (utf8)));
    return block;
}

void generatedHandlesAreProtocolSafe()
{
    const auto handle = dlm::LoopbackProtocol::createHandle();

    expect (handle.startsWith ("juce-vst3-"), "generated handle should use the JUCE VST3 prefix");
    expect (dlm::LoopbackProtocol::isValidHandle (handle), "generated handle should be valid");
    expect (! dlm::LoopbackProtocol::isValidHandle ({}), "empty handle should be invalid");
    expect (! dlm::LoopbackProtocol::isValidHandle ("bad\nhandle"), "control characters should be invalid");
}

void registerHeartbeatRenameAndCloseUseV1Shape()
{
    const juce::String handle = "juce-vst3-test";
    const auto registration = parseJson (dlm::LoopbackProtocol::encodeRegister (handle, "Vocal Bus", 48000, 2));

    expect (property (registration, "protocol") == "desktop-loudness-meter.loopback.v1", "register protocol mismatch");
    expect (property (registration, "type") == "register", "register type mismatch");
    expect (property (registration, "handle") == handle, "register handle mismatch");
    expect (property (registration, "displayName") == "Vocal Bus", "register display name mismatch");
    expect (property (registration, "sourceKind") == "plugin", "register source kind mismatch");
    expect (intProperty (registration, "sampleRate") == 48000, "register sample rate mismatch");
    expect (intProperty (registration, "channels") == 2, "register channel count mismatch");

    expect (property (parseJson (dlm::LoopbackProtocol::encodeHeartbeat (handle)), "type") == "heartbeat", "heartbeat type mismatch");
    expect (property (parseJson (dlm::LoopbackProtocol::encodeHeartbeat (handle)), "protocol") == "desktop-loudness-meter.loopback.v1", "heartbeat protocol mismatch");
    expect (property (parseJson (dlm::LoopbackProtocol::encodeHeartbeat (handle)), "handle") == handle, "heartbeat handle mismatch");

    const auto rename = parseJson (dlm::LoopbackProtocol::encodeRename (handle, "Drum Bus"));
    expect (property (rename, "protocol") == "desktop-loudness-meter.loopback.v1", "rename protocol mismatch");
    expect (property (rename, "type") == "rename", "rename type mismatch");
    expect (property (rename, "handle") == handle, "rename handle mismatch");
    expect (property (rename, "displayName") == "Drum Bus", "rename display name mismatch");

    expect (property (parseJson (dlm::LoopbackProtocol::encodeClose (handle)), "type") == "close", "close type mismatch");
    expect (property (parseJson (dlm::LoopbackProtocol::encodeClose (handle)), "protocol") == "desktop-loudness-meter.loopback.v1", "close protocol mismatch");
    expect (property (parseJson (dlm::LoopbackProtocol::encodeClose (handle)), "handle") == handle, "close handle mismatch");

    const auto ack = dlm::LoopbackProtocol::encodeAck (handle);
    expect (property (parseJson (ack), "protocol") == "desktop-loudness-meter.loopback.v1", "ack protocol mismatch");
    expect (property (parseJson (ack), "type") == "ack", "ack type mismatch");
    expect (property (parseJson (ack), "handle") == handle, "ack handle mismatch");
    expect (dlm::LoopbackProtocol::isAckForHandle (ack.getData(), static_cast<int> (ack.getSize()), handle), "ack should match its handle");
    expect (! dlm::LoopbackProtocol::isAckForHandle (ack.getData(), static_cast<int> (ack.getSize()), "other-handle"), "ack should reject other handles");
}

void audioUsesLittleEndianFloatBase64()
{
    const float samples[] { 1.0f, -0.5f };
    const auto packet = parseJson (dlm::LoopbackProtocol::encodeAudio ("juce-vst3-test", 42, samples, 2));

    std::uint8_t expectedBytes[] { 0x00, 0x00, 0x80, 0x3f, 0x00, 0x00, 0x00, 0xbf };
    const auto expectedBase64 = juce::Base64::toBase64 (expectedBytes, sizeof (expectedBytes));

    expect (property (packet, "type") == "audio", "audio type mismatch");
    expect (intProperty (packet, "sequence") == 42, "audio sequence mismatch");
    expect (property (packet, "samplesBase64") == expectedBase64, "audio payload should be little-endian float32 base64");
}

void ackMatchingRejectsInvalidPackets()
{
    const juce::String handle = "juce-vst3-ack";
    const auto wrongProtocol = utf8Block ("{\"protocol\":\"wrong\",\"type\":\"ack\",\"handle\":\"juce-vst3-ack\"}");
    const auto wrongType = utf8Block ("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"heartbeat\",\"handle\":\"juce-vst3-ack\"}");
    const auto wrongHandle = utf8Block ("{\"protocol\":\"desktop-loudness-meter.loopback.v1\",\"type\":\"ack\",\"handle\":\"other\"}");
    const auto invalidJson = utf8Block ("not-json");
    const juce::MemoryBlock empty;

    expect (! dlm::LoopbackProtocol::isAckForHandle (wrongProtocol.getData(), static_cast<int> (wrongProtocol.getSize()), handle), "ack should reject wrong protocol");
    expect (! dlm::LoopbackProtocol::isAckForHandle (wrongType.getData(), static_cast<int> (wrongType.getSize()), handle), "ack should reject wrong type");
    expect (! dlm::LoopbackProtocol::isAckForHandle (wrongHandle.getData(), static_cast<int> (wrongHandle.getSize()), handle), "ack should reject wrong handle");
    expect (! dlm::LoopbackProtocol::isAckForHandle (invalidJson.getData(), static_cast<int> (invalidJson.getSize()), handle), "ack should reject invalid JSON");
    expect (! dlm::LoopbackProtocol::isAckForHandle (nullptr, 0, handle), "ack should reject null data");
    expect (! dlm::LoopbackProtocol::isAckForHandle (empty.getData(), static_cast<int> (empty.getSize()), handle), "ack should reject empty data");
}
}

void runLoopbackProtocolTests()
{
    generatedHandlesAreProtocolSafe();
    registerHeartbeatRenameAndCloseUseV1Shape();
    audioUsesLittleEndianFloatBase64();
    ackMatchingRejectsInvalidPackets();
}
