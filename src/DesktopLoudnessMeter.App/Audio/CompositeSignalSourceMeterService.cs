namespace DesktopLoudnessMeter.App.Audio;

using DesktopLoudnessMeter.App.Audio.Analysis;

public sealed class CompositeSignalSourceMeterService :
    ISignalSourceMeterService,
    ISignalSourceResetService,
    ISignalSourceAnalysisSettingsService,
    IDisposable
{
    private readonly IReadOnlyList<ISignalSourceMeterService> _services;

    public CompositeSignalSourceMeterService(params ISignalSourceMeterService[] services)
    {
        _services = services;
    }

    public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
    {
        return _services
            .SelectMany(service => service.GetCurrentSources())
            .ToArray();
    }

    public void ResetSource(string sourceKey, SignalSourceKind sourceKind, int? processId)
    {
        foreach (var service in _services.OfType<ISignalSourceResetService>())
        {
            service.ResetSource(sourceKey, sourceKind, processId);
        }
    }

    public void ResetAllSources()
    {
        foreach (var service in _services.OfType<ISignalSourceResetService>())
        {
            service.ResetAllSources();
        }
    }

    public void SetTruePeakOversampling(string sourceKey, SignalSourceKind sourceKind, int? processId, int oversampling)
    {
        foreach (var service in _services.OfType<ISignalSourceAnalysisSettingsService>())
        {
            service.SetTruePeakOversampling(sourceKey, sourceKind, processId, oversampling);
        }
    }

    public void SetChannelWaveformEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
    {
        foreach (var service in _services.OfType<ISignalSourceAnalysisSettingsService>())
        {
            service.SetChannelWaveformEnabled(sourceKey, sourceKind, processId, enabled);
        }
    }

    public void SetChannelSpectrumEnabled(string sourceKey, SignalSourceKind sourceKind, int? processId, bool enabled)
    {
        foreach (var service in _services.OfType<ISignalSourceAnalysisSettingsService>())
        {
            service.SetChannelSpectrumEnabled(sourceKey, sourceKind, processId, enabled);
        }
    }

    public void SetPerformanceMode(PcmAnalysisPerformanceMode mode)
    {
        foreach (var service in _services.OfType<ISignalSourceAnalysisSettingsService>())
        {
            service.SetPerformanceMode(mode);
        }
    }

    public void Dispose()
    {
        foreach (var service in _services.OfType<IDisposable>())
        {
            service.Dispose();
        }
    }
}
