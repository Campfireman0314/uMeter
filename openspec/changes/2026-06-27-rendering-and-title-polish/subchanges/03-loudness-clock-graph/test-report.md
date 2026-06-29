# Test Report: 03-loudness-clock-graph

## Task 1: Add loudness clock mapper and rendering control

### Red Phase
- **Timestamp:** 2026-06-27 02:31
- **Failing Tests:**
  - `LoudnessClockPointMapperTests` - compile error: `LoudnessClockPointMapper` product code was missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 02:31
- **Passing Tests:**
  - `LoudnessClockPointMapperTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - added - maps loudness values to circular graph radii and angles.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - added - renders bounded momentary loudness history as a circular trace.
  - `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - added - covers below-minimum omission, radius mapping, and clockwise angle progression.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Samples below the configured minimum are not mapped for plotting. | `BelowMinimumLoudnessIsNotMapped` | pass |
| Minimum loudness maps to the inner radius. | `MinimumMaximumAndIntermediateLoudnessMapToExpectedRadii` | pass |
| Maximum and above-maximum loudness map to the outer radius. | `MinimumMaximumAndIntermediateLoudnessMapToExpectedRadii` | pass |
| Intermediate loudness maps proportionally between inner and outer radii. | `MinimumMaximumAndIntermediateLoudnessMapToExpectedRadii` | pass |
| The control keeps bounded history and draws without per-frame unbounded allocation growth. | Code review: `LoudnessClockView.MaximumSampleCount` and bounded list eviction | pass |

### Refactor Phase
- **Timestamp:** 2026-06-27 02:34
- **Passing Tests:**
  - `LoudnessClockPointMapperTests` - pass
  - Focused loudness clock and spectrum seam suite - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - clears retained samples when a plot tick arrives with non-finite loudness, matching reset/source-switch behavior.
- **Coverage:** N/A

## Task 2: Expose a dedicated momentary loudness clock feed

### Red Phase
- **Timestamp:** 2026-06-27 02:31
- **Failing Tests:**
  - `AudioSessionMeterViewModelTests.FiniteMomentaryLoudnessAdvancesClockPlotTick` - compile error: loudness clock feed properties were missing.
  - `AudioSessionMeterViewModelTests.MissingMomentaryLoudnessDoesNotAdvanceClockPlotTick` - compile error: loudness clock feed properties were missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 02:31
- **Passing Tests:**
  - `AudioSessionMeterViewModelTests.FiniteMomentaryLoudnessAdvancesClockPlotTick` - pass
  - `AudioSessionMeterViewModelTests.MissingMomentaryLoudnessDoesNotAdvanceClockPlotTick` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - modified - exposes raw momentary loudness and advances a dedicated clock plot tick on fresh finite frames.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - modified - covers finite and missing momentary loudness clock tick behavior.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Finite momentary loudness on a fresh plot frame advances `LoudnessClockPlotTick`. | `FiniteMomentaryLoudnessAdvancesClockPlotTick` | pass |
| Null/non-finite momentary loudness does not advance the clock plot tick. | `MissingMomentaryLoudnessDoesNotAdvanceClockPlotTick` | pass |
| Reset clears/advances the clock feed consistently with other plot ticks. | `ResetClearsRollBuffersAndAcceptsRewoundPlotFrames` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-27 02:34
- **Passing Tests:**
  - `AudioSessionMeterViewModelTests` focused clock/reset tests - pass
  - Full `DesktopLoudnessMeter.Tests` suite - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - modified - extends reset coverage to assert loudness clock tick and NaN reset state.
- **Coverage:** N/A

## Task 3: Persist and integrate loudness clock controls

### Red Phase
- **Timestamp:** 2026-06-27 02:31
- **Failing Tests:**
  - `MainViewModelAdvancedControlsTests.InlineDetailPreferencesPersistPerSource` - compile error: inline loudness clock settings were missing.
  - `MainViewModelAdvancedControlsTests.ForgetSelectedSourceClearsSavedWorkspaceAndResetsVisiblePreferences` - compile error: inline loudness clock settings were missing.
  - `MainViewModelAdvancedControlsTests.LoudnessClockRangePreservesMinimumSpan` - compile error: inline loudness clock range properties were missing.
  - `DetailWindowViewModelTests.PopupDetailPreferencesLoadFromWorkspace` - compile error: pop-up loudness clock settings were missing.
  - `DetailWindowViewModelTests.PopupDetailPreferencesSaveToWorkspace` - compile error: pop-up loudness clock settings were missing.
  - `DetailWindowViewModelTests.DetailWindowLoudnessClockRangePreservesMinimumSpan` - compile error: pop-up loudness clock range properties were missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 02:31
- **Passing Tests:**
  - Task 3 persistence/range focused tests - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - modified - persists inline/pop-up clock visibility and shared loudness clock range.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - adds inline clock visibility, range clamping, range text, and per-source persistence.
  - `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - modified - adds pop-up clock visibility, range clamping, range text, and per-source persistence.
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - adds inline clock toggle, clock range slider, and loudness clock panel.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - adds pop-up clock toggle, clock range slider, and loudness clock panel.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - modified - covers inline persistence, reset, and range span behavior.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs` - modified - covers pop-up persistence and range span behavior.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Inline detail exposes show/hide and minimum/maximum loudness controls. | `dotnet build .\desktop-loudness-meter.sln` validates XAML bindings | pass |
| Pop-up detail exposes show/hide and minimum/maximum loudness controls. | `dotnet build .\desktop-loudness-meter.sln` validates XAML bindings | pass |
| Clock visibility and range settings persist per source. | `InlineDetailPreferencesPersistPerSource`, `PopupDetailPreferencesLoadFromWorkspace`, `PopupDetailPreferencesSaveToWorkspace` | pass |
| Range setters preserve at least the minimum span. | `LoudnessClockRangePreservesMinimumSpan`, `DetailWindowLoudnessClockRangePreservesMinimumSpan` | pass |
| [manual] Live UI shows a circular loudness clock without overlapping existing detail controls. | Pending manual validation | pending |

### Refactor Phase
- **Timestamp:** 2026-06-27 02:34
- **Passing Tests:**
  - Focused loudness clock/settings/stripe suite - pass, 14 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 251 tests
  - `dotnet build .\desktop-loudness-meter.sln` - pass, 0 warnings
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - modified - normalizes persisted loudness clock range values during settings load/save.
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - keeps clock graph independent of waveform/spectrum roll layout.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - keeps pop-up clock graph independent of waveform/spectrum roll layout.
- **Coverage:** N/A

### Pending Manual Validation
- [ ] [manual] Live UI shows a circular loudness clock without overlapping existing detail controls - validation method: run the WPF app and inspect inline and pop-up detail at compact and wide widths.
