---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-01-source-continuity-reconfiguration
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive:2026-07-01-native-host-projection-core
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs
  - src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs
  - src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs
  - src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp
  - src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp
superseded_by:
source_refs:
  - change:2026-06-29-native-source-ingest-core
  - evidence:openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md
  - change:2026-06-29-juce-sender-protocol-boundary
  - evidence:openspec/changes/archive/2026-06-29-juce-sender-protocol-boundary/evidence.md
  - change:2026-06-30-native-protocol-ingest-core
  - evidence:openspec/changes/archive/2026-06-30-native-protocol-ingest-core/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
---

# Signal Source Ingest

## Responsibility
Tracks local/plugin loopback source ingestion. Native `DlmLoopbackProtocol` owns receiver-side Protocol v1 JSON byte decode and ack encoding, while native `DlmSourceIngestCore` owns platform-neutral source registry, register/heartbeat/rename/audio/close lifecycle, timeout cleanup, same-handle reconfiguration, per-source analyzer ownership, analyzer settings, and flat snapshot/copy projection. C# `LocalLoopbackUdpReceiver` remains UDP socket and loopback-remote glue; `LocalLoopbackSignalSourceService` remains the thread-safe adapter for app clock/lock boundaries, source-key/source-kind mapping, and UI-facing `SignalSourceSnapshot`.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h | Native source-ingest C ABI DTOs and opaque core declarations |
| src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp | Native source registry, lifecycle state machine, timeout cleanup, analyzer ownership, settings, snapshot/copy functions |
| src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.h | Native Protocol v1 receiver-side decode/ack C ABI declarations |
| src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp | Native Protocol v1 JSON field parser, packet validation, float32 base64 decode, and ack encoding |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs | C# P/Invoke wrapper mapping native protocol packets back to existing `LocalLoopbackPacket` |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs | C# P/Invoke wrapper and managed `PcmMetricsSnapshot` projection |
| src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs | Thread-safe app adapter, packet routing, source-key mapping, clock/timeout calls, `SignalSourceSnapshot` mapping |
| src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs | Host UDP socket/routing glue; filters loopback remote, calls native protocol decode, ignores inbound ack, routes valid non-ack packets to source ingest, and sends native-generated ack |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceProjection.cs | Generic managed wrapper for native source-list projection; consumes merged source snapshots after WASAPI/local/plugin services have produced them |
| src/DesktopLoudnessMeter.Native/DlmHostSourceProjection.* | Native source-list projection over generic source metadata and workspace row state; not tied to local/plugin ingest handles |
| tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs | Registration, reconfiguration, timeout, close, and analyzer preference coverage |
| tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs | Deterministic non-socket receiver routing coverage plus skipped real UDP smoke |

## Implicit Constraints
- `Ingest`, settings updates, snapshots, and disposal are protected by the service lock before crossing the native source-ingest handle.
- Registering an existing handle with a new sample rate or channel count reconfigures the analyzer in place and preserves source key identity.
- Explicit close and timeout remove the source and dispose the analyzer; same-handle format restarts do not.
- C# remains responsible for `loopback:<handle>` source keys and `SignalSourceKind` mapping; native core stores handle-keyed registry and numeric source kind only.
- Host receiver remains responsible for UDP socket ownership, async receive/send, and loopback remote filtering. Protocol v1 JSON byte decode and ack payload generation are native core responsibilities via `DlmLoopbackProtocol`.
- Native `DlmSourceIngestCore` still receives typed handle/sourceKind/displayName/sampleRate/channel/sample values; it does not own UDP transport or protocol JSON parsing.
- Windows WASAPI/process loopback capture remains outside this module's native migration; it still flows through capture adapter services.
- Host source-list projection now runs after source services are merged and covers WASAPI, LocalLoopback, and Plugin rows through generic metadata. It does not change local/plugin ingest ownership or make WASAPI capture native.
