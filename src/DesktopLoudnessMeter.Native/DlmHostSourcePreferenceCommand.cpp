#include "DlmHostSourcePreferenceCommand.h"

#include <algorithm>
#include <cctype>
#include <cstdint>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StatusSuccess = 0;
    constexpr int StatusSourceNotFound = 1;

    struct DlmHostSourcePreferenceCommandHandle
    {
        std::vector<std::string> sources;
        std::string alias;
        int status = StatusSuccess;
        bool hasAlias = false;
        bool isHidden = false;
        bool shouldForget = false;
        bool shouldClearSelected = false;
    };

    DlmHostSourcePreferenceCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSourcePreferenceCommandHandle*>(handle);
    }

    bool HasSource(const std::vector<std::string>& sources, const char* sourceKey)
    {
        if (sourceKey == nullptr || sourceKey[0] == '\0')
        {
            return false;
        }

        return std::find(sources.begin(), sources.end(), sourceKey) != sources.end();
    }

    void ResetPlan(DlmHostSourcePreferenceCommandHandle& planner)
    {
        planner.alias.clear();
        planner.status = StatusSuccess;
        planner.hasAlias = false;
        planner.isHidden = false;
        planner.shouldForget = false;
        planner.shouldClearSelected = false;
    }

    void SetSourceNotFound(DlmHostSourcePreferenceCommandHandle& planner)
    {
        ResetPlan(planner);
        planner.status = StatusSourceNotFound;
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

    bool Equals(const char* left, const char* right)
    {
        if (left == nullptr || right == nullptr)
        {
            return false;
        }

        return std::strcmp(left, right) == 0;
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

int DlmHostSourcePreferenceCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSourcePreferenceCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSourcePreferenceCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSourcePreferenceCommandClearSources(void* handle)
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

int DlmHostSourcePreferenceCommandAddSource(void* handle, const char* sourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    try
    {
        planner->sources.push_back(sourceKey);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourcePreferenceCommandPlanSetAlias(void* handle, const char* sourceKey, const char* alias)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    try
    {
        if (!HasSource(planner->sources, sourceKey))
        {
            SetSourceNotFound(*planner);
            return 0;
        }

        ResetPlan(*planner);
        planner->alias = Trim(alias);
        planner->hasAlias = !planner->alias.empty();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourcePreferenceCommandPlanSetHidden(void* handle, const char* sourceKey, int isHidden)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    if (!HasSource(planner->sources, sourceKey))
    {
        SetSourceNotFound(*planner);
        return 0;
    }

    ResetPlan(*planner);
    planner->isHidden = isHidden != 0;
    return 0;
}

int DlmHostSourcePreferenceCommandPlanForget(void* handle, const char* sourceKey, const char* selectedSourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    ResetPlan(*planner);
    planner->shouldForget = true;
    planner->shouldClearSelected = Equals(sourceKey, selectedSourceKey);
    return 0;
}

int DlmHostSourcePreferenceCommandGetPlan(void* handle, DlmHostSourcePreferenceCommandPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->status = planner->status;
    plan->hasAlias = planner->hasAlias ? 1 : 0;
    plan->aliasUtf8ByteCount = planner->hasAlias
        ? static_cast<int>(planner->alias.size())
        : 0;
    plan->isHidden = planner->isHidden ? 1 : 0;
    plan->shouldForget = planner->shouldForget ? 1 : 0;
    plan->shouldClearSelected = planner->shouldClearSelected ? 1 : 0;
    return 0;
}

int DlmHostSourcePreferenceCommandCopyAlias(void* handle, char* buffer, int capacity, int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(planner->alias, buffer, capacity, actualByteCount);
}
