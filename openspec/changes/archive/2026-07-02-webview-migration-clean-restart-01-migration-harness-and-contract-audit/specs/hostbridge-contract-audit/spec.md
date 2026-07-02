# HostBridge Contract Audit Spec

## Requirements

### Requirement: Command Contract Matrix
**Trace**: R5

The audit must define a HostBridge command matrix for every command used by the WebView surface or mock bridge in the reference branch.

#### Acceptance Criteria
- Each row names the command, payload fields, result shape, frontend caller, mock implementation, C# bridge handler, host execution path, and test coverage.
- Commands present in the mock bridge but absent from the real bridge are marked as contract failures.
- Commands present in the frontend but absent from both mock and real bridge layers are marked as contract failures.
- Commands present in the real bridge but unused by the frontend are marked as review items.

### Requirement: Real Bridge Enforcement
**Trace**: R6

The planned bridge behavior must make unknown or unimplemented commands fail visibly during development and tests.

#### Acceptance Criteria
- Unknown command handling has a diagnostic path.
- Missing command handlers are not silently swallowed.
- Mock bridge behavior cannot be considered complete unless the real bridge has equivalent behavior or an intentional difference entry.
- The audit explicitly covers the known alias-command mismatch from the reference branch.

### Requirement: Snapshot Ownership Matrix
**Trace**: R7

The audit must define one owner for every state field projected into the WebView snapshot.

#### Acceptance Criteria
- Each snapshot field is marked as host-owned, WebView-derived, persisted-workspace-owned, or transient-view-owned.
- Persisted settings name their storage owner.
- Runtime-only fields name the host service or view model that produces them.
- A field cannot have both React local state and host snapshot ownership unless the design records a synchronization rule.

### Requirement: Contract Test Plan
**Trace**: R8

The audit must define contract tests that verify WebView command payloads reach the real host behavior.

#### Acceptance Criteria
- Tests cover source alias, visibility, ordering, reset, selection, detail settings, and application settings commands.
- Tests exercise the real C# bridge parsing layer where it exists.
- Tests distinguish bridge parsing failures from host behavior failures.
- Tests can run without a live WebView2 browser process.
