using DesktopLoudnessMeter.App.Audio;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class ProcessLoopbackInteropTests
{
    [Fact]
    public void UsesWindowsSdkVirtualLoopbackDevicePath()
    {
        Assert.Equal("VAD\\Process_Loopback", ProcessLoopbackInterop.VirtualAudioDeviceProcessLoopback);
    }

    [Fact]
    public void CompatibilityNoticeIsUnavailableBelowAttemptBuild()
    {
        var notice = WasapiCompatibilityNotice.Create(new Version(10, 0, 18363));

        Assert.NotNull(notice);
        Assert.Contains("unavailable", notice);
        Assert.Contains("19041", notice);
    }

    [Fact]
    public void CompatibilityNoticeIsExperimentalBelowDocumentedSupportBuild()
    {
        var notice = WasapiCompatibilityNotice.Create(new Version(10, 0, 19045));

        Assert.NotNull(notice);
        Assert.Contains("experimental", notice);
        Assert.Contains("20348", notice);
    }

    [Fact]
    public void CompatibilityNoticeIsHiddenAtDocumentedSupportBuild()
    {
        Assert.Null(WasapiCompatibilityNotice.Create(new Version(10, 0, 20348)));
        Assert.Null(WasapiCompatibilityNotice.Create(new Version(10, 0, 22000)));
    }
}
