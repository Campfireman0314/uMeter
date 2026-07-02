# Change Spec: Native Protocol Ingest Core

## Requirement R1: Native Protocol v1 Decode

**Trace**: R1

Native C++ Core MUST expose a receiver-side Protocol v1 decode API that accepts raw UTF-8 JSON bytes and returns a typed flat packet representation for `register`, `heartbeat`, `rename`, `audio`, `close`, and `ack`.

### Scenarios

- Given a valid `register` packet with protocol `desktop-loudness-meter.loopback.v1`, supported source kind, sample rate and channel count, decode returns packet type `Register`, handle, display name, source kind, sample rate and channels.
- Given a valid `audio` packet with base64 little-endian float32 samples, decode returns packet type `Audio`, handle, sequence and decoded samples.
- Given heartbeat, rename, close or ack packets, decode returns the corresponding packet type and required fields.

## Requirement R2: Native Invalid Packet Rejection

**Trace**: R2

Native decode MUST reject invalid protocol bytes without routing partial packets to source ingest.

### Scenarios

- Wrong protocol version, missing/invalid handle, unknown type, invalid sample rate, unsupported channel count, blank display name for register/rename, malformed base64, empty audio payload and non-float-aligned audio payload are rejected.
- Rejected packets produce no managed `LocalLoopbackPacket`, no source-service ingest call and no ack response.

## Requirement R3: Native Ack Encoding

**Trace**: R3

Native C++ Core MUST expose an ack encoding API that generates the current Protocol v1 ack JSON wire shape from a valid handle.

### Scenarios

- Given handle `plugin-a`, ack encoding produces UTF-8 JSON with protocol `desktop-loudness-meter.loopback.v1`, type `ack`, and handle `plugin-a`.
- Invalid handles are rejected and do not produce an ack payload.

## Requirement R4: Host Receiver Glue Boundary

**Trace**: R4

The Windows Host receiver MUST keep UDP socket transport and loopback remote filtering in Host glue while delegating protocol decode and ack encoding to native protocol ingest.

### Scenarios

- Non-loopback remote packets are ignored before native decode.
- Valid non-ack loopback packets are decoded through native protocol ingest, routed to `LocalLoopbackSignalSourceService.Ingest`, and acknowledged with a native-generated ack.
- Inbound ack packets are decoded but ignored for source ingest and ack response.
- `LocalLoopbackUdpReceiver` no longer directly calls `LocalLoopbackProtocol.TryDecode` or `LocalLoopbackProtocol.EncodeAck`.

## Requirement R5: Contract Parity And Build Coverage

**Trace**: R5

The migration MUST preserve Protocol v1 wire compatibility and native build/deployment coverage.

### Scenarios

- Existing C# Protocol sender-side encode tests continue to pass and remain the wire-shape oracle for SenderKit.
- Receiver route tests pass through native protocol decode/ack behavior.
- Native MSBuild Release and native file lists include the new protocol unit.
- CMake source list includes the new protocol unit for the reserved cross-platform C++ Core boundary.
