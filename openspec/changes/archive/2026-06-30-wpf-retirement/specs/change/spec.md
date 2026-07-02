# Spec: 2026-06-30-wpf-retirement

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求: WebView primary activation remains exclusive
**Trace**: R1
**Slice**: app-startup/window-routing
Windows 桌面应用的普通启动、托盘恢复和二次实例激活必须恢复 WebView 主窗口，禁止在这些路径中打开 WPF fallback。

#### 场景: 普通启动显示 WebView
- **当** 应用完成单实例初始化并调用 startup window routing
- **那么** route 必须调用 WebView primary action

#### 场景: 托盘恢复显示 WebView
- **当** 用户通过托盘 double-click 或 `Show uMeter` 恢复窗口
- **那么** route 必须调用 WebView primary action

#### 场景: 二次实例激活显示 WebView
- **当** 已运行实例收到 secondary activation request
- **那么** route 必须调用 WebView primary action

### 需求: WPF fallback is removed from normal tray menu
**Trace**: R2
**Slice**: app-startup/tray-menu
Windows 托盘菜单必须只暴露普通用户工作流入口，禁止显示 `Open WPF Fallback` 或等价 WPF fallback 菜单项。

#### 场景: 托盘菜单构建
- **当** host 构建托盘 context menu
- **那么** 菜单项必须包含 WebView 恢复入口和 Exit
- **那么** 菜单项禁止包含普通用户可见的 WPF fallback 入口

### 需求: WPF fallback remains an internal oracle seam
**Trace**: R3
**Slice**: app-startup/wpf-oracle
WPF fallback 必须保留受控内部 seam，用于 regression oracle、开发期诊断或后续退休对照，禁止由普通启动、托盘恢复或二次激活路径隐式触发。

#### 场景: 显式内部调用 fallback
- **当** 测试或受控内部代码显式调用 WPF fallback seam
- **那么** route 可以调用 WPF fallback action

#### 场景: 普通路径不会触发 fallback
- **当** startup、tray restore 或 secondary activation 被调用
- **那么** route 禁止调用 WPF fallback action

### 需求: WPF oracle window stops advertising WebView migration controls
**Trace**: R4
**Slice**: wpf-oracle/main-window
WPF main window 作为 fallback/regression oracle 时，必须去除迁移期的 `Open WebView shell` 用户按钮或等价双主入口提示。

#### 场景: WPF fallback 编译资源
- **当** WPF main window XAML 被编译和打包
- **那么** 它禁止包含 `Open WebView shell` 用户按钮文本或 tooltip

### 需求: Retirement evidence covers both primary path and oracle preservation
**Trace**: R5
**Slice**: verification/regression
归档前必须验证 WebView primary path、托盘入口隔离和 WPF oracle 保留均未退化。

#### 场景: 自动化验证
- **当** 运行本 change 的自动化测试命令
- **那么** routing、tray exit、WPF resource packaging、Host/WebView 测试必须通过或在 evidence 中记录非本 change 导致的允许失败

#### 场景: Windows manual smoke
- **当** 在 Windows 上启动应用并打开托盘菜单
- **那么** WebView 主窗口必须可恢复
- **那么** 托盘菜单禁止显示 WPF fallback 普通入口
- **那么** Exit 必须仍可退出应用

### 需求: WebView host shell does not expose WPF fallback controls
**Trace**: R6
**Slice**: webview-shell/fallback-entry
WebView host shell 必须去除普通用户可见或键盘可触达的 WPF fallback 控件，禁止通过 WebView shell context menu、初始化面板按钮或默认热键打开 WPF fallback。

#### 场景: WebView shell 编译资源
- **当** WebView shell XAML 和 code-behind 被编译
- **那么** 它禁止包含 `Open WPF fallback` 用户菜单项或按钮
- **那么** 它禁止包含 `FallbackButton_OnClick` fallback handler

#### 场景: WebView shell 可访问 UI
- **当** 当前 Debug 构建启动 WebView 主窗口
- **那么** 可访问 UI 文本禁止显示 `Open WPF fallback`

## 修改需求

无。

## 移除需求

无。
