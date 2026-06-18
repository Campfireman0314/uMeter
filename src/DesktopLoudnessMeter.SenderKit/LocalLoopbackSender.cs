using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.SenderKit;

public sealed class LocalLoopbackSender : IAsyncDisposable, IDisposable
{
    private readonly LoopbackSenderOptions _options;
    private readonly IPEndPoint _endpoint;
    private readonly UdpClient _udpClient = new();
    private readonly ConcurrentQueue<PooledAudioPacket> _availablePackets = new();
    private readonly Channel<PooledAudioPacket> _audioPackets;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _lifetimeLock = new();
    private Task? _audioSendTask;
    private Task? _heartbeatTask;
    private bool _isStarted;
    private bool _isDisposed;
    private long _sequence;
    private long _droppedAudioPacketCount;

    public LocalLoopbackSender(LoopbackSenderOptions options)
    {
        _options = options;
        _options.ValidateRuntimeOptions();
        _endpoint = new IPEndPoint(options.Address, options.Port);
        _audioPackets = Channel.CreateUnbounded<PooledAudioPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        for (var index = 0; index < options.BufferedPacketCapacity; index++)
        {
            _availablePackets.Enqueue(new PooledAudioPacket(options.MaxSamplesPerPacket));
        }
    }

    public long DroppedAudioPacketCount => Interlocked.Read(ref _droppedAudioPacketCount);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        lock (_lifetimeLock)
        {
            if (_isStarted)
            {
                return;
            }

            _isStarted = true;
        }

        await SendAsync(LocalLoopbackProtocol.EncodeRegister(
            _options.Handle,
            _options.DisplayName,
            _options.SourceKind,
            _options.SampleRate,
            _options.Channels), cancellationToken).ConfigureAwait(false);

        _audioSendTask = Task.Run(() => AudioSendLoopAsync(_stop.Token), CancellationToken.None);
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_stop.Token), CancellationToken.None);
    }

    public async Task RenameAsync(string displayName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name must be non-empty.", nameof(displayName));
        }

        _options.DisplayName = displayName.Trim();
        await SendAsync(LocalLoopbackProtocol.EncodeRename(_options.Handle, _options.DisplayName), cancellationToken).ConfigureAwait(false);
    }

    public bool TryEnqueueAudio(ReadOnlySpan<float> samples)
    {
        if (_isDisposed
            || samples.Length == 0
            || samples.Length > _options.MaxSamplesPerPacket
            || samples.Length % _options.Channels != 0)
        {
            Interlocked.Increment(ref _droppedAudioPacketCount);
            return false;
        }

        if (!_availablePackets.TryDequeue(out var packet))
        {
            Interlocked.Increment(ref _droppedAudioPacketCount);
            return false;
        }

        samples.CopyTo(packet.Buffer);
        packet.Count = samples.Length;
        packet.Sequence = Interlocked.Increment(ref _sequence);

        if (_audioPackets.Writer.TryWrite(packet))
        {
            return true;
        }

        packet.Reset();
        _availablePackets.Enqueue(packet);
        Interlocked.Increment(ref _droppedAudioPacketCount);
        return false;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _audioPackets.Writer.TryComplete();

        if (_audioSendTask is not null)
        {
            await WaitForShutdownAsync(_audioSendTask).ConfigureAwait(false);
        }

        if (_isStarted)
        {
            await SendAsync(LocalLoopbackProtocol.EncodeClose(_options.Handle), CancellationToken.None).ConfigureAwait(false);
        }

        _stop.Cancel();
        if (_heartbeatTask is not null)
        {
            await WaitForShutdownAsync(_heartbeatTask).ConfigureAwait(false);
        }

        _udpClient.Dispose();
        _stop.Dispose();
    }

    private async Task AudioSendLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _audioPackets.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_audioPackets.Reader.TryRead(out var packet))
                {
                    try
                    {
                        await SendAsync(LocalLoopbackProtocol.EncodeAudio(_options.Handle, packet.Sequence, packet), cancellationToken).ConfigureAwait(false);
                    }
                    catch (SocketException)
                    {
                        Interlocked.Increment(ref _droppedAudioPacketCount);
                    }
                    finally
                    {
                        packet.Reset();
                        _availablePackets.Enqueue(packet);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatInterval, cancellationToken).ConfigureAwait(false);
                await SendAsync(LocalLoopbackProtocol.EncodeHeartbeat(_options.Handle), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _udpClient.SendAsync(bytes, bytes.Length, _endpoint).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private static async Task WaitForShutdownAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
    }

    private sealed class PooledAudioPacket : IReadOnlyList<float>
    {
        public PooledAudioPacket(int capacity)
        {
            Buffer = new float[capacity];
        }

        public float[] Buffer { get; }

        public int Count { get; set; }

        public long Sequence { get; set; }

        public float this[int index] => index >= 0 && index < Count
            ? Buffer[index]
            : throw new ArgumentOutOfRangeException(nameof(index));

        public void Reset()
        {
            Count = 0;
            Sequence = 0;
        }

        public IEnumerator<float> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return Buffer[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
