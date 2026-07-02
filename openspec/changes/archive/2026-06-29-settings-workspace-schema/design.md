# Design: 2026-06-29-settings-workspace-schema

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保留原文。

## 上下文

- `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/` 明确本 change 范围：定义 Web UI 可消费的 workspace/settings schema 与 TypeScript types，兼容旧 `UserWorkspaceSettings` 数据，并明确 HostBridge DTO 映射。
- `host-snapshot-command-contract` 已归档；其设计明确 settings snapshot 只 projection 当前 workflow 所需字段，不定义新 schema version。如果需要版本化，应拆到本 change。
- `webview-windows-app-shell` 已实现 Windows WebView2 shell、HostBridge v1 JSON envelope 和 React/Vite frontend；其设计明确 bridge DTO mapper 暂驻 App WebView boundary，长期 schema/types 稳定化留给本 change。
- 当前 `UserWorkspaceSettings` 没有 schema version，直接用 `System.Text.Json` 保存 `settings.json`，并使用 `Normalize()` 做字段裁剪和 legacy migration。
- 当前 Web frontend `hostBridge.ts` 手写 `HostSettingsSnapshotDto`，mock snapshot 也手写 settings payload。

## 目标 / 非目标

**目标：**
- 在 settings 层定义 current workspace schema version，并让 load/save/normalize 路径稳定处理 legacy no-version data。
- 保持旧 settings 文件兼容，尤其是 source-key 持久化、alias/hide/order、detail layout、loudness clock 和 collision settings。
- 将 schema version 投影到 host snapshot、HostBridge JSON DTO 和 TypeScript DTO/mock。
- 用 C# / TS tests 和 build 验证 schema compatibility 与 DTO alignment。

**非目标：**
- 不构建完整 Web settings UI。
- 不改变 WPF startup、tray、single-instance 或 installer 行为。
- 不迁移 source ingest 状态机、DSP/native core、JUCE sender 或 Protocol。
- 不做跨设备同步、profile import/export、多 workspace 管理。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U1]
- [R3] -> [U2]
- [R4] -> [U3]
- [R5] -> [U4]

## 实施单元

### [U1] Settings schema version and legacy normalization
- 关联需求: [R1], [R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
  - `tests/DesktopLoudnessMeter.Tests/Settings/UserWorkspaceSettingsSchemaTests.cs`
- 验证方式:
  - 添加 legacy JSON load tests，先证明缺少 schema version 和旧 collision field 的行为。
  - 添加 save JSON tests，证明 current schema version 写出并保持枚举/source/detail layout 字段。
- 知识沉淀:
  - settings schema version 属于 settings 层，不属于 HostBridge transport；HostBridge 只 projection。

### [U2] Host and HostBridge settings DTO projection
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs`
- 验证方式:
  - Host projection test asserts schema version.
  - HostBridge JSON test asserts `snapshot.settings.schemaVersion` camel-case field.
- 知识沉淀:
  - DTO drift guard 应覆盖 C# host projection 和 WebView JSON envelope 两层。

### [U3] Web TypeScript DTO and mock alignment
- 关联需求: [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.ts*`
- 验证方式:
  - `npm test`
  - `npm run build`
- 知识沉淀:
  - mock bridge 必须包含 required settings fields，否则非 WebView dev/test 会掩盖 DTO drift。

### [U4] Boundary docs and regression guard
- 关联需求: [R5]
- 模块边界:
  - `README.md`
  - `docs/current-status.md`
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/logs/2026-06.md`
  - `openspec/changes/2026-06-29-settings-workspace-schema/evidence.md`
- 验证方式:
  - Boundary grep confirms schema/native/runtime changes are scoped.
  - Full .NET regression confirms WPF fallback/workspace behavior still passes.
- 知识沉淀:
  - 归档后 codemap 要记录 schema ownership 和 HostBridge projection relationship。

## 决策

1. **Schema version owned by settings layer.**
   - 原因：持久化格式由 `UserWorkspaceSettings` 读写，legacy compatibility 也已经在 `Normalize()` 内。
   - 影响：HostBridge DTO 只暴露 `SchemaVersion` projection，不承担迁移逻辑。

2. **Current version starts as integer schema version.**
   - 原因：当前需求只需要区分 legacy no-version 与当前 schema，不需要复杂 migration graph。
   - 影响：若未来出现 destructive migration 或多版本转换，再拆 migration-specific change。

3. **Legacy no-version data remains valid input.**
   - 原因：现有用户 settings 文件都没有 schema version；本 change 不能要求用户重置设置。
   - 影响：load/normalize tests 必须从 raw JSON 输入证明兼容性。

4. **Bridge JSON keeps camel-case DTO convention.**
   - 原因：`HostBridgeJson` 已作为 WebView v1 JSON boundary，frontend DTO 采用 camel-case。
   - 影响：C# DTO record 字段使用 .NET naming，JSON tests 锁定 `schemaVersion`。

5. **No full settings UI in this change.**
   - 原因：schema/type compatibility 是独立交付闭环；settings editor workflow 会引入 UX、commands、validation UI 和 parity scope。
   - 影响：Web UI 只同步 DTO/mock/types，不新增 settings editor surfaces。

## 风险 / 权衡

- 风险：添加 `SchemaVersion` 后旧测试 `SettingsProjectionReadsExistingWorkspaceWithoutCreatingSchemaVersion` 会失效。
  - 处理：该测试是上一个 change 的边界保护，当前 change 正式接管 schema version；更新为 asserting schema version projection。
- 风险：`System.Text.Json` 默认 Pascal-case settings file 与 HostBridge camel-case JSON 混淆。
  - 处理：settings file schema 与 HostBridge DTO schema分别测试；不强制把 settings file 改为 camel-case。
- 风险：新增 version 字段看似简单，但如果顺手加入 settings UI command，会扩大 scope。
  - 处理：R5、tasks Allowed Files 和 boundary grep 约束。
- 风险：多版本迁移器可能现在就过度设计。
  - 处理：当前只处理 legacy no-version -> current version；未来版本升级再拆 change。

## 稳定化策略

- `[stabilization-first]` 适用单元：无。
- doc impact 处理：docs/codemap changes 是 scope 内直接产物；implementation evidence 记录为 `TASK_EVIDENCE_ONLY`，若实现发现 scope 变化再升级为 `DESIGN_AMENDMENT` 或 `SPEC_AMENDMENT`。
- final hardening：完整 .NET regression、Web npm tests/build、App build、boundary grep、codemap/docs update。

## 回退方案

移除 `SchemaVersion` 字段、HostBridge/TS DTO additions、新增 tests/docs，即可回到旧 settings format。由于 `System.Text.Json` 默认忽略未知字段，旧版本理论上应能读取带 `SchemaVersion` 的 JSON；implementation verify 必须用测试或现有 serializer 行为确认这一点，若不成立则补兼容策略。

## 并行 / Subagent 边界

- 默认串行：是。schema/DTO/types 是共享公共面，由 controller 串行实施。
- 可并行 lane：只读调查可并行；写入 `UserWorkspaceSettings`、HostBridge DTO、TypeScript DTO 和 OpenSpec artifacts 不并行。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后应更新 `.aiknowledge/codemap/source-list-workspace.md`、`host-contract.md`、`webview-shell.md` 或新增 settings schema codemap，记录：

- `UserWorkspaceSettings` owns schema version and migration/normalization。
- HostBridge exposes settings schema as projection for Web UI, not as migration owner。
- TypeScript DTO/mock must include required settings fields to prevent drift。
