using DesktopLoudnessMeter.App.Infrastructure;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class TrayExitCoordinatorTests
{
    [Fact]
    public void RequestExitPostsShutdownWithoutRunningSynchronously()
    {
        var events = new List<string>();
        Action? postedAction = null;
        var coordinator = new TrayExitCoordinator(
            action =>
            {
                events.Add("post");
                postedAction = action;
            },
            () => events.Add("shutdown"));

        coordinator.RequestExit();

        Assert.Equal(["post"], events);

        postedAction?.Invoke();

        Assert.Equal(["post", "shutdown"], events);
    }

    [Fact]
    public void RequestExitIsIdempotent()
    {
        var postCount = 0;
        var shutdownCount = 0;
        var coordinator = new TrayExitCoordinator(
            action =>
            {
                postCount++;
                action();
            },
            () => shutdownCount++);

        coordinator.RequestExit();
        coordinator.RequestExit();

        Assert.Equal(1, postCount);
        Assert.Equal(1, shutdownCount);
    }
}
