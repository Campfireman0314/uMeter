#include "DlmHostAggregateSnapshot.h"

#include <cstring>
#include <string>
#include <vector>

namespace
{
    struct DlmHostAggregateSnapshotHandle
    {
        std::vector<std::string> sourceKeys;
        bool hasSelectedSource = false;
        int selectedSourceIndex = -1;
        bool includeSelectedDetail = false;
        std::string statusText = "No active audio sources";
    };

    DlmHostAggregateSnapshotHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostAggregateSnapshotHandle*>(handle);
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

    int FindSourceIndex(const std::vector<std::string>& sourceKeys, const char* selectedSourceKey)
    {
        if (IsBlank(selectedSourceKey))
        {
            return -1;
        }

        for (int index = 0; index < static_cast<int>(sourceKeys.size()); ++index)
        {
            if (sourceKeys[static_cast<size_t>(index)] == selectedSourceKey)
            {
                return index;
            }
        }

        return -1;
    }

    std::string FormatSourceStatus(int sourceCount)
    {
        if (sourceCount == 0)
        {
            return "No active audio sources";
        }

        if (sourceCount == 1)
        {
            return "1 source tracked";
        }

        return std::to_string(sourceCount) + " sources tracked";
    }

    void ResetPlan(DlmHostAggregateSnapshotHandle& planner)
    {
        planner.hasSelectedSource = false;
        planner.selectedSourceIndex = -1;
        planner.includeSelectedDetail = false;
        planner.statusText = FormatSourceStatus(static_cast<int>(planner.sourceKeys.size()));
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

int DlmHostAggregateSnapshotCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostAggregateSnapshotHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostAggregateSnapshotDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostAggregateSnapshotClear(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    planner->sourceKeys.clear();
    ResetPlan(*planner);
    return 0;
}

int DlmHostAggregateSnapshotAddSource(void* handle, const char* sourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || IsBlank(sourceKey))
    {
        return -2;
    }

    try
    {
        planner->sourceKeys.emplace_back(sourceKey);
        planner->statusText = FormatSourceStatus(static_cast<int>(planner->sourceKeys.size()));
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostAggregateSnapshotProject(void* handle, const char* selectedSourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        ResetPlan(*planner);
        planner->selectedSourceIndex = FindSourceIndex(planner->sourceKeys, selectedSourceKey);
        planner->hasSelectedSource = planner->selectedSourceIndex >= 0;
        planner->includeSelectedDetail = planner->hasSelectedSource;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostAggregateSnapshotGetPlan(void* handle, DlmHostAggregateSnapshotPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->hasSelectedSource = planner->hasSelectedSource ? 1 : 0;
    plan->selectedSourceIndex = planner->selectedSourceIndex;
    plan->includeSelectedDetail = planner->includeSelectedDetail ? 1 : 0;
    plan->statusTextUtf8ByteCount = static_cast<int>(planner->statusText.size());
    return 0;
}

int DlmHostAggregateSnapshotCopyStatusText(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(planner->statusText, buffer, capacity, actualByteCount);
}
