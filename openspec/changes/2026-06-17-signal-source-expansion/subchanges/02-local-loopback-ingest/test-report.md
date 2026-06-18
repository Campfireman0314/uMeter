# Test Report: 2026-06-17-signal-source-expansion/02-local-loopback-ingest

## Task 1: Implement protocol packet encoding and decoding

### Red Phase
- **Timestamp:** 2026-06-17 20:29
- **Failing Tests:**
  - `LocalLoopbackProtocolTests` - `CS0103`: `LocalLoopbackProtocol` and `LocalLoopbackPacketType` are missing product types.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:34
- **Passing Tests:**
  - `LocalLoopbackProtocolTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackPacket.cs` - added - Defines decoded loopback packet type and payload model.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackProtocol.cs` - added - Encodes and decodes v1 JSON packets with base64 little-endian float PCM samples.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - added - Covers packet round trips and invalid packet rejection.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Register, heartbeat, rename, audio, and close packets round-trip through protocol helpers. | `RegisterPacketRoundTrips`, `AudioPacketRoundTripsFloatSamples` plus invalid packet coverage for control fields | pass |
| Audio packets decode little-endian float32 base64 samples. | `AudioPacketRoundTripsFloatSamples` | pass |
| Invalid protocol, missing handle, unsupported format, and malformed audio packets are rejected without throwing from the public decode path. | `InvalidPacketsAreRejected` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:34
- **Passing Tests:**
  - `LocalLoopbackProtocolTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 2: Implement loopback source service

### Red Phase
- **Timestamp:** 2026-06-17 20:36
- **Failing Tests:**
  - `LocalLoopbackSignalSourceServiceTests` - `CS0246`: `LocalLoopbackSignalSourceService` is missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:40
- **Passing Tests:**
  - `LocalLoopbackSignalSourceServiceTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - added - Maintains loopback sources by handle, feeds PCM to `PcmMetricAnalyzer`, handles reset/settings, timeout, and close.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - added - Covers identity, rename, duplicate names, analyzer feed, reset/settings, timeout, and close.
- **Test Change Record:** No red assertions were modified; added reset-all, true-peak setting, and audio-refresh timeout assertions after first green.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Registering a handle creates one `loopback:<handle>` source with the requested display name and source kind. | `RegisterRenameAndDuplicateNamesPreserveHandleIdentity` | pass |
| Renaming changes only display name, not source key. | `RegisterRenameAndDuplicateNamesPreserveHandleIdentity` | pass |
| Same-name different-handle sources remain separate. | `RegisterRenameAndDuplicateNamesPreserveHandleIdentity` | pass |
| Audio updates peak and analyzer metrics. | `AudioFeedsAnalyzerAndSettingsControlChannelMetrics` | pass |
| Reset and analysis settings affect the matching source. | `AudioFeedsAnalyzerAndSettingsControlChannelMetrics` | pass |
| Heartbeat/audio refresh last-seen time and expired sources disappear. | `HeartbeatTimeoutCloseAndReregisterControlVisibility` | pass |
| Close removes a source. | `HeartbeatTimeoutCloseAndReregisterControlVisibility` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:40
- **Passing Tests:**
  - `LocalLoopbackSignalSourceServiceTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 3: Add UDP receiver and diagnostic sender

### Red Phase
- **Timestamp:** 2026-06-17 20:42
- **Failing Tests:**
  - `LocalLoopbackUdpReceiverTests` - `CS0103`/`CS0246`: `LocalLoopbackUdpReceiver` and `LocalLoopbackDiagnosticSender` are missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:45
- **Passing Tests:**
  - `LocalLoopbackUdpReceiverTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - added - Binds UDP to loopback, decodes packets, and ingests them into the source service.
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackDiagnosticSender.cs` - added - Sends v1 protocol packets to a loopback receiver for local diagnostics.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - added - Covers receiver ingestion, loopback binding, diagnostic sender, and receiver disposal.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| UDP receiver binds to loopback. | `ReceiverIngestsDiagnosticSenderPackets` | pass |
| UDP receiver ingests valid packets into the source service. | `ReceiverIngestsDiagnosticSenderPackets` | pass |
| Diagnostic sender sends register, heartbeat, rename, audio, and close packets to loopback. | `ReceiverIngestsDiagnosticSenderPackets` | pass |
| Disposing the receiver stops the receive loop. | `DisposingReceiverStopsPacketIngest` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:45
- **Passing Tests:**
  - `LocalLoopbackUdpReceiverTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 4: Compose WASAPI and loopback providers in the app

### Red Phase
- **Timestamp:** 2026-06-17 20:47
- **Failing Tests:**
  - `CompositeSignalSourceMeterServiceTests` - `CS0246`: `CompositeSignalSourceMeterService` is missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:50
- **Passing Tests:**
  - `CompositeSignalSourceMeterServiceTests` - pass
  - `LocalLoopbackUdpReceiverTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/CompositeSignalSourceMeterService.cs` - added - Aggregates child source providers and fans out reset/settings/dispose calls.
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - modified - Starts WASAPI plus loopback source services and uses non-throwing UDP receiver startup.
  - `tests/DesktopLoudnessMeter.Tests/Audio/CompositeSignalSourceMeterServiceTests.cs` - added - Covers source aggregation, reset/settings fanout, and disposal.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - modified - Covers receiver bind failure reporting without throwing.
- **Test Change Record:** No red assertions were modified; added UDP bind-failure coverage after app composition used `TryStart`.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Composite service returns sources from all child providers. | `ReturnsSourcesFromAllChildren` | pass |
| Reset-all and per-source reset fan out to child providers. | `ResetAndSettingsFanOutToChildren` | pass |
| Analysis settings fan out to child providers. | `ResetAndSettingsFanOutToChildren` | pass |
| App startup constructs WASAPI plus loopback provider and continues if UDP receiver startup fails. | `TryStartReportsBindFailureWithoutThrowing` plus `App.xaml.cs` startup code path | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:50
- **Passing Tests:**
  - `CompositeSignalSourceMeterServiceTests` - pass
  - `LocalLoopbackUdpReceiverTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A
