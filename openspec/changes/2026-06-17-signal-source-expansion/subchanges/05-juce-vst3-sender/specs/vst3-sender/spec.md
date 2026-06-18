# VST3 Sender Spec

## Requirements

### Requirement: JUCE VST3 Plugin Build
**Trace**: R1

The repo must include a native JUCE plugin project that can produce a VST3 audio-effect sender.

#### Acceptance Criteria
- The project lives under `src/DesktopLoudnessMeter.JucePlugin`.
- CMake defaults to the local JUCE path `C:\JUCE` but allows overriding `JUCE_DIR`.
- The plugin target builds the VST3 format.
- The plugin is an audio effect, not a synth or MIDI effect.
- The plugin has no required custom editor for the first pass.

### Requirement: Pass-Through Audio Processor
**Trace**: R2

The plugin must not alter the host's audio stream while monitoring it.

#### Acceptance Criteria
- Supported bus layouts include mono and stereo with matching input/output layouts.
- Extra output channels are cleared when a host exposes more outputs than inputs.
- Input samples remain unchanged for supported input/output layouts.
- The processor reports zero tail length.

### Requirement: Stable Plugin Source Identity
**Trace**: R3

Each plugin instance must use a stable unique handle separate from the user-visible source name.

#### Acceptance Criteria
- A new instance without saved state generates a valid loopback handle.
- Saved plugin state restores the handle.
- The visible source name defaults to `Desktop Loudness Meter Sender`.
- The handle remains unchanged when the display name changes through saved state.

### Requirement: Real-Time-Safe Native Handoff
**Trace**: R4

The audio callback must not perform UDP I/O, blocking waits, logging, dynamic allocation, or JSON/base64 encoding.

#### Acceptance Criteria
- Audio blocks are copied into a bounded preallocated queue.
- Enqueue returns failure and increments a drop counter when the queue is full.
- The queue rejects blocks that exceed the configured packet capacity.
- Network send and protocol encoding happen on a worker thread outside `processBlock`.

### Requirement: Loopback Sender Lifecycle
**Trace**: R5

The plugin sender must follow the existing v1 localhost protocol consumed by the desktop app.

#### Acceptance Criteria
- The sender emits a `register` packet with `sourceKind` set to `plugin`, the stable handle, display name, sample rate, and channel count.
- The sender emits periodic heartbeat packets while prepared.
- The sender emits sequenced audio packets using little-endian float32 values encoded as base64.
- The sender emits `close` when resources are released or the processor is destroyed.
- The sender uses UDP over `127.0.0.1:38173` by default.

### Requirement: VST3 Smoke Documentation
**Trace**: R6

The VST3 slice must be usable by a tester without reading the source.

#### Acceptance Criteria
- Docs include configure and build commands.
- Docs identify the expected VST3 artifact location or how to find it.
- Docs explain how to copy/install the plugin for a host scan.
- Docs include a manual smoke path against the desktop app.
- Docs state that AAX remains a later SDK-gated slice.
