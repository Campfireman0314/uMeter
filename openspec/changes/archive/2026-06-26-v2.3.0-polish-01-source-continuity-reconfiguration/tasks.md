# Tasks

## Task 1: Preserve graph buffers during hibernation and analyzer warmup

**Requirement Trace:** [R3, R4, R5] -> [U3]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - row update and hibernation behavior
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - row-level graph-retention tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - source-retention regression tests

**Acceptance Criteria:**
- [x] Handle-missing state keeps existing waveform/spectrum buffers and does not advance plot ticks.
- [x] Live updates with null/empty graph buffers keep existing buffers until fresh non-empty buffers arrive.
- [x] Explicit reset and instance-not-found still clear graph buffers.

**Dependencies:** none

## Task 2: Reconfigure same-handle loopback sources in place

**Requirement Trace:** [R1, R5] -> [U1]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - same-handle register reconfiguration
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - reconfiguration tests

**Acceptance Criteria:**
- [x] Re-registering the same handle at a different sample rate keeps one source with the same source key.
- [x] Reconfiguration preserves source-level analysis settings.
- [x] Explicit close and heartbeat timeout still remove sources.

**Dependencies:** Task 1

## Task 3: Avoid sender close packets during same-handle JUCE restarts

**Requirement Trace:** [R2, R5] -> [U2]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h` - sender stop/restart API internals
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - same-handle restart behavior
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp` - packet-order regression test

**Acceptance Criteria:**
- [x] Calling `start` again with the same handle emits a new register without an intervening close.
- [x] Explicit `stop` still emits close.
- [x] Existing lifecycle and ack tests continue to pass.

**Dependencies:** none
