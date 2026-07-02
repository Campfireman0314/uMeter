# Design: Host Snapshot Assembly Boundary

## Decision

Introduce an internal `HostSnapshotAssembler` in `src/DesktopLoudnessMeter.App/Host/`. It owns the narrow operation "assemble Host snapshot DTOs from already-reconciled Host source rows, native snapshot planners, workspace settings, runtime state values, and native recent-event state".

The assembler remains Host glue. Native planners continue to own aggregate/detail/chrome planning decisions. The assembler centralizes DTO construction and selected-detail plan mapping that currently lives in `DesktopMeterHostController`.

## Requirement Trace

- R1 -> U1, U2
- R2 -> U1, U2, U3
- R3 -> U2, U3
- R4 -> U4

## Implementation Units

### [U1] Snapshot Assembler Boundary

模块边界:

- `src/DesktopLoudnessMeter.App/Host/HostSnapshotAssembler.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotAssemblerTests.cs`

Add an internal sealed assembler with a method that builds `HostSnapshot` from:

- current `IReadOnlyList<HostSourceSnapshot>`
- selected source key from native runtime state
- show-hidden state from native runtime state
- optional status text override
- `UserWorkspaceSettings`
- `NativeHostRecentEvents`

The assembler maps native selected-detail snapshot plans to Host DTOs.

### [U2] Controller Integration

模块边界:

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Add a private assembler field and replace controller-local `BuildSnapshot` / `BuildDetailSnapshot` helper implementation with assembler calls. Controller continues to:

- poll source service on refresh
- reconcile source rows and recent events
- route typed commands
- invoke native command planners
- apply runtime state plans
- apply workspace/runtime service effects
- preserve `LastSourceSnapshots`

### [U3] Regression Verification

模块边界:

- `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotAssemblerTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Focused tests prove the new boundary; existing controller tests prove command behavior did not drift.

### [U4] Knowledge Refresh

模块边界:

- `.aiknowledge/codemap/host-contract.md`
- `.aiknowledge/codemap/source-list-workspace.md`

Record the proven post-change boundary and keep remaining architecture gaps explicit.

## Interfaces

The assembler is internal to the App assembly. It must not introduce a new public contract, alter `HostSnapshot` DTO shape, or change native planner wrapper APIs.

## Risks

- Selected-detail mapping drift: mitigated by focused tests with visible modules and collision bands plus controller regression tests.
- Status/command availability drift: mitigated by focused aggregate snapshot assertions and existing controller command availability tests.
- Responsibility overclaim: mitigated by codemap explicitly stating Host still owns DTO assembly through a glue boundary and final audit remains incomplete.

## Rollback

Revert this change to restore controller-local snapshot assembly. No data migration or schema rollback is required.
