# Work Packet: 2026-06-29-settings-workspace-schema

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保留原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付闭环是“为 WebView UI 固化稳定 workspace/settings schema，并保持旧 `settings.json` 可读取”。它需要同时覆盖 C# 持久化模型、HostBridge settings DTO、TypeScript DTO/types 和兼容性测试；这些部分共享同一组 schema/version/compatibility 决策，拆开会让 Web UI 仍缺少完整消费边界。
- OpenSpec 下限检查：通过。虽然核心代码量可能不大，但它触及用户持久化数据、公共 DTO/schema 和 Web UI consumption contract，属于高风险公共接口/迁移面，值得独立 readiness/verify/review/archive。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Versioned workspace schema | 用户旧设置继续可用，后续 Web UI 有明确 schema 版本 | `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` | 高 | 本 change 的核心闭环 |
| HostBridge/Web DTO alignment | WebView UI 可消费 settings/workspace DTO，不依赖 WPF ViewModel | `src/DesktopLoudnessMeter.App/Host`, `src/DesktopLoudnessMeter.App/WebView`, `src/DesktopLoudnessMeter.Web/src/bridge` | 高 | 必须和 schema 同步落地 |
| Regression/documentation | WPF fallback 行为不回退，后续 source ingest/settings UI change 有依据 | tests, docs, `.aiknowledge` archive follow-up | 中 | 验证和归档沉淀 |

### 推荐切分 / Top-Level Changes

1. `settings-workspace-schema`
   - 范围：定义 versioned workspace/settings schema，兼容旧 `UserWorkspaceSettings` 数据，固化 HostBridge settings DTO 与 TypeScript types。
   - 不包含：完整 Web settings UI、跨设备同步、WPF retirement、native source ingest、packaging/startup/tray。
   - 依赖：`host-snapshot-command-contract`、`ui-runtime-architecture-replan`、`webview-windows-app-shell`、当前 `UserWorkspaceSettings` 行为。
   - 建议 change 名：`2026-06-29-settings-workspace-schema`
   - work 关注点：source-key 持久化、schema version、legacy no-version data、legacy collision threshold migration、detail layout/settings DTO、Web UI type drift prevention。

### 执行顺序

- changes：`2026-06-29-webview-windows-app-shell -> 2026-06-29-native-core-build-boundary -> 2026-06-29-settings-workspace-schema -> 2026-06-29-native-source-ingest-core`
- 依赖：本 change 基于已归档 WebView shell 和 native core boundary；后续 source ingest core 可消费稳定 host snapshot/schema。
- 现在应该进入：`opsx-readiness 2026-06-29-settings-workspace-schema`

## 意图

`ui-runtime-architecture-replan` 已把主 UI runtime 修订为 WebView，并明确 `settings-workspace-schema` 应定义 Web UI 可消费的 workspace/settings schema 与 TypeScript types。当前代码事实是：

- `UserWorkspaceSettings` 直接序列化到 `%APPDATA%\uMeter\settings.json`，目前没有显式 schema version。
- `SourceWorkspaceSettings.Normalize()` 已包含旧字段兼容逻辑，例如 `CollisionAbsoluteThresholdDb` 迁移到 `CollisionLevelFloorDb`。
- `HostSettingsSnapshot` 只是读取现有 workspace 字段的 projection；归档 host contract 明确“不做 schema 迁移，若需要版本化则拆到 settings-workspace-schema”。
- WebView shell 已通过 HostBridge v1 暴露 settings DTO，Web frontend 已有手写 TypeScript DTO，但缺少 schema/version contract 和 drift guard。

本 change 目标是在不重写完整设置 UI、不退休 WPF 的前提下，把 settings/workspace schema 变成后续 Web UI 可依赖的稳定边界。

## 范围

### 范围内

- 为 `UserWorkspaceSettings` 增加显式 schema version/current version 边界，并在 load/save/normalize 路径中稳定处理旧文件。
- 保持旧 `settings.json` 可读取：缺少 schema version 的文件视为 legacy 当前可迁移输入，不丢失 theme、recent event、event output folder、source alias/hidden/order、analysis settings、detail module layout、loudness clock 和 collision settings。
- 明确并测试 legacy `CollisionAbsoluteThresholdDb` 到 `CollisionLevelFloorDb` 的兼容行为。
- 扩展 `HostSettingsSnapshot` / HostBridge settings DTO / TypeScript DTO，使 Web UI 能看到 schema version 与稳定 source workspace fields。
- 增加 C# 和 TypeScript contract/regression tests，覆盖 legacy read、normalized save、HostBridge JSON shape、TS type/mock alignment。
- 更新当前状态文档和必要 `.aiknowledge/codemap`，归档时沉淀 settings schema 入口。

### 范围外

- 不实现完整 Web settings editor 或 settings workflow parity。
- 不迁移 WPF UI 到 WebView，不修改 startup/tray/installer 主窗口策略。
- 不改变 source ingest 状态机、DSP/native core、JUCE sender、Protocol lifecycle vocabulary。
- 不做跨设备同步、云配置、profile import/export 或多 workspace 管理。
- 不改变 detail chart rendering parity；图表 Canvas/WebGL 仍属于 `webview-chart-parity`。

## 成功标准

- `UserWorkspaceSettings.Load()` 能读取没有 schema version 的 legacy JSON，并规范化为当前 schema，不丢失现有字段语义。
- `UserWorkspaceSettings.Save()` 写出的 JSON 带有当前 schema version，且枚举、source-key dictionary、detail layout 字段仍与现有兼容。
- HostBridge snapshot JSON 的 `settings` payload 暴露 schema version，并保持 camel-case DTO 与 TypeScript types 对齐。
- WPF fallback 现有 workspace 行为测试继续通过：source alias/hide/order/detail preferences 不回退。
- README/current status 或等价文档说明 settings schema version 与后续 Web UI consumption boundary。

## 需求 / 验收摘要

- [R1] 系统必须提供 versioned workspace/settings schema，并在保存时写出当前版本。
- [R2] 系统必须兼容旧 `settings.json`，包括缺少 schema version 和旧 collision threshold 字段。
- [R3] HostBridge v1 settings DTO 必须把 schema version 与稳定 workspace fields 暴露给 Web UI。
- [R4] Web TypeScript DTO/mock 必须与 HostBridge settings JSON contract 对齐，并通过测试防止漂移。
- [R5] 本 change 必须保持边界，不实现完整 settings UI、不改变 WPF fallback/startup/native/source ingest。

## 方案摘要

在 settings 层引入 current schema version 常量和 `SchemaVersion` 字段，由 `UserWorkspaceSettings.Normalize()` 统一完成默认值、范围裁剪、legacy field migration 和 current-version stamping。Host settings projection 增加 `SchemaVersion`，WebView DTO mapper 和 frontend `hostBridge.ts` 同步暴露。测试优先锁定 legacy JSON 输入、保存输出 JSON、HostBridge camel-case payload 与 TypeScript mock/consumer compile-time shape。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - schema version、normalization、legacy compatibility。
- `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs` - settings projection 增加 schema version。
- `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` - HostBridge DTO 增加 settings schema field。
- `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` - DTO mapper 同步 schema version。
- `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - TypeScript DTO 同步。
- `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - mock snapshot 同步。
- `src/DesktopLoudnessMeter.Web/src/**/*.test.ts*` - Web contract/mock tests。
- `tests/DesktopLoudnessMeter.Tests/Host/**` - host projection/schema tests。
- `tests/DesktopLoudnessMeter.Tests/Settings/**` - settings schema/legacy tests（可新增）。
- `tests/DesktopLoudnessMeter.Tests/WebView/**` - HostBridge JSON DTO tests。
- `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - WPF regression tests 仅限必要修正。
- `README.md`, `docs/current-status.md` - 状态/架构文档。
- `.aiknowledge/codemap/**`, `.aiknowledge/logs/**`, `.aiknowledge/pitfalls/**` - 归档后知识维护。
- `openspec/changes/2026-06-29-settings-workspace-schema/**` - 当前 change 产物。

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.Setup/**`
- WPF XAML/startup/tray 代码，除非 verify 发现现有 regression test 需适配 schema version 字段。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-settings-workspace-schema`
- Base Branch: `codex/2026-06-29-native-core-build-boundary`
- Merge Target: `codex/2026-06-29-native-core-build-boundary`
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-29-settings-workspace-schema/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：当前 change 基于上一已归档 change 分支 `codex/2026-06-29-native-core-build-boundary`，`base_head=f9705f5c384f531f1344e470593ca29fd6cb0c37`；这是架构推进链式 change。
  - isolated 触发：本 change 触及持久化 schema 和 shared DTO，理论上命中 isolated 触发；但当前 repo clean、单 agent 串行、已创建专用分支，`.worktrees/` 未被 ignore，不适合静默创建仓库内 worktree。采用 current-worktree，若出现无关 dirty、并行 worker 或依赖隔离需求，回到 plan 改为仓库外 isolated worktree。
  - 结论：`current-worktree`，专用分支串行推进，path-limited staging。

## 验证计划

- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Settings|Host|WebView|MainViewModelAdvancedControls|DetailWindowViewModel"`，预期：settings compatibility、host/bridge DTO 和 WPF workspace regression 通过。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：完整 .NET regression 通过。
- `npm test`（workdir `src/DesktopLoudnessMeter.Web`），预期：HostBridge TS client/mock tests 通过。
- `npm run build`（workdir `src/DesktopLoudnessMeter.Web`），预期：TypeScript build 通过。
- `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`，预期：App build 通过并包含 Web frontend build/copy 仍可运行。
- Boundary grep：`rg -n "SchemaVersion|schemaVersion|CollisionAbsoluteThresholdDb|HostSettingsSnapshot" src tests docs openspec/changes/2026-06-29-settings-workspace-schema`，预期：schema/version/migration 命中在允许边界内。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`: no
- 不确定性来源：none；本 change 的目标行为可用 unit/contract/build tests 覆盖。真实 WebView2 manual smoke 已由 `webview-windows-app-shell` 覆盖，完整 settings UI parity 不在本 scope。
- 人工 / 平台覆盖：不需要新增 manual gate；如 implementation 意外触及 WebView shell runtime 或 WPF startup，则必须回 plan 增加 stabilization/manual matrix。
- 最终硬化策略：C# legacy JSON tests + HostBridge JSON contract tests + TS tests/build + WPF regression + docs/codemap update。

## 回退 / 升级

- 回退方式：移除 schema version 字段、HostBridge/TS DTO additions 和新增 tests/docs；旧 settings format 没有 destructive migration，回退不需要用户数据转换。但若已发布过写出 `SchemaVersion` 的版本，旧版本读取应忽略未知字段，需在 verify 中确认。
- 升级条件：如果实现发现需要改变 source-key identity、detail layout semantics、settings UI workflow 或 startup behavior，停止实施并回到 `opsx-plan` 拆分后续 change。
- 升级条件：如果旧数据兼容需要多版本迁移器、备份/restore 或 destructive migration，升级为 migration-specific design amendment 并重新 readiness。

## Bugfix Root Cause

- source_type=feature；none。

## Readiness Notes

- 探索自我反驳摘要：
  - 候选推荐：创建 `settings-workspace-schema`，以 strict-change 管理持久化 schema、HostBridge DTO 和 TS types。
  - 第 1 轮扫描：blocking=无。non-blocking=完整 settings UI 可能诱发 scope creep；处理方式是在 scope/non-goals/tasks 中明确只做 schema/type/compatibility。
  - 未验证前提：实现细节中 `SchemaVersion` 字段命名和旧 JSON 默认值如何落地需由 tests-first 确认；不改变 change/profile。
  - 最可能错处：把 settings snapshot projection 当成完整 settings editor API；已在 R5 和范围外约束。
  - 失败路径 / 边界：若 schema version 引入破坏旧 WPF tests 或旧文件保存语义，必须用 legacy JSON regression 修正，不得靠手工观察。
  - 更小替代：只在 TypeScript 添加字段太小且不能保证旧数据兼容；只在 C# 添加字段会让 Web UI contract 继续漂移，因此不采用。
  - 证据 vs 推断：代码和归档 OpenSpec 证实当前无 schema version、HostBridge 已有 settings DTO、schema migration 属于本 change；推断是具体字段名采用 `SchemaVersion` / `schemaVersion`，由 implementation tests 固化。
  - 最终扫描结论：无新的关键事实性漏洞，进入 `opsx-readiness`。
