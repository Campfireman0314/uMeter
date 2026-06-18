# Tasks

## Task 1: Implement protocol packet encoding and decoding

**Requirement Trace:** [R1, R6] -> [U1]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackPacket.cs` - implementation
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackProtocol.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - tests

**Acceptance Criteria:**
- [x] Register, heartbeat, rename, audio, and close packets round-trip through protocol helpers.
- [x] Audio packets decode little-endian float32 base64 samples.
- [x] Invalid protocol, missing handle, unsupported format, and malformed audio packets are rejected without throwing from the public decode path.

**Dependencies:** None

## Task 2: Implement loopback source service

**Requirement Trace:** [R2, R3, R4] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - tests

**Acceptance Criteria:**
- [x] Registering a handle creates one `loopback:<handle>` source with the requested display name and source kind.
- [x] Renaming changes only display name, not source key.
- [x] Same-name different-handle sources remain separate.
- [x] Audio updates peak and analyzer metrics.
- [x] Reset and analysis settings affect the matching source.
- [x] Heartbeat/audio refresh last-seen time and expired sources disappear.
- [x] Close removes a source.

**Dependencies:** Task 1

## Task 3: Add UDP receiver and diagnostic sender

**Requirement Trace:** [R5, R6] -> [U3]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - implementation
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackDiagnosticSender.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - tests

**Acceptance Criteria:**
- [x] UDP receiver binds to loopback.
- [x] UDP receiver ingests valid packets into the source service.
- [x] Diagnostic sender sends register, heartbeat, rename, audio, and close packets to loopback.
- [x] Disposing the receiver stops the receive loop.

**Dependencies:** Task 2

## Task 4: Compose WASAPI and loopback providers in the app

**Requirement Trace:** [R5] -> [U4]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Audio/CompositeSignalSourceMeterService.cs` - implementation
- `src/DesktopLoudnessMeter.App/App.xaml.cs` - implementation
- `tests/DesktopLoudnessMeter.Tests/Audio/CompositeSignalSourceMeterServiceTests.cs` - tests

**Acceptance Criteria:**
- [x] Composite service returns sources from all child providers.
- [x] Reset-all and per-source reset fan out to child providers.
- [x] Analysis settings fan out to child providers.
- [x] App startup constructs WASAPI plus loopback provider and continues if UDP receiver startup fails.

**Dependencies:** Task 3
