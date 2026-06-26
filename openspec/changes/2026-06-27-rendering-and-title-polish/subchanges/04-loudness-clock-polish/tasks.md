# Tasks

## Task 1: Stabilize clock phase and scale planning

**Requirement Trace:** [R1, R3] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs` - elapsed cycle phase mapping
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs` - scale marker planning
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - stable phase coverage
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockScalePlannerTests.cs` - marker coverage

**Acceptance Criteria:**
- [x] Elapsed times at 0 and one full cycle map to the same top angle.
- [x] Elapsed times beyond one full cycle preserve the same fixed table phase instead of shifting retained samples by list index.
- [x] The scale planner returns at least five marker values from minimum to maximum loudness.

**Dependencies:** None

## Task 2: Add cycle and warning settings

**Requirement Trace:** [R2, R4, R7] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - persisted clock cycle and warning settings
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - inline setting properties
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - pop-up setting properties
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - inline persistence/clamp coverage
- `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs` - pop-up persistence/clamp coverage

**Acceptance Criteria:**
- [x] Cycle duration defaults to 30 seconds and clamps to 10..600 seconds.
- [x] Warning loudness clamps to the current clock minimum/maximum loudness range.
- [x] Cycle duration and warning loudness persist per source in inline detail.
- [x] Cycle duration and warning loudness persist per source in pop-up detail.

**Dependencies:** Task 1

## Task 3: Render filled warning-aware clock fan and wire UI

**Requirement Trace:** [R5, R6, R2, R4] -> [U1, U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs` - timestamped samples, filled fan rendering, warning color
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - inline cycle/warning controls and bindings
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - pop-up cycle/warning controls and bindings
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs` - warning classification coverage

**Acceptance Criteria:**
- [x] Samples at or above warning loudness are classified for warning rendering.
- [x] The control uses filled fan geometry between the inner radius and loudness radius.
- [x] Inline and pop-up clock controls expose cycle duration and warning loudness.
- [ ] [manual] Live UI shows a stable non-spinning clock table with fan fill and warning color - validation method: run the WPF app and inspect at least one complete cycle.

**Dependencies:** Tasks 1, 2
