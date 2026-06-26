using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class AudioSessionMeterViewModelTests
{
    [Fact]
    public void UsesPeakAsDefaultPrimaryMetric()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(peakDbText: "-6.0 dB"));

        Assert.Equal(MeterDisplayMode.Peak, row.DisplayMode);
        Assert.Equal("-6.0 dB", row.PrimaryValueText);
    }

    [Theory]
    [InlineData(-24, ReadoutSeverity.Normal)]
    [InlineData(-12, ReadoutSeverity.Warm)]
    [InlineData(-6, ReadoutSeverity.Hot)]
    [InlineData(-0.5, ReadoutSeverity.Critical)]
    [InlineData(0.1, ReadoutSeverity.Over)]
    public void ClassifiesPeakReadoutSeverity(double peakDb, ReadoutSeverity expectedSeverity)
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            peakDb: peakDb,
            peakDbText: $"{peakDb:0.0} dB"));

        Assert.Equal(expectedSeverity, row.PrimaryValueSeverity);
    }

    [Fact]
    public void PeakReadoutSeverityIsMutedWhenSourceIsUnavailable()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(peakDb: -0.5));

        row.MarkAudioHandleMissing();

        Assert.Equal(ReadoutSeverity.Muted, row.PrimaryValueSeverity);
    }

    [Fact]
    public void LufsModeUsesNeutralPrimaryReadout()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            peakDb: -0.5,
            momentaryLufs: -18));

        row.ApplyDisplayMode(MeterDisplayMode.Lufs);

        Assert.Equal(ReadoutSeverity.Normal, row.PrimaryValueSeverity);
    }

    [Fact]
    public void SourceIdentityTextUsesCompactSourceKindLabel()
    {
        var row = new AudioSessionMeterViewModel(new SignalSourceSnapshot(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            "Lead Vocal",
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active"));

        Assert.Equal("Plugin", row.SourceKindLabel);
        Assert.Equal("Plugin source - lead", row.SourceIdentityText);
    }

    [Fact]
    public void SourceIdentityTextIncludesProcessOrStableHandle()
    {
        var wasapi = new AudioSessionMeterViewModel(new SignalSourceSnapshot(
            "wasapi:100",
            SignalSourceKind.WasapiSession,
            100,
            "Browser",
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active"));
        var plugin = new AudioSessionMeterViewModel(new SignalSourceSnapshot(
            "loopback:lead-vocal-bus",
            SignalSourceKind.Plugin,
            null,
            "Lead Vocal",
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active"));

        Assert.Equal("WASAPI source - PID 100", wasapi.SourceIdentityText);
        Assert.Equal("Plugin source - lead-vocal-bus", plugin.SourceIdentityText);
    }

    [Fact]
    public void DuplicateDisplayNamesUseQualifiedRowTitleWithoutChangingProviderName()
    {
        var row = new AudioSessionMeterViewModel(new SignalSourceSnapshot(
            "loopback:lead-vocal-bus",
            SignalSourceKind.Plugin,
            null,
            "Shared Name",
            0.5f,
            -6.0,
            "-6.0 dB",
            "Active"));

        Assert.Equal("Shared Name", row.DisplayTitle);

        row.SetDisplayNameCollision(true);

        Assert.Equal("Shared Name", row.DisplayName);
        Assert.Equal("Shared Name - Plugin lead-vocal-bus", row.DisplayTitle);

        row.SetDisplayNameCollision(false);

        Assert.Equal("Shared Name", row.DisplayTitle);
    }

    [Fact]
    public void MissingAudioHandleUsesSourceFacingStatusText()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());

        row.MarkAudioHandleMissing();

        Assert.Equal("Source handle missing", row.State);
        Assert.Equal("Source handle missing", row.SourceStatusText);
    }

    [Fact]
    public void ShowsUnavailableLufsPlaceholderWithoutAnalyzerData()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());

        row.ApplyDisplayMode(MeterDisplayMode.Lufs);

        Assert.Equal(MeterDisplayMode.Lufs, row.DisplayMode);
        Assert.Equal("LUFS n/a", row.PrimaryValueText);
    }

    [Fact]
    public void UsesMomentaryLufsWhenAnalyzerDataExists()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(momentaryLufs: -18.42));

        row.ApplyDisplayMode(MeterDisplayMode.Lufs);

        Assert.Equal("-18.4 LUFS", row.PrimaryValueText);
    }

    [Fact]
    public void ExposesSupportedTruePeakOversamplingOptions()
    {
        Assert.Equal(2, (int)TruePeakOversampling.TwoX);
        Assert.Equal(4, (int)TruePeakOversampling.FourX);
        Assert.Equal(8, (int)TruePeakOversampling.EightX);
    }

    [Fact]
    public void TruePeakOversamplingSelectionRaisesRequest()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());
        TruePeakOversampling? requested = null;
        row.TruePeakOversamplingRequested += (_, oversampling) => requested = oversampling;

        row.TruePeakOversampling = TruePeakOversampling.EightX;

        Assert.Equal(TruePeakOversampling.EightX, row.TruePeakOversampling);
        Assert.Equal(TruePeakOversampling.EightX, requested);
    }

    [Fact]
    public void SmoothsRisingDisplayLevelTowardTarget()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(peak: 0.1f));

        row.Update(Snapshot(peak: 0.9f));

        Assert.InRange(row.DisplayLevel, 0.1f, 0.9f);
    }

    [Fact]
    public void ExposesMeterFractionsForDetailLevels()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            momentaryLufs: -23,
            shortTermLufs: -18,
            integratedLufs: -14,
            truePeakDb: -1,
            rms100MsDb: -20,
            correlation: 0.25));

        Assert.InRange(row.MomentaryLufsLevel, 0, 1);
        Assert.InRange(row.ShortTermLufsLevel, 0, 1);
        Assert.InRange(row.IntegratedLufsLevel, 0, 1);
        Assert.InRange(row.TruePeakLevel, 0, 1);
        Assert.InRange(row.Rms100MsLevel, 0, 1);
        Assert.InRange(row.CorrelationLevel, 0, 1);
    }

    [Fact]
    public void MapsDetailLevelsOnDbScaleRanges()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            momentaryLufs: -30,
            shortTermLufs: -60,
            integratedLufs: 0,
            truePeakDb: -24,
            rms100MsDb: -30,
            correlation: -1));

        Assert.Equal(0.5, row.MomentaryLufsLevel, precision: 3);
        Assert.Equal(0, row.ShortTermLufsLevel, precision: 3);
        Assert.Equal(1, row.IntegratedLufsLevel, precision: 3);
        Assert.Equal(0.571, row.TruePeakLevel, precision: 3);
        Assert.Equal(0.5, row.Rms100MsLevel, precision: 3);
        Assert.Equal(0, row.CorrelationLevel, precision: 3);
    }

    [Fact]
    public void TruePeakTextShowsCurrentAndHeldMaximum()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(truePeakDb: -17));

        Assert.Equal("-17.0dB", row.TruePeakText);
        Assert.Equal("max -17.0dB", row.TruePeakMaxText);
        Assert.Equal(ReadoutSeverity.Normal, row.TruePeakReadoutSeverity);

        row.Update(Snapshot(truePeakDb: 0.3));

        Assert.Equal("+0.3dB", row.TruePeakText);
        Assert.Equal("max +0.3dB", row.TruePeakMaxText);
        Assert.Equal(ReadoutSeverity.Over, row.TruePeakReadoutSeverity);

        row.Update(Snapshot(truePeakDb: -12));

        Assert.Equal("-12.0dB", row.TruePeakText);
        Assert.Equal("max +0.3dB", row.TruePeakMaxText);
        Assert.Equal(ReadoutSeverity.Over, row.TruePeakReadoutSeverity);
    }

    [Fact]
    public void TruePeakResetClearsHeldMaximum()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(truePeakDb: 0.3));

        row.Reset();

        Assert.Equal("n/a", row.TruePeakText);
        Assert.Equal("max n/a", row.TruePeakMaxText);
        Assert.Equal(ReadoutSeverity.Muted, row.TruePeakReadoutSeverity);
    }

    [Fact]
    public void ExposesStereoFanAndCorrelationValues()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            stereoImageX: -0.35,
            stereoImageY: 0.42,
            stereoImagePoints: [new StereoImagePoint(-0.1, 0.2)],
            correlation: 0.75));

        Assert.Equal(-0.35, row.StereoImageX);
        Assert.Equal(0.42, row.StereoImageY);
        Assert.Equal(-0.1, Assert.Single(row.StereoImagePoints).Angle);
        Assert.Equal(0.75, row.CorrelationValue);
    }

    [Fact]
    public void ExposesWaveformAndSpectrumRollValues()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: [0.1, -0.2],
            channelWaveformSamples: [[0.2, -0.2], [0.4, -0.4]],
            spectrumBuckets: [0.25, 0.75],
            channelSpectrumBuckets: [[0.1, 0.2], [0.3, 0.4]]));

        Assert.Equal([0.1, -0.2], row.WaveformSamples);
        Assert.Equal([0.2, -0.2], row.LeftWaveformSamples);
        Assert.Equal([0.4, -0.4], row.RightWaveformSamples);
        Assert.Equal([0.25, 0.75], row.SpectrumBuckets);
        Assert.Equal([0.1, 0.2], row.LeftSpectrumBuckets);
        Assert.Equal([0.3, 0.4], row.RightSpectrumBuckets);
    }

    [Fact]
    public void SpectrumDisplayModeSelectionRaisesRequest()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());
        SpectrumDisplayMode? requested = null;
        row.SpectrumDisplayModeRequested += (_, displayMode) => requested = displayMode;

        row.SpectrumDisplayMode = SpectrumDisplayMode.Channels;

        Assert.Equal(SpectrumDisplayMode.Channels, row.SpectrumDisplayMode);
        Assert.True(row.IsChannelSpectrumVisible);
        Assert.False(row.IsOverallSpectrumVisible);
        Assert.Equal(SpectrumDisplayMode.Channels, requested);
    }

    [Fact]
    public void WaveformDisplayModeSelectionRaisesRequest()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());
        WaveformDisplayMode? requested = null;
        row.WaveformDisplayModeRequested += (_, displayMode) => requested = displayMode;

        row.WaveformDisplayMode = WaveformDisplayMode.Channels;

        Assert.Equal(WaveformDisplayMode.Channels, row.WaveformDisplayMode);
        Assert.True(row.IsChannelWaveformVisible);
        Assert.False(row.IsOverallWaveformVisible);
        Assert.Equal(WaveformDisplayMode.Channels, requested);
    }

    [Fact]
    public void UpdateDoesNotRepublishUnchangedRollBuffers()
    {
        double[] waveform = [0.1, -0.2];
        double[] spectrum = [0.25, 0.75];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum));
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.Update(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum));

        Assert.DoesNotContain(nameof(AudioSessionMeterViewModel.WaveformSamples), changedProperties);
        Assert.DoesNotContain(nameof(AudioSessionMeterViewModel.SpectrumBuckets), changedProperties);
    }

    [Fact]
    public void UpdateRaisesSpectrumPlotTicksForAdvancedAnalyzerFramesWithoutRepublishingBuffers()
    {
        double[] waveform = [0.1, -0.2];
        double[] spectrum = [0.25, 0.75];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 256));
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.Update(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 512));

        Assert.DoesNotContain(nameof(AudioSessionMeterViewModel.WaveformPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.SpectrumPlotTick), changedProperties);
        Assert.DoesNotContain(nameof(AudioSessionMeterViewModel.WaveformSamples), changedProperties);
        Assert.DoesNotContain(nameof(AudioSessionMeterViewModel.SpectrumBuckets), changedProperties);
    }

    [Fact]
    public void UpdateRaisesWaveformPlotTickWhenWaveformBufferChanges()
    {
        double[] waveform = [0.1, -0.2];
        double[] nextWaveform = [0.3, -0.4];
        double[] spectrum = [0.25, 0.75];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 256));
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.Update(Snapshot(
            waveformSamples: nextWaveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 512));

        Assert.Contains(nameof(AudioSessionMeterViewModel.WaveformPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.WaveformSamples), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.SpectrumPlotTick), changedProperties);
    }

    [Fact]
    public void FiniteMomentaryLoudnessAdvancesClockPlotTick()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            momentaryLufs: -24,
            plotFrameVersion: 256));
        var tick = row.LoudnessClockPlotTick;
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.Update(Snapshot(
            momentaryLufs: -18,
            plotFrameVersion: 512));

        Assert.True(row.LoudnessClockPlotTick > tick);
        Assert.Equal(-18, row.LoudnessClockMomentaryDb);
        Assert.Contains(nameof(AudioSessionMeterViewModel.LoudnessClockPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.LoudnessClockMomentaryDb), changedProperties);
    }

    [Fact]
    public void MissingMomentaryLoudnessDoesNotAdvanceClockPlotTick()
    {
        var row = new AudioSessionMeterViewModel(Snapshot(
            momentaryLufs: -24,
            plotFrameVersion: 256));
        var tick = row.LoudnessClockPlotTick;

        row.Update(Snapshot(
            momentaryLufs: null,
            plotFrameVersion: 512));

        Assert.Equal(tick, row.LoudnessClockPlotTick);
        Assert.True(double.IsNaN(row.LoudnessClockMomentaryDb));
    }

    [Fact]
    public void ResetClearsRollBuffersAndAcceptsRewoundPlotFrames()
    {
        double[] waveform = [0.1, -0.2];
        double[] spectrum = [0.25, 0.75];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            momentaryLufs: -18,
            plotFrameVersion: 4096));
        var waveformTickBeforeReset = row.WaveformPlotTick;
        var spectrumTickBeforeReset = row.SpectrumPlotTick;
        var loudnessClockTickBeforeReset = row.LoudnessClockPlotTick;
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.Reset();

        Assert.Empty(row.WaveformSamples);
        Assert.Empty(row.SpectrumBuckets);
        Assert.True(row.WaveformPlotTick > waveformTickBeforeReset);
        Assert.True(row.SpectrumPlotTick > spectrumTickBeforeReset);
        Assert.True(row.LoudnessClockPlotTick > loudnessClockTickBeforeReset);
        Assert.True(double.IsNaN(row.LoudnessClockMomentaryDb));
        Assert.Contains(nameof(AudioSessionMeterViewModel.WaveformSamples), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.SpectrumBuckets), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.WaveformPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.SpectrumPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.LoudnessClockPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.LoudnessClockMomentaryDb), changedProperties);

        changedProperties.Clear();
        row.Update(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 128));

        Assert.Equal(waveform, row.WaveformSamples);
        Assert.Equal(spectrum, row.SpectrumBuckets);
        Assert.Contains(nameof(AudioSessionMeterViewModel.WaveformPlotTick), changedProperties);
        Assert.Contains(nameof(AudioSessionMeterViewModel.SpectrumPlotTick), changedProperties);
    }

    [Fact]
    public void HandleMissingPreservesRollBuffersAndPlotTicks()
    {
        double[] waveform = [0.1, -0.2];
        double[] spectrum = [0.25, 0.75];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 4096));
        var waveformTick = row.WaveformPlotTick;
        var spectrumTick = row.SpectrumPlotTick;

        row.MarkAudioHandleMissing();

        Assert.Equal(waveform, row.WaveformSamples);
        Assert.Equal(spectrum, row.SpectrumBuckets);
        Assert.Equal(waveformTick, row.WaveformPlotTick);
        Assert.Equal(spectrumTick, row.SpectrumPlotTick);
        Assert.Equal("Source handle missing", row.SourceStatusText);
        Assert.Equal("handle n/a", row.PeakDbText);
    }

    [Fact]
    public void AliasEditCommandsCommitTrimmedAliasOrCancel()
    {
        var row = new AudioSessionMeterViewModel(Snapshot());

        row.StartAliasEditCommand.Execute(null);
        Assert.True(row.IsAliasEditing);
        Assert.Equal("Test App", row.AliasEditText);

        row.AliasEditText = "  Lead Bus  ";
        row.CommitAliasEditCommand.Execute(null);

        Assert.False(row.IsAliasEditing);
        Assert.Equal("Lead Bus", row.Alias);
        Assert.Equal("Lead Bus", row.DisplayTitle);

        row.StartAliasEditCommand.Execute(null);
        row.AliasEditText = "Will Cancel";
        row.CancelAliasEditCommand.Execute(null);

        Assert.False(row.IsAliasEditing);
        Assert.Equal("Lead Bus", row.Alias);
        Assert.Equal("Lead Bus", row.AliasEditText);
    }

    [Fact]
    public void EmptyAnalyzerWarmupFramesPreserveExistingRollBuffers()
    {
        double[] waveform = [0.1, -0.2];
        double[] spectrum = [0.25, 0.75];
        double[] nextWaveform = [0.3, -0.4];
        double[] nextSpectrum = [0.9, 0.1];
        var row = new AudioSessionMeterViewModel(Snapshot(
            waveformSamples: waveform,
            spectrumBuckets: spectrum,
            plotFrameVersion: 4096));
        var waveformTick = row.WaveformPlotTick;
        var spectrumTick = row.SpectrumPlotTick;

        row.Update(Snapshot(
            waveformSamples: null,
            spectrumBuckets: null,
            plotFrameVersion: 8192));

        Assert.Equal(waveform, row.WaveformSamples);
        Assert.Equal(spectrum, row.SpectrumBuckets);
        Assert.Equal(waveformTick, row.WaveformPlotTick);
        Assert.Equal(spectrumTick, row.SpectrumPlotTick);

        row.Update(Snapshot(
            waveformSamples: nextWaveform,
            spectrumBuckets: nextSpectrum,
            plotFrameVersion: 12288));

        Assert.Equal(nextWaveform, row.WaveformSamples);
        Assert.Equal(nextSpectrum, row.SpectrumBuckets);
        Assert.True(row.WaveformPlotTick > waveformTick);
        Assert.True(row.SpectrumPlotTick > spectrumTick);
    }

    private static AudioSessionSnapshot Snapshot(
        string peakDbText = "-12.0 dB",
        float peak = 0.25f,
        double? momentaryLufs = null,
        double? shortTermLufs = null,
        double? integratedLufs = null,
        double? truePeakDb = null,
        double? rms100MsDb = null,
        double? correlation = null,
        double? stereoImageX = 0.1,
        double? stereoImageY = -0.2,
        IReadOnlyList<StereoImagePoint>? stereoImagePoints = null,
        IReadOnlyList<double>? waveformSamples = null,
        IReadOnlyList<IReadOnlyList<double>>? channelWaveformSamples = null,
        IReadOnlyList<double>? spectrumBuckets = null,
        IReadOnlyList<IReadOnlyList<double>>? channelSpectrumBuckets = null,
        double peakDb = -12,
        long plotFrameVersion = 0)
    {
        return new AudioSessionSnapshot(
            "session-1",
            100,
            "Test App",
            peak,
            peakDb,
            peakDbText,
            "Active",
            momentaryLufs,
            shortTermLufs,
            integratedLufs,
            truePeakDb,
            rms100MsDb,
            StereoImageX: stereoImageX,
            StereoImageY: stereoImageY,
            StereoImagePoints: stereoImagePoints,
            Correlation: correlation,
            WaveformSamples: waveformSamples,
            ChannelWaveformSamples: channelWaveformSamples,
            SpectrumBuckets: spectrumBuckets,
            ChannelSpectrumBuckets: channelSpectrumBuckets,
            PlotFrameVersion: plotFrameVersion);
    }
}
