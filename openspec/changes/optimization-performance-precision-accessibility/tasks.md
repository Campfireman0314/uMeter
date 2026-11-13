# Delivery tasks

Authoritative scope and acceptance: [proposal.md](proposal.md). All work is local to optimization/performance-precision-accessibility. Parent alone updates this task graph. Existing user untracked directories are excluded.

## T1 — Rate-correct metering and independent trust fixtures

Dependencies: none. Method: test-first vertical slices.

Owned paths: Audio/Analysis under src/DesktopLoudnessMeter.App; tests/DesktopLoudnessMeter.Tests/Audio/Analysis; docs/audio-metering-notes.md. Avoid unrelated DSP changes.

- [x] K-weighting uses the configured input rate; independent reference checks cover 44.1, 48, and 96 kHz at multiple frequencies.
- [x] Existing 48 kHz calibration, startup-window, silence, gating, reset, Overall/Mid/Side, and managed/native behavior remain covered.
- [x] At least one independent true-peak known-signal fixture supplements backend-agreement tests; no certification claim.
- [x] Targeted tests pass after the final related edit; numerical assumptions and limitations are documented.

## T2 — Measured analyzer efficiency without precision loss

Dependencies: T1 (serial ownership of analyzer files). Method: benchmark baseline, then test-first vertical slices.

Owned paths: Audio/Analysis under src/DesktopLoudnessMeter.App; tests/DesktopLoudnessMeter.Tests/Audio/Analysis; tests/DesktopLoudnessMeter.Benchmarks. Do not change shared service/transport interfaces or consumer requirements without escalation.

- [x] Repeated integrated-loudness queries with no new completed gate block reuse the exact result; new blocks and reset invalidate it.
- [x] Repeated stereo-point display scans/allocations are reduced at the existing cadence; new input, mode changes, and reset update correctly. Immutable waveform/spectrum publication adds allocations elsewhere; the final measured tradeoff is explicit in test-report.md.
- [x] Numeric values and silence semantics remain correct; overall spectrum remains available for non-graph consumers.
- [x] Full-analyzer throughput/allocation harness works without extra packages, tolerates native unavailability, and prints reproducible run parameters.
- [x] Matched before/after results and limitations are recorded in the final test report; no timing assertion is used as a unit test.

## T3 — Keyboard, focus, and Windows UI Automation accessibility

Dependencies: T2 (single checkout writer serialization, not a logical code dependency). Method: test-first vertical slices on real STA WPF controls.

Owned paths: Controls, App.xaml, MainWindow.xaml, DetailWindow.xaml, GlobalSettingsWindow.xaml, Localization/LocalizationManager.cs under src/DesktopLoudnessMeter.App; tests/DesktopLoudnessMeter.Tests/Controls and existing localization/infrastructure tests as necessary. New small accessibility helpers under the app are permitted when needed. Preserve current design and binding contracts.

- [x] Both range-slider endpoints can be selected and adjusted by keyboard; arrows, Home/End, Enter/F2 editing, and Escape cancellation work. Tab is left unhandled; real-window traversal remains part of human acceptance.
- [x] Single-value slider keyboard behavior, direct editing, fractional bounds, and bindings remain correct.
- [x] UI Automation exposes descriptive names, per-value range patterns, valid bounds and enabled state; value-change notification-raising paths follow real updates. Client-side delivery remains a human check.
- [x] Custom sliders and themed core controls have visible focus in automated control/pixel checks; focus treatment uses theme resources and a high-contrast fallback. Actual high-contrast/DPI appearance remains open.
- [x] Icon-only commands, diagram settings controls, and compact metric selectors have meaningful names in both inline and popup detail views; localization stays live.
- [x] Targeted keyboard/provider/XAML tests pass after the final related edit.

## T4 — Independent review, final verification, and local delivery

Dependencies: T1, T2, T3.

- [x] Standards review completed; material correctness/maintainability findings are resolved.
- [x] Spec review completed; scoped engineering acceptance is verified and human/real-device gates remain explicit.
- [x] Final managed solution build, full tests (492 passed, 0 failed/skipped), scoped formatting check, and diff whitespace check completed serially, preserving real exit codes.
- [x] Actual changed paths are reviewed against ownership and unrelated user files remain untouched.
- [x] test-report.md contains observed commands, results, matched final benchmarks, review disposition, and honest limitations.
- [x] Normative capability deltas are present under specs/; OpenSpec strict validation passes with zero issues. The task-owned temporary benchmark worktree is removed.
- [ ] Scoped work is committed locally; no push, PR, merge, installer build, or external publication.

## Human and real-device acceptance — remains open until observed

- [ ] End-to-end Narrator operation in real inline/popup workflows.
- [ ] Visual focus, clipping, and readable controls at 100%, 150%, and 200% display scaling, including mixed-DPI windows.
- [ ] Real Windows high-contrast theme appearance and theme/language switching.
- [ ] Live WASAPI and DAW sender smoke checks at the common sample rates, including multiple sources and collision/Signal Shuffle consumers.

## Status

Implementation, final verification, and independent review are complete; local commit is the remaining delivery action. Parent verified 492 full-suite tests and 81 analysis tests from a native-DLL-free output copy. Both reviewers explicitly closed the fractional-bound finding after six parent regression cases were RED and then GREEN. The earlier test-host hang is resolved and is not counted as passing. Final baseline-to-final benchmarks supersede the early worker figures: Normal processing time improves about 4–6%, Eco about 7–9%, and the small-block case about 18%; immutable display publication increases longer Normal-mode allocations, while Eco and small-block allocations decrease. See [test-report.md](test-report.md) for parameters, raw elapsed repeats, tradeoffs, and review disposition. Human/real-device acceptance remains open; no compliance or end-to-end accessibility claim is made.
