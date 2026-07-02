#include "DlmSourceIngestCore.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <map>
#include <memory>
#include <string>
#include <vector>

namespace
{
    constexpr int DefaultTruePeakOversampling = 4;
    constexpr int SourceStringFieldHandle = 0;
    constexpr int SourceStringFieldDisplayName = 1;

    bool IsSupportedOversamplingValue(int value)
    {
        return value == 2 || value == 4 || value == 8;
    }

    float ClampPeak(float peak)
    {
        if (!std::isfinite(peak) || peak <= 0.0f)
        {
            return 0.0f;
        }

        return peak >= 1.0f ? 1.0f : peak;
    }

    float CalculatePeak(const float* samples, int sampleCount)
    {
        float peak = 0.0f;
        for (int index = 0; index < sampleCount; ++index)
        {
            const float sample = samples[index];
            if (!std::isfinite(sample))
            {
                continue;
            }

            peak = (std::max)(peak, std::abs(sample));
        }

        return ClampPeak(peak);
    }

    int CopyStringToBuffer(const std::string& value, char* buffer, int capacity, int* actualByteCount)
    {
        if (buffer == nullptr || actualByteCount == nullptr || capacity < 0)
        {
            return -2;
        }

        const int count = static_cast<int>(value.size());
        if (capacity < count)
        {
            return -5;
        }

        if (count > 0)
        {
            std::memcpy(buffer, value.data(), static_cast<size_t>(count));
        }

        *actualByteCount = count;
        return 0;
    }

    struct SourceSettings
    {
        int truePeakOversampling = DefaultTruePeakOversampling;
        bool channelWaveformEnabled = false;
        bool channelSpectrumEnabled = false;
    };

    struct SourceState
    {
        SourceState(
            std::string sourceHandle,
            std::string sourceDisplayName,
            int sourceKindValue,
            int sourceSampleRate,
            int sourceChannels,
            const SourceSettings& sourceSettings,
            int performanceModeValue,
            long long timestampMilliseconds)
            : handle(std::move(sourceHandle)),
              displayName(std::move(sourceDisplayName)),
              sourceKind(sourceKindValue),
              sampleRate(sourceSampleRate),
              channels(sourceChannels),
              settings(sourceSettings),
              performanceMode(performanceModeValue),
              lastSeenMilliseconds(timestampMilliseconds)
        {
            RecreateAnalyzer();
        }

        ~SourceState()
        {
            DlmPcmAnalyzerDestroy(analyzer);
        }

        SourceState(const SourceState&) = delete;
        SourceState& operator=(const SourceState&) = delete;

        std::string handle;
        std::string displayName;
        int sourceKind = 0;
        int sampleRate = 0;
        int channels = 0;
        SourceSettings settings;
        int performanceMode = 0;
        long long lastSeenMilliseconds = 0;
        float peak = 0.0f;
        void* analyzer = nullptr;

        int Reconfigure(
            std::string sourceDisplayName,
            int sourceKindValue,
            int sourceSampleRate,
            int sourceChannels,
            long long timestampMilliseconds)
        {
            displayName = std::move(sourceDisplayName);
            sourceKind = sourceKindValue;
            lastSeenMilliseconds = timestampMilliseconds;

            if (sampleRate == sourceSampleRate && channels == sourceChannels)
            {
                return 0;
            }

            sampleRate = sourceSampleRate;
            channels = sourceChannels;
            peak = 0.0f;
            return RecreateAnalyzer();
        }

        int Process(const float* samples, int sampleCount, long long timestampMilliseconds)
        {
            if (samples == nullptr || sampleCount <= 0 || sampleCount % channels != 0)
            {
                return -3;
            }

            const int result = DlmPcmAnalyzerProcessF32(analyzer, samples, sampleCount);
            if (result != 0)
            {
                return result;
            }

            peak = CalculatePeak(samples, sampleCount);
            lastSeenMilliseconds = timestampMilliseconds;
            return 0;
        }

        int Reset()
        {
            peak = 0.0f;
            return DlmPcmAnalyzerReset(analyzer);
        }

        int SetTruePeakOversampling(int oversampling)
        {
            if (!IsSupportedOversamplingValue(oversampling))
            {
                return -3;
            }

            if (settings.truePeakOversampling == oversampling)
            {
                return 0;
            }

            settings.truePeakOversampling = oversampling;
            peak = 0.0f;
            return RecreateAnalyzer();
        }

        int SetChannelWaveformEnabled(bool enabled)
        {
            settings.channelWaveformEnabled = enabled;
            return DlmPcmAnalyzerSetChannelWaveformEnabled(analyzer, enabled ? 1 : 0);
        }

        int SetChannelSpectrumEnabled(bool enabled)
        {
            settings.channelSpectrumEnabled = enabled;
            return DlmPcmAnalyzerSetChannelSpectrumEnabled(analyzer, enabled ? 1 : 0);
        }

        int SetPerformanceMode(int mode)
        {
            performanceMode = mode;
            return DlmPcmAnalyzerSetPerformanceMode(analyzer, mode);
        }

    private:
        int RecreateAnalyzer()
        {
            void* newAnalyzer = nullptr;
            const int result = DlmPcmAnalyzerCreate(sampleRate, channels, settings.truePeakOversampling, &newAnalyzer);
            if (result != 0)
            {
                return result;
            }

            DlmPcmAnalyzerSetChannelWaveformEnabled(newAnalyzer, settings.channelWaveformEnabled ? 1 : 0);
            DlmPcmAnalyzerSetChannelSpectrumEnabled(newAnalyzer, settings.channelSpectrumEnabled ? 1 : 0);
            DlmPcmAnalyzerSetPerformanceMode(newAnalyzer, performanceMode);
            DlmPcmAnalyzerDestroy(analyzer);
            analyzer = newAnalyzer;
            return 0;
        }
    };

    struct DlmSourceIngestCoreHandle
    {
        std::map<std::string, std::unique_ptr<SourceState>> sourcesByHandle;
        std::map<std::string, SourceSettings> settingsByHandle;
        int performanceMode = 0;

        SourceSettings SettingsForHandle(const std::string& sourceHandle)
        {
            auto [iterator, _] = settingsByHandle.try_emplace(sourceHandle);
            return iterator->second;
        }

        std::vector<SourceState*> OrderedSources()
        {
            std::vector<SourceState*> sources;
            sources.reserve(sourcesByHandle.size());
            for (auto& entry : sourcesByHandle)
            {
                sources.push_back(entry.second.get());
            }

            std::sort(sources.begin(), sources.end(), [](const SourceState* left, const SourceState* right)
            {
                if (left->displayName == right->displayName)
                {
                    return left->handle < right->handle;
                }

                return left->displayName < right->displayName;
            });
            return sources;
        }
    };

    DlmSourceIngestCoreHandle* AsCore(void* handle)
    {
        return static_cast<DlmSourceIngestCoreHandle*>(handle);
    }

    SourceState* FindSource(DlmSourceIngestCoreHandle* core, const char* sourceHandle)
    {
        if (core == nullptr || sourceHandle == nullptr || sourceHandle[0] == '\0')
        {
            return nullptr;
        }

        auto iterator = core->sourcesByHandle.find(sourceHandle);
        return iterator == core->sourcesByHandle.end() ? nullptr : iterator->second.get();
    }

    SourceState* SourceAt(DlmSourceIngestCoreHandle* core, int index)
    {
        if (core == nullptr || index < 0)
        {
            return nullptr;
        }

        auto sources = core->OrderedSources();
        if (index >= static_cast<int>(sources.size()))
        {
            return nullptr;
        }

        return sources[static_cast<size_t>(index)];
    }
}

int DlmSourceIngestCoreCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmSourceIngestCoreHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmSourceIngestCoreDestroy(void* handle)
{
    delete AsCore(handle);
}

int DlmSourceIngestCoreRegister(
    void* handle,
    const char* sourceHandle,
    const char* displayName,
    int sourceKind,
    int sampleRate,
    int channels,
    long long timestampMilliseconds)
{
    auto* core = AsCore(handle);
    if (core == nullptr || sourceHandle == nullptr || sourceHandle[0] == '\0' || displayName == nullptr)
    {
        return -2;
    }

    if (sampleRate <= 0 || channels < 1 || channels > 2)
    {
        return -3;
    }

    try
    {
        auto iterator = core->sourcesByHandle.find(sourceHandle);
        if (iterator != core->sourcesByHandle.end())
        {
            return iterator->second->Reconfigure(displayName, sourceKind, sampleRate, channels, timestampMilliseconds);
        }

        auto source = std::make_unique<SourceState>(
            sourceHandle,
            displayName,
            sourceKind,
            sampleRate,
            channels,
            core->SettingsForHandle(sourceHandle),
            core->performanceMode,
            timestampMilliseconds);
        core->sourcesByHandle[sourceHandle] = std::move(source);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmSourceIngestCoreHeartbeat(void* handle, const char* sourceHandle, long long timestampMilliseconds)
{
    auto* source = FindSource(AsCore(handle), sourceHandle);
    if (source == nullptr)
    {
        return 0;
    }

    source->lastSeenMilliseconds = timestampMilliseconds;
    return 0;
}

int DlmSourceIngestCoreRename(void* handle, const char* sourceHandle, const char* displayName, long long timestampMilliseconds)
{
    auto* source = FindSource(AsCore(handle), sourceHandle);
    if (source == nullptr || displayName == nullptr)
    {
        return 0;
    }

    source->displayName = displayName;
    source->lastSeenMilliseconds = timestampMilliseconds;
    return 0;
}

int DlmSourceIngestCoreProcessF32(
    void* handle,
    const char* sourceHandle,
    const float* interleavedSamples,
    int sampleCount,
    long long timestampMilliseconds)
{
    auto* source = FindSource(AsCore(handle), sourceHandle);
    if (source == nullptr || interleavedSamples == nullptr || sampleCount <= 0 || sampleCount % source->channels != 0)
    {
        return 0;
    }

    try
    {
        return source->Process(interleavedSamples, sampleCount, timestampMilliseconds);
    }
    catch (...)
    {
        return -4;
    }
}

int DlmSourceIngestCoreClose(void* handle, const char* sourceHandle)
{
    auto* core = AsCore(handle);
    if (core == nullptr || sourceHandle == nullptr)
    {
        return -2;
    }

    core->sourcesByHandle.erase(sourceHandle);
    return 0;
}

int DlmSourceIngestCoreRemoveExpired(void* handle, long long nowMilliseconds, long long timeoutMilliseconds)
{
    auto* core = AsCore(handle);
    if (core == nullptr)
    {
        return -2;
    }

    if (timeoutMilliseconds <= 0)
    {
        return 0;
    }

    for (auto iterator = core->sourcesByHandle.begin(); iterator != core->sourcesByHandle.end();)
    {
        if (nowMilliseconds - iterator->second->lastSeenMilliseconds > timeoutMilliseconds)
        {
            iterator = core->sourcesByHandle.erase(iterator);
        }
        else
        {
            ++iterator;
        }
    }

    return 0;
}

int DlmSourceIngestCoreResetSource(void* handle, const char* sourceHandle)
{
    auto* source = FindSource(AsCore(handle), sourceHandle);
    return source == nullptr ? 0 : source->Reset();
}

int DlmSourceIngestCoreResetAll(void* handle)
{
    auto* core = AsCore(handle);
    if (core == nullptr)
    {
        return -2;
    }

    for (auto& entry : core->sourcesByHandle)
    {
        const int result = entry.second->Reset();
        if (result != 0)
        {
            return result;
        }
    }

    return 0;
}

int DlmSourceIngestCoreSetTruePeakOversampling(void* handle, const char* sourceHandle, int oversampling)
{
    auto* core = AsCore(handle);
    if (core == nullptr || sourceHandle == nullptr || !IsSupportedOversamplingValue(oversampling))
    {
        return -3;
    }

    auto [settingsIterator, _] = core->settingsByHandle.try_emplace(sourceHandle);
    settingsIterator->second.truePeakOversampling = oversampling;
    auto* source = FindSource(core, sourceHandle);
    return source == nullptr ? 0 : source->SetTruePeakOversampling(oversampling);
}

int DlmSourceIngestCoreSetChannelWaveformEnabled(void* handle, const char* sourceHandle, int enabled)
{
    auto* core = AsCore(handle);
    if (core == nullptr || sourceHandle == nullptr)
    {
        return -2;
    }

    const bool isEnabled = enabled != 0;
    auto [settingsIterator, _] = core->settingsByHandle.try_emplace(sourceHandle);
    settingsIterator->second.channelWaveformEnabled = isEnabled;
    auto* source = FindSource(core, sourceHandle);
    return source == nullptr ? 0 : source->SetChannelWaveformEnabled(isEnabled);
}

int DlmSourceIngestCoreSetChannelSpectrumEnabled(void* handle, const char* sourceHandle, int enabled)
{
    auto* core = AsCore(handle);
    if (core == nullptr || sourceHandle == nullptr)
    {
        return -2;
    }

    const bool isEnabled = enabled != 0;
    auto [settingsIterator, _] = core->settingsByHandle.try_emplace(sourceHandle);
    settingsIterator->second.channelSpectrumEnabled = isEnabled;
    auto* source = FindSource(core, sourceHandle);
    return source == nullptr ? 0 : source->SetChannelSpectrumEnabled(isEnabled);
}

int DlmSourceIngestCoreSetPerformanceMode(void* handle, int mode)
{
    auto* core = AsCore(handle);
    if (core == nullptr)
    {
        return -2;
    }

    if (mode != 0 && mode != 1)
    {
        return -3;
    }

    core->performanceMode = mode;
    for (auto& entry : core->sourcesByHandle)
    {
        const int result = entry.second->SetPerformanceMode(mode);
        if (result != 0)
        {
            return result;
        }
    }

    return 0;
}

int DlmSourceIngestCoreGetSourceCount(void* handle, int* count)
{
    auto* core = AsCore(handle);
    if (core == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(core->sourcesByHandle.size());
    return 0;
}

int DlmSourceIngestCoreGetSourceSnapshotAt(void* handle, int index, DlmSourceIngestSnapshot* snapshot)
{
    auto* source = SourceAt(AsCore(handle), index);
    if (source == nullptr || snapshot == nullptr)
    {
        return -2;
    }

    *snapshot = {};
    snapshot->sourceKind = source->sourceKind;
    snapshot->sampleRate = source->sampleRate;
    snapshot->channels = source->channels;
    snapshot->lastSeenMilliseconds = source->lastSeenMilliseconds;
    snapshot->peak = source->peak;
    snapshot->handleUtf8ByteCount = static_cast<int>(source->handle.size());
    snapshot->displayNameUtf8ByteCount = static_cast<int>(source->displayName.size());
    return DlmPcmAnalyzerGetSnapshot(source->analyzer, &snapshot->metrics);
}

int DlmSourceIngestCoreCopySourceStringAt(void* handle, int index, int field, char* buffer, int capacity, int* actualByteCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    if (source == nullptr)
    {
        return -2;
    }

    if (field == SourceStringFieldHandle)
    {
        return CopyStringToBuffer(source->handle, buffer, capacity, actualByteCount);
    }

    if (field == SourceStringFieldDisplayName)
    {
        return CopyStringToBuffer(source->displayName, buffer, capacity, actualByteCount);
    }

    return -3;
}

int DlmSourceIngestCoreCopyWaveformAt(void* handle, int index, double* buffer, int capacity, int* actualCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    return source == nullptr ? -2 : DlmPcmAnalyzerCopyWaveform(source->analyzer, buffer, capacity, actualCount);
}

int DlmSourceIngestCoreCopyChannelWaveformAt(void* handle, int index, int channel, double* buffer, int capacity, int* actualCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    return source == nullptr ? -2 : DlmPcmAnalyzerCopyChannelWaveform(source->analyzer, channel, buffer, capacity, actualCount);
}

int DlmSourceIngestCoreCopySpectrumAt(void* handle, int index, double* buffer, int capacity, int* actualCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    return source == nullptr ? -2 : DlmPcmAnalyzerCopySpectrum(source->analyzer, buffer, capacity, actualCount);
}

int DlmSourceIngestCoreCopyChannelSpectrumAt(void* handle, int index, int channel, double* buffer, int capacity, int* actualCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    return source == nullptr ? -2 : DlmPcmAnalyzerCopyChannelSpectrum(source->analyzer, channel, buffer, capacity, actualCount);
}

int DlmSourceIngestCoreCopyStereoImagePointsAt(
    void* handle,
    int index,
    double* angleBuffer,
    double* radiusBuffer,
    int capacity,
    int* actualCount)
{
    auto* source = SourceAt(AsCore(handle), index);
    return source == nullptr
        ? -2
        : DlmPcmAnalyzerCopyStereoImagePoints(source->analyzer, angleBuffer, radiusBuffer, capacity, actualCount);
}
