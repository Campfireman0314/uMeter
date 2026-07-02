# Work Packet: 2026-06-30-webview-chart-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-30

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的用户可感知闭环是“WebView detail 视图从占位 readout 升级为可操作、可验证的实时图表视图”。它需要同时补齐 Host detail module/chart DTO、HostBridge chart command bus、React detail 工作流、Canvas 渲染和 Windows WebView 视觉 smoke；如果只做某一层，会留下无法独立验收的半成品。
- OpenSpec 下限检查：通过。该 change 涉及公共 DTO、命令、前端渲染、设置状态和人工视觉验证，具备可评审、可验证、可归档的模块闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Host chart contract | Web UI 能消费 detail 模块、图表设置和 collision 数据，不依赖 WPF ViewModel | `src/DesktopLoudnessMeter.App/Host`, `src/DesktopLoudnessMeter.App/WebView` | 高 | 是所有 Web 图表的边界 |
| Web chart renderers | 用户在 WebView detail 中看到 waveform/spectrum/stereo/loudness/collision 图表 | `src/DesktopLoudnessMeter.Web/src` | 中 | 依赖 Host DTO shape |
| Chart controls and persistence | 用户能调整模块可见性、模式和图表阈值，结果经 HostBridge 返回 snapshot | Host command + Web controls | 中 | 不做新 schema migration |
| Windows visual parity smoke | 确认 WebView 主窗口中的图表非空、无遮挡、WPF fallback 未被破坏 | Windows app + Web dev/build | 高 | 需要人工/截图 evidence |

## 自我反驳 / 漏洞扫描

- 候选推荐：创建单个 `2026-06-30-webview-chart-parity` strict change，覆盖 Host chart contract、HostBridge commands、React Canvas renderers 和 Windows visual smoke。
- 第 1 轮扫描：blocking=full parity 可能过大，尤其 loudness clock retained history 和 frequency collision 的状态归属尚未在 HostBridge 中落地；non-blocking=Web 侧已有 waveform/spectrum/stereo 数组，基础 Canvas 可直接消费。
- 修订后的候选推荐：保持一个顶层 change，但把边界收紧为“Host 投影数据 + Web 前端短期显示状态/Canvas 渲染”。loudness clock 的历史由 Web 侧按 selected source 和 `plotFrameVersion` 管理；collision bands 由 Host 从 host-neutral source spectra 投影，不把 WPF `DetailWindowViewModel` 作为跨平台 API。
- 第 2 轮扫描：blocking=没有新关键事实阻止进入 plan；remaining uncertainty=collision 算法复用方式和 WebView 视觉表现需在 implementation/stabilization 中验证。处理方式：写入 design 风险、tasks 的 test-first/stabilization-first 和 manual gate。
- 证据 vs 推断：已证实的是 Host DTO 当前已有 waveform/spectrum/stereo arrays，但 `DesktopMeterHostController` detail 仍返回空 modules 且 visibility 大多为 false；Web `AppShell` 当前只有三个 placeholder；WPF 控件和 ViewModel 已有 waveform/spectrum/stereo/loudness/collision 逻辑。推断的是 Canvas 渲染可用现有数组达到第一阶段 parity，需通过实现和视觉 smoke 验证。
- 最终扫描结论：无新的关键事实性漏洞；进入 `opsx-plan`，profile=`strict-change`。

## 意图

已归档的 WebView shell change 让 WebView 成为 Windows 主 UI 路径，但 detail 区域仍是静态 readout/placeholder。当前用户看到 source list、status、recent events 和基础 detail，但无法在 Web UI 中完成 WPF detail 图表承担的核心工作流：观察 waveform、spectrum、stereo image、loudness clock 和 frequency collision。

本 change 的目标是让 WebView detail shell 消费 UI-neutral HostBridge contract，渲染真实图表并提供最小可用的图表设置/模块控制。WPF 在本 change 中继续作为 fallback 和 regression oracle，不退休。

## 范围

### 范围内

- 扩展 Host snapshot/DTO，让 selected detail 暴露稳定的 detail module 列表、可见性、图表设置和 chart payload。
- 扩展 HostBridge v1 typed command bus，让 Web UI 可以更新 chart display mode、module visibility、spectrum range、loudness clock range/cycle 和 collision 设置。
- 为 frequency collision 提供 host-neutral projection，Web UI 消费 DTO，不依赖 WPF `DetailWindowViewModel` 或 WPF controls。
- 在 `src/DesktopLoudnessMeter.Web` 中新增 Canvas-based waveform、spectrum、stereo image、loudness clock 和 frequency collision renderer/component。
- 将 `AppShell` detail placeholder 替换为真实图表模块和紧凑 controls。
- 为 Host/WebView DTO、command mapping、Web chart utilities/components 添加自动化测试。
- 执行 Windows WebView 视觉/manual smoke，确认图表非空、布局无重叠、WPF fallback 可用。

### 范围外

- 不修改 `src/DesktopLoudnessMeter.Native/**` 的 C++ DSP/core build。
- 不修改 JUCE sender、SenderKit 或 Protocol lifecycle vocabulary。
- 不迁移 source ingest 状态机。
- 不退休或隔离 WPF UI。
- 不做新的 workspace schema 版本迁移；只消费并持久化当前 `UserWorkspaceSettings` 已存在字段。若发现必须新增持久化 schema，回到 plan 并拆到 settings change。
- 不实现 macOS WKWebView runner。
- 不改变 installer/publish 主流程，除非仅为验证 WebView asset build 所需的非破坏性补充。

## 依赖

- `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/`
- `openspec/changes/archive/2026-06-29-host-snapshot-command-contract/`
- `openspec/changes/archive/2026-06-29-webview-windows-app-shell/`
- `openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/host-contract.md`
- `.aiknowledge/codemap/audio-analysis.md`
- `.aiknowledge/codemap/analysis-rendering.md`

## 成功标准

- WebView selected detail 中不再出现 chart placeholder；可见模块渲染真实 chart canvas 或明确 empty state。
- HostBridge DTO 不暴露 WPF/XAML-only 类型。
- HostBridge commands 能 round-trip 图表相关设置，并返回更新后的 snapshot/result。
- Waveform、spectrum、stereo image、loudness clock 和 frequency collision 都有自动化覆盖或明确的 manual/visual evidence。
- 图表组件在无数据、单声道/双声道、隐藏模块、source 切换和 `plotFrameVersion` 变化时行为稳定。
- Windows WebView manual smoke 通过：图表非空、布局无明显重叠、settings/controls 可用、WPF fallback 仍可打开。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 和 `npm test` / `npm run build` 在对应项目下通过，或 evidence 中记录非本 change 导致的允许失败。

## 需求 / 验收摘要

- [R1] Host detail snapshot 必须暴露 Web UI 可消费的 chart module projection。
- [R2] HostBridge command bus 必须支持 chart settings 和 module visibility 的 typed update。
- [R3] Host 必须为 Web UI 投影 frequency collision DTO。
- [R4] Web UI 必须渲染 waveform chart。
- [R5] Web UI 必须渲染 spectrum chart。
- [R6] Web UI 必须渲染 stereo image chart。
- [R7] Web UI 必须渲染 loudness clock chart。
- [R8] Web UI 必须渲染 frequency collision chart。
- [R9] Windows WebView chart path 必须通过 visual/manual smoke，并保留 WPF fallback。

## 方案摘要

Host 层补齐 `HostDetailSnapshot` / DTO mapper，使 detail modules 从 workspace/settings 投影，并把当前 source 的 chart payload 与各模块配置一起暴露给 Web UI。HostBridge v1 新增 typed command names 和 payload validation，复用现有 `UserWorkspaceSettings` 字段，不引入 schema migration。

Web 层新增 `charts/` 下的纯渲染工具和 React components。普通 UI 继续使用 DOM；高频图表用 Canvas 绘制。loudness clock 的短期历史保存在 Web runtime state 中，按 selected source 和 `plotFrameVersion` 清理；core/host 仍只提供当前 snapshot truth。frequency collision bands 由 Host projection 提供，Web 只负责绘制。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.App/Host/**` - detail chart snapshot、commands、controller projection。
- `src/DesktopLoudnessMeter.App/WebView/**` - DTO mapper、request payload、bridge controller command mapping。
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 仅允许使用现有字段或添加非 schema-breaking helper。
- `src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs` - 仅允许提取/复用 host-neutral collision projection helper，不改变 WPF 行为。
- `src/DesktopLoudnessMeter.Web/src/**` - chart renderers、components、AppShell wiring、styles、tests、mock snapshot。
- `tests/DesktopLoudnessMeter.Tests/Host/**` - Host chart projection/command tests。
- `tests/DesktopLoudnessMeter.Tests/WebView/**` - HostBridge DTO/command tests。
- `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - WPF regression 补充。
- `tests/DesktopLoudnessMeter.Tests/Controls/**` - 仅当复用现有 rendering helper 需要补充 characterization。
- `openspec/changes/2026-06-30-webview-chart-parity/**` - 本 change 文档和 evidence。

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.SenderKit/**`
- `src/DesktopLoudnessMeter.Setup/**`
- macOS runner/WKWebView 文件
- WPF retirement/removal 相关文件

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-30-webview-chart-parity`
- Base Branch: `codex/2026-06-29-webview-packaging-startup-tray` at `9fe61ff8740647db99874c22958ba085e13c59b6`
- Merge Target: `codex/2026-06-29-webview-packaging-startup-tray`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-30-webview-chart-parity/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已归档的 WebView startup/tray change，当前 branch 从 `9fe61ff8740647db99874c22958ba085e13c59b6` 切出，属于架构迁移链式 implementation change。
  - isolated 触发：本 change 会触及 HostBridge DTO、command bus 和 Web UI，理论上存在 shared interface 风险；当前已在专用 branch、单 agent 串行推进、工作树干净，无并行 worker 或依赖隔离需求，因此采用 `current-worktree`。若后续出现并行实现、无关 dirty 或需要隔离验证，回到 plan 切换 isolated worktree。
  - 结论：`current-worktree` on dedicated branch。

## 验证计划

- `opsx changes resolve 2026-06-30-webview-chart-parity`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|WebView|DetailWindowViewModel|FrequencyCollision|Waveform|Spectrum|LoudnessClock"`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host src/DesktopLoudnessMeter.App/WebView`
- Windows manual smoke：启动 WebView 主窗口，选择 live 或 mock source，确认 waveform/spectrum/stereo/loudness/collision 图表渲染、controls 可操作、WPF fallback 可打开。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes，用于 Windows WebView visual/manual smoke。
- 不确定性来源：WebView2 runtime、Canvas 像素渲染、字体/缩放/DPI、live audio source availability、manual screenshot evidence。
- 人工 / 平台覆盖：Windows WebView chart smoke 是归档前必须关闭的 manual gate；若无 live source，可使用 mock/diagnostic snapshot 说明覆盖差异。
- 最终硬化策略：行为稳定后补齐 C# tests、Web unit/component tests、build smoke、boundary grep，并把 manual screenshot/结果写入 evidence。

## 回退 / 升级

- 回退方式：移除新增 Host chart DTO/commands、Web chart components/styles/tests，并恢复 Web detail placeholder；由于不做 schema migration，不需要用户数据回滚。
- 升级条件：如果 chart settings 需要新增持久化 schema 字段，停止本 change 的 schema 修改并回到 plan，拆到 settings/workspace change。
- 升级条件：如果 collision projection 必须依赖 WPF ViewModel instance 才能正确工作，停止实现并重新设计 host-neutral analyzer 边界。
- 升级条件：如果 Canvas/WebView2 性能需要 WebGL 或 native data throttling 才能达到可用状态，记录 stabilization finding 并回到 design/tasks 修订。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“只替换 Web placeholder 为静态 canvas”方案：无法验证 HostBridge detail/workspace/control 闭环。
- 不采用“把 WPF graph controls 暴露给 WebView”方案：会破坏 UI-neutral HostBridge 边界和 macOS runner 预留。
- 不采用“本 change 顺手退休 WPF”方案：WPF 仍是 fallback 和 regression oracle，退休属于后续 `wpf-retirement`。
- 不采用“把 chart DSP 迁入 C++ Core”方案：DSP/core boundary 是后续/既有 native core 工作，本 change 只消费已有 snapshot 与 host projection。

## Readiness Notes

- readiness 需要检查 R1-R9 是否在 specs/design/tasks 中完整 trace。
- readiness 需要检查 tasks 是否没有单独“补测试”任务，测试已并入业务 task。
- readiness 需要检查 `[stabilization-first]` manual gate 是否有 rationale、platform matrix 和 final hardening plan。
- readiness 需要检查 scope 是否仍未触碰 native/JUCE/protocol/schema migration/WPF retirement。
