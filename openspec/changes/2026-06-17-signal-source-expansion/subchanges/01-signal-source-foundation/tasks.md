# Tasks

## Task 1: Introduce source contracts

**Requirement Trace:** [R1, R6] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceKind.cs` - implementation
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs` - implementation
- `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/Audio/SignalSourceSnapshotTests.cs` - tests

**Acceptance Criteria:**
- [x] Source snapshots expose `SourceKey`, `SourceKind`, `SourceKindLabel`, `DisplayName`, `State`, process id, and existing analyzer metric fields.
- [x] WASAPI source kind formats as a compact user-facing label.
- [x] No listener or plugin dependency is introduced.

**Dependencies:** None

## Task 2: Adapt WASAPI provider to source contracts

**Requirement Trace:** [R2, R3, R6] -> [U2]
**Execution Mode:** [characterization-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` - implementation
- `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs` - compatibility
- `src/DesktopLoudnessMeter.App/Audio/IAudioSessionMeterService.cs` - compatibility
- `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs` - tests

**Acceptance Criteria:**
- [x] Existing duplicate collapse, self-filter, silent-filter, and metric merge tests still pass through source snapshots.
- [x] Reset and analysis-settings delegation use source keys while preserving process-loopback routing.
- [x] WASAPI snapshots are labeled as `WasapiSession`.

**Dependencies:** Task 1

## Task 3: Move main view model reconciliation to source identity

**Requirement Trace:** [R3, R4] -> [U3]
**Execution Mode:** [characterization-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - implementation
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - implementation
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - tests

**Acceptance Criteria:**
- [x] Rows update in place by `SourceKey`.
- [x] Detail tracking survives transient WASAPI source-key changes by display-name recovery.
- [x] Missing process-backed sources remain until process exit.
- [x] Settings and reset requests carry source keys to the provider.

**Dependencies:** Task 2

## Task 4: Add minimal source markers to UI

**Requirement Trace:** [R5] -> [U4]
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - implementation
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - implementation
- `src/DesktopLoudnessMeter.App/App.xaml.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - tests

**Acceptance Criteria:**
- [x] Global rows expose a compact source-type label.
- [x] Inline detail and pop-up detail headers expose the selected source type.
- [x] Tray pop-up source names include source-type context.
- [x] [manual] Visual check confirms source badges fit in the compact window without overlapping row meter text.

**Dependencies:** Task 3
