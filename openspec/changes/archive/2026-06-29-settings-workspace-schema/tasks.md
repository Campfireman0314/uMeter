# Tasks: 2026-06-29-settings-workspace-schema

## Default Constraints

**Forbidden Files**（默认）：
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**（默认）：
- `.openspec.yaml` — controller 串行更新
- `tasks.md` — controller 串行更新
- `evidence.md` — controller 串行更新

**Worker Return Format**（默认）：`DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Settings Schema Compatibility

- [x] 1.1 [test-first] 固化 legacy settings load/save schema 行为

  **需求追踪**：[R1], [R2] -> [U1]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` — schema version、normalize/load/save support
  - `tests/DesktopLoudnessMeter.Tests/Settings/UserWorkspaceSettingsSchemaTests.cs` — legacy/current schema tests

  **验收标准**：
  - [x] 缺少 schema version 的 legacy JSON load 后规范化为当前 schema version。
  - [x] legacy `CollisionAbsoluteThresholdDb` 有效值迁移到 `CollisionLevelFloorDb`，保存输出不保留旧字段值。
  - [x] 保存当前 settings JSON 写出 schema version，并保留 theme、recent event、source alias/hidden/order、enum 和 detail layout 语义。
  - [x] 无效 source key、空 alias、负 order index、越界 recent event limit 和非有限 loudness clock values 仍按现有规则规范化。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "UserWorkspaceSettingsSchema"`，预期：新增 schema tests 通过。

  **Evidence Required**：
  - red 阶段：新增 tests 在实现前失败或 compile fail。
  - green 阶段：上述 filter test 通过，记录 exit code 和摘要。

  **Done When**：
  - Settings schema tests 覆盖 legacy no-version、current save 和 normalization edge cases，并通过。

  **依赖**：无

## 2. HostBridge Contract Alignment

- [x] 2.1 [test-first] 暴露 Host/HostBridge settings schema version

  **需求追踪**：[R3] -> [U2]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs` — host settings projection
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` — JSON DTO record
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` — DTO mapping
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotTests.cs` — projection tests
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs` — JSON contract tests

  **验收标准**：
  - [x] `HostSettingsSnapshot.FromWorkspace()` exposes current schema version。
  - [x] HostBridge snapshot response JSON includes `snapshot.settings.schemaVersion`。
  - [x] Existing host/bridge DTO behavior remains WPF-free and stable source-key based。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|WebView|HostBridge"`，预期：host/bridge tests 通过。

  **Evidence Required**：
  - red 阶段：projection/JSON tests 在实现前失败或 compile fail。
  - green 阶段：host/bridge filter test 通过。

  **Done When**：
  - C# host snapshot 和 HostBridge JSON contract 同步 schema version，并通过 tests。

  **依赖**：Task 1.1

## 3. Web TypeScript DTO Alignment

- [x] 3.1 [test-first] 同步 frontend settings DTO 和 mock snapshot

  **需求追踪**：[R4] -> [U3]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` — TypeScript DTO
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` — mock settings payload
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.ts*` — bridge/mock DTO tests

  **验收标准**：
  - [x] `HostSettingsSnapshotDto` includes required `schemaVersion: number`。
  - [x] Mock bridge snapshot provides `settings.schemaVersion`。
  - [x] TypeScript tests/build fail if required settings schema field is missing。

  **验证命令 / 方法**：
  - `npm test`（workdir `src/DesktopLoudnessMeter.Web`），预期：前端 tests 通过。
  - `npm run build`（workdir `src/DesktopLoudnessMeter.Web`），预期：TypeScript build 通过。

  **Evidence Required**：
  - red 阶段：新增/调整 test 或 build 在 DTO 未同步时失败。
  - green 阶段：`npm test` 和 `npm run build` 通过。

  **Done When**：
  - Web DTO/mock 与 HostBridge settings schema version 对齐，并通过 test/build。

  **依赖**：Task 2.1

## 4. Regression, Docs, And Knowledge

- [x] 4.1 [characterization-first] 跑通 workspace/WPF regression 和 boundary guard

  **需求追踪**：[R5] -> [U4]
  **执行方式**：[characterization-first]
  **Allowed Files**：
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**` — 仅限必要 regression 适配
  - `tests/DesktopLoudnessMeter.Tests/Host/**` — 仅限 schema version expectation 适配
  - `tests/DesktopLoudnessMeter.Tests/WebView/**` — 仅限 schema version expectation 适配

  **验收标准**：
  - [x] Source alias/hide/order/detail preference tests 继续通过。
  - [x] Full .NET test suite 通过，或 allowed failure 有明确非阻断原因。
  - [x] Boundary grep 未发现 native/protocol/startup/tray/source ingest scope creep。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "MainViewModelAdvancedControls|DetailWindowViewModel|Host|WebView|Settings"`，预期：focused regression 通过。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：完整 regression 通过。
  - `rg -n "SchemaVersion|schemaVersion|CollisionAbsoluteThresholdDb|HostSettingsSnapshot" src tests docs openspec/changes/2026-06-29-settings-workspace-schema`，预期：命中集中在允许边界。

  **Evidence Required**：
  - characterization 阶段：当前 focused tests/boundary grep baseline。
  - green 阶段：focused regression、full regression、boundary grep 结果。

  **Done When**：
  - WPF fallback/workspace regression 未回退，scope guard 通过。

  **依赖**：Task 1.1, Task 2.1

- [x] 4.2 [direct] 更新 docs 和 codemap

  **需求追踪**：[R5] -> [U4]
  **执行方式**：[direct]
  **Allowed Files**：
  - `README.md` — current architecture/status note
  - `docs/current-status.md` — settings schema status
  - `.aiknowledge/codemap/index.md` — codemap entry update if needed
  - `.aiknowledge/codemap/source-list-workspace.md` — settings schema ownership
  - `.aiknowledge/codemap/host-contract.md` — settings projection/schema relationship
  - `.aiknowledge/codemap/webview-shell.md` — TypeScript DTO/HostBridge settings contract
  - `.aiknowledge/logs/2026-06.md` — change log

  **验收标准**：
  - [x] Docs state that workspace/settings schema is versioned for WebView UI consumption.
  - [x] Codemap records `UserWorkspaceSettings` as schema owner and HostBridge as projection boundary.
  - [x] No doc claims complete Web settings UI parity or WPF retirement.

  **验证命令 / 方法**：
  - `rg -n "settings schema|workspace schema|SchemaVersion|schemaVersion|WebView" README.md docs/current-status.md .aiknowledge/codemap .aiknowledge/logs/2026-06.md`，预期：docs/codemap contain accurate references。

  **Evidence Required**：
  - direct 阶段：docs/codemap grep 摘要。

  **Done When**：
  - Docs and codemap reflect the implemented schema boundary without overstating goal completion。

  **依赖**：Task 1.1, Task 2.1, Task 3.1, Task 4.1
