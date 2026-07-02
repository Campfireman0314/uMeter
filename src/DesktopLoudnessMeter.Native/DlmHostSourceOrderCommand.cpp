#include "DlmHostSourceOrderCommand.h"

#include <algorithm>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StatusSuccess = 0;
    constexpr int StatusSourceNotFound = 1;

    struct SourceOrderRow
    {
        std::string sourceKey;
        int orderIndex = 0;
    };

    struct DlmHostSourceOrderCommandHandle
    {
        std::vector<std::string> sources;
        std::vector<SourceOrderRow> rows;
        int status = StatusSuccess;
    };

    DlmHostSourceOrderCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSourceOrderCommandHandle*>(handle);
    }

    int ClampTarget(int targetIndex, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (std::max)(0, (std::min)(targetIndex, count - 1));
    }

    int FindSourceIndex(const std::vector<std::string>& sources, const char* sourceKey)
    {
        if (sourceKey == nullptr)
        {
            return -1;
        }

        const auto found = std::find(sources.begin(), sources.end(), sourceKey);
        return found == sources.end()
            ? -1
            : static_cast<int>(std::distance(sources.begin(), found));
    }

    void SetRows(DlmHostSourceOrderCommandHandle& planner, const std::vector<std::string>& ordered)
    {
        planner.rows.clear();
        planner.rows.reserve(ordered.size());
        for (int index = 0; index < static_cast<int>(ordered.size()); index++)
        {
            planner.rows.push_back(SourceOrderRow { ordered[static_cast<size_t>(index)], index });
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

    const SourceOrderRow* RowAt(DlmHostSourceOrderCommandHandle* planner, int index)
    {
        if (planner == nullptr || index < 0 || index >= static_cast<int>(planner->rows.size()))
        {
            return nullptr;
        }

        return &planner->rows[static_cast<size_t>(index)];
    }

    int PlanMoveToIndex(DlmHostSourceOrderCommandHandle& planner, const char* sourceKey, int targetIndex)
    {
        const int currentIndex = FindSourceIndex(planner.sources, sourceKey);
        if (currentIndex < 0)
        {
            planner.status = StatusSourceNotFound;
            planner.rows.clear();
            return 0;
        }

        auto ordered = planner.sources;
        const auto item = ordered[static_cast<size_t>(currentIndex)];
        ordered.erase(ordered.begin() + currentIndex);
        const int nextIndex = ClampTarget(targetIndex, static_cast<int>(planner.sources.size()));
        ordered.insert(ordered.begin() + nextIndex, item);
        planner.status = StatusSuccess;
        SetRows(planner, ordered);
        return 0;
    }
}

int DlmHostSourceOrderCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSourceOrderCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSourceOrderCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSourceOrderCommandClear(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->sources.clear();
    planner->rows.clear();
    planner->status = StatusSuccess;
    return 0;
}

int DlmHostSourceOrderCommandAddSource(void* handle, const char* sourceKey)
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

int DlmHostSourceOrderCommandPlanMove(void* handle, const char* sourceKey, int direction)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    try
    {
        const int currentIndex = FindSourceIndex(planner->sources, sourceKey);
        if (currentIndex < 0)
        {
            planner->status = StatusSourceNotFound;
            planner->rows.clear();
            return 0;
        }

        const int offset = direction < 0 ? -1 : 1;
        return PlanMoveToIndex(*planner, sourceKey, currentIndex + offset);
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourceOrderCommandPlanMoveToIndex(void* handle, const char* sourceKey, int targetIndex)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || sourceKey == nullptr || sourceKey[0] == '\0')
    {
        return -2;
    }

    try
    {
        return PlanMoveToIndex(*planner, sourceKey, targetIndex);
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourceOrderCommandGetStatus(void* handle, int* status)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || status == nullptr)
    {
        return -2;
    }

    *status = planner->status;
    return 0;
}

int DlmHostSourceOrderCommandGetRowCount(void* handle, int* count)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(planner->rows.size());
    return 0;
}

int DlmHostSourceOrderCommandGetRow(void* handle, int index, DlmHostSourceOrderCommandRow* row)
{
    const auto* source = RowAt(AsPlanner(handle), index);
    if (source == nullptr || row == nullptr)
    {
        return -2;
    }

    *row = {};
    row->sourceKeyUtf8ByteCount = static_cast<int>(source->sourceKey.size());
    row->orderIndex = source->orderIndex;
    return 0;
}

int DlmHostSourceOrderCommandCopyRowSourceKey(
    void* handle,
    int index,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    const auto* source = RowAt(AsPlanner(handle), index);
    if (source == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(source->sourceKey, buffer, capacity, actualByteCount);
}
