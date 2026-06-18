using System.Net;
using System.Net.Sockets;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.App.Audio;

public sealed class LocalLoopbackDiagnosticSender : IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly IPEndPoint _endpoint;

    public LocalLoopbackDiagnosticSender(int port = LocalLoopbackUdpReceiver.DefaultPort)
        : this(IPAddress.Loopback, port)
    {
    }

    public LocalLoopbackDiagnosticSender(IPAddress address, int port)
    {
        _endpoint = new IPEndPoint(address, port);
    }

    public Task RegisterAsync(
        string handle,
        string displayName,
        SignalSourceKind sourceKind,
        int sampleRate,
        int channels)
    {
        return SendAsync(LocalLoopbackProtocol.EncodeRegister(
            handle,
            displayName,
            ToProtocolSourceKind(sourceKind),
            sampleRate,
            channels));
    }

    public Task HeartbeatAsync(string handle)
    {
        return SendAsync(LocalLoopbackProtocol.EncodeHeartbeat(handle));
    }

    public Task RenameAsync(string handle, string displayName)
    {
        return SendAsync(LocalLoopbackProtocol.EncodeRename(handle, displayName));
    }

    public Task SendAudioAsync(string handle, long sequence, IReadOnlyList<float> samples)
    {
        return SendAsync(LocalLoopbackProtocol.EncodeAudio(handle, sequence, samples));
    }

    public Task CloseAsync(string handle)
    {
        return SendAsync(LocalLoopbackProtocol.EncodeClose(handle));
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }

    private async Task SendAsync(byte[] bytes)
    {
        await _udpClient.SendAsync(bytes, bytes.Length, _endpoint).ConfigureAwait(false);
    }

    private static LocalLoopbackSourceKind ToProtocolSourceKind(SignalSourceKind sourceKind)
    {
        return sourceKind == SignalSourceKind.Plugin
            ? LocalLoopbackSourceKind.Plugin
            : LocalLoopbackSourceKind.Loopback;
    }
}
