# Tasks

## Task 1: Remove render-stage frequency blur

**Requirement Trace:** [R1, R2] -> [U1]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs`
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollColumnRasterizerTests.cs`

**Acceptance Criteria:**
- [x] A one-hot bucket produces exactly one non-zero pixel in the rasterized column.
- [x] Recoloring stored levels uses the same sharp mapping as live columns.
- [x] The rolling bitmap uses nearest-neighbor scaling.

**Dependencies:** none

## Task 2: Decouple spectrum FFT input from true-peak oversampling

**Requirement Trace:** [R3, R4] -> [U2]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs`

**Acceptance Criteria:**
- [x] 100 Hz, 1 kHz, 4 kHz, and 12 kHz tones have dominant buckets near the expected Mel position.
- [x] The dominant bucket for a 1 kHz tone remains stable across 2x, 4x, and 8x true-peak oversampling.
- [x] Existing waveform oversampling and performance-mode tests continue to pass.

**Dependencies:** Task 1

## Task 3: Document the spectrograph model

**Requirement Trace:** [R5] -> [U3]  
**Execution Mode:** [direct]
**Files:**
- `docs/rendering-architecture.md`

**Acceptance Criteria:**
- [x] Documentation records render interpolation/smoothing as prior blur contributors.
- [x] Documentation records raw-PCM FFT input and remaining Hann/bucket limitations.
- [x] Future validation guidance is included.

**Dependencies:** Task 2
