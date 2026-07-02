# Design: 2026-06-30-wpf-retirement

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 决策

### D1: 退休普通入口，不删除 WPF oracle

本 change 不删除 WPF UI 文件。`MainWindow`, `DetailWindow`, WPF controls 和相关 ViewModel 仍编译并被测试覆盖，继续承担 regression oracle/fallback 职责。实际用户入口只保留 WebView primary。

对应需求：R1, R3, R5

### D2: 托盘菜单移除 WPF fallback 项

`App.xaml.cs` 的 `RebuildTrayMenu()` 将保留 `Show uMeter`、`Pop-up App...` 和 `Exit`，移除 `Open WPF Fallback` 普通菜单项。`Show uMeter` 和托盘 double-click 继续通过 `_windowRouter.RestoreFromTray()` 恢复 WebView。

对应需求：R1, R2, R5

### D3: 保留 `AppWindowRouter.OpenWpfFallback()` 作为受控 seam

`AppWindowRouter` 继续表达两个边界：WebView primary 和 WPF fallback。普通方法 `ShowStartupWindow`, `RestoreFromTray`, `RestoreFromSecondaryActivation` 必须只调用 WebView；`OpenWpfFallback` 只能由测试或受控内部入口显式调用。

对应需求：R1, R3

### D4: WPF main window 移除迁移按钮

`MainWindow.xaml` 当前包含 `Open WebView shell` chrome button，`MainWindow.xaml.cs` 包含对应 click handler。该按钮属于迁移期双入口提示；本 change 删除按钮和 handler，调整 header column 结构，避免 WPF oracle 反向引导普通用户进入 WebView。

对应需求：R4

### D5: WebView shell 不暴露 fallback 控件

`WebViewShellWindow` 当前属于 Windows WebView host glue，而不是 Web 前端。它不能再通过窗口 context menu、初始化状态面板按钮或默认热键向普通用户暴露 WPF fallback。保留的 fallback 边界只在 `App.OpenWpfFallback()` 和 `AppWindowRouter.OpenWpfFallback()` 内部 seam。

对应需求：R3, R6

## 实施单元

### [U1] Tray and router isolation

- 模块边界：
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/AppWindowRouterTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs`
- 行为：
  - 移除托盘普通菜单中的 WPF fallback 项。
  - 保持 startup/tray/secondary activation -> WebView primary 的 routing regression。
  - 保留显式 fallback seam 的 characterization。

### [U2] WPF oracle chrome cleanup

- 模块边界：
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml`
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/WpfResourcePackagingTests.cs`
- 行为：
  - 删除 WPF chrome 中的 WebView shell button。
  - 删除未使用 click handler。
  - 保持 WPF resources 仍被打包，证明 oracle 仍存在。

### [U3] Verification and manual smoke

- 模块边界：
  - `openspec/changes/2026-06-30-wpf-retirement/evidence.md`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
  - `src/DesktopLoudnessMeter.Web/src/**` 仅允许 manual smoke 中的小修复
- 行为：
  - 跑完整 .NET/Web 验证。
  - Windows 手工确认托盘菜单、WebView restore 和 Exit。
  - 确认 WebView host shell 不显示 WPF fallback 控件。
  - 记录 WPF 仍未删除的后续条件。

## 接口和数据

本 change 不新增 HostBridge DTO、workspace schema 或 native protocol。唯一行为接口是现有 `AppWindowRouter` route seam：

- `ShowStartupWindow()` -> WebView primary
- `RestoreFromTray()` -> WebView primary
- `RestoreFromSecondaryActivation()` -> WebView primary
- `OpenWpfFallback()` -> explicit WPF fallback only

## 风险和缓解

- 风险：用户失去从 UI 直接打开 WPF fallback 的能力。
  - 缓解：WPF fallback 仍保留内部 seam 和测试覆盖；若发布版仍需要 debug-only fallback，必须另开 change 定义触达、安全和验收。
- 风险：误删 WPF oracle 资源导致 regression 对照失效。
  - 缓解：保留 WPF resource packaging / view model tests，并禁止本 change 删除 WPF resource/control/view model 文件。
- 风险：托盘菜单手工验证依赖真实 Windows shell。
  - 缓解：用自动化测试覆盖路由/退出协调；manual gate 仅覆盖真实托盘可见行为。

## 不采用方案

- 删除 WPF 项目或 XAML 文件：过早，WebView workflow parity 尚未覆盖完整设置/别名/排序/布局导入导出。
- 保留托盘 fallback 但标成 debug：仍是普通用户可见入口，会延长双主 UI 状态。
- 借本 change 实现 WebView full settings parity：范围过大，应独立成 `webview-workflow-parity` 或 settings workflow change。
