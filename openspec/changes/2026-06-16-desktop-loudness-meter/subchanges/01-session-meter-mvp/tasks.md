# Tasks

## Task 0: Set up the .NET solution and test framework

**Requirement Trace:** [R1, R4, R5] -> [U1, U2, U3]  
**Execution Mode:** [direct]

**Files:**
- `desktop-loudness-meter.sln` - solution
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - WPF app project
- `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - unit test project

**Acceptance Criteria:**
- [x] `dotnet build` succeeds for the solution.
- [x] `dotnet test` discovers and runs the empty test project.

**Dependencies:** None

## Task 1: Build the compact floating WPF overlay shell

**Requirement Trace:** [R1, R6] -> [U1]  
**Execution Mode:** [direct]

**Files:**
- `src/DesktopLoudnessMeter.App/App.xaml` - WPF application entry
- `src/DesktopLoudnessMeter.App/App.xaml.cs` - application startup
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - overlay layout and styling
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` - drag and window controls

**Acceptance Criteria:**
- [x] The main window is topmost, borderless, compact, and starts directly on the meter UI.
- [x] The window has a visible close command and can be dragged from the header.
- [ ] [manual] Launch the app and confirm it floats above a normal desktop window.

**Dependencies:** Task 0

## Task 2: Implement WASAPI session snapshot collection

**Requirement Trace:** [R2, R3, R4, R5] -> [U2]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/IAudioSessionMeterService.cs` - service contract
- `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs` - snapshot model
- `src/DesktopLoudnessMeter.App/Audio/PeakMath.cs` - peak conversion helper
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` - NAudio WASAPI integration
- `tests/DesktopLoudnessMeter.Tests/Audio/PeakMathTests.cs` - peak conversion tests

**Acceptance Criteria:**
- [x] Peak-to-dB conversion is covered by unit tests for silence, nominal values, and clipping boundaries.
- [x] The service returns snapshots keyed by session identifier or process fallback.
- [x] The service resolves friendly labels from process metadata when available and provides fallbacks.
- [x] Core Audio failures are converted into an empty snapshot set or recoverable status, not a UI crash.
- [ ] [manual] While audio is playing, at least one app/session row reports a changing peak value.

**Dependencies:** Task 0

## Task 3: Implement meter view models and live refresh

**Requirement Trace:** [R2, R4, R5] -> [U3]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/ObservableObject.cs` - property notification helper
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - row state and smoothing
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - refresh loop and row reconciliation
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - reconciliation tests

**Acceptance Criteria:**
- [x] Row reconciliation adds new sessions, updates existing sessions, and removes stale sessions.
- [x] Meter display values update without replacing rows unnecessarily.
- [x] Refresh failures update status text without terminating the refresh loop.
- [ ] [manual] With an app playing audio, the WPF meter updates continuously without visible UI freezes.

**Dependencies:** Task 2

## Task 4: Wire metric-honesty notes into UI and docs

**Requirement Trace:** [R6] -> [U1, U4]  
**Execution Mode:** [direct]

**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - visible metric label/status copy
- `docs/audio-metering-notes.md` - measurement semantics and next-slice notes

**Acceptance Criteria:**
- [x] The UI labels the value as session peak/level rather than LUFS.
- [x] The documentation explains why true LUFS/process-loopback analysis is deferred.

**Dependencies:** Task 1, Task 2
