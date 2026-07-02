# Spec: webview-packaging-startup-tray

## 新增需求

### 需求：Windows startup defaults to the WebView shell
**Trace**: R1
**Slice**: startup/default-window
Windows primary instance 正常启动后必须默认显示 WebView shell，而不是 WPF main window。

#### 场景：primary startup shows WebView
- **当** primary instance 完成 single-instance ownership、settings、source service 和 tray 初始化
- **那么** app 必须创建并显示 `WebViewShellWindow`
- **并且** WebView shell 必须复用 primary app 已创建的 `ISignalSourceMeterService` 和 `UserWorkspaceSettings`

#### 场景：WPF fallback is not constructed as the default startup window
- **当** app 正常启动且没有显式请求 fallback
- **那么** WPF `MainWindow` 不得作为默认可见主窗口显示

### 需求：WPF remains an explicit fallback
**Trace**: R2
**Slice**: fallback/wpf
WPF UI 必须保留为显式 fallback 和 regression oracle。

#### 场景：fallback opens on demand
- **当** 用户通过 tray 或 WebView fallback 入口请求 WPF fallback
- **那么** app 必须创建或激活 `MainWindow`
- **并且** fallback 必须使用同一个 source service、workspace settings 和 `MainViewModel` graph

#### 场景：fallback close or hide does not exit app
- **当** WPF fallback 被隐藏或关闭
- **那么** app 不得因为 fallback window lifecycle 退出

### 需求：Tray and activation target WebView first
**Trace**: R3
**Slice**: tray/activation
托盘 restore、double-click 和 secondary launch activation 必须恢复 WebView 主窗口。

#### 场景：tray show restores WebView
- **当** 用户点击 tray “Show uMeter” 或双击 tray icon
- **那么** app 必须显示并激活 WebView shell

#### 场景：secondary launch activates WebView
- **当** secondary instance 成功向 primary instance 发送 activation
- **那么** primary instance 必须显示并激活 WebView shell

#### 场景：tray still exposes fallback and exit
- **当** 用户打开 tray menu
- **那么** menu 必须包含 WebView show、WPF fallback 和 Exit 行为

### 需求：Single-instance startup order remains protected
**Trace**: R4
**Slice**: startup/single-instance
single-instance lock 必须继续早于 receivers、settings writers、tray icons 或 windows。

#### 场景：secondary launch exits before app graph creation
- **当** single-instance lock acquisition fails
- **那么** secondary instance 必须请求 activation 并退出
- **并且** 不得创建 source receivers、tray icons、WebView shell 或 WPF fallback

### 需求：Publish and installer payload include Web assets and native runtime dependencies
**Trace**: R5
**Slice**: packaging/web-assets
发布/安装路径必须携带 WebView shell 运行所需的 frontend build output 和 app 启动所需的 native runtime dependency。

#### 场景：app publish contains Web assets
- **当** app project publish completes
- **那么** publish output 必须包含 `Web/index.html`
- **并且** publish output 必须包含 Vite built asset files under `Web/assets/`

#### 场景：installer payload preserves Web assets
- **当** installer packaging route builds payload
- **那么** payload 或 extracted app publish output 必须包含 `Web/index.html` 和 `Web/assets/`

#### 场景：app publish contains native runtime dependency
- **当** app project publish completes
- **那么** publish output 必须包含 `DesktopLoudnessMeter.Native.dll`
- **并且** published app startup 不得因为 missing native DLL 在创建 source service 前崩溃

#### 场景：installer payload preserves native runtime dependency
- **当** installer packaging route builds payload
- **那么** payload 或 extracted app publish output 必须包含 `DesktopLoudnessMeter.Native.dll`

#### 场景：VST3 packaging toolchain is unavailable
- **当** JUCE/CMake route cannot run in the current environment
- **那么** verification may use `-SkipVst3Plugin` for app/Web payload validation
- **并且** evidence 必须明确记录 full VST3 installer route was not run

### 需求：WebView startup failure has an observable fallback path
**Trace**: R6
**Slice**: runtime/fallback
WebView2 runtime 或 Web asset failure 不得让用户失去可见 UI 入口。

#### 场景：WebView initialization fails
- **当** WebView shell fails to initialize
- **那么** shell 必须显示可见错误状态
- **并且** app 必须仍允许打开 WPF fallback

## 修改需求
无。

## 移除需求
无。
