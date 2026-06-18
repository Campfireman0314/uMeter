# Tasks

## Task 1: Add analyzer contract and resettable snapshot state

**Requirement Trace:** [R1, R5] -> [U1]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzerOptions.cs` - analyzer construction options
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs` - analyzer output shape
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - analyzer entry point and reset
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs` - contract tests

**Acceptance Criteria:**
- [x] Analyzer rejects invalid sample rate, channel count, oversampling factor, and unaligned interleaved blocks.
- [x] Analyzer snapshot is independent of WPF, WASAPI, and view-model types.
- [x] Reset clears previously available metric state.

**Dependencies:** None

## Task 2: Implement K-weighted loudness windows and integrated gating

**Requirement Trace:** [R2] -> [U2]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs` - per-channel K-weighting filter
- `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs` - momentary/short-term power windows
- `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs` - integrated loudness gating
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - loudness update flow
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/LoudnessMetricTests.cs` - loudness behavior tests

**Acceptance Criteria:**
- [x] Momentary loudness is unavailable before 400 ms and available after enough non-silent PCM.
- [x] Short-term loudness is unavailable before 3 s and available after enough non-silent PCM.
- [x] Integrated loudness ignores silence and blocks below the relative gate.

**Dependencies:** Task 1

## Task 3: Implement true peak, RMS, stereo image, and correlation

**Requirement Trace:** [R3, R4] -> [U3]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs` - oversampled true-peak estimator
- `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs` - recent PCM history for RMS/stereo
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` - true peak/RMS/stereo update flow
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs` - true peak and stereo tests

**Acceptance Criteria:**
- [x] 100 ms RMS matches a known sine-like stereo signal within tolerance.
- [x] Correlation reports approximately +1 for mono and -1 for anti-phase stereo.
- [x] Stereo image points left/right for hard-panned signals and stays within -1..+1.
- [x] True peak oversampling evaluates inter-sample positions and can exceed the discrete sample peak.

**Dependencies:** Task 1
