# Work Packet: 2026-06-30-wpf-retirement

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: lean-change
- source_type: feature
- status: drafting
- created: 2026-06-30

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的用户可感知闭环是“WebView 成为唯一普通桌面入口，WPF 从正常入口退到受控 fallback/regression oracle 边界”。它需要同时调整启动/托盘路由、WPF fallback 入口可见性、回归测试和验证说明；如果只改托盘文案或只加测试，会留下无法验收的半成品。
- OpenSpec 下限检查：通过。该 change 涉及桌面启动路由、托盘工作流和 WPF fallback 生命周期边界，具备可评审、可验证、可归档的工程闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Normal entry retirement | 用户通过启动、托盘和二次激活只进入 WebView 主 UI | `App.xaml.cs`, `AppWindowRouter` | 高 | 本 change 的主闭环 |
| WPF oracle isolation | WPF 保留为受控 regression oracle/fallback，不作为普通用户菜单项 | `MainWindow`, WPF tests, resource tests | 中 | 不删除 WPF 文件 |
| Regression evidence | 证明 WebView 主入口未破坏，WPF oracle 仍可被工程验证覆盖 | infrastructure tests, Web tests/build, manual smoke | 高 | 归档前必须关闭 |

## 自我反驳 / 漏洞扫描

- 候选推荐：直接进入 `wpf-retirement`，删除 WPF 主窗口和 DetailWindow。
- 第 1 轮扫描：blocking=Web UI 仍缺完整 settings/workspace 工作流、别名编辑、source 排序、布局文件导入导出和独立 detail popup 等能力；直接删除 WPF 会失去 regression oracle。non-blocking=启动、托盘 show、二次激活已经走 WebView；chart parity 已完成，普通观察路径可用。
- 修订后的候选推荐：将本 change 收敛为“WPF normal-entry retirement / oracle isolation”，不做 WPF 文件删除。WebView 缺口写入范围外和后续依赖，普通用户入口先不再暴露 WPF fallback。
- 第 2 轮扫描：blocking=无新关键事实阻止 plan；remaining uncertainty=托盘菜单是否需要保留隐藏 debug fallback 的具体触达方式，需在 design 中选择保守方案并通过 manual smoke 验证。处理方式：默认去除普通托盘菜单的 WPF fallback 项，保留代码级/测试级 oracle；如需要运行 WPF fallback，可通过开发期路由/测试或后续 debug-only 入口处理，不作为用户主路径。
- 证据 vs 推断：已证实的是 `AppWindowRouter` 默认 startup/tray/secondary activation 均调用 WebView，`App.xaml.cs` 托盘菜单仍包含 `Open WPF Fallback`，`MainWindow.xaml` 仍包含完整 WPF UI 和 `Open WebView shell` 入口，Web `AppShell.tsx` 当前有 source/detail/chart/settings summary 但不是完整 WPF 设置 parity。推断的是普通发布界面应移除显式 WPF fallback 托盘项；该推断来自目标架构中“WPF 在 WebView UI 达到 parity 前保留为 fallback 和 regression oracle，之后退休或隔离”的约束，并通过本 change 的验证门收敛。
- 最终扫描结论：创建单个 `2026-06-30-wpf-retirement` lean change；不要扩大到 `webview-settings-workflow-parity` 或删除所有 WPF assets。

## 意图

WebView 已成为 Windows 主窗口并完成 chart parity，但 Windows host 仍在托盘菜单中把 WPF fallback 暴露为普通用户操作，WPF 主窗口中也保留“打开 WebView shell”的迁移按钮。当前状态容易让用户误以为两套 UI 都是一等入口，也会模糊后续 macOS/WebView runner 边界。

本 change 的目标是把 WPF 从普通用户入口退休：启动、托盘恢复、二次激活和可见菜单都指向 WebView；WPF 保留为受控 regression oracle/fallback 代码边界，直到后续 parity 缺口关闭后再做真正删除或项目隔离。

## 范围

### 范围内

- 调整 Windows 托盘菜单和窗口路由，使普通用户可见入口不再包含 `Open WPF Fallback`。
- 保留 `AppWindowRouter.OpenWpfFallback` 或等价内部 seam 作为测试/回归 oracle 边界，但不让它出现在正常用户工作流中。
- 移除或隔离 WPF 主窗口中的迁移期 WebView shell 按钮/文案，避免 fallback UI 反向宣传双主入口。
- 更新/补充基础设施测试，证明 startup、tray restore、secondary activation 仍打开 WebView，WPF fallback 只有显式内部调用才触发。
- 保留 WPF resource packaging / view model regression tests，证明 oracle 未被破坏。
- 更新 WebView/app startup codemap 或 evidence，记录 WPF 仍保留的原因和后续真正删除条件。

### 范围外

- 不删除 `MainWindow.xaml`, `DetailWindow.xaml`, WPF controls, WPF view models 或 WPF resource dictionaries。
- 不实现 WebView settings/workspace full parity、source alias editing、drag reorder、layout import/export 或 popup detail parity；这些若被证明必需，拆成后续 Web workflow parity change。
- 不改变 `DesktopLoudnessMeter.Native`, JUCE sender, Protocol, SenderKit 或 macOS runner。
- 不修改 workspace schema 或用户数据 migration。
- 不改变 installer/publish 主流程，除非发现托盘入口验证需要非破坏性测试修正。

## 依赖

- `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/`
- `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/`
- `openspec/changes/archive/2026-06-29-webview-windows-app-shell/`
- `openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/`
- `openspec/changes/archive/2026-06-30-webview-chart-parity/`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/app-startup.md`
- `.aiknowledge/codemap/source-list-workspace.md`

## 成功标准

- Windows startup、tray double-click/Show、secondary activation 仍只恢复 WebView shell。
- 普通托盘菜单不再显示 `Open WPF Fallback`。
- WPF fallback/oracle 仍保留可测试 seam，不被误删或破坏。
- WPF 主窗口不再显示迁移期 `Open WebView shell` 用户按钮。
- 自动化测试覆盖窗口路由和托盘菜单构成；现有 WPF resource packaging / view model regression 不退化。
- Web build/test 与 .NET 测试通过，或 evidence 记录非本 change 导致的允许失败。
- Windows manual smoke 通过：托盘可 Show/Exit，菜单无 WPF fallback 普通入口，WebView 主窗口可恢复。

## 需求 / 验收摘要

- [R1] 普通启动和激活路径必须只恢复 WebView 主窗口。
- [R2] 托盘菜单必须移除普通用户可见的 WPF fallback 入口。
- [R3] WPF fallback 必须保留为受控 regression oracle seam。
- [R4] WPF 主窗口必须去除迁移期 WebView shell 用户入口。
- [R5] 归档前必须验证 WebView primary 和 WPF oracle isolation 均未退化。
- [R6] WebView 宿主窗口必须去除普通用户可见的 WPF fallback 入口。

## 方案摘要

保守调整 `App.xaml.cs` 的托盘菜单构建，把 `Open WPF Fallback` 从普通菜单中移除，保留 `_windowRouter.OpenWpfFallback()` 的代码 seam 和测试覆盖。`AppWindowRouter` 保持清晰的 WebView primary / WPF fallback 边界，但测试要明确 fallback 不属于 startup/tray/secondary activation。

WPF `MainWindow` 继续作为 regression oracle 编译和测试，但移除窗口 chrome 中的 `Open WebView shell` 迁移按钮及其 click handler，减少双主入口暗示。由于 WPF oracle 仍用于对照和回归，不在本 change 删除 WPF resources 或大规模重构 view models。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.App/App.xaml.cs` - 托盘菜单和 fallback 入口隔离。
- `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs` - 仅允许为路由语义/测试 seam 做小幅调整。
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - 移除迁移期 WebView shell 用户按钮。
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` - 移除不再使用的 WebView shell click handler。
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml` - 移除 WebView host 可见 WPF fallback 控件。
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs` - 移除 WebView host fallback handler/hotkey glue。
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/**` - startup/tray/router regression。
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/WpfResourcePackagingTests.cs` - 若需强化 oracle 保留验证。
- `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - 若 WPF oracle 行为受影响才补充 regression。
- `src/DesktopLoudnessMeter.Web/src/**` - 仅允许为 manual smoke 中发现的非范围外小 UI 修复。
- `.aiknowledge/codemap/**` - 归档/知识阶段更新。
- `openspec/changes/2026-06-30-wpf-retirement/**` - 本 change 文档和 evidence。

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.SenderKit/**`
- `src/DesktopLoudnessMeter.Setup/**`
- workspace/settings schema migration 文件，除非回到 plan 重新切分。
- macOS runner/WKWebView 文件。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-30-wpf-retirement`
- Base Branch: `codex/2026-06-30-webview-chart-parity` at `9deb60b443997389e4a040683585016f76565a16`
- Merge Target: `codex/2026-06-30-webview-chart-parity`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-30-wpf-retirement/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已归档的 WebView chart parity 分支 HEAD `9deb60b443997389e4a040683585016f76565a16`，属于 WebView 架构迁移链式 implementation change。
  - isolated 触发：本 change 触及启动/托盘路由和 WPF fallback 边界，属于跨模块行为变更；当前已经在专用 branch、单 agent 串行推进、工作树干净，无并行 worker 或依赖隔离需求，因此采用 `current-worktree`。如后续出现无关 dirty、并行验证或需要隔离 installer/publish 验证，回到 plan 切换 isolated worktree。
  - 结论：`current-worktree` on dedicated branch。

## 验证计划

- `opsx changes resolve 2026-06-30-wpf-retirement`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouterTests|TrayExitCoordinatorTests|WpfResourcePackagingTests"`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- `rg -n "Open WPF Fallback|Open WebView shell" src/DesktopLoudnessMeter.App`
- `rg -n "Open WPF Fallback|Open WPF fallback|Open WebView shell|WebViewShellButton_OnClick|FallbackButton_OnClick" src/DesktopLoudnessMeter.App`
- Windows manual smoke：启动 WebView 主窗口，右键托盘确认无 `Open WPF Fallback`，执行 Show/Exit，确认 WebView 可恢复且托盘退出仍通过。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes，仅用于 Windows tray/menu/manual smoke。
- 不确定性来源：Windows tray context menu 行为、真实 WebView2 窗口恢复、用户手动退出路径。
- 人工 / 平台覆盖：Windows 手工 smoke 是归档前 blocking manual gate；若环境无法打开真实托盘，必须记录原因并用可替代的 UI/automation evidence 覆盖菜单构成。
- 最终硬化策略：行为稳定后补齐 infrastructure tests；manual-only 部分限于 Windows 托盘/WebView 恢复。

## 回退 / 升级

- 回退方式：恢复托盘菜单中的 WPF fallback 项和 WPF `Open WebView shell` 按钮/handler；保留测试调整可按同一 commit 回滚。
- 升级条件：如果 WebView 缺失的 settings/source workflow 被验证为阻止普通入口退休，停止实现并拆出 `webview-workflow-parity` 后再恢复本 change。
- 升级条件：如果需要发布版 debug-only fallback 入口或命令行开关，回到 plan 明确安全边界，不在本 change 隐式新增未验证入口。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“删除全部 WPF UI 文件”方案：WebView settings/workflow 尚未完整 parity，WPF 仍需作为 regression oracle。
- 不采用“保留托盘 WPF fallback 普通入口但改名为 debug”方案：仍会把 fallback 暴露给普通用户，和 WebView primary 目标冲突。
- 不采用“先实现所有 Web workflow parity 再做本 change”方案：范围会膨胀为多个独立能力，应拆成后续 changes。

## Readiness Notes

- readiness 需要检查 R1-R6 是否在 specs/design/tasks 中完整 trace。
- readiness 需要确认 tasks 没有把测试作为独立任务，而是并入行为 task。
- readiness 需要确认 `[stabilization-first]` manual gate 有 rationale、platform matrix 和 final hardening plan。
- readiness 需要确认 scope 没有删除 WPF oracle 或修改 native/JUCE/protocol/settings schema。
