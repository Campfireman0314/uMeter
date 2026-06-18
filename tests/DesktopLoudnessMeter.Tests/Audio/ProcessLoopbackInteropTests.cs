using DesktopLoudnessMeter.App.Audio;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class ProcessLoopbackInteropTests
{
    [Fact]
    public void UsesWindowsSdkVirtualLoopbackDevicePath()
    {
        Assert.Equal("VAD\\Process_Loopback", ProcessLoopbackInterop.VirtualAudioDeviceProcessLoopback);
    }
}
