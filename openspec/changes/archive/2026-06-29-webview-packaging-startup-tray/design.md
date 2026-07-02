# Design: 2026-06-29-webview-packaging-startup-tray

## 上下文

已证实事实：
- `App.OnStartup` 当前获得 single-instance ownership 后创建 `UserWorkspaceSettings`、source service、`MainViewModel`，然后显示 WPF `MainWindow`。
- `OpenWebViewShell()` 当前只作为显式入口创建 `WebViewShellWindow`，并复用 `_sourceMeterService` 和 `_workspaceSettings`。
- Tray `Show uMeter` 和 tray double-click 当前调用 `ShowMainWindow()`，目标是 WPF main window。
- `webview-shell` codemap 记录 WPF 当前仍是默认 startup/fallback，WebView shell 需要显式打开。
- `DesktopLoudnessMeter.App.csproj` 已在 build 时构建/copy `src/DesktopLoudnessMeter.Web/dist` 到 app output `Web/`。
- `DesktopLoudnessMeter.Native.targets` 已在 build 时把 `DesktopLoudnessMeter.Native.dll` copy 到 app/test output；publish output 是否包含 native DLL 需要验证。
- `build-installer.ps1` 先 publish app，再把 app publish output zip 成 installer payload；setup project 存在。

推断：
- 最小稳定做法是保留当前 app graph 创建顺序和 service ownership，只把 default window / activation target 切到 WebView。
- WPF fallback 可以懒创建；这样默认启动不会显示 WPF，但 fallback 仍可复用同一个 `MainViewModel`。
- WebView runtime/manual 行为需要真实 Windows UI smoke；自动化测试应覆盖可抽出的 decision/helper 和 payload invariants。

## 需求追踪

- [R1] -> [U1], [U2]
- [R2] -> [U1], [U3]
- [R3] -> [U2], [U3]
- [R4] -> [U1], [U2]
- [R5] -> [U4]
- [R6] -> [U2], [U3]

## 实施单元

### [U1] Startup graph keeps ownership while changing default window
- 关联需求: [R1], [R2], [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`
- 设计:
  - 保持 single-instance coordinator 第一位。
  - 保持 `_workspaceSettings`、`_sourceMeterService`、`_mainViewModel` 单例 ownership 在 `App` 内。
  - 启动后默认调用 WebView 主窗口显示逻辑，而不是立即构造/显示 WPF main window。
  - 将 WPF fallback 改为懒创建方法，只有 fallback 请求时创建/显示。
  - `Application.MainWindow` 应指向 WebView 主窗口；WPF fallback 不能成为 app shutdown owner。

### [U2] WebView shell host becomes activation target
- 关联需求: [R1], [R3], [R6]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/**`
- 设计:
  - `ShowWebViewShell()` 或等价方法负责 create/show/restore/activate WebView shell。
  - Secondary activation replay flag 应指向 WebView shell，不再指向 WPF。
  - WebView shell 初始化失败时保持 shell window 可见，展示 error status；fallback entry 仍可用。
  - 如需从 WebView shell 暴露 fallback 入口，可使用 host window chrome/menu/托盘，不修改 HostBridge DTO。

### [U3] Tray menu and fallback commands
- 关联需求: [R2], [R3], [R6]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/Infrastructure/TrayExitCoordinator.cs`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs`
- 设计:
  - Tray double-click 与 “Show uMeter” 指向 WebView shell。
  - Tray 增加/保留明确的 “Open WPF Fallback” 行为。
  - `Pop-up App...` 仍可使用现有 `MainViewModel.Sessions` 与 `OpenDetailPopup`，但不得强制显示 WPF main window。
  - Exit 仍通过 `TrayExitCoordinator` 延迟 shutdown，避免 context-menu callback 内同步退出。

### [U4] Publish and installer payload validation
- 关联需求: [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets`
  - `build-installer.ps1`
  - `src/DesktopLoudnessMeter.Setup/Program.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/**`
  - `docs/current-status.md`
- 设计:
  - 优先不改变 installer install semantics；验证现有 app publish output/payload 是否已经携带 `Web/` 和 app 启动必需的 native runtime dependency。
  - 如发现 publish 没有携带 Web assets，修复 `.csproj` build/copy target 或 publish target。
  - 如发现 publish 没有携带 `DesktopLoudnessMeter.Native.dll`，修复 native copy target 的 publish 路径，避免 published app 启动时 `DllNotFoundException`。
  - 如果 full installer route 因 JUCE/CMake unavailable 失败，使用 `-SkipVst3Plugin` 验证 app/Web payload，并在 evidence 中记录 full VST3 route limitation。

### [U5] Knowledge and status refresh
- 关联需求: [R1], [R2], [R3], [R5]
- 模块边界:
  - `.aiknowledge/codemap/app-startup.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/logs/2026-06.md`
  - `docs/current-status.md`
- 设计:
  - 归档后 codemap 应说明 WebView 是 Windows default startup target，WPF 是 fallback。
  - `docs/current-status.md` 的 WebView migration status 不得继续声称 WebView 只作为 explicit WPF entry。

## 关键决策

1. **默认窗口切换，不删除 WPF。**
   - 原因：WebView chart parity 未完成，WPF 仍是 fallback/regression oracle。
   - 影响：App 同时维护 WebView 主窗口和 WPF fallback，但 service graph 只创建一次。

2. **Activation target 是 WebView shell。**
   - 原因：用户二次启动或 tray restore 期待回到主 UI；本 change 的主 UI 是 WebView。
   - 影响：旧 `ShowMainWindow()` 语义应重命名或改为 fallback-only，避免概念漂移。

3. **Packaging 先验证 payload，不做 release signing/version bump。**
   - 原因：目标是让安装后的 app 能加载 Web UI，不是发布新版本。
   - 影响：installer route 可用 `-SkipVst3Plugin` 覆盖 Web payload，full VST3 bundle route 若工具链缺失则记录限制。

4. **不改变 HostBridge DTO。**
   - 原因：host contract/settings schema 已独立归档；startup change 不应制造 UI contract drift。
   - 影响：WebView fallback/open-WPF 行为优先放在 host shell/tray，不通过 HostBridge 新增命令。

## 风险 / 权衡

- WebView `Window.Closed` 可能触发 app shutdown：需要测试/手动验证主窗口关闭语义，并确保 tray/exit 行为符合预期。
- WPF fallback 懒创建可能影响 `OpenDetailPopup` owner positioning：当 WPF fallback 不存在时 popup 应 center screen 或不依赖 WPF owner。
- App publish target already builds frontend during build/publish; repeated npm build may slow verification but provides payload proof.
- Native DLL copy target historically covered build/test output but not necessarily publish output; published app startup smoke must verify native dependency deployment.
- Full installer may need JUCE/CMake; this change cannot make JUCE toolchain magically available.

## 稳定化策略

- `[stabilization-first]` 用于 WebView runtime/manual smoke 与 packaging route，因为 WebView2 window activation、tray integration 和 installer payload are platform/runtime-observed behaviors。
- doc impact:
  - `DESIGN_AMENDMENT` only if implementation discovers startup/fallback topology must differ from this design.
  - `TASK_EVIDENCE_ONLY` for command output/manual evidence.
- final hardening:
  - automated tests for extracted startup/tray decisions and payload checks where feasible.
  - full managed tests, app build, web tests/build, native build, app publish payload inspection.
  - Windows manual smoke must be `[x]` or archive must not proceed.

## 回退方案

- Restore startup to WPF default and keep WebView shell explicit.
- Restore tray show/double-click to WPF `ShowMainWindow`.
- Leave `.csproj` Web asset build target as it was if publish changes regress build.
- Remove docs/codemap updates if product change rolls back.

## 并行 / Subagent 边界

默认串行。该 change 的风险集中在 app startup ownership 和 platform UI smoke，不适合并行改同一文件。可并行只读检查 installer payload or WebView runtime docs，但不得由 sidecar 修改 `App.xaml.cs` 或 shared artifacts。

## 知识沉淀

Archive 后运行 `opsx-knowledge mode=auto`，刷新 `app-startup`、`webview-shell`、`ui-runtime-architecture`，并判断是否需要新增“default window must match tray activation target” pitfall。
