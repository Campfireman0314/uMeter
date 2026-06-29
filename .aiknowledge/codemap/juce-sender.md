---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-01-source-continuity-reconfiguration
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.JucePlugin/Source
  - src/DesktopLoudnessMeter.JucePlugin/Tests
superseded_by:
---

# JUCE Sender

## Responsibility
Owns the VST3 sender side of the local loopback protocol: worker-thread lifecycle, register/heartbeat/audio/rename/close packets, realtime audio queueing, receiver ack polling, and sender connection state.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp | Sender start/stop/restart behavior, UDP worker loop, packet emission, ack polling, and close-suppression state |
| src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h | Sender state, atomics, queue ownership, and worker declarations |
| src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp | Lifecycle packets, ack state, and same-handle restart packet-order coverage |
| src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt | VST3 project version and build definition |

## Implicit Constraints
- Same-handle restarts during host sample-rate/channel changes must emit a new register without an intervening close.
- Explicit stop, plugin unload, and destruction still emit close when the worker was active.
- Shutdown-mode flags must be published before waking or stopping the worker thread.
