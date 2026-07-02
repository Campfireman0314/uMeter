# Evidence: 2026-07-01-host-workspace-command-effects-boundary

## Explore

- 2026-07-01: `opsx changes list` returned no active changes.
- 2026-07-01: read archived architecture plan `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/`.
- 2026-07-01: read `.aiknowledge/codemap/index.md`, `.aiknowledge/codemap/ui-runtime-architecture.md`, `.aiknowledge/codemap/host-contract.md`, `.aiknowledge/codemap/source-list-workspace.md`.
- 2026-07-01: inspected `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` and confirmed controller command handlers directly write workspace settings through `GetOrCreateSource`, `ForgetSource`, and `Save()`.
- 2026-07-01: inspected native command planner wrappers for global/source preference/source order/chart settings and confirmed existing native plan records are available to a managed applier without ABI changes.

## Plan

- Scope decision: `KEEP_ONE_CHANGE`.
- Profile decision: `lean-change`.
- Reason: this is a coherent Host glue boundary refactor with focused tests and no transport/DTO/native ABI/schema changes.
- Non-blocking inference: isolating effect application should make later Host glue reduction easier. This is not claimed as final architecture completion.

## Readiness

### 2026-07-01 readiness pass

- decision: pass
- scope/profile: `KEEP_ONE_CHANGE`, `lean-change`
- artifacts checked: `work.md`, `specs/change/spec.md`, `design.md`, `tasks.md`, `evidence.md`
- R coverage: R1-R4 are present in specs, mapped to U1-U4 in design, and covered by Tasks 1.1-3.2.
- workspace strategy: `current-worktree` on `codex/2026-07-01-host-workspace-command-effects-boundary`; baseline was clean before branch creation.
- implementation boundary: allowed files constrain the change to Host workspace effect boundary, focused tests, codemap, and this OpenSpec change.
- readiness conclusion: implementation may proceed. This pass does not claim architecture goal completion.

## Implementation

### Task 1.1 red test evidence

- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostWorkspaceCommandEffectApplierTests`
- exit: 1
- result: expected compile failure before implementation
- key error: `CS0246: The type or namespace name 'HostWorkspaceCommandEffectApplier' could not be found`

### Task 1.2 focused passing evidence

- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostWorkspaceCommandEffectApplierTests`
- exit: 0
- result: passed
- summary: 4 passed, 0 failed, 0 skipped

### implement amendment: show-hidden boundary correction

- classification: spec-amendment
- reason: current code evidence shows `SetShowHiddenSources` updates `NativeHostRuntimeState` and does not persist workspace settings. Treating show-hidden as a workspace effect would introduce behavior drift.
- changes: removed show-hidden from `HostWorkspaceCommandEffectApplier`, focused test expectation, and workspace-backed global requirement wording.
- mini-readiness: R2 remains mapped to U1/U2 and Tasks 1.1/1.2; show-hidden runtime behavior remains covered by R3/U2/controller regression.

### Task 2.1 targeted regression evidence

- command: `rg -n "GetOrCreateSource|ForgetSource|\.Save\(" src\DesktopLoudnessMeter.App\Host\DesktopMeterHostController.cs src\DesktopLoudnessMeter.App\Host\HostWorkspaceCommandEffectApplier.cs`
- result: controller has no direct `GetOrCreateSource` or `.Save()` workspace effect calls; workspace mutation calls are isolated in `HostWorkspaceCommandEffectApplier`; controller retains `ForgetSource` command method and calls `_workspaceEffects.ApplyForgetSource`.
- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostWorkspaceCommandEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"`
- exit: 0
- result: passed
- summary: 27 passed, 0 failed, 0 skipped

### Task 3.1 codemap evidence

- command: `rg -n "workspace command effect|effect application|HostWorkspaceCommandEffectApplier|not complete|not fully|does not yet qualify" .aiknowledge\codemap\host-contract.md .aiknowledge\codemap\source-list-workspace.md`
- exit: 0
- result: codemap records `HostWorkspaceCommandEffectApplier`, workspace command effect application, and that Host still does not yet qualify as glue-only.

### Task 3.2 final managed validation evidence

- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- exit: 0
- result: passed
- summary: 451 passed, 0 failed, 5 skipped
- command: `git diff --check`
- exit: 0
- result: passed; Git emitted CRLF working-copy warnings only.

## Verify

### 2026-07-01 verify pass

- decision: pass
- branch: `codex/2026-07-01-host-workspace-command-effects-boundary`
- head: `3ef2a7216449f4a76e6aa6e922c78f11f4816edb`
- diff status: unstaged/untracked files are all owned by this change before archive.
- spec compliance:
  - R1: controller delegates workspace-backed command effect application to `HostWorkspaceCommandEffectApplier`.
  - R2: applier focused tests cover workspace-backed global settings, source preference/order, forget, and chart/detail settings.
  - R3: targeted Host controller regression tests pass, preserving command behavior.
  - R4: codemap updated with the new boundary and remaining non-complete architecture gaps.
- static boundary check:
  - command: `rg -n "_workspaceSettings\.(GetOrCreateSource|ForgetSource|Save)" src\DesktopLoudnessMeter.App\Host\DesktopMeterHostController.cs`
  - exit: 1
  - result: no direct controller workspace mutation/persistence calls remain.
- fresh validation:
  - focused Host tests: 27 passed, 0 failed, 0 skipped.
  - full managed tests: 451 passed, 0 failed, 5 skipped.
  - `git diff --check`: pass with CRLF warnings only.
- conclusion: verified for archive review. This does not claim final architecture completion.

## Review

### 2026-07-01 release risk review pass

- decision: pass
- review scope: current change-owned unstaged/untracked diff for Host controller integration, new `HostWorkspaceCommandEffectApplier`, focused tests, codemap updates, and OpenSpec artifacts.
- gates checked: `gates.verify.status: pass`.
- findings:
  - CRITICAL: none
  - WARNING: none
  - SUGGESTION: none
- risk checks:
  - show-hidden remains runtime state in `DesktopMeterHostController` and is not persisted by `HostWorkspaceCommandEffectApplier`.
  - workspace-backed command effects still use native planner outputs and one apply/save boundary per command.
  - source existence guarding remains in controller before source-scoped chart/detail workspace mutation.
  - analyzer/runtime service forwarding remains in controller and was covered by targeted Host controller regression tests.
- conclusion: no release-blocking risk found. Route to archive.

## Archive

### 2026-07-01 archive pass

- decision: pass
- archive_path: `openspec/changes/archive/2026-07-01-host-workspace-command-effects-boundary`
- spec_count: 1
- tasks_status: all tasks completed.
- gates: readiness pass, verify pass, review pass, archive pass.
- manual_followups: none.
- allowed_failures: none.
- knowledge_status:
  - status: done
  - evidence_ref: codemap updates in `.aiknowledge/codemap/host-contract.md` and `.aiknowledge/codemap/source-list-workspace.md`
- final_summary:
  - Added `HostWorkspaceCommandEffectApplier` as the Host glue boundary for workspace-backed command effect application.
  - Updated `DesktopMeterHostController` to delegate workspace persistence effects while keeping runtime/service/snapshot orchestration.
  - Added focused tests and retained Host controller regression coverage.
  - This archive does not mark the overall WebView UI + C++ Core architecture goal complete.
