using System.Net;
using System.Net.Sockets;
using DesktopLoudnessMeter.App.Audio.Analysis;
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

    internal static bool RouteReceivedPacket(
        IPAddress remoteAddress,
        ReadOnlySpan<byte> bytes,
        Action<LocalLoopbackPacket> ingest,
        out string? ackHandle)
    {
        ackHandle = null;
        if (!IPAddress.IsLoopback(remoteAddress)
            || !NativeLoopbackProtocol.TryDecode(bytes, out var packet)
            || packet.Type == LocalLoopbackPacketType.Ack)
        {
            return false;
        }

        ingest(packet);
        ackHandle = packet.Handle;
        return true;
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
                if (RouteReceivedPacket(
                    received.RemoteEndPoint.Address,
                    received.Buffer,
                    _sourceService.Ingest,
                    out var ackHandle))
                {
                    await SendAckAsync(ackHandle!, received.RemoteEndPoint).ConfigureAwait(false);
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

    private async Task SendAckAsync(string handle, IPEndPoint remoteEndPoint)
    {
        try
        {
            var bytes = NativeLoopbackProtocol.EncodeAck(handle);
            await _udpClient.SendAsync(bytes, bytes.Length, remoteEndPoint).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
        {
        }
    }
}
