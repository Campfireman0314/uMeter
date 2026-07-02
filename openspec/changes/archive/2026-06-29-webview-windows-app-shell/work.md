# Work Packet: 2026-06-29-webview-windows-app-shell

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的用户可感知闭环是“Windows 上出现可运行的 WebView 主 UI shell，并通过 HostBridge v1 消费真实 host snapshot/command contract”。它需要同时包含 Windows WebView2 host、native message transport、typed command bus envelope、React/Vite web app skeleton、基础 source/status/recent/settings/detail UI 和 smoke/test wiring；拆得更小会产生无法独立演示的 transport 或空 frontend 碎片。它不包含 packaging 主窗口切换、复杂 Canvas/WebGL 图表 parity、settings schema migration、native core migration 或 macOS runner，避免变成全栈迁移。
- OpenSpec 下限检查：通过。该 change 会形成可评审、可测试、可归档的工程闭环：新增 Windows WebView shell 入口、HostBridge v1 基础通信、Web UI skeleton 和自动/人工 smoke，WPF 保持 fallback/regression oracle。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Windows WebView host | 用户可以打开 WebView 承载的 uMeter 主 UI shell | `src/DesktopLoudnessMeter.App`, WebView2 host files | 高 | Windows-first 主路径 |
| HostBridge v1 transport | Web UI 可以读取 host snapshot 并发出 typed command | `src/DesktopLoudnessMeter.App/Host`, new bridge files | 高 | 复用已归档 host contract vocabulary |
| React/Vite frontend shell | 用户能看到 source list、status、recent events、基础 detail shell 和设置入口 | new web frontend project | 高 | 不做复杂 charts parity |
| WPF fallback guard | 保留现有 WPF UI 作为 regression oracle 和 fallback | existing WPF app files/tests | 高 | packaging 切换留给后续 change |

## 意图

`ui-runtime-architecture-replan` 已将主 UI runtime 修订为 WebView。当前仓库仍是 WPF/.NET Windows app，没有 Web frontend project，也没有 WebView2 host。已归档的 `host-snapshot-command-contract` 提供 UI-neutral `HostSnapshot`、`HostCommand` 和 `HostCommandResult`，这是 WebView shell 的主要消费边界。

本 change 的目标是在 Windows 上建立第一条可运行主路径：WPF app 内新增 WebView2 shell/runner 边界，通过 HostBridge v1 使用 native WebView message channel 和 JSON DTO 交换 host snapshot/command/result；同时新增 TypeScript + React + Vite web app，渲染 source list、status、recent event、基础 detail shell、主题/设置入口。WPF 主 UI 保留，不在本 change 中退休或替换 installer/startup behavior。

## 范围

### 范围内

- 在 Windows app 中新增 WebView2 shell host，可作为独立窗口/入口运行并加载 Web frontend build/dev asset。
- 定义 HostBridge v1 envelope 和 JSON DTO serialization boundary，区分 transport、typed command bus 和 payload。
- Web UI 使用 TypeScript + React + Vite，新增基础 UI shell：source list、status text、recent events、selected detail placeholder、theme/settings entry、command error display。
- Web UI 能通过 HostBridge v1 获取初始 snapshot、刷新 snapshot、选择 source、返回全局、切换 show hidden、触发 reset/alias/hide/restore/move 等基础命令中可安全落地的子集。
- 保留 WPF UI fallback；新增入口不得破坏现有 startup、tray、single-instance 和 WPF tests。
- 添加 .NET host/bridge tests、web unit tests 或 smoke tests，以及 Windows manual smoke 验收项。

### 范围外

- 不让 WebView shell 成为 installer/startup/tray 的唯一主窗口；主窗口切换留给 `webview-packaging-startup-tray`。
- 不迁移 waveform、spectrum、loudness clock、frequency collision 等高频图表；图表 parity 留给 `webview-chart-parity`。
- 不迁移 settings/workspace schema；只消费当前 host snapshot/settings projection，schema/type 稳定化留给 `settings-workspace-schema`。
- 不修改 C++ Core build/source ingest/protocol/JUCE sender。
- 不实现 macOS WKWebView runner。
- 不退休 WPF。

## 依赖

- `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/` 已归档，指定本 change 为 WebView route 的首个 implementation change。
- `openspec/changes/archive/2026-06-29-host-snapshot-command-contract/` 已归档，提供 UI-neutral host snapshot/command/result contract。
- `.aiknowledge/codemap/ui-runtime-architecture.md`、`host-contract.md`、`app-startup.md`、`source-list-workspace.md` 是本 change 的 codemap 入口。
- 当前仓库已有 .NET tests；当前仓库未发现 `package.json` / `vite.config.*`，Web frontend 测试基础设施需要随 frontend skeleton 一起建立。

## 成功标准

- Windows app 能构建并提供 WebView shell 入口，WebView 能加载 Web frontend asset。
- HostBridge v1 能向 Web UI 提供初始/刷新 host snapshot，并能执行至少一组基础 typed commands，返回 success/error result。
- Web UI 能显示 host status、source list、recent events、基础 detail shell、settings/theme entry，并在无 source/bridge error 时有明确状态。
- WPF UI、startup、single-instance、tray 和现有 host contract tests 不回退。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 通过或已记录既有环境限制；Web frontend test/build 命令通过。
- Windows manual smoke 证明 WebView shell 可打开、能显示 snapshot、能执行至少一个 command、WPF fallback 仍可用。

## 需求 / 验收摘要

- [R1] Windows app 必须提供 WebView2 shell 入口并保留 WPF fallback。
- [R2] HostBridge v1 必须通过 native WebView message channel 承载 JSON DTO envelope。
- [R3] Web UI 必须使用 TypeScript + React + Vite 并渲染基础 host snapshot。
- [R4] Web UI 必须能发出基础 typed host commands 并显示 result/error。
- [R5] Change 必须明确非目标并保护现有 WPF/startup/tray/host contract regression。

## 方案摘要

新增 `DesktopLoudnessMeter.App.WebView` 边界：WPF app 中增加 WebView shell window/host service，使用 WebView2 `CoreWebView2.WebMessageReceived` / `PostWebMessageAsJson` 作为 native message transport。C# bridge 层定义 `HostBridgeEnvelope`、`HostBridgeRequest`、`HostBridgeResponse` 和 DTO mapper，调用 `DesktopMeterHostController` 执行 snapshot refresh/command。

新增 `src/DesktopLoudnessMeter.Web` 或等效目录，包含 `package.json`、Vite、React、TypeScript 和基础组件。普通 UI 使用 DOM components；图表区域仅渲染 placeholder/readout，不做 Canvas/WebGL parity。开发期可加载 Vite dev server，发布/测试可加载 built static assets；具体 asset copy 策略由 implementation 在本 change 内收敛。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` — WebView2 package、web asset copy/build hook（如采用）。
- `src/DesktopLoudnessMeter.App/App.xaml.cs` — 仅限新增 WebView shell 入口 wiring，不能破坏现有 startup order。
- `src/DesktopLoudnessMeter.App/WebView/**` — WebView shell window/host/bridge/DTO mapper。
- `src/DesktopLoudnessMeter.App/Host/**` — 仅限 bridge 需要的非破坏性 helper/serialization adapter；不得改变 host contract 语义。
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` / related WPF menu files — 仅限新增打开 WebView shell 入口（如果需要）。
- `src/DesktopLoudnessMeter.Web/**` — React/Vite web app、components、tests、build config。
- `tests/DesktopLoudnessMeter.Tests/WebView/**` — bridge/envelope/DTO tests。
- `tests/DesktopLoudnessMeter.Tests/Host/**` — host regression 更新（如需要）。
- solution/project metadata required to build/test the above.

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`（除非 readiness 发现 bridge envelope 必须引用 protocol，但默认禁止）
- `src/DesktopLoudnessMeter.SenderKit/**`
- `src/DesktopLoudnessMeter.Setup/**`
- WPF retirement/removal files beyond optional launch entry.

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-webview-windows-app-shell`
- Base Branch: `opsx/native-core-dsp-migration` at `11dece2bd6ad3546fccbf6430de4c481f60a6d05`
- Merge Target: `opsx/native-core-dsp-migration`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-29-webview-windows-app-shell/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已归档的 replan/host-contract/native-dsp 链，当前专用分支从 `opsx/native-core-dsp-migration` 的 `11dece2bd6ad3546fccbf6430de4c481f60a6d05` 切出；属于链式 implementation change。
  - isolated 触发：本 change 会触及 package/build config、WebView host glue、frontend project 和 bridge public surface，理论上适合隔离；当前已经切到专用分支、工作树干净、单 agent 串行推进，不需要额外 worktree。若 implementation 需要并行 worker、依赖隔离或出现无关 dirty，再回 plan 切换 isolated worktree。
  - 结论：`current-worktree` on dedicated branch。

## 验证计划

- `opsx changes resolve 2026-06-29-webview-windows-app-shell`
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- Web frontend install/build/test command（implementation 根据实际 package manager 写入 evidence，例如 `npm install` / `npm test` / `npm run build`）。
- Boundary grep：`rg -n "Flutter|flutter|pubspec" src/DesktopLoudnessMeter.Web src/DesktopLoudnessMeter.App/WebView` 预期无主路径命中。
- Windows manual smoke：打开 WPF app，启动 WebView shell，确认 source/status/recent events render，执行 refresh/select/back 或 show-hidden command，确认 WPF fallback 仍可打开。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes，用于 WebView runtime/manual smoke，因为 WebView2 runtime、dev/static asset loading 和 native message channel 需要在本机 Windows 交互验证。
- 不确定性来源：WebView2 NuGet/runtime availability、static asset path、Vite dev/build mode、WebMessageReceived JSON envelope、manual UI smoke。
- 人工 / 平台覆盖：Windows WebView2 manual smoke 是归档前必须关闭的 manual gate。
- 最终硬化策略：稳定后补 bridge/DTO unit tests、web component tests/build smoke、WPF regression test；manual smoke 结果写入 evidence。

## 回退 / 升级

- 回退方式：移除 `src/DesktopLoudnessMeter.App/WebView/**`、`src/DesktopLoudnessMeter.Web/**`、WebView2 package/build hooks 和入口 wiring；WPF UI 保持原状。
- 升级条件：如果 bridge envelope 需要正式 schema/versioning 才能安全落地，停止实现并回 plan，或拆出 `settings-workspace-schema` / bridge-contract change。
- 升级条件：如果 WebView shell 必须接管 startup/tray/installer 才能可运行，停止该部分并路由到 `webview-packaging-startup-tray`，本 change 保持独立 shell/fallback。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“直接替换 WPF 主窗口”：packaging/startup/tray 和 WPF retirement 是后续 change，本 change 先建立可运行 WebView shell。
- 不采用“只建 React app 不接 host”：无法形成 uMeter 用户可感知闭环，且不能验证 HostBridge v1。
- 不采用“只写 WebView host 不建 frontend”：无法证明 Web UI stack 和 bridge payload 能消费 host snapshot。
- 不采用“在本 change 做图表 parity”：高频图表的 Canvas/WebGL performance/visual regression 是独立风险，应由 `webview-chart-parity` 接管。

## Readiness Notes

- readiness 需检查 `tasks.md` 中 WebView manual smoke 是否有四态 manual gate。
- readiness 需检查 package/build config、bridge public surface 和 frontend project 是否仍属于单个可运行 shell 闭环。
- readiness 需检查 Web frontend test setup 是否作为脚手架的一部分，而不是孤立测试任务。
- readiness 需检查 WPF fallback、startup/tray 和 packaging non-goals 是否清晰。
