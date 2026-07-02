#include "DlmHostSelectedDetailShell.h"

#include <algorithm>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int DetailModuleStereo = 0;
    constexpr int DetailModuleCollision = 1;
    constexpr int DetailModuleLoudnessClock = 2;
    constexpr int DetailModuleWaveform = 3;
    constexpr int DetailModuleSpectrum = 4;
    constexpr int CollisionFrequencyScaleLogarithmic = 1;
    constexpr int StringFieldComparisonSourceKey = 0;
    constexpr int StringFieldComparisonDisplayTitle = 1;

    struct SourceInput
    {
        std::string key;
        std::string displayTitle;
    };

    struct DlmHostSelectedDetailShellHandle
    {
        std::vector<SourceInput> sources;
        std::vector<int> comparisonIndexes;
        std::string comparisonSourceKey;
        std::string comparisonDisplayTitle;
        bool hasSelectedSource = false;
        int selectedSourceIndex = -1;
        bool isWaveformVisible = false;
        bool isSpectrumVisible = false;
        bool isStereoVisible = false;
        bool isLoudnessClockVisible = false;
        bool isCollisionVisible = false;
        double levelFloorDb = -120.0;
        double prominenceThresholdDb = 6.0;
        double relativeThreshold = 0.18;
        int frequencyScale = CollisionFrequencyScaleLogarithmic;
    };

    DlmHostSelectedDetailShellHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSelectedDetailShellHandle*>(handle);
    }

    bool IsBlank(const char* value)
    {
        if (value == nullptr)
        {
            return true;
        }

        while (*value != '\0')
        {
            if (*value != ' ' && *value != '\t' && *value != '\r' && *value != '\n')
            {
                return false;
            }

            ++value;
        }

        return true;
    }

    int FindSourceIndex(const std::vector<SourceInput>& sources, const char* key)
    {
        if (IsBlank(key))
        {
            return -1;
        }

        for (int index = 0; index < static_cast<int>(sources.size()); ++index)
        {
            if (sources[static_cast<size_t>(index)].key == key)
            {
                return index;
            }
        }

        return -1;
    }

    void ResetPlan(DlmHostSelectedDetailShellHandle& planner)
    {
        planner.comparisonIndexes.clear();
        planner.comparisonSourceKey.clear();
        planner.comparisonDisplayTitle.clear();
        planner.hasSelectedSource = false;
        planner.selectedSourceIndex = -1;
        planner.isWaveformVisible = false;
        planner.isSpectrumVisible = false;
        planner.isStereoVisible = false;
        planner.isLoudnessClockVisible = false;
        planner.isCollisionVisible = false;
        planner.levelFloorDb = -120.0;
        planner.prominenceThresholdDb = 6.0;
        planner.relativeThreshold = 0.18;
        planner.frequencyScale = CollisionFrequencyScaleLogarithmic;
    }

    void SetVisible(DlmHostSelectedDetailShellHandle& planner, int kind, bool isVisible)
    {
        if (!isVisible)
        {
            return;
        }

        switch (kind)
        {
        case DetailModuleStereo:
            planner.isStereoVisible = true;
            break;
        case DetailModuleCollision:
            planner.isCollisionVisible = true;
            break;
        case DetailModuleLoudnessClock:
            planner.isLoudnessClockVisible = true;
            break;
        case DetailModuleWaveform:
            planner.isWaveformVisible = true;
            break;
        case DetailModuleSpectrum:
            planner.isSpectrumVisible = true;
            break;
        default:
            break;
        }
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
            *actualByteCount = count;
            return -5;
        }

        if (count > 0)
        {
            std::memcpy(buffer, value.data(), static_cast<size_t>(count));
        }

        *actualByteCount = count;
        return 0;
    }
}

int DlmHostSelectedDetailShellCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSelectedDetailShellHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSelectedDetailShellDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSelectedDetailShellClear(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->sources.clear();
    ResetPlan(*planner);
    return 0;
}

int DlmHostSelectedDetailShellAddSource(void* handle, const char* sourceKey, const char* displayTitle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || IsBlank(sourceKey))
    {
        return -2;
    }

    try
    {
        SourceInput source;
        source.key = sourceKey;
        source.displayTitle = displayTitle == nullptr ? std::string() : std::string(displayTitle);
        planner->sources.push_back(source);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedDetailShellAddModule(void* handle, int kind, int isVisible)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    SetVisible(*planner, kind, isVisible != 0);
    return 0;
}

int DlmHostSelectedDetailShellProject(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasLevelFloorDb,
    double levelFloorDb,
    int hasProminenceThresholdDb,
    double prominenceThresholdDb,
    int hasRelativeThreshold,
    double relativeThreshold,
    int hasFrequencyScale,
    int frequencyScale)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        const bool waveform = planner->isWaveformVisible;
        const bool spectrum = planner->isSpectrumVisible;
        const bool stereo = planner->isStereoVisible;
        const bool loudnessClock = planner->isLoudnessClockVisible;
        const bool collision = planner->isCollisionVisible;

        ResetPlan(*planner);
        planner->isWaveformVisible = waveform;
        planner->isSpectrumVisible = spectrum;
        planner->isStereoVisible = stereo;
        planner->isLoudnessClockVisible = loudnessClock;
        planner->isCollisionVisible = collision;
        planner->levelFloorDb = hasLevelFloorDb != 0 ? levelFloorDb : -120.0;
        planner->prominenceThresholdDb = hasProminenceThresholdDb != 0 ? prominenceThresholdDb : 6.0;
        planner->relativeThreshold = hasRelativeThreshold != 0 ? relativeThreshold : 0.18;
        planner->frequencyScale = hasFrequencyScale != 0 ? frequencyScale : CollisionFrequencyScaleLogarithmic;

        planner->selectedSourceIndex = FindSourceIndex(planner->sources, selectedSourceKey);
        planner->hasSelectedSource = planner->selectedSourceIndex >= 0;
        if (!planner->hasSelectedSource)
        {
            return 0;
        }

        if (!IsBlank(comparisonSourceKey))
        {
            planner->comparisonSourceKey = comparisonSourceKey;
            const int comparisonIndex = FindSourceIndex(planner->sources, comparisonSourceKey);
            if (comparisonIndex >= 0)
            {
                planner->comparisonIndexes.push_back(comparisonIndex);
                planner->comparisonDisplayTitle = planner->sources[static_cast<size_t>(comparisonIndex)].displayTitle;
                return 0;
            }
        }

        for (int index = 0; index < static_cast<int>(planner->sources.size()); ++index)
        {
            if (index != planner->selectedSourceIndex)
            {
                planner->comparisonIndexes.push_back(index);
            }
        }

        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedDetailShellGetPlan(void* handle, DlmHostSelectedDetailShellPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->hasSelectedSource = planner->hasSelectedSource ? 1 : 0;
    plan->selectedSourceIndex = planner->selectedSourceIndex;
    plan->isWaveformVisible = planner->isWaveformVisible ? 1 : 0;
    plan->isSpectrumVisible = planner->isSpectrumVisible ? 1 : 0;
    plan->isStereoVisible = planner->isStereoVisible ? 1 : 0;
    plan->isLoudnessClockVisible = planner->isLoudnessClockVisible ? 1 : 0;
    plan->isCollisionVisible = planner->isCollisionVisible ? 1 : 0;
    plan->comparisonSourceKeyUtf8ByteCount = static_cast<int>(planner->comparisonSourceKey.size());
    plan->comparisonDisplayTitleUtf8ByteCount = static_cast<int>(planner->comparisonDisplayTitle.size());
    plan->levelFloorDb = planner->levelFloorDb;
    plan->prominenceThresholdDb = planner->prominenceThresholdDb;
    plan->relativeThreshold = planner->relativeThreshold;
    plan->frequencyScale = planner->frequencyScale;
    return 0;
}

int DlmHostSelectedDetailShellGetComparisonIndexCount(void* handle, int* count)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(planner->comparisonIndexes.size());
    return 0;
}

int DlmHostSelectedDetailShellGetComparisonIndexAt(void* handle, int index, int* sourceIndex)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceIndex == nullptr || index < 0 || index >= static_cast<int>(planner->comparisonIndexes.size()))
    {
        return -2;
    }

    *sourceIndex = planner->comparisonIndexes[static_cast<size_t>(index)];
    return 0;
}

int DlmHostSelectedDetailShellCopyString(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case StringFieldComparisonSourceKey:
        return CopyStringToBuffer(planner->comparisonSourceKey, buffer, capacity, actualByteCount);
    case StringFieldComparisonDisplayTitle:
        return CopyStringToBuffer(planner->comparisonDisplayTitle, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}
