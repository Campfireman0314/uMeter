#pragma once

#include "PluginProcessor.h"

#include <juce_gui_basics/juce_gui_basics.h>

class DesktopLoudnessMeterAudioProcessorEditor final : public juce::AudioProcessorEditor
{
public:
    explicit DesktopLoudnessMeterAudioProcessorEditor (DesktopLoudnessMeterAudioProcessor& processor);
    ~DesktopLoudnessMeterAudioProcessorEditor() override;

    void paint (juce::Graphics& graphics) override;
    void resized() override;

private:
    class RoundedTextEditorLookAndFeel final : public juce::LookAndFeel_V4
    {
    public:
        void fillTextEditorBackground (juce::Graphics& graphics, int width, int height, juce::TextEditor& textEditor) override;
        void drawTextEditorOutline (juce::Graphics& graphics, int width, int height, juce::TextEditor& textEditor) override;
    };

    void updateSourceName (bool refreshEditorText);
    void refreshMuteButton();

    DesktopLoudnessMeterAudioProcessor& audioProcessor;
    RoundedTextEditorLookAndFeel roundedTextEditorLookAndFeel;
    juce::Label titleLabel;
    juce::Label sourceNameLabel;
    juce::TextEditor sourceNameEditor;
    juce::TextButton muteButton { "Mute" };

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (DesktopLoudnessMeterAudioProcessorEditor)
};
