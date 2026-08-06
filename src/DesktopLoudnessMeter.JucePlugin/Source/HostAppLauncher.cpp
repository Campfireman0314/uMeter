#include "HostAppLauncher.h"

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
                                  ProcessStarter processStarter)
    : readInstallLocation (installLocationReader ? std::move (installLocationReader)
                                                 : InstallLocationReader { readRegisteredInstallLocation }),
      getLocalAppDataDirectory (localAppDataProvider ? std::move (localAppDataProvider)
                                                     : LocalAppDataProvider { localAppDataDirectory }),
      startProcess (processStarter ? std::move (processStarter)
                                   : ProcessStarter { [] (const juce::File& executable, const juce::String& arguments)
                                     {
                                         return executable.startAsProcess (arguments);
                                     } })
{
}

HostAppLaunchResult HostAppLauncher::launch (bool startHidden) const
{
    const auto executable = resolveExecutable (readInstallLocation(), getLocalAppDataDirectory());
    if (! executable.existsAsFile())
    {
        return { false, "uMeter app was not found." };
    }

    if (! startProcess (executable, startHidden ? "--background" : juce::String {}))
    {
        return { false, "uMeter could not be started." };
    }

    return { true, "uMeter started." };
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
