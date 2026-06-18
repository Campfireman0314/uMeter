#include "PluginEditor.h"

namespace
{
constexpr auto textEditorCornerRadius = 8.0f;
constexpr auto textEditorTopIndent = 1;
constexpr auto textEditorHorizontalViewportInset = 1;
}

DesktopLoudnessMeterAudioProcessorEditor::DesktopLoudnessMeterAudioProcessorEditor (DesktopLoudnessMeterAudioProcessor& processor)
    : AudioProcessorEditor (&processor),
      audioProcessor (processor)
{
    setSize (420, 132);

    titleLabel.setText ("uMeter Sender", juce::dontSendNotification);
    titleLabel.setJustificationType (juce::Justification::centredLeft);
    titleLabel.setColour (juce::Label::textColourId, juce::Colour (0xfff2f5f8));
    titleLabel.setFont (juce::FontOptions (18.0f, juce::Font::bold));
    addAndMakeVisible (titleLabel);

    sourceNameLabel.setText ("Source name", juce::dontSendNotification);
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

void DesktopLoudnessMeterAudioProcessorEditor::paint (juce::Graphics& graphics)
{
    graphics.fillAll (juce::Colour (0xff0f1722));
}

void DesktopLoudnessMeterAudioProcessorEditor::resized()
{
    auto bounds = getLocalBounds().reduced (18);
    titleLabel.setBounds (bounds.removeFromTop (28));
    bounds.removeFromTop (12);

    auto row = bounds.removeFromTop (42);
    sourceNameLabel.setBounds (row.removeFromLeft (98));
    row.removeFromLeft (8);
    muteButton.setBounds (row.removeFromRight (92));
    row.removeFromRight (10);
    sourceNameEditor.setBounds (row);
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
    muteButton.setButtonText (audioProcessor.isMuted() ? "Muted" : "Mute");
}
