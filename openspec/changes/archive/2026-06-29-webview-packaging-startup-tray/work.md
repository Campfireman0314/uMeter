# Work: 2026-06-29-webview-packaging-startup-tray

## 目标

让 Windows 主路径从“WPF 默认启动，手动打开 WebView shell”演进为“WebView shell 默认主窗口，WPF 作为 fallback/regression oracle 可显式打开”。本 change 聚焦 host glue 层：启动顺序、单实例 activation、托盘 show/exit 菜单、WebView 主窗口创建/复用、WPF fallback 入口、publish/installer payload 中 Web frontend asset 的可验证性。

目标不是退休 WPF，也不是完成 WebView 图表 parity。WPF 仍保留，复杂图表仍留给 `webview-chart-parity`。

## 范围内

- 修改 Windows app startup，使 primary instance 正常启动后默认创建并显示 `WebViewShellWindow`。
- 保留 `MainWindow`/WPF 作为 fallback，并提供明确入口打开 WPF fallback。
- 迁移 tray 的默认 show/double-click 行为到 WebView 主窗口，同时保留 WPF fallback 菜单项和 pop-up detail 能力。
- 保持 single-instance gate 仍早于 settings、source receivers、tray icons 和 windows；secondary launch 只请求 activation 并退出。
- 保证 activation request 会显示/激活 WebView 主窗口；如果 WebView 初始化失败或不可用，应有可观察 fallback 路径。
- 复用单一 `ISignalSourceMeterService` 和 `UserWorkspaceSettings`，禁止为 WebView/WPF 分别创建 capture/UDP/service graph。
- 验证 app build/publish 会包含 `Web/` frontend assets 和启动必需的 `DesktopLoudnessMeter.Native.dll`；installer payload 继续携带 app publish output。
- 更新 `.aiknowledge/codemap` 与 `docs/current-status.md` 中 WebView startup/packaging 状态。

## 范围外

- 不实现 waveform、spectrum、loudness clock、frequency collision 等 Canvas/WebGL chart parity。
- 不退休、删除或隔离 WPF UI；`wpf-retirement` 单独处理。
- 不实现 macOS WKWebView runner、macOS capture adapter、notarization 或 packaging。
- 不改变 HostBridge v1 DTO/command vocabulary，除非启动切换暴露了必要的 wiring 缺口。
- 不改变 native DSP/source ingest ABI。
- 不改变 JUCE sender、Protocol v1 或 VST3 sender lifecycle。
- 不引入安装包签名、SmartScreen 处理或 release version bump。

## 依赖

- 已归档 `2026-06-29-ui-runtime-architecture-replan`：WebView2/WKWebView + TypeScript/React/Vite 是当前 UI runtime 方向。
- 已归档 `2026-06-29-host-snapshot-command-contract`：WebView 通过 UI-neutral host snapshot/command/result contract 消费 host state。
- 已归档 `2026-06-29-webview-windows-app-shell`：Windows WebView2 shell、HostBridge v1 native message transport 和 React/Vite shell 已可运行，但不是默认主窗口。
- 已归档 `2026-06-29-settings-workspace-schema`：settings DTO/schema 已稳定为 Web UI 消费边界。
- 当前 codemap：
  - `.aiknowledge/codemap/app-startup.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
- 相关 pitfall：
  - `.aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
  - `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`

## 完成标准

- 正常启动路径默认显示 WebView shell，并把 `Application.MainWindow` 指向 WebView 主窗口或等价的 activation target。
- Tray “Show uMeter”/double-click/secondary-launch activation 显示并激活 WebView 主窗口。
- Tray 仍提供 WPF fallback 入口；WPF fallback 可打开并复用同一个 source service / workspace settings，不创建第二套 receiver/capture graph。
- WPF fallback 关闭/隐藏不会退出应用；tray Exit 仍是退出路径。
- WebView 初始化失败或 Web assets 缺失时，用户能通过 fallback 路径打开 WPF，并且错误可见。
- App publish 输出包含 `Web/index.html`、built assets 和 `DesktopLoudnessMeter.Native.dll`；installer payload zip 包含 Web/native runtime assets，或 installer route 的工具链限制被结构化记录且不伪称通过。
- Automated tests 覆盖 startup/tray/activation decision 可测试部分、Web asset locator/publish payload invariant、existing SingleInstance/Tray tests。
- Windows manual smoke 覆盖 WebView default launch、tray restore、secondary activation、WPF fallback、exit。

## 切分结论

### 总判断

- 结论：`KEEP_ONE_CHANGE`
- 理由：默认主窗口切换、tray show/activation、WPF fallback 和 packaging payload 是同一个用户可感知闭环。若只改 startup 不验证 payload，安装后可能无法加载 Web UI；若只改 packaging 不迁移 activation/tray，WebView 仍不是主路径。chart parity 和 WPF retirement 可独立延期，因此不纳入本 change。
- OpenSpec 下限检查：通过。该 change 触及 startup/tray/package glue 和可运行主路径，具备可评审、可验证、可归档的模块闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| WebView default startup | 启动后直接进入 WebView 主 UI | `App.xaml.cs`, `WebViewShellWindow` | 高 | 本 change 核心 |
| Tray and activation routing | 托盘和二次启动都恢复 WebView 主窗口 | `App.xaml.cs`, `SingleInstanceCoordinator`, tray menu | 高 | 与 default startup 同闭环 |
| WPF fallback | WebView 未 parity 前仍有 regression oracle | `MainWindow`, fallback menu | 高 | 保留而非退休 |
| Publish/installer payload | 安装/发布后的主窗口能加载 Web frontend | `.csproj`, `build-installer.ps1`, setup payload | 中 | 与 default startup 共同验证 |

### 更小替代

- 只把 WPF 按钮改成“Open WebView first”：拒绝。不能让 WebView 成为主路径。
- 只改 `App.OnStartup` 显示 WebView：拒绝。tray/activation 仍会恢复 WPF，主路径语义不闭环。
- 同时做 chart parity 或 WPF retirement：拒绝。会把主窗口切换和 UI parity 混在一个过大的 change 中。

## 探索自我反驳 / 漏洞扫描固化

- 候选推荐：创建 `2026-06-29-webview-packaging-startup-tray`，`profile=strict-change`。
- 第 1 轮扫描：blocking=曾怀疑 setup project 缺失；non-blocking=JUCE/VST3 packaging route 可能受 CMake/JUCE 环境限制。
- 修订后候选推荐：setup project 已确认存在，change 范围包含 publish/installer payload 验证，但如果 JUCE toolchain 不可用，允许用 `-SkipVst3Plugin` 验证 app/installer payload，并结构化记录完整 VST3 installer route 未运行。
- 第 2 轮扫描：blocking=无新的关键事实漏洞；non-blocking=WebView2 runtime/manual smoke 需要真实 Windows UI 环境，必须使用 `[stabilization-first]` task 和 manual gate。
- 未验证前提：WebView2 runtime 在当前机器可初始化；installer full route 能否在缺少 JUCE/CMake 时完整运行；secondary activation 到 WebView 的真实窗口行为。
- 最可能错处：隐藏/关闭 WebView 主窗口时误触发 app shutdown，或者 tray/activation 仍指向 WPF；publish 输出可能缺 Web assets。
- 失败路径 / 边界：WebView asset 缺失、WebView2 runtime 初始化异常、secondary launch activation before window ready、tray menu rebuilding while fallback unopened、installer payload zip missing `Web/`。
- 证据 vs 推断：已证实 WebView shell、HostBridge、setup project、single-instance/tray coordinators 存在；推断是可通过 small startup orchestration seam 自动化大部分行为，真实 UI activation 仍需 manual smoke。
- 处理方式：plan 中将 startup/tray decision 抽成可测试 helper/host seam，WebView runtime 走 stabilization + manual matrix，installer route 区分 app publish payload 和 full VST3 bundle route。

## Workspace Strategy

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-webview-packaging-startup-tray`
- Base Branch: `codex/2026-06-29-juce-sender-protocol-boundary`
- Merge Target: 待用户后续决定；本 change 链接在 `3c8069a405590da0e9ac36230100d9c3b39a27e5` 之后。
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树干净；创建 change 后 dirty 仅允许包含 `openspec/changes/2026-06-29-webview-packaging-startup-tray/**`，后续实现 dirty 必须属于本 change 范围。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已完成的 WebView shell、settings schema、native/source/protocol 边界继续推进总体架构，`base_branch=codex/2026-06-29-juce-sender-protocol-boundary`，`base_head=3c8069a405590da0e9ac36230100d9c3b39a27e5`，属于链式 change。
  - isolated 触发：该 change 触及 startup/tray/package glue 和 app build config，理论上是共享行为；当前已经切到专用分支，工作树干净，单 agent 串行推进，不需要额外 worktree。若出现无关 dirty 或需要并行 UI/runtime验证，再回 plan 切换 isolated worktree。
  - 结论：使用 `current-worktree`，path-limited staging，archive 后形成 change-level checkpoint。

## 验证计划

- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "SingleInstance|Tray|WebView|HostBridge|Startup"`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
- `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/app-publish-webview-smoke`
- Assert published output contains `Web/index.html` and `Web/assets/*`.
- Installer payload route:
  - Preferred: `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4`
  - If JUCE/CMake unavailable: `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin` and inspect payload/app publish Web assets; record full VST3 bundle route as toolchain-limited.
- Windows manual smoke: launch app, verify WebView opens by default; tray show/double-click restores WebView; secondary launch activates WebView; WPF fallback opens from tray; exit closes app.

## 回退策略

- Revert startup orchestration to create/show WPF `MainWindow` by default and leave WebView shell explicit only.
- Revert tray menu labels/actions to WPF-first behavior.
- Revert app publish/installer payload changes if Web assets are not reliably included.
- WPF fallback remains available throughout this change, so rollback does not require rebuilding old UI.

## Bugfix Root Cause

none（feature / architecture migration）。

## Rejected Attempts

- 不采用“直接删除 WPF 或让 WPF 只作为代码死档”：WebView chart parity 尚未完成，WPF 仍是 fallback/regression oracle。
- 不采用“只改托盘菜单文案”：无法改变启动主路径。
- 不采用“把 full installer signing/version bump 合并进来”：签名/版本发布是 release work，不是主窗口切换闭环。

## Readiness Notes

- readiness 必须确认 WPF fallback 是保留边界，不是退休。
- readiness 必须确认 installer/publish 验证不会依赖不可用 JUCE runner 才能证明 Web assets payload。
- readiness 必须确认 manual WebView2 smoke 是 blocking manual gate，不能在未验证时 archive。
- readiness 必须确认没有改变 HostBridge DTO/schema、native ABI、JUCE sender 或 Protocol v1。
