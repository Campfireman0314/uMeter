# Spec: 2026-06-29-webview-windows-app-shell

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求:Windows WebView shell 入口
**Trace**: R1
**Slice**: webview-shell/windows-host
Windows app 必须提供可运行的 WebView2 shell 入口，并保留 WPF UI 作为 fallback。

#### 场景:打开 WebView shell
- **当** 用户从 Windows app 启动 WebView shell
- **那么** app 必须创建 WebView2 host window 或等效 shell，并加载 Web frontend asset

#### 场景:保留 WPF fallback
- **当** WebView shell 存在
- **那么** 现有 WPF main window、startup、single-instance、tray 和 WPF regression tests 仍必须保持可用

### 需求:HostBridge v1 transport
**Trace**: R2
**Slice**: webview-shell/hostbridge-transport
HostBridge v1 必须使用 native WebView message channel 作为 transport，并用 JSON DTO envelope 交换 request/response/event。

#### 场景:获取初始 snapshot
- **当** Web UI 初始化并请求 host snapshot
- **那么** bridge 必须返回可 JSON 序列化的 host snapshot DTO

#### 场景:处理 bridge 错误
- **当** Web UI 发送未知、格式错误或无法执行的 request
- **那么** bridge 必须返回 typed error response，而不是让 WebView 或 host 崩溃

### 需求:React/Vite 基础 UI
**Trace**: R3
**Slice**: webview-shell/frontend
Web UI 必须使用 TypeScript + React + Vite，并渲染基础 host snapshot。

#### 场景:显示 source overview
- **当** Web UI 收到 host snapshot
- **那么** UI 必须显示 status、source list、source title/identity/readout 和空/无 source 状态

#### 场景:显示 recent/detail/settings shell
- **当** Web UI 收到 recent events、selected detail 或 settings snapshot
- **那么** UI 必须显示 recent event 列表、基础 detail shell 和主题/设置入口

### 需求:基础 host command workflow
**Trace**: R4
**Slice**: webview-shell/commands
Web UI 必须能通过 HostBridge v1 发出基础 typed host commands，并显示 command result/error。

#### 场景:刷新 snapshot
- **当** 用户触发刷新
- **那么** Web UI 必须通过 bridge 执行 refresh command，并用返回 snapshot 更新 UI

#### 场景:选择和返回 source detail
- **当** 用户选择 source 或返回全局视图
- **那么** Web UI 必须通过 bridge 执行对应 command，并显示 selected detail 或 global state

#### 场景:显示 command error
- **当** command 返回 typed error
- **那么** Web UI 必须显示可理解的 error 状态，并保留最近一次有效 UI 状态或显示 safe empty state

### 需求:范围守护和回归
**Trace**: R5
**Slice**: webview-shell/regression-guard
本 change 必须保护现有 WPF/startup/tray/host contract 行为，并明确不实施 packaging、chart parity、settings schema、native core 或 macOS。

#### 场景:运行自动化回归
- **当** implementation 完成
- **那么** .NET host/bridge/WPF regression tests 和 Web frontend test/build 必须通过，或 evidence 记录非本 change 引入的环境限制

#### 场景:执行 Windows manual smoke
- **当** 归档前进行人工验证
- **那么** 必须确认 WebView shell 可打开、显示 snapshot、执行至少一个 command，且 WPF fallback 仍可用

#### 场景:发现 scope creep
- **当** implementation 需要接管 installer/startup/tray、复杂 chart parity、settings migration、native core 或 macOS runner 才能继续
- **那么** 必须停止并回到 opsx-plan 拆分或改由后续 change 处理

## 修改需求

无。

## 移除需求

无。
