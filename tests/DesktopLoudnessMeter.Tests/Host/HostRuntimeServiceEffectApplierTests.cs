using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class HostRuntimeServiceEffectApplierTests
{
    [Fact]
    public void AppliesResetPlansWhenResetServiceIsAvailable()
    {
        var service = new RuntimeService();
        var applier = new HostRuntimeServiceEffectApplier(service);

        applier.ApplyResetAll(NativeHostRuntimeServiceCommandPlanner.PlanResetAll());
        applier.ApplyResetSource(NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            ["plugin:lead"],
            "plugin:lead",
            SignalSourceKind.Plugin,
            17));

        Assert.Equal(1, service.ResetAllSourcesCount);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, 17)], service.ResetRequests);
    }

    [Fact]
    public void AppliesAnalysisSettingsPlansWhenSettingsServiceIsAvailable()
    {
        var service = new RuntimeService();
        var applier = new HostRuntimeServiceEffectApplier(service);

        applier.ApplyPerformanceMode(NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(HostPerformanceMode.Eco));
        applier.ApplyTruePeakOversampling(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetTruePeakOversampling(TruePeakOversampling.EightX));
        applier.ApplyWaveformDisplayMode(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(WaveformDisplayMode.Channels));
        applier.ApplySpectrumDisplayMode(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(SpectrumDisplayMode.Channels));

        Assert.Equal([PcmAnalysisPerformanceMode.Eco], service.PerformanceModeRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, 8)], service.TruePeakOversamplingRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelWaveformRequests);
        Assert.Equal([("plugin:lead", SignalSourceKind.Plugin, null, true)], service.ChannelSpectrumRequests);
    }

    [Fact]
    public void NoOpsWhenOptionalInterfacesAreMissing()
    {
        var applier = new HostRuntimeServiceEffectApplier(new MeterOnlyService());

        applier.ApplyResetAll(NativeHostRuntimeServiceCommandPlanner.PlanResetAll());
        applier.ApplyResetSource(NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            ["plugin:lead"],
            "plugin:lead",
            SignalSourceKind.Plugin,
            null));
        applier.ApplyPerformanceMode(NativeHostRuntimeServiceCommandPlanner.PlanSetPerformanceMode(HostPerformanceMode.Eco));
        applier.ApplyTruePeakOversampling(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetTruePeakOversampling(TruePeakOversampling.EightX));
        applier.ApplyWaveformDisplayMode(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetWaveformDisplayMode(WaveformDisplayMode.Channels));
        applier.ApplySpectrumDisplayMode(
            "plugin:lead",
            SignalSourceKind.Plugin,
            null,
            NativeHostChartSettingsCommandPlanner.PlanSetSpectrumDisplayMode(SpectrumDisplayMode.Channels));
    }

    [Fact]
    public void DoesNotInvokeForUnsuccessfulPlans()
    {
        var service = new RuntimeService();
        var applier = new HostRuntimeServiceEffectApplier(service);

        applier.ApplyResetSource(NativeHostRuntimeServiceCommandPlanner.PlanResetSource(
            ["plugin:lead"],
            "plugin:missing",
            SignalSourceKind.Plugin,
            null));

        Assert.Empty(service.ResetRequests);
    }

    private sealed class MeterOnlyService : ISignalSourceMeterService
    {
        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return [];
        }
    }

    private sealed class RuntimeService :
        ISignalSourceMeterService,
        ISignalSourceResetService,
        ISignalSourceAnalysisSettingsService
    {
        public int ResetAllSourcesCount { get; private set; }

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId)> ResetRequests { get; } = [];

        public List<PcmAnalysisPerformanceMode> PerformanceModeRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, int Oversampling)> TruePeakOversamplingRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelWaveformRequests { get; } = [];

        public List<(string SourceKey, SignalSourceKind SourceKind, int? ProcessId, bool Enabled)> ChannelSpectrumRequests { get; } = [];

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return [];
        }

        public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
        {
            ResetRequests.Add((sourceKey, sourceKind, processId));
        }

        public void ResetAllSources()
        {
            ResetAllSourcesCount++;
        }

        public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
        {
            PerformanceModeRequests.Add(mode);
        }

        public void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling)
        {
            TruePeakOversamplingRequests.Add((sourceKey, sourceKind, processId, oversampling));
        }

        public void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelWaveformRequests.Add((sourceKey, sourceKind, processId, enabled));
        }

        public void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
        {
            ChannelSpectrumRequests.Add((sourceKey, sourceKind, processId, enabled));
        }
    }
}
