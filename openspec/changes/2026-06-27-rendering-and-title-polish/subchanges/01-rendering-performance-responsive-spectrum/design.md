# Design

## Overview

Keep the analyzer and WPF rendering architecture intact, but reduce work at the two hot spots that affect low-end machines: waveform buffer rebuild cadence and fixed-size render buffers. Waveform buffers will follow the same cache/eligibility pattern already used by spectrum buckets. Waveform and spectrograph controls will then choose render density from their visible plot dimensions.

## Implementation Units

### U1: Analyzer waveform refresh cadence
Module-boundary:
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs`

Add waveform refresh intervals to performance settings. `RecentSampleWindow.WaveformSamples` and `ChannelWaveformSamples` will compute immediately for the first available buffer, then reuse the prior buffer until enough input frames have elapsed. Performance-mode changes and reset invalidate waveform eligibility the same way spectrum eligibility is invalidated.

Traces: R1, R5

### U2: Adaptive waveform display points
Module-boundary:
- `src/DesktopLoudnessMeter.App/Controls/WaveformRollView.cs`
- `src/DesktopLoudnessMeter.App/Controls/WaveformRenderResolution.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/WaveformRenderResolutionTests.cs`

Introduce a small helper that maps plot width to a bounded point budget. `WaveformRollView` will use this budget when creating display points, and use less expensive line geometry when high point counts would make Bezier smoothing costly.

Traces: R2, R5

### U3: Adaptive spectrograph bitmap resolution
Module-boundary:
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs`
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollResolution.cs`
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollColumnRasterizerTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollResolutionTests.cs`

Introduce a helper that maps plot width/height to bounded history columns and frequency rows. `SpectrumRollView` will allocate bitmaps from that resolution and use the live resolution in circular-buffer drawing, recoloring, and write paths. The rasterizer will resample analyzer buckets into the chosen row count using max-in-range to preserve narrow peaks.

Traces: R3, R4, R5

## Verification

- Unit tests cover waveform point budgets, spectrograph resolution bounds, max-preserving bucket resampling, and waveform refresh throttling.
- Run focused control/analyzer tests, then the managed test suite.
