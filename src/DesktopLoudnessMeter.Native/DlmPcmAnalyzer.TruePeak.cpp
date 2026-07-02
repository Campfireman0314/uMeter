#include "DlmPcmAnalyzerInternal.h"

#include "RfRapid/RfRapid.h"

#include <algorithm>
#include <cmath>

namespace dlm::pcm
{
    namespace
    {
        double Sinc(double value)
        {
            if (std::abs(value) < 1.0e-12)
            {
                return 1.0;
            }

            const double angle = Pi * value;
            return std::sin(angle) / angle;
        }

        double Hann(double distance)
        {
            return 0.5 + (0.5 * std::cos(Pi * distance / TruePeakKernelRadius));
        }

        double Reconstruct(const std::vector<double>& samples, const std::vector<double>& kernel)
        {
            return RF::RfRapidVec::dconv_dot_product(samples.data(), kernel.data(), kernel.size());
        }
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

    int EvaluateTruePeakSegment(
        DlmPcmAnalyzerHandle& analyzer,
        double rawFramePeak,
        double& framePeak)
    {
        framePeak = rawFramePeak;
        analyzer.latestOversampledMono.resize(static_cast<size_t>(analyzer.truePeakOversampling));
        analyzer.latestOversampledInterleaved.resize(static_cast<size_t>(analyzer.truePeakOversampling * analyzer.channels));

        for (int step = 0; step < analyzer.truePeakOversampling; ++step)
        {
            double mono = 0.0;
            for (int channel = 0; channel < analyzer.channels; ++channel)
            {
                const double reconstructed = Reconstruct(
                    analyzer.truePeakBuffers[static_cast<size_t>(channel)],
                    analyzer.truePeakKernels[static_cast<size_t>(step)]);
                mono += reconstructed;
                analyzer.latestOversampledInterleaved[static_cast<size_t>((step * analyzer.channels) + channel)] = reconstructed;
                if (step > 0)
                {
                    framePeak = (std::max)(framePeak, std::abs(reconstructed));
                }
            }

            analyzer.latestOversampledMono[static_cast<size_t>(step)] = mono / analyzer.channels;
        }

        return analyzer.truePeakOversampling;
    }

    void ShiftTruePeakBuffers(DlmPcmAnalyzerHandle& analyzer)
    {
        for (int channel = 0; channel < analyzer.channels; ++channel)
        {
            auto& buffer = analyzer.truePeakBuffers[static_cast<size_t>(channel)];
            std::move(buffer.begin() + 1, buffer.end(), buffer.begin());
            analyzer.truePeakBufferCounts[static_cast<size_t>(channel)] = TruePeakBufferLength - 1;
        }
    }
}
