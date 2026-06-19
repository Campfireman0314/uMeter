using System.Runtime.InteropServices;
using DesktopLoudnessMeter.App.Diagnostics;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeRfRapidSpectrumFft : ISpectrumFft
{
    private static readonly object LogSync = new();
    private static bool s_loggedBackend;
    private readonly ISpectrumFft _fallback;
    private bool _disabled;
    private bool _initialized;

    public NativeRfRapidSpectrumFft()
        : this(new ManagedSpectrumFft())
    {
    }

    internal NativeRfRapidSpectrumFft(ISpectrumFft fallback)
    {
        _fallback = fallback;
    }

    public string Name => _disabled ? _fallback.Name : "RfRapid native FFT";

    public void ComputeMagnitudes(
        double[] samples,
        double[] window,
        int fftSize,
        double windowSum,
        double[] magnitudes)
    {
        if (!_disabled && TryComputeNative(samples, window, fftSize, windowSum, magnitudes))
        {
            LogBackendOnce("Spectrum FFT backend: RfRapid native FFT.");
            return;
        }

        LogBackendOnce($"Spectrum FFT backend: {_fallback.Name}.");
        _fallback.ComputeMagnitudes(samples, window, fftSize, windowSum, magnitudes);
    }

    private bool TryComputeNative(
        double[] samples,
        double[] window,
        int fftSize,
        double windowSum,
        double[] magnitudes)
    {
        try
        {
            if (!_initialized)
            {
                if (NativeMethods.DlmRfRapidInit() != 0)
                {
                    _disabled = true;
                    return false;
                }

                _initialized = true;
            }

            var result = NativeMethods.DlmRfRapidSpectrumF64(
                samples,
                window,
                fftSize,
                windowSum,
                magnitudes,
                magnitudes.Length);
            if (result == 0)
            {
                return true;
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (BadImageFormatException)
        {
        }
        catch (SEHException)
        {
        }

        _disabled = true;
        return false;
    }

    private static void LogBackendOnce(string message)
    {
        lock (LogSync)
        {
            if (s_loggedBackend)
            {
                return;
            }

            s_loggedBackend = true;
            AppLogger.Info(message);
        }
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmRfRapidInit();

        [DllImport("DesktopLoudnessMeter.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmRfRapidSpectrumF64(
            [In] double[] samples,
            [In] double[] window,
            int fftSize,
            double windowSum,
            [Out] double[] magnitudes,
            int magnitudeCount);
    }
}
