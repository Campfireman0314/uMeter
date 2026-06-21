# uMeter Development Cycles

This document groups the long-term dream map into practical implementation cycles.

The rule for bundling is simple: merge work when it shares a foundation, and split work when it touches different runtimes, validation surfaces, or user mental models.

## Cycle 1: Workspace Memory Foundation

### Bundled Areas

- Persistent per-source workspace memory.
- Small source-management controls.
- Small design-system polish where needed to expose the controls cleanly.

### Target Outcome

uMeter remembers how the user works with each stable source.

### First Delivery

- Source aliases.
- Remembered detail visibility for waveform, spectrum, and collision analyzer.
- Remembered analysis modes for true peak, waveform, and spectrum.
- Remembered stereo image display settings.
- Remembered spectrograph display range and collision thresholds.
- A clear "forget this source" path.

### Explicitly Out Of Scope

- Hidden source lists.
- Pinned/manual source ordering.
- Source color tags.
- A full source-management window.

Those are valuable, but they need a stronger management surface.

## Cycle 2: Event Substrate

### Status

Completed in `v2.2.4`.

The first landed slice adds a bounded Recent Events strip in the global view and records source appeared/disappeared events, true-peak max risk crossings, and clear momentary loudness jumps from the main refresh loop. The strip now has a fixed-height result area and can filter events by all, source, peak, or loudness type. Global settings expose the maximum retained event-entry count and the future event output folder.

### Bundled Areas

- Timeline/events/snapshots.
- Metering trust groundwork.

### Target Outcome

uMeter gains a bounded language for "what just happened" without yet pretending to diagnose every mix problem.

### First Delivery

- Recent source appeared/disappeared events. [landed]
- True-peak max event. [landed]
- Loudness jump event. [landed]
- Basic diagnostic snapshot.
- Deterministic silence, sine, clipped, and noise-like fixture tests.

### Deferred

- Add a lightweight diagnostic snapshot command for support/debugging.
- Add deterministic fixture tests around silence, sine, clipped, and noise-like inputs.
- Decide whether collision events belong in the global Recent strip or only in source detail views.

## Cycle 3: Mix Problem Detection MVP

### Status

First slice completed in `v2.2.5`.

The first landed slice turns the existing collision analyzer output into bounded, neutral Recent Events entries. It records a `Possible overlap` event when the enabled detail collision analyzer finds a stable, prominent overlap region, and suppresses repeats while the same region remains active.

### Bundled Areas

- Collision history.
- True-peak risk hints.
- Loudness jump attribution.
- Conservative mono/stereo warnings if the signal is clear enough.

### Target Outcome

uMeter starts surfacing useful, signal-derived mix clues while keeping the language neutral.

### First Delivery

- "Possible frequency overlap" history. [landed]
- Recent collision regions. [landed]
- Source-pair collision events.
- User-tunable sensitivity.

## Cycle 4: Sender Ecosystem Maturity

### Status

First slice completed in `v2.2.5`.

The first landed slice adds receiver acknowledgements to the local loopback protocol and surfaces VST3 sender connection state in the plugin editor. The sender now distinguishes waiting for uMeter, connected, and no-recent-reply states without changing the audio transport shape.

### Bundled Areas

- VST3 sender connection status.
- Receiver found/not found state.
- Sender rename and identity polish.

### Target Outcome

The sender plugin feels like a dependable bridge, not just a silent data emitter.

### First Delivery

- Sender editor connection feedback.
- Receiver discovery state.
- Better source-name sync behavior.
- Reconnect diagnostics.

### Explicitly Out Of Scope

- Binary protocol migration.
- AU implementation.
- AAX distribution.

## Cycle 5: Rendering Architecture Cleanup

### Status

Started after `v2.2.5`.

First cleanup slices completed in `v2.2.6`.

This pass extracted `DeferredRenderUpdate`, migrated waveform and spectrum controls to the shared one-queued-render-callback policy, moved the main graph controls onto named `PlotColorRole` lookups, and extracted `PlotBackgroundCache` for static plot backgrounds. `docs/rendering-architecture.md` now records the current control responsibilities and next cleanup candidates.

### Bundled Areas

- Shared plot color roles.
- Shared render scheduling helpers.
- Debug frame-rate/dropped-frame visibility.
- Waveform, spectrum, and collision rendering cleanup.

### Target Outcome

Graph rendering becomes easier to reason about, test, and extend.

## Cycle 6: Platform Strategy

### Status

First planning slice completed in `v2.2.6`.

`docs/product-roadmap/platform-strategy.md` now separates reusable contracts from Windows/WPF/VST3-specific boundaries and records expansion order for AAX, macOS/AU, and iOS.

### Bundled Areas

- iOS concept.
- macOS/AU concept.
- AAX checklist.
- Shared protocol and design convention notes.

### Target Outcome

The project knows what is reusable before it tries to become cross-platform.

## Recommended Sequence

1. Workspace Memory Foundation.
2. Event Substrate.
3. Mix Problem Detection MVP.
4. Sender Ecosystem Maturity.
5. Rendering Architecture Cleanup.
6. Platform Strategy.

Cycle 1 starts first because it makes the existing app feel more mature without destabilizing the DSP or plugin sender paths.
