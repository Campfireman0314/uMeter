#include "DlmPcmAnalyzer.h"
#include "RfRapid/RfRapid.h"

#include <algorithm>
#include <cmath>
#include <deque>
#include <exception>
#include <vector>

bool if_log = false;

namespace
{
    constexpr int TruePeakKernelRadius = 16;
    constexpr int TruePeakBufferLength = (TruePeakKernelRadius * 2) + 2;

    struct TruePeakEstimatorHandle
    {
        int channels = 0;
        int oversampling = 0;
        int windowFrames = 0;
        double truePeak = 0.0;
        long long sampleCount = 0;
        std::vector<std::vector<double>> buffers;
        std::vector<int> bufferCounts;
        std::vector<std::vector<double>> kernels;
        std::deque<double> recentFramePeaks;
    };

    bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    bool IsSupportedOversampling(int value)
    {
        return value == 2 || value == 4 || value == 8;
    }

    double Sinc(double value)
    {
        if (std::abs(value) < 1.0e-12)
        {
            return 1.0;
        }

        const double angle = 3.14159265358979323846 * value;
        return std::sin(angle) / angle;
    }

    double Hann(double distance)
    {
        return 0.5 + (0.5 * std::cos(3.14159265358979323846 * distance / TruePeakKernelRadius));
    }

    std::vector<std::vector<double>> CreateTruePeakKernels(int oversampling)
    {
        std::vector<std::vector<double>> kernels(static_cast<size_t>(oversampling));
        for (int step = 0; step < oversampling; ++step)
        {
            const double position = TruePeakKernelRadius + (static_cast<double>(step) / oversampling);
            auto& kernel = kernels[static_cast<size_t>(step)];
            kernel.resize(TruePeakBufferLength);
            for (int index = 0; index < TruePeakBufferLength; ++index)
            {
                const double distance = position - index;
                kernel[static_cast<size_t>(index)] = std::abs(distance) > TruePeakKernelRadius
                    ? 0.0
                    : Sinc(distance) * Hann(distance);
            }
        }

        return kernels;
    }

    double Reconstruct(const std::vector<double>& samples, const std::vector<double>& kernel)
    {
        return RF::RfRapidVec::dconv_dot_product(samples.data(), kernel.data(), kernel.size());
    }

    int EvaluateBufferedSegment(
        TruePeakEstimatorHandle& estimator,
        double rawFramePeak,
        double* oversampledMono,
        double* oversampledInterleaved,
        int outputFrameOffset,
        double& peak)
    {
        peak = rawFramePeak;
        for (int step = 0; step < estimator.oversampling; ++step)
        {
            double mono = 0.0;
            for (int channel = 0; channel < estimator.channels; ++channel)
            {
                const double reconstructed = Reconstruct(
                    estimator.buffers[static_cast<size_t>(channel)],
                    estimator.kernels[static_cast<size_t>(step)]);
                mono += reconstructed;

                if (oversampledInterleaved != nullptr)
                {
                    oversampledInterleaved[
                        ((outputFrameOffset + step) * estimator.channels) + channel] = reconstructed;
                }

                if (step > 0)
                {
                    peak = (std::max)(peak, std::abs(reconstructed));
                }
            }

            if (oversampledMono != nullptr)
            {
                oversampledMono[outputFrameOffset + step] = mono / estimator.channels;
            }
        }

        return estimator.oversampling;
    }

    void ShiftBuffers(TruePeakEstimatorHandle& estimator)
    {
        for (int channel = 0; channel < estimator.channels; ++channel)
        {
            auto& buffer = estimator.buffers[static_cast<size_t>(channel)];
            std::move(buffer.begin() + 1, buffer.end(), buffer.begin());
            estimator.bufferCounts[static_cast<size_t>(channel)] = TruePeakBufferLength - 1;
        }
    }

    void UpdatePeak(TruePeakEstimatorHandle& estimator, double amplitude)
    {
        if (estimator.windowFrames <= 0)
        {
            estimator.truePeak = (std::max)(estimator.truePeak, amplitude);
            return;
        }

        estimator.recentFramePeaks.push_back(amplitude);
        double removedPeak = 0.0;
        while (static_cast<int>(estimator.recentFramePeaks.size()) > estimator.windowFrames)
        {
            removedPeak = (std::max)(removedPeak, estimator.recentFramePeaks.front());
            estimator.recentFramePeaks.pop_front();
        }

        if (amplitude >= estimator.truePeak)
        {
            estimator.truePeak = amplitude;
        }
        else if (removedPeak >= estimator.truePeak)
        {
            estimator.truePeak = estimator.recentFramePeaks.empty()
                ? 0.0
                : *std::max_element(estimator.recentFramePeaks.begin(), estimator.recentFramePeaks.end());
        }
    }
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCreateExport(
    int sampleRate,
    int channels,
    int truePeakOversampling,
    void** handle)
{
    return DlmPcmAnalyzerCreate(sampleRate, channels, truePeakOversampling, handle);
}

extern "C" __declspec(dllexport) void __cdecl DlmPcmAnalyzerDestroyExport(void* handle)
{
    DlmPcmAnalyzerDestroy(handle);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerResetExport(void* handle)
{
    return DlmPcmAnalyzerReset(handle);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerProcessF32Export(
    void* handle,
    const float* interleavedSamples,
    int sampleCount)
{
    return DlmPcmAnalyzerProcessF32(handle, interleavedSamples, sampleCount);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerGetSnapshotExport(
    void* handle,
    DlmPcmAnalyzerSnapshot* snapshot)
{
    return DlmPcmAnalyzerGetSnapshot(handle, snapshot);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerSetChannelWaveformEnabledExport(
    void* handle,
    int enabled)
{
    return DlmPcmAnalyzerSetChannelWaveformEnabled(handle, enabled);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerSetChannelSpectrumEnabledExport(
    void* handle,
    int enabled)
{
    return DlmPcmAnalyzerSetChannelSpectrumEnabled(handle, enabled);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerSetPerformanceModeExport(
    void* handle,
    int mode)
{
    return DlmPcmAnalyzerSetPerformanceMode(handle, mode);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCopyWaveformExport(
    void* handle,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyWaveform(handle, buffer, capacity, actualCount);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCopyChannelWaveformExport(
    void* handle,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyChannelWaveform(handle, channel, buffer, capacity, actualCount);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCopySpectrumExport(
    void* handle,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopySpectrum(handle, buffer, capacity, actualCount);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCopyChannelSpectrumExport(
    void* handle,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyChannelSpectrum(handle, channel, buffer, capacity, actualCount);
}

extern "C" __declspec(dllexport) int __cdecl DlmPcmAnalyzerCopyStereoImagePointsExport(
    void* handle,
    double* angleBuffer,
    double* radiusBuffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyStereoImagePoints(handle, angleBuffer, radiusBuffer, capacity, actualCount);
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

extern "C" __declspec(dllexport) int __cdecl DlmRfRapidTruePeakCreate(
    int channels,
    int oversampling,
    int windowFrames,
    void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    if ((channels < 1 || channels > 2) || !IsSupportedOversampling(oversampling) || windowFrames < 0)
    {
        return -3;
    }

    try
    {
        RF::RfRapidVec::init();

        auto estimator = new TruePeakEstimatorHandle();
        estimator->channels = channels;
        estimator->oversampling = oversampling;
        estimator->windowFrames = windowFrames;
        estimator->buffers.resize(static_cast<size_t>(channels), std::vector<double>(TruePeakBufferLength));
        estimator->bufferCounts.resize(static_cast<size_t>(channels));
        estimator->kernels = CreateTruePeakKernels(oversampling);
        *handle = estimator;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

extern "C" __declspec(dllexport) void __cdecl DlmRfRapidTruePeakDestroy(void* handle)
{
    delete static_cast<TruePeakEstimatorHandle*>(handle);
}

extern "C" __declspec(dllexport) int __cdecl DlmRfRapidTruePeakReset(void* handle)
{
    auto estimator = static_cast<TruePeakEstimatorHandle*>(handle);
    if (estimator == nullptr)
    {
        return -2;
    }

    try
    {
        for (auto& buffer : estimator->buffers)
        {
            std::fill(buffer.begin(), buffer.end(), 0.0);
        }

        std::fill(estimator->bufferCounts.begin(), estimator->bufferCounts.end(), 0);
        estimator->recentFramePeaks.clear();
        estimator->truePeak = 0.0;
        estimator->sampleCount = 0;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

extern "C" __declspec(dllexport) int __cdecl DlmRfRapidTruePeakProcessF64(
    void* handle,
    const double* interleavedSamples,
    int frameCount,
    double* oversampledMono,
    double* oversampledInterleaved,
    double* framePeaks,
    int* frameOutputCounts,
    int outputCapacityFrames,
    int* outputFrameCount,
    double* truePeak)
{
    auto estimator = static_cast<TruePeakEstimatorHandle*>(handle);
    if (estimator == nullptr
        || interleavedSamples == nullptr
        || framePeaks == nullptr
        || frameOutputCounts == nullptr
        || outputFrameCount == nullptr
        || truePeak == nullptr)
    {
        return -2;
    }

    if (frameCount < 0 || outputCapacityFrames < 0)
    {
        return -3;
    }

    const int requiredOutputFrames = frameCount * estimator->oversampling;
    if (requiredOutputFrames > outputCapacityFrames)
    {
        return -5;
    }

    try
    {
        int writtenFrames = 0;
        for (int frame = 0; frame < frameCount; ++frame)
        {
            double framePeak = 0.0;
            for (int channel = 0; channel < estimator->channels; ++channel)
            {
                const double sample = interleavedSamples[(frame * estimator->channels) + channel];
                framePeak = (std::max)(framePeak, std::abs(sample));
                estimator->buffers[static_cast<size_t>(channel)]
                    [static_cast<size_t>(estimator->bufferCounts[static_cast<size_t>(channel)]++)] = sample;
            }

            int producedFrames = 0;
            if (estimator->bufferCounts[0] == TruePeakBufferLength)
            {
                double peak = 0.0;
                producedFrames = EvaluateBufferedSegment(
                    *estimator,
                    framePeak,
                    oversampledMono,
                    oversampledInterleaved,
                    frame * estimator->oversampling,
                    peak);
                framePeak = peak;
                ShiftBuffers(*estimator);
            }

            framePeaks[frame] = framePeak;
            frameOutputCounts[frame] = producedFrames;
            UpdatePeak(*estimator, framePeak);
            estimator->sampleCount++;
            writtenFrames += producedFrames;
        }

        *outputFrameCount = writtenFrames;
        *truePeak = estimator->truePeak;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}
