namespace DesktopLoudnessMeter.App.ViewModels;

using DesktopLoudnessMeter.App.Localization;

public sealed record FrequencyCollisionSourceOption
{
    public FrequencyCollisionSourceOption(string sourceKey, string displayName, bool isAllSources = false)
    {
        SourceKey = sourceKey;
        SourceDisplayName = displayName;
        IsAllSources = isAllSources;
    }

    public string SourceKey { get; }

    private string SourceDisplayName { get; }

    public string DisplayName => LocalizationManager.Translate(SourceDisplayName);

    public bool IsAllSources { get; }

    public override string ToString()
    {
        return DisplayName;
    }
}
