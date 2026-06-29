using DesktopLoudnessMeter.App.Infrastructure;

namespace DesktopLoudnessMeter.Tests.Infrastructure;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void StartReturnsPrimaryAndStartsServerWhenLockIsAcquired()
    {
        var instanceLock = new FakeSingleInstanceLock(canAcquire: true);
        var server = new FakeActivationSignalServer();
        var client = new FakeActivationSignalClient();
        using var coordinator = new SingleInstanceCoordinator(instanceLock, server, client);

        var result = coordinator.Start(TimeSpan.FromMilliseconds(25));

        Assert.True(result.IsPrimary);
        Assert.False(result.ActivationRequestSent);
        Assert.True(instanceLock.TryAcquireCalled);
        Assert.True(server.StartCalled);
        Assert.False(client.TryRequestActivationCalled);
    }

    [Fact]
    public void StartRequestsActivationAndDoesNotStartServerWhenLockIsRejected()
    {
        var instanceLock = new FakeSingleInstanceLock(canAcquire: false);
        var server = new FakeActivationSignalServer();
        var client = new FakeActivationSignalClient(requestResult: true);
        using var coordinator = new SingleInstanceCoordinator(instanceLock, server, client);
        var timeout = TimeSpan.FromMilliseconds(25);

        var result = coordinator.Start(timeout);

        Assert.False(result.IsPrimary);
        Assert.True(result.ActivationRequestSent);
        Assert.True(instanceLock.TryAcquireCalled);
        Assert.False(server.StartCalled);
        Assert.True(client.TryRequestActivationCalled);
        Assert.Equal(timeout, client.RequestedTimeout);
    }

    [Fact]
    public void ActivationServerSignalRaisesCoordinatorEventForPrimary()
    {
        var server = new FakeActivationSignalServer();
        using var coordinator = new SingleInstanceCoordinator(
            new FakeSingleInstanceLock(canAcquire: true),
            server,
            new FakeActivationSignalClient());
        var requestCount = 0;
        coordinator.ActivationRequested += () => requestCount++;

        coordinator.Start(TimeSpan.FromMilliseconds(25));
        server.RaiseActivationRequested();

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public void DisposeReleasesServerAndLock()
    {
        var instanceLock = new FakeSingleInstanceLock(canAcquire: true);
        var server = new FakeActivationSignalServer();
        var coordinator = new SingleInstanceCoordinator(
            instanceLock,
            server,
            new FakeActivationSignalClient());

        coordinator.Start(TimeSpan.FromMilliseconds(25));
        coordinator.Dispose();

        Assert.True(server.DisposeCalled);
        Assert.True(instanceLock.DisposeCalled);
    }

    [Fact]
    public void DisposeStopsForwardingActivationEvents()
    {
        var server = new FakeActivationSignalServer();
        var coordinator = new SingleInstanceCoordinator(
            new FakeSingleInstanceLock(canAcquire: true),
            server,
            new FakeActivationSignalClient());
        var requestCount = 0;
        coordinator.ActivationRequested += () => requestCount++;

        coordinator.Start(TimeSpan.FromMilliseconds(25));
        coordinator.Dispose();
        server.RaiseActivationRequested();

        Assert.Equal(0, requestCount);
    }

    private sealed class FakeSingleInstanceLock : ISingleInstanceLock
    {
        private readonly bool _canAcquire;

        public FakeSingleInstanceLock(bool canAcquire)
        {
            _canAcquire = canAcquire;
        }

        public bool TryAcquireCalled { get; private set; }

        public bool DisposeCalled { get; private set; }

        public bool TryAcquire()
        {
            TryAcquireCalled = true;
            return _canAcquire;
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }

    private sealed class FakeActivationSignalServer : IActivationSignalServer
    {
        public event Action? ActivationRequested;

        public bool StartCalled { get; private set; }

        public bool DisposeCalled { get; private set; }

        public void Start()
        {
            StartCalled = true;
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }

        public void RaiseActivationRequested()
        {
            ActivationRequested?.Invoke();
        }
    }

    private sealed class FakeActivationSignalClient : IActivationSignalClient
    {
        private readonly bool _requestResult;

        public FakeActivationSignalClient(bool requestResult = false)
        {
            _requestResult = requestResult;
        }

        public bool TryRequestActivationCalled { get; private set; }

        public TimeSpan RequestedTimeout { get; private set; }

        public bool TryRequestActivation(TimeSpan timeout)
        {
            TryRequestActivationCalled = true;
            RequestedTimeout = timeout;
            return _requestResult;
        }
    }
}
