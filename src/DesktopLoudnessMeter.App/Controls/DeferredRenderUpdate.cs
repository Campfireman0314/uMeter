using System.Windows;
using System.Windows.Threading;

namespace DesktopLoudnessMeter.App.Controls;

internal sealed class DeferredRenderUpdate
{
    private readonly FrameworkElement _owner;
    private readonly Func<bool> _canApply;
    private readonly Action _apply;
    private readonly DispatcherPriority _priority;
    private bool _isQueued;

    public DeferredRenderUpdate(
        FrameworkElement owner,
        Func<bool> canApply,
        Action apply,
        DispatcherPriority priority = DispatcherPriority.Render)
    {
        _owner = owner;
        _canApply = canApply;
        _apply = apply;
        _priority = priority;
    }

    public bool HasPending { get; private set; }

    public void Request()
    {
        HasPending = true;
        Queue();
    }

    public bool TryApply()
    {
        if (!HasPending || !_canApply())
        {
            return false;
        }

        HasPending = false;
        _apply();
        return true;
    }

    private void Queue()
    {
        if (_isQueued
            || _owner.Dispatcher.HasShutdownStarted
            || _owner.Dispatcher.HasShutdownFinished)
        {
            return;
        }

        _isQueued = true;
        _owner.Dispatcher.BeginInvoke(
            (Action)(() =>
            {
                _isQueued = false;
                TryApply();
            }),
            _priority);
    }
}
