#include "PluginEditor.h"

namespace
{
constexpr auto textEditorCornerRadius = 8.0f;
constexpr auto textEditorTopIndent = 1;
constexpr auto textEditorHorizontalViewportInset = 1;

juce::Colour statusColour (dlm::LoopbackSenderConnectionState state)
{
    switch (state)
    {
        case dlm::LoopbackSenderConnectionState::Connected:
            return juce::Colour (0xff2dd4bf);
        case dlm::LoopbackSenderConnectionState::NoRecentAck:
            return juce::Colour (0xfff59e0b);
        case dlm::LoopbackSenderConnectionState::WaitingForReceiver:
            return juce::Colour (0xff94a3b8);
        case dlm::LoopbackSenderConnectionState::Stopped:
        default:
            return juce::Colour (0xff64748b);
    }
}
}

DesktopLoudnessMeterAudioProcessorEditor::DesktopLoudnessMeterAudioProcessorEditor (DesktopLoudnessMeterAudioProcessor& processor)
    : AudioProcessorEditor (&processor),
      audioProcessor (processor)
{
    setSize (560, 164);

    titleLabel.setText ("uMeter Sender", juce::dontSendNotification);
    titleLabel.setJustificationType (juce::Justification::centredLeft);
    titleLabel.setColour (juce::Label::textColourId, juce::Colour (0xfff2f5f8));
    titleLabel.setFont (juce::FontOptions (18.0f, juce::Font::bold));
    addAndMakeVisible (titleLabel);

    startHostButton.setButtonText (localization.translate ("Start uMeter"));
    startHostButton.setColour (juce::TextButton::buttonColourId, juce::Colour (0xff176b64));
    startHostButton.setColour (juce::TextButton::textColourOffId, juce::Colours::white);
    startHostButton.onClick = [this] { launchHostApp(); };
    addAndMakeVisible (startHostButton);

    statusLabel.setJustificationType (juce::Justification::centredLeft);
    statusLabel.setColour (juce::Label::textColourId, juce::Colour (0xffaab4c2));
    statusLabel.setFont (juce::FontOptions (13.0f, juce::Font::plain));
    addAndMakeVisible (statusIndicator);
    addAndMakeVisible (statusLabel);

    sourceNameLabel.setText (localization.translate ("Source name"), juce::dontSendNotification);
    sourceNameLabel.setJustificationType (juce::Justification::centredLeft);
    sourceNameLabel.setColour (juce::Label::textColourId, juce::Colour (0xffaab4c2));
    addAndMakeVisible (sourceNameLabel);

    sourceNameEditor.setLookAndFeel (&roundedTextEditorLookAndFeel);
    sourceNameEditor.setMultiLine (false);
    sourceNameEditor.setReturnKeyStartsNewLine (false);
    sourceNameEditor.setScrollbarsShown (false);
    sourceNameEditor.setPopupMenuEnabled (true);
    sourceNameEditor.setSelectAllWhenFocused (true);
    sourceNameEditor.setJustification (juce::Justification::centredLeft);
    sourceNameEditor.setIndents (12, textEditorTopIndent);
    sourceNameEditor.setBorder (juce::BorderSize<int> (textEditorHorizontalViewportInset));
    sourceNameEditor.setFont (juce::FontOptions (15.0f, juce::Font::plain));
    sourceNameEditor.setColour (juce::TextEditor::backgroundColourId, juce::Colour (0xff17202b));
    sourceNameEditor.setColour (juce::TextEditor::outlineColourId, juce::Colour (0xff394658));
    sourceNameEditor.setColour (juce::TextEditor::focusedOutlineColourId, juce::Colour (0xff60a5fa));
    sourceNameEditor.setColour (juce::TextEditor::textColourId, juce::Colour (0xfff2f5f8));
    sourceNameEditor.setColour (juce::TextEditor::highlightColourId, juce::Colour (0xff2563eb).withAlpha (0.48f));
    sourceNameEditor.setColour (juce::TextEditor::highlightedTextColourId, juce::Colours::white);
    sourceNameEditor.setColour (juce::TextEditor::shadowColourId, juce::Colours::transparentBlack);
    sourceNameEditor.setText (audioProcessor.getSourceDisplayName(), juce::dontSendNotification);
    sourceNameEditor.onTextChange = [this] { updateSourceName (false); };
    sourceNameEditor.onReturnKey = [this] { updateSourceName (true); };
    sourceNameEditor.onFocusLost = [this] { updateSourceName (true); };
    addAndMakeVisible (sourceNameEditor);

    muteButton.setClickingTogglesState (true);
    muteButton.setColour (juce::TextButton::buttonColourId, juce::Colour (0xff243142));
    muteButton.setColour (juce::TextButton::buttonOnColourId, juce::Colour (0xffef4444));
    muteButton.setColour (juce::TextButton::textColourOffId, juce::Colour (0xffd8e1ec));
    muteButton.setColour (juce::TextButton::textColourOnId, juce::Colours::white);
    muteButton.setToggleState (audioProcessor.isMuted(), juce::dontSendNotification);
    muteButton.onClick = [this]
    {
        audioProcessor.setMuted (muteButton.getToggleState());
        refreshMuteButton();
    };
    refreshMuteButton();
    addAndMakeVisible (muteButton);

    hostLaunchStatusLabel.setJustificationType (juce::Justification::centredRight);
    hostLaunchStatusLabel.setColour (juce::Label::textColourId, juce::Colour (0xffaab4c2));
    hostLaunchStatusLabel.setFont (juce::FontOptions (12.0f, juce::Font::plain));
    addAndMakeVisible (hostLaunchStatusLabel);

    refreshConnectionStatus();
    startTimerHz (4);
}

DesktopLoudnessMeterAudioProcessorEditor::~DesktopLoudnessMeterAudioProcessorEditor()
{
    sourceNameEditor.setLookAndFeel (nullptr);
}

void DesktopLoudnessMeterAudioProcessorEditor::RoundedTextEditorLookAndFeel::fillTextEditorBackground (
    juce::Graphics& graphics,
    int width,
    int height,
    juce::TextEditor& textEditor)
{
    const auto area = juce::Rectangle<float> (0.5f, 0.5f, static_cast<float> (width) - 1.0f, static_cast<float> (height) - 1.0f);
    const auto background = textEditor.findColour (juce::TextEditor::backgroundColourId);

    graphics.setColour (textEditor.hasKeyboardFocus (true)
                            ? background.brighter (0.06f)
                            : background);
    graphics.fillRoundedRectangle (area, textEditorCornerRadius);
}

void DesktopLoudnessMeterAudioProcessorEditor::RoundedTextEditorLookAndFeel::drawTextEditorOutline (
    juce::Graphics& graphics,
    int width,
    int height,
    juce::TextEditor& textEditor)
{
    const auto area = juce::Rectangle<float> (1.0f, 1.0f, static_cast<float> (width) - 2.0f, static_cast<float> (height) - 2.0f);
    const auto hasFocus = textEditor.hasKeyboardFocus (true) && ! textEditor.isReadOnly();

    graphics.setColour (hasFocus
                            ? textEditor.findColour (juce::TextEditor::focusedOutlineColourId)
                            : textEditor.findColour (juce::TextEditor::outlineColourId));
    graphics.drawRoundedRectangle (area, textEditorCornerRadius, hasFocus ? 1.6f : 1.0f);
}

void DesktopLoudnessMeterAudioProcessorEditor::StatusIndicator::setState (dlm::LoopbackSenderConnectionState nextState)
{
    if (state == nextState)
    {
        return;
    }

    state = nextState;
    repaint();
}

void DesktopLoudnessMeterAudioProcessorEditor::StatusIndicator::paint (juce::Graphics& graphics)
{
    const auto area = getLocalBounds().toFloat();
    const auto size = juce::jmin (area.getWidth(), area.getHeight());
    const auto outer = juce::Rectangle<float> (size, size).withCentre (area.getCentre()).reduced (1.5f);
    const auto inner = outer.reduced (4.0f);
    const auto colour = statusColour (state);

    graphics.setColour (colour.withAlpha (0.16f));
    graphics.fillEllipse (outer);
    graphics.setColour (colour.withAlpha (0.42f));
    graphics.drawEllipse (outer.reduced (1.0f), 1.1f);
    graphics.setColour (colour);
    graphics.fillEllipse (inner);
}

void DesktopLoudnessMeterAudioProcessorEditor::paint (juce::Graphics& graphics)
{
    graphics.fillAll (juce::Colour (0xff0f1722));
}

void DesktopLoudnessMeterAudioProcessorEditor::resized()
{
    auto bounds = getLocalBounds().reduced (18);
    auto header = bounds.removeFromTop (28);
    startHostButton.setBounds (header.removeFromRight (128));
    header.removeFromRight (10);
    titleLabel.setBounds (header);
    bounds.removeFromTop (8);

    auto statusArea = bounds.removeFromTop (24);
    statusIndicator.setBounds (statusArea.removeFromLeft (18).withSizeKeepingCentre (18, 18));
    statusArea.removeFromLeft (7);
    statusLabel.setBounds (statusArea);
    bounds.removeFromTop (8);

    auto row = bounds.removeFromTop (42);
    sourceNameLabel.setBounds (row.removeFromLeft (112));
    row.removeFromLeft (8);
    muteButton.setBounds (row.removeFromRight (92));
    row.removeFromRight (10);
    sourceNameEditor.setBounds (row);

    bounds.removeFromTop (4);
    hostLaunchStatusLabel.setBounds (bounds.removeFromTop (18));
}

void DesktopLoudnessMeterAudioProcessorEditor::updateSourceName (bool refreshEditorText)
{
    audioProcessor.setSourceDisplayName (sourceNameEditor.getText());

    if (refreshEditorText)
    {
        sourceNameEditor.setText (audioProcessor.getSourceDisplayName(), juce::dontSendNotification);
    }
}

void DesktopLoudnessMeterAudioProcessorEditor::refreshMuteButton()
{
    muteButton.setButtonText (localization.translate (audioProcessor.isMuted() ? "Muted" : "Mute"));
}

void DesktopLoudnessMeterAudioProcessorEditor::refreshConnectionStatus()
{
    const auto state = audioProcessor.getSenderConnectionState();
    auto text = localization.translate (audioProcessor.getSenderConnectionStatusText());
    const auto dropped = audioProcessor.getDroppedSenderPacketCount();
    if (dropped > 0)
    {
        text += " - " + localization.translate ("dropped") + " " + juce::String (static_cast<juce::int64> (dropped));
    }

    statusIndicator.setState (state);
    statusLabel.setText (text, juce::dontSendNotification);
    statusLabel.setColour (juce::Label::textColourId, statusColour (state).interpolatedWith (juce::Colour (0xffd8e1ec), 0.45f));
}

void DesktopLoudnessMeterAudioProcessorEditor::launchHostApp()
{
    jassert (juce::MessageManager::getInstance()->isThisTheMessageThread());

    const auto result = hostAppLauncher.launch();
    hostLaunchStatusLabel.setText (localization.translate (result.message), juce::dontSendNotification);
    hostLaunchStatusLabel.setColour (juce::Label::textColourId,
                                     result.started ? juce::Colour (0xff5eead4) : juce::Colour (0xfffca5a5));
    hostLaunchStatusTicksRemaining = 16;
}

void DesktopLoudnessMeterAudioProcessorEditor::timerCallback()
{
    refreshConnectionStatus();

    if (hostLaunchStatusTicksRemaining > 0 && --hostLaunchStatusTicksRemaining == 0)
    {
        hostLaunchStatusLabel.setText ({}, juce::dontSendNotification);
    }
}
