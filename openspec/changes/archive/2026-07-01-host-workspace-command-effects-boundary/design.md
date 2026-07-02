# Design: Host Workspace Command Effect Boundary

## Decision

Introduce an internal `HostWorkspaceCommandEffectApplier` in `src/DesktopLoudnessMeter.App/Host/`. It owns the narrow operation "apply already-planned native command effects to `UserWorkspaceSettings` and persist them".

This is intentionally a Host glue boundary, not a C++ Core migration. Native planners remain the decision authority for normalization/validation where they already are; the applier only materializes accepted plans into the existing managed workspace store.

## Requirement Trace

- R1 -> U1, U2
- R2 -> U1, U2
- R3 -> U2, U3
- R4 -> U4

## Implementation Units

### [U1] Workspace Effect Boundary

模块边界:

- `src/DesktopLoudnessMeter.App/Host/HostWorkspaceCommandEffectApplier.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/HostWorkspaceCommandEffectApplierTests.cs`

Add an internal sealed applier with methods for:

- workspace-backed global settings plans
- source alias/hidden/forget plans
- source order plans
- chart settings plans

The applier takes `UserWorkspaceSettings` through its constructor. Each apply method performs the same workspace mutation previously performed inside the controller and calls `Save()` with the same per-command semantics.

### [U2] Controller Integration

模块边界:

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Add a private applier field to the controller and replace direct workspace command writes with applier calls. The controller continues to:

- route command types
- invoke native planners
- perform source existence checks
- apply show-hidden runtime state
- forward analyzer/runtime service effects
- reconcile native recent events
- rebuild Host snapshots

### [U3] Regression Verification

模块边界:

- `tests/DesktopLoudnessMeter.Tests/Host/HostWorkspaceCommandEffectApplierTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Keep existing controller tests as behavior regression coverage and add focused applier tests so the new boundary is directly testable.

### [U4] Knowledge Refresh

模块边界:

- `.aiknowledge/codemap/host-contract.md`
- `.aiknowledge/codemap/source-list-workspace.md`

Record the proven post-change boundary. Do not mark Host glue reduction, macOS readiness, WPF retirement, chart parity, or final completion audit as complete.

## Interfaces

The applier remains internal to the App assembly. It may accept native plan record types already returned by existing planners. This keeps DTO/ABI unchanged and avoids duplicating normalization logic.

## Risks

- Save semantics drift: mitigated by focused tests and existing controller regression tests.
- Accidental overclaim in knowledge docs: mitigated by explicitly documenting remaining gaps.
- Hidden source/key behavior drift: mitigated by preserving controller checks before invoking the applier.

## Rollback

Revert this change to restore controller-local workspace mutation. No data migration or schema rollback is required.
