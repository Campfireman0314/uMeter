#pragma once

#include "HostAppLauncher.h"
#include "PluginLocalization.h"
#include "PluginProcessor.h"

#include <juce_gui_basics/juce_gui_basics.h>

class DesktopLoudnessMeterAudioProcessorEditor final : public juce::AudioProcessorEditor,
                                                       private juce::Timer
{
public:
    explicit DesktopLoudnessMeterAudioProcessorEditor (DesktopLoudnessMeterAudioProcessor& processor);
    ~DesktopLoudnessMeterAudioProcessorEditor() override;

    void paint (juce::Graphics& graphics) override;
    void resized() override;

private:
    class StatusIndicator final : public juce::Component
    {
    public:
        void setState (dlm::LoopbackSenderConnectionState nextState);
        void paint (juce::Graphics& graphics) override;

    private:
        dlm::LoopbackSenderConnectionState state = dlm::LoopbackSenderConnectionState::Stopped;
    };

    class RoundedTextEditorLookAndFeel final : public juce::LookAndFeel_V4
    {
    public:
        void fillTextEditorBackground (juce::Graphics& graphics, int width, int height, juce::TextEditor& textEditor) override;
        void drawTextEditorOutline (juce::Graphics& graphics, int width, int height, juce::TextEditor& textEditor) override;
    };

    void updateSourceName (bool refreshEditorText);
    void refreshMuteButton();
    void refreshConnectionStatus();
    void launchHostApp();
    void timerCallback() override;

    DesktopLoudnessMeterAudioProcessor& audioProcessor;
    dlm::HostAppLauncher hostAppLauncher;
    dlm::PluginLocalization localization;
    RoundedTextEditorLookAndFeel roundedTextEditorLookAndFeel;
    juce::Label titleLabel;
    StatusIndicator statusIndicator;
    juce::Label statusLabel;
    juce::Label sourceNameLabel;
    juce::TextEditor sourceNameEditor;
    juce::TextButton muteButton;
    juce::TextButton startHostButton;
    juce::Label hostLaunchStatusLabel;
    int hostLaunchStatusTicksRemaining = 0;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (DesktopLoudnessMeterAudioProcessorEditor)
};
