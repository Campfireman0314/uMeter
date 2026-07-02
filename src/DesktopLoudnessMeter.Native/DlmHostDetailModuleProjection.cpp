#include "DlmHostDetailModuleProjection.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <limits>
#include <memory>
#include <string>
#include <vector>

namespace
{
    constexpr int DetailModuleStereo = 0;
    constexpr int DetailModuleCollision = 1;
    constexpr int DetailModuleLoudnessClock = 2;
    constexpr int DetailModuleWaveform = 3;
    constexpr int DetailModuleSpectrum = 4;
    constexpr int StringFieldTitle = 0;
    constexpr double MinimumHeight = 160.0;
    constexpr double MaximumHeight = 1200.0;
    constexpr double FixedCollisionHeight = 112.0;

    constexpr int DefaultOrder[] =
    {
        DetailModuleStereo,
        DetailModuleCollision,
        DetailModuleLoudnessClock,
        DetailModuleWaveform,
        DetailModuleSpectrum
    };

    struct LayoutInput
    {
        int kind = 0;
        int orderIndex = 0;
        bool hasHeight = false;
        double height = 0.0;
    };

    struct ProjectedRow
    {
        int kind = 0;
        int orderIndex = 0;
        bool isVisible = false;
        double height = 0.0;
        std::string title;
    };

    struct VisibilityInput
    {
        bool hasStereo = false;
        bool stereo = false;
        bool hasCollision = false;
        bool collision = false;
        bool hasLoudnessClock = false;
        bool loudnessClock = false;
        bool hasWaveform = false;
        bool waveform = false;
        bool hasSpectrum = false;
        bool spectrum = false;
    };

    struct DlmHostDetailModuleProjectionHandle
    {
        std::vector<LayoutInput> inputs;
        std::vector<ProjectedRow> rows;
    };

    DlmHostDetailModuleProjectionHandle* AsProjection(void* handle)
    {
        return static_cast<DlmHostDetailModuleProjectionHandle*>(handle);
    }

    bool IsKnownKind(int kind)
    {
        return kind == DetailModuleStereo
            || kind == DetailModuleCollision
            || kind == DetailModuleLoudnessClock
            || kind == DetailModuleWaveform
            || kind == DetailModuleSpectrum;
    }

    bool ContainsKind(const std::vector<ProjectedRow>& rows, int kind)
    {
        return std::any_of(rows.begin(), rows.end(), [kind](const ProjectedRow& row)
        {
            return row.kind == kind;
        });
    }

    double DefaultHeight(int kind)
    {
        switch (kind)
        {
        case DetailModuleStereo:
            return 320.0;
        case DetailModuleCollision:
            return FixedCollisionHeight;
        case DetailModuleLoudnessClock:
            return 260.0;
        case DetailModuleWaveform:
        case DetailModuleSpectrum:
        default:
            return 220.0;
        }
    }

    double MinimumHeightFor(int kind)
    {
        switch (kind)
        {
        case DetailModuleStereo:
            return 320.0;
        case DetailModuleCollision:
            return FixedCollisionHeight;
        default:
            return MinimumHeight;
        }
    }

    double RoundAwayFromZero(double value)
    {
        return value >= 0.0
            ? std::floor(value + 0.5)
            : std::ceil(value - 0.5);
    }

    double NormalizeHeight(int kind, bool hasHeight, double height)
    {
        if (kind == DetailModuleCollision)
        {
            return FixedCollisionHeight;
        }

        if (!hasHeight || !std::isfinite(height))
        {
            return DefaultHeight(kind);
        }

        const double rounded = RoundAwayFromZero(height);
        return (std::min)((std::max)(rounded, MinimumHeightFor(kind)), MaximumHeight);
    }

    std::string ModuleTitle(int kind)
    {
        switch (kind)
        {
        case DetailModuleStereo:
            return "Stereo image";
        case DetailModuleCollision:
            return "Frequency collision";
        case DetailModuleLoudnessClock:
            return "Clock";
        case DetailModuleWaveform:
            return "Waveform";
        case DetailModuleSpectrum:
            return "Spectrum";
        default:
            return {};
        }
    }

    bool IsVisible(int kind, const VisibilityInput& visibility)
    {
        switch (kind)
        {
        case DetailModuleStereo:
            return visibility.hasStereo ? visibility.stereo : true;
        case DetailModuleCollision:
            return visibility.hasCollision ? visibility.collision : false;
        case DetailModuleLoudnessClock:
            return visibility.hasLoudnessClock ? visibility.loudnessClock : false;
        case DetailModuleWaveform:
            return visibility.hasWaveform ? visibility.waveform : false;
        case DetailModuleSpectrum:
            return visibility.hasSpectrum ? visibility.spectrum : false;
        default:
            return false;
        }
    }

    ProjectedRow ProjectRow(const LayoutInput& input, int orderIndex, const VisibilityInput& visibility)
    {
        ProjectedRow row;
        row.kind = input.kind;
        row.orderIndex = orderIndex;
        row.isVisible = IsVisible(input.kind, visibility);
        row.height = NormalizeHeight(input.kind, input.hasHeight, input.height);
        row.title = ModuleTitle(input.kind);
        return row;
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

    const ProjectedRow* RowAt(DlmHostDetailModuleProjectionHandle* projection, int index)
    {
        if (projection == nullptr || index < 0 || index >= static_cast<int>(projection->rows.size()))
        {
            return nullptr;
        }

        return &projection->rows[static_cast<size_t>(index)];
    }
}

int DlmHostDetailModuleProjectionCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostDetailModuleProjectionHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostDetailModuleProjectionDestroy(void* handle)
{
    delete AsProjection(handle);
}

int DlmHostDetailModuleProjectionClear(void* handle)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    projection->inputs.clear();
    projection->rows.clear();
    return 0;
}

int DlmHostDetailModuleProjectionAddLayout(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        LayoutInput input;
        input.kind = kind;
        input.orderIndex = orderIndex;
        input.hasHeight = hasHeight != 0;
        input.height = height;
        projection->inputs.push_back(input);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostDetailModuleProjectionProject(
    void* handle,
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
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        VisibilityInput visibility;
        visibility.hasStereo = hasStereoVisible != 0;
        visibility.stereo = stereoVisible != 0;
        visibility.hasCollision = hasCollisionVisible != 0;
        visibility.collision = collisionVisible != 0;
        visibility.hasLoudnessClock = hasLoudnessClockVisible != 0;
        visibility.loudnessClock = loudnessClockVisible != 0;
        visibility.hasWaveform = hasWaveformVisible != 0;
        visibility.waveform = waveformVisible != 0;
        visibility.hasSpectrum = hasSpectrumVisible != 0;
        visibility.spectrum = spectrumVisible != 0;

        projection->rows.clear();
        std::vector<LayoutInput> sortedInputs = projection->inputs;
        std::stable_sort(sortedInputs.begin(), sortedInputs.end(), [](const LayoutInput& left, const LayoutInput& right)
        {
            const int leftIndex = left.orderIndex < 0 ? (std::numeric_limits<int>::max)() : left.orderIndex;
            const int rightIndex = right.orderIndex < 0 ? (std::numeric_limits<int>::max)() : right.orderIndex;
            return leftIndex < rightIndex;
        });

        for (const auto& input : sortedInputs)
        {
            if (!IsKnownKind(input.kind) || ContainsKind(projection->rows, input.kind))
            {
                continue;
            }

            projection->rows.push_back(ProjectRow(input, static_cast<int>(projection->rows.size()), visibility));
        }

        for (const int kind : DefaultOrder)
        {
            if (ContainsKind(projection->rows, kind))
            {
                continue;
            }

            LayoutInput input;
            input.kind = kind;
            input.orderIndex = static_cast<int>(projection->rows.size());
            input.hasHeight = false;
            projection->rows.push_back(ProjectRow(input, static_cast<int>(projection->rows.size()), visibility));
        }

        for (int index = 0; index < static_cast<int>(projection->rows.size()); ++index)
        {
            projection->rows[static_cast<size_t>(index)].orderIndex = index;
        }

        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostDetailModuleProjectionGetCount(void* handle, int* count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(projection->rows.size());
    return 0;
}

int DlmHostDetailModuleProjectionGetAt(void* handle, int index, DlmHostDetailModuleProjectionRow* row)
{
    auto* projected = RowAt(AsProjection(handle), index);
    if (projected == nullptr || row == nullptr)
    {
        return -2;
    }

    *row = {};
    row->kind = projected->kind;
    row->orderIndex = projected->orderIndex;
    row->isVisible = projected->isVisible ? 1 : 0;
    row->height = projected->height;
    row->titleUtf8ByteCount = static_cast<int>(projected->title.size());
    return 0;
}

int DlmHostDetailModuleProjectionCopyStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* projected = RowAt(AsProjection(handle), index);
    if (projected == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case StringFieldTitle:
        return CopyStringToBuffer(projected->title, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}
