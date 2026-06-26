#include "DlmPcmAnalyzerInternal.h"

#include "RfRapid/RfRapid.h"

#include <algorithm>
#include <cstring>

namespace dlm::pcm
{
    int CopyVectorToBuffer(const std::vector<double>& values, double* buffer, int capacity, int* actualCount)
    {
        if (buffer == nullptr || actualCount == nullptr || capacity < 0)
        {
            return -2;
        }

        const int count = static_cast<int>(values.size());
        if (capacity < count)
        {
            return -5;
        }

        if (count > 0)
        {
            std::memcpy(buffer, values.data(), sizeof(double) * static_cast<size_t>(count));
        }

        *actualCount = count;
        return 0;
    }
}

using namespace dlm::pcm;

int DlmPcmAnalyzerCreate(int sampleRate, int channels, int truePeakOversampling, void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    if (sampleRate <= 0 || channels < 1 || channels > 2 || !IsSupportedOversampling(truePeakOversampling))
    {
        return -3;
    }

    try
    {
        RF::RfRapidVec::init();
        auto* analyzer = new DlmPcmAnalyzerHandle();
        analyzer->sampleRate = sampleRate;
        analyzer->channels = channels;
        analyzer->truePeakOversampling = truePeakOversampling;
        analyzer->momentaryWindow = SlidingPowerWindow(MillisecondsToFrames(sampleRate, 400));
        analyzer->shortTermWindow = SlidingPowerWindow(MillisecondsToFrames(sampleRate, 3000));
        analyzer->integratedGate = IntegratedLoudnessGate(
            MillisecondsToFrames(sampleRate, 400),
            MillisecondsToFrames(sampleRate, 100));
        analyzer->recentWindow = RecentSampleWindow(
            channels,
            sampleRate,
            truePeakOversampling,
            MillisecondsToFrames(sampleRate, 100));
        analyzer->recentWindow.SetPerformanceMode(analyzer->performanceMode);
        analyzer->truePeakBuffers.resize(static_cast<size_t>(channels), std::vector<double>(TruePeakBufferLength));
        analyzer->truePeakBufferCounts.resize(static_cast<size_t>(channels), 0);
        analyzer->truePeakKernels = CreateTruePeakKernels(truePeakOversampling);
        *handle = analyzer;
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmPcmAnalyzerDestroy(void* handle)
{
    delete static_cast<DlmPcmAnalyzerHandle*>(handle);
}

int DlmPcmAnalyzerReset(void* handle)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    for (int channel = 0; channel < 2; ++channel)
    {
        analyzer->kWeightingFilters[static_cast<size_t>(channel)].Reset();
    }

    analyzer->momentaryWindow.Reset();
    analyzer->shortTermWindow.Reset();
    analyzer->integratedGate.Reset();
    analyzer->recentWindow.Reset();
    analyzer->processedFrames = 0;
    analyzer->latestOversampledMono.clear();
    analyzer->latestOversampledInterleaved.clear();

    for (auto& buffer : analyzer->truePeakBuffers)
    {
        std::fill(buffer.begin(), buffer.end(), 0.0);
    }

    std::fill(analyzer->truePeakBufferCounts.begin(), analyzer->truePeakBufferCounts.end(), 0);
    return 0;
}

int DlmPcmAnalyzerProcessF32(void* handle, const float* interleavedSamples, int sampleCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr || interleavedSamples == nullptr)
    {
        return -2;
    }

    if (sampleCount < 0 || sampleCount % analyzer->channels != 0)
    {
        return -3;
    }

    try
    {
        std::array<double, 2> frameBuffer{ 0.0, 0.0 };
        for (int index = 0; index < sampleCount; index += analyzer->channels)
        {
            double weightedPower = 0.0;
            double rawFramePeak = 0.0;
            for (int channel = 0; channel < analyzer->channels; ++channel)
            {
                const double sample = NormalizeSample(interleavedSamples[index + channel]);
                frameBuffer[static_cast<size_t>(channel)] = sample;
                rawFramePeak = (std::max)(rawFramePeak, std::abs(sample));

                auto& truePeakBuffer = analyzer->truePeakBuffers[static_cast<size_t>(channel)];
                truePeakBuffer[static_cast<size_t>(analyzer->truePeakBufferCounts[static_cast<size_t>(channel)]++)] = sample;

                const double weighted = analyzer->kWeightingFilters[static_cast<size_t>(channel)].Process(sample);
                weightedPower += weighted * weighted;
            }

            double framePeak = rawFramePeak;
            std::vector<double> monoSamples;
            std::vector<double> channelSamples;
            if (analyzer->truePeakBufferCounts[0] == TruePeakBufferLength)
            {
                EvaluateTruePeakSegment(*analyzer, rawFramePeak, framePeak);
                ShiftTruePeakBuffers(*analyzer);
                monoSamples = analyzer->latestOversampledMono;
                channelSamples = analyzer->latestOversampledInterleaved;
            }

            analyzer->recentWindow.Add(frameBuffer, framePeak, monoSamples, channelSamples);
            analyzer->momentaryWindow.Add(weightedPower);
            analyzer->shortTermWindow.Add(weightedPower);
            analyzer->integratedGate.Add(weightedPower);
            analyzer->processedFrames++;
        }

        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmPcmAnalyzerGetSnapshot(void* handle, DlmPcmAnalyzerSnapshot* snapshot)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr || snapshot == nullptr)
    {
        return -2;
    }

    *snapshot = {};
    if (analyzer->processedFrames <= 0)
    {
        return 0;
    }

    if (analyzer->momentaryWindow.IsFull())
    {
        const double momentaryMean = analyzer->momentaryWindow.Mean();
        if (momentaryMean > 0.0)
        {
            snapshot->hasMomentaryLufs = 1;
            snapshot->momentaryLufs = MeanSquareToLufs(momentaryMean);
        }
    }

    if (analyzer->shortTermWindow.IsFull())
    {
        const double shortTermMean = analyzer->shortTermWindow.Mean();
        if (shortTermMean > 0.0)
        {
            snapshot->hasShortTermLufs = 1;
            snapshot->shortTermLufs = MeanSquareToLufs(shortTermMean);
        }
    }

    double integratedLufs = 0.0;
    if (analyzer->integratedGate.TryCalculateLufs(integratedLufs))
    {
        snapshot->hasIntegratedLufs = 1;
        snapshot->integratedLufs = integratedLufs;
    }

    snapshot->hasTruePeakDb = 1;
    snapshot->truePeakDb = ToDb(analyzer->recentWindow.currentTruePeak);

    const double seconds = static_cast<double>(analyzer->processedFrames) / analyzer->sampleRate;
    if (seconds >= 0.1)
    {
        snapshot->hasRms100MsDb = 1;
        snapshot->rms100MsDb = ToDb(analyzer->recentWindow.Rms());
    }

    double stereoImageX = 0.0;
    if (analyzer->recentWindow.TryGetStereoImageX(stereoImageX))
    {
        snapshot->hasStereoImageX = 1;
        snapshot->stereoImageX = stereoImageX;
    }

    double stereoImageY = 0.0;
    if (analyzer->recentWindow.TryGetStereoImageY(stereoImageY))
    {
        snapshot->hasStereoImageY = 1;
        snapshot->stereoImageY = stereoImageY;
    }

    double correlation = 0.0;
    if (analyzer->recentWindow.TryGetCorrelation(correlation))
    {
        snapshot->hasCorrelation = 1;
        snapshot->correlation = correlation;
    }

    snapshot->waveformCount = static_cast<int>(analyzer->recentWindow.GetWaveformSamples().size());
    snapshot->channelWaveformCount = analyzer->channelWaveformEnabled
        ? static_cast<int>(analyzer->recentWindow.GetChannelWaveformSamples(0).size())
        : 0;
    snapshot->spectrumCount = static_cast<int>(analyzer->recentWindow.GetSpectrumBuckets().size());
    snapshot->channelSpectrumCount = analyzer->channelSpectrumEnabled
        ? static_cast<int>(analyzer->recentWindow.GetChannelSpectrumBuckets(0).size())
        : 0;
    snapshot->stereoImagePointCount = static_cast<int>(analyzer->recentWindow.GetStereoImagePoints().size());
    snapshot->plotFrameVersion = analyzer->processedFrames;
    return 0;
}

int DlmPcmAnalyzerSetChannelWaveformEnabled(void* handle, int enabled)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    analyzer->channelWaveformEnabled = enabled != 0;
    return 0;
}

int DlmPcmAnalyzerSetChannelSpectrumEnabled(void* handle, int enabled)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    analyzer->channelSpectrumEnabled = enabled != 0;
    return 0;
}

int DlmPcmAnalyzerSetPerformanceMode(void* handle, int mode)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    if (mode != 0 && mode != 1)
    {
        return -3;
    }

    analyzer->performanceMode = mode == 1 ? PerformanceMode::Eco : PerformanceMode::Normal;
    analyzer->recentWindow.SetPerformanceMode(analyzer->performanceMode);
    return 0;
}

int DlmPcmAnalyzerCopyWaveform(void* handle, double* buffer, int capacity, int* actualCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    return CopyVectorToBuffer(analyzer->recentWindow.GetWaveformSamples(), buffer, capacity, actualCount);
}

int DlmPcmAnalyzerCopyChannelWaveform(void* handle, int channel, double* buffer, int capacity, int* actualCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    if (channel < 0 || channel >= analyzer->channels)
    {
        return -3;
    }

    return CopyVectorToBuffer(analyzer->recentWindow.GetChannelWaveformSamples(channel), buffer, capacity, actualCount);
}

int DlmPcmAnalyzerCopySpectrum(void* handle, double* buffer, int capacity, int* actualCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    return CopyVectorToBuffer(analyzer->recentWindow.GetSpectrumBuckets(), buffer, capacity, actualCount);
}

int DlmPcmAnalyzerCopyChannelSpectrum(void* handle, int channel, double* buffer, int capacity, int* actualCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr)
    {
        return -2;
    }

    if (channel < 0 || channel >= analyzer->channels)
    {
        return -3;
    }

    return CopyVectorToBuffer(analyzer->recentWindow.GetChannelSpectrumBuckets(channel), buffer, capacity, actualCount);
}

int DlmPcmAnalyzerCopyStereoImagePoints(void* handle, double* angleBuffer, double* radiusBuffer, int capacity, int* actualCount)
{
    auto* analyzer = static_cast<DlmPcmAnalyzerHandle*>(handle);
    if (analyzer == nullptr || angleBuffer == nullptr || radiusBuffer == nullptr || actualCount == nullptr)
    {
        return -2;
    }

    if (capacity < 0)
    {
        return -3;
    }

    const auto& points = analyzer->recentWindow.GetStereoImagePoints();
    const int count = static_cast<int>(points.size());
    if (capacity < count)
    {
        return -5;
    }

    for (int index = 0; index < count; ++index)
    {
        angleBuffer[index] = points[static_cast<size_t>(index)][0];
        radiusBuffer[index] = points[static_cast<size_t>(index)][1];
    }

    *actualCount = count;
    return 0;
}
