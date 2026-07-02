# Design: Host Runtime Service Effect Boundary

## Decision

Introduce an internal `HostRuntimeServiceEffectApplier` in `src/DesktopLoudnessMeter.App/Host/`. It owns the narrow operation "apply already-planned runtime/analyzer side effects to optional source service interfaces".

The applier remains Host glue. Native planners continue to decide reset/performance/chart forwarding values; source services continue to implement capture/analyzer behavior. The new class only centralizes optional interface checks and method invocation.

## Requirement Trace

- R1 -> U1, U2
- R2 -> U1, U2
- R3 -> U2, U3
- R4 -> U4

## Implementation Units

### [U1] Runtime Service Effect Boundary

模块边界:

- `src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs`

Add an internal sealed applier with methods for:

- reset-all and reset-source runtime service plans
- performance mode runtime service plans
- true-peak oversampling analyzer forwarding
- waveform/spectrum channel analyzer forwarding

The applier accepts `ISignalSourceMeterService` and internally checks optional interfaces before invoking side effects.

### [U2] Controller Integration

模块边界:

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Add a private applier field to the controller and replace direct optional service calls with applier calls. The controller continues to:

- route command types
- invoke native planners
- handle source-not-found command flow
- apply native runtime state plans
- apply workspace effects through `HostWorkspaceCommandEffectApplier`
- rebuild Host snapshots

### [U3] Regression Verification

模块边界:

- `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`

Focused tests prove the new boundary; existing controller tests prove command behavior did not drift.

### [U4] Knowledge Refresh

模块边界:

- `.aiknowledge/codemap/host-contract.md`
- `.aiknowledge/codemap/source-list-workspace.md`

Record the proven post-change boundary and keep remaining architecture gaps explicit.

## Interfaces

The applier is internal to the App assembly. It may accept existing native plan record types and Host command request DTOs where needed for source identity. It must not introduce a new public contract or alter service interfaces.

## Risks

- Optional service no-op behavior drift: mitigated by focused tests covering services that do and do not implement optional interfaces.
- Source identity forwarding drift: mitigated by tests verifying source key, source kind, process id, and native-planned values.
- Controller responsibility overclaim: mitigated by codemap explicitly stating Host still owns DTO assembly, native-wrapper orchestration, workspace persistence boundary, and final audit is incomplete.

## Rollback

Revert this change to restore controller-local optional service invocation. No data migration or schema rollback is required.
