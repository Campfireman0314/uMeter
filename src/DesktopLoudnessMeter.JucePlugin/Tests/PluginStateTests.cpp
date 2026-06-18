#include "PluginState.h"

#include <juce_core/juce_core.h>

#include <cstring>
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

void validStateRoundTripsHandleAndDisplayName()
{
    const auto state = dlm::PluginState::save ("juce-vst3-state-test", "Print Bus");

    juce::String handle;
    juce::String displayName;
    expect (dlm::PluginState::tryRestore (state.getData(), static_cast<int> (state.getSize()), handle, displayName),
            "saved state should restore");
    expect (handle == "juce-vst3-state-test", "state handle mismatch");
    expect (displayName == "Print Bus", "state display name mismatch");
}

void defaultDisplayNameUsesHandleSuffix()
{
    expect (dlm::PluginState::defaultDisplayNameForHandle ("juce-vst3-12345678", "Cubase") == "Cubase 5678",
            "default display name should use host name and a short stable handle suffix");
    expect (dlm::PluginState::defaultDisplayNameForHandle ("juce-vst3-12345678") == "VST 5678",
            "default display name should fall back to VST and a short stable handle suffix");
}

void emptyDisplayNameUsesGeneratedDefault()
{
    const auto state = dlm::PluginState::save ("juce-vst3-abcdef12", "   ");

    juce::String handle;
    juce::String displayName;
    expect (dlm::PluginState::tryRestore (state.getData(), static_cast<int> (state.getSize()), handle, displayName, "Cubase"),
            "empty display name state should restore");
    expect (displayName == "VST EF12", "empty display name should restore generated default");
}

void legacyDefaultDisplayNameMigratesToGeneratedDefault()
{
    juce::XmlElement xml ("DesktopLoudnessMeterSender");
    xml.setAttribute ("handle", "juce-vst3-87654321");
    xml.setAttribute ("displayName", "Desktop Loudness Meter Sender");

    juce::MemoryBlock state;
    const auto text = xml.toString (juce::XmlElement::TextFormat().singleLine());
    state.append (text.toRawUTF8(), std::strlen (text.toRawUTF8()));

    juce::String handle;
    juce::String displayName;
    expect (dlm::PluginState::tryRestore (state.getData(), static_cast<int> (state.getSize()), handle, displayName, "Cubase"),
            "legacy default display name state should restore");
    expect (displayName == "Cubase 4321", "legacy default display name should migrate to host generated default");
}

void savingLegacyTextAsCustomNameIsPreserved()
{
    const auto state = dlm::PluginState::save ("juce-vst3-aaaabbbb", "Desktop Loudness Meter Sender");
    const auto stateText = juce::String::fromUTF8 (static_cast<const char*> (state.getData()), static_cast<int> (state.getSize()));

    expect (stateText.contains ("Desktop Loudness Meter Sender"), "explicit legacy text should save as a custom name");
}

void invalidSavedHandlesAreRejected()
{
    juce::XmlElement xml ("DesktopLoudnessMeterSender");
    xml.setAttribute ("handle", "bad\nhandle");
    xml.setAttribute ("displayName", "Bad Handle");

    juce::MemoryBlock state;
    const auto text = xml.toString (juce::XmlElement::TextFormat().singleLine());
    state.append (text.toRawUTF8(), std::strlen (text.toRawUTF8()));

    juce::String handle;
    juce::String displayName;
    expect (! dlm::PluginState::tryRestore (state.getData(), static_cast<int> (state.getSize()), handle, displayName),
            "invalid handle should reject state");
}
}

void runPluginStateTests()
{
    validStateRoundTripsHandleAndDisplayName();
    defaultDisplayNameUsesHandleSuffix();
    emptyDisplayNameUsesGeneratedDefault();
    legacyDefaultDisplayNameMigratesToGeneratedDefault();
    savingLegacyTextAsCustomNameIsPreserved();
    invalidSavedHandlesAreRejected();
}
