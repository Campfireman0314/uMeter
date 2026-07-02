# Design: 2026-06-30-web-settings-workflow-parity

正文默认使用中文；路径、命令、配置键和状态枚举保留原文。

## 上下文

`2026-06-30-completion-audit` 将 Web settings/theme workflow 标为 high-priority gap。当前事实：

- `AppShell.tsx` 有 Settings 按钮，但没有交互 handler。
- 侧栏 settings 区域只读显示 `themeId` 和 `maxRecentEventEntries`。
- `HostCommand` / `DesktopMeterHostController` 已有 `SetThemeHostCommand`、`SetRecentEventLimitHostCommand`、`SetEventOutputFolderHostCommand`。
- `HostBridgeController` 尚未把这些全局 settings command 映射到 WebView JSON command。
- `HostSettingsSnapshotDto` 已包含 `themeId`、`maxRecentEventEntries`、`eventOutputFolder`、`showHiddenSources`、`schemaVersion`。

## 目标 / 非目标

**目标：**
- Web UI 提供可编辑 settings surface。
- HostBridge v1 暴露全局 settings commands。
- Web shell 根据 themeId 应用主题 token。
- 自动化测试覆盖 settings workflow。

**非目标：**
- 不实现平台 folder/file picker。
- 不实现默认 detail layout file UI。
- 不暴露 WPF `AppTheme` 类型到 HostBridge DTO。
- 不修改 native/protocol/macOS runner。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U1, U2]
- [R3] -> [U2]
- [R4] -> [U3]
- [R5] -> [U4]

## 实施单元

### [U1] React settings workflow
- 关联需求: [R1], [R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.ts`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
- 设计:
  - Settings button 切换 settings surface。
  - surface 使用 DOM form controls：select(theme)、number input(recent limit)、text input(event output folder)、checkbox(show hidden)。
  - 设置提交通过现有 `store.sendCommand`，命令成功后状态来自 post-command snapshot。
  - show-hidden top-bar toggle 和 settings surface checkbox 使用同一 command，避免状态分叉。

### [U2] HostBridge settings commands
- 关联需求: [R2], [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
- 设计:
  - `HostBridgeRequestPayload` / TS `HostCommandPayload` 增加 `themeId`, `maxRecentEventEntries`, `eventOutputFolder`。
  - `HostCommandName` 增加 `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`。
  - `HostBridgeController.ToHostCommand` 映射到现有 Host command。
  - 新增 settings command 均要求 request payload object 存在；payload object 内部字段按下列命令规则允许为空、清空或必填。
  - `setRecentEventLimit` 在 bridge 层要求 1..200 的整数。
  - `setTheme` 接受 `null`、空白或已知 Web theme id；unknown id 返回 invalid request，避免持久化不可呈现的 theme。
  - `setEventOutputFolder` 接受 string 或 null；空白归一为 null。

### [U3] Web theme tokens
- 关联需求: [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
- 设计:
  - Web 侧定义主题 id 集合：`midnight`, `milk-white`, `forest-green`, `pink-dream`。
  - shell 根元素设置 `data-theme=<id>`，unknown/null fallback 为 `midnight`。
  - CSS 用 custom properties 替换硬编码主色，避免切换 theme 时局部控件仍停留在旧 palette。

### [U4] Regression verification
- 关联需求: [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.test.ts`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
  - `openspec/changes/2026-06-30-web-settings-workflow-parity/evidence.md`
- 设计:
  - React test 覆盖 open settings、dispatch commands、post-command UI update、theme `data-theme`。
  - HostBridge controller test 覆盖 valid 和 invalid settings payload。
  - Build/test 命令写入 evidence。

## Command / DTO 设计

新增 command:

| Command | Payload | Validation | Host command |
|---------|---------|------------|--------------|
| `setTheme` | `{ themeId?: string | null }` | payload object required; null/blank/default allowed; known ids only | `SetThemeHostCommand` |
| `setRecentEventLimit` | `{ maxRecentEventEntries: number }` | integer, 1..200 | `SetRecentEventLimitHostCommand` |
| `setEventOutputFolder` | `{ eventOutputFolder?: string | null }` | payload object required; string/null; blank -> null | `SetEventOutputFolderHostCommand` |

已存在 command:

| Command | Note |
|---------|------|
| `setShowHiddenSources` | 继续复用；settings surface 和 top bar 同步使用 |

## 决策

1. **主题选项先在 Web 侧固定。**
   - 原因：Host snapshot 当前没有 theme option DTO；直接暴露 WPF `AppTheme` 会破坏 UI-neutral boundary。
   - 影响：Web 与 WPF 主题 id 需保持测试覆盖；后续可新增主题 palette DTO。

2. **event output folder 使用文本输入。**
   - 原因：平台 folder picker 属于 host glue，不应阻塞 Web settings parity MVP。
   - 影响：用户可配置/清空路径；选择器留给后续 change。

3. **默认 detail layout file 不进入本轮。**
   - 原因：该设置需要文件选择器和 preset validation 状态，复杂度独立。
   - 影响：snapshot 可继续只读投影，Web settings surface 可显示但不编辑，或暂不作为可编辑项。

## 风险 / 权衡

- Web theme token 与 WPF theme palette 不是同一对象；本 change 只保证 id 对齐和 Web shell 视觉切换。
- Recent limit 影响 `DesktopMeterHostController` 的 recent event trimming；Host controller 已有 clamp，bridge 层仍需提前拒绝明显非法 payload。
- Text input path 不能验证文件夹存在；这与现有 settings schema 一致，真实 folder picker 后续处理。

## 稳定化策略

- `[stabilization-first]` 不适用；所有目标行为可自动化验证。
- doc impact: `TASK_EVIDENCE_ONLY`，归档后更新 codemap。

## 回退方案

- 删除新增 HostBridge command 映射和 TS payload 字段。
- Web settings surface 回退为只读摘要。
- CSS theme tokens 可回退为现有固定颜色。

## 并行 / Subagent 边界

默认串行。前端和 .NET command mapping 有共享 DTO 语义，分裂并行容易造成 payload 字段不一致。

## 知识沉淀

归档后更新 `webview-shell` 和 `host-contract` codemap，记录 Web settings workflow 已经通过 HostBridge v1 command bus 落地，以及仍未覆盖的 default detail layout picker / platform folder picker。
