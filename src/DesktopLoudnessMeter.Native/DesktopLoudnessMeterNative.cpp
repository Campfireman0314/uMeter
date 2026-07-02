#include "DlmPcmAnalyzer.h"
#include "DlmHostAggregateSnapshot.h"
#include "DlmHostCollisionProjection.h"
#include "DlmHostDetailModuleProjection.h"
#include "DlmHostChartSettingsCommand.h"
#include "DlmHostGlobalSettingsCommand.h"
#include "DlmHostRecentEvents.h"
#include "DlmHostRuntimeState.h"
#include "DlmHostRuntimeServiceCommand.h"
#include "DlmHostSelectedDetailSnapshot.h"
#include "DlmHostSelectedDetailShell.h"
#include "DlmHostSelectedSourceCommand.h"
#include "DlmHostSnapshotChromeProjection.h"
#include "DlmHostSourceOrderCommand.h"
#include "DlmHostSourcePreferenceCommand.h"
#include "DlmHostSourceProjection.h"
#include "DlmHostSourceTargetCommand.h"
#include "DlmLoopbackProtocol.h"
#include "DlmSourceIngestCore.h"
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

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCreateExport(
    int sampleRate,
    int channels,
    int truePeakOversampling,
    void** handle)
{
    return DlmPcmAnalyzerCreate(sampleRate, channels, truePeakOversampling, handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmPcmAnalyzerDestroyExport(void* handle)
{
    DlmPcmAnalyzerDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerResetExport(void* handle)
{
    return DlmPcmAnalyzerReset(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerProcessF32Export(
    void* handle,
    const float* interleavedSamples,
    int sampleCount)
{
    return DlmPcmAnalyzerProcessF32(handle, interleavedSamples, sampleCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerGetSnapshotExport(
    void* handle,
    DlmPcmAnalyzerSnapshot* snapshot)
{
    return DlmPcmAnalyzerGetSnapshot(handle, snapshot);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerSetChannelWaveformEnabledExport(
    void* handle,
    int enabled)
{
    return DlmPcmAnalyzerSetChannelWaveformEnabled(handle, enabled);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerSetChannelSpectrumEnabledExport(
    void* handle,
    int enabled)
{
    return DlmPcmAnalyzerSetChannelSpectrumEnabled(handle, enabled);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerSetPerformanceModeExport(
    void* handle,
    int mode)
{
    return DlmPcmAnalyzerSetPerformanceMode(handle, mode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCopyWaveformExport(
    void* handle,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyWaveform(handle, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCopyChannelWaveformExport(
    void* handle,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyChannelWaveform(handle, channel, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCopySpectrumExport(
    void* handle,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopySpectrum(handle, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCopyChannelSpectrumExport(
    void* handle,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyChannelSpectrum(handle, channel, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmPcmAnalyzerCopyStereoImagePointsExport(
    void* handle,
    double* angleBuffer,
    double* radiusBuffer,
    int capacity,
    int* actualCount)
{
    return DlmPcmAnalyzerCopyStereoImagePoints(handle, angleBuffer, radiusBuffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCreateExport(void** handle)
{
    return DlmSourceIngestCoreCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotCreateExport(void** handle)
{
    return DlmHostAggregateSnapshotCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostAggregateSnapshotDestroyExport(void* handle)
{
    DlmHostAggregateSnapshotDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotClearExport(void* handle)
{
    return DlmHostAggregateSnapshotClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostAggregateSnapshotAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotProjectExport(
    void* handle,
    const char* selectedSourceKey)
{
    return DlmHostAggregateSnapshotProject(handle, selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotGetPlanExport(
    void* handle,
    DlmHostAggregateSnapshotPlan* plan)
{
    return DlmHostAggregateSnapshotGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostAggregateSnapshotCopyStatusTextExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostAggregateSnapshotCopyStatusText(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsCreateExport(void** handle)
{
    return DlmHostRecentEventsCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionCreateExport(void** handle)
{
    return DlmHostDetailModuleProjectionCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionCreateExport(void** handle)
{
    return DlmHostSnapshotChromeProjectionCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionCreateExport(void** handle)
{
    return DlmHostCollisionProjectionCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostCollisionProjectionDestroyExport(void* handle)
{
    DlmHostCollisionProjectionDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionClearExport(void* handle)
{
    return DlmHostCollisionProjectionClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionSetFocusSpectrumExport(
    void* handle,
    const double* values,
    int count)
{
    return DlmHostCollisionProjectionSetFocusSpectrum(handle, values, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionAddComparisonSpectrumExport(
    void* handle,
    const double* values,
    int count)
{
    return DlmHostCollisionProjectionAddComparisonSpectrum(handle, values, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionProjectExport(
    void* handle,
    double levelFloorDb,
    double prominenceThresholdDb,
    double relativeThreshold,
    int frequencyScale)
{
    return DlmHostCollisionProjectionProject(
        handle,
        levelFloorDb,
        prominenceThresholdDb,
        relativeThreshold,
        frequencyScale);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionGetBandCountExport(
    void* handle,
    int* count)
{
    return DlmHostCollisionProjectionGetBandCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostCollisionProjectionGetBandAtExport(
    void* handle,
    int index,
    DlmHostCollisionBand* band)
{
    return DlmHostCollisionProjectionGetBandAt(handle, index, band);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionDestroyExport(void* handle)
{
    DlmHostSnapshotChromeProjectionDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionClearExport(void* handle)
{
    return DlmHostSnapshotChromeProjectionClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionSetGlobalSettingsExport(
    void* handle,
    int schemaVersion,
    const char* themeId,
    int hasMaxRecentEventEntries,
    int maxRecentEventEntries,
    const char* eventOutputFolder,
    int loadDefaultDetailLayoutFromFile,
    const char* defaultDetailLayoutFile,
    int showHiddenSources)
{
    return DlmHostSnapshotChromeProjectionSetGlobalSettings(
        handle,
        schemaVersion,
        themeId,
        hasMaxRecentEventEntries,
        maxRecentEventEntries,
        eventOutputFolder,
        loadDefaultDetailLayoutFromFile,
        defaultDetailLayoutFile,
        showHiddenSources);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionAddSourceSettingsExport(
    void* handle,
    const char* sourceKey,
    const char* alias,
    int isHidden,
    int hasOrderIndex,
    int orderIndex,
    int hasTruePeakOversampling,
    int truePeakOversampling,
    int hasWaveformDisplayMode,
    int waveformDisplayMode,
    int hasSpectrumDisplayMode,
    int spectrumDisplayMode,
    int hasInlineStereoVisible,
    int inlineStereoVisible,
    int hasInlineWaveformVisible,
    int inlineWaveformVisible,
    int hasInlineSpectrumVisible,
    int inlineSpectrumVisible,
    int hasInlineLoudnessClockVisible,
    int inlineLoudnessClockVisible,
    int hasInlineCollisionVisible,
    int inlineCollisionVisible,
    const char* inlineCollisionSourceKey,
    int hasCollisionLevelFloorDb,
    double collisionLevelFloorDb,
    int hasCollisionProminenceThresholdDb,
    double collisionProminenceThresholdDb,
    int hasCollisionRelativeThreshold,
    double collisionRelativeThreshold,
    int hasCollisionFrequencyScale,
    int collisionFrequencyScale,
    int hasSpectrumDisplayFloorDb,
    double spectrumDisplayFloorDb,
    int hasSpectrumDisplayCeilingDb,
    double spectrumDisplayCeilingDb,
    int hasLoudnessClockMinimumDb,
    double loudnessClockMinimumDb,
    int hasLoudnessClockMaximumDb,
    double loudnessClockMaximumDb,
    int hasLoudnessClockWarningDb,
    double loudnessClockWarningDb,
    int hasLoudnessClockCycleSeconds,
    double loudnessClockCycleSeconds)
{
    return DlmHostSnapshotChromeProjectionAddSourceSettings(
        handle,
        sourceKey,
        alias,
        isHidden,
        hasOrderIndex,
        orderIndex,
        hasTruePeakOversampling,
        truePeakOversampling,
        hasWaveformDisplayMode,
        waveformDisplayMode,
        hasSpectrumDisplayMode,
        spectrumDisplayMode,
        hasInlineStereoVisible,
        inlineStereoVisible,
        hasInlineWaveformVisible,
        inlineWaveformVisible,
        hasInlineSpectrumVisible,
        inlineSpectrumVisible,
        hasInlineLoudnessClockVisible,
        inlineLoudnessClockVisible,
        hasInlineCollisionVisible,
        inlineCollisionVisible,
        inlineCollisionSourceKey,
        hasCollisionLevelFloorDb,
        collisionLevelFloorDb,
        hasCollisionProminenceThresholdDb,
        collisionProminenceThresholdDb,
        hasCollisionRelativeThreshold,
        collisionRelativeThreshold,
        hasCollisionFrequencyScale,
        collisionFrequencyScale,
        hasSpectrumDisplayFloorDb,
        spectrumDisplayFloorDb,
        hasSpectrumDisplayCeilingDb,
        spectrumDisplayCeilingDb,
        hasLoudnessClockMinimumDb,
        loudnessClockMinimumDb,
        hasLoudnessClockMaximumDb,
        loudnessClockMaximumDb,
        hasLoudnessClockWarningDb,
        loudnessClockWarningDb,
        hasLoudnessClockCycleSeconds,
        loudnessClockCycleSeconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionProjectExport(
    void* handle,
    int hasSelectedSourceIndex,
    int selectedSourceIndex,
    int sourceCount)
{
    return DlmHostSnapshotChromeProjectionProject(handle, hasSelectedSourceIndex, selectedSourceIndex, sourceCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionGetSettingsExport(
    void* handle,
    DlmHostSnapshotChromeSettings* settings)
{
    return DlmHostSnapshotChromeProjectionGetSettings(handle, settings);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionGetSourceSettingsCountExport(
    void* handle,
    int* count)
{
    return DlmHostSnapshotChromeProjectionGetSourceSettingsCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionGetSourceSettingsAtExport(
    void* handle,
    int index,
    DlmHostSnapshotChromeSourceSettingsRow* row)
{
    return DlmHostSnapshotChromeProjectionGetSourceSettingsAt(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionGetCommandAvailabilityExport(
    void* handle,
    DlmHostSnapshotChromeCommandAvailability* commands)
{
    return DlmHostSnapshotChromeProjectionGetCommandAvailability(handle, commands);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionCopySettingsStringExport(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSnapshotChromeProjectionCopySettingsString(handle, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSnapshotChromeProjectionCopySourceSettingsStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSnapshotChromeProjectionCopySourceSettingsStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostDetailModuleProjectionDestroyExport(void* handle)
{
    DlmHostDetailModuleProjectionDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionClearExport(void* handle)
{
    return DlmHostDetailModuleProjectionClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionAddLayoutExport(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height)
{
    return DlmHostDetailModuleProjectionAddLayout(handle, kind, orderIndex, hasHeight, height);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionProjectExport(
    void* handle,
    int hasStereoVisible,
    int stereoVisible,
    int hasCollisionVisible,
    int collisionVisible,
    int hasLoudnessClockVisible,
    int loudnessClockVisible,
    int hasWaveformVisible,
    int waveformVisible,
    int hasSpectrumVisible,
    int spectrumVisible)
{
    return DlmHostDetailModuleProjectionProject(
        handle,
        hasStereoVisible,
        stereoVisible,
        hasCollisionVisible,
        collisionVisible,
        hasLoudnessClockVisible,
        loudnessClockVisible,
        hasWaveformVisible,
        waveformVisible,
        hasSpectrumVisible,
        spectrumVisible);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionGetCountExport(
    void* handle,
    int* count)
{
    return DlmHostDetailModuleProjectionGetCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionGetAtExport(
    void* handle,
    int index,
    DlmHostDetailModuleProjectionRow* row)
{
    return DlmHostDetailModuleProjectionGetAt(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostDetailModuleProjectionCopyStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostDetailModuleProjectionCopyStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostRecentEventsDestroyExport(void* handle)
{
    DlmHostRecentEventsDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsClearCurrentSourcesExport(void* handle)
{
    return DlmHostRecentEventsClearCurrentSources(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsAddLiveSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostRecentEventsAddLiveSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsAddVisibleSourceExport(
    void* handle,
    const char* sourceKey,
    const char* displayTitle)
{
    return DlmHostRecentEventsAddVisibleSource(handle, sourceKey, displayTitle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsReconcileExport(
    void* handle,
    const char* timeText,
    int maxRecentEventEntries)
{
    return DlmHostRecentEventsReconcile(handle, timeText, maxRecentEventEntries);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsGetCountExport(
    void* handle,
    int* count)
{
    return DlmHostRecentEventsGetCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsGetAtExport(
    void* handle,
    int index,
    DlmHostRecentEventRow* row)
{
    return DlmHostRecentEventsGetAt(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRecentEventsCopyStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostRecentEventsCopyStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionCreateExport(void** handle)
{
    return DlmHostSourceProjectionCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandCreateExport(void** handle)
{
    return DlmHostSourceOrderCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSourceOrderCommandDestroyExport(void* handle)
{
    DlmHostSourceOrderCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandClearExport(void* handle)
{
    return DlmHostSourceOrderCommandClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSourceOrderCommandAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandPlanMoveExport(
    void* handle,
    const char* sourceKey,
    int direction)
{
    return DlmHostSourceOrderCommandPlanMove(handle, sourceKey, direction);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandPlanMoveToIndexExport(
    void* handle,
    const char* sourceKey,
    int targetIndex)
{
    return DlmHostSourceOrderCommandPlanMoveToIndex(handle, sourceKey, targetIndex);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandGetStatusExport(
    void* handle,
    int* status)
{
    return DlmHostSourceOrderCommandGetStatus(handle, status);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandGetRowCountExport(
    void* handle,
    int* count)
{
    return DlmHostSourceOrderCommandGetRowCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandGetRowExport(
    void* handle,
    int index,
    DlmHostSourceOrderCommandRow* row)
{
    return DlmHostSourceOrderCommandGetRow(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceOrderCommandCopyRowSourceKeyExport(
    void* handle,
    int index,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSourceOrderCommandCopyRowSourceKey(handle, index, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandCreateExport(void** handle)
{
    return DlmHostSourcePreferenceCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSourcePreferenceCommandDestroyExport(void* handle)
{
    DlmHostSourcePreferenceCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandClearSourcesExport(void* handle)
{
    return DlmHostSourcePreferenceCommandClearSources(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSourcePreferenceCommandAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandPlanSetAliasExport(
    void* handle,
    const char* sourceKey,
    const char* alias)
{
    return DlmHostSourcePreferenceCommandPlanSetAlias(handle, sourceKey, alias);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandPlanSetHiddenExport(
    void* handle,
    const char* sourceKey,
    int isHidden)
{
    return DlmHostSourcePreferenceCommandPlanSetHidden(handle, sourceKey, isHidden);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandPlanForgetExport(
    void* handle,
    const char* sourceKey,
    const char* selectedSourceKey)
{
    return DlmHostSourcePreferenceCommandPlanForget(handle, sourceKey, selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandGetPlanExport(
    void* handle,
    DlmHostSourcePreferenceCommandPlan* plan)
{
    return DlmHostSourcePreferenceCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourcePreferenceCommandCopyAliasExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSourcePreferenceCommandCopyAlias(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandCreateExport(void** handle)
{
    return DlmHostGlobalSettingsCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostGlobalSettingsCommandDestroyExport(void* handle)
{
    DlmHostGlobalSettingsCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandPlanSetThemeExport(
    void* handle,
    const char* themeId)
{
    return DlmHostGlobalSettingsCommandPlanSetTheme(handle, themeId);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandPlanSetRecentEventLimitExport(
    void* handle,
    int maxRecentEventEntries)
{
    return DlmHostGlobalSettingsCommandPlanSetRecentEventLimit(handle, maxRecentEventEntries);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandPlanSetEventOutputFolderExport(
    void* handle,
    const char* folder)
{
    return DlmHostGlobalSettingsCommandPlanSetEventOutputFolder(handle, folder);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandPlanSetShowHiddenSourcesExport(
    void* handle,
    int showHiddenSources)
{
    return DlmHostGlobalSettingsCommandPlanSetShowHiddenSources(handle, showHiddenSources);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandGetPlanExport(
    void* handle,
    DlmHostGlobalSettingsCommandPlan* plan)
{
    return DlmHostGlobalSettingsCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandCopyThemeIdExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostGlobalSettingsCommandCopyThemeId(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostGlobalSettingsCommandCopyEventOutputFolderExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostGlobalSettingsCommandCopyEventOutputFolder(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandCreateExport(void** handle)
{
    return DlmHostRuntimeServiceCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateCreateExport(void** handle)
{
    return DlmHostRuntimeStateCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostRuntimeStateDestroyExport(void* handle)
{
    DlmHostRuntimeStateDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateSetSelectedSourceKeyExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostRuntimeStateSetSelectedSourceKey(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateClearSelectedSourceKeyExport(void* handle)
{
    return DlmHostRuntimeStateClearSelectedSourceKey(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateSetShowHiddenSourcesExport(
    void* handle,
    int showHiddenSources)
{
    return DlmHostRuntimeStateSetShowHiddenSources(handle, showHiddenSources);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateSetPerformanceModeExport(
    void* handle,
    int hostPerformanceMode)
{
    return DlmHostRuntimeStateSetPerformanceMode(handle, hostPerformanceMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateApplySelectedSourcePlanExport(
    void* handle,
    int shouldClearSelected,
    int shouldSetSelected,
    const char* selectedSourceKey)
{
    return DlmHostRuntimeStateApplySelectedSourcePlan(
        handle,
        shouldClearSelected,
        shouldSetSelected,
        selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateApplyShowHiddenPlanExport(
    void* handle,
    int showHiddenSources)
{
    return DlmHostRuntimeStateApplyShowHiddenPlan(handle, showHiddenSources);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateApplyPerformanceModePlanExport(
    void* handle,
    int hostPerformanceMode)
{
    return DlmHostRuntimeStateApplyPerformanceModePlan(handle, hostPerformanceMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateGetSnapshotExport(
    void* handle,
    DlmHostRuntimeStateSnapshot* snapshot)
{
    return DlmHostRuntimeStateGetSnapshot(handle, snapshot);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeStateCopySelectedSourceKeyExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostRuntimeStateCopySelectedSourceKey(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostRuntimeServiceCommandDestroyExport(void* handle)
{
    DlmHostRuntimeServiceCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandClearSourcesExport(void* handle)
{
    return DlmHostRuntimeServiceCommandClearSources(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostRuntimeServiceCommandAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandPlanResetAllExport(void* handle)
{
    return DlmHostRuntimeServiceCommandPlanResetAll(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandPlanResetSourceExport(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId)
{
    return DlmHostRuntimeServiceCommandPlanResetSource(
        handle,
        sourceKey,
        sourceKind,
        hasProcessId,
        processId);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandPlanSetPerformanceModeExport(
    void* handle,
    int hostPerformanceMode)
{
    return DlmHostRuntimeServiceCommandPlanSetPerformanceMode(handle, hostPerformanceMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandGetPlanExport(
    void* handle,
    DlmHostRuntimeServiceCommandPlan* plan)
{
    return DlmHostRuntimeServiceCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostRuntimeServiceCommandCopySourceKeyExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostRuntimeServiceCommandCopySourceKey(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellCreateExport(void** handle)
{
    return DlmHostSelectedDetailShellCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotCreateExport(void** handle)
{
    return DlmHostSelectedDetailSnapshotCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotDestroyExport(void* handle)
{
    DlmHostSelectedDetailSnapshotDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotClearExport(void* handle)
{
    return DlmHostSelectedDetailSnapshotClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotAddSourceExport(
    void* handle,
    const char* sourceKey,
    const char* displayTitle,
    const double* spectrumValues,
    int spectrumCount)
{
    return DlmHostSelectedDetailSnapshotAddSource(handle, sourceKey, displayTitle, spectrumValues, spectrumCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotAddModuleLayoutExport(
    void* handle,
    int kind,
    int orderIndex,
    int hasHeight,
    double height)
{
    return DlmHostSelectedDetailSnapshotAddModuleLayout(handle, kind, orderIndex, hasHeight, height);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotProjectExport(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasStereoVisible,
    int stereoVisible,
    int hasCollisionVisible,
    int collisionVisible,
    int hasLoudnessClockVisible,
    int loudnessClockVisible,
    int hasWaveformVisible,
    int waveformVisible,
    int hasSpectrumVisible,
    int spectrumVisible,
    int hasLevelFloorDb,
    double levelFloorDb,
    int hasProminenceThresholdDb,
    double prominenceThresholdDb,
    int hasRelativeThreshold,
    double relativeThreshold,
    int hasFrequencyScale,
    int frequencyScale)
{
    return DlmHostSelectedDetailSnapshotProject(
        handle,
        selectedSourceKey,
        comparisonSourceKey,
        hasStereoVisible,
        stereoVisible,
        hasCollisionVisible,
        collisionVisible,
        hasLoudnessClockVisible,
        loudnessClockVisible,
        hasWaveformVisible,
        waveformVisible,
        hasSpectrumVisible,
        spectrumVisible,
        hasLevelFloorDb,
        levelFloorDb,
        hasProminenceThresholdDb,
        prominenceThresholdDb,
        hasRelativeThreshold,
        relativeThreshold,
        hasFrequencyScale,
        frequencyScale);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotGetPlanExport(
    void* handle,
    DlmHostSelectedDetailSnapshotPlan* plan)
{
    return DlmHostSelectedDetailSnapshotGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotGetModuleCountExport(
    void* handle,
    int* count)
{
    return DlmHostSelectedDetailSnapshotGetModuleCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotGetModuleAtExport(
    void* handle,
    int index,
    DlmHostDetailModuleProjectionRow* row)
{
    return DlmHostSelectedDetailSnapshotGetModuleAt(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotCopyModuleStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSelectedDetailSnapshotCopyModuleStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotGetBandCountExport(
    void* handle,
    int* count)
{
    return DlmHostSelectedDetailSnapshotGetBandCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotGetBandAtExport(
    void* handle,
    int index,
    DlmHostCollisionBand* band)
{
    return DlmHostSelectedDetailSnapshotGetBandAt(handle, index, band);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailSnapshotCopyStringExport(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSelectedDetailSnapshotCopyString(handle, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSelectedDetailShellDestroyExport(void* handle)
{
    DlmHostSelectedDetailShellDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellClearExport(void* handle)
{
    return DlmHostSelectedDetailShellClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellAddSourceExport(
    void* handle,
    const char* sourceKey,
    const char* displayTitle)
{
    return DlmHostSelectedDetailShellAddSource(handle, sourceKey, displayTitle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellAddModuleExport(
    void* handle,
    int kind,
    int isVisible)
{
    return DlmHostSelectedDetailShellAddModule(handle, kind, isVisible);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellProjectExport(
    void* handle,
    const char* selectedSourceKey,
    const char* comparisonSourceKey,
    int hasLevelFloorDb,
    double levelFloorDb,
    int hasProminenceThresholdDb,
    double prominenceThresholdDb,
    int hasRelativeThreshold,
    double relativeThreshold,
    int hasFrequencyScale,
    int frequencyScale)
{
    return DlmHostSelectedDetailShellProject(
        handle,
        selectedSourceKey,
        comparisonSourceKey,
        hasLevelFloorDb,
        levelFloorDb,
        hasProminenceThresholdDb,
        prominenceThresholdDb,
        hasRelativeThreshold,
        relativeThreshold,
        hasFrequencyScale,
        frequencyScale);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellGetPlanExport(
    void* handle,
    DlmHostSelectedDetailShellPlan* plan)
{
    return DlmHostSelectedDetailShellGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellGetComparisonIndexCountExport(
    void* handle,
    int* count)
{
    return DlmHostSelectedDetailShellGetComparisonIndexCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellGetComparisonIndexAtExport(
    void* handle,
    int index,
    int* sourceIndex)
{
    return DlmHostSelectedDetailShellGetComparisonIndexAt(handle, index, sourceIndex);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedDetailShellCopyStringExport(
    void* handle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSelectedDetailShellCopyString(handle, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandCreateExport(void** handle)
{
    return DlmHostSelectedSourceCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSelectedSourceCommandDestroyExport(void* handle)
{
    DlmHostSelectedSourceCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandClearSourcesExport(void* handle)
{
    return DlmHostSelectedSourceCommandClearSources(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSelectedSourceCommandAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandPlanSelectExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSelectedSourceCommandPlanSelect(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandPlanBackToGlobalExport(void* handle)
{
    return DlmHostSelectedSourceCommandPlanBackToGlobal(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandPlanReconcileExport(
    void* handle,
    const char* selectedSourceKey)
{
    return DlmHostSelectedSourceCommandPlanReconcile(handle, selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandPlanHiddenSourceExport(
    void* handle,
    const char* sourceKey,
    int isHidden,
    int showHiddenSources,
    const char* selectedSourceKey)
{
    return DlmHostSelectedSourceCommandPlanHiddenSource(
        handle,
        sourceKey,
        isHidden,
        showHiddenSources,
        selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandGetPlanExport(
    void* handle,
    DlmHostSelectedSourceCommandPlan* plan)
{
    return DlmHostSelectedSourceCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSelectedSourceCommandCopySelectedSourceKeyExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSelectedSourceCommandCopySelectedSourceKey(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandCreateExport(void** handle)
{
    return DlmHostSourceTargetCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSourceTargetCommandDestroyExport(void* handle)
{
    DlmHostSourceTargetCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandClearSourcesExport(void* handle)
{
    return DlmHostSourceTargetCommandClearSources(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandAddSourceExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSourceTargetCommandAddSource(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandPlanTargetExport(
    void* handle,
    const char* sourceKey)
{
    return DlmHostSourceTargetCommandPlanTarget(handle, sourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandGetPlanExport(
    void* handle,
    DlmHostSourceTargetCommandPlan* plan)
{
    return DlmHostSourceTargetCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceTargetCommandCopySourceKeyExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSourceTargetCommandCopySourceKey(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandCreateExport(void** handle)
{
    return DlmHostChartSettingsCommandCreate(handle);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostChartSettingsCommandDestroyExport(void* handle)
{
    DlmHostChartSettingsCommandDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetTruePeakOversamplingExport(
    void* handle,
    int oversampling)
{
    return DlmHostChartSettingsCommandPlanSetTruePeakOversampling(handle, oversampling);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetWaveformDisplayModeExport(
    void* handle,
    int displayMode)
{
    return DlmHostChartSettingsCommandPlanSetWaveformDisplayMode(handle, displayMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetSpectrumDisplayModeExport(
    void* handle,
    int displayMode)
{
    return DlmHostChartSettingsCommandPlanSetSpectrumDisplayMode(handle, displayMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetDetailModuleVisibilityExport(
    void* handle,
    int moduleKind,
    int isVisible)
{
    return DlmHostChartSettingsCommandPlanSetDetailModuleVisibility(handle, moduleKind, isVisible);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetSpectrumDisplayRangeExport(
    void* handle,
    double floorDb,
    double ceilingDb)
{
    return DlmHostChartSettingsCommandPlanSetSpectrumDisplayRange(handle, floorDb, ceilingDb);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetLoudnessClockSettingsExport(
    void* handle,
    double minimumDb,
    double maximumDb,
    double warningDb,
    double cycleSeconds)
{
    return DlmHostChartSettingsCommandPlanSetLoudnessClockSettings(
        handle,
        minimumDb,
        maximumDb,
        warningDb,
        cycleSeconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandPlanSetCollisionSettingsExport(
    void* handle,
    double levelFloorDb,
    double prominenceThresholdDb,
    double relativeThreshold,
    int frequencyScale,
    const char* comparisonSourceKey)
{
    return DlmHostChartSettingsCommandPlanSetCollisionSettings(
        handle,
        levelFloorDb,
        prominenceThresholdDb,
        relativeThreshold,
        frequencyScale,
        comparisonSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandGetPlanExport(
    void* handle,
    DlmHostChartSettingsCommandPlan* plan)
{
    return DlmHostChartSettingsCommandGetPlan(handle, plan);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostChartSettingsCommandCopyCollisionComparisonSourceKeyExport(
    void* handle,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostChartSettingsCommandCopyCollisionComparisonSourceKey(handle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmHostSourceProjectionDestroyExport(void* handle)
{
    DlmHostSourceProjectionDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionClearExport(void* handle)
{
    return DlmHostSourceProjectionClear(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionAddSourceExport(
    void* handle,
    const char* sourceKey,
    int sourceKind,
    int hasProcessId,
    int processId,
    const char* displayName,
    const char* alias,
    int isHidden,
    int hasOrderIndex,
    int orderIndex,
    int truePeakOversampling,
    int waveformDisplayMode,
    int spectrumDisplayMode)
{
    return DlmHostSourceProjectionAddSource(
        handle,
        sourceKey,
        sourceKind,
        hasProcessId,
        processId,
        displayName,
        alias,
        isHidden,
        hasOrderIndex,
        orderIndex,
        truePeakOversampling,
        waveformDisplayMode,
        spectrumDisplayMode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionProjectExport(
    void* handle,
    int showHiddenSources,
    const char* selectedSourceKey)
{
    return DlmHostSourceProjectionProject(handle, showHiddenSources, selectedSourceKey);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionGetProjectedCountExport(
    void* handle,
    int* count)
{
    return DlmHostSourceProjectionGetProjectedCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionIsSelectedSourceVisibleExport(
    void* handle,
    int* isVisible)
{
    return DlmHostSourceProjectionIsSelectedSourceVisible(handle, isVisible);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionGetProjectedAtExport(
    void* handle,
    int index,
    DlmHostSourceProjectionRow* row)
{
    return DlmHostSourceProjectionGetProjectedAt(handle, index, row);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmHostSourceProjectionCopyProjectedStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmHostSourceProjectionCopyProjectedStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmLoopbackProtocolDecodeExport(
    const unsigned char* bytes,
    int byteCount,
    void** packetHandle,
    DlmLoopbackProtocolPacket* packet)
{
    return DlmLoopbackProtocolDecode(bytes, byteCount, packetHandle, packet);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmLoopbackProtocolDestroyPacketExport(void* packetHandle)
{
    DlmLoopbackProtocolDestroyPacket(packetHandle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmLoopbackProtocolCopyStringExport(
    void* packetHandle,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmLoopbackProtocolCopyString(packetHandle, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmLoopbackProtocolCopySamplesExport(
    void* packetHandle,
    float* buffer,
    int capacity,
    int* actualCount)
{
    return DlmLoopbackProtocolCopySamples(packetHandle, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmLoopbackProtocolEncodeAckExport(
    const char* sourceHandle,
    unsigned char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmLoopbackProtocolEncodeAck(sourceHandle, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmSourceIngestCoreDestroyExport(void* handle)
{
    DlmSourceIngestCoreDestroy(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreRegisterExport(
    void* handle,
    const char* sourceHandle,
    const char* displayName,
    int sourceKind,
    int sampleRate,
    int channels,
    long long timestampMilliseconds)
{
    return DlmSourceIngestCoreRegister(handle, sourceHandle, displayName, sourceKind, sampleRate, channels, timestampMilliseconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreHeartbeatExport(
    void* handle,
    const char* sourceHandle,
    long long timestampMilliseconds)
{
    return DlmSourceIngestCoreHeartbeat(handle, sourceHandle, timestampMilliseconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreRenameExport(
    void* handle,
    const char* sourceHandle,
    const char* displayName,
    long long timestampMilliseconds)
{
    return DlmSourceIngestCoreRename(handle, sourceHandle, displayName, timestampMilliseconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreProcessF32Export(
    void* handle,
    const char* sourceHandle,
    const float* interleavedSamples,
    int sampleCount,
    long long timestampMilliseconds)
{
    return DlmSourceIngestCoreProcessF32(handle, sourceHandle, interleavedSamples, sampleCount, timestampMilliseconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCloseExport(void* handle, const char* sourceHandle)
{
    return DlmSourceIngestCoreClose(handle, sourceHandle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreRemoveExpiredExport(
    void* handle,
    long long nowMilliseconds,
    long long timeoutMilliseconds)
{
    return DlmSourceIngestCoreRemoveExpired(handle, nowMilliseconds, timeoutMilliseconds);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreResetSourceExport(void* handle, const char* sourceHandle)
{
    return DlmSourceIngestCoreResetSource(handle, sourceHandle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreResetAllExport(void* handle)
{
    return DlmSourceIngestCoreResetAll(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreSetTruePeakOversamplingExport(
    void* handle,
    const char* sourceHandle,
    int oversampling)
{
    return DlmSourceIngestCoreSetTruePeakOversampling(handle, sourceHandle, oversampling);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreSetChannelWaveformEnabledExport(
    void* handle,
    const char* sourceHandle,
    int enabled)
{
    return DlmSourceIngestCoreSetChannelWaveformEnabled(handle, sourceHandle, enabled);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreSetChannelSpectrumEnabledExport(
    void* handle,
    const char* sourceHandle,
    int enabled)
{
    return DlmSourceIngestCoreSetChannelSpectrumEnabled(handle, sourceHandle, enabled);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreSetPerformanceModeExport(void* handle, int mode)
{
    return DlmSourceIngestCoreSetPerformanceMode(handle, mode);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreGetSourceCountExport(void* handle, int* count)
{
    return DlmSourceIngestCoreGetSourceCount(handle, count);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreGetSourceSnapshotAtExport(
    void* handle,
    int index,
    DlmSourceIngestSnapshot* snapshot)
{
    return DlmSourceIngestCoreGetSourceSnapshotAt(handle, index, snapshot);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopySourceStringAtExport(
    void* handle,
    int index,
    int field,
    char* buffer,
    int capacity,
    int* actualByteCount)
{
    return DlmSourceIngestCoreCopySourceStringAt(handle, index, field, buffer, capacity, actualByteCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopyWaveformAtExport(
    void* handle,
    int index,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmSourceIngestCoreCopyWaveformAt(handle, index, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopyChannelWaveformAtExport(
    void* handle,
    int index,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmSourceIngestCoreCopyChannelWaveformAt(handle, index, channel, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopySpectrumAtExport(
    void* handle,
    int index,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmSourceIngestCoreCopySpectrumAt(handle, index, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopyChannelSpectrumAtExport(
    void* handle,
    int index,
    int channel,
    double* buffer,
    int capacity,
    int* actualCount)
{
    return DlmSourceIngestCoreCopyChannelSpectrumAt(handle, index, channel, buffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmSourceIngestCoreCopyStereoImagePointsAtExport(
    void* handle,
    int index,
    double* angleBuffer,
    double* radiusBuffer,
    int capacity,
    int* actualCount)
{
    return DlmSourceIngestCoreCopyStereoImagePointsAt(handle, index, angleBuffer, radiusBuffer, capacity, actualCount);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmRfRapidInit()
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

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmRfRapidSpectrumF64(
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

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmRfRapidTruePeakCreate(
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

extern "C" DLM_NATIVE_EXPORT void DLM_NATIVE_CALL DlmRfRapidTruePeakDestroy(void* handle)
{
    delete static_cast<TruePeakEstimatorHandle*>(handle);
}

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmRfRapidTruePeakReset(void* handle)
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

extern "C" DLM_NATIVE_EXPORT int DLM_NATIVE_CALL DlmRfRapidTruePeakProcessF64(
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
