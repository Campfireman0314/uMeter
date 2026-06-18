# Tasks

## Task 1: Add analyzer-ready metric state and option types

**Requirement Trace:** [R1, R2, R6] -> [U1]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs` - optional analyzer metrics
- `src/DesktopLoudnessMeter.App/ViewModels/MeterDisplayMode.cs` - metric mode options
- `src/DesktopLoudnessMeter.App/ViewModels/TruePeakOversampling.cs` - oversampling options
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - display-state tests

**Acceptance Criteria:**
- [x] Rows can display Peak by default.
- [x] Rows display an unavailable LUFS placeholder when LUFS mode is selected without analyzer data.
- [x] True-peak oversampling exposes 2x, 4x, and 8x options.

**Dependencies:** None

## Task 2: Add view-model commands for metric controls, topmost, details, and reset

**Requirement Trace:** [R1, R2, R3, R4, R5, R6, R8] -> [U2]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/RelayCommand.cs` - command helper
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - global controls and detail requests
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - row controls and metric display
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - command/state tests

**Acceptance Criteria:**
- [x] Changing metric mode updates existing row primary-value text.
- [x] True-peak toggle and oversampling selection are stored in the main view model.
- [x] Global reset advances all row reset counters.
- [x] Per-session reset advances only the selected row reset counter.
- [x] Row detail command raises a detail-open request for that row.
- [x] Always-on-top state can be toggled from the view model.
- [x] Detail selection stays inside the main view model and can return to the global list.
- [x] Default refresh cadence is below 100 ms.
- [x] Display-level smoothing moves toward rising values without jumping directly to the target.

**Dependencies:** Task 1

## Task 3: Add main-window controls and row detail actions

**Requirement Trace:** [R1, R2, R3, R4, R5] -> [U3]  
**Execution Mode:** [direct]

**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - controls and row actions
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` - compact window shell handlers

**Acceptance Criteria:**
- [x] Main window includes Peak/LUFS, true-peak toggle, oversampling selector, topmost toggle, and global reset controls.
- [x] Session rows include a detail action.
- [ ] [manual] Launch the app and confirm controls are visible and do not overlap in the compact window.

**Dependencies:** Task 2

## Task 4: Add inline per-session detail panel

**Requirement Trace:** [R3, R5, R6, R7] -> [U4]  
**Execution Mode:** [direct]

**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - inline detail metric layout
- `src/DesktopLoudnessMeter.App/Controls/ScaledLevelMeter.cs` - marked dB-scale level meter drawing
- `src/DesktopLoudnessMeter.App/Controls/StereoFanView.cs` - semi-circle stereo image drawing
- `src/DesktopLoudnessMeter.App/Controls/CorrelationMeter.cs` - -1..+1 correlation arrow drawing

**Acceptance Criteria:**
- [x] Inline detail panel displays session name and fields for Momentary loudness, Short-term loudness, Integrated loudness, True-peak level, and 100 ms RMS level.
- [x] Level metrics are marked dB-scale horizontal meters with numeric values to their right.
- [x] Stereo image graph appears as a semi-circle fan with glowing sample dots below level meters.
- [x] Correlation appears at the bottom as a floating arrow over a -1..+1 bar.
- [x] Inline detail panel includes a per-session reset action and top-left return button.
- [ ] [manual] Opening details from a row switches the same window into detail mode and Back returns to the global list.

**Dependencies:** Task 2, Task 3
