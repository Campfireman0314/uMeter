#include "DlmHostRecentEvents.h"

#include <cstring>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace
{
    constexpr int RecentEventKindSource = 0;
    constexpr int ReadoutSeverityNormal = 1;
    constexpr int StringFieldTimeText = 0;
    constexpr int StringFieldTitle = 1;
    constexpr int StringFieldDetail = 2;

    struct RecentEvent
    {
        std::string timeText;
        int kind = RecentEventKindSource;
        std::string title;
        std::string detail;
        int severity = ReadoutSeverityNormal;
    };

    struct DlmHostRecentEventsHandle
    {
        std::unordered_set<std::string> previousLiveKeys;
        std::unordered_set<std::string> currentLiveKeys;
        std::vector<std::string> currentLiveOrder;
        std::unordered_map<std::string, std::string> visibleTitles;
        std::vector<RecentEvent> recentEvents;
    };

    DlmHostRecentEventsHandle* AsEvents(void* handle)
    {
        return static_cast<DlmHostRecentEventsHandle*>(handle);
    }

    int ClampLimit(int value)
    {
        return (std::max)(1, (std::min)(value, 200));
    }

    void TrimEvents(std::vector<RecentEvent>& events, int maxRecentEventEntries)
    {
        const int limit = ClampLimit(maxRecentEventEntries);
        if (static_cast<int>(events.size()) > limit)
        {
            events.resize(static_cast<size_t>(limit));
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

    const RecentEvent* EventAt(DlmHostRecentEventsHandle* events, int index)
    {
        if (events == nullptr || index < 0 || index >= static_cast<int>(events->recentEvents.size()))
        {
            return nullptr;
        }

        return &events->recentEvents[static_cast<size_t>(index)];
    }
}

int DlmHostRecentEventsCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostRecentEventsHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostRecentEventsDestroy(void* handle)
{
    delete AsEvents(handle);
}

int DlmHostRecentEventsClearCurrentSources(void* handle)
{
    auto* events = AsEvents(handle);
    if (events == nullptr)
    {
        return -2;
    }

    events->currentLiveKeys.clear();
    events->currentLiveOrder.clear();
    events->visibleTitles.clear();
    return 0;
}

int DlmHostRecentEventsAddLiveSource(void* handle, const char* sourceKey)
{
    auto* events = AsEvents(handle);
    if (events == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    try
    {
        auto insert = events->currentLiveKeys.insert(sourceKey);
        if (insert.second)
        {
            events->currentLiveOrder.push_back(sourceKey);
        }

        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostRecentEventsAddVisibleSource(void* handle, const char* sourceKey, const char* displayTitle)
{
    auto* events = AsEvents(handle);
    if (events == nullptr || sourceKey == nullptr || sourceKey[0] == '\0' || displayTitle == nullptr)
    {
        return -2;
    }

    try
    {
        events->visibleTitles[sourceKey] = displayTitle;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostRecentEventsReconcile(void* handle, const char* timeText, int maxRecentEventEntries)
{
    auto* events = AsEvents(handle);
    if (events == nullptr || timeText == nullptr)
    {
        return -2;
    }

    try
    {
        std::vector<RecentEvent> newEvents;
        for (const auto& sourceKey : events->currentLiveOrder)
        {
            if (events->previousLiveKeys.find(sourceKey) != events->previousLiveKeys.end())
            {
                continue;
            }

            const auto visible = events->visibleTitles.find(sourceKey);
            if (visible == events->visibleTitles.end())
            {
                continue;
            }

            RecentEvent recent;
            recent.timeText = timeText;
            recent.kind = RecentEventKindSource;
            recent.title = "Source appeared";
            recent.detail = visible->second;
            recent.severity = ReadoutSeverityNormal;
            newEvents.push_back(std::move(recent));
        }

        for (const auto& recent : newEvents)
        {
            events->recentEvents.insert(events->recentEvents.begin(), recent);
        }

        TrimEvents(events->recentEvents, maxRecentEventEntries);
        events->previousLiveKeys = events->currentLiveKeys;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostRecentEventsGetCount(void* handle, int* count)
{
    auto* events = AsEvents(handle);
    if (events == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(events->recentEvents.size());
    return 0;
}

int DlmHostRecentEventsGetAt(void* handle, int index, DlmHostRecentEventRow* row)
{
    const auto* recent = EventAt(AsEvents(handle), index);
    if (recent == nullptr || row == nullptr)
    {
        return -2;
    }

    *row = {};
    row->kind = recent->kind;
    row->severity = recent->severity;
    row->timeTextUtf8ByteCount = static_cast<int>(recent->timeText.size());
    row->titleUtf8ByteCount = static_cast<int>(recent->title.size());
    row->detailUtf8ByteCount = static_cast<int>(recent->detail.size());
    return 0;
}

int DlmHostRecentEventsCopyStringAt(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    const auto* recent = EventAt(AsEvents(handle), index);
    if (recent == nullptr)
    {
        return -2;
    }

    switch (field)
    {
    case StringFieldTimeText:
        return CopyStringToBuffer(recent->timeText, buffer, capacity, actualByteCount);
    case StringFieldTitle:
        return CopyStringToBuffer(recent->title, buffer, capacity, actualByteCount);
    case StringFieldDetail:
        return CopyStringToBuffer(recent->detail, buffer, capacity, actualByteCount);
    default:
        return -3;
    }
}
