#pragma once

extern "C"
{
    struct DlmPcmAnalyzerSnapshot
    {
        int hasMomentaryLufs;
        double momentaryLufs;
        int hasShortTermLufs;
        double shortTermLufs;
        int hasIntegratedLufs;
        double integratedLufs;
        int hasTruePeakDb;
        double truePeakDb;
        int hasRms100MsDb;
        double rms100MsDb;
        int hasStereoImageX;
        double stereoImageX;
        int hasStereoImageY;
        double stereoImageY;
        int hasCorrelation;
        double correlation;
        int waveformCount;
        int channelWaveformCount;
        int spectrumCount;
        int channelSpectrumCount;
        int stereoImagePointCount;
        long long plotFrameVersion;
    };
}

int DlmPcmAnalyzerCreate(int sampleRate, int channels, int truePeakOversampling, void** handle);
void DlmPcmAnalyzerDestroy(void* handle);
int DlmPcmAnalyzerReset(void* handle);
int DlmPcmAnalyzerProcessF32(void* handle, const float* interleavedSamples, int sampleCount);
int DlmPcmAnalyzerGetSnapshot(void* handle, DlmPcmAnalyzerSnapshot* snapshot);
int DlmPcmAnalyzerSetChannelWaveformEnabled(void* handle, int enabled);
int DlmPcmAnalyzerSetChannelSpectrumEnabled(void* handle, int enabled);
int DlmPcmAnalyzerSetPerformanceMode(void* handle, int mode);
int DlmPcmAnalyzerCopyWaveform(void* handle, double* buffer, int capacity, int* actualCount);
int DlmPcmAnalyzerCopyChannelWaveform(void* handle, int channel, double* buffer, int capacity, int* actualCount);
int DlmPcmAnalyzerCopySpectrum(void* handle, double* buffer, int capacity, int* actualCount);
int DlmPcmAnalyzerCopyChannelSpectrum(void* handle, int channel, double* buffer, int capacity, int* actualCount);
int DlmPcmAnalyzerCopyStereoImagePoints(void* handle, double* angleBuffer, double* radiusBuffer, int capacity, int* actualCount);
