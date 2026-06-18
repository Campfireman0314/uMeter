# Test Report: 2026-06-17-signal-source-expansion/03-plugin-sender-prototypes

## Task 1: Extract loopback protocol into a shared project

### Characterization Phase
- **Timestamp:** 2026-06-17 20:52
- **Passing Tests:**
  - `LocalLoopbackProtocolTests` - pass before extraction, 10 tests
- **Locked Behavior:**
  - v1 register, heartbeat, rename, audio, and close packets round-trip.
  - Invalid protocol, missing handle, unsupported format, and malformed audio are rejected without throwing.

### Green Phase
- **Timestamp:** 2026-06-17 20:52
- **Passing Tests:**
  - `LocalLoopbackProtocolTests` - pass
  - `LocalLoopbackSignalSourceServiceTests` - pass
  - `LocalLoopbackUdpReceiverTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.Protocol/DesktopLoudnessMeter.Protocol.csproj` - added - Hosts protocol code outside the WPF app assembly.
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs` - added - Defines reusable packet and packet-type models.
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs` - added - Moves v1 JSON/base64 protocol encoding and decoding into the shared project.
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackSourceKind.cs` - added - Defines sender-facing loopback/plugin source kind values.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackPacket.cs` - deleted - Replaced by the shared protocol model.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackProtocol.cs` - deleted - Replaced by the shared protocol codec.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - modified - Maps protocol source kinds into app source kinds.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - modified - Decodes packets from the shared protocol project.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackDiagnosticSender.cs` - modified - Encodes packets through the shared protocol project.
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - modified - References the shared protocol project.
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - modified - References the shared protocol project.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - modified - Tests the shared protocol namespace.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - modified - Uses sender-facing protocol source kinds.
- **Test Change Record:** No behavior assertions were changed; imports and protocol source-kind enum names were updated after extraction.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Existing protocol round-trip and invalid-packet behavior is locked by characterization tests before extraction. | `LocalLoopbackProtocolTests` pre-extraction run | pass |
| Protocol types build from a non-UI project. | `dotnet test` builds `DesktopLoudnessMeter.Protocol` | pass |
| App receiver still decodes v1 register, heartbeat, rename, audio, and close packets. | `LocalLoopbackUdpReceiverTests`, `LocalLoopbackSignalSourceServiceTests` | pass |
| Sender-side projects can reference protocol without WPF dependencies. | `DesktopLoudnessMeter.Protocol` targets `net8.0` without WPF settings | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:52
- **Passing Tests:**
  - `LocalLoopbackProtocolTests`, `LocalLoopbackSignalSourceServiceTests`, `LocalLoopbackUdpReceiverTests` - pass, 16 tests
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 2: Implement sender identity and real-time-safe handoff

### Red Phase
- **Timestamp:** 2026-06-17 20:56
- **Failing Tests:**
  - `SenderIdentityStoreTests` - `CS0234`: `DesktopLoudnessMeter.SenderKit` namespace is missing.
  - `LocalLoopbackSenderTests` - `CS0234`: `DesktopLoudnessMeter.SenderKit` namespace is missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:56
- **Passing Tests:**
  - `SenderIdentityStoreTests` - pass
  - `LocalLoopbackSenderTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs` - modified - Exposes sender-handle validation for sender-side options.
  - `src/DesktopLoudnessMeter.SenderKit/DesktopLoudnessMeter.SenderKit.csproj` - added - Hosts reusable sender-side code.
  - `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs` - added - Validates sender handle, display name, format, endpoint, and queue settings.
  - `src/DesktopLoudnessMeter.SenderKit/SenderIdentityStore.cs` - added - Persists and reloads generated DAW-free diagnostic handles.
  - `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs` - added - Sends register/heartbeat/rename/audio/close packets with bounded pooled audio handoff.
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - modified - References the sender-kit project.
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/SenderIdentityStoreTests.cs` - added - Covers identity persistence.
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs` - added - Covers plugin source lifecycle, audio handoff drops, and invalid handles.
- **Test Change Record:** The lifecycle test's audio packet size changed from 4800 frames to 480 frames after Windows rejected the larger JSON/base64 UDP datagram; the sender default remains small-packet oriented for this diagnostic transport.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Explicit handles are used as stable sender identities. | `SenderCreatesRenamesStreamsAndClosesPluginSource` | pass |
| Generated handles persist and reload from an identity file. | `GeneratedHandlesPersistAndReload` | pass |
| Rename sends a rename message without changing the source key. | `SenderCreatesRenamesStreamsAndClosesPluginSource` | pass |
| Audio submission uses bounded preallocated buffers and returns `false` when full. | `AudioHandoffDropsInsteadOfBlockingWhenPoolIsFull` | pass |
| Worker-side sending creates, updates, renames, and closes a plugin source through the app receiver path. | `SenderCreatesRenamesStreamsAndClosesPluginSource` | pass |
| Dropped audio packet count is observable. | `AudioHandoffDropsInsteadOfBlockingWhenPoolIsFull` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:56
- **Passing Tests:**
  - `SenderIdentityStoreTests`, `LocalLoopbackSenderTests` - pass, 6 tests
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

### Additional Red Phase
- **Timestamp:** 2026-06-17 21:02
- **Failing Tests:**
  - `LocalLoopbackSenderTests.OversizedFrameChunksAreRejectedForUdpJsonTransport` - `Assert.Throws` failure: oversized 4800-frame stereo JSON/base64 UDP packets were not rejected.
- **False Red Fixes:** none

### Additional Green Phase
- **Timestamp:** 2026-06-17 21:02
- **Passing Tests:**
  - `LocalLoopbackSenderTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs` - modified - Rejects frame chunk sizes that exceed the v1 UDP JSON transport packet budget.
  - `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs` - modified - Sends registration before starting queued audio worker tasks so queued packets cannot race ahead of registration.
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs` - modified - Covers oversized frame chunk rejection.
- **Test Change Record:** No red assertions were modified.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Audio submission uses bounded preallocated buffers and diagnostic transport limits. | `OversizedFrameChunksAreRejectedForUdpJsonTransport` | pass |

### Additional Refactor Phase
- **Timestamp:** 2026-06-17 21:04
- **Passing Tests:**
  - `LocalLoopbackSenderTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs` - modified - Treats sender-side socket failures as dropped packets instead of surfacing them during shutdown.
- **Coverage:** N/A

## Task 3: Add DAW-free sender probe

### Red Phase
- **Timestamp:** 2026-06-17 20:58
- **Failing Tests:**
  - `SenderProbeOptionsTests` - `CS0234`: `DesktopLoudnessMeter.SenderProbe` namespace is missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:58
- **Passing Tests:**
  - `SenderProbeOptionsTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.SenderProbe/DesktopLoudnessMeter.SenderProbe.csproj` - added - Builds the DAW-free sender probe executable.
  - `src/DesktopLoudnessMeter.SenderProbe/Program.cs` - added - Entrypoint for the probe.
  - `src/DesktopLoudnessMeter.SenderProbe/SenderProbeProgram.cs` - added - Handles command dispatch and usage text.
  - `src/DesktopLoudnessMeter.SenderProbe/SenderProbeOptions.cs` - added - Parses and validates sender probe CLI options.
  - `src/DesktopLoudnessMeter.SenderProbe/SineWaveGenerator.cs` - added - Generates interleaved mono/stereo sine PCM.
  - `src/DesktopLoudnessMeter.SenderProbe/SenderProbeRunner.cs` - added - Runs register, stream, optional rename, and close through the sender kit.
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - modified - References the sender-probe project.
  - `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs` - added - Covers option parsing, sine generation, and receiver-facing probe lifecycle.
- **Test Change Record:** Added `ProbeRunnerRegistersStreamsRenamesAndCloses` after initial green to cover the executable smoke behavior through the receiver path; no red assertions were modified.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Probe options parse display name, handle, sample rate, channels, duration, frequency, amplitude, frame chunk size, rename target, and port. | `ParsesProbeOptions` | pass |
| Probe can generate interleaved mono or stereo sine samples. | `SineGeneratorProducesInterleavedSamplesInsideAmplitude` | pass |
| Probe registers as a plugin-kind source, streams audio, optionally renames, and closes. | `ProbeRunnerRegistersStreamsRenamesAndCloses` | pass |
| [manual] Running the probe while the app is open shows a `Plugin` source row and animated metrics. | pending manual validation | pending |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:58
- **Passing Tests:**
  - `SenderProbeOptionsTests` - pass, 3 tests
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs` - modified - Removed an unused import after the integration test was added.
  - `src/DesktopLoudnessMeter.SenderProbe/SenderProbeOptions.cs` - modified - Parses numeric CLI values with invariant culture for predictable probe commands.
- **Coverage:** N/A

### Pending Manual Validation
- [x] [manual] Running the probe while the app is open shows a `Plugin` source row and animated metrics - validation method: user smoke-tested app plus probe on 2026-06-18 and reported all pass.

## Task 4: Document plugin packaging status and smoke flow

### Direct Implementation
- **Timestamp:** 2026-06-17 20:59
- **Validation:**
  - Documentation reviewed against implemented project names and current SDK/toolchain observations.
- **Change Summary:**
  - `docs/plugin-sender-prototype.md` - added - Documents sender probe smoke flow, VST3/AAX packaging status, future wrapper boundary, and SDK/toolchain constraints.
  - `README.md` - modified - Adds the shared protocol, sender kit, sender probe, and new docs to the project layout.
