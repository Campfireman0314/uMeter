using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Audio.Analysis;

namespace DesktopLoudnessMeter.App.Host;

internal sealed class HostRuntimeServiceEffectApplier
{
    private readonly ISignalSourceMeterService _sourceMeterService;

    public HostRuntimeServiceEffectApplier(ISignalSourceMeterService sourceMeterService)
    {
        _sourceMeterService = sourceMeterService;
    }

    public void ApplyResetAll(NativeHostRuntimeServiceCommandPlan plan)
    {
        if (plan.ShouldResetAll && _sourceMeterService is ISignalSourceResetService resetService)
        {
            resetService.ResetAllSources();
        }
    }

    public void ApplyResetSource(NativeHostRuntimeServiceCommandPlan plan)
    {
        if (plan.Success &&
            plan.ShouldResetSource &&
            plan.SourceKey is not null &&
            _sourceMeterService is ISignalSourceResetService resetService)
        {
            resetService.ResetSource(plan.SourceKey, plan.SourceKind, plan.ProcessId);
        }
    }

    public void ApplyPerformanceMode(NativeHostRuntimeServiceCommandPlan plan)
    {
        if (plan.Success && _sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetPerformanceMode(plan.AnalysisMode);
        }
    }

    public void ApplyTruePeakOversampling(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        NativeTruePeakOversamplingPlan plan)
    {
        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetTruePeakOversampling(sourceKey, sourceKind, processId, plan.ServiceOversampling);
        }
    }

    public void ApplyWaveformDisplayMode(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        NativeWaveformDisplayModePlan plan)
    {
        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetChannelWaveformEnabled(sourceKey, sourceKind, processId, plan.ChannelEnabled);
        }
    }

    public void ApplySpectrumDisplayMode(
        string sourceKey,
        SignalSourceKind sourceKind,
        int? processId,
        NativeSpectrumDisplayModePlan plan)
    {
        if (_sourceMeterService is ISignalSourceAnalysisSettingsService settingsService)
        {
            settingsService.SetChannelSpectrumEnabled(sourceKey, sourceKind, processId, plan.ChannelEnabled);
        }
    }
}
