#pragma once

#include "LoopbackSender.h"

#include <juce_audio_processors/juce_audio_processors.h>

#include <atomic>

class DesktopLoudnessMeterAudioProcessor final : public juce::AudioProcessor
{
public:
    DesktopLoudnessMeterAudioProcessor();
    ~DesktopLoudnessMeterAudioProcessor() override;

    void prepareToPlay (double sampleRate, int samplesPerBlock) override;
    void releaseResources() override;

    bool isBusesLayoutSupported (const BusesLayout& layouts) const override;

    void processBlock (juce::AudioBuffer<float>& buffer, juce::MidiBuffer& midiMessages) override;
    using AudioProcessor::processBlock;

    juce::AudioProcessorEditor* createEditor() override;
    bool hasEditor() const override;

    const juce::String getName() const override;
    bool acceptsMidi() const override;
    bool producesMidi() const override;
    bool isMidiEffect() const override;
    double getTailLengthSeconds() const override;

    int getNumPrograms() override;
    int getCurrentProgram() override;
    void setCurrentProgram (int index) override;
    const juce::String getProgramName (int index) override;
    void changeProgramName (int index, const juce::String& newName) override;

    void getStateInformation (juce::MemoryBlock& destData) override;
    void setStateInformation (const void* data, int sizeInBytes) override;

    juce::String getSourceDisplayName() const;
    void setSourceDisplayName (const juce::String& newDisplayName);

    bool isMuted() const noexcept;
    void setMuted (bool shouldMute) noexcept;
    dlm::LoopbackSenderConnectionState getSenderConnectionState() const noexcept;
    juce::String getSenderConnectionStatusText() const;
    std::uint64_t getDroppedSenderPacketCount() const noexcept;

private:
    static juce::String getHostDisplayName();
    static juce::String normalizeDisplayName (const juce::String& value, const juce::String& fallbackHandle);

    mutable juce::CriticalSection stateLock;
    juce::String sourceHandle;
    juce::String displayName;
    std::atomic<bool> muted { false };
    dlm::LoopbackSender sender;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (DesktopLoudnessMeterAudioProcessor)
};
