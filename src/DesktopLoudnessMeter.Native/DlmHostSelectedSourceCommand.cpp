#include "DlmHostSelectedSourceCommand.h"

#include <algorithm>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StatusSuccess = 0;
    constexpr int StatusSourceNotFound = 1;

    struct DlmHostSelectedSourceCommandHandle
    {
        std::vector<std::string> sources;
        std::string selectedSourceKey;
        int status = StatusSuccess;
        bool shouldClearSelected = false;
        bool shouldSetSelected = false;
    };

    DlmHostSelectedSourceCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostSelectedSourceCommandHandle*>(handle);
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

    bool Equals(const char* left, const char* right)
    {
        if (left == nullptr || right == nullptr)
        {
            return false;
        }

        return std::strcmp(left, right) == 0;
    }

    void ResetPlan(DlmHostSelectedSourceCommandHandle& planner)
    {
        planner.selectedSourceKey.clear();
        planner.status = StatusSuccess;
        planner.shouldClearSelected = false;
        planner.shouldSetSelected = false;
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

int DlmHostSelectedSourceCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostSelectedSourceCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostSelectedSourceCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostSelectedSourceCommandClearSources(void* handle)
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

int DlmHostSelectedSourceCommandAddSource(void* handle, const char* sourceKey)
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

int DlmHostSelectedSourceCommandPlanSelect(void* handle, const char* sourceKey)
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

        planner->selectedSourceKey = sourceKey;
        planner->shouldSetSelected = true;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostSelectedSourceCommandPlanBackToGlobal(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    ResetPlan(*planner);
    planner->shouldClearSelected = true;
    return 0;
}

int DlmHostSelectedSourceCommandPlanReconcile(void* handle, const char* selectedSourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    ResetPlan(*planner);
    if (!IsBlank(selectedSourceKey) && !HasSource(planner->sources, selectedSourceKey))
    {
        planner->shouldClearSelected = true;
    }

    return 0;
}

int DlmHostSelectedSourceCommandPlanHiddenSource(
    void* handle,
    const char* sourceKey,
    int isHidden,
    int showHiddenSources,
    const char* selectedSourceKey)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    ResetPlan(*planner);
    if (isHidden != 0 && showHiddenSources == 0 && Equals(sourceKey, selectedSourceKey))
    {
        planner->shouldClearSelected = true;
    }

    return 0;
}

int DlmHostSelectedSourceCommandGetPlan(void* handle, DlmHostSelectedSourceCommandPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->status = planner->status;
    plan->shouldClearSelected = planner->shouldClearSelected ? 1 : 0;
    plan->shouldSetSelected = planner->shouldSetSelected ? 1 : 0;
    plan->selectedSourceKeyUtf8ByteCount = static_cast<int>(planner->selectedSourceKey.size());
    return 0;
}

int DlmHostSelectedSourceCommandCopySelectedSourceKey(
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

    return CopyStringToBuffer(planner->selectedSourceKey, buffer, capacity, actualByteCount);
}
