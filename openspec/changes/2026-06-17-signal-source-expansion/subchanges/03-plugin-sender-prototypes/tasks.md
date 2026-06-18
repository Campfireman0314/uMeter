# Tasks

## Task 1: Extract loopback protocol into a shared project

**Requirement Trace:** [R1] -> [U1]
**Execution Mode:** [characterization-first]
**Files:**
- `src/DesktopLoudnessMeter.Protocol/DesktopLoudnessMeter.Protocol.csproj` - implementation
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs` - implementation
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs` - implementation
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackSourceKind.cs` - implementation
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - implementation
- `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - tests
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - tests

**Acceptance Criteria:**
- [x] Existing protocol round-trip and invalid-packet behavior is locked by characterization tests before extraction.
- [x] Protocol types build from a non-UI project.
- [x] App receiver still decodes v1 register, heartbeat, rename, audio, and close packets.
- [x] Sender-side projects can reference protocol without WPF dependencies.

**Dependencies:** None

## Task 2: Implement sender identity and real-time-safe handoff

**Requirement Trace:** [R2, R3, R5] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.SenderKit/DesktopLoudnessMeter.SenderKit.csproj` - implementation
- `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs` - implementation
- `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs` - implementation
- `src/DesktopLoudnessMeter.SenderKit/SenderIdentityStore.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs` - tests
- `tests/DesktopLoudnessMeter.Tests/SenderKit/SenderIdentityStoreTests.cs` - tests

**Acceptance Criteria:**
- [x] Explicit handles are used as stable sender identities.
- [x] Generated handles persist and reload from an identity file.
- [x] Rename sends a rename message without changing the source key.
- [x] Audio submission uses bounded preallocated buffers and returns `false` when full.
- [x] Worker-side sending creates, updates, renames, and closes a plugin source through the app receiver path.
- [x] Dropped audio packet count is observable.

**Dependencies:** Task 1

## Task 3: Add DAW-free sender probe

**Requirement Trace:** [R4, R5] -> [U3]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.SenderProbe/DesktopLoudnessMeter.SenderProbe.csproj` - implementation
- `src/DesktopLoudnessMeter.SenderProbe/Program.cs` - implementation
- `src/DesktopLoudnessMeter.SenderProbe/SenderProbeOptions.cs` - implementation
- `src/DesktopLoudnessMeter.SenderProbe/SineWaveGenerator.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs` - tests

**Acceptance Criteria:**
- [x] Probe options parse display name, handle, sample rate, channels, duration, frequency, amplitude, frame chunk size, rename target, and port.
- [x] Probe can generate interleaved mono or stereo sine samples.
- [x] Probe registers as a plugin-kind source, streams audio, optionally renames, and closes.
- [x] [manual] Running the probe while the app is open shows a `Plugin` source row and animated metrics.

**Dependencies:** Task 2

## Task 4: Document plugin packaging status and smoke flow

**Requirement Trace:** [R6] -> [U4]
**Execution Mode:** [direct]
**Files:**
- `docs/plugin-sender-prototype.md` - documentation

**Acceptance Criteria:**
- [x] Docs include the exact sender-probe command for local smoke tests.
- [x] Docs state that real VST3/AAX binaries are not emitted in this pass.
- [x] Docs describe the future plugin wrapper boundary.
- [x] Docs record current SDK/toolchain constraints.

**Dependencies:** Task 3
