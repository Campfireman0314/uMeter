#include "DlmHostSourceProjection.h"

#include <algorithm>
#include <cctype>
#include <cstring>
#include <memory>
#include <string>
#include <vector>

namespace
{
    constexpr int SourceKindUnknown = 0;
    constexpr int SourceKindWasapiSession = 1;
    constexpr int SourceKindLocalLoopback = 2;
    constexpr int SourceKindPlugin = 3;
    constexpr int DefaultTruePeakOversampling = 4;
    constexpr int DefaultWaveformDisplayMode = 0;
    constexpr int DefaultSpectrumDisplayMode = 0;

    constexpr int StringFieldSourceKey = 0;
    constexpr int StringFieldSourceKindLabel = 1;
    constexpr int StringFieldDisplayName = 2;
    constexpr int StringFieldAlias = 3;
    constexpr int StringFieldEffectiveDisplayName = 4;
    constexpr int StringFieldDisplayTitle = 5;
    constexpr int StringFieldSourceIdentityText = 6;

    struct SourceInput
    {
        int originalSourceIndex = 0;
        std::string sourceKey;
        int sourceKind = SourceKindUnknown;
        bool hasProcessId = false;
        int processId = 0;
        std::string displayName;
        std::string alias;
        bool isHidden = false;
        bool hasOrderIndex = false;
        int orderIndex = 0;
        int truePeakOversampling = DefaultTruePeakOversampling;
        int waveformDisplayMode = DefaultWaveformDisplayMode;
        int spectrumDisplayMode = DefaultSpectrumDisplayMode;
    };

    struct ProjectedSource
    {
        SourceInput input;
        std::string sourceKindLabel;
        std::string effectiveDisplayName;
        std::string displayTitle;
        std::string sourceIdentityText;
    };

    struct DlmHostSourceProjectionHandle
    {
        std::vector<SourceInput> inputs;
        std::vector<ProjectedSource> projected;
        bool selectedSourceVisible = false;
    };

    DlmHostSourceProjectionHandle* AsProjection(void* handle)
    {
        return static_cast<DlmHostSourceProjectionHandle*>(handle);
    }

    std::string Trim(const char* value)
    {
        if (value == nullptr)
        {
            return {};
        }

        std::string text(value);
        const auto first = std::find_if_not(text.begin(), text.end(), [](unsigned char ch)
        {
            return std::isspace(ch) != 0;
        });
        const auto last = std::find_if_not(text.rbegin(), text.rend(), [](unsigned char ch)
        {
            return std::isspace(ch) != 0;
        }).base();
        return first >= last ? std::string() : std::string(first, last);
    }

    std::string ToLowerAscii(const std::string& value)
    {
        std::string lowered(value);
        std::transform(lowered.begin(), lowered.end(), lowered.begin(), [](unsigned char ch)
        {
            return static_cast<char>(std::tolower(ch));
        });
        return lowered;
    }

    std::string SourceKindLabel(int kind)
    {
        switch (kind)
        {
        case SourceKindWasapiSession:
            return "WASAPI";
        case SourceKindLocalLoopback:
            return "Loopback";
        case SourceKindPlugin:
            return "Plugin";
        default:
            return "Unknown";
        }
    }

    int SourceKindSortRank(int kind)
    {
        switch (kind)
        {
        case SourceKindPlugin:
            return 0;
        case SourceKindLocalLoopback:
            return 1;
        case SourceKindWasapiSession:
            return 2;
        default:
            return 3;
        }
    }

    std::string CompactSourceIdentifier(const std::string& sourceKey)
    {
        auto value = sourceKey;
        const auto separator = sourceKey.find(':');
        if (separator != std::string::npos && separator < sourceKey.size() - 1)
        {
            value = sourceKey.substr(separator + 1);
        }

        return value.size() <= 24 ? value : value.substr(0, 21) + "...";
    }

    std::string FormatSourceQualifier(const SourceInput& source, const std::string& sourceKindLabel)
    {
        if (source.sourceKind == SourceKindWasapiSession && source.hasProcessId)
        {
            return sourceKindLabel + " PID " + std::to_string(source.processId);
        }

        const auto stableId = CompactSourceIdentifier(source.sourceKey);
        return stableId.empty() ? sourceKindLabel : sourceKindLabel + " " + stableId;
    }

    std::string FormatSourceIdentityText(const SourceInput& source, const std::string& sourceKindLabel)
    {
        if (source.sourceKind == SourceKindWasapiSession && source.hasProcessId)
        {
            return sourceKindLabel + " source - PID " + std::to_string(source.processId);
        }

        const auto stableId = CompactSourceIdentifier(source.sourceKey);
        return stableId.empty()
            ? sourceKindLabel + " source"
            : sourceKindLabel + " source - " + stableId;
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

    bool SameTextIgnoreCase(const std::string& left, const std::string& right)
    {
        return ToLowerAscii(left) == ToLowerAscii(right);
    }

    ProjectedSource ProjectOne(const SourceInput& input)
    {
        ProjectedSource projected;
        projected.input = input;
        projected.sourceKindLabel = SourceKindLabel(input.sourceKind);
        projected.effectiveDisplayName = input.alias.empty() ? input.displayName : input.alias;
        projected.displayTitle = projected.effectiveDisplayName;
        projected.sourceIdentityText = FormatSourceIdentityText(input, projected.sourceKindLabel);
        return projected;
    }

    void MarkDuplicateTitles(std::vector<ProjectedSource>& projected)
    {
        for (auto& source : projected)
        {
            const int matchCount = static_cast<int>(std::count_if(projected.begin(), projected.end(), [&](const ProjectedSource& other)
            {
                return SameTextIgnoreCase(source.effectiveDisplayName, other.effectiveDisplayName);
            }));

            if (matchCount > 1)
            {
                source.displayTitle = source.effectiveDisplayName + " - "
                    + FormatSourceQualifier(source.input, source.sourceKindLabel);
            }
        }
    }

    const ProjectedSource* ProjectedAt(DlmHostSourceProjectionHandle* projection, int index)
    {
        if (projection == nullptr || index < 0 || index >= static_cast<int>(projection->projected.size()))
        {
            return nullptr;
        }

        return &projection->projected[static_cast<size_t>(index)];
    }
}

int DlmHostSourceProjectionCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSourceProjectionHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSourceProjectionDestroy(void* handle)
{
    delete AsProjection(handle);
}

int DlmHostSourceProjectionClear(void* handle)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    projection->inputs.clear();
    projection->projected.clear();
    projection->selectedSourceVisible = false;
    return 0;
}

int DlmHostSourceProjectionAddSource(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId,
    const char* displayName,
    const char* alias,
    int isHidden,
    int hasOrderIndex,
    int orderIndex,
    int truePeakOversampling,
    int waveformDisplayMode,
    int spectrumDisplayMode)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || sourceKey == nullptr || sourceKey[0] == '\0' || displayName == nullptr)
    {
        return -2;
    }

    try
    {
        SourceInput input;
        input.originalSourceIndex = static_cast<int>(projection->inputs.size());
        input.sourceKey = sourceKey;
        input.sourceKind = sourceKind;
        input.hasProcessId = hasProcessId != 0;
        input.processId = processId;
        input.displayName = displayName;
        input.alias = Trim(alias);
        input.isHidden = isHidden != 0;
        input.hasOrderIndex = hasOrderIndex != 0 && orderIndex >= 0;
        input.orderIndex = input.hasOrderIndex ? orderIndex : 0;
        input.truePeakOversampling = truePeakOversampling == 2 || truePeakOversampling == 4 || truePeakOversampling == 8
            ? truePeakOversampling
            : DefaultTruePeakOversampling;
        input.waveformDisplayMode = waveformDisplayMode;
        input.spectrumDisplayMode = spectrumDisplayMode;
        projection->inputs.push_back(std::move(input));
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourceProjectionProject(void* handle, int showHiddenSources, const char* selectedSourceKey)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        projection->projected.clear();
        projection->selectedSourceVisible = false;
        for (const auto& source : projection->inputs)
        {
            if (!showHiddenSources && source.isHidden)
            {
                continue;
            }

            auto projected = ProjectOne(source);
            if (selectedSourceKey != nullptr && source.sourceKey == selectedSourceKey)
            {
                projection->selectedSourceVisible = true;
            }

            projection->projected.push_back(std::move(projected));
        }

        std::sort(projection->projected.begin(), projection->projected.end(), [](const ProjectedSource& left, const ProjectedSource& right)
        {
            if (left.input.hasOrderIndex != right.input.hasOrderIndex)
            {
                return left.input.hasOrderIndex;
            }

            if (left.input.hasOrderIndex && left.input.orderIndex != right.input.orderIndex)
            {
                return left.input.orderIndex < right.input.orderIndex;
            }

            const int leftRank = SourceKindSortRank(left.input.sourceKind);
            const int rightRank = SourceKindSortRank(right.input.sourceKind);
            if (leftRank != rightRank)
            {
                return leftRank < rightRank;
            }

            const auto leftName = ToLowerAscii(left.effectiveDisplayName);
            const auto rightName = ToLowerAscii(right.effectiveDisplayName);
            if (leftName != rightName)
            {
                return leftName < rightName;
            }

            return left.input.sourceKey < right.input.sourceKey;
        });

        MarkDuplicateTitles(projection->projected);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourceProjectionGetProjectedCount(void* handle, int* count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(projection->projected.size());
    return 0;
}

int DlmHostSourceProjectionIsSelectedSourceVisible(void* handle, int* isVisible)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || isVisible == nullptr)
    {
        return -2;
    }

    *isVisible = projection->selectedSourceVisible ? 1 : 0;
    return 0;
}

int DlmHostSourceProjectionGetProjectedAt(void* handle, int index, DlmHostSourceProjectionRow* row)
{
    auto* source = ProjectedAt(AsProjection(handle), index);
    if (source == nullptr || row == nullptr)
    {
        return -2;
    }

    *row = {};
    row->originalSourceIndex = source->input.originalSourceIndex;
    row->sourceKind = source->input.sourceKind;
    row->processId = source->input.processId;
    row->hasProcessId = source->input.hasProcessId ? 1 : 0;
    row->isHidden = source->input.isHidden ? 1 : 0;
    row->orderIndex = source->input.orderIndex;
    row->hasOrderIndex = source->input.hasOrderIndex ? 1 : 0;
    row->truePeakOversampling = source->input.truePeakOversampling;
    row->waveformDisplayMode = source->input.waveformDisplayMode;
    row->spectrumDisplayMode = source->input.spectrumDisplayMode;
    row->sourceKeyUtf8ByteCount = static_cast<int>(source->input.sourceKey.size());
    row->sourceKindLabelUtf8ByteCount = static_cast<int>(source->sourceKindLabel.size());
    row->displayNameUtf8ByteCount = static_cast<int>(source->input.displayName.size());
    row->aliasUtf8ByteCount = static_cast<int>(source->input.alias.size());
    row->effectiveDisplayNameUtf8ByteCount = static_cast<int>(source->effectiveDisplayName.size());
    row->displayTitleUtf8ByteCount = static_cast<int>(source->displayTitle.size());
    row->sourceIdentityTextUtf8ByteCount = static_cast<int>(source->sourceIdentityText.size());
    return 0;
}

int DlmHostSourceProjectionCopyProjectedStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* source = ProjectedAt(AsProjection(handle), index);
    if (source == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case StringFieldSourceKey:
        return CopyStringToBuffer(source->input.sourceKey, buffer, capacity, actualByteCount);
    case StringFieldSourceKindLabel:
        return CopyStringToBuffer(source->sourceKindLabel, buffer, capacity, actualByteCount);
    case StringFieldDisplayName:
        return CopyStringToBuffer(source->input.displayName, buffer, capacity, actualByteCount);
    case StringFieldAlias:
        return CopyStringToBuffer(source->input.alias, buffer, capacity, actualByteCount);
    case StringFieldEffectiveDisplayName:
        return CopyStringToBuffer(source->effectiveDisplayName, buffer, capacity, actualByteCount);
    case StringFieldDisplayTitle:
        return CopyStringToBuffer(source->displayTitle, buffer, capacity, actualByteCount);
    case StringFieldSourceIdentityText:
        return CopyStringToBuffer(source->sourceIdentityText, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}
