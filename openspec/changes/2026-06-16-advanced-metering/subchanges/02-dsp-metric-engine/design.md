# DSP Metric Engine Design

## Overview

This slice adds a standalone streaming PCM analyzer under `DesktopLoudnessMeter.App.Audio.Analysis`. It intentionally does not capture audio; the later process-loopback slice will feed decoded/interleaved float samples into this engine and copy snapshots onto `AudioSessionSnapshot`.

## Key Decisions

- Keep the analyzer independent from WPF, NAudio session enumeration, and view models.
- Use floating-point samples in the nominal `[-1, 1]` full-scale range.
- Implement BS.1770-style K-weighting with the 48 kHz coefficient set currently used by the standards literature and tests; document the engine as approximate until reference-vector validation is added.
- Use 400 ms momentary, 3 s short-term, and 400 ms integrated loudness blocks with 100 ms hops.
- Use a bounded sinc interpolator for true-peak estimation so oversampling can reveal inter-sample excursions instead of only sample peaks.

## Implementation Units

### [U1] Analyzer Contract And Snapshot
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzerOptions.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs`

Defines construction options, input validation, reset behavior, and snapshot shape.

### [U2] Loudness Engine
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/LoudnessMetricTests.cs`

Applies K-weighting, stores 400 ms and 3 s power windows, and computes integrated loudness using absolute and relative gates.

### [U3] True Peak, RMS, And Stereo Metrics
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs`

Tracks recent unweighted samples for RMS, stereo image, correlation, and oversampled true peak.

## Requirement Tracking

| Requirement | Implementation Unit | Notes |
|-------------|---------------------|-------|
| R1 | U1 | Validated interleaved PCM input and independent snapshot API. |
| R2 | U2 | K-weighted momentary, short-term, and gated integrated loudness. |
| R3 | U3 | 2x/4x/8x bounded sinc true-peak estimation. |
| R4 | U3 | Recent 100 ms RMS, stereo image, and -1..+1 correlation. |
| R5 | U1, U2, U3 | Analyzer reset clears all accumulated state. |

## Validation Strategy

- Unit-test invalid construction and frame-alignment errors.
- Unit-test silence, mono sine, loud/quiet integrated gating, and required loudness window readiness.
- Unit-test RMS level on a known sine-like signal.
- Unit-test mono, hard-panned, and anti-phase stereo metrics.
- Unit-test true-peak oversampling detects an inter-sample excursion above the discrete sample peak.

## Open Risks

- The first engine is standards-inspired but not yet certified; reference-vector validation should be added before claiming formal compliance.
- Non-48 kHz sample rates are accepted, but the initial K-weighting filter uses the 48 kHz coefficient set, so non-48 kHz accuracy is approximate.
