using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal static class NativeLoopbackProtocol
{
    private const int SourceStringFieldHandle = 0;
    private const int SourceStringFieldDisplayName = 1;

    public static bool TryDecode(ReadOnlySpan<byte> bytes, out LocalLoopbackPacket packet)
    {
        packet = default!;
        var byteArray = bytes.ToArray();
        var result = NativeMethods.DlmLoopbackProtocolDecodeExport(
            byteArray,
            byteArray.Length,
            out var packetHandle,
            out var nativePacket);
        if (result == 1)
        {
            return false;
        }

        ThrowIfFailed(result, "Native loopback protocol decode failed");
        try
        {
            var handle = CopyString(packetHandle, SourceStringFieldHandle, nativePacket.HandleUtf8ByteCount);
            var displayName = CopyString(packetHandle, SourceStringFieldDisplayName, nativePacket.DisplayNameUtf8ByteCount);
            var samples = CopySamples(packetHandle, nativePacket.SampleCount);
            packet = new LocalLoopbackPacket(
                ToPacketType(nativePacket.PacketType),
                handle,
                string.IsNullOrEmpty(displayName) ? null : displayName,
                ToSourceKind(nativePacket.SourceKind),
                nativePacket.SampleRate == 0 ? null : nativePacket.SampleRate,
                nativePacket.Channels == 0 ? null : nativePacket.Channels,
                nativePacket.Sequence,
                samples);
            return true;
        }
        finally
        {
            NativeMethods.DlmLoopbackProtocolDestroyPacketExport(packetHandle);
        }
    }

    public static byte[] EncodeAck(string handle)
    {
        var buffer = new byte[512];
        var result = NativeMethods.DlmLoopbackProtocolEncodeAckExport(handle, buffer, buffer.Length, out var actualCount);
        if (result == -5)
        {
            buffer = new byte[Math.Max(actualCount, Encoding.UTF8.GetByteCount(handle) + 128)];
            result = NativeMethods.DlmLoopbackProtocolEncodeAckExport(handle, buffer, buffer.Length, out actualCount);
        }

        ThrowIfFailed(result, "Native loopback protocol ack encode failed");
        return buffer[..actualCount];
    }

    private static string CopyString(IntPtr packetHandle, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmLoopbackProtocolCopyStringExport(packetHandle, field, buffer, buffer.Length, out var actualCount),
            "Native loopback protocol string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static IReadOnlyList<float>? CopySamples(IntPtr packetHandle, int count)
    {
        if (count <= 0)
        {
            return null;
        }

        var buffer = new float[count];
        ThrowIfFailed(
            NativeMethods.DlmLoopbackProtocolCopySamplesExport(packetHandle, buffer, buffer.Length, out var actualCount),
            "Native loopback protocol sample copy failed");
        return actualCount == buffer.Length ? buffer : buffer[..actualCount];
    }

    private static LocalLoopbackPacketType ToPacketType(int value)
    {
        return Enum.IsDefined(typeof(LocalLoopbackPacketType), value)
            ? (LocalLoopbackPacketType)value
            : throw new InvalidOperationException($"Unknown native loopback packet type {value}.");
    }

    private static LocalLoopbackSourceKind ToSourceKind(int value)
    {
        return value == (int)LocalLoopbackSourceKind.Plugin
            ? LocalLoopbackSourceKind.Plugin
            : LocalLoopbackSourceKind.Loopback;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result < 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeLoopbackProtocolPacket
    {
        public int PacketType;
        public int SourceKind;
        public int SampleRate;
        public int Channels;
        public long Sequence;
        public int HandleUtf8ByteCount;
        public int DisplayNameUtf8ByteCount;
        public int SampleCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmLoopbackProtocolDecodeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmLoopbackProtocolDecodeExport(
            [In] byte[] bytes,
            int byteCount,
            out IntPtr packetHandle,
            out NativeLoopbackProtocolPacket packet);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmLoopbackProtocolDestroyPacketExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmLoopbackProtocolDestroyPacketExport(IntPtr packetHandle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmLoopbackProtocolCopyStringExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmLoopbackProtocolCopyStringExport(
            IntPtr packetHandle,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmLoopbackProtocolCopySamplesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmLoopbackProtocolCopySamplesExport(
            IntPtr packetHandle,
            [Out] float[] buffer,
            int capacity,
            out int actualCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmLoopbackProtocolEncodeAckExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmLoopbackProtocolEncodeAckExport(
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceHandle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}
