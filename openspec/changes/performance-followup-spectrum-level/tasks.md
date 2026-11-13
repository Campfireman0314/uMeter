# Performance follow-up delivery tasks

Specification: [proposal.md](proposal.md); normative delta: [specs/performance-and-spectrum-cursor/spec.md](specs/performance-and-spectrum-cursor/spec.md).
Baseline: `35f713f1c4139be3f9b449a932da16f4192f6c17`, existing integration branch `optimization/performance-precision-accessibility`. Parent alone maintains this carrier. Prior delivery/manual acceptance remains unchanged.

## P1 — Analyzer allocation and exact long-session gating

Dependencies: none. One writer; test-first vertical slices.

Owned paths: `src/DesktopLoudnessMeter.App/Audio/Analysis/`, `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/`, `tests/DesktopLoudnessMeter.Benchmarks/`, `docs/audio-metering-notes.md`.

- [x] Rechecked the 492-test baseline and extended matched benchmark configuration for channel waveform/spectrum and source count before optimizing.
- [x] Eliminated avoidable sliding-window buffer growth while preserving existing arithmetic ordering, public metric behavior and snapshot lifetime.
- [x] Replaced per-completed-block full historical scans with exact-value indexed aggregation, preserving strict gates and matching an independent two-pass oracle within 1e-10 LU across adversarial and varied histories. Explicit relative equality and duplicate-key boundary fixtures are included.
- [x] Reset, cache, duplicate powers, silence, block boundaries and existing rate/Mid/Side/true-peak behavior remain tested.
- [x] Captured matched current-baseline versus final measurements, isolated long-history gate results, channel-enabled/multi-source workloads and retained-memory tradeoffs; see test-report.md. The index is not bounded session memory.

## P2 — Spectrum frequency plus historical decibel readout

Dependencies: P1 for checkout writer serialization; no necessary code dependency.

Owned paths: `src/DesktopLoudnessMeter.App/Controls/Spectrum*`, `tests/DesktopLoudnessMeter.Tests/Controls/Spectrum*`, `docs/rendering-architecture.md`. Existing `StaControlHost` may be reused, not expanded or replaced without a concrete need.

- [x] Replaced row-level retention with bounded raw source-bucket history, without a second full history; source numeric data is independent of color-range changes.
- [x] Cursor label shows frequency plus one-decimal FFT-derived dBFS from the correct history column and nearest source bucket.
- [x] Partial/full/wrapped history, seam snapping/DPI, effective pan/zoom mapping, bucket aggregation, empty history, reset and source-size changes are tested. Human mouse interaction/visual checks remain open.
- [x] Floor, ceiling, non-finite and out-of-plot states are indicated honestly; no value is inferred from colors.
- [x] Real STA spectrum-control tests exercise the rendering readout path; existing frequency-hover tests remain green. Shared inline/popup/channel integration is verified from actual XAML control usage.
- [x] Pointer movement does no FFT/audio processing; existing render gates/pan/zoom/rollover remain. Typical raw-history retention increases (approximately +286 KiB per representative control); the bound and tradeoff are documented.

## P3 — Review, verification and local delivery

Dependencies: P1 and P2.

- [x] Independent Standards review completed and material findings resolved, including corrected distinct-node memory accounting.
- [x] Independent Spec review completed and notes resolved, including empty-series benchmark reporting and exact relative-threshold equality coverage.
- [x] Final build, full tests (557 passed, 0 failed/skipped), scoped formatting, whitespace, and strict OpenSpec checks completed serially after the final relevant edits. Native-DLL-free analysis copy: 98 passed.
- [x] Parent verified actual diff, evidence, changed paths, final matched benchmarks, and exclusion of user-owned untracked files.
- [ ] Verification report and carriers reconciled/read back; scoped work committed locally with no remote publication.

## Open acceptance

- [ ] Human visual confirmation of the expanded cursor label at narrow sizes, pan/zoom, and mixed-DPI displays.
- [ ] Prior live Narrator/high-contrast/WASAPI/DAW acceptance gates (in the preceding change) remain open; this work does not close them.

## Current status

Implementation, independent review/remediation and parent verification are complete; local commit recording is the remaining delivery action. Parent full suite: 557 passed; managed-fallback analysis copy: 98 passed; build/format/strict OpenSpec/whitespace checks green. Fresh per-scenario paired benchmarks against 35f713f demonstrate lower cumulative analyzer allocations and the measured time deltas in [test-report.md](test-report.md); at 30k blocks the isolated gate median was 566.9 -> 22.0 ms. Reported low-bit integrated differences remain within 1e-10 LU. Retained AVL storage grows with distinct powers, and raw spectrum history adds bounded typical-control memory: neither is concealed as an overall retained-memory win. Readout is an FFT bucket estimate and preserves the existing displayed frequency-domain approximation. The initial unavailable-model failure, successful configured-model recovery, corrected memory projection, observed 0.002-second crash/fix, and timing limitations are preserved in the report. Human visual and prior live acceptance remain open.
