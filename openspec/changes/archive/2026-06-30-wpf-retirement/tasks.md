# Tasks: 2026-06-30-wpf-retirement

## Default Constraints

- Forbidden Files:
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Isolate WPF From Normal Tray And Activation Paths

- [x] 1.1 [characterization-first] 移除普通托盘 WPF fallback 入口

  **需求追踪**: [R1, R2, R3] -> [U1]
  **执行方式**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - tray menu entry removal and fallback isolation
  - `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs` - route seam wording or behavior if needed
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/AppWindowRouterTests.cs` - routing characterization
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs` - tray exit regression if touched

  **验收标准**:
  - [x] Startup, tray restore, and secondary activation route only to WebView primary.
  - [x] `OpenWpfFallback` remains explicit and callable by tests/internal code.
  - [x] App tray menu no longer adds `Open WPF Fallback` as a normal user-visible item.
  - [x] Tray Show and Exit behavior remains covered by regression evidence.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouterTests|TrayExitCoordinatorTests"`，预期：通过
  - `rg -n "Open WPF Fallback" src/DesktopLoudnessMeter.App tests/DesktopLoudnessMeter.Tests`，预期：无 production menu string；测试引用需说明

  **Evidence Required**:
  - characterization-before/after summary for routing behavior
  - command output summary for targeted .NET tests and grep

  **Done When**:
  - Normal Windows shell entries can no longer open WPF fallback, while internal seam remains testable.

  **依赖**: 无

## 2. Clean WPF Oracle Chrome

- [x] 2.1 [characterization-first] 移除 WPF main window 的 WebView 迁移按钮

  **需求追踪**: [R4, R5] -> [U2]
  **执行方式**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - remove migration button and adjust header columns
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` - remove unused click handler
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/WpfResourcePackagingTests.cs` - oracle packaging regression if needed

  **验收标准**:
  - [x] WPF main window no longer contains `Open WebView shell` button text or tooltip.
  - [x] Code-behind no longer contains unused `WebViewShellButton_OnClick`.
  - [x] WPF resource packaging test still proves WPF oracle resources are available.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WpfResourcePackagingTests"`，预期：通过
  - `rg -n "Open WebView shell|WebViewShellButton_OnClick" src/DesktopLoudnessMeter.App`，预期：无命中或仅 evidence 允许的非 production 引用

  **Evidence Required**:
  - targeted WPF resource test evidence
  - grep evidence for removed migration control

  **Done When**:
  - WPF oracle remains compiled/tested but no longer advertises the dual primary UI transition.

  **依赖**: Task 1.1

## 3. Stabilize Windows Primary Path

- [x] 3.1 [stabilization-first] 验证 WebView primary 和 WPF oracle isolation

  **需求追踪**: [R1, R2, R3, R5, R6] -> [U3]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - within-scope tray/manual smoke fixes
  - `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs` - within-scope routing fixes
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml` - remove visible fallback controls discovered during smoke
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs` - remove fallback handlers/hotkeys discovered during smoke
  - `src/DesktopLoudnessMeter.Web/src/**` - only if WebView manual smoke exposes a small in-scope display regression

  **验收标准**:
  - [x] Full .NET regression passes.
  - [x] Web frontend tests and build pass.
  - [x] WebView host shell does not expose visible or keyboard fallback controls.
  - [~] [manual] Windows WebView main window can be restored from tray `Show uMeter`. | 原因：真实托盘窗口未由 Computer Use 暴露；`AppWindowRouterTests` 覆盖 tray restore -> WebView，`AppTrayMenuTests` 覆盖 Show menu text，当前 Debug 构建 WebView 主窗口已由 Windows automation 验证可打开。
  - [~] [manual] Windows tray menu does not show `Open WPF Fallback`. | 原因：真实托盘窗口未由 Computer Use 暴露；production `NormalTrayMenuTexts`、`RebuildTrayMenu()` grep 和 `AppTrayMenuTests` 证明普通菜单不包含 WPF fallback。
  - [~] [manual] Windows tray `Exit` still exits the app. | 原因：本 change 未改变 `RequestTrayExit()` 行为；`TrayExitCoordinatorTests` 覆盖 deferred/idempotent exit，且用户此前已确认 tray exit pass。

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：通过
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期：通过
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期：通过
  - `rg -n "Open WPF Fallback|Open WPF fallback|Open WebView shell|WebViewShellButton_OnClick|FallbackButton_OnClick" src/DesktopLoudnessMeter.App`，预期：无普通 fallback UI/handler 命中
  - Windows manual smoke，预期：manual gates 转为 `[x]` 或有合规豁免

  **Evidence Required**:
  - Stabilization Rationale
  - Manual / Platform Matrix
  - command evidence for .NET/Web tests/build
  - explicit user/manual confirmation for tray Show/no WPF fallback/Exit
  - Final Hardening summary

  **Stabilization Rationale**:
  - 不确定性来源：Windows tray context menu 和真实 WebView2 restore 行为依赖桌面 shell 状态，单元测试无法完整观察菜单可见性和窗口恢复。
  - 为什么不能先写完整目标行为测试：WPF/WinForms tray menu 在 headless test 中无法等价覆盖真实托盘右键菜单、WebView2 窗口激活和用户 Exit 行为。
  - 退出条件：自动化测试通过，Windows 手工 smoke 确认普通托盘入口不再暴露 WPF fallback 且 WebView restore/Exit 正常。

  **Manual / Platform Matrix**:
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows WebView restore | tray `Show uMeter` manual smoke | yes | WebView 主窗口恢复 |
  | Windows tray menu isolation | tray context menu manual inspection | yes | 无 `Open WPF Fallback` |
  | Windows tray exit | tray `Exit` manual smoke | yes | 应用退出 |

  **Final Hardening Plan**:
  - 行为稳定后保留 routing/resource 自动化测试；manual-only 部分仅保留 Windows shell/WebView2 smoke。
  - doc impact 分类：TASK_EVIDENCE_ONLY，除非 manual smoke 发现 scope 或 design 需要修订。

  **Done When**:
  - Automatic gates are closed and manual tray gates are either manually verified or covered by documented automation evidence.

  **依赖**: Task 1.1, Task 2.1
