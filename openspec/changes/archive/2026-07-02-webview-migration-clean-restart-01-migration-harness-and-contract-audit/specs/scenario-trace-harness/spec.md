# Scenario Trace Harness Spec

## Requirements

### Requirement: Deterministic Fake Source Harness
**Trace**: R9

The scenario harness must use deterministic fake sources to compare `main` behavior with migrated WebView contract expectations.

#### Acceptance Criteria
- Fake sources can represent WASAPI, local loopback, and plugin-style sources.
- Fake sources can disappear and reconnect with the same stable source key.
- Fake sources can change display names without changing source identity.
- Fake source snapshots can be generated without live audio devices or plugin binaries.

### Requirement: Source Workflow Traces
**Trace**: R10

The scenario harness must define trace cases for source-list workflows that are product promises.

#### Acceptance Criteria
- Trace cases cover refresh, selection, alias edit, hide, restore, reorder, reset, forget, show hidden, and reconnect.
- Each trace records initial state, action, expected host state, expected persisted state, and expected WebView projection.
- Source-key identity is asserted separately from display title.
- Trace cases identify fatal differences before later WebView source workflow implementation begins.

### Requirement: Detail Settings Traces
**Trace**: R11

The scenario harness must define trace cases for per-source detail settings that later WebView slices will migrate.

#### Acceptance Criteria
- Trace cases cover true peak, waveform mode, spectrum mode, collision visibility, detail module order, and loudness-clock settings.
- Each trace records whether the state is global, per-source persisted, or runtime-only.
- Reset behavior is specified for every migrated detail setting.
- Trace cases identify settings that were local-only in the reference WebView branch.

### Requirement: Rendering Work Gate Traces
**Trace**: R12

The scenario harness must define trace cases for hidden or inactive visualization work.

#### Acceptance Criteria
- Trace cases cover hidden waveform work.
- Trace cases cover hidden spectrum work.
- Trace cases cover hidden loudness-clock work.
- Trace cases record the active gate expected from the WebView rendering architecture.

### Requirement: Native Boundary Trace
**Trace**: R13

The scenario harness must define a trace for native analyzer availability and fallback behavior.

#### Acceptance Criteria
- The trace records the current `main` promise for native DSP availability.
- The trace records the reference branch behavior when native dependencies are mandatory.
- The trace classifies mandatory native dependency behavior as preserved, intentional, or fatal before packaging work begins.
- The trace identifies tests or publish checks needed by the packaging slice.

### Requirement: Build Packaging Baseline Trace
**Trace**: R14

The scenario harness must define a baseline trace for restore, build, test, publish, and packaged asset loading.

#### Acceptance Criteria
- The trace records commands needed to restore, build, and test the current branch on this PC.
- The trace records which WebView assets must exist for development mode.
- The trace records which WebView assets must exist for packaged mode.
- The trace records failure messages expected when required assets are missing.
