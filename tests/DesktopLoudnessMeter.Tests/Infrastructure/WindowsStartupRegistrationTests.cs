using DesktopLoudnessMeter.App.Infrastructure;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class WindowsStartupRegistrationTests
{
    [Fact]
    public void BuildCommandQuotesExecutablePath()
    {
        Assert.Equal(
            "\"C:\\Program Files\\uMeter\\uMeter.exe\"",
            WindowsStartupRegistration.BuildCommand("C:\\Program Files\\uMeter\\uMeter.exe", startHidden: false));
    }

    [Fact]
    public void BuildCommandAddsBackgroundArgumentOnlyForHiddenStartup()
    {
        Assert.Equal(
            "\"C:\\Apps\\uMeter.exe\" --background",
            WindowsStartupRegistration.BuildCommand(" C:\\Apps\\uMeter.exe ", startHidden: true));
    }
}
