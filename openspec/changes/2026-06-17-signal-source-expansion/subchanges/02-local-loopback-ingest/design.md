# Local Loopback Ingest Design

## Overview

This slice adds a localhost loopback source provider beside the existing WASAPI provider. It introduces a simple protocol, an in-process receiver service, optional UDP listener startup, and a diagnostic sender. The receiver feeds incoming float PCM into `PcmMetricAnalyzer` so the existing detail UI and DSP controls work without special-case rendering paths.

## Key Decisions

- UDP loopback is the first transport. It avoids blocking a future plugin audio thread and can be driven by a worker-side sender. This satisfies R5 and keeps TCP/session management out of the first pass.
- The first packet encoding is JSON UTF-8 with base64 float32 PCM payloads for audio. This is easy to inspect and test. A later plugin slice may add a binary fast path without changing the source-provider contract.
- Source identity is `loopback:<handle>`. Display names can change freely. Handles must be unique and stable from the sender side. This satisfies R2.
- `LocalLoopbackSignalSourceService` exposes both an in-memory `Ingest` method for deterministic tests and a UDP receive loop for runtime. This satisfies R1, R5, and R6.
- The app composes `WasapiSessionMeterService` and `LocalLoopbackSignalSourceService` through `CompositeSignalSourceMeterService`, rather than teaching `MainViewModel` about multiple providers. This satisfies R5.

## Data Model

- `LocalLoopbackPacketType`: `Register`, `Heartbeat`, `Rename`, `Audio`, `Close`.
- `LocalLoopbackPacket`: decoded packet with protocol version, handle, display name, source kind, sample rate, channels, sequence, and samples.
- `LocalLoopbackProtocol`: JSON encode/decode helper used by the receiver and diagnostic sender.
- `LocalLoopbackSignalSourceService.SourceState`: one active sender handle, analyzer, format, display name, kind, peak, last-seen time, and per-source analysis settings.

## Protocol

Common fields:
- `protocol`: `desktop-loudness-meter.loopback.v1`
- `type`: `register`, `heartbeat`, `rename`, `audio`, or `close`
- `handle`: sender-owned stable handle

Registration fields:
- `displayName`
- `sourceKind`: optional, `loopback` or `plugin`; defaults to `loopback`
- `sampleRate`
- `channels`: `1` or `2`

Audio fields:
- `sequence`: optional sender sequence number
- `samplesBase64`: little-endian IEEE float32 interleaved samples

## Implementation Units

### U1: Protocol Model
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackPacket.cs`
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackProtocol.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`

Responsibilities:
- Decode and encode v1 packets.
- Validate handles, formats, and sample payload alignment.

### U2: Loopback Source Service
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`

Responsibilities:
- Maintain source state by handle.
- Feed audio into `PcmMetricAnalyzer`.
- Expose `SignalSourceSnapshot` values.
- Implement reset/settings interfaces.
- Enforce heartbeat timeout and close handling.

### U3: UDP Receiver And Diagnostic Sender
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackDiagnosticSender.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`

Responsibilities:
- Bind UDP receive loop to `127.0.0.1`.
- Ingest decoded packets into the source service.
- Send diagnostic packets to loopback for manual and automated smoke paths.

### U4: App Composition
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/CompositeSignalSourceMeterService.cs`
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/CompositeSignalSourceMeterServiceTests.cs`

Responsibilities:
- Aggregate WASAPI and loopback source providers.
- Fan out reset/settings requests.
- Start WASAPI even if the UDP receiver cannot bind.

## Traceability

- R1: U1
- R2: U2
- R3: U2
- R4: U2
- R5: U3, U4
- R6: U1, U3

## Risks

- JSON audio packets are not the final high-performance plugin path. This is acceptable for the first receiver because the provider contract and analyzer path are the durable pieces.
- UDP has packet loss semantics. Localhost loss should be rare, and the next plugin slice can batch frames or add a binary fast path if needed.
- The app must tolerate port bind failure. The composition layer logs and continues with WASAPI sources.
