# Spec: Host Workspace Command Effect Boundary

## Requirements

### R1: Controller Delegation

`DesktopMeterHostController` shall delegate workspace-backed command effect application to a dedicated Host boundary.

**Trace**: R1

#### Scenarios

- When a Host command produces a native plan that writes workspace settings, the controller calls the dedicated boundary to apply that plan.
- The controller does not directly call `UserWorkspaceSettings.GetOrCreateSource` for command effect mutation.
- The controller does not directly call `UserWorkspaceSettings.ForgetSource` for command effect mutation.
- The controller does not directly call `UserWorkspaceSettings.Save()` for command effect persistence.

### R2: Workspace Mutation Preservation

The dedicated boundary shall preserve the existing workspace mutations for workspace-backed global, source preference, source order, and chart settings commands.

**Trace**: R2

#### Scenarios

- Applying workspace-backed global settings plans updates theme, recent-event limit, and event output folder exactly as current command handlers do.
- Applying a show-hidden plan remains outside the workspace effect boundary because current behavior stores show-hidden in runtime state.
- Applying a source preference plan updates alias and hidden state for the target source.
- Applying a source preference forget plan removes the source only when the native plan says it should be forgotten.
- Applying a source order plan updates order indices for every native row and persists once per command application.
- Applying chart settings plans updates true-peak oversampling, waveform display mode, spectrum display mode, detail module visibility, spectrum display range, loudness clock settings, and collision settings.

### R3: Host Command Regression

Existing Host command externally observable behavior shall remain unchanged.

**Trace**: R3

#### Scenarios

- Command success/error results remain the same for known and unknown source keys.
- Snapshot rebuilds after commands expose the same settings, source rows, selected detail, and recent event state as before.
- Analyzer/runtime service forwarding remains owned by the controller/service glue and is not removed by this change.

### R4: Knowledge Update

Codemap documentation shall describe the new boundary and remaining incomplete architecture work.

**Trace**: R4

#### Scenarios

- `host-contract` records that workspace command effect application is isolated from `DesktopMeterHostController`.
- Codemap notes that Host is still not fully glue-only and macOS/WPF/final audit gaps remain out of scope.
