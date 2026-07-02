# Evidence: 2026-06-29-native-source-ingest-core

正文默认使用中文；路径、命令、配置键和代码标识保持原文。

## Explore Baseline

### Commands
- `opsx changes list`
  - result: `无活动变更`
- `git status --short --branch`
  - result before branch creation: `## codex/2026-06-29-settings-workspace-schema`, clean
- `git log -5 --oneline`
  - key head: `568842a feat(settings): version workspace schema`
- Read archived planning:
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**`
  - `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/**` via codemap
- Read codemap:
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- Read source/tests:
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`
  - `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt`

### 已证实事实
- 当前没有活动 OpenSpec change。
- 当前 clean base head 是 `568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`。
- `LocalLoopbackSignalSourceService` 当前 C# implementation owns `_sourcesByHandle`, timeout cleanup, register/heartbeat/rename/audio/close handling, per-handle settings dictionaries, `SourceState`, `PcmMetricAnalyzer` ownership, and `SignalSourceSnapshot` creation。
- Existing tests cover duplicate names preserving handle identity, audio metrics/settings controls, same-handle changed-format reconfiguration, heartbeat/timeout/close/reregister。
- Native analyzer ABI already uses opaque handles, integer return codes, exported wrapper functions and copy-buffer methods。
- `DesktopLoudnessMeter.Native.vcxproj` and `CMakeLists.txt` both explicitly list native source files。
- `.aiknowledge/codemap/ui-runtime-architecture.md` states future UI runtime is WebView, not Flutter; old cross-platform architecture plan is historical context.

### 推断 / 非阻塞假设
- Native source ingest can reuse `DlmPcmAnalyzer` per source without changing analyzer semantics.
- C# can remain string/source-key mapping owner while native owns handle-keyed registry and lifecycle.
- Existing local loopback focused tests are sufficient starting parity oracle but should be expanded for invalid audio and same-format register behavior.

### 自我反驳 / 漏洞扫描
- 漏洞：`native-source-ingest-core` could become too large if it includes WASAPI/process loopback capture.
  - 处理：明确 out-of-scope；本 change only local/plugin loopback packet lifecycle.
- 漏洞：old archived planning mentions Flutter.
  - 处理：work/design explicitly record WebView replan supersedes Flutter wording.
- 漏洞：native ABI string copy design may be underspecified.
  - 处理：design requires capacity/actualCount copy semantics and tests for names.
- 漏洞：native build tools may be unavailable.
  - 处理：tasks require recording exact blocker and fallback coverage if unavailable.
- 漏洞：moving snapshot projection native could accidentally change HostBridge DTOs.
  - 处理：scope forbids HostBridge/WebView changes; C# service maps back to existing `SignalSourceSnapshot`.

## Plan Evidence

### Branch / change creation
- `git switch -c codex/2026-06-29-native-source-ingest-core`
  - result: created and switched to dedicated branch.
- `opsx changes init 2026-06-29-native-source-ingest-core spec-driven --profile strict-change --source-type feature`
  - result: initialized `openspec/changes/2026-06-29-native-source-ingest-core`.

### Scope decision
- result: KEEP_ONE_CHANGE
- profile: strict-change
- rationale: touches native ABI, managed adapter, source lifecycle state machine, tests and codemap, but remains one module closure around local/plugin loopback ingest.

## Readiness Evidence

### 2026-06-29T19:47:29.7186283+08:00 - pass

StageResult:
- stage: `opsx-readiness`
- status: `pass`
- change: `2026-06-29-native-source-ingest-core`
- branch: `codex/2026-06-29-native-source-ingest-core`
- workspace: `E:\gitlab\uMeter`

Checks:
- `work.md` contains intent, scope, non-goals, success criteria, verification plan, rollback/escalation, workspace strategy and rejected attempts.
- `specs/change/spec.md` defines globally unique R1-R5 requirements with observable scenarios.
- `design.md` maps R1-R5 to U1-U5, and every U section includes module boundary.
- `tasks.md` covers R/U mappings with concrete allowed files, verification commands, evidence requirements and dependencies.
- `Default Constraints` are present, so omitted forbidden/shared/return fields inherit the controller-owned artifact rules.
- No `[stabilization-first]` task is used; this is acceptable because behavior can be covered by automated regression/build tests.
- Workspace strategy is resolved as `current-worktree` on a clean dedicated branch based at `568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`.
- The stale Flutter wording risk is handled: work/design/evidence state that WebView runtime replan supersedes the historical Flutter route.

Warnings:
- Native string-copy ABI exact function names remain design-level until implementation. This is non-blocking because the design constrains the pattern and tasks require tests/evidence.
- Native build tooling may be unavailable in this environment. This is non-blocking because tasks require exact blocker recording and fallback smoke coverage.

Decision:
- readiness passed; implementation may begin under `tasks.md`.

## Implementation Evidence

### Task 1.1 - lifecycle parity tests

Changes:
- Expanded `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` with:
  - same-format register touches same handle without resetting analyzer metrics;
  - invalid/empty/misaligned/unknown-handle audio packets are ignored without corrupting existing state.

Command:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`

Result:
- pass: 6 passed, 0 failed, 0 skipped.

Coverage:
- R1/R2 lifecycle and identity parity oracle established before native implementation.

### Tasks 1.2, 2.1, 2.2 and 3.1 - native source ingest implementation

Changes:
- Added `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h` and `.cpp`.
- Added `DlmSourceIngestCore*Export` functions in `DesktopLoudnessMeterNative.cpp`.
- Added `DlmSourceIngestCore.cpp` and `.h` to `DesktopLoudnessMeter.Native.vcxproj`; added `.cpp` to `CMakeLists.txt`.
- Added `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs`.
- Reworked `LocalLoopbackSignalSourceService` into a managed adapter over native source ingest while preserving public interfaces.

Build command:
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`

Build result:
- pass: MSBuild 17.14 compiled `DlmSourceIngestCore.cpp` and produced `src\DesktopLoudnessMeter.Native\x64\Release\DesktopLoudnessMeter.Native.dll`.

Focused command:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`

Focused result:
- initial fail: copied native Release DLL lacked `DlmSourceIngestCoreCreateExport`.
- after native Release rebuild: pass, 6 passed, 0 failed, 0 skipped.

Coverage:
- R1 native lifecycle functions exercised through managed service.
- R2 identity/reconfiguration parity exercised by focused tests.
- R3 snapshot/metric copy exercised by audio/channel-array tests.
- R4 managed service adapter compatibility exercised by existing public service calls.
- R5 Windows native MSBuild file list and CMake file list updated.

### Task 3.2 - regression/build sanity

Commands and results:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~Native`
  - pass: 13 passed, 0 failed, 0 skipped.
- `dotnet test desktop-loudness-meter.sln`
  - command-level fail: `dotnet` CLI cannot import native `.vcxproj` because `$(VCTargetsPath)\Microsoft.Cpp.Default.props` is unavailable in dotnet CLI context.
  - test assembly still ran and passed: 311 passed, 5 skipped.
  - handling: use verified native `build-native.ps1` MSBuild route plus full test project command as this change's reliable full regression route.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
  - pass: 311 passed, 5 skipped.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
  - pass: build succeeded, 0 warnings, 0 errors.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
  - pass: 5 files passed, 11 tests passed.
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
  - pass: Vite production build succeeded.

Notes:
- A parallel rerun of focused/native filters caused a transient build-output lock on `uMeter.dll`; serial rerun of focused tests passed. Avoid parallel dotnet builds for WPF project output.
- `where.exe msbuild` and `where.exe cmake` did not find PATH entries, but `build-native.ps1` located Visual Studio/MSBuild through `vswhere` and built Release x64 successfully.

### Task 3.3 - codemap/evidence update

Changes:
- Updated `.aiknowledge/codemap/signal-source-ingest.md` to describe native ownership and C# adapter boundary.
- Updated `.aiknowledge/codemap/native-dsp.md` with `DlmSourceIngestCore` key files and constraints.
- Updated `.aiknowledge/codemap/chains/pcm-metric-flow.md` with the local/plugin loopback metric flow through native source ingest.

Command:
- `rg -n "native source|DlmSourceIngestCore|source ingest|LocalLoopbackSignalSourceService" .aiknowledge/codemap openspec/changes/2026-06-29-native-source-ingest-core/evidence.md`

Result:
- completed in the Verification Evidence section below.

## Verification Evidence

### 2026-06-29T20:06:49.9634707+08:00 - pass

StageResult:
- stage: `opsx-verify`
- status: `pass`
- branch: `codex/2026-06-29-native-source-ingest-core`
- head: `568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`
- diff status: dirty tree contains current change implementation, tests, OpenSpec artifacts and codemap updates; no unrelated dirty files observed.

Spec compliance:
- R1 pass: native `DlmSourceIngestCore` owns local/plugin register, heartbeat, rename, audio, close and timeout; focused lifecycle tests pass through `LocalLoopbackSignalSourceService`.
- R2 pass: duplicate names, rename, same-format touch, changed-format reconfiguration, settings preservation and stable `loopback:<handle>` identity are covered.
- R3 pass: native snapshot/copy projection feeds existing `SignalSourceSnapshot` / `PcmMetricsSnapshot` semantics; waveform/spectrum/channel arrays are covered by focused tests.
- R4 pass: `LocalLoopbackSignalSourceService` still implements existing service/reset/settings interfaces and delegates to managed native wrapper.
- R5 pass: Windows native MSBuild path, CMake file list, tests, app build, web tests/build and codemap updates are evidenced.

Fresh commands:
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
  - pass: MSBuild 17.14, 0 warnings, 0 errors; native DLL produced at `src\DesktopLoudnessMeter.Native\x64\Release\DesktopLoudnessMeter.Native.dll`.
- `rg -n "native source|DlmSourceIngestCore|source ingest|LocalLoopbackSignalSourceService" .aiknowledge\codemap openspec\changes\2026-06-29-native-source-ingest-core\evidence.md`
  - pass: updated codemap/evidence entries found.
- `rg -n "DlmSourceIngestCore" src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj src\DesktopLoudnessMeter.Native\CMakeLists.txt src\DesktopLoudnessMeter.Native\DesktopLoudnessMeterNative.cpp`
  - pass: native files and exports found.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
  - pass: 6 passed, 0 failed, 0 skipped.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~Native`
  - pass: 13 passed, 0 failed, 0 skipped.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
  - pass: 311 passed, 5 skipped.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
  - pass: build succeeded, 0 warnings, 0 errors.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
  - pass: 5 files passed, 11 tests passed.
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
  - pass: Vite production build succeeded.

Corrected verification route:
- `dotnet test desktop-loudness-meter.sln` is not used as the pass/fail gate because dotnet CLI cannot import the native `.vcxproj` C++ targets (`$(VCTargetsPath)\Microsoft.Cpp.Default.props`); this was observed during implementation. The verified route is native `build-native.ps1` for C++ plus `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` for managed test coverage.

Manual checks:
- `[~] [manual] UI smoke for WebView/WPF source list` remains waived because no UI behavior changed and app/web automated checks passed.

Decision:
- verify passed; route to `opsx-review`.

## Review Evidence

### 2026-06-29T20:08:10.9931169+08:00 - pass

Review Scope Snapshot:
- branch: `codex/2026-06-29-native-source-ingest-core`
- head: `568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`
- scope: uncommitted diff owned by this change, including native source-ingest core, managed adapter, focused tests, OpenSpec artifacts and codemap updates.
- diff stat excluding OpenSpec artifacts: 8 files, 308 insertions, 281 deletions.

Risk review:
- CRITICAL: none.
- WARNING: none.
- SUGGESTION: none blocking archive.

Checked risks:
- Native lifetime: `DlmSourceIngestCoreHandle` owns `SourceState`; `SourceState` destroys its per-source analyzer; service disposal destroys the core.
- ABI/copy safety: variable strings/arrays use caller-owned buffers and capacity/actual count checks; managed wrapper uses UTF-8 string marshalling.
- Scope: no WASAPI capture, JUCE sender, Protocol, HostBridge, WebView UI, tray/startup/installer behavior changes were made.
- Verify freshness: fresh native build, focused/native/full managed tests, app build and web tests/build are recorded under verify.

Decision:
- review passed; route to `opsx-archive`.

## Manual Checks

- [~] [manual] UI smoke for WebView/WPF source list | 原因：本 change preserves host-facing service snapshots and is covered by automated service/host build tests; no UI behavior is intentionally changed.

## Archive Notes

### 2026-06-29T20:08:57.3651455+08:00 - pass

Archive action:
- Moved change root to `openspec/changes/archive/2026-06-29-native-source-ingest-core`.
- Updated `.openspec.yaml` with `status: archived` and `gates.archive.status: pass`.
- Spec count: 1 (`specs/change/spec.md`).

Knowledge auto follow-up:
- mode: `auto`
- status: done
- source_refs:
  - `change:2026-06-29-native-source-ingest-core`
  - `evidence:openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md`
- pre_knowledge_status:
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/native-dsp.md`
  - `M .aiknowledge/codemap/signal-source-ingest.md`
- post_knowledge_status:
  - `M .aiknowledge/codemap/audio-capture-sources.md`
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/native-dsp.md`
  - `M .aiknowledge/codemap/signal-source-ingest.md`
  - `M .aiknowledge/logs/2026-06.md`
- diff_stat:
  - `.aiknowledge/codemap/audio-capture-sources.md  | 10 ++++++----`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md | 12 ++++++++++--`
  - `.aiknowledge/codemap/index.md                  |  4 ++--`
  - `.aiknowledge/codemap/native-dsp.md             | 22 +++++++++++++++++++---`
  - `.aiknowledge/codemap/signal-source-ingest.md   | 22 ++++++++++++++++------`
  - `.aiknowledge/logs/2026-06.md                   |  2 ++`
  - `6 files changed, 55 insertions(+), 17 deletions(-)`

Final Gate Summary:
- archive_path: `openspec/changes/archive/2026-06-29-native-source-ingest-core`
- readiness: pass (`2026-06-29T19:47:29.7186283+08:00`)
- verify: pass (`2026-06-29T20:06:49.9634707+08:00`)
- review: pass (`2026-06-29T20:08:10.9931169+08:00`)
- archive: pass (`2026-06-29T20:08:57.3651455+08:00`)
- manual_followups: none; UI smoke waiver is closed as non-blocking because no UI behavior changed and automated service/host/web checks passed.
- knowledge_status: done; evidence_ref: `Archive Notes / Knowledge auto follow-up`
- allowed_failures: none.
- retained_warnings: none.
