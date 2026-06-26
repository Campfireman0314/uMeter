namespace DesktopLoudnessMeter.App.Infrastructure;

internal sealed class TrayExitCoordinator
{
    private readonly Action<Action> _postShutdown;
    private readonly Action _shutdown;
    private bool _isExitRequested;

    public TrayExitCoordinator(Action<Action> postShutdown, Action shutdown)
    {
        _postShutdown = postShutdown;
        _shutdown = shutdown;
    }

    public void RequestExit()
    {
        if (_isExitRequested)
        {
            return;
        }

        _isExitRequested = true;
        _postShutdown(_shutdown);
    }
}
