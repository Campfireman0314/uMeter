using System.Runtime.InteropServices;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeRfRapidSpectrumFft : ISpectrumFft
{
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
            return;
        }

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
