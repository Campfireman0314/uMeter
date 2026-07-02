# Evidence: 2026-06-29-settings-workspace-schema

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保留原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-29 19:20 +08:00
- final_verify:
  - status: pass
  - evidence_ref: Verify / 2026-06-29 19:34 +08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-29 19:35 +08:00
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-06-29-settings-workspace-schema
  - evidence_ref: Archive / 2026-06-29 19:36 +08:00
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Task 4.2 docs/codemap update and Archive / 2026-06-29 19:36 +08:00

## Plan Evidence

### 2026-06-29 plan

- stage: plan
- change: `2026-06-29-settings-workspace-schema`
- branch: `codex/2026-06-29-settings-workspace-schema`
- profile: `strict-change`
- source_type: `feature`
- scope_decision: `KEEP_ONE_CHANGE`
- workspace_strategy: `current-worktree`
- commands:
  - `opsx changes list`
    - result: `无活动变更`
  - `git status --short --branch`
    - result: clean on `codex/2026-06-29-native-core-build-boundary`
  - `opsx changes resolve 2026-06-29-settings-workspace-schema`
    - result: change did not exist before init
  - `git switch -c codex/2026-06-29-settings-workspace-schema`
    - result: branch created
  - `opsx changes init 2026-06-29-settings-workspace-schema spec-driven --profile strict-change --source-type feature`
    - result: initialized change root
- codemap:
  - `.aiknowledge/codemap/source-list-workspace.md` confirms source identity/workspace preferences are stable by source key.
  - `.aiknowledge/codemap/host-contract.md` confirms settings schema versioning belongs to `settings-workspace-schema`, not previous host contract.
  - `.aiknowledge/codemap/webview-shell.md` confirms frontend consumes HostBridge DTOs and currently has mockable TypeScript boundary.
  - `.aiknowledge/codemap/ui-runtime-architecture.md` confirms WebView UI + C++ Core route and that a single module does not complete the overall goal.
- archive_refs:
  - `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/work.md` lines for `settings-workspace-schema`.
  - `openspec/changes/archive/2026-06-29-host-snapshot-command-contract/design.md` and specs confirming schema migration was intentionally out of scope.
  - `openspec/changes/archive/2026-06-29-webview-windows-app-shell/design.md` confirming DTO mapper/type stability belongs to later schema work.
- proven_facts:
  - `UserWorkspaceSettings` currently has no explicit schema version.
  - Existing normalize path already handles legacy `CollisionAbsoluteThresholdDb`.
  - HostBridge and TypeScript DTOs currently expose settings fields but not schema version.
- inferences:
  - `SchemaVersion` / `schemaVersion` is the likely minimal field naming; implementation tests will lock exact shape.
  - Multi-version migration is not needed yet because only legacy no-version -> current schema is in scope.
- self_check:
  - blocking: none after codemap/archive/source read.
  - non_blocking: scope creep into full settings UI; handled by R5, non-goals, tasks, and boundary grep.

## Readiness

### 2026-06-29 19:20 +08:00

- stage: readiness
- decision: pass
- reviewer: main-agent
- evidence_refs:
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `.openspec.yaml`
- checks:
  - work: pass，Intent、Scope、Non-Goals、Verification Plan、Rollback / Escalation 完整。
  - specs: pass，R1-R5 全局唯一，均描述可观察行为。
  - design: pass，R1-R5 全部映射到 U1-U4，所有实施单元都有模块边界、验证方式和风险/回退说明。
  - tasks: pass，5 个 task 均有需求追踪、Allowed Files、验收标准、验证命令、Evidence Required、Done When；`Default Constraints` 已声明，Forbidden Files / Shared Artifacts / Worker Return Format 省略处按默认继承。
  - stabilization: pass，无 `[stabilization-first]` task；manual gate 已用 `[~]` 加原因豁免。
  - workspace: pass，`Workspace Strategy=current-worktree` 已解析；schema/shared DTO isolated 触发已记录，当前 repo clean、专用分支、单 agent 串行，`.worktrees/` 未 ignore，因此 current-worktree 可接受。
  - profile: pass，`strict-change` 与持久化 schema/public DTO 风险匹配。
- warnings:
  - item: schema/shared DTO 理论上适合 isolated worktree
    why_non_blocking: 已创建专用分支且工作树 clean；`.worktrees/` 未 ignore，当前串行推进不会混入无关 dirty。若出现并行 worker 或无关 dirty，必须回 plan 改 workspace strategy。
- next: 进入 `opsx-implement`。

## Test Cycle Evidence

### 2026-06-29 19:22 +08:00

- task: Task 1.1 - 固化 legacy settings load/save schema 行为
- phase: red
- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "UserWorkspaceSettingsSchema"`
- exit_status: 1
- result: 合法红。新增测试引用 `UserWorkspaceSettings.CurrentSchemaVersion`、`SchemaVersion`、`LoadFromFile`、`SaveToFile`，当前产品代码尚未提供这些 schema API，因此编译失败。
- failure_summary:
  - `CS0117`: `UserWorkspaceSettings` does not contain `CurrentSchemaVersion`
  - `CS1061`: `UserWorkspaceSettings` does not contain `SchemaVersion`
  - `CS0117`: `UserWorkspaceSettings` does not contain `LoadFromFile`
  - `CS1061`: `UserWorkspaceSettings` does not contain `SaveToFile`
- coverage: R1/R2 red baseline，覆盖 new workspace current version、legacy no-version load、legacy collision field migration、save current schema。

### 2026-06-29 19:23 +08:00

- task: Task 1.1 - 固化 legacy settings load/save schema 行为
- phase: green
- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "UserWorkspaceSettingsSchema"`
- exit_status: 0
- result: 通过。`Passed: 3, Failed: 0, Skipped: 0, Total: 3`。
- passing_tests:
  - `UserWorkspaceSettingsSchemaTests.NewWorkspaceUsesCurrentSchemaVersion`
  - `UserWorkspaceSettingsSchemaTests.LoadFromFileUpgradesLegacyWorkspaceWithoutDroppingPreferences`
  - `UserWorkspaceSettingsSchemaTests.SaveToFileWritesCurrentSchemaVersionAndDropsLegacyCollisionField`
- changed_files:
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 新增 `CurrentSchemaVersion`、`SchemaVersion`、internal file load/save helpers，并在 normalize 中 stamp 当前版本。
  - `tests/DesktopLoudnessMeter.Tests/Settings/UserWorkspaceSettingsSchemaTests.cs` - 新增 legacy/current schema tests。
- acceptance_coverage:
  - 缺少 schema version 的 legacy JSON load 后规范化为当前 schema version: pass
  - legacy `CollisionAbsoluteThresholdDb` 有效值迁移到 `CollisionLevelFloorDb`，保存输出不保留旧字段值: pass
  - 保存当前 settings JSON 写出 schema version，并保留 theme、recent event、source alias/hidden/order、enum 和 detail layout 语义: pass
  - 无效 source key、空 alias、负 order index、越界 recent event limit 和非有限 loudness clock values 仍按现有规则规范化: pass
- coverage: N/A（本仓库当前 task 验证未采集覆盖率；补救：verify 阶段运行 focused/full regression 作为行为覆盖）。

### 2026-06-29 19:24 +08:00

- task: Task 2.1 - 暴露 Host/HostBridge settings schema version
- phase: red
- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "Host|WebView|HostBridge"`
- exit_status: 1
- result: 合法红。Host projection/HostBridge JSON tests 已要求 schema version，但 `HostSettingsSnapshot` 尚未暴露该属性。
- failure_summary:
  - `CS1061`: `HostSettingsSnapshot` does not contain a definition for `SchemaVersion`
- coverage: R3 red baseline，覆盖 host settings projection 和 HostBridge JSON `snapshot.settings.schemaVersion`。

### 2026-06-29 19:25 +08:00

- task: Task 2.1 - 暴露 Host/HostBridge settings schema version
- phase: green
- command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "Host|WebView|HostBridge"`
- exit_status: 0
- result: 通过。`Passed: 24, Failed: 0, Skipped: 0, Total: 24`。
- passing_tests:
  - `HostSnapshotTests.SettingsProjectionReadsWorkspaceSchemaVersion`
  - `HostBridgeDtoMapperTests.SnapshotResponseSerializesStableJsonEnvelope`
  - Host / WebView / HostBridge focused suite
- changed_files:
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs` - settings projection 增加 schema version，并通过 settings normalize path 投影 current version。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` - HostBridge settings DTO 增加 `SchemaVersion`。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` - mapper 传递 schema version。
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 增加 internal projection normalize helper，避免 host 复制 normalization。
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotTests.cs` - 更新 settings projection expectation。
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs` - 锁定 JSON `snapshot.settings.schemaVersion`。
- acceptance_coverage:
  - `HostSettingsSnapshot.FromWorkspace()` exposes current schema version: pass
  - HostBridge snapshot response JSON includes `snapshot.settings.schemaVersion`: pass
  - Existing host/bridge DTO behavior remains WPF-free and stable source-key based: pass
- coverage: N/A（未采集数字覆盖率；补救：verify 阶段运行 full .NET regression）。

### 2026-06-29 19:26 +08:00

- task: Task 3.1 - 同步 frontend settings DTO 和 mock snapshot
- phase: red
- commands:
  - `npm test` (workdir `src/DesktopLoudnessMeter.Web`) exit=0，结果：Vitest runtime tests 通过，未执行 full TypeScript build check。
  - `npm run build` (workdir `src/DesktopLoudnessMeter.Web`) exit=1，结果：合法红，`tsc -b` 捕获 `schemaVersion` 未在 `HostSettingsSnapshotDto` 中定义。
- failure_summary:
  - `TS2353`: `schemaVersion` does not exist in type `HostSettingsSnapshotDto`
  - `TS2339`: Property `schemaVersion` does not exist on type `HostSettingsSnapshotDto`
- coverage: R4 red baseline，覆盖 frontend bridge type 和 settings schema field usage。

### 2026-06-29 19:27 +08:00

- task: Task 3.1 - 同步 frontend settings DTO 和 mock snapshot
- phase: green
- commands:
  - `npm test` (workdir `src/DesktopLoudnessMeter.Web`) exit=0，结果：`Test Files 5 passed, Tests 11 passed`
  - `npm run build` (workdir `src/DesktopLoudnessMeter.Web`) exit=0，结果：`tsc -b && vite build` 通过
- changed_files:
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - `HostSettingsSnapshotDto` 增加 required `schemaVersion`。
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - mock settings payload 增加 schema version。
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.test.ts` - bridge client test 断言 schema version。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`、`src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`、`src/DesktopLoudnessMeter.Web/src/state/hostState.test.ts` - fixtures 同步 required settings field。
- acceptance_coverage:
  - `HostSettingsSnapshotDto` includes required `schemaVersion: number`: pass
  - Mock bridge snapshot provides `settings.schemaVersion`: pass
  - TypeScript tests/build fail if required settings schema field is missing: pass，red 阶段由 `npm run build` 捕获
- coverage: N/A（前端未采集数字覆盖率；补救：`npm test` 和 `npm run build` 覆盖 DTO/mock/type contract）。

### 2026-06-29 19:29 +08:00

- task: Task 4.1 - 跑通 workspace/WPF regression 和 boundary guard
- phase: characterization/green
- commands:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "MainViewModelAdvancedControls|DetailWindowViewModel|Host|WebView|Settings"` exit=0，结果：`Passed: 103, Failed: 0, Skipped: 0, Total: 103`
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0，结果：`Passed: 309, Failed: 0, Skipped: 5, Total: 314`
  - `rg -n "SchemaVersion|schemaVersion|CollisionAbsoluteThresholdDb|HostSettingsSnapshot" src tests docs openspec/changes/2026-06-29-settings-workspace-schema` exit=0，结果：命中集中在 settings schema、Host/HostBridge DTO、Web DTO/tests、既有 detail preset schema 与 legacy collision compatibility 路径。
- note:
  - 曾并行运行 focused/full `dotnet test`，focused build 因 MSBuild/WPF intermediate `uMeter.dll` 被另一进程占用而失败；随后串行重跑 focused regression 通过。该失败归因为并行 build contention，不是产品代码或测试行为失败。
- changed_files:
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - refinement：新增 `NormalizeSchemaVersion`，Host projection 不再 normalize/mutate entire workspace。
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs` - projection 使用 schema-version-only normalization。
- acceptance_coverage:
  - Source alias/hide/order/detail preference tests 继续通过: pass
  - Full .NET test suite 通过: pass
  - Boundary grep 未发现 native/protocol/startup/tray/source ingest scope creep: pass
- coverage: N/A（未采集数字覆盖率；补救：focused regression + full .NET suite + boundary grep）。

### 2026-06-29 19:31 +08:00

- task: Task 4.2 - 更新 docs 和 codemap
- phase: direct
- command: `rg -n "settings schema|workspace schema|SchemaVersion|schemaVersion|WebView" README.md docs/current-status.md .aiknowledge/codemap .aiknowledge/logs/2026-06.md`
- exit_status: 0
- result: 通过。docs/codemap/logs 命中 versioned workspace schema、HostBridge settings `schemaVersion`、WebView follow-up boundaries，并明确 Web settings editor/WPF retirement 仍为后续 change。
- skip_test_cycle_reason: 文档和知识库更新，无产品行为逻辑；使用 grep 和前序 automated tests 作为替代验证。
- changed_files:
  - `README.md` - 增加 persisted workspace schema / HostBridge settings DTO note。
  - `docs/current-status.md` - 增加 versioned settings schema 和 WebView architecture migration status。
  - `.aiknowledge/codemap/index.md` - 更新 source workspace / host contract summary。
  - `.aiknowledge/codemap/source-list-workspace.md` - 记录 `UserWorkspaceSettings` schema ownership。
  - `.aiknowledge/codemap/host-contract.md` - 记录 host projection 包含 schema version 但不 owns migration。
  - `.aiknowledge/codemap/webview-shell.md` - 记录 Web DTO required `schemaVersion`。
  - `.aiknowledge/logs/2026-06.md` - 追加本 change 知识沉淀。
- acceptance_coverage:
  - Docs state that workspace/settings schema is versioned for WebView UI consumption: pass
  - Codemap records `UserWorkspaceSettings` as schema owner and HostBridge as projection boundary: pass
  - No doc claims complete Web settings UI parity or WPF retirement: pass

## Stabilization Ledger

<!-- no stabilization-first tasks planned -->

## Implement Amendments

<!-- implementation amendments append here -->

## Verify

### 2026-06-29 19:34 +08:00

- stage: verify
- decision: pass
- branch: `codex/2026-06-29-settings-workspace-schema`
- head: `f9705f5c384f531f1344e470593ca29fd6cb0c37`
- change_root: `/e/gitlab/uMeter/openspec/changes/2026-06-29-settings-workspace-schema`
- diff_state:
  - staged: none
  - unstaged: implementation/docs/OpenSpec files belonging to this change
  - untracked:
    - `openspec/changes/2026-06-29-settings-workspace-schema/**`
    - `tests/DesktopLoudnessMeter.Tests/Settings/UserWorkspaceSettingsSchemaTests.cs`
  - unrelated_dirty: none observed
- verification_scope:
  - 当前工作树 diff + untracked current change files。
  - 运行命令覆盖 settings schema compatibility、HostBridge DTO、Web TS DTO/mock、WPF/source workspace regression、full .NET regression、frontend test/build、app build、docs/codemap scope。
- commands:
  - command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "Settings|Host|WebView|MainViewModelAdvancedControls|DetailWindowViewModel"`
    exit_status: 0
    result: `Passed: 103, Failed: 0, Skipped: 0, Total: 103`
  - command: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
    exit_status: 0
    result: `Passed: 309, Failed: 0, Skipped: 5, Total: 314`
  - command: `npm test`
    workdir: `src/DesktopLoudnessMeter.Web`
    exit_status: 0
    result: `Test Files 5 passed, Tests 11 passed`
  - command: `npm run build`
    workdir: `src/DesktopLoudnessMeter.Web`
    exit_status: 0
    result: `tsc -b && vite build` passed
  - command: `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
    exit_status: 0
    result: `Build succeeded. 0 Warning(s), 0 Error(s)`
  - command: `rg -n "SchemaVersion|schemaVersion|CollisionAbsoluteThresholdDb|HostSettingsSnapshot" src tests docs openspec/changes/2026-06-29-settings-workspace-schema`
    exit_status: 0
    result: scope grep 命中 settings schema、legacy collision compatibility、Host/HostBridge/Web DTO/tests、OpenSpec docs；未发现 native/protocol/startup/tray/source ingest scope creep。
  - command: `rg -n "settings schema|workspace schema|SchemaVersion|schemaVersion|WebView" README.md docs/current-status.md .aiknowledge/codemap .aiknowledge/logs/2026-06.md`
    exit_status: 0
    result: docs/codemap/logs 记录 schema boundary，并明确 Web settings editor/WPF retirement 仍为后续 change。
- spec_compliance:
  - R1: pass，`UserWorkspaceSettings` has current schema version, save writes `SchemaVersion`, tests cover new/default/save behavior。
  - R2: pass，legacy no-version JSON and `CollisionAbsoluteThresholdDb` migration covered by `UserWorkspaceSettingsSchemaTests`。
  - R3: pass，`HostSettingsSnapshot` and HostBridge JSON expose schema version; host projection remains WPF-free and source-key based。
  - R4: pass，TypeScript `HostSettingsSnapshotDto` requires `schemaVersion`; mock/test fixtures aligned and `npm run build` validates drift guard。
  - R5: pass，boundary greps and docs show no full settings UI, startup/tray/native/source ingest scope creep。
- manual_checks:
  - `Windows WebView runtime smoke for settings schema`: waived with reason in `Manual Checks`; this change is DTO/schema contract only and is covered by HostBridge JSON + TS tests/build.
- allowed_failures:
  - none
- next: 进入 `opsx-review`。

## Risk Review

### 2026-06-29 19:35 +08:00

- stage: review
- decision: pass
- scope:
  - base: `codex/2026-06-29-native-core-build-boundary` / `f9705f5c384f531f1344e470593ca29fd6cb0c37`
  - branch: `codex/2026-06-29-settings-workspace-schema`
  - committed_diff: none beyond base
  - unstaged_diff: current change implementation/docs/OpenSpec files
  - untracked_diff:
    - `openspec/changes/2026-06-29-settings-workspace-schema/**`
    - `tests/DesktopLoudnessMeter.Tests/Settings/UserWorkspaceSettingsSchemaTests.cs`
  - unrelated_dirty: none observed
- reviewed_risk_threads:
  - schema / serialization / persistence: `UserWorkspaceSettings` write/read path, legacy no-version input, legacy `CollisionAbsoluteThresholdDb`, save output, unknown-field compatibility.
  - API / DTO compatibility: `HostSettingsSnapshot`, HostBridge DTO mapper/envelope, frontend `HostSettingsSnapshotDto`, mock/test fixtures.
  - state mutation: Host settings projection now normalizes only schema version and does not mutate the full workspace.
  - scope creep: docs/boundary grep show no native/protocol/startup/tray/source ingest changes and no claim of Web settings UI parity.
- findings:
  - none
- allowed_failures: []
- notes:
  - Existing skipped full-suite tests are unrelated local loopback/probe tests already reported by xUnit as skipped; verify passed with no allowed failure needed.
  - LF/CRLF warnings in `git diff --stat` are line-ending notices only; no release risk observed.
- next: 进入 `opsx-archive`。

## Manual Checks

- [~] [manual] Windows WebView runtime smoke for settings schema | 原因：本 change 只改变 settings/DTO/schema contract，WebView2 runtime/manual smoke 已由 `2026-06-29-webview-windows-app-shell` 覆盖；本 change 用 HostBridge JSON tests 和 Web TypeScript tests/build 覆盖。

## Rejected Attempts

### Attempt 1

- hypothesis: 只在 TypeScript DTO 添加 `schemaVersion`。
- rejection_reason: 不能保证旧 `settings.json` 兼容或 C# HostBridge payload 真实提供字段。

### Attempt 2

- hypothesis: 只在 `UserWorkspaceSettings` 添加 version 字段，不碰 HostBridge/Web DTO。
- rejection_reason: WebView UI 的 consumption boundary 仍会漂移，不能形成 Web UI 可消费 schema 闭环。

## Archive

### 2026-06-29 19:36 +08:00

- stage: archive
- decision: pass
- archive_path: `openspec/changes/archive/2026-06-29-settings-workspace-schema`
- schema: `spec-driven`
- profile: `strict-change`
- specs_count: 1
- gates:
  - readiness: pass
  - verify: pass
  - review: pass
  - archive: pass
- manual_followups: []
- allowed_failures: []
- knowledge_status:
  - status: done
  - evidence_ref: Task 4.2 updated `README.md`, `docs/current-status.md`, `.aiknowledge/codemap/**`, and `.aiknowledge/logs/2026-06.md`; verify docs/codemap grep passed.
- final_summary:
  - Versioned workspace/settings schema landed in `UserWorkspaceSettings` with legacy no-version compatibility.
  - HostBridge settings DTOs project `schemaVersion` for Web UI consumption.
  - TypeScript `HostSettingsSnapshotDto` and mock/test fixtures require `schemaVersion`.
  - Full Web settings editor, WebView startup/packaging switch, chart parity, WPF retirement, and macOS readiness remain follow-up changes.
