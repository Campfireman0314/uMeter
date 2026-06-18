#include "PluginState.h"

#include "LoopbackProtocol.h"

#include <cstring>

namespace
{
juce::MemoryBlock toMemoryBlock (const juce::String& text)
{
    juce::MemoryBlock block;
    block.append (text.toRawUTF8(), std::strlen (text.toRawUTF8()));
    return block;
}
}

namespace dlm
{
namespace
{
juce::String normalizeDefaultPrefix (const juce::String& prefix)
{
    const auto normalized = prefix.trim().retainCharacters ("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ _-.").trim();
    return normalized.isEmpty() ? PluginState::defaultDisplayName : normalized;
}

juce::String normalizeDisplayNameForSave (const juce::String& handle, const juce::String& displayName)
{
    const auto trimmed = displayName.trim();

    if (trimmed.isEmpty())
    {
        return PluginState::defaultDisplayNameForHandle (handle);
    }

    return trimmed;
}

juce::String normalizeDisplayNameForRestore (
    const juce::String& handle,
    const juce::String& displayName,
    const juce::String& defaultNamePrefix)
{
    const auto trimmed = displayName.trim();

    if (trimmed.isEmpty() || trimmed == PluginState::legacyDefaultDisplayName)
    {
        return PluginState::defaultDisplayNameForHandle (handle, defaultNamePrefix);
    }

    return trimmed;
}
}

juce::String PluginState::defaultDisplayNameForHandle (const juce::String& handle, const juce::String& prefix)
{
    const auto compact = handle.retainCharacters ("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ").toUpperCase();
    const auto normalizedPrefix = normalizeDefaultPrefix (prefix);

    if (compact.length() >= 4)
    {
        return normalizedPrefix + " " + compact.substring (compact.length() - 4);
    }

    return normalizedPrefix;
}

juce::MemoryBlock PluginState::save (const juce::String& handle, const juce::String& displayName)
{
    juce::XmlElement xml (stateTag);
    xml.setAttribute ("handle", handle);
    xml.setAttribute ("displayName", normalizeDisplayNameForSave (handle, displayName));

    return toMemoryBlock (xml.toString (juce::XmlElement::TextFormat().singleLine()));
}

bool PluginState::tryRestore (
    const void* data,
    int sizeInBytes,
    juce::String& handle,
    juce::String& displayName,
    const juce::String& defaultNamePrefix)
{
    if (data == nullptr || sizeInBytes <= 0)
    {
        return false;
    }

    const auto text = juce::String::fromUTF8 (static_cast<const char*> (data), sizeInBytes);
    const auto xml = juce::parseXMLIfTagMatches (text, stateTag);

    if (xml == nullptr)
    {
        return false;
    }

    const auto restoredHandle = xml->getStringAttribute ("handle").trim();
    if (! LoopbackProtocol::isValidHandle (restoredHandle))
    {
        return false;
    }

    handle = restoredHandle;
    displayName = normalizeDisplayNameForRestore (restoredHandle, xml->getStringAttribute ("displayName"), defaultNamePrefix);
    return true;
}
}
