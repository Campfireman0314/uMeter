using DesktopLoudnessMeter.SenderProbe;
using DesktopLoudnessMeter.App.Audio;

namespace DesktopLoudnessMeter.Tests.SenderProbe;

public sealed class SenderProbeOptionsTests
{
    [Fact]
    public void ParsesProbeOptions()
    {
        var options = SenderProbeOptions.Parse(
            "--name", "Probe",
            "--handle", "probe-handle",
            "--sample-rate", "44100",
            "--channels", "1",
            "--seconds", "0.5",
            "--frequency", "880",
            "--amplitude", "0.25",
            "--frames", "256",
            "--rename-after", "Probe Renamed",
            "--port", "12345");

        Assert.Equal("Probe", options.DisplayName);
        Assert.Equal("probe-handle", options.Handle);
        Assert.Equal(44100, options.SampleRate);
        Assert.Equal(1, options.Channels);
        Assert.Equal(0.5, options.Duration.TotalSeconds, precision: 3);
        Assert.Equal(880, options.FrequencyHz);
        Assert.Equal(0.25f, options.Amplitude);
        Assert.Equal(256, options.FramesPerPacket);
        Assert.Equal("Probe Renamed", options.RenameAfter);
        Assert.Equal(12345, options.Port);
    }

    [Fact]
    public void SineGeneratorProducesInterleavedSamplesInsideAmplitude()
    {
        var phase = 0d;

        var samples = SineWaveGenerator.Generate(
            frames: 16,
            channels: 2,
            sampleRate: 48000,
            frequencyHz: 1000,
            amplitude: 0.5f,
            ref phase);

        Assert.Equal(32, samples.Length);
        Assert.True(samples.All(sample => Math.Abs(sample) <= 0.5f));
        for (var frame = 0; frame < 16; frame++)
        {
            Assert.Equal(samples[frame * 2], samples[(frame * 2) + 1]);
        }
    }

    [Fact]
    public async Task ProbeRunnerRegistersStreamsRenamesAndCloses()
    {
        using var service = new LocalLoopbackSignalSourceService();
        using var receiver = LocalLoopbackUdpReceiver.Start(service, port: 0);
        var options = new SenderProbeOptions
        {
            DisplayName = "Probe Runner",
            Handle = "probe-runner",
            Duration = TimeSpan.FromMilliseconds(350),
            FramesPerPacket = 120,
            FrequencyHz = 440,
            Amplitude = 0.2f,
            RenameAfter = "Probe Runner Renamed",
            Port = receiver.Port
        };

        var runTask = SenderProbeRunner.RunAsync(options);

        await WaitUntil(() => service.GetCurrentSources().Count == 1);
        await WaitUntil(() => service.GetCurrentSources().Single().TruePeakDb is not null);
        await WaitUntil(() => service.GetCurrentSources().Single().DisplayName == "Probe Runner Renamed");
        await runTask;
        await WaitUntil(() => service.GetCurrentSources().Count == 0);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTimeOffset.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(condition());
    }
}
