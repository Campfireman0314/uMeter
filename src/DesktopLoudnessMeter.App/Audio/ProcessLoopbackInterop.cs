using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;

namespace DesktopLoudnessMeter.App.Audio;

internal static class ProcessLoopbackInterop
{
    internal const string VirtualAudioDeviceProcessLoopback = "VAD\\Process_Loopback";
    internal const int MinimumAttemptBuild = 19041;
    internal const int DocumentedSupportBuild = 20348;
    private const ushort VtBlob = 65;
    private static readonly Guid IidAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, MinimumAttemptBuild);

    public static async Task<AudioClient> ActivateAudioClientAsync(uint processId)
    {
        var activationParams = new AudioClientActivationParams
        {
            ActivationType = AudioClientActivationType.ProcessLoopback,
            ProcessLoopbackParams = new AudioClientProcessLoopbackParams
            {
                TargetProcessId = processId,
                ProcessLoopbackMode = ProcessLoopbackMode.IncludeTargetProcessTree
            }
        };

        var activationParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<AudioClientActivationParams>());
        var propVariantPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
        IActivateAudioInterfaceAsyncOperation? operation = null;

        try
        {
            Marshal.StructureToPtr(activationParams, activationParamsPtr, false);
            var propVariant = new PropVariant
            {
                Vt = VtBlob,
                Blob = new Blob
                {
                    Size = (uint)Marshal.SizeOf<AudioClientActivationParams>(),
                    Data = activationParamsPtr
                }
            };
            Marshal.StructureToPtr(propVariant, propVariantPtr, false);

            var completionHandler = new ProcessLoopbackActivationHandler();
            var iid = IidAudioClient;
            var hr = ActivateAudioInterfaceAsync(
                VirtualAudioDeviceProcessLoopback,
                ref iid,
                propVariantPtr,
                completionHandler,
                out operation);
            Marshal.ThrowExceptionForHR(hr);

            var audioClient = await completionHandler.Task.ConfigureAwait(false);
            return new AudioClient(audioClient);
        }
        finally
        {
            if (operation is not null)
            {
                Marshal.ReleaseComObject(operation);
            }

            Marshal.FreeHGlobal(propVariantPtr);
            Marshal.FreeHGlobal(activationParamsPtr);
        }
    }

    [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int ActivateAudioInterfaceAsync(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        ref Guid riid,
        IntPtr activationParams,
        [MarshalAs(UnmanagedType.Interface)] IActivateAudioInterfaceCompletionHandler completionHandler,
        [MarshalAs(UnmanagedType.Interface)] out IActivateAudioInterfaceAsyncOperation activationOperation);

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientActivationParams
    {
        public AudioClientActivationType ActivationType;

        public AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientProcessLoopbackParams
    {
        public uint TargetProcessId;

        public ProcessLoopbackMode ProcessLoopbackMode;
    }

    private enum AudioClientActivationType
    {
        Default,
        ProcessLoopback
    }

    private enum ProcessLoopbackMode
    {
        IncludeTargetProcessTree,
        ExcludeTargetProcessTree
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        public uint Size;

        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort Vt;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public Blob Blob;
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class ProcessLoopbackActivationHandler : IActivateAudioInterfaceCompletionHandler
    {
        private readonly TaskCompletionSource<IAudioClient> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IAudioClient> Task => _completion.Task;

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            try
            {
                activateOperation.GetActivateResult(out var activateResult, out var activatedInterface);
                if (activateResult != 0)
                {
                    _completion.TrySetException(Marshal.GetExceptionForHR(activateResult) ?? new COMException("Process loopback activation failed.", activateResult));
                    return;
                }

                _completion.TrySetResult((IAudioClient)activatedInterface);
            }
            catch (Exception ex)
            {
                _completion.TrySetException(ex);
            }
        }
    }
}
