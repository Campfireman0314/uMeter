# Spec: 2026-06-29-ui-runtime-architecture-replan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求:WebView 主 UI runtime
**Trace**: R1
**Slice**: ui-runtime-replan/runtime-decision
规划必须把主 UI runtime 从旧 Flutter 路线修订为 WebView 路线，并禁止后续实施继续以 Flutter 作为主 UI runtime。

#### 场景:修订旧规划
- **当** 阅读本 change 的 work/design
- **那么** 可以看到 `2026-06-29-cross-platform-architecture-plan` 中的 Flutter UI runtime 是历史规划，当前主路径已修订为 WebView UI

#### 场景:阻止 Flutter implementation route
- **当** 后续 change 准备创建主 UI shell
- **那么** 规划必须要求使用 WebView2/WKWebView 边界，而不是创建 Flutter runner

### 需求:HostBridge v1 和 runner 边界
**Trace**: R2
**Slice**: ui-runtime-replan/hostbridge-runner
规划必须定义 Windows WebView2、macOS WKWebView 预留边界、HostBridge v1 transport、typed command bus 和 JSON DTO payload 的关系。

#### 场景:Windows 主路径
- **当** 后续实现 Windows 主窗口
- **那么** 必须以 WebView2 承载主 UI，并通过 native WebView message channel 连接 HostBridge v1

#### 场景:macOS 预留路径
- **当** 规划描述 macOS
- **那么** 必须把 WKWebView runner、native message transport equivalent、capture adapter 和 packaging 写成后续待验证边界，而不是宣称已完成

#### 场景:Bridge 分层
- **当** 规划描述 HostBridge v1
- **那么** 必须区分 native WebView message transport、typed command bus 和 JSON DTO payload

### 需求:Web UI 技术栈和渲染分层
**Trace**: R3
**Slice**: ui-runtime-replan/web-frontend
规划必须定义 Web UI 使用 TypeScript + React + Vite，并区分 DOM UI 与 Canvas/WebGL 高频图表职责。

#### 场景:普通 UI
- **当** 规划描述 source list、settings、status、recent events 和用户工作流
- **那么** 必须要求 Web 前端使用 DOM component 承担交互、主题、设置和普通窗口内容

#### 场景:高频图表
- **当** 规划描述 waveform、spectrum、loudness clock 或 frequency collision
- **那么** 必须要求后续 chart parity change 使用 Canvas/WebGL 或等效高频渲染路径，而不是把复杂图表塞进基础 shell

### 需求:Core/Host/Sender/Protocol/WPF 边界
**Trace**: R4
**Slice**: ui-runtime-replan/layer-boundaries
规划必须明确 C++ Core、Host glue、JUCE sender、Protocol 和 WPF fallback/regression oracle 的职责。

#### 场景:C++ Core 边界
- **当** 规划描述 core
- **那么** C++ Core 必须承担 DSP、协议接入、平台无关 source ingest 状态机、snapshot generation 和核心状态机

#### 场景:Host glue 边界
- **当** 规划描述 host
- **那么** Host 只能保留 Windows/macOS runner、WebView 宿主、HostBridge v1、平台 capture adapter、托盘、启动、单实例、安装包等 glue

#### 场景:Sender 和 Protocol
- **当** 规划描述 JUCE sender 和 Protocol
- **那么** JUCE sender 必须保持 DAW 发送端职责，Protocol 必须保持版本化 lifecycle vocabulary

#### 场景:WPF fallback
- **当** WebView UI 尚未达到 parity
- **那么** WPF 必须保留为 fallback 和 regression oracle，退休或隔离只能由后续 `wpf-retirement` change 决定

### 需求:后续 change map 和 OpsX 生命周期
**Trace**: R5
**Slice**: ui-runtime-replan/change-map
规划必须输出 WebView + C++ Core 后续模块化 changes，并明确每个 change 都需要独立经过 OpsX/OpenSpec 生命周期。

#### 场景:后续 change 独立闭环
- **当** 用户选择任一推荐模块继续推进
- **那么** 该模块必须有清晰范围、不包含项、依赖和 work 关注点，并独立执行 plan/readiness/implement/verify/review/archive

#### 场景:避免把模块完成误判为 goal 完成
- **当** 单个后续模块完成归档
- **那么** 规划必须说明它不是整体 goal 完成；整体 goal 需要最终架构真实落地并通过 completion audit

#### 场景:发现切分不足
- **当** readiness 或后续 planning 发现某个推荐 change 过大、过小或事实不足
- **那么** 必须回到 opsx-explore 或 opsx-plan 修订，而不是硬进入 implementation

## 修改需求

无。

## 移除需求

无。
