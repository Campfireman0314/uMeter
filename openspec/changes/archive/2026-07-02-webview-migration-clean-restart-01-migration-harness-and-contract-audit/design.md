# Migration Harness And Contract Audit Design

## Overview

This slice creates the migration ruler before product-code migration resumes. It does not port the WebView shell, source workflows, charts, or packaging. Instead, it defines how to classify differences from `main`, how to audit the reference WebView branch, and how to convert important product promises into deterministic traces and contract tests.

The working rule is simple: `main` is the behavior oracle, the reference WebView branch is a source of salvage candidates, and compatibility bridges are temporary implementation aids only when they have an owner and a retirement condition.

## Key Decisions

- Keep this slice documentation-heavy but test-aware. The outputs are ledgers, matrices, and harness test scaffolding that later slices can execute against real migrated code.
- Store migration audit documents under `docs/webview-migration/` so they survive across subchanges and remain easy to review outside OPSX.
- Put C# harness tests under `tests/DesktopLoudnessMeter.Tests/WebViewMigration/` so they can reuse existing xUnit infrastructure and app test helpers.
- Avoid live WebView2 browser automation in this slice. Contract tests should exercise command parsing and host-facing behavior without requiring a browser process.
- Treat the prior WebView branch as read-only. Copying code from `D:\GitRepo\uMeter-webview-reference` requires an explicit salvage-ledger entry first.

## Audit Documents

The implementation should create or update these durable documents:

- `docs/webview-migration/parity-decision-rules.md`
- `docs/webview-migration/salvage-ledger.md`
- `docs/webview-migration/hostbridge-contract-matrix.md`
- `docs/webview-migration/state-ownership-map.md`
- `docs/webview-migration/scenario-traces.md`
- `docs/webview-migration/native-boundary-baseline.md`

## Implementation Units

### U1: Parity Decision Rules
Module Boundary:
- `docs/webview-migration/parity-decision-rules.md`
- `docs/webview-migration/salvage-ledger.md`
- `openspec/changes/2026-07-02-webview-migration-clean-restart/subchanges/01-migration-harness-and-contract-audit/specs/parity-decision-harness/spec.md`

Responsibilities:
- Define the fatal, blocking-fixable, acceptable-intentional, and discardable-legacy taxonomy.
- Record product-promise categories from `main`.
- Record compatibility bridge policy.
- Seed the salvage ledger with known reference-branch findings.

### U2: HostBridge Contract Audit
Module Boundary:
- `docs/webview-migration/hostbridge-contract-matrix.md`
- `docs/webview-migration/state-ownership-map.md`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs`
- `openspec/changes/2026-07-02-webview-migration-clean-restart/subchanges/01-migration-harness-and-contract-audit/specs/hostbridge-contract-audit/spec.md`

Responsibilities:
- Inventory frontend commands, mock bridge commands, real C# bridge handlers, host execution paths, and test coverage.
- Mark mock-only and frontend-only commands as failures.
- Define unknown-command diagnostics for later bridge work.
- Assign single ownership for each projected snapshot field.

### U3: Scenario Trace Harness
Module Boundary:
- `docs/webview-migration/scenario-traces.md`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/FakeSourceScenarioHarness.cs`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/SourceWorkflowScenarioTraceTests.cs`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/DetailSettingsScenarioTraceTests.cs`
- `openspec/changes/2026-07-02-webview-migration-clean-restart/subchanges/01-migration-harness-and-contract-audit/specs/scenario-trace-harness/spec.md`

Responsibilities:
- Define deterministic fake source inputs for WASAPI, local loopback, and plugin-style sources.
- Capture trace cases for source workflows, detail settings, and rendering work gates.
- Make source-key identity independent from display title in every relevant trace.
- Keep traces runnable without live audio devices, plugin binaries, or WebView2 automation.

### U4: Native And Packaging Baseline
Module Boundary:
- `docs/webview-migration/native-boundary-baseline.md`
- `docs/webview-migration/scenario-traces.md`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/NativeBoundaryBaselineTests.cs`
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/BuildPackagingBaselineTests.cs`

Responsibilities:
- Record the current native analyzer and managed fallback promise from `main`.
- Record the reference branch native-mandatory behavior.
- Define restore, build, test, publish, and WebView asset-loading baselines for later packaging work.
- Identify checks that belong to `05-packaging-native-boundary-and-retirement`.

## Traceability

- R1: U1
- R2: U1, U3, U4
- R3: U1
- R4: U1, U2, U4
- R5: U2
- R6: U2
- R7: U2
- R8: U2
- R9: U3
- R10: U3
- R11: U3
- R12: U3
- R13: U4
- R14: U4

## Split Check

This remains one delivery unit. The artifacts are distinct, but they all answer one question: how the migration will judge, audit, and characterize behavior before rebuilding product surfaces. None of these outputs is independently valuable without the others, and all should converge in one review and verification pass.

## Risks

- The reference branch can change or disappear. The current detached worktree path and commit should be recorded in the salvage ledger.
- The contract matrix can become stale if later slices add commands without updating it. Later slices should treat matrix updates as part of their done criteria.
- Over-documenting without executable tests would create false confidence. The task plan should prioritize at least thin tests for matrix shape and trace fixtures.
