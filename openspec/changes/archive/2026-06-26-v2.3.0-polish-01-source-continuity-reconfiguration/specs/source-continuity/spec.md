# Source Continuity Spec

## Requirement: same-handle sender reconfiguration keeps the source continuous
**Trace**: R1

When a plugin or loopback sender re-registers the same handle with a different sample rate or channel count, the receiver must preserve the stable source key, display name/alias ownership, source preferences, and row identity while rebuilding analyzer state only as needed for the new format.

### Scenarios
- Given a plugin source is visible, when it registers the same handle at a new sample rate, then the same source key remains visible and the row is not treated as a source close.
- Given source-specific analysis settings were applied before reconfiguration, when the same handle re-registers, then the rebuilt analyzer receives those settings.

## Requirement: sender sample-rate changes do not emit close for the same instance
**Trace**: R2

When the JUCE sender is restarted by host `prepareToPlay` for the same stable handle, it must send a fresh register for the new format without first emitting a close packet for that handle.

### Scenarios
- Given an active sender, when `start` is called again with the same handle and a different sample rate, then receiver-observable packets include the new register and no close before that register.
- Given an active sender is explicitly stopped or destroyed, then it still emits a close packet.

## Requirement: temporary source hibernation preserves rolling graph continuity
**Trace**: R3

When a source temporarily loses a WASAPI/session handle, process-loopback worker, or analyzer snapshot while the source/app is still considered alive, the UI must mark the source as hibernating or handle-missing without clearing the last waveform/spectrum buffers or appending empty spectrograph columns.

### Scenarios
- Given a row has spectrum and waveform history, when it enters temporary handle-missing state, then its rolling graph buffers and plot ticks remain stable.
- Given the next live analyzer snapshot has not produced spectrum buckets yet, then the row keeps the previous spectrum buckets until a fresh non-empty spectrum frame arrives.
- Given the user explicitly resets the source, then rolling graph buffers are cleared.

## Requirement: WASAPI device reconfiguration does not permanently orphan live rows
**Trace**: R4

When default render endpoint enumeration temporarily returns no sessions or throws a recoverable device error during output-device changes, live process-backed rows must be retained according to the existing process-alive and missing-row retention rules.

### Scenarios
- Given a process-backed WASAPI row is alive, when a refresh returns no snapshots, then the row remains visible as handle-missing while the process is alive.
- Given the process later returns with a compatible display identity or source key, then the existing detail view reattaches to the live row.

## Requirement: true close/exit still ends the source
**Trace**: R5

Continuity rules must not hide real lifecycle endings: explicit plugin close, expired loopback heartbeat beyond timeout, and dead WASAPI process must still remove the source from the global list or mark an open detail as instance not found.

### Scenarios
- Given a plugin sender sends a close packet, then the global source row disappears and an open detail view shows instance-not-found.
- Given a process-backed WASAPI row is missing and its process has exited, then the global row is removed after the configured retention rule.
