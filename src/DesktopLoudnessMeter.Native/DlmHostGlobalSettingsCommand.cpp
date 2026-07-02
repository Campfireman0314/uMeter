#include "DlmHostGlobalSettingsCommand.h"

#include <algorithm>
#include <cstdint>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    struct DlmHostGlobalSettingsCommandHandle
    {
        std::string themeId;
        bool hasThemeId = false;
        int maxRecentEventEntries = 6;
        std::string eventOutputFolder;
        bool hasEventOutputFolder = false;
        bool showHiddenSources = false;
    };

    DlmHostGlobalSettingsCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostGlobalSettingsCommandHandle*>(handle);
    }

    struct Utf8CodePointSpan
    {
        size_t begin = 0;
        size_t end = 0;
        std::uint32_t codePoint = 0;
    };

    bool TryDecodeUtf8(
        const std::string& text,
        size_t index,
        Utf8CodePointSpan& span)
    {
        const auto first = static_cast<unsigned char>(text[index]);
        span.begin = index;
        span.end = index + 1;
        span.codePoint = first;

        if (first < 0x80)
        {
            return true;
        }

        size_t length = 0;
        std::uint32_t codePoint = 0;
        if ((first & 0xE0) == 0xC0)
        {
            length = 2;
            codePoint = first & 0x1F;
        }
        else if ((first & 0xF0) == 0xE0)
        {
            length = 3;
            codePoint = first & 0x0F;
        }
        else if ((first & 0xF8) == 0xF0)
        {
            length = 4;
            codePoint = first & 0x07;
        }
        else
        {
            return false;
        }

        if (index + length > text.size())
        {
            return false;
        }

        for (size_t offset = 1; offset < length; offset++)
        {
            const auto next = static_cast<unsigned char>(text[index + offset]);
            if ((next & 0xC0) != 0x80)
            {
                return false;
            }

            codePoint = (codePoint << 6) | (next & 0x3F);
        }

        span.end = index + length;
        span.codePoint = codePoint;
        return true;
    }

    bool IsWhitespaceCodePoint(std::uint32_t codePoint)
    {
        return (codePoint >= 0x0009 && codePoint <= 0x000D)
            || codePoint == 0x0020
            || codePoint == 0x0085
            || codePoint == 0x00A0
            || codePoint == 0x1680
            || (codePoint >= 0x2000 && codePoint <= 0x200A)
            || codePoint == 0x2028
            || codePoint == 0x2029
            || codePoint == 0x202F
            || codePoint == 0x205F
            || codePoint == 0x3000;
    }

    std::string Trim(const char* value)
    {
        if (value == nullptr)
        {
            return {};
        }

        std::string text(value);
        std::vector<Utf8CodePointSpan> spans;
        spans.reserve(text.size());
        for (size_t index = 0; index < text.size();)
        {
            Utf8CodePointSpan span;
            if (!TryDecodeUtf8(text, index, span))
            {
                span.begin = index;
                span.end = index + 1;
                span.codePoint = static_cast<unsigned char>(text[index]);
            }

            spans.push_back(span);
            index = span.end;
        }

        auto first = spans.begin();
        while (first != spans.end() && IsWhitespaceCodePoint(first->codePoint))
        {
            ++first;
        }

        auto last = spans.end();
        while (last != first && IsWhitespaceCodePoint((last - 1)->codePoint))
        {
            --last;
        }

        if (first == last)
        {
            return {};
        }

        return text.substr(first->begin, (last - 1)->end - first->begin);
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

int DlmHostGlobalSettingsCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostGlobalSettingsCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostGlobalSettingsCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostGlobalSettingsCommandPlanSetTheme(void* handle, const char* themeId)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        planner->themeId = Trim(themeId);
        planner->hasThemeId = !planner->themeId.empty();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostGlobalSettingsCommandPlanSetRecentEventLimit(void* handle, int maxRecentEventEntries)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->maxRecentEventEntries = std::clamp(maxRecentEventEntries, 1, 200);
    return 0;
}

int DlmHostGlobalSettingsCommandPlanSetEventOutputFolder(void* handle, const char* folder)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        planner->eventOutputFolder = Trim(folder);
        planner->hasEventOutputFolder = !planner->eventOutputFolder.empty();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostGlobalSettingsCommandPlanSetShowHiddenSources(void* handle, int showHiddenSources)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->showHiddenSources = showHiddenSources != 0;
    return 0;
}

int DlmHostGlobalSettingsCommandGetPlan(void* handle, DlmHostGlobalSettingsCommandPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->hasThemeId = planner->hasThemeId ? 1 : 0;
    plan->themeIdUtf8ByteCount = planner->hasThemeId
        ? static_cast<int>(planner->themeId.size())
        : 0;
    plan->maxRecentEventEntries = planner->maxRecentEventEntries;
    plan->hasEventOutputFolder = planner->hasEventOutputFolder ? 1 : 0;
    plan->eventOutputFolderUtf8ByteCount = planner->hasEventOutputFolder
        ? static_cast<int>(planner->eventOutputFolder.size())
        : 0;
    plan->showHiddenSources = planner->showHiddenSources ? 1 : 0;
    return 0;
}

int DlmHostGlobalSettingsCommandCopyThemeId(void* handle, char* buffer, int capacity, int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(planner->themeId, buffer, capacity, actualByteCount);
}

int DlmHostGlobalSettingsCommandCopyEventOutputFolder(void* handle, char* buffer, int capacity, int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(planner->eventOutputFolder, buffer, capacity, actualByteCount);
}
