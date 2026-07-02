namespace DesktopLoudnessMeter.App.Host;

public sealed record HostCommandResult(
    HostCommand Command,
    bool Success,
    HostSnapshot Snapshot,
    HostCommandError? Error)
{
    public static HostCommandResult Ok(HostCommand command, HostSnapshot snapshot)
    {
        return new HostCommandResult(command, Success: true, snapshot, Error: null);
    }

    public static HostCommandResult Fail(HostCommand command, HostSnapshot snapshot, HostCommandError error)
    {
        return new HostCommandResult(command, Success: false, snapshot, error);
    }
}

public sealed record HostCommandError(
    HostCommandErrorCode Code,
    string Message,
    string? TargetSourceKey = null,
    string? Detail = null)
{
    public static HostCommandError NotFound(string sourceKey, string message)
    {
        return new HostCommandError(HostCommandErrorCode.SourceNotFound, message, sourceKey);
    }

    public static HostCommandError Invalid(string message, string? targetSourceKey = null)
    {
        return new HostCommandError(HostCommandErrorCode.InvalidCommand, message, targetSourceKey);
    }

    public static HostCommandError Failed(string message, string? targetSourceKey = null, string? detail = null)
    {
        return new HostCommandError(HostCommandErrorCode.ServiceFailure, message, targetSourceKey, detail);
    }
}

public enum HostCommandErrorCode
{
    InvalidCommand,
    SourceNotFound,
    CommandUnavailable,
    ServiceFailure
}
