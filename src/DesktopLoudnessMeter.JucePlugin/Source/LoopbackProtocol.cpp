#include "LoopbackProtocol.h"

#include <cstring>
#include <vector>

namespace
{
juce::String quoted (const juce::String& value)
{
    return "\"" + juce::JSON::escapeString (value) + "\"";
}

juce::MemoryBlock toUtf8Block (const juce::String& text)
{
    juce::MemoryBlock block;
    const auto* utf8 = text.toRawUTF8();
    block.append (utf8, std::strlen (utf8));
    return block;
}

juce::MemoryBlock simplePacket (const juce::String& type, const juce::String& handle)
{
    return toUtf8Block ("{\"protocol\":" + quoted (dlm::LoopbackProtocol::protocolVersion)
                        + ",\"type\":" + quoted (type)
                        + ",\"handle\":" + quoted (handle)
                        + "}");
}

std::vector<std::uint8_t> toLittleEndianFloatBytes (const float* samples, int sampleCount)
{
    std::vector<std::uint8_t> bytes (static_cast<std::size_t> (sampleCount) * sizeof (float));

    for (auto index = 0; index < sampleCount; ++index)
    {
        std::uint32_t bits = 0;
        std::memcpy (&bits, samples + index, sizeof (float));

        const auto byteIndex = static_cast<std::size_t> (index) * sizeof (float);
        bytes[byteIndex] = static_cast<std::uint8_t> (bits & 0xffu);
        bytes[byteIndex + 1] = static_cast<std::uint8_t> ((bits >> 8) & 0xffu);
        bytes[byteIndex + 2] = static_cast<std::uint8_t> ((bits >> 16) & 0xffu);
        bytes[byteIndex + 3] = static_cast<std::uint8_t> ((bits >> 24) & 0xffu);
    }

    return bytes;
}
}

namespace dlm
{
juce::String LoopbackProtocol::createHandle()
{
    return "juce-vst3-" + juce::Uuid().toString();
}

bool LoopbackProtocol::isValidHandle (const juce::String& handle)
{
    if (handle.trim().isEmpty() || handle.length() > 128)
    {
        return false;
    }

    for (auto index = 0; index < handle.length(); ++index)
    {
        if (handle[index] < 0x20)
        {
            return false;
        }
    }

    return true;
}

juce::MemoryBlock LoopbackProtocol::encodeRegister (const juce::String& handle,
                                                    const juce::String& displayName,
                                                    int sampleRate,
                                                    int channels)
{
    return toUtf8Block ("{\"protocol\":" + quoted (protocolVersion)
                        + ",\"type\":\"register\""
                        + ",\"handle\":" + quoted (handle)
                        + ",\"displayName\":" + quoted (displayName)
                        + ",\"sourceKind\":\"plugin\""
                        + ",\"sampleRate\":" + juce::String (sampleRate)
                        + ",\"channels\":" + juce::String (channels)
                        + "}");
}

juce::MemoryBlock LoopbackProtocol::encodeHeartbeat (const juce::String& handle)
{
    return simplePacket ("heartbeat", handle);
}

juce::MemoryBlock LoopbackProtocol::encodeRename (const juce::String& handle, const juce::String& displayName)
{
    return toUtf8Block ("{\"protocol\":" + quoted (protocolVersion)
                        + ",\"type\":\"rename\""
                        + ",\"handle\":" + quoted (handle)
                        + ",\"displayName\":" + quoted (displayName)
                        + "}");
}

juce::MemoryBlock LoopbackProtocol::encodeAudio (const juce::String& handle,
                                                 std::int64_t sequence,
                                                 const float* samples,
                                                 int sampleCount)
{
    const auto bytes = toLittleEndianFloatBytes (samples, sampleCount);
    const auto encodedSamples = juce::Base64::toBase64 (bytes.data(), bytes.size());

    return toUtf8Block ("{\"protocol\":" + quoted (protocolVersion)
                        + ",\"type\":\"audio\""
                        + ",\"handle\":" + quoted (handle)
                        + ",\"sequence\":" + juce::String (sequence)
                        + ",\"samplesBase64\":" + quoted (encodedSamples)
                        + "}");
}

juce::MemoryBlock LoopbackProtocol::encodeClose (const juce::String& handle)
{
    return simplePacket ("close", handle);
}
}
