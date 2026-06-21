# uMeter Dream Map

This document captures the post-`v2.2.4` product directions in the order I would pursue them if I were stewarding the project.

The ranking favors changes that increase daily usefulness, trust, and product identity before broader platform expansion.

## Priority Order

1. Persistent per-source workspace memory
2. Mix problem detection
3. Timeline, events, and snapshots
4. Sender ecosystem maturity
5. Metering calibration and trust
6. Rendering architecture cleanup
7. Design system polish
8. Platform expansion

## 1. Persistent Per-Source Workspace Memory

### Why This Matters

uMeter already gives the user many good controls, but several of them still behave like temporary session choices. Remembering source-specific intent would make the app feel personal and stable.

This is the most important next step because it improves nearly every existing feature without requiring a new visual concept.

### Candidate Features

- Persist source aliases.
- Persist hidden, pinned, or preferred source ordering.
- Persist waveform, spectrum, stereo image, and collision analyzer visibility.
- Persist collision analyzer compare source where the target can be resolved safely.
- Persist true-peak oversampling and stereo scale settings per stable source.
- Add optional source color tags.

### First Usable Slice

Persist per-source aliases, visibility, and detail-panel preferences for stable source keys. Keep the storage local, human-readable, and compatible with the existing theme settings style.

### Main Risks

- Source identities can change across app, DAW, plugin, and OS sessions.
- Persisting stale plugin instances may confuse users if reconnect behavior is not clear.
- Too many remembered preferences can make reset behavior harder to reason about.

### Discussion Questions

- Should hidden sources stay hidden across restarts, or only within a session?
- Should plugin sender names override remembered aliases, or should user aliases always win?
- Do we need a source-management panel before adding more persistence?

## 2. Mix Problem Detection

### Why This Matters

This is where uMeter can become more than a meter. It can start answering practical audio questions: what collided, what clipped, what became too wide, what suddenly got loud, and which source caused it.

The goal is not to fake mastering intelligence. The goal is honest, signal-derived clues.

### Candidate Features

- Frequency collision history.
- Mono compatibility warnings.
- Stereo imbalance detector.
- Low crest-factor or over-compression hints.
- True-peak risk events.
- Loudness jump attribution by source.
- User-tunable detection sensitivity.

### First Usable Slice

Add collision event tracking over a short rolling time window. Show recent collision regions and source pair names in the detail view without interrupting the current meter workflow.

### Main Risks

- False positives can make the app feel noisy or judgmental.
- Detection thresholds need domain-friendly defaults.
- It must remain clear that these are hints, not absolute mix decisions.

### Discussion Questions

- Should warnings appear globally, per source, or only inside detail views?
- Should the app use neutral language like "possible overlap" instead of "problem"?
- Which detector would be most useful first: collision, mono compatibility, or true-peak risk?

## 3. Timeline, Events, and Snapshots

### Why This Matters

uMeter is strong at showing what is happening now. A short history would answer the common studio question: "What just happened?"

Timeline features would also support debugging, plugin sender validation, and user reports.

### Candidate Features

- Rolling loudness and true-peak event history.
- Source activity timeline.
- Collision event timeline.
- Max true-peak markers.
- Exportable diagnostic snapshot.
- Optional session summary after long runs.

### First Usable Slice

Add a compact recent-events list: true-peak max, loudness jump, source appeared/disappeared, and collision detected. Keep it lightweight and bounded in memory.

### Main Risks

- Timeline storage can grow if not carefully bounded.
- Too much history UI could make the app feel heavier than its overlay identity.
- Event attribution must be conservative to avoid misleading users.

### Discussion Questions

- Should history be visual, textual, or both?
- Should snapshots include raw spectrum buckets, or only derived summaries?
- Should event history persist after app restart?

## 4. Sender Ecosystem Maturity

### Why This Matters

The VST3 sender gives uMeter a unique identity: OS app audio and DAW/plugin audio in one desktop meter. Maturing this bridge could become the product's strongest differentiator.

### Candidate Features

- Plugin editor connection status.
- Receiver found/not found state.
- Sender-side peak/LUFS preview.
- Better sender identity and rename synchronization.
- Binary loopback protocol option after JSON stabilizes.
- AU sender planning.
- AAX sender follow-through after SDK/signing constraints are clear.

### First Usable Slice

Add connection/status feedback to the VST3 sender editor and expose receiver state from the sender loopback path.

### Main Risks

- DAW/plugin compatibility testing grows quickly.
- Binary protocol work can break inspectability if done too early.
- AAX distribution requires external signing and validation constraints.

### Discussion Questions

- Should VST3 remain the primary sender until the UX is mature?
- How much information should the plugin editor show before it becomes distracting?
- Is binary protocol worth prioritizing before more user-visible sender features?

## 5. Metering Calibration and Trust

### Why This Matters

For an audio meter, trust is product oxygen. Strong validation would let us speak about uMeter more confidently and catch subtle regressions before they reach users.

### Candidate Features

- Loudness reference-vector tests.
- True-peak reference-vector tests.
- Known sine, noise, silence, sweep, and clipped fixtures.
- Sender packet-loss and reconnect tests.
- Documented accuracy limits.
- Optional calibration diagnostics page.

### First Usable Slice

Create a small deterministic fixture suite for sine, silence, pink noise approximation, and clipped samples. Use it to protect loudness, peak, true-peak, waveform, and spectrum assumptions.

### Main Risks

- Official reference material may have licensing or access constraints.
- Some test vectors require careful interpretation.
- Overpromising certification would be dangerous unless the validation is truly complete.

### Discussion Questions

- Do we want "engineering confidence" tests first, or public-facing accuracy documentation first?
- Which measurements should be treated as product-critical?
- Should test fixtures live in source control or be generated at test time?

## 6. Rendering Architecture Cleanup

### Why This Matters

Waveform, spectrum, collision analyzer, and stereo image now carry real product value. Rendering should become a shared subsystem instead of a set of control-specific solutions.

This would reduce future bugs and make new visual tools easier to build.

### Candidate Features

- Shared rolling bitmap/history engine.
- Shared plot color roles.
- Shared frame scheduling policy.
- Render-only smoothing utilities.
- Debug overlay for render FPS and dropped frames.
- Clear separation between analysis data and display transforms.

### First Usable Slice

Extract shared plot palette/color roles and a small render scheduling helper, then migrate spectrum and waveform first.

### Main Risks

- Refactoring rendering can easily disturb visuals that now feel acceptable.
- Shared abstractions may become too generic if designed before the next concrete feature.
- Performance wins need measurement, not vibes.

### Discussion Questions

- Should rendering cleanup happen before or after the next user-visible feature?
- Which control should become the reference implementation?
- Do we need visual regression screenshots before major plot refactors?

## 7. Design System Polish

### Why This Matters

Themes gave uMeter personality. A stronger design system would make future UI additions feel coherent instead of patched on.

The design target should be a calm studio instrument: compact, readable, and trustworthy.

### Candidate Features

- Formal plot color roles per theme.
- Source-type badges for WASAPI, plugin sender, probe, and future sender types.
- Better empty and disconnected states.
- More consistent icon buttons and tooltip timing.
- Compact/dense layout options.
- Better detail-window rhythm.

### First Usable Slice

Define theme roles for signal, secondary signal, warning, collision, inactive plot, cursor marker, and text-muted states. Apply them across plot controls.

### Main Risks

- Visual polish can consume time without changing core usefulness.
- More theme roles increase maintenance cost.
- Dense UI can become intimidating if the hierarchy is weak.

### Discussion Questions

- Should uMeter prioritize compact pro-tool density or softer consumer readability?
- Should themes remain small in number, or should custom themes be supported later?
- Which screen should define the visual standard: global list or detail view?

## 8. Platform Expansion

### Why This Matters

iOS, macOS, AU, and AAX planning are attractive, but platform expansion should follow a solid core architecture. The current Windows/VST3 product should become dependable before the project spreads too wide.

### Candidate Features

- Shared design conventions for future clients.
- Shared protocol documentation.
- AU sender feasibility notes.
- iOS app architecture exploration.
- AAX sender build/signing checklist.
- Cross-platform sender identity model.

### First Usable Slice

Write a platform strategy note that separates reusable concepts from Windows/WPF-specific implementation: source identity, packet protocol, metering semantics, theme roles, and graph behavior.

### Main Risks

- iOS audio routing constraints may not match the desktop product.
- AAX distribution may be blocked by signing or SDK access.
- Premature cross-platform abstraction can slow the Windows app down.

### Discussion Questions

- Is iOS meant to be a companion display, a meter, or a sender/receiver endpoint?
- Which platform has the strongest user value after Windows: macOS, iOS, or AAX?
- What must be stabilized before we call any behavior cross-platform?

## Suggested Next Discussion

Start with Priority 1 and decide the shape of per-source workspace memory.

That discussion should answer:

- What counts as a stable source identity?
- Which settings deserve persistence first?
- Where should reset/forget controls live?
- How much source management UI is needed for the first release?

The current bundled delivery plan lives in `dev-cycles.md`.
