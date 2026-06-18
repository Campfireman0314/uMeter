using System.Diagnostics;
using DesktopLoudnessMeter.Protocol;
using DesktopLoudnessMeter.SenderKit;

namespace DesktopLoudnessMeter.SenderProbe;

public static class SenderProbeRunner
{
    public static async Task RunAsync(SenderProbeOptions options, CancellationToken cancellationToken = default)
    {
        var handle = options.Handle ?? SenderIdentityStore.LoadOrCreate(options.IdentityPath, "probe");
        await using var sender = new LocalLoopbackSender(new LoopbackSenderOptions(
            handle,
            options.DisplayName,
            LocalLoopbackSourceKind.Plugin,
            options.SampleRate,
            options.Channels,
            options.Port)
        {
            FramesPerPacket = options.FramesPerPacket,
            BufferedPacketCapacity = options.BufferedPackets
        });

        await sender.StartAsync(cancellationToken).ConfigureAwait(false);

        var phase = 0d;
        var packetDuration = TimeSpan.FromSeconds((double)options.FramesPerPacket / options.SampleRate);
        var renameAt = options.Duration / 2;
        var renamed = false;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < options.Duration && !cancellationToken.IsCancellationRequested)
        {
            var samples = SineWaveGenerator.Generate(
                options.FramesPerPacket,
                options.Channels,
                options.SampleRate,
                options.FrequencyHz,
                options.Amplitude,
                ref phase);

            sender.TryEnqueueAudio(samples);

            if (!renamed && options.RenameAfter is not null && stopwatch.Elapsed >= renameAt)
            {
                await sender.RenameAsync(options.RenameAfter, cancellationToken).ConfigureAwait(false);
                renamed = true;
            }

            await Task.Delay(packetDuration, cancellationToken).ConfigureAwait(false);
        }
    }
}
