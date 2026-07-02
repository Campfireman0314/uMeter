# WebView Migration Parity Decision Rules

## Purpose

These rules define how the clean WebView migration judges differences from `main`. `main` is the behavior oracle. The old WebView refactor branch is a salvage reference, not a compatibility contract.

## Severity Taxonomy

| Classification | Meaning | Required Action |
|----------------|---------|-----------------|
| `fatal` | Breaks a preserved product promise or makes the app impossible to restore, build, run, publish, or trust. | Stop the slice and fix before proceeding. |
| `blocking-fixable` | Matches the intended WebView direction but lacks a clean contract, owner, state path, or test. | Fix inside the current or next dependent slice before release. |
| `acceptable-intentional` | Changes implementation or UX while preserving the user outcome. | Record the rationale, owner, and verification method. |
| `discardable-legacy` | Preserves only a WPF-specific artifact that is not a product promise. | Delete or avoid reimplementing it. |

## Product Promise Categories

Differences are `fatal` when they break any preserved promise in these categories:

| Category | Preserved Promise |
|----------|-------------------|
| Startup and shutdown | Single-instance behavior, activation replay, tray lifecycle, hide/show, and clean disposal continue to work. |
| Restore/build/publish/package | A developer can restore, build, test, publish, and package the app with documented prerequisites. |
| Source identity | Stable source keys survive display-name changes, reconnects, and same-handle restarts. |
| Workspace persistence | Alias, hidden state, explicit order, detail selection, and per-source settings persist according to `main` behavior. |
| Metering correctness | Loudness, true peak, spectrum, waveform, stereo, clock, and collision calculations preserve validated semantics. |
| Command/UI alignment | Every WebView command has a real host handler, testable host effect, and visible failure for unknown commands. |
| Detail settings | Per-source detail controls, module visibility, order, ranges, reset behavior, and clock settings keep one owner. |
| Performance gates | Hidden or inactive heavy visualizations stop expensive render work at the callback or scheduling boundary. |

## Compatibility Bridge Policy

The migration should not keep WPF and WebView as permanent peer product surfaces. A bridge is allowed only as a staged implementation aid when all of these are true:

- It has an owner.
- It has a retirement condition.
- It names the later slice that must remove, absorb, or replace it.
- It does not hide a fatal parity break from tests.

Unowned fallback behavior is classified as `blocking-fixable` when WebView still needs the behavior, or `discardable-legacy` when it only preserves a WPF artifact.

## Decision Record Template

Each difference should be recorded with this shape:

| Field | Required Content |
|-------|------------------|
| Area | Startup, bridge, source workflow, rendering, packaging, or other affected area. |
| Main behavior | The preserved behavior observed on `main`. |
| Reference behavior | The behavior observed in the old WebView branch. |
| Classification | `fatal`, `blocking-fixable`, `acceptable-intentional`, or `discardable-legacy`. |
| Rationale | Why the classification is correct. |
| Owner | The slice or module that owns the follow-up. |
| Verification | Test, scenario trace, manual check, or packaging check that proves the decision. |

## Known Fatal Examples

- A frontend command exists only in the mock bridge and has no real C# host handler.
- Source alias, hidden state, or ordering changes are stored only in React local state.
- Native DSP becomes mandatory while docs and tests still promise managed fallback.
- A packaged build cannot load WebView assets or native dependencies with a clear error path.

## Known Acceptable Change Examples

- A WebView control layout differs from WPF while preserving the same source command and persisted state.
- A WPF-only visual artifact disappears because the WebView design exposes the same user action in a cleaner control.
- A temporary migration adapter exists with an explicit owner and retirement slice.
