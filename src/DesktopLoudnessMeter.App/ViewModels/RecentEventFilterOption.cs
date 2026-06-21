namespace DesktopLoudnessMeter.App.ViewModels;

public sealed record RecentEventFilterOption(
    RecentEventKind? Kind,
    string DisplayName)
{
    public bool IsAll => Kind is null;
}
