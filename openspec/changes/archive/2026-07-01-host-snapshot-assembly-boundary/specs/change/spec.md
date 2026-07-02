# Spec: Host Snapshot Assembly Boundary

## Requirements

### R1: Controller Delegation

`DesktopMeterHostController` shall delegate Host snapshot assembly to a dedicated Host boundary.

**Trace**: R1

#### Scenarios

- When the controller needs an initial, refreshed, failure, or post-command snapshot, it calls the dedicated boundary.
- The controller does not contain selected-detail DTO assembly helper methods.

### R2: Snapshot Preservation

The dedicated boundary shall preserve aggregate snapshot fields and selected-detail mapping.

**Trace**: R2

#### Scenarios

- Aggregate snapshots include the same sources, selected detail, settings, recent events, status text, and command availability as before.
- Selected-detail snapshots preserve module rows, visibility flags, collision settings, comparison metadata, and collision bands from native plans.
- Status text overrides continue to take precedence over native aggregate status text.

### R3: Host Command Regression

Existing Host command externally observable snapshots shall remain unchanged.

**Trace**: R3

#### Scenarios

- Refresh, selected-source, source preference, global settings, chart settings, runtime service, and source-order commands continue to return equivalent post-command snapshots.
- Mutating commands continue rebuilding from `LastSourceSnapshots` without polling the source service again.

### R4: Knowledge Update

Codemap documentation shall describe the snapshot assembly boundary and remaining incomplete architecture work.

**Trace**: R4

#### Scenarios

- `host-contract` records that snapshot assembly is isolated from `DesktopMeterHostController`.
- Codemap notes that Host still does not yet qualify as glue-only and final completion audit remains out of scope.
