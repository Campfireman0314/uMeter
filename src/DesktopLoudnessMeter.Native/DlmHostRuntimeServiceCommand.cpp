#include "DlmHostRuntimeServiceCommand.h"

#include <algorithm>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    constexpr int StatusSuccess = 0;
    constexpr int StatusSourceNotFound = 1;
    constexpr int HostPerformanceModeEco = 1;
    constexpr int AnalysisPerformanceModeNormal = 0;
    constexpr int AnalysisPerformanceModeEco = 1;

    struct DlmHostRuntimeServiceCommandHandle
    {
        std::vector<std::string> sources;
        std::string sourceKey;
        int status = StatusSuccess;
        bool shouldResetAll = false;
        bool shouldResetSource = false;
        int sourceKind = 0;
        bool hasProcessId = false;
        int processId = 0;
        int hostPerformanceMode = 0;
        int analysisPerformanceMode = AnalysisPerformanceModeNormal;
    };

    DlmHostRuntimeServiceCommandHandle* AsPlanner(void* handle)
    {
        return static_cast<DlmHostRuntimeServiceCommandHandle*>(handle);
    }

    bool HasSource(const std::vector<std::string>& sources, const char* sourceKey)
    {
        if (sourceKey == nullptr || sourceKey[0] == '\0')
        {
            return false;
        }

        return std::find(sources.begin(), sources.end(), sourceKey) != sources.end();
    }

    void ResetPlan(DlmHostRuntimeServiceCommandHandle& planner)
    {
        planner.sourceKey.clear();
        planner.status = StatusSuccess;
        planner.shouldResetAll = false;
        planner.shouldResetSource = false;
        planner.sourceKind = 0;
        planner.hasProcessId = false;
        planner.processId = 0;
        planner.hostPerformanceMode = 0;
        planner.analysisPerformanceMode = AnalysisPerformanceModeNormal;
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

int DlmHostRuntimeServiceCommandCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostRuntimeServiceCommandHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostRuntimeServiceCommandDestroy(void* handle)
{
    delete AsPlanner(handle);
}

int DlmHostRuntimeServiceCommandClearSources(void* handle)
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

int DlmHostRuntimeServiceCommandAddSource(void* handle, const char* sourceKey)
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

int DlmHostRuntimeServiceCommandPlanResetAll(void* handle)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    ResetPlan(*planner);
    planner->shouldResetAll = true;
    return 0;
}

int DlmHostRuntimeServiceCommandPlanResetSource(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId)
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

        planner->shouldResetSource = true;
        planner->sourceKey = sourceKey;
        planner->sourceKind = sourceKind;
        planner->hasProcessId = hasProcessId != 0;
        planner->processId = processId;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostRuntimeServiceCommandPlanSetPerformanceMode(void* handle, int hostPerformanceMode)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    ResetPlan(*planner);
    planner->hostPerformanceMode = hostPerformanceMode;
    planner->analysisPerformanceMode = hostPerformanceMode == HostPerformanceModeEco
        ? AnalysisPerformanceModeEco
        : AnalysisPerformanceModeNormal;
    return 0;
}

int DlmHostRuntimeServiceCommandGetPlan(void* handle, DlmHostRuntimeServiceCommandPlan* plan)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr || plan == nullptr)
    {
        return -2;
    }

    *plan = {};
    plan->status = planner->status;
    plan->shouldResetAll = planner->shouldResetAll ? 1 : 0;
    plan->shouldResetSource = planner->shouldResetSource ? 1 : 0;
    plan->sourceKeyUtf8ByteCount = static_cast<int>(planner->sourceKey.size());
    plan->sourceKind = planner->sourceKind;
    plan->hasProcessId = planner->hasProcessId ? 1 : 0;
    plan->processId = planner->processId;
    plan->hostPerformanceMode = planner->hostPerformanceMode;
    plan->analysisPerformanceMode = planner->analysisPerformanceMode;
    return 0;
}

int DlmHostRuntimeServiceCommandCopySourceKey(void* handle, char* buffer, int capacity, int* actualByteCount)
{
    auto* planner = AsPlanner(handle);
    if (planner == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(planner->sourceKey, buffer, capacity, actualByteCount);
}
