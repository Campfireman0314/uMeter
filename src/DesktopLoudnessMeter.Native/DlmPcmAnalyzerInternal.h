#pragma once

#include "DlmPcmAnalyzer.h"

#include <array>
#include <deque>
#include <vector>

namespace dlm::pcm
{
    constexpr double Pi = 3.14159265358979323846;
    constexpr int TruePeakKernelRadius = 16;
    constexpr int TruePeakBufferLength = (TruePeakKernelRadius * 2) + 2;
    constexpr double AbsoluteGateLufs = -70.0;
    constexpr double RelativeGateLu = -10.0;
    constexpr double LufsOffset = -0.691;
    constexpr int MaximumWaveformPoints = 960;
    constexpr int MaximumStereoImagePoints = 160;
    constexpr double SpectrumDisplayFloorDb = -200.0;
    constexpr double SpectrumDisplayRangeDb = 200.0;
    constexpr int SpectrumBucketCount = 320;
    constexpr double SpectrumMinimumFrequencyHz = 20.0;
    constexpr double SpectrumMaximumFrequencyHz = 22000.0;

    enum class PerformanceMode
    {
        Normal = 0,
        Eco = 1,
    };

    struct PerformanceSettings
    {
        PerformanceMode mode = PerformanceMode::Normal;
        int spectrumRefreshIntervalFrames = 1024;
        int spectrumAnalysisSamples = 16384;
    };

    bool IsSupportedOversampling(int value);
    PerformanceSettings SettingsForMode(PerformanceMode mode);
    double ToDb(double amplitude);
    double MeanSquareToLufs(double meanSquare);
    double LufsToMeanSquare(double lufs);
    double NormalizeSample(float sample);
    double Clamp(double value, double minValue, double maxValue);
    int MillisecondsToFrames(int sampleRate, int milliseconds);
    int CopyVectorToBuffer(const std::vector<double>& values, double* buffer, int capacity, int* actualCount);

    struct BiquadFilter
    {
        double b0 = 0.0;
        double b1 = 0.0;
        double b2 = 0.0;
        double a1 = 0.0;
        double a2 = 0.0;
        double x1 = 0.0;
        double x2 = 0.0;
        double y1 = 0.0;
        double y2 = 0.0;

        double Process(double input);
        void Reset();
    };

    struct KWeightingFilter
    {
        BiquadFilter preFilter{
            1.53512485958697,
            -2.69169618940638,
            1.19839281085285,
            -1.69065929318241,
            0.73248077421585 };
        BiquadFilter rlbFilter{
            1.0,
            -2.0,
            1.0,
            -1.99004745483398,
            0.99007225036621 };

        double Process(double sample);
        void Reset();
    };

    struct SlidingPowerWindow
    {
        explicit SlidingPowerWindow(int capacityValue);

        int capacity = 0;
        std::deque<double> values;
        double sum = 0.0;

        bool IsFull() const;
        double Mean() const;
        void Add(double power);
        void Reset();
    };

    struct IntegratedLoudnessGate
    {
        IntegratedLoudnessGate(int blockFramesValue, int hopFramesValue);

        int blockFrames = 0;
        int hopFrames = 0;
        std::deque<double> window;
        std::vector<double> blockPowers;
        double windowSum = 0.0;
        int framesSinceHop = 0;

        void Add(double power);
        bool TryCalculateLufs(double& lufs) const;
        void Reset();
    };

    struct FrameMetrics
    {
        double sampleSquares = 0.0;
        double leftSquare = 0.0;
        double rightSquare = 0.0;
        double cross = 0.0;
        double midSquare = 0.0;
        double sideSquare = 0.0;
        double polarAngle = 0.0;
        double polarRadius = 0.0;
    };

    struct NativeSpectrumFft;

    struct RecentSampleWindow
    {
        RecentSampleWindow(int channelCount, int sampleRate, int oversamplingValue, int capacityFramesValue);
        RecentSampleWindow(const RecentSampleWindow&) = delete;
        RecentSampleWindow& operator=(const RecentSampleWindow&) = delete;
        RecentSampleWindow(RecentSampleWindow&& other) noexcept;
        RecentSampleWindow& operator=(RecentSampleWindow&& other) noexcept;

        int channels = 0;
        int displaySampleRate = 0;
        int oversampling = 0;
        int capacityFrames = 0;
        PerformanceSettings performanceSettings = SettingsForMode(PerformanceMode::Normal);
        NativeSpectrumFft* nativeSpectrumFft = nullptr;
        std::deque<FrameMetrics> frames;
        std::deque<double> recentFramePeaks;
        std::vector<double> displaySamples;
        std::vector<std::vector<double>> channelDisplaySamples;
        int displayStartIndex = 0;
        int displaySampleCount = 0;
        long long displayVersion = 0;
        long long displayFrameVersion = 0;
        long long waveformVersion = -1;
        long long channelWaveformVersion = -1;
        long long spectrumVersion = -1;
        long long channelSpectrumVersion = -1;
        long long spectrumEligibleFrameVersion = -1;
        long long channelSpectrumEligibleFrameVersion = -1;
        std::vector<double> waveformCache;
        std::vector<std::vector<double>> channelWaveformCache;
        std::vector<double> spectrumCache;
        std::vector<std::vector<double>> channelSpectrumCache;
        std::vector<std::array<double, 2>> stereoImagePointsCache;
        long long stereoImagePointsVersion = -1;
        std::vector<double> fftInput;
        std::vector<double> fftMagnitudes;
        std::vector<double> fftWindow;
        double fftWindowSum = 0.0;
        double sampleSquareSum = 0.0;
        double leftSquareSum = 0.0;
        double rightSquareSum = 0.0;
        double crossSum = 0.0;
        double midSquareSum = 0.0;
        double sideSquareSum = 0.0;
        double currentTruePeak = 0.0;

        ~RecentSampleWindow();

        bool IsFull() const;
        void SetPerformanceMode(PerformanceMode mode);
        void Add(
            const std::array<double, 2>& samples,
            double framePeak,
            const std::vector<double>& oversampledMonoSamples,
            const std::vector<double>& oversampledInterleavedSamples);
        double Rms() const;
        bool TryGetCorrelation(double& value) const;
        bool TryGetStereoImageX(double& value) const;
        bool TryGetStereoImageY(double& value) const;
        const std::vector<std::array<double, 2>>& GetStereoImagePoints();
        const std::vector<double>& GetWaveformSamples();
        const std::vector<double>& GetChannelWaveformSamples(int channel);
        const std::vector<double>& GetSpectrumBuckets();
        const std::vector<double>& GetChannelSpectrumBuckets(int channel);
        void Reset();

    private:
        void EnqueueDisplaySamples(
            const std::vector<double>& monoSamples,
            const std::vector<double>& interleavedChannelSamples);
        int ReserveDisplaySampleSlot();
        double GetDisplaySample(int offset) const;
        double GetChannelDisplaySample(int channel, int offset) const;
        void FillWaveformBuffer(std::vector<double>& target, int channelIndex);
        void FillSpectrumBuffer(std::vector<double>& target, int channelIndex);
        bool ShouldRefreshSpectrum(long long& eligibleFrameVersion, bool hasCache);
        void EnsureFftWorkspace(int fftSize);
        static double SanitizeSample(double sample);
    };

    struct DlmPcmAnalyzerHandle
    {
        int sampleRate = 0;
        int channels = 0;
        int truePeakOversampling = 0;
        bool channelWaveformEnabled = false;
        bool channelSpectrumEnabled = false;
        long long processedFrames = 0;
        PerformanceMode performanceMode = PerformanceMode::Normal;
        std::array<KWeightingFilter, 2> kWeightingFilters{};
        SlidingPowerWindow momentaryWindow{ 1 };
        SlidingPowerWindow shortTermWindow{ 1 };
        IntegratedLoudnessGate integratedGate{ 1, 1 };
        RecentSampleWindow recentWindow{ 1, 48000, 4, 1 };
        std::vector<std::vector<double>> truePeakBuffers;
        std::vector<int> truePeakBufferCounts;
        std::vector<std::vector<double>> truePeakKernels;
        std::vector<double> latestOversampledMono;
        std::vector<double> latestOversampledInterleaved;
    };

    std::vector<std::vector<double>> CreateTruePeakKernels(int oversampling);
    int EvaluateTruePeakSegment(DlmPcmAnalyzerHandle& analyzer, double rawFramePeak, double& framePeak);
    void ShiftTruePeakBuffers(DlmPcmAnalyzerHandle& analyzer);
}
