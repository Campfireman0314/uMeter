using System.Net;
using System.Net.Sockets;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.App.Audio;

public sealed class LocalLoopbackUdpReceiver : IDisposable
{
    public const int DefaultPort = 38173;
    private readonly LocalLoopbackSignalSourceService _sourceService;
    private readonly UdpClient _udpClient;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _receiveTask;
    private bool _isDisposed;

    private LocalLoopbackUdpReceiver(LocalLoopbackSignalSourceService sourceService, UdpClient udpClient)
    {
        _sourceService = sourceService;
        _udpClient = udpClient;
        LocalEndPoint = (IPEndPoint)(_udpClient.Client.LocalEndPoint
            ?? throw new InvalidOperationException("UDP receiver did not bind a local endpoint."));
        Port = LocalEndPoint.Port;
        _receiveTask = Task.Run(ReceiveLoopAsync);
    }

    public int Port { get; }

    public IPEndPoint LocalEndPoint { get; }

    public static LocalLoopbackUdpReceiver Start(LocalLoopbackSignalSourceService sourceService, int port = DefaultPort)
    {
        var udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        return new LocalLoopbackUdpReceiver(sourceService, udpClient);
    }

    public static bool TryStart(
        LocalLoopbackSignalSourceService sourceService,
        out LocalLoopbackUdpReceiver? receiver,
        int port = DefaultPort)
    {
        receiver = null;
        try
        {
            receiver = Start(sourceService, port);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppLogger.Error($"Local loopback UDP receiver failed to bind 127.0.0.1:{port}.", ex);
            return false;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _stop.Cancel();
        _udpClient.Close();
        try
        {
            _receiveTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(IsExpectedShutdownException))
        {
        }
        finally
        {
            _udpClient.Dispose();
            _stop.Dispose();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var received = await _udpClient.ReceiveAsync(_stop.Token).ConfigureAwait(false);
                if (!IPAddress.IsLoopback(received.RemoteEndPoint.Address))
                {
                    continue;
                }

                if (LocalLoopbackProtocol.TryDecode(received.Buffer, out var packet))
                {
                    _sourceService.Ingest(packet);
                }
            }
        }
        catch (Exception ex) when (IsExpectedShutdownException(ex))
        {
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
        {
            AppLogger.Error("Local loopback UDP receiver stopped after a socket failure.", ex);
        }
    }

    private static bool IsExpectedShutdownException(Exception ex)
    {
        return ex is OperationCanceledException or ObjectDisposedException or SocketException;
    }
}
