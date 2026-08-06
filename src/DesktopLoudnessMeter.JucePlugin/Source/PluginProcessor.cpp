#include "PluginProcessor.h"

#include "LoopbackProtocol.h"
#include "PluginEditor.h"
#include "PluginState.h"

#include <algorithm>
#include <cmath>

DesktopLoudnessMeterAudioProcessor::DesktopLoudnessMeterAudioProcessor()
    : AudioProcessor (BusesProperties()
                     #if ! JucePlugin_IsMidiEffect
                      #if ! JucePlugin_IsSynth
                       .withInput ("Input", juce::AudioChannelSet::stereo(), true)
                      #endif
                       .withOutput ("Output", juce::AudioChannelSet::stereo(), true)
                     #endif
                       )
{
    sourceHandle = dlm::LoopbackProtocol::createHandle();
    displayName = dlm::PluginState::defaultDisplayNameForHandle (sourceHandle, getHostDisplayName());
    hostAppLaunchPreferences = dlm::HostAppLaunchPreferences::load (dlm::PluginLocalization::defaultSettingsFile());
    if (hostAppLaunchPreferences.autoInvoke)
    {
        startTimer (1000);
    }
}

DesktopLoudnessMeterAudioProcessor::~DesktopLoudnessMeterAudioProcessor()
{
    stopTimer();
    sender.stop();
}

void DesktopLoudnessMeterAudioProcessor::prepareToPlay (double sampleRate, int samplesPerBlock)
{
    const auto channels = juce::jlimit (1, 2, getTotalNumInputChannels());
    const auto requestedSamples = std::max (samplesPerBlock * channels, channels);
    juce::String handle;
    juce::String name;

    {
        const juce::ScopedLock lock (stateLock);
        handle = sourceHandle;
        name = displayName;
    }

    dlm::LoopbackSenderConfig config;
    config.handle = handle;
    config.displayName = name;
    config.sampleRate = static_cast<int> (std::round (sampleRate));
    config.channels = channels;
    config.maxSamplesPerPacket = juce::jlimit (channels, 8192, requestedSamples);

    sender.start (config);
}

void DesktopLoudnessMeterAudioProcessor::releaseResources()
{
    // Hosts may release audio resources while sleeping or deactivating the graph without
    // preparing the processor again on resume. Discovery stays alive until destruction.
}

bool DesktopLoudnessMeterAudioProcessor::isBusesLayoutSupported (const BusesLayout& layouts) const
{
   #if JucePlugin_IsMidiEffect
    juce::ignoreUnused (layouts);
    return true;
   #else
    const auto output = layouts.getMainOutputChannelSet();

    if (output != juce::AudioChannelSet::mono()
        && output != juce::AudioChannelSet::stereo())
    {
        return false;
    }

   #if ! JucePlugin_IsSynth
    if (output != layouts.getMainInputChannelSet())
    {
        return false;
    }
   #endif

    return true;
   #endif
}

void DesktopLoudnessMeterAudioProcessor::processBlock (juce::AudioBuffer<float>& buffer, juce::MidiBuffer& midiMessages)
{
    juce::ignoreUnused (midiMessages);

    juce::ScopedNoDenormals noDenormals;
    const auto totalNumInputChannels = getTotalNumInputChannels();
    const auto totalNumOutputChannels = getTotalNumOutputChannels();

    for (auto channel = totalNumInputChannels; channel < totalNumOutputChannels; ++channel)
    {
        buffer.clear (channel, 0, buffer.getNumSamples());
    }

    if (muted.load (std::memory_order_acquire))
    {
        buffer.clear();
    }

    if (totalNumInputChannels > 0)
    {
        sender.tryEnqueue (buffer, juce::jlimit (1, 2, totalNumInputChannels));
    }
}

juce::AudioProcessorEditor* DesktopLoudnessMeterAudioProcessor::createEditor()
{
    return new DesktopLoudnessMeterAudioProcessorEditor (*this);
}

bool DesktopLoudnessMeterAudioProcessor::hasEditor() const
{
    return true;
}

const juce::String DesktopLoudnessMeterAudioProcessor::getName() const
{
    return JucePlugin_Name;
}

bool DesktopLoudnessMeterAudioProcessor::acceptsMidi() const
{
    return false;
}

bool DesktopLoudnessMeterAudioProcessor::producesMidi() const
{
    return false;
}

bool DesktopLoudnessMeterAudioProcessor::isMidiEffect() const
{
    return false;
}

double DesktopLoudnessMeterAudioProcessor::getTailLengthSeconds() const
{
    return 0.0;
}

int DesktopLoudnessMeterAudioProcessor::getNumPrograms()
{
    return 1;
}

int DesktopLoudnessMeterAudioProcessor::getCurrentProgram()
{
    return 0;
}

void DesktopLoudnessMeterAudioProcessor::setCurrentProgram (int index)
{
    juce::ignoreUnused (index);
}

const juce::String DesktopLoudnessMeterAudioProcessor::getProgramName (int index)
{
    juce::ignoreUnused (index);
    return {};
}

void DesktopLoudnessMeterAudioProcessor::changeProgramName (int index, const juce::String& newName)
{
    juce::ignoreUnused (index, newName);
}

void DesktopLoudnessMeterAudioProcessor::getStateInformation (juce::MemoryBlock& destData)
{
    const juce::ScopedLock lock (stateLock);
    destData = dlm::PluginState::save (sourceHandle, displayName);
}

void DesktopLoudnessMeterAudioProcessor::setStateInformation (const void* data, int sizeInBytes)
{
    juce::String restoredHandle;
    juce::String restoredDisplayName;

    if (dlm::PluginState::tryRestore (data, sizeInBytes, restoredHandle, restoredDisplayName, getHostDisplayName()))
    {
        auto shouldRename = false;

        {
            const juce::ScopedLock lock (stateLock);
            shouldRename = sourceHandle == restoredHandle && displayName != restoredDisplayName;
            sourceHandle = restoredHandle;
            displayName = restoredDisplayName;
        }

        if (shouldRename)
        {
            sender.rename (restoredDisplayName);
        }
    }
}

juce::String DesktopLoudnessMeterAudioProcessor::getSourceDisplayName() const
{
    const juce::ScopedLock lock (stateLock);
    return displayName;
}

void DesktopLoudnessMeterAudioProcessor::setSourceDisplayName (const juce::String& newDisplayName)
{
    juce::String handle;
    {
        const juce::ScopedLock lock (stateLock);
        handle = sourceHandle;
    }

    const auto normalized = normalizeDisplayName (newDisplayName, handle);
    auto changed = false;

    {
        const juce::ScopedLock lock (stateLock);
        changed = displayName != normalized;
        displayName = normalized;
    }

    if (changed)
    {
        sender.rename (normalized);
    }
}

bool DesktopLoudnessMeterAudioProcessor::isMuted() const noexcept
{
    return muted.load (std::memory_order_acquire);
}

void DesktopLoudnessMeterAudioProcessor::setMuted (bool shouldMute) noexcept
{
    muted.store (shouldMute, std::memory_order_release);
}

dlm::LoopbackSenderConnectionState DesktopLoudnessMeterAudioProcessor::getSenderConnectionState() const noexcept
{
    return sender.getConnectionState();
}

juce::String DesktopLoudnessMeterAudioProcessor::getSenderConnectionStatusText() const
{
    return sender.getConnectionStatusText();
}

std::uint64_t DesktopLoudnessMeterAudioProcessor::getDroppedSenderPacketCount() const noexcept
{
    return sender.getDroppedPacketCount();
}

void DesktopLoudnessMeterAudioProcessor::timerCallback()
{
    const auto state = sender.getConnectionState();
    if (state == dlm::LoopbackSenderConnectionState::Connected)
    {
        stopTimer();
        return;
    }

    if (state == dlm::LoopbackSenderConnectionState::WaitingForReceiver
        || state == dlm::LoopbackSenderConnectionState::NoRecentAck)
    {
        stopTimer();
        hostAppLauncher.launch (hostAppLaunchPreferences.startHidden);
    }
}

juce::String DesktopLoudnessMeterAudioProcessor::getHostDisplayName()
{
    const auto hostPath = juce::File::getSpecialLocation (juce::File::hostApplicationPath);
    const auto hostName = hostPath.getFileNameWithoutExtension().trim();
    return hostName.isEmpty() ? dlm::PluginState::defaultDisplayName : hostName;
}

juce::String DesktopLoudnessMeterAudioProcessor::normalizeDisplayName (const juce::String& value, const juce::String& fallbackHandle)
{
    const auto trimmed = value.trim();
    return trimmed.isEmpty() ? dlm::PluginState::defaultDisplayNameForHandle (fallbackHandle, getHostDisplayName()) : trimmed;
}

juce::AudioProcessor* JUCE_CALLTYPE createPluginFilter()
{
    return new DesktopLoudnessMeterAudioProcessor();
}
