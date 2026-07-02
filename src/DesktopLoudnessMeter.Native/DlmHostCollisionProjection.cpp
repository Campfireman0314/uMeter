#include "DlmHostCollisionProjection.h"

#include <algorithm>
#include <cmath>
#include <vector>

namespace
{
    constexpr double DisplayFloorDb = -200.0;
    constexpr double DisplayRangeDb = 200.0;
    constexpr int MaximumProminenceRadius = 8;
    constexpr double ComparisonTolerance = 1.0e-9;
    constexpr double MinimumFrequencyHz = 20.0;
    constexpr double MaximumFrequencyHz = 22000.0;
    constexpr int CollisionFrequencyScaleMel = 0;
    constexpr int CollisionFrequencyScaleLogarithmic = 1;

    struct CollisionBand
    {
        double startRatio = 0.0;
        double endRatio = 0.0;
        double intensity = 0.0;
    };

    struct DlmHostCollisionProjectionHandle
    {
        std::vector<double> focus;
        std::vector<std::vector<double>> comparisons;
        std::vector<CollisionBand> bands;
    };

    DlmHostCollisionProjectionHandle* AsProjection(void* handle)
    {
        return static_cast<DlmHostCollisionProjectionHandle*>(handle);
    }

    double Clamp(double value, double minimum, double maximum)
    {
        return (std::min)((std::max)(value, minimum), maximum);
    }

    double HertzToMel(double hertz)
    {
        return 2595.0 * std::log10(1.0 + (hertz / 700.0));
    }

    double MelToHertz(double mel)
    {
        return 700.0 * (std::pow(10.0, mel / 2595.0) - 1.0);
    }

    double MinimumMel()
    {
        return HertzToMel(MinimumFrequencyHz);
    }

    double MaximumMel()
    {
        return HertzToMel(MaximumFrequencyHz);
    }

    double LogDomain()
    {
        return std::log(MaximumFrequencyHz / MinimumFrequencyHz);
    }

    double DbToDisplayLevel(double decibels)
    {
        return Clamp((decibels - DisplayFloorDb) / DisplayRangeDb, 0.0, 1.0);
    }

    double BucketRatioToFrequency(double bucketRatio)
    {
        const double ratio = Clamp(bucketRatio, 0.0, 1.0);
        const double mel = MinimumMel() + ((MaximumMel() - MinimumMel()) * ratio);
        return MelToHertz(mel);
    }

    double FrequencyToScaleRatio(double frequency, int scale)
    {
        const double clamped = Clamp(frequency, MinimumFrequencyHz, MaximumFrequencyHz);
        if (scale == CollisionFrequencyScaleLogarithmic)
        {
            return Clamp(std::log(clamped / MinimumFrequencyHz) / LogDomain(), 0.0, 1.0);
        }

        return Clamp((HertzToMel(clamped) - MinimumMel()) / (MaximumMel() - MinimumMel()), 0.0, 1.0);
    }

    double BucketRatioToScaleRatio(double bucketRatio, int scale)
    {
        return FrequencyToScaleRatio(BucketRatioToFrequency(bucketRatio), scale);
    }

    int GetProminenceRadius(int bucketCount)
    {
        return (std::min)((std::max)(bucketCount / 32, 1), MaximumProminenceRadius);
    }

    double EstimateLocalBackground(
        const std::vector<double>& levels,
        int bucketCount,
        int centerIndex,
        int radius)
    {
        const int start = (std::max)(0, centerIndex - radius);
        const int end = (std::min)(bucketCount - 1, centerIndex + radius);
        double sum = 0.0;
        int count = 0;

        for (int index = start; index <= end; ++index)
        {
            if (index == centerIndex)
            {
                continue;
            }

            sum += Clamp(levels[static_cast<size_t>(index)], 0.0, 1.0);
            ++count;
        }

        return count == 0 ? 0.0 : sum / count;
    }

    std::vector<double> CalculateProminenceScores(
        const std::vector<double>& levels,
        int bucketCount,
        double safeLevelFloor,
        double prominenceThresholdDb)
    {
        std::vector<double> scores(static_cast<size_t>(bucketCount));
        if (bucketCount < 3 || prominenceThresholdDb <= 0.0)
        {
            std::fill(scores.begin(), scores.end(), 1.0);
            return scores;
        }

        const int radius = GetProminenceRadius(bucketCount);
        for (int index = 0; index < bucketCount; ++index)
        {
            const double level = Clamp(levels[static_cast<size_t>(index)], 0.0, 1.0);
            if (level + ComparisonTolerance < safeLevelFloor)
            {
                continue;
            }

            const double localBackground = EstimateLocalBackground(levels, bucketCount, index, radius);
            const double prominenceDb = (level - localBackground) * DisplayRangeDb;
            if (prominenceDb + ComparisonTolerance < prominenceThresholdDb)
            {
                continue;
            }

            scores[static_cast<size_t>(index)] = Clamp(prominenceDb / (prominenceThresholdDb * 2.0), 0.0, 1.0);
        }

        return scores;
    }

    void AnalyzePair(
        const std::vector<double>& first,
        const std::vector<double>& second,
        int bucketCount,
        double safeLevelFloor,
        double safeRelativeThreshold,
        const std::vector<double>& firstProminence,
        const std::vector<double>& secondProminence,
        std::vector<double>& collisions)
    {
        for (int index = 0; index < bucketCount; ++index)
        {
            const double firstLevel = Clamp(first[static_cast<size_t>(index)], 0.0, 1.0);
            const double secondLevel = Clamp(second[static_cast<size_t>(index)], 0.0, 1.0);
            const double stronger = (std::max)(firstLevel, secondLevel);
            const double weaker = (std::min)(firstLevel, secondLevel);
            if (weaker + ComparisonTolerance < safeLevelFloor)
            {
                continue;
            }

            const double prominenceFit = (std::min)(
                firstProminence[static_cast<size_t>(index)],
                secondProminence[static_cast<size_t>(index)]);
            if (prominenceFit <= 0.0)
            {
                continue;
            }

            const double displayGap = std::abs(firstLevel - secondLevel);
            const double relativeGap = displayGap / (std::max)(stronger, safeLevelFloor);
            if (relativeGap > safeRelativeThreshold + ComparisonTolerance)
            {
                continue;
            }

            const double relativeFit = 1.0 - (relativeGap / safeRelativeThreshold);
            const double intensity = Clamp(relativeFit * prominenceFit * stronger, 0.0, 1.0);
            auto& current = collisions[static_cast<size_t>(index)];
            current = (std::max)(current, intensity);
        }
    }

    void CompactBands(
        const std::vector<double>& collisions,
        int frequencyScale,
        std::vector<CollisionBand>& bands)
    {
        int start = -1;
        double strongest = 0.0;

        for (int index = 0; index <= static_cast<int>(collisions.size()); ++index)
        {
            const double value = index < static_cast<int>(collisions.size())
                ? collisions[static_cast<size_t>(index)]
                : 0.0;
            if (value > 0.0)
            {
                if (start < 0)
                {
                    start = index;
                    strongest = value;
                }
                else
                {
                    strongest = (std::max)(strongest, value);
                }

                continue;
            }

            if (start < 0)
            {
                continue;
            }

            const auto count = static_cast<double>(collisions.size());
            bands.push_back(CollisionBand
            {
                BucketRatioToScaleRatio(static_cast<double>(start) / count, frequencyScale),
                BucketRatioToScaleRatio(static_cast<double>(index) / count, frequencyScale),
                Clamp(strongest, 0.0, 1.0)
            });
            start = -1;
            strongest = 0.0;
        }
    }

    int GetBucketCount(const DlmHostCollisionProjectionHandle& projection)
    {
        if (projection.focus.empty() || projection.comparisons.empty())
        {
            return 0;
        }

        auto count = static_cast<int>(projection.focus.size());
        for (const auto& comparison : projection.comparisons)
        {
            if (comparison.empty())
            {
                continue;
            }

            count = (std::min)(count, static_cast<int>(comparison.size()));
        }

        return count;
    }
}

int DlmHostCollisionProjectionCreate(void** handle)
{
    if (handle == nullptr)
    {
        return -2;
    }

    *handle = nullptr;
    try
    {
        *handle = new DlmHostCollisionProjectionHandle();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

void DlmHostCollisionProjectionDestroy(void* handle)
{
    delete AsProjection(handle);
}

int DlmHostCollisionProjectionClear(void* handle)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    projection->focus.clear();
    projection->comparisons.clear();
    projection->bands.clear();
    return 0;
}

int DlmHostCollisionProjectionSetFocusSpectrum(void* handle, const double* values, int count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count < 0 || (count > 0 && values == nullptr))
    {
        return -2;
    }

    try
    {
        projection->focus.assign(values, values + count);
        projection->bands.clear();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostCollisionProjectionAddComparisonSpectrum(void* handle, const double* values, int count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count < 0 || (count > 0 && values == nullptr))
    {
        return -2;
    }

    try
    {
        if (count > 0)
        {
            projection->comparisons.emplace_back(values, values + count);
        }

        projection->bands.clear();
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostCollisionProjectionProject(
    void* handle,
    double levelFloorDb,
    double prominenceThresholdDb,
    double relativeThreshold,
    int frequencyScale)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr)
    {
        return -2;
    }

    try
    {
        projection->bands.clear();
        const int bucketCount = GetBucketCount(*projection);
        if (bucketCount <= 0)
        {
            return 0;
        }

        const double safeLevelFloorDb = Clamp(levelFloorDb, DisplayFloorDb, 0.0);
        const double safeLevelFloor = DbToDisplayLevel(safeLevelFloorDb);
        const double safeProminenceThresholdDb = Clamp(prominenceThresholdDb, 0.0, 48.0);
        const double safeRelativeThreshold = (std::max)(relativeThreshold, 0.001);
        const auto focusProminence = CalculateProminenceScores(
            projection->focus,
            bucketCount,
            safeLevelFloor,
            safeProminenceThresholdDb);
        std::vector<double> collisions(static_cast<size_t>(bucketCount));

        for (const auto& comparison : projection->comparisons)
        {
            if (comparison.empty())
            {
                continue;
            }

            const auto comparisonProminence = CalculateProminenceScores(
                comparison,
                bucketCount,
                safeLevelFloor,
                safeProminenceThresholdDb);
            AnalyzePair(
                projection->focus,
                comparison,
                bucketCount,
                safeLevelFloor,
                safeRelativeThreshold,
                focusProminence,
                comparisonProminence,
                collisions);
        }

        CompactBands(collisions, frequencyScale, projection->bands);
        return 0;
    }
    catch (...)
    {
        return -4;
    }
}

int DlmHostCollisionProjectionGetBandCount(void* handle, int* count)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || count == nullptr)
    {
        return -2;
    }

    *count = static_cast<int>(projection->bands.size());
    return 0;
}

int DlmHostCollisionProjectionGetBandAt(void* handle, int index, DlmHostCollisionBand* band)
{
    auto* projection = AsProjection(handle);
    if (projection == nullptr || band == nullptr || index < 0 || index >= static_cast<int>(projection->bands.size()))
    {
        return -2;
    }

    const auto& source = projection->bands[static_cast<size_t>(index)];
    band->startRatio = source.startRatio;
    band->endRatio = source.endRatio;
    band->intensity = source.intensity;
    return 0;
}
