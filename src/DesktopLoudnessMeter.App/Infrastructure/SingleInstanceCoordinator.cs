using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace DesktopLoudnessMeter.App.Infrastructure;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly ISingleInstanceLock _instanceLock;
    private readonly IActivationSignalServer _activationServer;
    private readonly IActivationSignalClient _activationClient;
    private bool _isDisposed;
    private bool _isStarted;

    public SingleInstanceCoordinator(
        ISingleInstanceLock instanceLock,
        IActivationSignalServer activationServer,
        IActivationSignalClient activationClient)
    {
        _instanceLock = instanceLock;
        _activationServer = activationServer;
        _activationClient = activationClient;
    }

    public event Action? ActivationRequested;

    public static SingleInstanceCoordinator CreateDefault()
    {
        var names = SingleInstanceNames.ForCurrentUserSession();
        return new SingleInstanceCoordinator(
            new NamedMutexSingleInstanceLock(names.MutexName),
            new NamedPipeActivationSignalServer(names.PipeName),
            new NamedPipeActivationSignalClient(names.PipeName));
    }

    public SingleInstanceStartupResult Start(TimeSpan activationRequestTimeout)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_isStarted)
        {
            throw new InvalidOperationException("Single-instance coordination has already started.");
        }

        _isStarted = true;
        if (!_instanceLock.TryAcquire())
        {
            var activationRequestSent = _activationClient.TryRequestActivation(activationRequestTimeout);
            return new SingleInstanceStartupResult(IsPrimary: false, activationRequestSent);
        }

        _activationServer.ActivationRequested += OnActivationRequested;
        _activationServer.Start();
        return new SingleInstanceStartupResult(IsPrimary: true, ActivationRequestSent: false);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _activationServer.ActivationRequested -= OnActivationRequested;
        _activationServer.Dispose();
        _instanceLock.Dispose();
    }

    private void OnActivationRequested()
    {
        ActivationRequested?.Invoke();
    }

    private readonly record struct SingleInstanceNames(string MutexName, string PipeName)
    {
        public static SingleInstanceNames ForCurrentUserSession()
        {
            var identityPart = SanitizeNamePart(GetCurrentUserIdentityPart());
            var sessionId = Process.GetCurrentProcess().SessionId;

            // Local\ scopes the mutex to the current interactive logon session.
            // The SID and session id keep both coordination names user/session-specific.
            var suffix = $"{identityPart}.{sessionId}";
            return new SingleInstanceNames(
                $"Local\\CampfiremanStudio.uMeter.SingleInstance.{suffix}",
                $"CampfiremanStudio.uMeter.Activation.{suffix}");
        }

        private static string GetCurrentUserIdentityPart()
        {
            try
            {
                return WindowsIdentity.GetCurrent().User?.Value
                    ?? Environment.UserName
                    ?? "unknown";
            }
            catch (SystemException)
            {
                return string.IsNullOrWhiteSpace(Environment.UserName)
                    ? "unknown"
                    : Environment.UserName;
            }
        }

        private static string SanitizeNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_'
                    ? character
                    : '_');
            }

            return builder.Length == 0 ? "unknown" : builder.ToString();
        }
    }

    private sealed class NamedMutexSingleInstanceLock : ISingleInstanceLock
    {
        private readonly Mutex _mutex;
        private bool _isAcquired;

        public NamedMutexSingleInstanceLock(string mutexName)
        {
            _mutex = new Mutex(initiallyOwned: false, mutexName);
        }

        public bool TryAcquire()
        {
            if (_isAcquired)
            {
                return true;
            }

            try
            {
                _isAcquired = _mutex.WaitOne(millisecondsTimeout: 0);
            }
            catch (AbandonedMutexException)
            {
                _isAcquired = true;
            }

            return _isAcquired;
        }

        public void Dispose()
        {
            if (_isAcquired)
            {
                _mutex.ReleaseMutex();
                _isAcquired = false;
            }

            _mutex.Dispose();
        }
    }

    private sealed class NamedPipeActivationSignalServer : IActivationSignalServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellation = new();
        private Task? _listenTask;
        private bool _isStarted;

        public NamedPipeActivationSignalServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public event Action? ActivationRequested;

        public void Start()
        {
            if (_isStarted)
            {
                throw new InvalidOperationException("Activation signal listener has already started.");
            }

            _isStarted = true;
            _listenTask = Task.Run(ListenAsync);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            try
            {
                _listenTask?.Wait(TimeSpan.FromMilliseconds(500));
            }
            catch (AggregateException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            _cancellation.Dispose();
        }

        private async Task ListenAsync()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = CreatePipeServer();
                    await pipe.WaitForConnectionAsync(_cancellation.Token).ConfigureAwait(false);

                    var buffer = new byte[1];
                    _ = await pipe.ReadAsync(buffer, _cancellation.Token).ConfigureAwait(false);
                    ActivationRequested?.Invoke();
                }
                catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_cancellation.IsCancellationRequested)
                {
                    break;
                }
                catch (IOException) when (_cancellation.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), _cancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        private NamedPipeServerStream CreatePipeServer()
        {
            return new NamedPipeServerStream(
                _pipeName,
                PipeDirection.In,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        }
    }

    private sealed class NamedPipeActivationSignalClient : IActivationSignalClient
    {
        private readonly string _pipeName;

        public NamedPipeActivationSignalClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public bool TryRequestActivation(TimeSpan timeout)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.CurrentUserOnly);
                pipe.Connect(ToTimeoutMilliseconds(timeout));
                pipe.WriteByte(1);
                pipe.Flush();
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                return 0;
            }

            return timeout.TotalMilliseconds >= int.MaxValue
                ? int.MaxValue
                : Math.Max(1, (int)Math.Ceiling(timeout.TotalMilliseconds));
        }
    }
}

internal readonly record struct SingleInstanceStartupResult(bool IsPrimary, bool ActivationRequestSent);

internal interface ISingleInstanceLock : IDisposable
{
    bool TryAcquire();
}

internal interface IActivationSignalServer : IDisposable
{
    event Action? ActivationRequested;

    void Start();
}

internal interface IActivationSignalClient
{
    bool TryRequestActivation(TimeSpan timeout);
}
