#include "PluginLocalization.h"

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

void automaticPreferenceUsesDisplayLanguage()
{
    expect (dlm::PluginLocalization::resolveLanguage ("auto", "zh-CN") == dlm::PluginLanguage::Chinese,
            "automatic language should recognize Chinese");
    expect (dlm::PluginLocalization::resolveLanguage ({}, "ja-JP") == dlm::PluginLanguage::Japanese,
            "empty preference should recognize Japanese");
    expect (dlm::PluginLocalization::resolveLanguage ("auto", "ru-RU") == dlm::PluginLanguage::Russian,
            "automatic language should recognize Russian");
    expect (dlm::PluginLocalization::resolveLanguage ("auto", "de-DE") == dlm::PluginLanguage::English,
            "unsupported display languages should fall back to English");
}

void explicitPreferenceOverridesDisplayLanguage()
{
    expect (dlm::PluginLocalization::resolveLanguage ("ru", "ja-JP") == dlm::PluginLanguage::Russian,
            "explicit preference should override display language");
    expect (dlm::PluginLocalization::resolveLanguage ("en", "zh-CN") == dlm::PluginLanguage::English,
            "explicit English should override display language");
}

void selectedLanguageTranslatesEditorText()
{
    const dlm::PluginLocalization chinese (dlm::PluginLanguage::Chinese);
    const dlm::PluginLocalization japanese (dlm::PluginLanguage::Japanese);
    const dlm::PluginLocalization russian (dlm::PluginLanguage::Russian);

    expect (chinese.translate ("Start uMeter") == juce::String::fromUTF8 ("启动 uMeter"),
            "Chinese launch label mismatch");
    expect (japanese.translate ("Source name") == juce::String::fromUTF8 ("ソース名"),
            "Japanese source label mismatch");
    expect (russian.translate ("Mute") == juce::String::fromUTF8 ("Отключить звук"),
            "Russian mute label mismatch");
    expect (chinese.translate ("uMeter is already running.") == juce::String::fromUTF8 ("uMeter 已在运行。"),
            "Chinese already-running launch status mismatch");
}
}

void runPluginLocalizationTests()
{
    automaticPreferenceUsesDisplayLanguage();
    explicitPreferenceOverridesDisplayLanguage();
    selectedLanguageTranslatesEditorText();
}
