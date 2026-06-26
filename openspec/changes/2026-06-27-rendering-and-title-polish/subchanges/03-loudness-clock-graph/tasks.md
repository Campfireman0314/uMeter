# Tasks

## Task 1: Add loudness clock mapper and rendering control

**Requirement Trace:** [R2, R3, R4, R5] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - loudness-to-radius and angle mapping
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - WPF clock graph control
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - mapper coverage

**Acceptance Criteria:**
- [x] Samples below the configured minimum are not mapped for plotting.
- [x] Minimum loudness maps to the inner radius.
- [x] Maximum and above-maximum loudness map to the outer radius.
- [x] Intermediate loudness maps proportionally between inner and outer radii.
- [x] The control keeps bounded history and draws without per-frame unbounded allocation growth.

**Dependencies:** None

## Task 2: Expose a dedicated momentary loudness clock feed

**Requirement Trace:** [R1] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - raw momentary value and clock plot tick
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - tick behavior coverage

**Acceptance Criteria:**
- [x] Finite momentary loudness on a fresh plot frame advances `LoudnessClockPlotTick`.
- [x] Null/non-finite momentary loudness does not advance the clock plot tick.
- [x] Reset clears/advances the clock feed consistently with other plot ticks.

**Dependencies:** Task 1

## Task 3: Persist and integrate loudness clock controls

**Requirement Trace:** [R6, R7, R8, R9] -> [U3]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - persisted source settings
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - inline clock settings
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - pop-up clock settings
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - inline detail controls and graph
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - pop-up detail controls and graph
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - inline persistence coverage
- `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs` - pop-up persistence coverage

**Acceptance Criteria:**
- [x] Inline detail exposes show/hide and minimum/maximum loudness controls.
- [x] Pop-up detail exposes show/hide and minimum/maximum loudness controls.
- [x] Clock visibility and range settings persist per source.
- [x] Range setters preserve at least the minimum span.
- [ ] [manual] Live UI shows a circular loudness clock without overlapping existing detail controls - validation method: run the WPF app and inspect inline and pop-up detail at compact and wide widths.

**Dependencies:** Tasks 1, 2
