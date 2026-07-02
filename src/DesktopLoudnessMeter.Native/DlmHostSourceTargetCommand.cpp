#include "DlmHostSourceTargetCommand.h"

#include <algorithm>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StatusSuccess = 0;
    constexpr int StatusSourceNotFound = 1;

    struct DlmHostSourceTargetCommandHandle
    {
        std::vector<std::string> sources;
        std::string sourceKey;
        int status = StatusSuccess;
    };

    DlmHostSourceTargetCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSourceTargetCommandHandle*>(handle);
    }

    bool IsBlank(const char* value)
    {
        return value == nullptr || value[0] == '\0';
    }

    bool HasSource(const std::vector<std::string>& sources, const char* sourceKey)
    {
        if (IsBlank(sourceKey))
        {
            return false;
        }

        return std::find(sources.begin(), sources.end(), sourceKey) != sources.end();
    }

    void ResetPlan(DlmHostSourceTargetCommandHandle& planner)
    {
        planner.sourceKey.clear();
        planner.status = StatusSuccess;
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

int DlmHostSourceTargetCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSourceTargetCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSourceTargetCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSourceTargetCommandClearSources(void* handle)
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

int DlmHostSourceTargetCommandAddSource(void* handle, const char* sourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || IsBlank(sourceKey))
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

int DlmHostSourceTargetCommandPlanTarget(void* handle, const char* sourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    try
    {
        ResetPlan(*planner);
        if (!HasSource(planner->sources, sourceKey))
        {
            planner->status = StatusSourceNotFound;
            return 0;
        }

        planner->sourceKey = sourceKey;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSourceTargetCommandGetPlan(void* handle, DlmHostSourceTargetCommandPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->status = planner->status;
    plan->sourceKeyUtf8ByteCount = static_cast<int>(planner->sourceKey.size());
    return 0;
}

int DlmHostSourceTargetCommandCopySourceKey(
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

    return CopyStringToBuffer(planner->sourceKey, buffer, capacity, actualByteCount);
}
