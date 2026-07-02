# Spec: Host Runtime Service Effect Boundary

## Requirements

### R1: Controller Delegation

`DesktopMeterHostController` shall delegate runtime service side-effect application to a dedicated Host boundary.

**Trace**: R1

#### Scenarios

- When a Host command produces a native runtime service plan, the controller calls the dedicated boundary to apply the service side effect.
- When a source-scoped chart command produces analyzer service forwarding values, the controller calls the dedicated boundary to apply them.
- The controller does not directly call reset or analysis settings service methods.

### R2: Service Forwarding Preservation

The dedicated boundary shall preserve existing reset, performance, true-peak, waveform, and spectrum service forwarding behavior.

**Trace**: R2

#### Scenarios

- Applying a reset-all plan invokes `ResetAllSources` only when the source service implements reset support and the plan allows reset-all.
- Applying a reset-source plan invokes `ResetSource` only when the source service implements reset support and the plan includes a successful reset target.
- Applying a performance plan invokes `SetPerformanceMode` only when the source service implements analysis settings support and the plan is successful.
- Applying a true-peak plan invokes `SetTruePeakOversampling` with the source identity and service oversampling value from the native chart plan.
- Applying waveform and spectrum plans invoke channel waveform/spectrum enablement with source identity and native channel-enabled values.
- When the source service lacks the relevant optional interface, applying the effect is a no-op.

### R3: Host Command Regression

Existing Host command externally observable behavior shall remain unchanged.

**Trace**: R3

#### Scenarios

- Command success/error results remain the same for reset, performance, and source-scoped chart settings.
- Snapshot rebuilds after commands expose the same settings and runtime state as before.
- Workspace persistence remains owned by `HostWorkspaceCommandEffectApplier` and is not merged into the runtime service boundary.

### R4: Knowledge Update

Codemap documentation shall describe the runtime service effect boundary and remaining incomplete architecture work.

**Trace**: R4

#### Scenarios

- `host-contract` records that runtime service effect application is isolated from `DesktopMeterHostController`.
- Codemap notes that Host still does not yet qualify as glue-only and final completion audit remains out of scope.
