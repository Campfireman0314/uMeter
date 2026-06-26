---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-01-source-continuity-reconfiguration
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp
  - src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs
  - src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs
superseded_by:
---

# Same-Handle Reconfiguration

## Trigger
`LoopbackSender::start()` receives a config with the active sender handle and changed audio format.

## Call Chain
1. `LoopbackSender::start()`
   -> `stopInternal(false)` suppresses close for the active same-handle worker, then starts a new worker with the same handle.
2. Sender worker
   -> emits a fresh `register` packet containing the same handle and new sample rate/channel count.
3. `LocalLoopbackSignalSourceService.Ingest(register)`
   -> `SourceState.Reconfigure(...)` updates display/source kind and recreates the analyzer only when format changed.
4. `MainViewModel.RefreshNow()`
   -> reconciles by source key, updates the existing row, reapplies workspace state, and avoids source removal events for the restart.

## Implicit Constraints
- The sender must not emit `close` between same-handle stop/start, or the app will remove the source before the replacement register arrives.
- Reconfiguration recreates analyzer state but preserves per-source settings such as true-peak oversampling and channel waveform/spectrum toggles.
- UI graph retention is handled at row update boundaries: temporary missing or warmup frames do not clear existing waveform/spectrum buffers.
