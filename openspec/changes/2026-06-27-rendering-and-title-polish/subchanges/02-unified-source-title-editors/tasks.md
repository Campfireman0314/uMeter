# Tasks

## Task 1: Convert inline detail alias bar into title-area editing

**Requirement Trace:** [R1, R2, R3, R7, R8, R9, R10, R11] -> [U1]
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - inline detail title and toolbar layout
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` - existing title-edit handlers

**Acceptance Criteria:**
- [x] Inline detail title enters alias-edit mode from the displayed source title.
- [x] Enter, Escape, and focus-loss preserve existing commit/cancel behavior.
- [x] The separate inline detail "Alias" naming row is removed.
- [x] The inline detail forget-source command remains available.

**Dependencies:** None

## Task 2: Convert pop-up detail alias bar into title-area editing

**Requirement Trace:** [R4, R5, R6, R7, R9, R10, R11] -> [U2]
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - pop-up detail title layout
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs` - title-edit handlers

**Acceptance Criteria:**
- [x] Pop-up content title enters alias-edit mode from the displayed source title.
- [x] Enter, Escape, and focus-loss preserve existing commit/cancel behavior.
- [x] The separate pop-up detail "Alias" naming row is removed.
- [x] The chrome header remains display-only and draggable.

**Dependencies:** None
