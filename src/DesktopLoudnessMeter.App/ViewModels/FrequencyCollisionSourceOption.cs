namespace DesktopLoudnessMeter.App.ViewModels;

public sealed record FrequencyCollisionSourceOption
{
    public FrequencyCollisionSourceOption(string sourceKey, string displayName, bool isAllSources = false)
    {
        SourceKey = sourceKey;
        DisplayName = displayName;
        IsAllSources = isAllSources;
    }

    public string SourceKey { get; }

    public string DisplayName { get; }

    public bool IsAllSources { get; }

    public override string ToString()
    {
        return DisplayName;
    }
}
