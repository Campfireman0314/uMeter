# Tasks

## Task 1: Persist hide and order preferences

**Requirement Trace:** [R2, R4, R5] -> [U1, U2]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`

**Acceptance Criteria:**
- [x] Hidden rows are filtered while their workspace entry remains.
- [x] `ShowHiddenSources` reveals hidden rows and restore clears the hidden flag.
- [x] Move/reorder persists contiguous order indices.
- [x] Reconnected/hibernating rows keep their explicit order.

**Dependencies:** none

## Task 2: Add row commands for alias/hide/reorder

**Requirement Trace:** [R1, R2, R3, R4] -> [U2, U3]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`

**Acceptance Criteria:**
- [x] Row title editing commits trimmed aliases and cancel restores current alias.
- [x] Hide/restore commands update the view model and persist preferences.
- [x] Move up/down commands change row order and can-execute state.

**Dependencies:** Task 1

## Task 3: Wire global source-list UI

**Requirement Trace:** [R1, R2, R3, R4] -> [U3]  
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`

**Acceptance Criteria:**
- [x] Title area supports double-click-to-edit alias.
- [x] Separate row alias editor is removed.
- [x] Each row exposes hide/restore and move controls.
- [x] Global view exposes a show-hidden toggle.

**Dependencies:** Task 2
