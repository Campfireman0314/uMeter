# Tasks

## Task 1: Throttle waveform buffer refresh

**Requirement Trace:** [R1, R5] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs` - performance settings
- `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs` - waveform cache eligibility
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs` - refresh cadence tests

**Acceptance Criteria:**
- [x] Existing waveform buffers are reused until the configured frame interval elapses.
- [x] First waveform buffer remains available immediately after samples arrive.
- [x] Performance-mode changes invalidate waveform refresh eligibility.

**Dependencies:** None

## Task 2: Adapt waveform render point budget to viewport width

**Requirement Trace:** [R2, R5] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/WaveformRenderResolution.cs` - point-budget helper
- `src/DesktopLoudnessMeter.App/Controls/WaveformRollView.cs` - adaptive geometry generation
- `tests/DesktopLoudnessMeter.Tests/Controls/WaveformRenderResolutionTests.cs` - point-budget tests

**Acceptance Criteria:**
- [x] Narrow plots use fewer rendered points than wide plots.
- [x] Point budgets stay within fixed minimum and maximum bounds.
- [x] High-density paths avoid expensive smoothing while preserving existing pan/zoom behavior.

**Dependencies:** None

## Task 3: Adapt spectrograph bitmap resolution to viewport size

**Requirement Trace:** [R3, R4, R5] -> [U3]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollResolution.cs` - dynamic resolution helper
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs` - max-preserving row resampling
- `src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs` - dynamic bitmap dimensions
- `tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollResolutionTests.cs` - resolution tests
- `tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollColumnRasterizerTests.cs` - resampling tests

**Acceptance Criteria:**
- [x] Narrow/short plots use fewer history columns and frequency rows than large plots.
- [x] History columns and frequency rows stay within documented bounds.
- [x] One-hot source buckets remain visible when rows are fewer than analyzer buckets.
- [x] Existing spectrum display range, pan/zoom, and recoloring behavior remains intact.

**Dependencies:** None
