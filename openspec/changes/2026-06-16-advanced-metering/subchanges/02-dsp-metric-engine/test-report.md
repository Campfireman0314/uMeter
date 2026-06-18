# Test Report: 02-dsp-metric-engine

## Task 1: Add analyzer contract and resettable snapshot state

### Red Phase
- **Timestamp:** 2026-06-16 15:34
- **Failing Tests:**
  - `PcmMetricAnalyzerContractTests` - `CS0234`: analyzer namespace and contract types did not exist yet.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-16 15:35
- **Passing Tests:**
  - `PcmMetricAnalyzerContractTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzerOptions.cs` - added - Validated sample rate, channel count, and true-peak oversampling factor.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs` - added - Added nullable analyzer metric snapshot.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - added - Added analyzer entry point, block alignment validation, and reset.
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs` - added - Added contract tests.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Invalid sample rate, channel count, oversampling, and alignment are rejected. | `PcmMetricAnalyzerContractTests.RejectsInvalidOptions`, `RejectsUnalignedInterleavedBlocks` | pass |
| Snapshot remains independent of UI/WASAPI/view-model types. | `PcmMetricAnalyzerContractTests.SnapshotStartsUnavailableAndIndependentOfUiTypes` | pass |
| Reset clears available metric state. | `PcmMetricAnalyzerContractTests.ResetClearsAvailableMetrics` | pass |

## Task 2: Implement K-weighted loudness windows and integrated gating

### Red Phase
- **Timestamp:** 2026-06-16 15:36
- **Failing Tests:**
  - `LoudnessMetricTests.SilenceDoesNotProduceLoudnessMetrics` - placeholder loudness returned `-inf` instead of unavailable.
  - `LoudnessMetricTests.IntegratedLoudnessIgnoresQuietTailBelowGate` - cumulative placeholder RMS allowed quiet tails to pull integrated loudness down.
- **False Red Fixes:**
  - Adjusted contract reset test from DC to sine input because K-weighting intentionally removes DC.
  - Allowed a 0.5 LU integrated tolerance around the loud-to-quiet transition because overlapping 400 ms windows include transition energy.

### Green Phase
- **Timestamp:** 2026-06-16 15:38
- **Passing Tests:**
  - `LoudnessMetricTests` - pass
  - `PcmMetricAnalyzerContractTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs` - added - Added cascaded K-weighting filters.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs` - added - Added fixed-size power windows for momentary and short-term loudness.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs` - added - Added 400 ms / 100 ms-hop integrated loudness blocks with absolute and relative gates.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - modified - Wired K-weighted loudness and gating into snapshots.
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/LoudnessMetricTests.cs` - added - Added loudness window, silence, and gating tests.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Momentary loudness requires 400 ms. | `LoudnessMetricTests.MomentaryLoudnessRequiresFourHundredMilliseconds` | pass |
| Short-term loudness requires 3 s. | `LoudnessMetricTests.ShortTermLoudnessRequiresThreeSeconds` | pass |
| Integrated loudness ignores silence and quiet tail energy below gate. | `LoudnessMetricTests.SilenceDoesNotProduceLoudnessMetrics`, `IntegratedLoudnessIgnoresQuietTailBelowGate` | pass |

## Task 3: Implement true peak, RMS, stereo image, and correlation

### Red Phase
- **Timestamp:** 2026-06-16 15:38
- **Failing Tests:**
  - `TruePeakAndStereoMetricTests.RmsUsesMostRecentHundredMilliseconds` - cumulative RMS did not use the most recent 100 ms.
  - `TruePeakAndStereoMetricTests.CorrelationReportsMonoAndAntiPhase` - placeholder correlation always returned +1.
  - `TruePeakAndStereoMetricTests.StereoImagePointsTowardHardPannedSide` - placeholder stereo image stayed centered.
  - `TruePeakAndStereoMetricTests.TruePeakOversamplingCanExceedDiscreteSamplePeak` - placeholder true peak only checked discrete samples.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-16 15:39
- **Passing Tests:**
  - `PcmMetricAnalyzerContractTests` - pass
  - `LoudnessMetricTests` - pass
  - `TruePeakAndStereoMetricTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs` - added - Added recent 100 ms RMS, stereo image, and correlation accumulation.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs` - added - Added oversampled true-peak estimation over intermediate sample positions with fixed rolling buffers.
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - modified - Wired recent-sample and true-peak estimators into snapshots.
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs` - added - Added RMS, stereo, correlation, and true-peak tests.
- **Defect Found During Green:**
  - `stackalloc` inside the per-frame loop caused a stack overflow on long test buffers; fixed by moving the frame buffer to analyzer state.
  - Per-sample `Queue.ToArray()` allocation in true-peak estimation was replaced with a fixed rolling buffer before final validation.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| 100 ms RMS uses the most recent window. | `TruePeakAndStereoMetricTests.RmsUsesMostRecentHundredMilliseconds` | pass |
| Correlation reports +1 for mono and -1 for anti-phase. | `TruePeakAndStereoMetricTests.CorrelationReportsMonoAndAntiPhase` | pass |
| Stereo image points left/right for hard-panned signals. | `TruePeakAndStereoMetricTests.StereoImagePointsTowardHardPannedSide` | pass |
| True peak oversampling can exceed discrete sample peak. | `TruePeakAndStereoMetricTests.TruePeakOversamplingCanExceedDiscreteSamplePeak` | pass |

## Smoke Validation

- `dotnet build desktop-loudness-meter.sln` passed with 0 warnings and 0 errors.
- `dotnet test desktop-loudness-meter.sln --no-restore` passed with 41 tests.
- `dotnet format desktop-loudness-meter.sln --verify-no-changes` passed.
- Direct exe launch stayed alive for 3 seconds and produced a main window handle/title: `Desktop Loudness Meter`.
