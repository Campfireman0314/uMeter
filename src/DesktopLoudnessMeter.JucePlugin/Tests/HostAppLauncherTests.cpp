#include "HostAppLauncher.h"

#include <stdexcept>

namespace
{
void expect (bool condition, const char* message)
{
    if (! condition)
    {
        throw std::runtime_error (message);
    }
}

void registeredDirectoryResolvesExecutable()
{
    const auto executable = dlm::HostAppLauncher::resolveExecutable (
        R"("D:\Apps\uMeter")",
        juce::File (R"(C:\Users\tester\AppData\Local)"));

    expect (executable == juce::File (R"(D:\Apps\uMeter\uMeter.exe)"),
            "registered install directory should resolve uMeter.exe");
}

void registeredExecutableIsPreserved()
{
    const auto executable = dlm::HostAppLauncher::resolveExecutable (
        R"(D:\Portable\uMeter.exe)",
        juce::File (R"(C:\Users\tester\AppData\Local)"));

    expect (executable == juce::File (R"(D:\Portable\uMeter.exe)"),
            "registered executable path should be preserved");
}

void missingRegistrationUsesPerUserInstallFallback()
{
    const auto executable = dlm::HostAppLauncher::resolveExecutable (
        {},
        juce::File (R"(C:\Users\tester\AppData\Local)"));

    expect (executable == juce::File (R"(C:\Users\tester\AppData\Local\Programs\uMeter\uMeter.exe)"),
            "missing registration should use the per-user installer path");
}

void hiddenLaunchAddsBackgroundArgument()
{
    juce::String receivedArguments;
    const auto testExecutable = juce::File::getSpecialLocation (juce::File::currentExecutableFile);
    const dlm::HostAppLauncher launcher (
        [testExecutable] { return testExecutable.getFullPathName(); },
        [] { return juce::File {}; },
        [&receivedArguments] (const juce::File&, const juce::String& arguments)
        {
            receivedArguments = arguments;
            return true;
        },
        [] (const juce::File&) { return false; });

    expect (launcher.launch (true).started, "hidden launch should use the injected process starter");
    expect (receivedArguments == "--background", "hidden launch should add the background argument");
}

void runningInstanceSkipsSecondaryProcessLaunch()
{
    auto processStarterCalled = false;
    auto detectorCalled = false;
    const auto testExecutable = juce::File::getSpecialLocation (juce::File::currentExecutableFile);
    const dlm::HostAppLauncher launcher (
        [testExecutable] { return testExecutable.getFullPathName(); },
        [] { return juce::File {}; },
        [&processStarterCalled] (const juce::File&, const juce::String&)
        {
            processStarterCalled = true;
            return true;
        },
        [&detectorCalled, testExecutable] (const juce::File& executable)
        {
            detectorCalled = true;
            return executable == testExecutable;
        });

    const auto result = launcher.launch (true);

    expect (detectorCalled, "launcher should check for a running installed executable");
    expect (! processStarterCalled, "running host should suppress a secondary process launch");
    expect (! result.started, "already-running result should not claim a process was started");
    expect (result.alreadyRunning, "already-running result should identify the existing instance");
    expect (result.succeeded(), "already-running result should be a successful no-op");
    expect (result.message == "uMeter is already running.", "already-running result message mismatch");
}

void defaultDetectorFindsCurrentExecutable()
{
   #if JUCE_WINDOWS
    auto processStarterCalled = false;
    const auto testExecutable = juce::File::getSpecialLocation (juce::File::currentExecutableFile);
    const dlm::HostAppLauncher launcher (
        [testExecutable] { return testExecutable.getFullPathName(); },
        [] { return juce::File {}; },
        [&processStarterCalled] (const juce::File&, const juce::String&)
        {
            processStarterCalled = true;
            return true;
        });

    const auto result = launcher.launch();

    expect (result.alreadyRunning, "default detector should find the current executable");
    expect (! processStarterCalled, "default detector should suppress launching the current executable again");
   #endif
}

void manualLaunchHasNoBackgroundArgument()
{
    juce::String receivedArguments = "not-called";
    const auto testExecutable = juce::File::getSpecialLocation (juce::File::currentExecutableFile);
    const dlm::HostAppLauncher launcher (
        [testExecutable] { return testExecutable.getFullPathName(); },
        [] { return juce::File {}; },
        [&receivedArguments] (const juce::File&, const juce::String& arguments)
        {
            receivedArguments = arguments;
            return true;
        },
        [] (const juce::File&) { return false; });

    expect (launcher.launch().started, "manual launch should use the injected process starter");
    expect (receivedArguments.isEmpty(), "manual launch should remain visible");
}
}

void runHostAppLauncherTests()
{
    registeredDirectoryResolvesExecutable();
    registeredExecutableIsPreserved();
    missingRegistrationUsesPerUserInstallFallback();
    hiddenLaunchAddsBackgroundArgument();
    runningInstanceSkipsSecondaryProcessLaunch();
    defaultDetectorFindsCurrentExecutable();
    manualLaunchHasNoBackgroundArgument();
}
