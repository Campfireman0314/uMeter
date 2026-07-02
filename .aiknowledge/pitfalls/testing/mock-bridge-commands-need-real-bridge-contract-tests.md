---
status: active
created_at: 2026-07-02
created_from: change:2026-07-02-webview-migration-clean-restart/01-migration-harness-and-contract-audit
last_verified_at: 2026-07-02
last_verified_by: opsx-knowledge
verification_basis: archive
applies_to:
  - docs/webview-migration/hostbridge-contract-matrix.md
  - tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs
superseded_by:
---

# Mock Bridge Commands Need Real Bridge Contract Tests

**Tags:** [webview, bridge, testing, csharp, typescript]

## Symptom

A WebView UI appears functional in development because the mock bridge accepts a command, but the packaged app cannot perform the same action because the real C# bridge has no matching handler.

## Root Cause

The mock bridge and frontend were treated as enough evidence for behavior. The real bridge command switch, host execution path, and test coverage were not checked as one contract.

## Before Fix

```diff
+ mockHostBridge.ts handles "setSourceAlias"
+ AppShell.tsx sends "setSourceAlias"
- HostBridgeController.cs has no "setSourceAlias" command mapping
```

## After Fix

```diff
+ HostBridge contract matrix includes command, payload, frontend caller, mock implementation, C# bridge handler, host execution path, test coverage, and status.
+ HostBridgeContractMatrixTests assert the known alias mismatch is recorded as a contract-failure.
```

## Key Takeaway

For WebView host bridges, test the contract across frontend, mock, real bridge, host execution, and coverage so mock-only behavior cannot masquerade as product behavior.

## Source

archive: openspec/changes/archive/2026-07-02-webview-migration-clean-restart-01-migration-harness-and-contract-audit (2026-07-02)
