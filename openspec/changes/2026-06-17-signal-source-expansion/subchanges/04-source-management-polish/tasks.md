# Tasks

## Task 1: Polish row identity and duplicate-name presentation

**Requirement Trace:** [R2, R3] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - implementation
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - implementation
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - implementation
- `src/DesktopLoudnessMeter.App/App.xaml.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - tests

**Acceptance Criteria:**
- [x] `DisplayName` remains the provider-owned display name.
- [x] Duplicate display-name rows expose a row title with compact source qualifier.
- [x] Unique display-name rows expose the clean display name.
- [x] Source identity text identifies WASAPI process ids and plugin/loopback handles.
- [x] Missing/reconnecting rows use source-facing status text.

**Dependencies:** None

## Task 2: Add deterministic source ordering and reconnect preference memory

**Requirement Trace:** [R1, R4] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - tests

**Acceptance Criteria:**
- [x] Plugin sources sort before local-loopback sources.
- [x] Local-loopback sources sort before WASAPI sources.
- [x] Same-kind sources sort by display name, then source key.
- [x] Per-source true-peak oversampling survives disappearance and reconnect.
- [x] Per-source waveform and spectrum channel modes survive disappearance and reconnect.
- [x] Global true-peak oversampling remains the default for newly seen sources.

**Dependencies:** Task 1

## Task 3: Document multi-source source management behavior

**Requirement Trace:** [R5] -> [U3]
**Execution Mode:** [direct]
**Files:**
- `README.md` - documentation
- `docs/source-management.md` - documentation
- `docs/current-status.md` - documentation

**Acceptance Criteria:**
- [x] README mentions mixed WASAPI/loopback/plugin source rows.
- [x] Docs explain duplicate names and stable handles.
- [x] Docs explain reconnect preference behavior.
- [x] Docs state that alias and visibility persistence are deferred future UI work.

**Dependencies:** Task 2
