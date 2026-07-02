# Work Packet: 2026-06-30-web-settings-workflow-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## 元数据

- profile: lean-change
- source_type: feature
- status: drafting
- created: 2026-06-30

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：completion audit 发现的高优先级 gap 是 Web settings/theme workflow parity。它不是单个按钮修复，而是一条用户可感知闭环：Web UI 打开设置、编辑全局设置、通过 HostBridge v1 typed command bus 持久化、刷新 snapshot，并让 Web 自身体现主题状态。前端、TS DTO、WebView HostBridge 映射和测试需要一起落地，否则任一半边都无法独立交付。
- OpenSpec 下限检查：通过。该 change 涉及公共 HostBridge command vocabulary、JSON DTO payload、React UI、CSS theme tokens 和跨层回归测试，具备可评审、可验证、可归档闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Web global settings workflow | 用户可在 WebView 主 UI 内查看和修改主题、recent limit、event output folder 与 hidden-source visibility | `src/DesktopLoudnessMeter.Web`, HostBridge v1 | 高 | 直接修复 completion audit 的 Web settings gap |
| HostBridge settings command exposure | Web command bus 能发送 settings DTO 并获得 post-command snapshot | `src/DesktopLoudnessMeter.App/WebView`, `src/DesktopLoudnessMeter.App/Host` | 高 | Host controller 已有部分命令，需补 WebBridge 映射和验证 |
| Web theme application | Web UI 根据 `settings.themeId` 应用已知主题 token，避免只持久化不呈现 | CSS + React shell | 中 | 主题 token 可先覆盖 Web shell，WPF theme manager 不是本 change 目标 |

## 意图

让 WebView UI 不再只有“Settings”入口和只读摘要，而是拥有可运行、可测试的全局设置工作流。该 change 先覆盖 Host snapshot 已公开且 Host controller 已支持或可小幅补齐的设置，不把平台文件选择器、默认 detail layout picker 或更深的 source/workspace 管理塞进本轮。

## 范围

### 范围内

- Web UI 提供 settings panel/surface，Settings 按钮可打开/关闭。
- Web UI 可编辑：
  - `themeId`
  - `maxRecentEventEntries`
  - `eventOutputFolder`
  - `showHiddenSources`
- Web UI 根据 `themeId` 应用 Web theme token，至少覆盖 shell、surface、button、row、settings controls、error banner 和 chart container 的颜色变量。
- HostBridge v1 增加 settings command payload：
  - `setTheme`
  - `setRecentEventLimit`
  - `setEventOutputFolder`
- HostBridge settings payload 需要进行基础校验，invalid payload 返回 typed error 且不修改 snapshot。
- Mock bridge、TypeScript types、React tests、HostBridge .NET tests 和现有 Web/.NET regression 需要更新。

### 范围外

- 不实现平台 folder/file picker；`eventOutputFolder` 本轮使用文本输入和 clear 行为。
- 不实现 `defaultDetailLayoutFile` 选择器或 layout preset 验证 UI；该功能涉及文件 picker 与 detail layout validation，可作为后续 change。
- 不迁移 WPF theme manager 或 WPF global settings window。
- 不移动 settings schema ownership；`UserWorkspaceSettings` 仍拥有 schema、normalization 和 persistence。
- 不处理 native core、protocol ingest、host glue-only boundary 或 macOS WKWebView transport。

## 依赖

- `2026-06-30-completion-audit` 已归档，明确本 gap 为高优先级。
- `settings-workspace-schema` 已归档，`HostSettingsSnapshotDto` 带 `schemaVersion`。
- `host-snapshot-command-contract` 已归档，Host command vocabulary 已包含全局 settings command 类型。
- `webview-windows-app-shell` 和 `webview-chart-parity` 已归档，WebView shell 和 React/Vite test harness 可用。

## 成功标准

- Web settings panel 能通过自动化测试打开并编辑 theme、recent limit、event output folder、show hidden。
- HostBridge v1 支持 `setTheme`、`setRecentEventLimit`、`setEventOutputFolder`，并保持 commandResult + post-command snapshot 语义。
- Invalid settings payload 不会修改 workspace/snapshot，并返回 `invalidRequest`。
- Web UI 根据 snapshot theme 应用对应 `data-theme` 或等效 theme token；unknown/null theme 有稳定 fallback。
- `npm test`、`npm run build` 和相关 .NET tests 通过。

## 自我反驳 / 漏洞扫描固化

- 候选推荐：创建 `web-settings-workflow-parity`，一轮完成 Web UI + HostBridge settings command + Web theme application。
- 第 1 轮扫描：blocking=主题选项来源尚未由 Host snapshot 暴露；如果强行从 WPF `AppThemes` 动态读取会扩大 DTO 和 host responsibility。non-blocking=可在 Web 侧定义与现有 `AppThemes` id 对齐的有限主题选项，并在 plan 中要求 tests 固化 id fallback。
- 修订后候选推荐：本 change 只要求 Web UI 支持已知 theme ids：`midnight`, `milk-white`, `forest-green`, `pink-dream`，并允许 `null/system` fallback；不要求 Host snapshot 暴露完整 theme palette。
- 第 2 轮扫描：blocking=默认 detail layout file 和 event output folder 都可能需要平台 picker。处理方式：本 change 只实现 `eventOutputFolder` 文本输入/clear；默认 detail layout picker 明确 out-of-scope。
- 最终扫描结论：无新的关键事实性漏洞。剩余不确定性均已转为 scope/non-goals、需求或验证项。

## 方案摘要

在 HostBridge v1 payload 中补齐 settings command 字段和映射；在 React shell 中把 settings 摘要升级为可编辑 panel，使用受控输入和 `store.sendCommand` 触发设置命令；用 CSS custom properties + `data-theme` 实现 Web shell 主题切换。测试以“先刻画缺口再实现”为主，避免只靠快照渲染证明设置 workflow。

## 文件 / Ownership

### 允许修改

- `openspec/changes/2026-06-30-web-settings-workflow-parity/**`
- `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
- `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
- `src/DesktopLoudnessMeter.Web/src/**`
- `tests/DesktopLoudnessMeter.Tests/WebView/**`
- `tests/DesktopLoudnessMeter.Tests/Host/**`（仅在需要补 Host settings command coverage 时）

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`（除非 readiness 发现 HostBridge 无法在现有 schema 上完成）
- `src/DesktopLoudnessMeter.App/Themes/**`（本 change 不迁移 WPF theme palette）
- `docs/**`
- `.aiknowledge/**`（归档后由 knowledge follow-up 处理）
- 其他 `openspec/changes/**`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-30-web-settings-workflow-parity`
- Base Branch: `codex/2026-06-30-completion-audit`
- Merge Target: user-owned integration branch
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树 clean；创建后 dirty 仅包含 `openspec/changes/2026-06-30-web-settings-workflow-parity/**`，归属本 change。
  - 链式判断：本 change 基于 `2026-06-30-completion-audit` 的归档结论，`base_head=ff75f0535d9a6d3f1367f1b58055e0b26647c8ec`；属于链式推进。
  - isolated 触发：本 change 涉及 public HostBridge DTO 和 Web UI，但当前工作树 clean、单 agent 串行、无需并行依赖隔离；使用专用 branch 即可提供回滚边界。
  - 结论：`current-worktree`。

## 验证计划

- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"`
- 必要时运行完整 `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- `git diff --check`

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：否。目标行为可用 React Testing Library、TypeScript build 和 .NET HostBridge tests 自动化。
- 人工 / 平台覆盖：不需要人工 Windows smoke 作为归档 blocker；UI 交互可自动化覆盖。真实 WebView2 smoke 可作为 verify 记录的可选补充，但不是 hard gate。

## 回退 / 升级

- 回退方式：撤销本 change 对 Web UI、HostBridge DTO/mapper/controller 和 tests 的修改；settings schema 不变。
- 升级条件：如果实现发现 `themeId` 必须由 Host 动态提供完整 theme palette，或 default detail layout picker 必须纳入 parity，回到 plan 拆分后续 change。

## Bugfix Root Cause

none（feature / parity gap）。

## Rejected Attempts

- 不只给 Settings 按钮加 `onClick`：无法满足用户目标中的“Web 前端承担主题、设置和用户工作流”。
- 不把所有 WPF GlobalSettingsWindow 能力一次迁移：默认 detail layout file、folder picker、theme manager 等会把平台 glue 和 WPF-specific theme assets 混入本 change，难以一次验证。
- 不让 HostBridge 返回 WPF `AppTheme` 对象：会把 WPF theme 类型泄漏到 UI-neutral DTO 边界。

## Readiness Notes

- readiness 需要检查 R 编号在 specs/design/tasks 中一致。
- readiness 需要确认 tests 路径已覆盖 React UI、TS DTO、HostBridge .NET command mapping。
- readiness 需要确认 out-of-scope 不会让 settings workflow 变成半成品：至少 theme/recent limit/event output folder/show hidden 必须可编辑。
