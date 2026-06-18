# Plugin Sender Prototypes Spec

## Requirements

### Requirement: Shared Sender Protocol
**Trace**: R1

The localhost protocol used by senders must be consumable without referencing the WPF app assembly.

#### Acceptance Criteria
- Protocol packet models and encode/decode helpers live in a reusable non-UI project.
- Existing app receiver behavior remains compatible with the v1 protocol.
- Existing loopback protocol tests continue to cover register, heartbeat, rename, audio, close, and invalid packet handling.
- Sender-side code can reference the protocol project without pulling in WPF, WASAPI, or app view-model dependencies.

### Requirement: Stable Sender Identity
**Trace**: R2

A plugin-style sender must expose a stable unique handle separately from the user-visible source name.

#### Acceptance Criteria
- A sender can be created with an explicit handle.
- A sender can persist and reload a generated handle from a local identity file for DAW-free diagnostics.
- Renaming sends a rename message without changing the handle.
- Handles are normalized or rejected before packets are sent.

### Requirement: Real-Time-Safe Audio Handoff
**Trace**: R3

Plugin-style audio submission must not perform UDP I/O, logging, blocking waits, or unbounded allocation on the caller's audio path.

#### Acceptance Criteria
- Audio samples submitted by the caller are copied into bounded preallocated buffers.
- The audio submission method returns `false` when the queue is full instead of blocking.
- Network send work happens on a worker loop outside the caller's audio path.
- Dropped packet count is observable for diagnostics.
- Disposal sends a close packet from the worker path and releases sender resources.

### Requirement: DAW-Free Sender Probe
**Trace**: R4

The repo must include a small executable sender probe that can exercise the app receiver without a DAW or plugin SDK.

#### Acceptance Criteria
- The probe registers a plugin-kind source with the app.
- The probe streams generated PCM to the app over localhost.
- The probe can optionally rename the source while streaming.
- The probe sends close on shutdown.
- The probe supports configurable display name, handle, sample rate, channel count, duration, frequency, amplitude, and UDP port.

### Requirement: App Compatibility Smoke Path
**Trace**: R5

The sender prototype must be validated against the app's loopback receiver.

#### Acceptance Criteria
- Tests verify that sender-kit packets create a plugin source in `LocalLoopbackSignalSourceService`.
- Tests verify that streamed audio updates analyzer metrics through the existing app receiver path.
- Tests verify that rename and close from the sender update and remove the same source key.

### Requirement: VST3/AAX Feasibility Notes
**Trace**: R6

The pass must document what is and is not currently available for real plugin packaging.

#### Acceptance Criteria
- The docs state whether a real VST3 build is included in this pass.
- The docs state whether an AAX build is included in this pass.
- The docs list the expected future wrapper boundary: plugin callback -> sender kit audio handoff -> worker loop -> local protocol.
- Missing SDK/toolchain constraints are documented without pretending a distributable plugin exists.
