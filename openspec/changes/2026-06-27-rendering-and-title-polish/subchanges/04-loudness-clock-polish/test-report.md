# Test Report: 04-loudness-clock-polish

## Task 1: Stabilize clock phase and scale planning

### Red Phase
- **Timestamp:** 2026-06-27 03:00
- **Failing Tests:**
  - `LoudnessClockPointMapperTests` - compile error: `Map` did not expose elapsed-cycle parameters.
  - `LoudnessClockScalePlannerTests.BuildsAtLeastFiveMarkersAcrossConfiguredRange` - compile error: `LoudnessClockScalePlanner` product code was missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 03:01
- **Passing Tests:**
  - `LoudnessClockPointMapperTests` - pass
  - `LoudnessClockScalePlannerTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - modified - maps angles from elapsed cycle phase instead of list index.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs` - added - creates at least five loudness marker values across the configured range.
  - `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - modified - covers stable phase wrapping.
  - `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockScalePlannerTests.cs` - added - covers marker count and endpoints.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Elapsed times at 0 and one full cycle map to the same top angle. | `AnglesUseStableElapsedCyclePhaseAfterWrap` | pass |
| Elapsed times beyond one full cycle preserve the same fixed table phase instead of shifting retained samples by list index. | `AnglesUseStableElapsedCyclePhaseAfterWrap` | pass |
| The scale planner returns at least five marker values from minimum to maximum loudness. | `BuildsAtLeastFiveMarkersAcrossConfiguredRange` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-27 03:07
- **Passing Tests:**
  - Focused loudness clock polish suite - pass, 11 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 256 tests
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - modified - keeps stable angle mapping reusable for the fan renderer.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs` - added - centralizes marker calculation for rendering and tests.
- **Coverage:** N/A

## Task 2: Add cycle and warning settings

### Red Phase
- **Timestamp:** 2026-06-27 03:02
- **Failing Tests:**
  - `MainViewModelAdvancedControlsTests.InlineDetailPreferencesPersistPerSource` - compile error: cycle and warning settings were missing.
  - `MainViewModelAdvancedControlsTests.LoudnessClockCycleAndWarningClampToUsableRanges` - compile error: cycle and warning settings were missing.
  - `DetailWindowViewModelTests.PopupDetailPreferencesLoadFromWorkspace` - compile error: cycle and warning settings were missing.
  - `DetailWindowViewModelTests.PopupDetailPreferencesSaveToWorkspace` - compile error: cycle and warning settings were missing.
  - `DetailWindowViewModelTests.DetailWindowLoudnessClockCycleAndWarningClampToUsableRanges` - compile error: cycle and warning settings were missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 03:04
- **Passing Tests:**
  - Task 2 focused settings tests - pass, 5 tests
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - modified - persists and normalizes clock cycle seconds and warning loudness.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - adds inline cycle/warning properties, texts, clamping, and persistence.
  - `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - modified - adds pop-up cycle/warning properties, texts, clamping, and persistence.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - modified - covers inline persistence and clamps.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs` - modified - covers pop-up persistence and clamps.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Cycle duration defaults to 30 seconds and clamps to 10..600 seconds. | `LoudnessClockCycleAndWarningClampToUsableRanges`, `DetailWindowLoudnessClockCycleAndWarningClampToUsableRanges` | pass |
| Warning loudness clamps to the current clock minimum/maximum loudness range. | `LoudnessClockCycleAndWarningClampToUsableRanges`, `DetailWindowLoudnessClockCycleAndWarningClampToUsableRanges` | pass |
| Cycle duration and warning loudness persist per source in inline detail. | `InlineDetailPreferencesPersistPerSource` | pass |
| Cycle duration and warning loudness persist per source in pop-up detail. | `PopupDetailPreferencesLoadFromWorkspace`, `PopupDetailPreferencesSaveToWorkspace` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-27 03:07
- **Passing Tests:**
  - Focused loudness clock polish suite - pass, 11 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 256 tests
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - reuses clamping helpers when min/max loudness changes.
  - `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - modified - reuses clamping helpers when min/max loudness changes.
- **Coverage:** N/A

## Task 3: Render filled warning-aware clock fan and wire UI

### Red Phase
- **Timestamp:** 2026-06-27 03:05
- **Failing Tests:**
  - `LoudnessClockPointMapperTests.SamplesAtOrAboveWarningLevelAreClassified` - compile error: warning classification was missing from mapped points.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-27 03:06
- **Passing Tests:**
  - `LoudnessClockPointMapperTests.SamplesAtOrAboveWarningLevelAreClassified` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - modified - classifies samples at or above warning loudness.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - stores timestamped samples, trims to the active cycle, draws filled normal/warning fan wedges, and renders denser scale rings.
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - adds inline cycle/warning controls and passes values to the clock view.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - adds pop-up cycle/warning controls and passes values to the clock view.
  - `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - modified - covers warning classification.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Samples at or above warning loudness are classified for warning rendering. | `SamplesAtOrAboveWarningLevelAreClassified` | pass |
| The control uses filled fan geometry between the inner radius and loudness radius. | Code review of `LoudnessClockView.AppendFanWedge` | pass |
| Inline and pop-up clock controls expose cycle duration and warning loudness. | `dotnet build .\desktop-loudness-meter.sln` validates XAML bindings | pass |
| [manual] Live UI shows a stable non-spinning clock table with fan fill and warning color. | Pending manual validation | pending |

### Refactor Phase
- **Timestamp:** 2026-06-27 03:08
- **Passing Tests:**
  - Focused loudness clock polish suite - pass, 11 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 256 tests
  - `dotnet build .\desktop-loudness-meter.sln` - pass, 0 warnings
  - `git diff --check` - pass, CRLF normalization warnings only
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - keeps fan drawing bounded and splits geometry at cycle wrap.
- **Coverage:** N/A

### Pending Manual Validation
- [ ] [manual] Live UI shows a stable non-spinning clock table with fan fill and warning color - validation method: run the WPF app and inspect at least one complete cycle.

## Final 2.3.1 Polish Addendum

### Verification
- **Timestamp:** 2026-06-27 03:29
- **Passing Tests:**
  - Focused loudness clock polish suite - pass, 11 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 256 tests
  - `dotnet build .\desktop-loudness-meter.sln` - pass, 0 warnings
  - `git diff --check` - pass, CRLF normalization warnings only
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - narrows the circular grid inset, defaults the fallback clock range to `-32..-4 LUFS`, and renders the current-position scan ray/level dot.
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - moves the Clock toggle before Waveform and narrows inline clock panel padding.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - moves the Clock toggle before Waveform and narrows pop-up clock panel padding.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - changes the inline default clock range to `-32..-4 LUFS`.
  - `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - modified - changes the pop-up default clock range to `-32..-4 LUFS`.
  - View-model tests - modified - assert the new default range and retained `-12 LUFS` warning default.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Default clock range is `-32..-4 LUFS`, warning default is `-12 LUFS`. | `LoudnessClockCycleAndWarningClampToUsableRanges`, `DetailWindowLoudnessClockCycleAndWarningClampToUsableRanges` | pass |
| Clock toggle appears before Waveform on both detail surfaces. | `dotnet build .\desktop-loudness-meter.sln` validates modified XAML | pass |
| Clock table uses a narrower margin and shows a current-position scan line. | Code review of `LoudnessClockView.OnRender` and `DrawScanLine` | pass |

## Origin Slit Bugfix Addendum

### Verification
- **Timestamp:** 2026-06-27 03:44
- **Passing Tests:**
  - `LoudnessClockFanSegmentPlannerTests` - pass, 2 tests
  - Focused loudness clock slice - pass, 10 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 258 tests
  - `dotnet build .\desktop-loudness-meter.sln` - pass, 0 warnings
  - `git diff --check` - pass, CRLF normalization warnings only
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockFanSegmentPlanner.cs` - added - emits normal fan segments and two boundary segments when phase wraps through the cycle origin.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - uses the planner while streaming adjacent mapped samples.
  - `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockFanSegmentPlannerTests.cs` - added - covers normal and wrapped segment planning.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Wrapped loudness-clock samples do not leave an uncovered origin slit. | `WrappedPhasesBridgeThroughCycleOrigin` | pass |

## Over-Maximum Color and v2.3.2 Addendum

### Verification
- **Timestamp:** 2026-06-27 03:51
- **Passing Tests:**
  - Focused loudness clock slice - pass, 11 tests
  - Full `DesktopLoudnessMeter.Tests` suite - pass, 259 tests
  - `dotnet build .\desktop-loudness-meter.sln` - pass, 0 warnings
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - modified - classifies values above maximum as over-maximum while clamping radius.
  - `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - modified - renders over-maximum fan, trace, scan-line, and dot elements with the danger color role.
  - `src/DesktopLoudnessMeter.App/Controls/ThemedDrawing.cs` - modified - adds a danger color role fallback.
  - Version metadata and release docs - modified - updates the release target to `2.3.2`.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Loudness above the maximum display boundary remains clamped but is visually distinguishable. | `SamplesAboveMaximumAreClampedAndClassifiedAsOverMaximum` | pass |
| Product metadata targets `2.3.2`. | `dotnet build .\desktop-loudness-meter.sln` | pass |
