#pragma once

#include <juce_core/juce_core.h>

namespace dlm
{
enum class PluginLanguage
{
    English,
    Chinese,
    Japanese,
    Russian
};

class PluginLocalization
{
public:
    PluginLocalization();
    explicit PluginLocalization (PluginLanguage selectedLanguage);

    juce::String translate (const juce::String& englishText) const;

    static PluginLanguage resolveLanguage (const juce::String& preferenceCode,
                                           const juce::String& displayLanguage);
    static juce::String loadPreferenceCode (const juce::File& settingsFile);
    static juce::File defaultSettingsFile();

private:
    PluginLanguage language = PluginLanguage::English;
};
}
