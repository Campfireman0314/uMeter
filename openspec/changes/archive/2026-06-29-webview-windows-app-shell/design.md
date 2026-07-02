# Design: 2026-06-29-webview-windows-app-shell

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

已证实事实：
- 当前 app 是 `net8.0-windows` WPF app，`DesktopLoudnessMeter.App.csproj` 目前仅引用 `NAudio` 和 `DesktopLoudnessMeter.Protocol`，尚未引用 WebView2。
- `App.xaml.cs` owns startup、single-instance、tray、receiver、settings、`MainViewModel` 和 WPF main window creation。
- `DesktopMeterHostController`、`HostSnapshot`、`HostCommand`、`HostCommandResult` 已存在，并由 WPF adapter 消费。
- 当前仓库没有 `package.json`、`vite.config.*` 或 `pubspec.yaml`，因此 Web frontend 资产需要从零建立。
- WPF source/detail/settings 行为仍是 fallback 和 regression oracle。

推断：
- 初版 WebView shell 可以作为 WPF app 内的可打开窗口/入口，而不是替换 startup 主窗口。
- C# host DTO record 可以先通过 bridge mapper 转成 JSON DTO；长期 schema/types 稳定化留给 `settings-workspace-schema`。
- WebView2 runtime/package 和 static asset loading 需要 implementation 阶段验证。

## 目标 / 非目标

**目标：**
- 在 Windows app 中新增可运行 WebView2 shell。
- 定义并实现 HostBridge v1 初版 transport/envelope。
- 新增 React/Vite frontend shell，显示 host snapshot 并执行基础 commands。
- 保持 WPF fallback 和现有 regression。

**非目标：**
- 不把 WebView shell 设为 installer/startup/tray 唯一主窗口。
- 不做复杂图表 parity。
- 不做 settings schema migration。
- 不迁移 native core/source ingest/protocol/JUCE。
- 不实现 macOS runner。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U2, U3]
- [R5] -> [U4, U5]

## 实施单元

### [U1] Windows WebView host shell
- 关联需求: [R1, R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewAssetLocator.cs`
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml`（仅如需入口按钮/菜单）
- 设计:
  - 添加 WebView2 dependency 和 shell window。
  - Shell window 初始化 WebView2，加载 built static asset 或 dev URL；失败时显示 host-side error 状态。
  - WPF main window 继续默认启动；WebView shell 可通过明确入口打开。

### [U2] HostBridge v1 transport and command bus
- 关联需求: [R2, R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeJson.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `src/DesktopLoudnessMeter.App/Host/**`（仅非破坏性 helper）
  - `tests/DesktopLoudnessMeter.Tests/WebView/**`
- 设计:
  - envelope 包含 `id`、`type`、`command`/`payload`、`ok`、`error`、`snapshot` 等字段，具体命名 implementation 收敛但必须版本化为 v1。
  - WebView transport 使用 `WebMessageReceived` 接收 JSON request，使用 `PostWebMessageAsJson` 返回 response/event。
  - Bridge controller 调用 `DesktopMeterHostController.Refresh()` / `Execute(HostCommand)`，将 result 转成 DTO。
  - Unknown command、invalid payload 和 service failure 返回 typed error，不抛到 UI thread 顶层。

### [U3] React/Vite frontend shell
- 关联需求: [R3, R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/package.json`
  - `src/DesktopLoudnessMeter.Web/vite.config.*`
  - `src/DesktopLoudnessMeter.Web/tsconfig*.json`
  - `src/DesktopLoudnessMeter.Web/src/**`
  - `src/DesktopLoudnessMeter.Web/tests/**` 或 colocated `*.test.*`
- 设计:
  - 使用 TypeScript + React + Vite。
  - 定义 bridge client，支持 WebView host 注入对象和 test/mock bridge。
  - DOM components 渲染 source list、status、recent events、detail shell、settings/theme entry 和 command error。
  - 高频图表区域只保留 placeholder/readout，不启动 Canvas/WebGL parity。

### [U4] Regression and startup guard
- 关联需求: [R1, R5]
- 模块边界:
  - `tests/DesktopLoudnessMeter.Tests/Host/**`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/Infrastructure/**`
- 设计:
  - Existing WPF startup/single-instance/tray semantics remain unchanged except optional shell entry.
  - Add/retain regression tests for host bridge DTO mapping and command execution.
  - Manual smoke covers both WebView shell and WPF fallback.

### [U5] Build/test wiring and evidence
- 关联需求: [R5]
- 模块边界:
  - `desktop-loudness-meter.sln`（如需纳入 project）
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `src/DesktopLoudnessMeter.Web/package.json`
  - `openspec/changes/2026-06-29-webview-windows-app-shell/tasks.md`
  - `openspec/changes/2026-06-29-webview-windows-app-shell/evidence.md`
- 设计:
  - .NET tests 和 web build/test 都必须进入 evidence。
  - Web asset copy/build 策略需可重复；若 Node/npm 不可用，必须记录为 blocker 而不是跳过。
  - Manual checks 用四态字段关闭。

## HostBridge v1 草案

草案用于实现方向，最终命名可按代码风格微调：

- request:
  - `version: 1`
  - `id: string`
  - `kind: "snapshot" | "command"`
  - `command?: string`
  - `payload?: object`
- response:
  - `version: 1`
  - `id: string`
  - `ok: boolean`
  - `snapshot?: HostSnapshotDto`
  - `result?: HostCommandResultDto`
  - `error?: HostBridgeErrorDto`
- event:
  - `version: 1`
  - `kind: "snapshotChanged" | "hostError"`
  - `snapshot?: HostSnapshotDto`
  - `error?: HostBridgeErrorDto`

Initial command coverage:
- `refresh`
- `backToGlobal`
- `selectDetail`
- `setShowHiddenSources`
- `resetAll` or `resetSource` if implementation can safely expose without UX ambiguity
- Alias/hide/restore/move may be included if DTO mapping remains small; otherwise leave visible controls disabled and document as follow-up within shell parity.

## 决策

1. **WebView shell first, packaging later.**
   - 原因：startup/tray/installer 切换是独立风险；先证明 WebView UI + HostBridge 主路径。
   - 影响：WPF remains default/fallback in this change.

2. **Bridge DTO mapper lives in App WebView boundary.**
   - 原因：host contract exists as C# records; JSON schema stability belongs to later schema/type work.
   - 影响：avoid leaking WPF types and avoid changing host contract semantics.

3. **Frontend project sits under `src/DesktopLoudnessMeter.Web`.**
   - 原因：keeps web app source separate from WPF host while still inside repo/build ownership.
   - 影响：later packaging can decide asset bundling.

4. **Manual smoke is required.**
   - 原因：WebView2 runtime and native message channel need real Windows verification.
   - 影响：archive cannot pass with pending manual item.

## 风险 / 权衡

- WebView2 runtime not installed or NuGet unavailable: implementation must surface a clear blocker and evidence.
- Node/npm toolchain may be unavailable: web build/test cannot be silently skipped.
- JSON DTO drift from host records: bridge mapper tests must cover representative snapshots/commands.
- WPF startup side effects: do not move single-instance/receiver/tray ordering in this change.
- UI surface may be visually basic: acceptable for shell, but no claim of chart parity.

## 稳定化策略

- `[stabilization-first]` 适用单元：U1/U2/U3 integrated shell smoke。
- doc impact 处理：`TASK_EVIDENCE_ONLY` unless implementation discovers scope/schema changes.
- final hardening：bridge DTO unit tests, web build/test, .NET regression, Windows manual smoke.

## 回退方案

移除 WebView2 package/reference、`src/DesktopLoudnessMeter.App/WebView/**`、`src/DesktopLoudnessMeter.Web/**` 和 optional WPF entry wiring。WPF app remains current main UI; no user data migration is introduced.

## 并行 / Subagent 边界

默认串行实现。可并行只读调研 WebView2/Vite docs 或 WPF entry points；写入 `App.xaml.cs`、`.csproj`、bridge DTO、frontend package 和 shared OpenSpec artifacts 必须由 controller 串行合并。

## 知识沉淀

归档后应更新 `.aiknowledge/codemap/ui-runtime-architecture.md`，并新增或更新 WebView shell/HostBridge codemap；如发现 WebView2 runtime/package pitfall，应写入 `.aiknowledge/pitfalls/`。
