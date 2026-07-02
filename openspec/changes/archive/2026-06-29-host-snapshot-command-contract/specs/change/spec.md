# Spec: 2026-06-29-host-snapshot-command-contract

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求：Host snapshot aggregate
**Trace**: R1
**Slice**: host/snapshot
系统必须提供 UI-neutral 的 host snapshot aggregate，包含全局状态、source rows、selected detail、recent events、settings projection 和 command feedback 所需的稳定数据。

#### 场景：Flutter 或 WPF adapter 读取完整状态
- **当** host controller 完成一次 refresh 或 command 后
- **那么** 调用方可以读取一个 immutable snapshot，并且该 snapshot 不要求调用方引用 WPF/XAML-only 类型

#### 场景：状态投影包含设置入口
- **当** workspace settings 已存在 theme、recent event limit、event output folder 或 per-source preferences
- **那么** host snapshot 必须暴露当前 UI workflow 所需的 projection，同时不改变现有 JSON 持久化格式

### 需求：Source and detail semantics
**Trace**: R2
**Slice**: host/source-detail-projection
Host projection 必须保留现有 source identity、sort order、hide/show filtering、alias/effective title、missing detail retention、metric/readout 和 graph frame semantics。

#### 场景：source key 作为稳定身份
- **当** source display name 或 alias 改变
- **那么** host projection 仍必须以 stable source key 作为持久化和 command target 身份

#### 场景：detail source 暂时消失
- **当** selected detail source 从 service snapshot 中消失但仍应保留 detail context
- **那么** host projection 必须表达 missing/instance-not-found 状态，并允许后续同一 identity 或可恢复实例重新绑定

#### 场景：graph frame 数据刷新
- **当** `SignalSourceSnapshot` 提供 waveform/spectrum/stereo/loudness metric 和 `PlotFrameVersion`
- **那么** host projection 必须保留足够数据让 UI client 判断图表刷新，而不读取 analyzer 内部状态

### 需求：Command request and result vocabulary
**Trace**: R3
**Slice**: host/commands
系统必须定义 typed host command request 和 typed result，覆盖现有 source/detail/settings 用户操作，并以可消费错误结果表达失败或不可执行状态。

#### 场景：source command 成功
- **当** UI client 请求 reset、select detail、alias、hide、restore、move/reorder、forget source 或 analysis setting 更新
- **那么** host controller 必须更新状态或转发到现有 source/settings service，并返回 success result 与最新 snapshot

#### 场景：source command 不可执行
- **当** command target source 不存在、已隐藏状态不匹配、参数无效或服务层拒绝操作
- **那么** host controller 不得要求 UI 捕获底层异常，必须返回 typed error result、错误 code 和保持一致的 snapshot

#### 场景：全局 command 成功
- **当** UI client 请求 refresh、back to global、reset all、theme/settings 或 performance mode 更新
- **那么** host controller 必须更新全局状态或转发现有 service，并在 result 中反映 command 后状态

### 需求：WPF adapter parity
**Trace**: R4
**Slice**: wpf/adapter-regression
WPF `MainViewModel` 必须作为 host contract client 继续提供现有 binding 属性、commands 和 user-visible behavior。

#### 场景：现有 ViewModel regression
- **当** 现有 `MainViewModelTests`、`MainViewModelAdvancedControlsTests`、`AudioSessionMeterViewModelTests` 和 `DetailWindowViewModelTests` 运行
- **那么** 与 source reconciliation、recent events、settings persistence、detail controls、alias/hide/order 和 graph settings 相关的行为必须继续通过

#### 场景：WPF command wrapper
- **当** WPF command 执行
- **那么** command wrapper 必须调用 host command，并将 result/snapshot 同步回 WPF binding state，而不是复制一套独立业务逻辑

### 需求：Boundary guardrails
**Trace**: R5
**Slice**: architecture/boundary
本 change 必须保持 host contract 边界，不得实现 Flutter shell、native/core build、source ingest migration、settings schema migration、packaging 或 JUCE/protocol 改动。

#### 场景：文件范围检查
- **当** review 检查本 change diff
- **那么** diff 不得包含 `DesktopLoudnessMeter.Native`、JUCE sender、Protocol、SenderKit、Setup 或 Flutter runner/project 的产品实现修改

#### 场景：schema 兼容
- **当** host snapshot 暴露 settings projection
- **那么** 只能读取/写回现有 `UserWorkspaceSettings` 兼容字段；如需版本化 schema，必须回到 plan 并拆到 `settings-workspace-schema`

## 修改需求

无。

## 移除需求

无。
