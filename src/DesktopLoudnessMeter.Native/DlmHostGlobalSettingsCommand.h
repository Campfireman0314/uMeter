#pragma once

#include "DlmPcmAnalyzer.h"

extern "C"
{
    struct DlmHostGlobalSettingsCommandPlan
    {
        int hasThemeId;
        int themeIdUtf8ByteCount;
        int maxRecentEventEntries;
        int hasEventOutputFolder;
        int eventOutputFolderUtf8ByteCount;
        int showHiddenSources;
    };
}

int DlmHostGlobalSettingsCommandCreate(void** handle);
void DlmHostGlobalSettingsCommandDestroy(void* handle);
int DlmHostGlobalSettingsCommandPlanSetTheme(void* handle, const char* themeId);
int DlmHostGlobalSettingsCommandPlanSetRecentEventLimit(void* handle, int maxRecentEventEntries);
int DlmHostGlobalSettingsCommandPlanSetEventOutputFolder(void* handle, const char* folder);
int DlmHostGlobalSettingsCommandPlanSetShowHiddenSources(void* handle, int showHiddenSources);
int DlmHostGlobalSettingsCommandGetPlan(void* handle, DlmHostGlobalSettingsCommandPlan* plan);
int DlmHostGlobalSettingsCommandCopyThemeId(void* handle, char* buffer, int capacity, int* actualByteCount);
int DlmHostGlobalSettingsCommandCopyEventOutputFolder(void* handle, char* buffer, int capacity, int* actualByteCount);
