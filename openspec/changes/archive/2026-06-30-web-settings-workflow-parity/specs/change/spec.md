# Spec: 2026-06-30-web-settings-workflow-parity

正文默认使用中文；路径、命令、配置键和状态枚举保留原文。

## 新增需求

### 需求: Web settings surface
**Trace**: R1
**Slice**: web-settings/surface

Web UI 必须提供可打开/关闭的 settings surface，展示当前 `schemaVersion`、theme、recent limit、event output folder 和 hidden-source visibility。

#### 场景: 打开设置
- **当** 用户点击 top bar 的 `Settings` 控件
- **那么** Web UI 显示 settings surface
- **并且** surface 中显示当前 Host snapshot settings 值

### 需求: Global settings commands
**Trace**: R2
**Slice**: web-settings/commands

Web UI 必须通过 HostBridge v1 typed command bus 修改全局 settings，并在命令完成后使用 post-command snapshot 刷新 UI。

#### 场景: 修改全局设置
- **当** 用户修改 theme、recent limit、event output folder 或 hidden-source visibility
- **那么** Web UI 发送对应 HostBridge command
- **并且** 成功结果中的 snapshot 成为新的 UI 状态

### 需求: HostBridge settings DTOs
**Trace**: R3
**Slice**: web-settings/hostbridge

HostBridge v1 必须支持 `setTheme`、`setRecentEventLimit` 和 `setEventOutputFolder` 命令，并对缺失或越界 payload 返回 typed invalid request error。

#### 场景: 有效 payload
- **当** WebView host 收到有效 settings command
- **那么** Host controller 持久化 workspace 设置
- **并且** response kind 为 `commandResult`

#### 场景: 无效 payload
- **当** WebView host 收到缺失或越界 settings payload
- **那么** response kind 为 `error`
- **并且** code 为 `invalidRequest`
- **并且** workspace/snapshot 不被修改

### 需求: Web theme application
**Trace**: R4
**Slice**: web-settings/theme

Web shell 必须根据 `settings.themeId` 应用 Web theme token；未知或空 theme 必须 fallback 到稳定默认主题。

#### 场景: 应用主题
- **当** Host snapshot 中的 `themeId` 为已知主题 id
- **那么** Web shell 使用对应 `data-theme` 或等效 token

#### 场景: fallback 主题
- **当** Host snapshot 中的 `themeId` 为空或未知
- **那么** Web shell 使用默认 theme token

### 需求: Regression coverage
**Trace**: R5
**Slice**: web-settings/regression

本 change 必须用 Web tests、TypeScript build 和 .NET HostBridge tests 覆盖 settings workflow，不能只依赖人工截图。

#### 场景: 自动化验证
- **当** 执行 change 验证命令
- **那么** Web settings interaction、HostBridge settings command mapping、invalid payload 和 build 均被覆盖

## 修改需求

无。

## 移除需求

无。
