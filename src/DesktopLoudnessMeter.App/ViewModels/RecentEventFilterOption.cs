namespace DesktopLoudnessMeter.App.ViewModels;

using DesktopLoudnessMeter.App.Localization;

public sealed record RecentEventFilterOption
{
    private readonly string _sourceDisplayName;

    public RecentEventFilterOption(RecentEventKind? kind, string displayName)
    {
        Kind = kind;
        _sourceDisplayName = displayName;
    }

    public RecentEventKind? Kind { get; }

    public string DisplayName => LocalizationManager.Translate(_sourceDisplayName);

    public bool IsAll => Kind is null;
}
