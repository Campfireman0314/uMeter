# Performance follow-up and spectrum cursor level

## Problem Statement

The previous optimization pass improved CPU cost and accessibility but immutable graph publication increased Normal-mode cumulative allocations. Integrated-loudness queries still scan all retained blocks whenever a new block completes. The spectrograph cursor currently shows frequency only, forcing users to infer a frequency's level from colors that depend on display settings and can combine several source buckets into one rendered row.

## Solution

Continue the existing local spec-to-delivery workflow, prioritizing measured analyzer efficiency without sacrificing numerical fidelity, immutable published views, or display refresh cadence. Add a frequency-plus-decibel cursor label using the retained source FFT bucket at the hovered historical column, not an RGB inversion, rendered-row maximum, or unrelated latest sample.

## User Stories

1. As a Normal-mode user, I want lower analyzer allocation cost without slower or less accurate metering.
2. As a multi-source user, I want sliding measurement windows not to allocate unnecessary doubled buffers at first fill.
3. As a long-session user, I want integrated-loudness evaluation to avoid repeatedly scanning every retained block while keeping exact absolute and relative gating.
4. As a user retaining graph snapshots, I want previously published views to stay unchanged across refreshes, mode changes, and reset.
5. As a user enabling channel graphs, I want performance evidence to include that workload rather than only the default configuration.
6. As a maintainer, I want matched multi-source and long-session measurements with honest retained-memory and allocation tradeoffs.
7. As a spectrum user, I want the cursor label to show both frequency and the corresponding FFT-derived dBFS level.
8. As a history reader, I want the value from the hovered time column, including after circular history wraps.
9. As a user zooming/panning the spectrum, I want the readout to follow the displayed frequency mapping.
10. As a user changing the color floor, ceiling, or theme, I want the same retained measurement to keep the same numeric level.
11. As a user pointing at blank history or a clamped signal, I want unavailable or saturated readings indicated honestly.
12. As an inline, popup, or channel-graph user, I want identical cursor behavior from the shared spectrograph control.

## Implementation Decisions

- Reuse the current integration branch. Original delivered change and its human acceptance gates remain intact; this is a separate follow-up change at the current tip.
- Preserve double-precision numeric measurement and published-view semantics, current sample-rate handling, oversampling, public snapshot contracts, source identity, and existing graph cadence. Do not revert to mutable pooled published buffers, lower resolution/refresh to win a benchmark, or add visibility-demand service plumbing.
- First eliminate avoidable sliding-window capacity growth while preserving chronological floating-point update order. Other allocation changes must be minimal and benchmark-supported.
- Replace full historical gate scans with an exact-value indexed aggregate or equivalently justified exact method. Do not quantize levels, truncate session history, or substitute a rolling approximation. Reordered floating-point aggregation may differ within a documented 1e-10 LU numerical budget against the existing two-pass reference; threshold membership remains strict, including equality and duplicate powers. Retained-memory growth and data-structure cost must be reported rather than called bounded memory.
- Keep gate-result caching between completed blocks. Preserve reset, unavailable, absolute/relative gates, duplicate powers, Overall/Mid/Side and analyzer streaming behavior.
- Extend the existing dependency-free benchmark harness for optional channel waveform/spectrum and multiple analyzer instances. Capture throughput, cumulative allocations, configuration/backend, repeated measurements, and meaningful final numeric signatures. Add an isolated gate-scaling scenario if required to measure long histories without hours of PCM processing.
- The cursor reports the retained nearest source Mel bucket corresponding to the displayed frequency at the hovered horizontal history location. It is an FFT/bucket estimate, not a new true-peak/RMS or calibrated arbitrary-frequency measurement. Existing display frequency-domain semantics are unchanged.
- Read source normalized levels before the color transform and before max-preserving row downsampling. Prefer reusing/refactoring the existing retained-level storage rather than keeping redundant histories. Retained history is bounded by the existing maximum columns and source-bucket count; report any memory tradeoff. No FFT or audio processing on pointer movement.
- Correctly invert partially filled right-aligned history and full circular/wrapped drawing, including the device-pixel-snapped seam. Y mapping uses the effective viewport and the displayed source Mel scale.
- Display frequency plus one-decimal dBFS. A stored zero is at/below the analyzer's -200 dBFS floor, not proof of exact silence; show a floor indication. A stored one is at/above the 0 dBFS ceiling; show a ceiling indication. Empty/non-finite/out-of-plot values have no fabricated numeric level. Changing theme/color range must not change retained numeric readouts.
- Preserve current lightweight drawing, hidden-control gates, pan/zoom, rollover, and common inline/popup/channel behavior. Keep label placement within the control/plot as practicable, including narrow views. No separate tooltip window or new UI dependency.
- Parent owns spec/task/evidence maintenance and final Git actions. One implementation writer at a time in the dirty checkout; independent read-only reviews may run concurrently.

## Testing Decisions

- Existing analyzer snapshots and immutable display views are the primary metering seams. Existing internal gate/window tests can isolate exact threshold/history behavior, including worked examples, adversarial duplicate/equality cases, randomized deterministic power sequences against a separate straightforward two-pass oracle, order patterns, and reset.
- Add allocation behavior checks only when deterministic and robust (for example, no allocations while appending to a preallocated full scalar window). Do not use wall-clock test thresholds.
- Cursor tests use a pure mapping/history seam and at least one actual STA WPF control scenario exercising the same readout path used by rendering. Cover empty/partial/full/wrapped histories, seam boundaries and DPI scales, oldest/newest columns, pan/zoom, source buckets differing inside one rendered row, source-count/size changes, reset, floor/ceiling, invalid data, and color-range invariance. Prefer existing STA helper and mapper test conventions; no test package installation.
- Run targeted tests after vertical slices. Final verification is a serial managed solution build, full test suite, scoped format check, whitespace check, strict OpenSpec validation, and independent Standards/Spec review of this follow-up only.
- Measure against baseline 35f713f using the same final harness, native DLL, rate, duration, block size, modes, channel flags, source count, warmup and repeats. Include Normal/Eco, longer sessions, channel-enabled and multi-source cases. Re-measure affected scenarios after any related fixes. Preserve raw evidence and real exits.
- Reuse previous manual acceptance limitations. Real Narrator/DPI/high-contrast/DAW acceptance and subjective visual confirmation remain open until observed; automated cursor/provider tests do not close them.

## Out of Scope

Transport/plugin/installer rewrites; WebView migration; new dependencies; global configuration; release/version changes; remote publication, push, PR, merge or deployment; full compliance certification; changing frequency-band aggregation or the public analysis snapshot schema; unrelated game or accessibility redesign. Existing untracked .claude, .codex, output and tmp contents remain untouched.

## Further Notes

- Authorization: the user selected performance first and explicitly requested the cursor decibel display, continuing the same spec-to-local-delivery workflow with delegated implementation/review as useful.
- Baseline: optimization/performance-precision-accessibility at 35f713f1c4139be3f9b449a932da16f4192f6c17. No tracked dirty paths. Prior applicable test evidence: 492 passing tests; baseline will be rechecked before implementation.
- Test seams and reversible technical slicing are selected within the user's continuing implementation authorization; no new approval gate is needed unless a goal, fidelity, scope, or permission changes.
