#include "DlmLoopbackProtocol.h"

#include <cctype>
#include <cmath>
#include <cstring>
#include <limits>
#include <map>
#include <string>
#include <vector>

namespace
{
    constexpr const char* ProtocolVersion = "desktop-loudness-meter.loopback.v1";
    constexpr int PacketTypeRegister = 0;
    constexpr int PacketTypeHeartbeat = 1;
    constexpr int PacketTypeRename = 2;
    constexpr int PacketTypeAudio = 3;
    constexpr int PacketTypeClose = 4;
    constexpr int PacketTypeAck = 5;
    constexpr int SourceKindLoopback = 0;
    constexpr int SourceKindPlugin = 1;
    constexpr int StringFieldHandle = 0;
    constexpr int StringFieldDisplayName = 1;

    struct JsonValue
    {
        bool isString = false;
        bool isNumber = false;
        std::string stringValue;
        long long numberValue = 0;
    };

    struct ParsedPacket
    {
        DlmLoopbackProtocolPacket packet = {};
        std::string handle;
        std::string displayName;
        std::vector<float> samples;
    };

    bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    void SkipWhitespace(const std::string& text, size_t& index)
    {
        while (index < text.size() && IsWhitespace(text[index]))
        {
            ++index;
        }
    }

    bool HexValue(char value, int& result)
    {
        if (value >= '0' && value <= '9')
        {
            result = value - '0';
            return true;
        }

        if (value >= 'a' && value <= 'f')
        {
            result = 10 + (value - 'a');
            return true;
        }

        if (value >= 'A' && value <= 'F')
        {
            result = 10 + (value - 'A');
            return true;
        }

        return false;
    }

    void AppendUtf8(unsigned int codePoint, std::string& output)
    {
        if (codePoint <= 0x7F)
        {
            output.push_back(static_cast<char>(codePoint));
        }
        else if (codePoint <= 0x7FF)
        {
            output.push_back(static_cast<char>(0xC0 | ((codePoint >> 6) & 0x1F)));
            output.push_back(static_cast<char>(0x80 | (codePoint & 0x3F)));
        }
        else if (codePoint <= 0xFFFF)
        {
            output.push_back(static_cast<char>(0xE0 | ((codePoint >> 12) & 0x0F)));
            output.push_back(static_cast<char>(0x80 | ((codePoint >> 6) & 0x3F)));
            output.push_back(static_cast<char>(0x80 | (codePoint & 0x3F)));
        }
        else
        {
            output.push_back(static_cast<char>(0xF0 | ((codePoint >> 18) & 0x07)));
            output.push_back(static_cast<char>(0x80 | ((codePoint >> 12) & 0x3F)));
            output.push_back(static_cast<char>(0x80 | ((codePoint >> 6) & 0x3F)));
            output.push_back(static_cast<char>(0x80 | (codePoint & 0x3F)));
        }
    }

    bool TryReadUnicodeEscape(const std::string& text, size_t& index, unsigned int& codePoint)
    {
        if (index + 4 > text.size())
        {
            return false;
        }

        codePoint = 0;
        for (int digit = 0; digit < 4; ++digit)
        {
            int hex = 0;
            if (!HexValue(text[index++], hex))
            {
                return false;
            }

            codePoint = (codePoint << 4) | static_cast<unsigned int>(hex);
        }

        return true;
    }

    bool ParseJsonString(const std::string& text, size_t& index, std::string& value)
    {
        if (index >= text.size() || text[index] != '"')
        {
            return false;
        }

        ++index;
        value.clear();
        while (index < text.size())
        {
            const char current = text[index++];
            if (current == '"')
            {
                return true;
            }

            if (static_cast<unsigned char>(current) < 0x20)
            {
                return false;
            }

            if (current != '\\')
            {
                value.push_back(current);
                continue;
            }

            if (index >= text.size())
            {
                return false;
            }

            const char escaped = text[index++];
            switch (escaped)
            {
            case '"':
            case '\\':
            case '/':
                value.push_back(escaped);
                break;
            case 'b':
                value.push_back('\b');
                break;
            case 'f':
                value.push_back('\f');
                break;
            case 'n':
                value.push_back('\n');
                break;
            case 'r':
                value.push_back('\r');
                break;
            case 't':
                value.push_back('\t');
                break;
            case 'u':
            {
                unsigned int codePoint = 0;
                if (!TryReadUnicodeEscape(text, index, codePoint))
                {
                    return false;
                }

                if (codePoint >= 0xD800 && codePoint <= 0xDBFF)
                {
                    if (index + 6 > text.size() || text[index] != '\\' || text[index + 1] != 'u')
                    {
                        return false;
                    }

                    index += 2;
                    unsigned int lowSurrogate = 0;
                    if (!TryReadUnicodeEscape(text, index, lowSurrogate)
                        || lowSurrogate < 0xDC00
                        || lowSurrogate > 0xDFFF)
                    {
                        return false;
                    }

                    codePoint = 0x10000
                        + (((codePoint - 0xD800) << 10) | (lowSurrogate - 0xDC00));
                }
                else if (codePoint >= 0xDC00 && codePoint <= 0xDFFF)
                {
                    return false;
                }

                AppendUtf8(codePoint, value);
                break;
            }
            default:
                return false;
            }
        }

        return false;
    }

    bool ParseNumber(const std::string& text, size_t& index, long long& value)
    {
        const size_t start = index;
        if (index < text.size() && text[index] == '-')
        {
            ++index;
        }

        if (index >= text.size() || !std::isdigit(static_cast<unsigned char>(text[index])))
        {
            return false;
        }

        while (index < text.size() && std::isdigit(static_cast<unsigned char>(text[index])))
        {
            ++index;
        }

        if (index < text.size() && (text[index] == '.' || text[index] == 'e' || text[index] == 'E'))
        {
            return false;
        }

        try
        {
            value = std::stoll(text.substr(start, index - start));
            return true;
        }
        catch (...)
        {
            return false;
        }
    }

    bool SkipLiteral(const std::string& text, size_t& index, const char* literal)
    {
        const size_t length = std::strlen(literal);
        if (text.compare(index, length, literal) != 0)
        {
            return false;
        }

        index += length;
        return true;
    }

    bool SkipArrayOrObject(const std::string& text, size_t& index)
    {
        if (index >= text.size() || (text[index] != '{' && text[index] != '['))
        {
            return false;
        }

        std::vector<char> stack;
        stack.push_back(text[index] == '{' ? '}' : ']');
        ++index;
        while (index < text.size() && !stack.empty())
        {
            if (text[index] == '"')
            {
                std::string ignored;
                if (!ParseJsonString(text, index, ignored))
                {
                    return false;
                }

                continue;
            }

            if (text[index] == '{' || text[index] == '[')
            {
                stack.push_back(text[index] == '{' ? '}' : ']');
                ++index;
                continue;
            }

            if (text[index] == stack.back())
            {
                stack.pop_back();
                ++index;
                continue;
            }

            ++index;
        }

        return stack.empty();
    }

    bool ParseJsonObject(const std::string& text, std::map<std::string, JsonValue>& fields)
    {
        size_t index = 0;
        SkipWhitespace(text, index);
        if (index >= text.size() || text[index] != '{')
        {
            return false;
        }

        ++index;
        SkipWhitespace(text, index);
        if (index < text.size() && text[index] == '}')
        {
            ++index;
            SkipWhitespace(text, index);
            return index == text.size();
        }

        while (index < text.size())
        {
            std::string key;
            if (!ParseJsonString(text, index, key))
            {
                return false;
            }

            SkipWhitespace(text, index);
            if (index >= text.size() || text[index] != ':')
            {
                return false;
            }

            ++index;
            SkipWhitespace(text, index);

            JsonValue value;
            if (index < text.size() && text[index] == '"')
            {
                value.isString = true;
                if (!ParseJsonString(text, index, value.stringValue))
                {
                    return false;
                }
            }
            else if (index < text.size() && (text[index] == '-' || std::isdigit(static_cast<unsigned char>(text[index]))))
            {
                value.isNumber = true;
                if (!ParseNumber(text, index, value.numberValue))
                {
                    return false;
                }
            }
            else if (index < text.size() && (text[index] == '{' || text[index] == '['))
            {
                if (!SkipArrayOrObject(text, index))
                {
                    return false;
                }
            }
            else if (!SkipLiteral(text, index, "true")
                && !SkipLiteral(text, index, "false")
                && !SkipLiteral(text, index, "null"))
            {
                return false;
            }

            fields[key] = std::move(value);
            SkipWhitespace(text, index);
            if (index < text.size() && text[index] == ',')
            {
                ++index;
                SkipWhitespace(text, index);
                continue;
            }

            if (index < text.size() && text[index] == '}')
            {
                ++index;
                SkipWhitespace(text, index);
                return index == text.size();
            }

            return false;
        }

        return false;
    }

    std::string TrimAscii(std::string value)
    {
        size_t start = 0;
        while (start < value.size() && IsWhitespace(value[start]))
        {
            ++start;
        }

        size_t end = value.size();
        while (end > start && IsWhitespace(value[end - 1]))
        {
            --end;
        }

        return value.substr(start, end - start);
    }

    std::string ToLowerAscii(std::string value)
    {
        for (auto& character : value)
        {
            character = static_cast<char>(std::tolower(static_cast<unsigned char>(character)));
        }

        return value;
    }

    const JsonValue* Find(const std::map<std::string, JsonValue>& fields, const char* key)
    {
        const auto iterator = fields.find(key);
        return iterator == fields.end() ? nullptr : &iterator->second;
    }

    bool TryGetString(const std::map<std::string, JsonValue>& fields, const char* key, std::string& value)
    {
        const auto* field = Find(fields, key);
        if (field == nullptr || !field->isString)
        {
            return false;
        }

        value = field->stringValue;
        return true;
    }

    bool TryGetNumber(const std::map<std::string, JsonValue>& fields, const char* key, long long& value)
    {
        const auto* field = Find(fields, key);
        if (field == nullptr || !field->isNumber)
        {
            return false;
        }

        value = field->numberValue;
        return true;
    }

    bool IsValidHandle(const std::string& handle)
    {
        if (TrimAscii(handle).empty())
        {
            return false;
        }

        int codePointCount = 0;
        size_t index = 0;
        while (index < handle.size())
        {
            const auto lead = static_cast<unsigned char>(handle[index]);
            size_t byteCount = 0;
            if (lead <= 0x7F)
            {
                byteCount = 1;
            }
            else if ((lead & 0xE0) == 0xC0)
            {
                byteCount = 2;
            }
            else if ((lead & 0xF0) == 0xE0)
            {
                byteCount = 3;
            }
            else if ((lead & 0xF8) == 0xF0)
            {
                byteCount = 4;
            }
            else
            {
                return false;
            }

            if (index + byteCount > handle.size())
            {
                return false;
            }

            for (size_t continuation = 1; continuation < byteCount; ++continuation)
            {
                const auto current = static_cast<unsigned char>(handle[index + continuation]);
                if ((current & 0xC0) != 0x80)
                {
                    return false;
                }
            }

            index += byteCount;
            ++codePointCount;
            if (codePointCount > 128)
            {
                return false;
            }
        }

        for (const char character : handle)
        {
            if (std::iscntrl(static_cast<unsigned char>(character)))
            {
                return false;
            }
        }

        return true;
    }

    bool IsSupportedSampleRate(long long sampleRate)
    {
        return sampleRate >= 8000 && sampleRate <= 384000;
    }

    bool IsSupportedChannelCount(long long channels)
    {
        return channels == 1 || channels == 2;
    }

    bool TryParsePacketType(const std::string& value, int& type)
    {
        const auto normalized = ToLowerAscii(TrimAscii(value));
        if (normalized == "register")
        {
            type = PacketTypeRegister;
            return true;
        }

        if (normalized == "heartbeat")
        {
            type = PacketTypeHeartbeat;
            return true;
        }

        if (normalized == "rename")
        {
            type = PacketTypeRename;
            return true;
        }

        if (normalized == "audio")
        {
            type = PacketTypeAudio;
            return true;
        }

        if (normalized == "close")
        {
            type = PacketTypeClose;
            return true;
        }

        if (normalized == "ack")
        {
            type = PacketTypeAck;
            return true;
        }

        return false;
    }

    int ParseSourceKind(const std::map<std::string, JsonValue>& fields)
    {
        std::string sourceKind;
        if (!TryGetString(fields, "sourceKind", sourceKind))
        {
            return SourceKindLoopback;
        }

        const auto normalized = ToLowerAscii(TrimAscii(sourceKind));
        return normalized == "plugin" ? SourceKindPlugin : SourceKindLoopback;
    }

    int DecodeBase64(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }

        if (value >= 'a' && value <= 'z')
        {
            return 26 + (value - 'a');
        }

        if (value >= '0' && value <= '9')
        {
            return 52 + (value - '0');
        }

        if (value == '+')
        {
            return 62;
        }

        if (value == '/')
        {
            return 63;
        }

        return -1;
    }

    bool TryDecodeBase64(const std::string& value, std::vector<unsigned char>& bytes)
    {
        bytes.clear();
        if (value.empty() || value.size() % 4 != 0)
        {
            return false;
        }

        for (size_t index = 0; index < value.size(); index += 4)
        {
            int values[4] = {};
            int padding = 0;
            for (int offset = 0; offset < 4; ++offset)
            {
                const char current = value[index + static_cast<size_t>(offset)];
                if (current == '=')
                {
                    values[offset] = 0;
                    ++padding;
                    continue;
                }

                if (padding > 0)
                {
                    return false;
                }

                values[offset] = DecodeBase64(current);
                if (values[offset] < 0)
                {
                    return false;
                }
            }

            if (padding > 2 || (padding > 0 && index + 4 != value.size()))
            {
                return false;
            }

            const int combined = (values[0] << 18) | (values[1] << 12) | (values[2] << 6) | values[3];
            bytes.push_back(static_cast<unsigned char>((combined >> 16) & 0xFF));
            if (padding < 2)
            {
                bytes.push_back(static_cast<unsigned char>((combined >> 8) & 0xFF));
            }

            if (padding < 1)
            {
                bytes.push_back(static_cast<unsigned char>(combined & 0xFF));
            }
        }

        return true;
    }

    bool TryDecodeFloatSamples(const std::string& base64, std::vector<float>& samples)
    {
        std::vector<unsigned char> bytes;
        if (!TryDecodeBase64(base64, bytes) || bytes.empty() || bytes.size() % sizeof(float) != 0)
        {
            return false;
        }

        samples.resize(bytes.size() / sizeof(float));
        for (size_t index = 0; index < samples.size(); ++index)
        {
            unsigned int raw = 0;
            raw |= static_cast<unsigned int>(bytes[(index * 4) + 0]);
            raw |= static_cast<unsigned int>(bytes[(index * 4) + 1]) << 8;
            raw |= static_cast<unsigned int>(bytes[(index * 4) + 2]) << 16;
            raw |= static_cast<unsigned int>(bytes[(index * 4) + 3]) << 24;

            float sample = 0.0f;
            static_assert(sizeof(sample) == sizeof(raw), "float32 sample size expected");
            std::memcpy(&sample, &raw, sizeof(sample));
            samples[index] = sample;
        }

        return true;
    }

    std::string EscapeJsonString(const std::string& value)
    {
        std::string escaped;
        for (const char character : value)
        {
            switch (character)
            {
            case '"':
                escaped += "\\\"";
                break;
            case '\\':
                escaped += "\\\\";
                break;
            case '\b':
                escaped += "\\b";
                break;
            case '\f':
                escaped += "\\f";
                break;
            case '\n':
                escaped += "\\n";
                break;
            case '\r':
                escaped += "\\r";
                break;
            case '\t':
                escaped += "\\t";
                break;
            default:
                if (static_cast<unsigned char>(character) < 0x20)
                {
                    return {};
                }

                escaped.push_back(character);
                break;
            }
        }

        return escaped;
    }

    int CopyStringToBuffer(const std::string& value, char* buffer, int capacity, int* actualByteCount)
    {
        if (buffer == nullptr || actualByteCount == nullptr || capacity < 0)
        {
            return -2;
        }

        const int count = static_cast<int>(value.size());
        if (capacity < count)
        {
            return -5;
        }

        if (count > 0)
        {
            std::memcpy(buffer, value.data(), static_cast<size_t>(count));
        }

        *actualByteCount = count;
        return 0;
    }

    bool TryBuildPacket(const std::map<std::string, JsonValue>& fields, ParsedPacket& parsed)
    {
        std::string protocol;
        std::string typeText;
        std::string handle;
        if (!TryGetString(fields, "protocol", protocol)
            || protocol != ProtocolVersion
            || !TryGetString(fields, "type", typeText)
            || !TryParsePacketType(typeText, parsed.packet.packetType)
            || !TryGetString(fields, "handle", handle)
            || !IsValidHandle(handle))
        {
            return false;
        }

        parsed.handle = handle;
        parsed.packet.sourceKind = ParseSourceKind(fields);

        if (parsed.packet.packetType == PacketTypeRegister)
        {
            long long sampleRate = 0;
            long long channels = 0;
            std::string displayName;
            if (!TryGetNumber(fields, "sampleRate", sampleRate)
                || !TryGetNumber(fields, "channels", channels)
                || !TryGetString(fields, "displayName", displayName)
                || !IsSupportedSampleRate(sampleRate)
                || !IsSupportedChannelCount(channels))
            {
                return false;
            }

            parsed.displayName = TrimAscii(displayName);
            if (parsed.displayName.empty())
            {
                return false;
            }

            parsed.packet.sampleRate = static_cast<int>(sampleRate);
            parsed.packet.channels = static_cast<int>(channels);
        }
        else if (parsed.packet.packetType == PacketTypeRename)
        {
            std::string displayName;
            if (!TryGetString(fields, "displayName", displayName))
            {
                return false;
            }

            parsed.displayName = TrimAscii(displayName);
            if (parsed.displayName.empty())
            {
                return false;
            }
        }
        else if (parsed.packet.packetType == PacketTypeAudio)
        {
            std::string samplesBase64;
            if (!TryGetString(fields, "samplesBase64", samplesBase64)
                || !TryDecodeFloatSamples(samplesBase64, parsed.samples))
            {
                return false;
            }

            long long sequence = 0;
            if (TryGetNumber(fields, "sequence", sequence))
            {
                parsed.packet.sequence = sequence;
            }
        }

        parsed.packet.handleUtf8ByteCount = static_cast<int>(parsed.handle.size());
        parsed.packet.displayNameUtf8ByteCount = static_cast<int>(parsed.displayName.size());
        parsed.packet.sampleCount = static_cast<int>(parsed.samples.size());
        return true;
    }
}

int DlmLoopbackProtocolDecode(
    const unsigned char* bytes,
    int byteCount,
    void** packetHandle,
    DlmLoopbackProtocolPacket* packet)
{
    if (packetHandle == nullptr || packet == nullptr)
    {
        return -2;
    }

    *packetHandle = nullptr;
    *packet = {};
    if (bytes == nullptr || byteCount <= 0)
    {
        return 1;
    }

    try
    {
        const std::string text(reinterpret_cast<const char*>(bytes), static_cast<size_t>(byteCount));
        std::map<std::string, JsonValue> fields;
        if (!ParseJsonObject(text, fields))
        {
            return 1;
        }

        auto parsed = new ParsedPacket();
        if (!TryBuildPacket(fields, *parsed))
        {
            delete parsed;
            return 1;
        }

        *packet = parsed->packet;
        *packetHandle = parsed;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmLoopbackProtocolDestroyPacket(void* packetHandle)
{
    delete static_cast<ParsedPacket*>(packetHandle);
}

int DlmLoopbackProtocolCopyString(
    void* packetHandle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* packet = static_cast<ParsedPacket*>(packetHandle);
    if (packet == nullptr)
    {
        return -2;
    }

    if (field == StringFieldHandle)
    {
        return CopyStringToBuffer(packet->handle, buffer, capacity, actualByteCount);
    }

    if (field == StringFieldDisplayName)
    {
        return CopyStringToBuffer(packet->displayName, buffer, capacity, actualByteCount);
    }

    return -3;
}

int DlmLoopbackProtocolCopySamples(
    void* packetHandle,
    float* buffer,
    int capacity,
    int* actualCount)
{
    auto* packet = static_cast<ParsedPacket*>(packetHandle);
    if (packet == nullptr || buffer == nullptr || actualCount == nullptr || capacity < 0)
    {
        return -2;
    }

    const int count = static_cast<int>(packet->samples.size());
    if (capacity < count)
    {
        return -5;
    }

    if (count > 0)
    {
        std::memcpy(buffer, packet->samples.data(), static_cast<size_t>(count) * sizeof(float));
    }

    *actualCount = count;
    return 0;
}

int DlmLoopbackProtocolEncodeAck(
    const char* sourceHandle,
    unsigned char* buffer,
    int capacity,
    int* actualByteCount)
{
    if (sourceHandle == nullptr || buffer == nullptr || actualByteCount == nullptr || capacity < 0)
    {
        return -2;
    }

    const std::string handle(sourceHandle);
    if (!IsValidHandle(handle))
    {
        return -3;
    }

    const auto escapedHandle = EscapeJsonString(handle);
    if (escapedHandle.empty() && !handle.empty())
    {
        return -3;
    }

    const std::string json = std::string("{\"protocol\":\"")
        + ProtocolVersion
        + "\",\"type\":\"ack\",\"handle\":\""
        + escapedHandle
        + "\"}";
    const int count = static_cast<int>(json.size());
    if (capacity < count)
    {
        return -5;
    }

    if (count > 0)
    {
        std::memcpy(buffer, json.data(), static_cast<size_t>(count));
    }

    *actualByteCount = count;
    return 0;
}
