#pragma once

#include <juce_core/juce_core.h>

#include <functional>

namespace dlm
{
struct HostAppLaunchResult
{
    bool started = false;
    bool alreadyRunning = false;
    juce::String message;

    bool succeeded() const noexcept { return started || alreadyRunning; }
};

struct HostAppLaunchPreferences
{
    bool autoInvoke = false;
    bool startHidden = false;

    static HostAppLaunchPreferences load (const juce::File& settingsFile);
};

class HostAppLauncher
{
public:
    using InstallLocationReader = std::function<juce::String()>;
    using LocalAppDataProvider = std::function<juce::File()>;
    using ProcessStarter = std::function<bool(const juce::File&, const juce::String&)>;
    using RunningProcessDetector = std::function<bool(const juce::File&)>;

    HostAppLauncher (InstallLocationReader installLocationReader = {},
                     LocalAppDataProvider localAppDataProvider = {},
                     ProcessStarter processStarter = {},
                     RunningProcessDetector runningProcessDetector = {});

    HostAppLaunchResult launch (bool startHidden = false) const;

    static juce::File resolveExecutable (const juce::String& installLocation,
                                         const juce::File& localAppDataDirectory);

private:
    InstallLocationReader readInstallLocation;
    LocalAppDataProvider getLocalAppDataDirectory;
    ProcessStarter startProcess;
    RunningProcessDetector isProcessRunning;
};
}
