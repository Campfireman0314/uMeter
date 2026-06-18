using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.Tests.Audio;

public sealed class SignalSourceSnapshotTests
{
    [Fact]
    public void SnapshotExposesSourceIdentityAndExistingAnalyzerMetrics()
    {
        var snapshot = new SignalSourceSnapshot(
            "loopback:plugin-instance-1",
            SignalSourceKind.LocalLoopback,
            null,
            "Lead Vocal",
            0.25f,
            -12.0,
            "-12.0 dB",
            "Active",
            MomentaryLufs: -18.1,
            ShortTermLufs: -17.5,
            IntegratedLufs: -19.3,
            TruePeakDb: -1.2,
            Rms100MsDb: -21.0,
            StereoImageX: 0.2,
            StereoImageY: 0.8,
            StereoImagePoints: [new StereoImagePoint(0.25, 0.5)],
            Correlation: 0.75,
            WaveformSamples: [0.1, -0.1],
            ChannelWaveformSamples: [[0.2], [-0.2]],
            SpectrumBuckets: [0.3, 0.7],
            ChannelSpectrumBuckets: [[0.4], [0.6]]);

        Assert.Equal("loopback:plugin-instance-1", snapshot.SourceKey);
        Assert.Equal(SignalSourceKind.LocalLoopback, snapshot.SourceKind);
        Assert.Equal("Loopback", snapshot.SourceKindLabel);
        Assert.Equal("Lead Vocal", snapshot.DisplayName);
        Assert.Null(snapshot.ProcessId);
        Assert.Equal(-18.1, snapshot.MomentaryLufs);
        Assert.Equal(-1.2, snapshot.TruePeakDb);
        Assert.Equal([0.1, -0.1], snapshot.WaveformSamples);
        Assert.Equal([0.4], snapshot.ChannelSpectrumBuckets![0]);
    }

    [Theory]
    [InlineData(SignalSourceKind.Unknown, "Unknown")]
    [InlineData(SignalSourceKind.WasapiSession, "WASAPI")]
    [InlineData(SignalSourceKind.LocalLoopback, "Loopback")]
    [InlineData(SignalSourceKind.Plugin, "Plugin")]
    public void SourceKindFormatsCompactLabels(SignalSourceKind kind, string expectedLabel)
    {
        var snapshot = new SignalSourceSnapshot(
            "source",
            kind,
            null,
            "Source",
            0,
            double.NegativeInfinity,
            "-inf dB",
            "Inactive");

        Assert.Equal(expectedLabel, snapshot.SourceKindLabel);
    }
}
