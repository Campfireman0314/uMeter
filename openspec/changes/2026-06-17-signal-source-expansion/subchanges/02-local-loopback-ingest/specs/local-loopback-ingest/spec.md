# Local Loopback Ingest Spec

## Requirements

### Requirement: Protocol V1 Packets
**Trace**: R1

The app must define a localhost loopback protocol for source registration, heartbeat, rename, audio, and close messages.

#### Acceptance Criteria
- Protocol packets include a version identifier.
- Registration packets include a stable sender handle, display name, source kind, sample rate, and channel count.
- Rename packets update display name without changing the source key.
- Audio packets carry interleaved float PCM associated with an existing handle.
- Close packets remove the source from current snapshots.
- Invalid protocol, missing handle, unsupported sample rate, unsupported channel count, and malformed audio payloads are rejected without crashing the app.

### Requirement: Stable Remote Handle Identity
**Trace**: R2

The receiver must manage remote senders by stable handle rather than display name.

#### Acceptance Criteria
- Two packets with the same handle update one source.
- Renaming a source keeps the same `SourceKey`.
- Two sources with the same display name but different handles remain separate.
- Re-registering an expired handle recovers the same `SourceKey`.

### Requirement: Analyzer Feed
**Trace**: R3

Received PCM must feed the same analyzer pipeline used by existing detail views.

#### Acceptance Criteria
- Audio packets update peak, dB text, true-peak, RMS, stereo/correlation, waveform, and spectrum metrics where enough samples exist.
- Reset clears analyzer state for one loopback source.
- Reset-all clears analyzer state for all loopback sources.
- Per-source true-peak oversampling, waveform channel mode, and spectrum channel mode update the analyzer used by loopback sources.

### Requirement: Heartbeat Timeout
**Trace**: R4

Loopback sources must disappear when the sender stops heartbeating or sending audio for longer than the configured timeout.

#### Acceptance Criteria
- Heartbeat updates the source's last-seen timestamp.
- Audio updates the source's last-seen timestamp.
- Expired sources are omitted from current snapshots.
- A later registration with the same handle creates a fresh active source with the same source key.

### Requirement: UDP Localhost Receiver
**Trace**: R5

The app must be able to receive protocol packets over local network loopback without replacing the WASAPI provider.

#### Acceptance Criteria
- The UDP receiver binds only to loopback.
- Startup failure of the UDP receiver does not prevent WASAPI metering from starting.
- The app combines WASAPI and loopback sources in one source list.
- Disposing the app stops the UDP receiver.

### Requirement: Diagnostic Sender
**Trace**: R6

The codebase must include a small diagnostic sender path for repeatable local testing without a DAW plugin.

#### Acceptance Criteria
- The diagnostic sender can send register, heartbeat, rename, audio, and close packets.
- The diagnostic sender targets loopback by default.
- Tests can verify the protocol without opening a DAW.
