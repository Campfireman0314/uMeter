using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Host;

internal sealed class HostWorkspaceCommandEffectApplier
{
    private readonly UserWorkspaceSettings _workspaceSettings;

    public HostWorkspaceCommandEffectApplier(UserWorkspaceSettings workspaceSettings)
    {
        _workspaceSettings = workspaceSettings;
    }

    public void ApplyTheme(NativeHostGlobalSettingsCommandPlan plan)
    {
        _workspaceSettings.ThemeId = plan.ThemeId;
        _workspaceSettings.Save();
    }

    public void ApplyRecentEventLimit(NativeHostGlobalSettingsCommandPlan plan)
    {
        _workspaceSettings.MaxRecentEventEntries = plan.MaxRecentEventEntries;
        _workspaceSettings.Save();
    }

    public void ApplyEventOutputFolder(NativeHostGlobalSettingsCommandPlan plan)
    {
        _workspaceSettings.EventOutputFolder = plan.EventOutputFolder;
        _workspaceSettings.Save();
    }

    public void ApplySourceAlias(string sourceKey, NativeHostSourcePreferenceCommandPlan plan)
    {
        _workspaceSettings.GetOrCreateSource(sourceKey).Alias = plan.Alias;
        _workspaceSettings.Save();
    }

    public void ApplySourceHidden(string sourceKey, NativeHostSourcePreferenceCommandPlan plan)
    {
        _workspaceSettings.GetOrCreateSource(sourceKey).IsHidden = plan.IsHidden;
        _workspaceSettings.Save();
    }

    public void ApplySourceOrder(NativeHostSourceOrderCommandPlan plan)
    {
        foreach (var row in plan.Rows)
        {
            _workspaceSettings.GetOrCreateSource(row.SourceKey).OrderIndex = row.OrderIndex;
        }

        _workspaceSettings.Save();
    }

    public void ApplyForgetSource(string sourceKey, NativeHostSourcePreferenceCommandPlan plan)
    {
        if (plan.ShouldForget)
        {
            _workspaceSettings.ForgetSource(sourceKey);
        }

        _workspaceSettings.Save();
    }

    public void ApplyTruePeakOversampling(string sourceKey, NativeTruePeakOversamplingPlan plan)
    {
        _workspaceSettings.GetOrCreateSource(sourceKey).TruePeakOversampling = plan.Oversampling;
        _workspaceSettings.Save();
    }

    public void ApplyWaveformDisplayMode(string sourceKey, NativeWaveformDisplayModePlan plan)
    {
        _workspaceSettings.GetOrCreateSource(sourceKey).WaveformDisplayMode = plan.DisplayMode;
        _workspaceSettings.Save();
    }

    public void ApplySpectrumDisplayMode(string sourceKey, NativeSpectrumDisplayModePlan plan)
    {
        _workspaceSettings.GetOrCreateSource(sourceKey).SpectrumDisplayMode = plan.DisplayMode;
        _workspaceSettings.Save();
    }

    public void ApplyDetailModuleVisibility(string sourceKey, NativeDetailModuleVisibilityPlan plan)
    {
        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        switch (plan.ModuleKind)
        {
            case DetailModuleKind.Stereo:
                sourceSettings.InlineStereoVisible = plan.IsVisible;
                break;
            case DetailModuleKind.Collision:
                sourceSettings.InlineCollisionVisible = plan.IsVisible;
                break;
            case DetailModuleKind.LoudnessClock:
                sourceSettings.InlineLoudnessClockVisible = plan.IsVisible;
                break;
            case DetailModuleKind.Waveform:
                sourceSettings.InlineWaveformVisible = plan.IsVisible;
                break;
            case DetailModuleKind.Spectrum:
                sourceSettings.InlineSpectrumVisible = plan.IsVisible;
                break;
        }

        _workspaceSettings.Save();
    }

    public void ApplyDetailModuleHeight(string sourceKey, DetailModuleKind moduleKind, double height)
    {
        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        var modules = DetailModuleLayoutNormalizer.Normalize(sourceSettings.InlineDetailModules);
        var normalizedHeight = DetailModuleLayoutNormalizer.NormalizeHeight(moduleKind, height);
        foreach (var module in modules)
        {
            if (module.Kind == moduleKind)
            {
                module.Height = normalizedHeight;
                break;
            }
        }

        sourceSettings.InlineDetailModules = modules;
        _workspaceSettings.Save();
    }

    public void ApplyDetailModuleOrder(
        string sourceKey,
        DetailModuleKind moduleKind,
        DetailModuleKind targetModuleKind,
        bool insertAfter)
    {
        if (moduleKind == targetModuleKind)
        {
            return;
        }

        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        var modules = DetailModuleLayoutNormalizer.Normalize(sourceSettings.InlineDetailModules);
        var source = modules.FirstOrDefault(module => module.Kind == moduleKind);
        if (source is null)
        {
            return;
        }

        modules.Remove(source);
        var targetIndex = modules.FindIndex(module => module.Kind == targetModuleKind);
        if (targetIndex < 0)
        {
            return;
        }

        modules.Insert(targetIndex + (insertAfter ? 1 : 0), source);
        for (var index = 0; index < modules.Count; index++)
        {
            modules[index].OrderIndex = index;
        }

        sourceSettings.InlineDetailModules = modules;
        _workspaceSettings.Save();
    }

    public void ApplySpectrumDisplayRange(string sourceKey, NativeSpectrumDisplayRangePlan plan)
    {
        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        sourceSettings.SpectrumDisplayFloorDb = plan.FloorDb;
        sourceSettings.SpectrumDisplayCeilingDb = plan.CeilingDb;
        _workspaceSettings.Save();
    }

    public void ApplyLoudnessClockSettings(string sourceKey, NativeLoudnessClockSettingsPlan plan)
    {
        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        sourceSettings.LoudnessClockMinimumDb = plan.MinimumDb;
        sourceSettings.LoudnessClockMaximumDb = plan.MaximumDb;
        sourceSettings.LoudnessClockWarningDb = plan.WarningDb;
        sourceSettings.LoudnessClockCycleSeconds = plan.CycleSeconds;
        _workspaceSettings.Save();
    }

    public void ApplyCollisionSettings(string sourceKey, NativeCollisionSettingsPlan plan)
    {
        var sourceSettings = _workspaceSettings.GetOrCreateSource(sourceKey);
        sourceSettings.CollisionLevelFloorDb = plan.LevelFloorDb;
        sourceSettings.CollisionProminenceThresholdDb = plan.ProminenceThresholdDb;
        sourceSettings.CollisionRelativeThreshold = plan.RelativeThreshold;
        sourceSettings.CollisionFrequencyScale = plan.FrequencyScale;
        sourceSettings.InlineCollisionSourceKey = plan.ComparisonSourceKey;
        _workspaceSettings.Save();
    }
}
