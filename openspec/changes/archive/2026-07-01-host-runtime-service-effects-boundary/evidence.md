# Evidence: 2026-07-01-host-runtime-service-effects-boundary

## Explore

- 2026-07-01: `opsx changes list` returned no active changes.
- 2026-07-01: read archived architecture plan `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/`.
- 2026-07-01: read `.aiknowledge/codemap/index.md`, `.aiknowledge/codemap/ui-runtime-architecture.md`, `.aiknowledge/codemap/host-contract.md`, `.aiknowledge/codemap/source-list-workspace.md`, `.aiknowledge/codemap/signal-source-ingest.md`, and `.aiknowledge/codemap/native-dsp.md`.
- 2026-07-01: inspected `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` and confirmed reset/performance/analyzer-setting service calls are still inline in command handlers.
- 2026-07-01: inspected service interfaces and tests via `rg`, confirming existing reset and analysis settings service methods are optional interfaces implemented by fake services and concrete source services.

## Plan

- Scope decision: `KEEP_ONE_CHANGE`.
- Profile decision: `lean-change`.
- Reason: this is a coherent Host glue boundary refactor with focused tests and no native ABI, service interface, DTO, Web UI, or schema changes.
- Non-blocking inference: isolating runtime service effect application should reduce controller responsibility and make later Host glue reduction easier. This is not claimed as final architecture completion.

## Readiness

- 2026-07-01: readiness gate passed.
- Checked `work.md`, `specs/change/spec.md`, `design.md`, `tasks.md`, and `.openspec.yaml`.
- R/U trace is complete: R1 -> U1/U2, R2 -> U1/U2, R3 -> U2/U3, R4 -> U4.
- `tasks.md` includes executable tasks with allowed files, default forbidden/shared-artifact constraints, verification commands, evidence requirements, dependencies, and done conditions.
- Branch/workspace strategy is recorded as `current-worktree` on `codex/2026-07-01-host-runtime-service-effects-boundary`, based on clean commit `8046811`.
- No readiness blocker found; implementation may proceed.

## Implementation

- Task 1.1: Added `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs`.
- Red evidence: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests` failed with expected compile error `CS0246: The type or namespace name 'HostRuntimeServiceEffectApplier' could not be found`.
- Task 1.2: Added `src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs`.
- Focused green evidence: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests` passed 4 tests.
- Task 2.1: Updated `DesktopMeterHostController` to delegate reset/performance/analyzer forwarding to `HostRuntimeServiceEffectApplier`.
- Targeted regression evidence: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostRuntimeServiceEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"` passed 27 tests.
- Search evidence: `rg -n "resetService|settingsService|ResetAllSources|SetTruePeakOversampling|SetChannelWaveformEnabled|SetChannelSpectrumEnabled" src\DesktopLoudnessMeter.App\Host\DesktopMeterHostController.cs src\DesktopLoudnessMeter.App\Host\HostRuntimeServiceEffectApplier.cs` shows optional service method calls only in `HostRuntimeServiceEffectApplier`; controller hits are command method names only.
- Task 3.1: Updated `.aiknowledge/codemap/host-contract.md` and `.aiknowledge/codemap/source-list-workspace.md`.
- Codemap evidence: `rg -n "runtime service effect|HostRuntimeServiceEffectApplier|does not yet qualify|not complete|completion audit" .aiknowledge\codemap\host-contract.md .aiknowledge\codemap\source-list-workspace.md` found the new boundary and remaining non-glue-only note.
- Task 3.2: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed 455 tests with 5 skipped.
- Task 3.2: `git diff --check` passed with no whitespace errors; Git reported LF/CRLF working-copy warnings only.

## Verify

- 2026-07-01: verify passed.
- Change root resolved to `/e/gitlab/uMeter/openspec/changes/2026-07-01-host-runtime-service-effects-boundary`.
- Git evidence: branch `codex/2026-07-01-host-runtime-service-effects-boundary`, HEAD `8046811`.
- Diff ownership: all staged/unstaged/untracked changes belong to this change: OpenSpec artifacts, `HostRuntimeServiceEffectApplier`, controller integration, focused tests, and codemap updates.
- Spec compliance: R1 covered by controller delegation and search evidence; R2 covered by focused tests; R3 covered by targeted controller regression and full managed tests; R4 covered by codemap grep evidence.
- Validation commands were current for this worktree:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests`: passed 4 tests.
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostRuntimeServiceEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"`: passed 27 tests.
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`: passed 455 tests, skipped 5.
  - `git diff --check`: passed; LF/CRLF warnings only.
- No manual/platform checks are required for this refactor.
- No allowed failures or blocking warnings.

## Review

- 2026-07-01: review passed.
- Review scope: current change-owned working tree diff on branch `codex/2026-07-01-host-runtime-service-effects-boundary`, HEAD `8046811`.
- Reviewed changed runtime files: `DesktopMeterHostController.cs`, `HostRuntimeServiceEffectApplier.cs`, and `HostRuntimeServiceEffectApplierTests.cs`.
- Reviewed documentation/knowledge scope: codemap updates and OpenSpec artifacts for this change.
- Findings: no CRITICAL, no WARNING.
- Risk conclusion: the change preserves the existing command flow while moving optional reset/analysis settings service calls into a narrow Host glue class. No service interface, native ABI, HostBridge DTO, Web UI, workspace schema, macOS, startup, tray, or installer behavior was changed.
- Verify gate was current and consistent with review scope.

## Archive

- 2026-07-01: archived to `openspec/changes/archive/2026-07-01-host-runtime-service-effects-boundary`.
- Archive precheck: readiness, verify, and review gates were `pass`.
- Archive precheck: tasks were 5/5 complete; spec file count was 1.
- Manual checks: none required for this refactor.
- Knowledge status: done in implementation; `.aiknowledge/codemap/host-contract.md` and `.aiknowledge/codemap/source-list-workspace.md` were updated before archive.

## Final Gate Summary

- status: archived
- archive_path: `openspec/changes/archive/2026-07-01-host-runtime-service-effects-boundary`
- gates: readiness=pass, verify=pass, review=pass, archive=pass
- specs_count: 1
- manual_followups: none
- allowed_failures: none
- knowledge_status: done; codemap updates are included in this change
- remaining_architecture_note: this change reduces Host glue by isolating runtime service effects, but it is not the final completion audit and does not make the overall WebView UI + C++ Core goal complete.
