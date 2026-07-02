#include "DlmHostRuntimeState.h"

#include <cstring>
#include <string>

namespace
{
    struct DlmHostRuntimeStateHandle
    {
        std::string selectedSourceKey;
        bool hasSelectedSourceKey = false;
        bool showHiddenSources = false;
        int hostPerformanceMode = 0;
    };

    DlmHostRuntimeStateHandle* AsState(void* handle)
    {
        return static_cast<DlmHostRuntimeStateHandle*>(handle);
    }

    bool IsBlank(const char* value)
    {
        return value == nullptr || value[0] == '\0';
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

int DlmHostRuntimeStateCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostRuntimeStateHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostRuntimeStateDestroy(void* handle)
{
    delete AsState(handle);
}

int DlmHostRuntimeStateSetSelectedSourceKey(void* handle, const char* sourceKey)
{
    auto* state = AsState(handle);
    if (state == nullptr)
    {
        return -2;
    }

    try
    {
        if (IsBlank(sourceKey))
        {
            state->selectedSourceKey.clear();
            state->hasSelectedSourceKey = false;
            return 0;
        }

        state->selectedSourceKey = sourceKey;
        state->hasSelectedSourceKey = true;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostRuntimeStateClearSelectedSourceKey(void* handle)
{
    auto* state = AsState(handle);
    if (state == nullptr)
    {
        return -2;
    }

    state->selectedSourceKey.clear();
    state->hasSelectedSourceKey = false;
    return 0;
}

int DlmHostRuntimeStateSetShowHiddenSources(void* handle, int showHiddenSources)
{
    auto* state = AsState(handle);
    if (state == nullptr)
    {
        return -2;
    }

    state->showHiddenSources = showHiddenSources != 0;
    return 0;
}

int DlmHostRuntimeStateSetPerformanceMode(void* handle, int hostPerformanceMode)
{
    auto* state = AsState(handle);
    if (state == nullptr)
    {
        return -2;
    }

    state->hostPerformanceMode = hostPerformanceMode;
    return 0;
}

int DlmHostRuntimeStateApplySelectedSourcePlan(
    void* handle,
    int shouldClearSelected,
    int shouldSetSelected,
    const char* selectedSourceKey)
{
    if (shouldClearSelected != 0)
    {
        return DlmHostRuntimeStateClearSelectedSourceKey(handle);
    }

    if (shouldSetSelected != 0)
    {
        return DlmHostRuntimeStateSetSelectedSourceKey(handle, selectedSourceKey);
    }

    return AsState(handle) == nullptr ? -2 : 0;
}

int DlmHostRuntimeStateApplyShowHiddenPlan(void* handle, int showHiddenSources)
{
    return DlmHostRuntimeStateSetShowHiddenSources(handle, showHiddenSources);
}

int DlmHostRuntimeStateApplyPerformanceModePlan(void* handle, int hostPerformanceMode)
{
    return DlmHostRuntimeStateSetPerformanceMode(handle, hostPerformanceMode);
}

int DlmHostRuntimeStateGetSnapshot(void* handle, DlmHostRuntimeStateSnapshot* snapshot)
{
    auto* state = AsState(handle);
    if (state == nullptr || snapshot == nullptr)
    {
        return -2;
    }

    *snapshot = {};
    snapshot->hasSelectedSourceKey = state->hasSelectedSourceKey ? 1 : 0;
    snapshot->selectedSourceKeyUtf8ByteCount = state->hasSelectedSourceKey
        ? static_cast<int>(state->selectedSourceKey.size())
        : 0;
    snapshot->showHiddenSources = state->showHiddenSources ? 1 : 0;
    snapshot->hostPerformanceMode = state->hostPerformanceMode;
    return 0;
}

int DlmHostRuntimeStateCopySelectedSourceKey(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    auto* state = AsState(handle);
    if (state == nullptr)
    {
        return -2;
    }

    return CopyStringToBuffer(state->selectedSourceKey, buffer, capacity, actualByteCount);
}
