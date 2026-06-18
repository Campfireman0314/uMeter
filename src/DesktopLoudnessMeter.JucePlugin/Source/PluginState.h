#pragma once

#include <juce_core/juce_core.h>

namespace dlm
{
struct RestoredPluginState
{
    juce::String handle;
    juce::String displayName;
};

class PluginState final
{
public:
    static constexpr const char* stateTag = "DesktopLoudnessMeterSender";
    static constexpr const char* defaultDisplayName = "VST";
    static constexpr const char* legacyDefaultDisplayName = "Desktop Loudness Meter Sender";

    static juce::String defaultDisplayNameForHandle (const juce::String& handle, const juce::String& prefix = {});
    static juce::MemoryBlock save (const juce::String& handle, const juce::String& displayName);
    static bool tryRestore (
        const void* data,
        int sizeInBytes,
        juce::String& handle,
        juce::String& displayName,
        const juce::String& defaultNamePrefix = {});
};
}
