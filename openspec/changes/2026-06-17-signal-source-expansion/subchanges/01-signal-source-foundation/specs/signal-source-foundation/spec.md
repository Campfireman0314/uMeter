# Signal Source Foundation Spec

## Requirements

### Requirement: Generic Source Snapshot Contract
**Trace**: R1

The app must expose metered inputs through a source-oriented snapshot contract instead of requiring callers to speak in WASAPI audio-session terminology.

#### Acceptance Criteria
- The contract includes a stable source key.
- The contract includes a source kind.
- The contract includes a user-facing display name.
- The contract includes a lifecycle/status string.
- The contract carries all existing level, loudness, true-peak, RMS, stereo image, waveform, and spectrum metric fields.

### Requirement: WASAPI Adapter Compatibility
**Trace**: R2

The existing WASAPI render-session provider must adapt to the generic source contract without changing current visible behavior.

#### Acceptance Criteria
- WASAPI rows still collapse duplicate process-backed sessions to one row per process.
- The meter app still filters its own process from visible rows.
- Silent active loopback rows without audible analyzer metrics remain hidden.
- Process-loopback metrics still merge into the visible process-backed source.
- WASAPI source keys remain stable enough to keep existing detail-window tracking behavior.

### Requirement: Source Settings Routing
**Trace**: R3

Reset and analysis settings must route through source-oriented interfaces while preserving current process-loopback behavior for WASAPI sources.

#### Acceptance Criteria
- Per-source reset still reaches the process-loopback metric provider for process-backed WASAPI sources.
- Reset-all still clears all analyzer workers and visible row metrics.
- Per-source true-peak oversampling still reaches the process-loopback metric provider.
- Per-source waveform and spectrum channel-mode requests still reach the process-loopback metric provider.
- Requests for the meter app's own process remain ignored.

### Requirement: Source View Model Identity
**Trace**: R4

The main and detail view models must track sources by source identity rather than audio-session identity.

#### Acceptance Criteria
- Existing rows update in place when a source with the same key appears.
- A selected detail source survives a short WASAPI handle disappearance while its process remains alive.
- A selected detail source can reattach by display name when a recovered WASAPI row receives a different transient session key.
- Missing process-backed sources are retained until the process exits, then removed.

### Requirement: Source Type UI Markers
**Trace**: R5

Visible rows and detail headers must identify the type of source being displayed.

#### Acceptance Criteria
- WASAPI-backed rows show a compact source-type label.
- Inline detail view shows the selected source type.
- Pop-up detail view shows the selected source type.
- Tray pop-up source selection includes enough source identity text to distinguish future source kinds.

### Requirement: Backward Compatibility Surface
**Trace**: R6

The first foundation slice must avoid introducing local network, VST3, or AAX runtime dependencies.

#### Acceptance Criteria
- The app still starts with the WASAPI provider by default.
- No socket listener is opened.
- No plugin binary project is required.
- Existing tests remain meaningful as characterization tests for WASAPI behavior.
