namespace DesktopLoudnessMeter.App.ViewModels;

public sealed record RecentEventViewModel(
    string TimeText,
    RecentEventKind Kind,
    string Title,
    string Detail,
    ReadoutSeverity Severity);
