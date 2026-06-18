# Source Management Polish Spec

## Requirements

### Requirement: Mixed Source Sorting
**Trace**: R1

The main source list must use deterministic grouping and sorting for WASAPI, loopback, and plugin sources.

#### Acceptance Criteria
- Plugin sources sort before local-loopback sources.
- Local-loopback sources sort before WASAPI sources.
- Sources with the same kind sort by display name, then by source key.
- Refreshing with the same logical sources in a different provider order does not reorder visible rows unpredictably.

### Requirement: Duplicate Display Name Disambiguation
**Trace**: R2

Rows with duplicate display names must remain understandable without changing provider-owned names or unique handles.

#### Acceptance Criteria
- `DisplayName` remains the provider-owned display name.
- A separate row title includes a compact source qualifier when multiple current rows share the same display name.
- Rows with unique display names show the clean display name without extra qualifier.
- Duplicate-name detail headers and tray labels include enough source identity to distinguish the source.

### Requirement: Source Status Text
**Trace**: R3

Rows and detail headers must explain source kind, source identity, and current status in source-facing language.

#### Acceptance Criteria
- WASAPI rows identify their process when a process id is available.
- Plugin and loopback rows identify their stable handle in a compact form.
- Missing/reconnecting rows use source-facing status text rather than audio-session-only wording.
- The global status text says `source` or `sources`, not `session`.

### Requirement: Reconnect Preference Memory
**Trace**: R4

Per-source analysis preferences must survive common handle loss and reconnect paths while the app is running.

#### Acceptance Criteria
- Per-source true-peak oversampling is remembered by source key after a source disappears and later returns with the same key.
- Per-source waveform channel mode is remembered by source key after reconnect.
- Per-source spectrum channel mode is remembered by source key after reconnect.
- Resetting a source does not erase its remembered analysis preferences.
- Global true-peak oversampling remains the default for newly seen sources without source-specific preferences.

### Requirement: Multi-Source Documentation
**Trace**: R5

Docs must explain mixed source types, naming, reconnect behavior, and sender routing.

#### Acceptance Criteria
- README mentions mixed WASAPI/loopback/plugin source rows.
- Troubleshooting or source docs explain duplicate names and stable handles.
- Docs state that alias and visibility persistence are deferred future UI work.
