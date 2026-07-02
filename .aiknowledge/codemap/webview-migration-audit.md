---
status: active
created_at: 2026-07-02
created_from: archive:2026-07-02-webview-migration-clean-restart-01-migration-harness-and-contract-audit
last_verified_at: 2026-07-02
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - docs/webview-migration
  - tests/DesktopLoudnessMeter.Tests/WebViewMigration
  - openspec/changes/archive/2026-07-02-webview-migration-clean-restart-01-migration-harness-and-contract-audit
superseded_by:
---

# WebView Migration Audit

## Responsibility
Maintains the clean WebView migration decision harness: parity rules, salvage ledger, HostBridge command matrix, state ownership map, deterministic scenario traces, native-boundary baseline, and xUnit guards that keep those documents from drifting.

## Key Files

| File | Role |
|------|------|
| docs/webview-migration/parity-decision-rules.md | Classifies differences from `main` as fatal, blocking-fixable, acceptable-intentional, or discardable-legacy |
| docs/webview-migration/salvage-ledger.md | Records which reference WebView branch components may be copied, adapted, redesigned, deleted, or deferred |
| docs/webview-migration/hostbridge-contract-matrix.md | Audits frontend commands, mock behavior, real C# bridge handlers, host execution paths, test coverage, and command status |
| docs/webview-migration/state-ownership-map.md | Assigns one owner for migrated snapshot, settings, derived, and transient WebView state |
| docs/webview-migration/scenario-traces.md | Defines deterministic source workflow, detail settings, render gate, and build/package trace cases |
| docs/webview-migration/native-boundary-baseline.md | Records current optional-native managed fallback promise and reference branch mandatory-native drift |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/MigrationDocumentReader.cs | Locates migration documents from test output directories |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs | Guards required matrix columns, the known alias mismatch, and ownership categories |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/FakeSourceScenarioHarness.cs | Provides deterministic fake source identities for source workflow traces |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/SourceWorkflowScenarioTraceTests.cs | Guards source workflow trace coverage and source-key identity semantics |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/DetailSettingsScenarioTraceTests.cs | Guards detail setting and render active-gate trace coverage |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/NativeBoundaryBaselineTests.cs | Characterizes optional native fallback and reference mandatory-native drift |
| tests/DesktopLoudnessMeter.Tests/WebViewMigration/BuildPackagingBaselineTests.cs | Guards build, publish, and WebView asset baseline traces |

## Implicit Constraints
- `main` is the behavior oracle; the reference WebView branch is a salvage source, not a runtime compatibility contract.
- No reference branch code should be copied unless `salvage-ledger.md` marks it as `copy` or `adapt` with a reason.
- Mock-only WebView commands are contract failures until the real bridge and host execution path exist or the behavior is intentionally redesigned.
- React local state may own transient drafts and focus/hover state only; durable settings and runtime state require a host or persisted owner.
- Scenario traces must run without live audio devices, plugin binaries, or WebView2 browser automation.
- Native acceleration remains optional with managed fallback until the packaging/native slice intentionally changes that product promise.
