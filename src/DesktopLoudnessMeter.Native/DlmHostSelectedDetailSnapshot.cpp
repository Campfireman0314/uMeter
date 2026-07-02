#include "DlmHostSelectedDetailSnapshot.h"

#include "DlmHostSelectedDetailShell.h"

#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StringFieldComparisonSourceKey = 0;
    constexpr int StringFieldComparisonDisplayTitle = 1;

    struct SourceInput
    {
        std::string key;
        std::string displayTitle;
        std::vector<double> spectrum;
    };

    struct LayoutInput
    {
        int kind = 0;
        int orderIndex = 0;
        bool hasHeight = false;
        double height = 0.0;
    };

    struct ModuleRow
    {
        int kind = 0;
        int orderIndex = 0;
        bool isVisible = false;
        double height = 0.0;
        std::string title;
    };

    struct BandRow
    {
        double startRatio = 0.0;
        double endRatio = 0.0;
        double intensity = 0.0;
    };

    struct DlmHostSelectedDetailSnapshotHandle
    {
        std::vector<SourceInput> sources;
        std::vector<LayoutInput> layouts;
        std::vector<ModuleRow> modules;
        std::vector<BandRow> bands;
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
        int frequencyScale = 1;
    };

    constexpr double EmptySpectrumValue = 0.0;

    DlmHostSelectedDetailSnapshotHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSelectedDetailSnapshotHandle*>(handle);
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

    void ResetPlan(DlmHostSelectedDetailSnapshotHandle& planner)
    {
        planner.modules.clear();
        planner.bands.clear();
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
        planner.frequencyScale = 1;
    }

    int ProjectModules(
        DlmHostSelectedDetailSnapshotHandle& planner,
        int hasStereoVisible,
        int stereoVisible,
        int hasCollisionVisible,
        int collisionVisible,
        int hasLoudnessClockVisible,
        int loudnessClockVisible,
        int hasWaveformVisible,
        int waveformVisible,
        int hasSpectrumVisible,
        int spectrumVisible)
    {
        void* moduleHandle = nullptr;
        int result = DlmHostDetailModuleProjectionCreate(&moduleHandle);
        if (result != 0)
        {
            return result;
        }

        try
        {
            for (const auto& layout : planner.layouts)
            {
                result = DlmHostDetailModuleProjectionAddLayout(
                    moduleHandle,
                    layout.kind,
                    layout.orderIndex,
                    layout.hasHeight ? 1 : 0,
                    layout.height);
                if (result != 0)
                {
                    DlmHostDetailModuleProjectionDestroy(moduleHandle);
                    return result;
                }
            }

            result = DlmHostDetailModuleProjectionProject(
                moduleHandle,
                hasStereoVisible,
                stereoVisible,
                hasCollisionVisible,
                collisionVisible,
                hasLoudnessClockVisible,
                loudnessClockVisible,
                hasWaveformVisible,
                waveformVisible,
                hasSpectrumVisible,
                spectrumVisible);
            if (result != 0)
            {
                DlmHostDetailModuleProjectionDestroy(moduleHandle);
                return result;
            }

            int count = 0;
            result = DlmHostDetailModuleProjectionGetCount(moduleHandle, &count);
            if (result != 0)
            {
                DlmHostDetailModuleProjectionDestroy(moduleHandle);
                return result;
            }

            planner.modules.clear();
            for (int index = 0; index < count; ++index)
            {
                DlmHostDetailModuleProjectionRow nativeRow {};
                result = DlmHostDetailModuleProjectionGetAt(moduleHandle, index, &nativeRow);
                if (result != 0)
                {
                    DlmHostDetailModuleProjectionDestroy(moduleHandle);
                    return result;
                }

                std::string title;
                if (nativeRow.titleUtf8ByteCount > 0)
                {
                    title.resize(static_cast<size_t>(nativeRow.titleUtf8ByteCount));
                    int actualCount = 0;
                    result = DlmHostDetailModuleProjectionCopyStringAt(
                        moduleHandle,
                        index,
                        0,
                        title.data(),
                        nativeRow.titleUtf8ByteCount,
                        &actualCount);
                    if (result != 0)
                    {
                        DlmHostDetailModuleProjectionDestroy(moduleHandle);
                        return result;
                    }

                    title.resize(static_cast<size_t>(actualCount));
                }

                planner.modules.push_back(ModuleRow
                {
                    nativeRow.kind,
                    nativeRow.orderIndex,
                    nativeRow.isVisible != 0,
                    nativeRow.height,
                    title
                });
            }

            DlmHostDetailModuleProjectionDestroy(moduleHandle);
            return 0;
        }
        catch (...)
        {
            DlmHostDetailModuleProjectionDestroy(moduleHandle);
            return -4;
        }
    }

    int ProjectShellAndBands(
        DlmHostSelectedDetailSnapshotHandle& planner,
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
        void* shellHandle = nullptr;
        int result = DlmHostSelectedDetailShellCreate(&shellHandle);
        if (result != 0)
        {
            return result;
        }

        try
        {
            for (const auto& source : planner.sources)
            {
                result = DlmHostSelectedDetailShellAddSource(shellHandle, source.key.c_str(), source.displayTitle.c_str());
                if (result != 0)
                {
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }
            }

            for (const auto& module : planner.modules)
            {
                result = DlmHostSelectedDetailShellAddModule(shellHandle, module.kind, module.isVisible ? 1 : 0);
                if (result != 0)
                {
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }
            }

            result = DlmHostSelectedDetailShellProject(
                shellHandle,
                selectedSourceKey,
                comparisonSourceKey,
                hasLevelFloorDb,
                levelFloorDb,
                hasProminenceThresholdDb,
                prominenceThresholdDb,
                hasRelativeThreshold,
                relativeThreshold,
                hasFrequencyScale,
                frequencyScale);
            if (result != 0)
            {
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            DlmHostSelectedDetailShellPlan shellPlan {};
            result = DlmHostSelectedDetailShellGetPlan(shellHandle, &shellPlan);
            if (result != 0)
            {
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            planner.hasSelectedSource = shellPlan.hasSelectedSource != 0;
            planner.selectedSourceIndex = shellPlan.selectedSourceIndex;
            planner.isWaveformVisible = shellPlan.isWaveformVisible != 0;
            planner.isSpectrumVisible = shellPlan.isSpectrumVisible != 0;
            planner.isStereoVisible = shellPlan.isStereoVisible != 0;
            planner.isLoudnessClockVisible = shellPlan.isLoudnessClockVisible != 0;
            planner.isCollisionVisible = shellPlan.isCollisionVisible != 0;
            planner.levelFloorDb = shellPlan.levelFloorDb;
            planner.prominenceThresholdDb = shellPlan.prominenceThresholdDb;
            planner.relativeThreshold = shellPlan.relativeThreshold;
            planner.frequencyScale = shellPlan.frequencyScale;

            if (shellPlan.comparisonSourceKeyUtf8ByteCount > 0)
            {
                planner.comparisonSourceKey.resize(static_cast<size_t>(shellPlan.comparisonSourceKeyUtf8ByteCount));
                int actualCount = 0;
                result = DlmHostSelectedDetailShellCopyString(
                    shellHandle,
                    StringFieldComparisonSourceKey,
                    planner.comparisonSourceKey.data(),
                    shellPlan.comparisonSourceKeyUtf8ByteCount,
                    &actualCount);
                if (result != 0)
                {
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }

                planner.comparisonSourceKey.resize(static_cast<size_t>(actualCount));
            }

            if (shellPlan.comparisonDisplayTitleUtf8ByteCount > 0)
            {
                planner.comparisonDisplayTitle.resize(static_cast<size_t>(shellPlan.comparisonDisplayTitleUtf8ByteCount));
                int actualCount = 0;
                result = DlmHostSelectedDetailShellCopyString(
                    shellHandle,
                    StringFieldComparisonDisplayTitle,
                    planner.comparisonDisplayTitle.data(),
                    shellPlan.comparisonDisplayTitleUtf8ByteCount,
                    &actualCount);
                if (result != 0)
                {
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }

                planner.comparisonDisplayTitle.resize(static_cast<size_t>(actualCount));
            }

            if (!planner.hasSelectedSource
                || planner.selectedSourceIndex < 0
                || planner.selectedSourceIndex >= static_cast<int>(planner.sources.size()))
            {
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return 0;
            }

            void* collisionHandle = nullptr;
            result = DlmHostCollisionProjectionCreate(&collisionHandle);
            if (result != 0)
            {
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

                const auto& focus = planner.sources[static_cast<size_t>(planner.selectedSourceIndex)].spectrum;
                const double* focusValues = focus.empty() ? &EmptySpectrumValue : focus.data();
                result = DlmHostCollisionProjectionSetFocusSpectrum(
                    collisionHandle,
                    focusValues,
                    static_cast<int>(focus.size()));
            if (result != 0)
            {
                DlmHostCollisionProjectionDestroy(collisionHandle);
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            int comparisonCount = 0;
            result = DlmHostSelectedDetailShellGetComparisonIndexCount(shellHandle, &comparisonCount);
            if (result != 0)
            {
                DlmHostCollisionProjectionDestroy(collisionHandle);
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            for (int index = 0; index < comparisonCount; ++index)
            {
                int sourceIndex = -1;
                result = DlmHostSelectedDetailShellGetComparisonIndexAt(shellHandle, index, &sourceIndex);
                if (result != 0)
                {
                    DlmHostCollisionProjectionDestroy(collisionHandle);
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }

                if (sourceIndex < 0 || sourceIndex >= static_cast<int>(planner.sources.size()))
                {
                    continue;
                }

                const auto& comparison = planner.sources[static_cast<size_t>(sourceIndex)].spectrum;
                const double* comparisonValues = comparison.empty() ? &EmptySpectrumValue : comparison.data();
                result = DlmHostCollisionProjectionAddComparisonSpectrum(
                    collisionHandle,
                    comparisonValues,
                    static_cast<int>(comparison.size()));
                if (result != 0)
                {
                    DlmHostCollisionProjectionDestroy(collisionHandle);
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }
            }

            result = DlmHostCollisionProjectionProject(
                collisionHandle,
                planner.levelFloorDb,
                planner.prominenceThresholdDb,
                planner.relativeThreshold,
                planner.frequencyScale);
            if (result != 0)
            {
                DlmHostCollisionProjectionDestroy(collisionHandle);
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            int bandCount = 0;
            result = DlmHostCollisionProjectionGetBandCount(collisionHandle, &bandCount);
            if (result != 0)
            {
                DlmHostCollisionProjectionDestroy(collisionHandle);
                DlmHostSelectedDetailShellDestroy(shellHandle);
                return result;
            }

            planner.bands.clear();
            for (int index = 0; index < bandCount; ++index)
            {
                DlmHostCollisionBand band {};
                result = DlmHostCollisionProjectionGetBandAt(collisionHandle, index, &band);
                if (result != 0)
                {
                    DlmHostCollisionProjectionDestroy(collisionHandle);
                    DlmHostSelectedDetailShellDestroy(shellHandle);
                    return result;
                }

                planner.bands.push_back(BandRow { band.startRatio, band.endRatio, band.intensity });
            }

            DlmHostCollisionProjectionDestroy(collisionHandle);
            DlmHostSelectedDetailShellDestroy(shellHandle);
            return 0;
        }
        catch (...)
        {
            DlmHostSelectedDetailShellDestroy(shellHandle);
            return -4;
        }
    }

    const ModuleRow* ModuleAt(DlmHostSelectedDetailSnapshotHandle* planner, int index)
    {
        if (planner == nullptr || index < 0 || index >= static_cast<int>(planner->modules.size()))
        {
            return nullptr;
        }

        return &planner->modules[static_cast<size_t>(index)];
    }

    const BandRow* BandAt(DlmHostSelectedDetailSnapshotHandle* planner, int index)
    {
        if (planner == nullptr || index < 0 || index >= static_cast<int>(planner->bands.size()))
        {
            return nullptr;
        }

        return &planner->bands[static_cast<size_t>(index)];
    }
}

int DlmHostSelectedDetailSnapshotCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSelectedDetailSnapshotHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSelectedDetailSnapshotDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSelectedDetailSnapshotClear(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->sources.clear();
    planner->layouts.clear();
    ResetPlan(*planner);
    return 0;
}

int DlmHostSelectedDetailSnapshotAddSource(
    void* handle,
    const char* sourceKey,
    const char* displayTitle,
    const double* spectrumValues,
    int spectrumCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || IsBlank(sourceKey) || spectrumCount < 0 || (spectrumCount > 0 && spectrumValues == nullptr))
    {
        return -2;
    }

    try
    {
        SourceInput source;
        source.key = sourceKey;
        source.displayTitle = displayTitle == nullptr ? std::string() : std::string(displayTitle);
        if (spectrumCount > 0)
        {
            source.spectrum.assign(spectrumValues, spectrumValues + spectrumCount);
        }
        planner->sources.push_back(source);
        ResetPlan(*planner);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedDetailSnapshotAddModuleLayout(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        planner->layouts.push_back(LayoutInput { kind, orderIndex, hasHeight != 0, height });
        ResetPlan(*planner);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedDetailSnapshotProject(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasStereoVisible,
    int stereoVisible,
    int hasCollisionVisible,
    int collisionVisible,
    int hasLoudnessClockVisible,
    int loudnessClockVisible,
    int hasWaveformVisible,
    int waveformVisible,
    int hasSpectrumVisible,
    int spectrumVisible,
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
        ResetPlan(*planner);
        int result = ProjectModules(
            *planner,
            hasStereoVisible,
            stereoVisible,
            hasCollisionVisible,
            collisionVisible,
            hasLoudnessClockVisible,
            loudnessClockVisible,
            hasWaveformVisible,
            waveformVisible,
            hasSpectrumVisible,
            spectrumVisible);
        if (result != 0)
        {
            return result;
        }

        return ProjectShellAndBands(
            *planner,
            selectedSourceKey,
            comparisonSourceKey,
            hasLevelFloorDb,
            levelFloorDb,
            hasProminenceThresholdDb,
            prominenceThresholdDb,
            hasRelativeThreshold,
            relativeThreshold,
            hasFrequencyScale,
            frequencyScale);
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedDetailSnapshotGetPlan(void* handle, DlmHostSelectedDetailSnapshotPlan* plan)
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

int DlmHostSelectedDetailSnapshotGetModuleCount(void* handle, int* count)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(planner->modules.size());
    return 0;
}

int DlmHostSelectedDetailSnapshotGetModuleAt(void* handle, int index, DlmHostDetailModuleProjectionRow* row)
{
    const auto* module = ModuleAt(AsPlanner(handle), index);
    if (module == nullptr || row == nullptr)
    {
        return -2;
    }

    *row = {};
    row->kind = module->kind;
    row->orderIndex = module->orderIndex;
    row->isVisible = module->isVisible ? 1 : 0;
    row->height = module->height;
    row->titleUtf8ByteCount = static_cast<int>(module->title.size());
    return 0;
}

int DlmHostSelectedDetailSnapshotCopyModuleStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    const auto* module = ModuleAt(AsPlanner(handle), index);
    if (module == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case 0:
        return CopyStringToBuffer(module->title, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}

int DlmHostSelectedDetailSnapshotGetBandCount(void* handle, int* count)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(planner->bands.size());
    return 0;
}

int DlmHostSelectedDetailSnapshotGetBandAt(void* handle, int index, DlmHostCollisionBand* band)
{
    const auto* source = BandAt(AsPlanner(handle), index);
    if (source == nullptr || band == nullptr)
    {
        return -2;
    }

    band->startRatio = source->startRatio;
    band->endRatio = source->endRatio;
    band->intensity = source->intensity;
    return 0;
}

int DlmHostSelectedDetailSnapshotCopyString(
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
