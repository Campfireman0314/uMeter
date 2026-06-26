#include "DlmPcmAnalyzerInternal.h"

#include "RfRapid/RfRapid.h"

#include <algorithm>
#include <cmath>
#include <limits>

namespace dlm::pcm
{
    namespace
    {
        bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        double HannWindow(int index, int length)
        {
            return 0.5 - (0.5 * std::cos((2.0 * Pi * index) / (std::max)(1, length - 1)));
        }

        double HertzToMel(double hertz)
        {
            return 2595.0 * std::log10(1.0 + (hertz / 700.0));
        }

        double MelToHertz(double mel)
        {
            return 700.0 * (std::pow(10.0, mel / 2595.0) - 1.0);
        }

        int PreviousPowerOfTwo(int value)
        {
            int power = 1;
            while ((power * 2) <= value)
            {
                power *= 2;
            }

            return power;
        }

        double MagnitudeToDisplayLevel(double magnitude)
        {
            const double decibels = magnitude <= 1.0e-12 ? SpectrumDisplayFloorDb : 20.0 * std::log10(magnitude);
            return Clamp((decibels - SpectrumDisplayFloorDb) / SpectrumDisplayRangeDb, 0.0, 1.0);
        }

        double InterpolateMagnitude(const std::vector<double>& magnitudes, int count, double binPosition)
        {
            const int lower = static_cast<int>(std::floor(binPosition));
            if (lower < 0)
            {
                return magnitudes.front();
            }

            if (lower >= count - 1)
            {
                return magnitudes[static_cast<size_t>(count - 1)];
            }

            const double fraction = binPosition - lower;
            return magnitudes[static_cast<size_t>(lower)]
                + ((magnitudes[static_cast<size_t>(lower + 1)] - magnitudes[static_cast<size_t>(lower)]) * fraction);
        }

        struct ComplexSample
        {
            double real = 0.0;
            double imaginary = 0.0;
        };

        void FastFourierTransform(std::vector<ComplexSample>& samples, int count)
        {
            for (int index = 1, reverse = 0; index < count; ++index)
            {
                int bit = count >> 1;
                for (; (reverse & bit) != 0; bit >>= 1)
                {
                    reverse ^= bit;
                }

                reverse ^= bit;
                if (index < reverse)
                {
                    std::swap(samples[static_cast<size_t>(index)], samples[static_cast<size_t>(reverse)]);
                }
            }

            for (int length = 2; length <= count; length <<= 1)
            {
                const double angle = -2.0 * Pi / length;
                const double unitReal = std::cos(angle);
                const double unitImaginary = std::sin(angle);

                for (int start = 0; start < count; start += length)
                {
                    double real = 1.0;
                    double imaginary = 0.0;
                    const int halfLength = length / 2;
                    for (int offset = 0; offset < halfLength; ++offset)
                    {
                        const ComplexSample even = samples[static_cast<size_t>(start + offset)];
                        const ComplexSample odd = samples[static_cast<size_t>(start + offset + halfLength)];
                        const double oddReal = (odd.real * real) - (odd.imaginary * imaginary);
                        const double oddImaginary = (odd.real * imaginary) + (odd.imaginary * real);

                        samples[static_cast<size_t>(start + offset)] = { even.real + oddReal, even.imaginary + oddImaginary };
                        samples[static_cast<size_t>(start + offset + halfLength)] = { even.real - oddReal, even.imaginary - oddImaginary };

                        const double nextReal = (real * unitReal) - (imaginary * unitImaginary);
                        imaginary = (real * unitImaginary) + (imaginary * unitReal);
                        real = nextReal;
                    }
                }
            }
        }

    }

    struct NativeSpectrumFft
    {
        bool initialized = false;
        bool nativeDisabled = false;
        std::vector<ComplexSample> managedBuffer;

        bool TryComputeNative(
            const std::vector<double>& samples,
            const std::vector<double>& window,
            int fftSize,
            double windowSum,
            std::vector<double>& magnitudes)
        {
            if (nativeDisabled)
            {
                return false;
            }

            try
            {
                if (!initialized)
                {
                    RF::RfRapidVec::init();
                    initialized = true;
                }

                thread_local std::vector<RfCplx64> buffer;
                buffer.resize(static_cast<size_t>(fftSize));
                for (int index = 0; index < fftSize; ++index)
                {
                    buffer[static_cast<size_t>(index)].re = samples[static_cast<size_t>(index)] * window[static_cast<size_t>(index)];
                    buffer[static_cast<size_t>(index)].im = 0.0;
                }

                RF::RfRapidVec::cfft_forward(buffer.data(), static_cast<size_t>(fftSize));

                const int bins = (fftSize / 2) + 1;
                const double scale = 2.0 / windowSum;
                magnitudes.resize(static_cast<size_t>(bins));
                for (int bin = 0; bin < bins; ++bin)
                {
                    const RfCplx64& value = buffer[static_cast<size_t>(bin)];
                    magnitudes[static_cast<size_t>(bin)] = scale * std::sqrt((value.re * value.re) + (value.im * value.im));
                }

                return true;
            }
            catch (...)
            {
                nativeDisabled = true;
                return false;
            }
        }

        void ComputeMagnitudes(
            const std::vector<double>& samples,
            const std::vector<double>& window,
            int fftSize,
            double windowSum,
            std::vector<double>& magnitudes)
        {
            if (TryComputeNative(samples, window, fftSize, windowSum, magnitudes))
            {
                return;
            }

            if (static_cast<int>(managedBuffer.size()) != fftSize)
            {
                managedBuffer.resize(static_cast<size_t>(fftSize));
            }

            for (int index = 0; index < fftSize; ++index)
            {
                managedBuffer[static_cast<size_t>(index)] = {
                    samples[static_cast<size_t>(index)] * window[static_cast<size_t>(index)],
                    0.0 };
            }

            FastFourierTransform(managedBuffer, fftSize);
            const int bins = (fftSize / 2) + 1;
            const double scale = 2.0 / (std::max)(1.0, windowSum);
            magnitudes.resize(static_cast<size_t>(bins));
            for (int bin = 0; bin < bins; ++bin)
            {
                const ComplexSample& value = managedBuffer[static_cast<size_t>(bin)];
                magnitudes[static_cast<size_t>(bin)] = scale * std::sqrt((value.real * value.real) + (value.imaginary * value.imaginary));
            }
        }
    };

    RecentSampleWindow::RecentSampleWindow(int channelCount, int sampleRate, int oversamplingValue, int capacityFramesValue)
        : channels(channelCount),
          displaySampleRate(sampleRate * oversamplingValue),
          oversampling(oversamplingValue),
          capacityFrames(capacityFramesValue),
          nativeSpectrumFft(new NativeSpectrumFft())
    {
        displaySamples.resize(static_cast<size_t>(capacityFramesValue * oversamplingValue), 0.0);
        channelDisplaySamples.resize(static_cast<size_t>(channelCount));
        for (auto& values : channelDisplaySamples)
        {
            values.resize(static_cast<size_t>(capacityFramesValue * oversamplingValue), 0.0);
        }
    }

    RecentSampleWindow::RecentSampleWindow(RecentSampleWindow&& other) noexcept
        : channels(other.channels),
          displaySampleRate(other.displaySampleRate),
          oversampling(other.oversampling),
          capacityFrames(other.capacityFrames),
          performanceSettings(other.performanceSettings),
          nativeSpectrumFft(other.nativeSpectrumFft),
          frames(std::move(other.frames)),
          recentFramePeaks(std::move(other.recentFramePeaks)),
          displaySamples(std::move(other.displaySamples)),
          channelDisplaySamples(std::move(other.channelDisplaySamples)),
          displayStartIndex(other.displayStartIndex),
          displaySampleCount(other.displaySampleCount),
          displayVersion(other.displayVersion),
          displayFrameVersion(other.displayFrameVersion),
          waveformVersion(other.waveformVersion),
          channelWaveformVersion(other.channelWaveformVersion),
          spectrumVersion(other.spectrumVersion),
          channelSpectrumVersion(other.channelSpectrumVersion),
          spectrumEligibleFrameVersion(other.spectrumEligibleFrameVersion),
          channelSpectrumEligibleFrameVersion(other.channelSpectrumEligibleFrameVersion),
          waveformCache(std::move(other.waveformCache)),
          channelWaveformCache(std::move(other.channelWaveformCache)),
          spectrumCache(std::move(other.spectrumCache)),
          channelSpectrumCache(std::move(other.channelSpectrumCache)),
          stereoImagePointsCache(std::move(other.stereoImagePointsCache)),
          stereoImagePointsVersion(other.stereoImagePointsVersion),
          fftInput(std::move(other.fftInput)),
          fftMagnitudes(std::move(other.fftMagnitudes)),
          fftWindow(std::move(other.fftWindow)),
          fftWindowSum(other.fftWindowSum),
          sampleSquareSum(other.sampleSquareSum),
          leftSquareSum(other.leftSquareSum),
          rightSquareSum(other.rightSquareSum),
          crossSum(other.crossSum),
          midSquareSum(other.midSquareSum),
          sideSquareSum(other.sideSquareSum),
          currentTruePeak(other.currentTruePeak)
    {
        other.nativeSpectrumFft = nullptr;
    }

    RecentSampleWindow& RecentSampleWindow::operator=(RecentSampleWindow&& other) noexcept
    {
        if (this == &other)
        {
            return *this;
        }

        delete nativeSpectrumFft;

        channels = other.channels;
        displaySampleRate = other.displaySampleRate;
        oversampling = other.oversampling;
        capacityFrames = other.capacityFrames;
        performanceSettings = other.performanceSettings;
        nativeSpectrumFft = other.nativeSpectrumFft;
        frames = std::move(other.frames);
        recentFramePeaks = std::move(other.recentFramePeaks);
        displaySamples = std::move(other.displaySamples);
        channelDisplaySamples = std::move(other.channelDisplaySamples);
        displayStartIndex = other.displayStartIndex;
        displaySampleCount = other.displaySampleCount;
        displayVersion = other.displayVersion;
        displayFrameVersion = other.displayFrameVersion;
        waveformVersion = other.waveformVersion;
        channelWaveformVersion = other.channelWaveformVersion;
        spectrumVersion = other.spectrumVersion;
        channelSpectrumVersion = other.channelSpectrumVersion;
        spectrumEligibleFrameVersion = other.spectrumEligibleFrameVersion;
        channelSpectrumEligibleFrameVersion = other.channelSpectrumEligibleFrameVersion;
        waveformCache = std::move(other.waveformCache);
        channelWaveformCache = std::move(other.channelWaveformCache);
        spectrumCache = std::move(other.spectrumCache);
        channelSpectrumCache = std::move(other.channelSpectrumCache);
        stereoImagePointsCache = std::move(other.stereoImagePointsCache);
        stereoImagePointsVersion = other.stereoImagePointsVersion;
        fftInput = std::move(other.fftInput);
        fftMagnitudes = std::move(other.fftMagnitudes);
        fftWindow = std::move(other.fftWindow);
        fftWindowSum = other.fftWindowSum;
        sampleSquareSum = other.sampleSquareSum;
        leftSquareSum = other.leftSquareSum;
        rightSquareSum = other.rightSquareSum;
        crossSum = other.crossSum;
        midSquareSum = other.midSquareSum;
        sideSquareSum = other.sideSquareSum;
        currentTruePeak = other.currentTruePeak;

        other.nativeSpectrumFft = nullptr;
        return *this;
    }

    RecentSampleWindow::~RecentSampleWindow()
    {
        delete nativeSpectrumFft;
    }

    bool RecentSampleWindow::IsFull() const
    {
        return static_cast<int>(frames.size()) == capacityFrames;
    }

    void RecentSampleWindow::SetPerformanceMode(PerformanceMode mode)
    {
        const PerformanceSettings next = SettingsForMode(mode);
        if (next.mode == performanceSettings.mode)
        {
            return;
        }

        performanceSettings = next;
        spectrumVersion = -1;
        channelSpectrumVersion = -1;
        spectrumEligibleFrameVersion = -1;
        channelSpectrumEligibleFrameVersion = -1;
    }

    void RecentSampleWindow::Add(
        const std::array<double, 2>& samples,
        double framePeak,
        const std::vector<double>& oversampledMonoSamples,
        const std::vector<double>& oversampledInterleavedSamples)
    {
        const double left = samples[0];
        const double right = channels == 1 ? samples[0] : samples[1];
        const double mid = (left + right) * 0.5;
        const double side = (left - right) * 0.5;
        const double leftMagnitude = std::abs(left);
        const double rightMagnitude = std::abs(right);
        const double magnitudeSum = leftMagnitude + rightMagnitude;
        const double polarAngle = channels == 1 || magnitudeSum <= std::numeric_limits<double>::epsilon()
            ? 0.0
            : Clamp((rightMagnitude - leftMagnitude) / magnitudeSum, -1.0, 1.0);
        const double polarRadius = Clamp(std::sqrt(((left * left) + (right * right)) / 2.0), 0.0, 1.0);

        EnqueueDisplaySamples(oversampledMonoSamples, oversampledInterleavedSamples);
        displayFrameVersion++;

        FrameMetrics metrics;
        metrics.sampleSquares = channels == 1 ? left * left : (left * left) + (right * right);
        metrics.leftSquare = left * left;
        metrics.rightSquare = right * right;
        metrics.cross = left * right;
        metrics.midSquare = mid * mid;
        metrics.sideSquare = side * side;
        metrics.polarAngle = polarAngle;
        metrics.polarRadius = polarRadius;

        frames.push_back(metrics);
        sampleSquareSum += metrics.sampleSquares;
        leftSquareSum += metrics.leftSquare;
        rightSquareSum += metrics.rightSquare;
        crossSum += metrics.cross;
        midSquareSum += metrics.midSquare;
        sideSquareSum += metrics.sideSquare;

        recentFramePeaks.push_back(framePeak);
        if (framePeak >= currentTruePeak)
        {
            currentTruePeak = framePeak;
        }

        bool peakNeedsRecompute = false;
        if (static_cast<int>(frames.size()) > capacityFrames)
        {
            const FrameMetrics removed = frames.front();
            frames.pop_front();
            sampleSquareSum -= removed.sampleSquares;
            leftSquareSum -= removed.leftSquare;
            rightSquareSum -= removed.rightSquare;
            crossSum -= removed.cross;
            midSquareSum -= removed.midSquare;
            sideSquareSum -= removed.sideSquare;

            const double removedPeak = recentFramePeaks.front();
            recentFramePeaks.pop_front();
            if (removedPeak >= currentTruePeak)
            {
                peakNeedsRecompute = true;
            }
        }

        if (peakNeedsRecompute)
        {
            currentTruePeak = 0.0;
            for (double peak : recentFramePeaks)
            {
                currentTruePeak = (std::max)(currentTruePeak, peak);
            }
        }
    }

    double RecentSampleWindow::Rms() const
    {
        const int sampleCount = static_cast<int>(frames.size()) * channels;
        return sampleCount == 0 ? 0.0 : std::sqrt(sampleSquareSum / sampleCount);
    }

    bool RecentSampleWindow::TryGetCorrelation(double& value) const
    {
        if (!IsFull() || channels < 2)
        {
            return false;
        }

        const double denominator = std::sqrt(leftSquareSum * rightSquareSum);
        value = denominator <= std::numeric_limits<double>::epsilon()
            ? 0.0
            : Clamp(crossSum / denominator, -1.0, 1.0);
        return true;
    }

    bool RecentSampleWindow::TryGetStereoImageX(double& value) const
    {
        if (!IsFull())
        {
            return false;
        }

        const double frameCount = static_cast<double>(frames.size());
        const double leftRms = std::sqrt(leftSquareSum / frameCount);
        const double rightRms = std::sqrt(rightSquareSum / frameCount);
        const double sum = leftRms + rightRms;
        value = sum <= std::numeric_limits<double>::epsilon()
            ? 0.0
            : Clamp((rightRms - leftRms) / sum, -1.0, 1.0);
        return true;
    }

    bool RecentSampleWindow::TryGetStereoImageY(double& value) const
    {
        if (!IsFull())
        {
            return false;
        }

        const double frameCount = static_cast<double>(frames.size());
        const double midRms = std::sqrt(midSquareSum / frameCount);
        const double sideRms = std::sqrt(sideSquareSum / frameCount);
        const double sum = midRms + sideRms;
        value = sum <= std::numeric_limits<double>::epsilon()
            ? 0.0
            : Clamp((midRms - sideRms) / sum, -1.0, 1.0);
        return true;
    }

    const std::vector<std::array<double, 2>>& RecentSampleWindow::GetStereoImagePoints()
    {
        if (!IsFull() || channels < 2)
        {
            stereoImagePointsCache.clear();
            return stereoImagePointsCache;
        }

        if (stereoImagePointsVersion == displayFrameVersion)
        {
            return stereoImagePointsCache;
        }

        const int stride = (std::max)(1, static_cast<int>(frames.size()) / MaximumStereoImagePoints);
        stereoImagePointsCache.clear();
        stereoImagePointsCache.reserve((std::min)(MaximumStereoImagePoints, static_cast<int>(frames.size())));
        int index = 0;
        for (const FrameMetrics& frame : frames)
        {
            if (index++ % stride == 0)
            {
                stereoImagePointsCache.push_back({ frame.polarAngle, frame.polarRadius });
            }
        }

        stereoImagePointsVersion = displayFrameVersion;
        return stereoImagePointsCache;
    }

    const std::vector<double>& RecentSampleWindow::GetWaveformSamples()
    {
        if (displaySampleCount == 0)
        {
            waveformCache.clear();
            return waveformCache;
        }

        if (waveformVersion == displayVersion)
        {
            return waveformCache;
        }

        FillWaveformBuffer(waveformCache, -1);
        waveformVersion = displayVersion;
        return waveformCache;
    }

    const std::vector<double>& RecentSampleWindow::GetChannelWaveformSamples(int channel)
    {
        if (channel < 0 || channel >= channels)
        {
            static const std::vector<double> empty;
            return empty;
        }

        if (displaySampleCount == 0)
        {
            if (static_cast<int>(channelWaveformCache.size()) <= channel)
            {
                channelWaveformCache.resize(static_cast<size_t>(channels));
            }

            channelWaveformCache[static_cast<size_t>(channel)].clear();
            return channelWaveformCache[static_cast<size_t>(channel)];
        }

        if (static_cast<int>(channelWaveformCache.size()) != channels)
        {
            channelWaveformCache.resize(static_cast<size_t>(channels));
        }

        if (channelWaveformVersion != displayVersion)
        {
            for (int index = 0; index < channels; ++index)
            {
                FillWaveformBuffer(channelWaveformCache[static_cast<size_t>(index)], index);
            }

            channelWaveformVersion = displayVersion;
        }

        return channelWaveformCache[static_cast<size_t>(channel)];
    }

    const std::vector<double>& RecentSampleWindow::GetSpectrumBuckets()
    {
        if (displaySampleCount < 128)
        {
            spectrumCache.clear();
            return spectrumCache;
        }

        if (spectrumVersion == displayVersion)
        {
            return spectrumCache;
        }

        if (!ShouldRefreshSpectrum(spectrumEligibleFrameVersion, !spectrumCache.empty()))
        {
            return spectrumCache;
        }

        FillSpectrumBuffer(spectrumCache, -1);
        spectrumVersion = displayVersion;
        return spectrumCache;
    }

    const std::vector<double>& RecentSampleWindow::GetChannelSpectrumBuckets(int channel)
    {
        if (channel < 0 || channel >= channels)
        {
            static const std::vector<double> empty;
            return empty;
        }

        if (displaySampleCount < 128)
        {
            if (static_cast<int>(channelSpectrumCache.size()) <= channel)
            {
                channelSpectrumCache.resize(static_cast<size_t>(channels));
            }

            channelSpectrumCache[static_cast<size_t>(channel)].clear();
            return channelSpectrumCache[static_cast<size_t>(channel)];
        }

        if (static_cast<int>(channelSpectrumCache.size()) != channels)
        {
            channelSpectrumCache.resize(static_cast<size_t>(channels));
        }

        if (channelSpectrumVersion == displayVersion)
        {
            return channelSpectrumCache[static_cast<size_t>(channel)];
        }

        if (!ShouldRefreshSpectrum(channelSpectrumEligibleFrameVersion, !channelSpectrumCache.empty() && !channelSpectrumCache.front().empty()))
        {
            return channelSpectrumCache[static_cast<size_t>(channel)];
        }

        for (int index = 0; index < channels; ++index)
        {
            FillSpectrumBuffer(channelSpectrumCache[static_cast<size_t>(index)], index);
        }

        channelSpectrumVersion = displayVersion;
        return channelSpectrumCache[static_cast<size_t>(channel)];
    }

    void RecentSampleWindow::Reset()
    {
        frames.clear();
        recentFramePeaks.clear();
        sampleSquareSum = 0.0;
        leftSquareSum = 0.0;
        rightSquareSum = 0.0;
        crossSum = 0.0;
        midSquareSum = 0.0;
        sideSquareSum = 0.0;
        currentTruePeak = 0.0;
        std::fill(displaySamples.begin(), displaySamples.end(), 0.0);
        for (auto& values : channelDisplaySamples)
        {
            std::fill(values.begin(), values.end(), 0.0);
        }

        displayStartIndex = 0;
        displaySampleCount = 0;
        displayVersion++;
        displayFrameVersion = 0;
        waveformVersion = -1;
        channelWaveformVersion = -1;
        spectrumVersion = -1;
        channelSpectrumVersion = -1;
        spectrumEligibleFrameVersion = -1;
        channelSpectrumEligibleFrameVersion = -1;
        stereoImagePointsVersion = -1;
        waveformCache.clear();
        channelWaveformCache.clear();
        spectrumCache.clear();
        channelSpectrumCache.clear();
        stereoImagePointsCache.clear();
        fftInput.clear();
        fftMagnitudes.clear();
        fftWindow.clear();
        fftWindowSum = 0.0;
    }

    void RecentSampleWindow::EnqueueDisplaySamples(
        const std::vector<double>& monoSamples,
        const std::vector<double>& interleavedChannelSamples)
    {
        const int channelFrameCount = static_cast<int>(interleavedChannelSamples.size()) / channels;
        for (int index = 0; index < static_cast<int>(monoSamples.size()); ++index)
        {
            const int slot = ReserveDisplaySampleSlot();
            const double mono = SanitizeSample(monoSamples[static_cast<size_t>(index)]);
            displaySamples[static_cast<size_t>(slot)] = mono;
            for (int channel = 0; channel < channels; ++channel)
            {
                const double channelSample = index < channelFrameCount
                    ? interleavedChannelSamples[static_cast<size_t>((index * channels) + channel)]
                    : mono;
                channelDisplaySamples[static_cast<size_t>(channel)][static_cast<size_t>(slot)] = SanitizeSample(channelSample);
            }

            displayVersion++;
        }
    }

    int RecentSampleWindow::ReserveDisplaySampleSlot()
    {
        if (displaySamples.empty())
        {
            return 0;
        }

        if (displaySampleCount < static_cast<int>(displaySamples.size()))
        {
            const int slot = (displayStartIndex + displaySampleCount) % static_cast<int>(displaySamples.size());
            displaySampleCount++;
            return slot;
        }

        const int overwriteSlot = displayStartIndex;
        displayStartIndex = (displayStartIndex + 1) % static_cast<int>(displaySamples.size());
        return overwriteSlot;
    }

    double RecentSampleWindow::GetDisplaySample(int offset) const
    {
        return displaySamples[static_cast<size_t>((displayStartIndex + offset) % static_cast<int>(displaySamples.size()))];
    }

    double RecentSampleWindow::GetChannelDisplaySample(int channel, int offset) const
    {
        return channelDisplaySamples[static_cast<size_t>(channel)][static_cast<size_t>((displayStartIndex + offset) % static_cast<int>(displaySamples.size()))];
    }

    void RecentSampleWindow::FillWaveformBuffer(std::vector<double>& target, int channelIndex)
    {
        target.clear();
        if (displaySampleCount <= MaximumWaveformPoints)
        {
            target.reserve(static_cast<size_t>(displaySampleCount));
            for (int index = 0; index < displaySampleCount; ++index)
            {
                const double sample = channelIndex >= 0
                    ? GetChannelDisplaySample(channelIndex, index)
                    : GetDisplaySample(index);
                target.push_back(Clamp(sample, -1.5, 1.5));
            }

            return;
        }

        const int bucketCount = MaximumWaveformPoints / 2;
        target.reserve(MaximumWaveformPoints);
        for (int bucket = 0; bucket < bucketCount; ++bucket)
        {
            const int start = bucket * displaySampleCount / bucketCount;
            const int end = (std::max)(start + 1, ((bucket + 1) * displaySampleCount) / bucketCount);
            double minimumValue = std::numeric_limits<double>::infinity();
            double maximumValue = -std::numeric_limits<double>::infinity();
            int minimumIndex = start;
            int maximumIndex = start;

            for (int index = start; index < end && index < displaySampleCount; ++index)
            {
                const double sample = channelIndex >= 0
                    ? GetChannelDisplaySample(channelIndex, index)
                    : GetDisplaySample(index);
                const double value = Clamp(sample, -1.5, 1.5);
                if (value < minimumValue)
                {
                    minimumValue = value;
                    minimumIndex = index;
                }

                if (value > maximumValue)
                {
                    maximumValue = value;
                    maximumIndex = index;
                }
            }

            if (minimumIndex <= maximumIndex)
            {
                target.push_back(minimumValue);
                target.push_back(maximumValue);
            }
            else
            {
                target.push_back(maximumValue);
                target.push_back(minimumValue);
            }
        }
    }

    void RecentSampleWindow::FillSpectrumBuffer(std::vector<double>& target, int channelIndex)
    {
        target.clear();
        const int fftSize = PreviousPowerOfTwo((std::min)(displaySampleCount, performanceSettings.spectrumAnalysisSamples));
        if (fftSize < 128)
        {
            return;
        }

        EnsureFftWorkspace(fftSize);
        const int start = displaySampleCount - fftSize;
        for (int index = 0; index < fftSize; ++index)
        {
            fftInput[static_cast<size_t>(index)] = channelIndex >= 0
                ? GetChannelDisplaySample(channelIndex, start + index)
                : GetDisplaySample(start + index);
        }

        nativeSpectrumFft->ComputeMagnitudes(fftInput, fftWindow, fftSize, fftWindowSum, fftMagnitudes);
        const int magnitudeCount = (fftSize / 2) + 1;
        const double minimumMel = HertzToMel(SpectrumMinimumFrequencyHz);
        const double maximumMel = HertzToMel((std::min)(SpectrumMaximumFrequencyHz, displaySampleRate * 0.49));
        target.resize(SpectrumBucketCount);
        for (int bucket = 0; bucket < SpectrumBucketCount; ++bucket)
        {
            const double ratio = SpectrumBucketCount == 1 ? 0.0 : static_cast<double>(bucket) / (SpectrumBucketCount - 1);
            const double frequency = MelToHertz(minimumMel + ((maximumMel - minimumMel) * ratio));
            const double binPosition = frequency * fftSize / displaySampleRate;
            target[static_cast<size_t>(bucket)] = MagnitudeToDisplayLevel(
                InterpolateMagnitude(fftMagnitudes, magnitudeCount, binPosition));
        }
    }

    bool RecentSampleWindow::ShouldRefreshSpectrum(long long& eligibleFrameVersion, bool hasCache)
    {
        if (!hasCache)
        {
            eligibleFrameVersion = displayFrameVersion;
            return true;
        }

        if ((displayFrameVersion - eligibleFrameVersion) < performanceSettings.spectrumRefreshIntervalFrames)
        {
            return false;
        }

        eligibleFrameVersion = displayFrameVersion;
        return true;
    }

    void RecentSampleWindow::EnsureFftWorkspace(int fftSize)
    {
        if (static_cast<int>(fftInput.size()) == fftSize)
        {
            return;
        }

        fftInput.resize(static_cast<size_t>(fftSize));
        fftMagnitudes.resize(static_cast<size_t>((fftSize / 2) + 1));
        fftWindow.resize(static_cast<size_t>(fftSize));
        fftWindowSum = 0.0;
        for (int index = 0; index < fftSize; ++index)
        {
            const double window = HannWindow(index, fftSize);
            fftWindow[static_cast<size_t>(index)] = window;
            fftWindowSum += window;
        }
    }

    double RecentSampleWindow::SanitizeSample(double sample)
    {
        return std::isfinite(sample) ? sample : 0.0;
    }
}
