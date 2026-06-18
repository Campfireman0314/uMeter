# Plugin Sender Prototypes Design

## Overview

This slice turns the previous receiver-only loopback support into a sender-ready architecture. Because the VST3 and AAX SDKs are not present in this repo, the first deliverable is a plugin-style sender core plus a DAW-free sender probe. The sender core is designed so a future VST3 or AAX wrapper can call a small non-blocking audio handoff API from its process callback while a worker thread owns protocol encoding and UDP I/O.

## Key Decisions

- Move protocol types into `DesktopLoudnessMeter.Protocol` so sender code does not reference the WPF app.
- Use a sender-side `LocalLoopbackSender` in `DesktopLoudnessMeter.SenderKit` as the durable wrapper boundary for future plugin formats.
- Keep the audio-path API synchronous and non-blocking: `TryEnqueueAudio(ReadOnlySpan<float>)` copies into a bounded pool and returns `false` on overflow.
- Keep JSON/base64 v1 packets for compatibility with the receiver from `02-local-loopback-ingest`; binary transport can be introduced later behind the same sender/app contracts.
- Add `DesktopLoudnessMeter.SenderProbe` as the current smoke tool. It behaves like a plugin source from the app's point of view but does not require a DAW.

## Implementation Units

### U1: Shared Protocol Project
Module Boundary:
- `src/DesktopLoudnessMeter.Protocol/DesktopLoudnessMeter.Protocol.csproj`
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs`
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs`
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackSourceKind.cs`
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
- `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`

Responsibilities:
- Host protocol packet models and v1 JSON encode/decode helpers outside the app assembly.
- Preserve existing protocol behavior through characterization tests.
- Provide source-kind values needed by sender-side code without depending on app UI types.

### U2: Sender Kit
Module Boundary:
- `src/DesktopLoudnessMeter.SenderKit/DesktopLoudnessMeter.SenderKit.csproj`
- `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs`
- `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs`
- `src/DesktopLoudnessMeter.SenderKit/SenderIdentityStore.cs`
- `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs`
- `tests/DesktopLoudnessMeter.Tests/SenderKit/SenderIdentityStoreTests.cs`

Responsibilities:
- Manage sender identity, display name, protocol kind, sample format, endpoint, heartbeat, rename, audio handoff, drop counting, and close.
- Use a bounded preallocated audio buffer pool for real-time-friendly caller behavior.
- Send protocol packets from a worker path rather than from the caller's audio path.

### U3: Sender Probe
Module Boundary:
- `src/DesktopLoudnessMeter.SenderProbe/DesktopLoudnessMeter.SenderProbe.csproj`
- `src/DesktopLoudnessMeter.SenderProbe/Program.cs`
- `src/DesktopLoudnessMeter.SenderProbe/SenderProbeOptions.cs`
- `src/DesktopLoudnessMeter.SenderProbe/SineWaveGenerator.cs`
- `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs`

Responsibilities:
- Provide a command-line source that registers, streams generated sine PCM, optionally renames, and closes.
- Support configurable handle, display name, sample rate, channels, duration, tone frequency, amplitude, frame chunk size, and port.
- Keep the smoke path DAW-free and repeatable.

### U4: Plugin Feasibility Docs
Module Boundary:
- `docs/plugin-sender-prototype.md`
- `openspec/changes/2026-06-17-signal-source-expansion/subchanges/03-plugin-sender-prototypes/review-report.md`

Responsibilities:
- Document how to run the probe against the app.
- State that real VST3/AAX binaries are not produced in this pass without SDK integration.
- Record the future wrapper boundary and the current local SDK/toolchain observations.

## Traceability

- R1: U1
- R2: U2, U3
- R3: U2
- R4: U3
- R5: U2, U3
- R6: U4

## Risks

- JSON/base64 packets are still not ideal for heavy plugin throughput. This pass keeps compatibility and focuses on sender boundaries; a later slice can introduce binary packets if profiling demands it.
- The bounded pool copies sample data before the worker sends it. This is intentional so plugin callbacks never retain host-owned buffers.
- Real VST3/AAX packaging remains conditional on installing and licensing the relevant SDKs.
