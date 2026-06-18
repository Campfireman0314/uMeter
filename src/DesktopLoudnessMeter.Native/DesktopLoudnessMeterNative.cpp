#include "RfRapid/RfRapid.h"

#include <cmath>
#include <exception>
#include <vector>

bool if_log = false;

namespace
{
    bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }
}

extern "C" __declspec(dllexport) int __cdecl DlmRfRapidInit()
{
    try
    {
        RF::RfRapidVec::init();
        return 0;
    }
    catch (...)
    {
        return -1;
    }
}

extern "C" __declspec(dllexport) int __cdecl DlmRfRapidSpectrumF64(
    const double* samples,
    const double* window,
    int fftSize,
    double windowSum,
    double* magnitudes,
    int magnitudeCount)
{
    if (samples == nullptr || window == nullptr || magnitudes == nullptr)
    {
        return -2;
    }

    if (!IsPowerOfTwo(fftSize) || magnitudeCount < ((fftSize / 2) + 1) || windowSum <= 0.0)
    {
        return -3;
    }

    try
    {
        RF::RfRapidVec::init();

        thread_local std::vector<RfCplx64> buffer;
        buffer.resize(static_cast<size_t>(fftSize));

        for (int index = 0; index < fftSize; ++index)
        {
            buffer[static_cast<size_t>(index)].re = samples[index] * window[index];
            buffer[static_cast<size_t>(index)].im = 0.0;
        }

        RF::RfRapidVec::cfft_forward(buffer.data(), static_cast<size_t>(fftSize));

        const int bins = (fftSize / 2) + 1;
        const double scale = 2.0 / windowSum;
        for (int bin = 0; bin < bins; ++bin)
        {
            const RfCplx64& value = buffer[static_cast<size_t>(bin)];
            magnitudes[bin] = scale * std::sqrt((value.re * value.re) + (value.im * value.im));
        }

        return 0;
    }
    catch (...)
    {
        return -4;
    }
}
