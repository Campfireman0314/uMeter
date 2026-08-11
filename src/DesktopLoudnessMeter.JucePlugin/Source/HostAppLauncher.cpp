#include "HostAppLauncher.h"

#if JUCE_WINDOWS
#include <windows.h>
#include <tlhelp32.h>
#endif

namespace
{
constexpr auto installLocationRegistryValue =
    "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\uMeter\\InstallLocation";

juce::String readRegisteredInstallLocation()
{
   #if JUCE_WINDOWS
    return juce::WindowsRegistry::getValue (installLocationRegistryValue).trim().unquoted();
   #else
    return {};
   #endif
}

juce::File localAppDataDirectory()
{
   #if JUCE_WINDOWS
    return juce::File::getSpecialLocation (juce::File::windowsLocalAppData);
   #else
    return juce::File::getSpecialLocation (juce::File::userApplicationDataDirectory);
   #endif
}

bool isExecutableRunningInCurrentSession (const juce::File& executable)
{
   #if JUCE_WINDOWS
    DWORD currentSessionId = 0;
    if (! ProcessIdToSessionId (GetCurrentProcessId(), &currentSessionId))
    {
        return false;
    }

    const auto snapshot = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    PROCESSENTRY32W entry {};
    entry.dwSize = sizeof (entry);
    auto found = false;
    if (Process32FirstW (snapshot, &entry))
    {
        do
        {
            if (juce::String (entry.szExeFile).compareIgnoreCase (executable.getFileName()) != 0)
            {
                continue;
            }

            DWORD processSessionId = 0;
            if (! ProcessIdToSessionId (entry.th32ProcessID, &processSessionId)
                || processSessionId != currentSessionId)
            {
                continue;
            }

            const auto process = OpenProcess (PROCESS_QUERY_LIMITED_INFORMATION, FALSE, entry.th32ProcessID);
            if (process == nullptr)
            {
                if (GetLastError() == ERROR_ACCESS_DENIED)
                {
                    found = true;
                    break;
                }

                continue;
            }

            wchar_t imagePath[32768] {};
            DWORD imagePathLength = static_cast<DWORD> (std::size (imagePath));
            if (QueryFullProcessImageNameW (process, 0, imagePath, &imagePathLength))
            {
                const auto runningExecutable = juce::File (juce::String (imagePath, static_cast<int> (imagePathLength)));
                found = runningExecutable.getFullPathName().compareIgnoreCase (executable.getFullPathName()) == 0;
            }
            else if (GetLastError() == ERROR_ACCESS_DENIED)
            {
                found = true;
            }

            CloseHandle (process);
            if (found)
            {
                break;
            }
        }
        while (Process32NextW (snapshot, &entry));
    }

    CloseHandle (snapshot);
    return found;
   #else
    juce::ignoreUnused (executable);
    return false;
   #endif
}
}

namespace dlm
{
HostAppLaunchPreferences HostAppLaunchPreferences::load (const juce::File& settingsFile)
{
    if (! settingsFile.existsAsFile())
    {
        return {};
    }

    const auto parsed = juce::JSON::parse (settingsFile.loadFileAsString());
    if (const auto* object = parsed.getDynamicObject())
    {
        return {
            static_cast<bool> (object->getProperty ("AutoInvokeHostApp")),
            static_cast<bool> (object->getProperty ("HideToTrayAfterStartup"))
        };
    }

    return {};
}

HostAppLauncher::HostAppLauncher (InstallLocationReader installLocationReader,
                                  LocalAppDataProvider localAppDataProvider,
                                  ProcessStarter processStarter,
                                  RunningProcessDetector runningProcessDetector)
    : readInstallLocation (installLocationReader ? std::move (installLocationReader)
                                                 : InstallLocationReader { readRegisteredInstallLocation }),
      getLocalAppDataDirectory (localAppDataProvider ? std::move (localAppDataProvider)
                                                     : LocalAppDataProvider { localAppDataDirectory }),
      startProcess (processStarter ? std::move (processStarter)
                                   : ProcessStarter { [] (const juce::File& executable, const juce::String& arguments)
                                     {
                                         return executable.startAsProcess (arguments);
                                     } }),
      isProcessRunning (runningProcessDetector ? std::move (runningProcessDetector)
                                               : RunningProcessDetector { isExecutableRunningInCurrentSession })
{
}

HostAppLaunchResult HostAppLauncher::launch (bool startHidden) const
{
    const auto executable = resolveExecutable (readInstallLocation(), getLocalAppDataDirectory());
    if (! executable.existsAsFile())
    {
        return { false, false, "uMeter app was not found." };
    }

    if (isProcessRunning (executable))
    {
        return { false, true, "uMeter is already running." };
    }

    if (! startProcess (executable, startHidden ? "--background" : juce::String {}))
    {
        return { false, false, "uMeter could not be started." };
    }

    return { true, false, "uMeter started." };
}

juce::File HostAppLauncher::resolveExecutable (const juce::String& installLocation,
                                               const juce::File& localAppDataDirectory)
{
    const auto registeredPath = installLocation.trim().unquoted();
    if (registeredPath.isNotEmpty())
    {
        const juce::File registeredFile (registeredPath);
        return registeredFile.hasFileExtension ("exe")
            ? registeredFile
            : registeredFile.getChildFile ("uMeter.exe");
    }

    return localAppDataDirectory
        .getChildFile ("Programs")
        .getChildFile ("uMeter")
        .getChildFile ("uMeter.exe");
}
}
