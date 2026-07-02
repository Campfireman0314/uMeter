#include "DlmPcmAnalyzerInternal.h"

#include <algorithm>
#include <cmath>
#include <limits>

namespace dlm::pcm
{
    bool IsSupportedOversampling(int value)
    {
        return value == 2 || value == 4 || value == 8;
    }

    PerformanceSettings SettingsForMode(PerformanceMode mode)
    {
        return mode == PerformanceMode::Eco
            ? PerformanceSettings{ PerformanceMode::Eco, 4096, 8192 }
            : PerformanceSettings{ PerformanceMode::Normal, 1024, 16384 };
    }

    double ToDb(double amplitude)
    {
        return amplitude <= 0.0
            ? -std::numeric_limits<double>::infinity()
            : 20.0 * std::log10(amplitude);
    }

    double MeanSquareToLufs(double meanSquare)
    {
        return meanSquare <= 0.0
            ? -std::numeric_limits<double>::infinity()
            : LufsOffset + (10.0 * std::log10(meanSquare));
    }

    double LufsToMeanSquare(double lufs)
    {
        return std::pow(10.0, (lufs - LufsOffset) / 10.0);
    }

    double NormalizeSample(float sample)
    {
        return std::isfinite(sample) ? static_cast<double>(sample) : 0.0;
    }

    double Clamp(double value, double minValue, double maxValue)
    {
        return value < minValue ? minValue : (value > maxValue ? maxValue : value);
    }

    int MillisecondsToFrames(int sampleRate, int milliseconds)
    {
        const double frames = static_cast<double>(sampleRate) * milliseconds / 1000.0;
        return (std::max)(1, static_cast<int>(std::llround(frames)));
    }

    double BiquadFilter::Process(double input)
    {
        const double output = (b0 * input) + (b1 * x1) + (b2 * x2) - (a1 * y1) - (a2 * y2);
        x2 = x1;
        x1 = input;
        y2 = y1;
        y1 = output;
        return output;
    }

    void BiquadFilter::Reset()
    {
        x1 = 0.0;
        x2 = 0.0;
        y1 = 0.0;
        y2 = 0.0;
    }

    double KWeightingFilter::Process(double sample)
    {
        return rlbFilter.Process(preFilter.Process(sample));
    }

    void KWeightingFilter::Reset()
    {
        preFilter.Reset();
        rlbFilter.Reset();
    }

    SlidingPowerWindow::SlidingPowerWindow(int capacityValue)
        : capacity(capacityValue)
    {
    }

    bool SlidingPowerWindow::IsFull() const
    {
        return static_cast<int>(values.size()) == capacity;
    }

    double SlidingPowerWindow::Mean() const
    {
        return IsFull() && capacity > 0 ? sum / capacity : 0.0;
    }

    void SlidingPowerWindow::Add(double power)
    {
        values.push_back(power);
        sum += power;
        if (static_cast<int>(values.size()) > capacity)
        {
            sum -= values.front();
            values.pop_front();
        }
    }

    void SlidingPowerWindow::Reset()
    {
        values.clear();
        sum = 0.0;
    }

    IntegratedLoudnessGate::IntegratedLoudnessGate(int blockFramesValue, int hopFramesValue)
        : blockFrames(blockFramesValue),
          hopFrames(hopFramesValue)
    {
    }

    void IntegratedLoudnessGate::Add(double power)
    {
        window.push_back(power);
        windowSum += power;
        framesSinceHop++;

        if (static_cast<int>(window.size()) > blockFrames)
        {
            windowSum -= window.front();
            window.pop_front();
        }

        if (static_cast<int>(window.size()) == blockFrames && framesSinceHop >= hopFrames)
        {
            blockPowers.push_back(windowSum / blockFrames);
            framesSinceHop = 0;
        }
    }

    bool IntegratedLoudnessGate::TryCalculateLufs(double& lufs) const
    {
        const double absoluteGatePower = LufsToMeanSquare(AbsoluteGateLufs);
        double absoluteGatedSum = 0.0;
        int absoluteGatedCount = 0;
        for (double power : blockPowers)
        {
            if (power <= absoluteGatePower)
            {
                continue;
            }

            absoluteGatedSum += power;
            absoluteGatedCount++;
        }

        if (absoluteGatedCount == 0)
        {
            return false;
        }

        const double preliminaryMean = absoluteGatedSum / absoluteGatedCount;
        const double relativeGate = MeanSquareToLufs(preliminaryMean) + RelativeGateLu;
        const double relativeGatePower = LufsToMeanSquare(relativeGate);
        double gatedSum = 0.0;
        int gatedCount = 0;
        for (double power : blockPowers)
        {
            if (power <= absoluteGatePower || power <= relativeGatePower)
            {
                continue;
            }

            gatedSum += power;
            gatedCount++;
        }

        if (gatedCount == 0)
        {
            return false;
        }

        lufs = MeanSquareToLufs(gatedSum / gatedCount);
        return true;
    }

    void IntegratedLoudnessGate::Reset()
    {
        window.clear();
        blockPowers.clear();
        windowSum = 0.0;
        framesSinceHop = 0;
    }
}
