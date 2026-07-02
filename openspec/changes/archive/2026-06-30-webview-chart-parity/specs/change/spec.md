# Spec: 2026-06-30-webview-chart-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求: Host detail chart module projection
**Trace**: R1
**Slice**: host-chart-contract
Host detail snapshot 必须为 selected source 暴露 Web UI 可消费的 chart module projection，包括 module kind、title、order、visibility、height 和对应 chart settings。

#### 场景: Web UI receives chart modules for selected detail
- **当** Host snapshot 存在 selected detail source
- **那么** `selectedDetail.modules` 包含 waveform、spectrum、stereo、loudnessClock、collision 的稳定 module entries
- **并且** 每个 module 的 visibility 来源于 workspace/default settings，而不是 WPF control state

#### 场景: Host detail handles missing chart data
- **当** selected source 没有某类 chart payload
- **那么** Host snapshot 仍保留该 module entry
- **并且** DTO 中的数据字段为空或 empty collection，由 Web UI 渲染 empty state

### 需求: HostBridge chart setting commands
**Trace**: R2
**Slice**: host-chart-commands
HostBridge v1 command bus 必须支持 chart settings 和 module visibility 的 typed command payload validation。

#### 场景: Web UI updates waveform display mode
- **当** Web UI 发送 `setWaveformDisplayMode` 并提供有效 source identity 与 display mode
- **那么** Host 执行现有 analysis setting forwarding
- **并且** command result 返回包含新 display mode 的 snapshot

#### 场景: Web UI updates module visibility
- **当** Web UI 发送 chart module visibility command
- **那么** Host 更新对应 source 的 inline detail visibility setting
- **并且** command result 返回更新后的 selected detail modules

#### 场景: Invalid chart command payload is rejected
- **当** HostBridge 收到缺少 source key 或 enum 值非法的 chart command
- **那么** response 为 typed invalid request/error
- **并且** Host snapshot 不因该请求改变

### 需求: Frequency collision DTO projection
**Trace**: R3
**Slice**: host-collision-projection
Host 必须为 selected source 投影 frequency collision DTO，Web UI 禁止依赖 WPF ViewModel instance 或 WPF controls。

#### 场景: Collision bands are projected from source spectra
- **当** selected source 与 comparison source 都有 spectrum buckets
- **那么** Host DTO 包含 compact collision bands、scale、threshold settings 和 comparison source identity

#### 场景: Collision projection handles absent comparison source
- **当** collision comparison source 缺失或没有 spectrum buckets
- **那么** Host DTO 返回空 collision bands
- **并且** Web UI 可以显示 empty state 而不抛出异常

### 需求: Web waveform chart
**Trace**: R4
**Slice**: web-waveform-chart
Web UI 必须使用 Canvas 渲染 selected source 的 waveform chart。

#### 场景: Overall waveform renders from samples
- **当** selected source 提供 `waveformSamples`
- **那么** waveform module canvas 绘制非空波形
- **并且** readout/labels 不与 canvas 或 controls 重叠

#### 场景: Channel waveform renders from channel samples
- **当** selected source display mode 为 `channels` 且提供 `channelWaveformSamples`
- **那么** waveform module 显示左右声道或可用声道的独立轨迹

### 需求: Web spectrum chart
**Trace**: R5
**Slice**: web-spectrum-chart
Web UI 必须使用 Canvas 渲染 selected source 的 spectrum chart。

#### 场景: Overall spectrum renders from buckets
- **当** selected source 提供 `spectrumBuckets`
- **那么** spectrum module canvas 绘制非空频谱柱或曲线
- **并且** floor/ceiling settings 影响垂直映射

#### 场景: Channel spectrum renders from channel buckets
- **当** selected source display mode 为 `channels` 且提供 `channelSpectrumBuckets`
- **那么** spectrum module 显示左右声道或可用声道的独立频谱

### 需求: Web stereo image chart
**Trace**: R6
**Slice**: web-stereo-chart
Web UI 必须使用 Canvas 渲染 selected source 的 stereo image chart。

#### 场景: Stereo points render in polar field
- **当** selected source 提供 `stereoImagePoints`
- **那么** stereo module canvas 绘制 stereo field 与 points
- **并且** correlation/readout 与图表保持一致

### 需求: Web loudness clock chart
**Trace**: R7
**Slice**: web-loudness-clock-chart
Web UI 必须使用 Canvas 渲染 selected source 的 loudness clock chart。

#### 场景: Loudness clock accumulates selected source history
- **当** selected source 持续返回 momentary loudness 和 frame updates
- **那么** loudness clock module 按 configured cycle 绘制当前 source 的 loudness history

#### 场景: Loudness clock resets on source switch
- **当** selected detail source 改变
- **那么** Web UI 清理旧 source 的 loudness clock runtime history
- **并且** 新 source 从新的 history buffer 开始绘制

### 需求: Web frequency collision chart
**Trace**: R8
**Slice**: web-collision-chart
Web UI 必须使用 Canvas 渲染 Host DTO 中的 frequency collision bands。

#### 场景: Collision bands render with configured scale
- **当** Host DTO 提供 collision bands 和 frequency scale
- **那么** collision module canvas 按 linear/log scale 绘制 collision intensity bands

#### 场景: Collision empty state is explicit
- **当** Host DTO 没有 collision bands
- **那么** collision module 显示稳定 empty state
- **并且** canvas/layout 尺寸不因 empty state 跳动

### 需求: Windows WebView chart smoke
**Trace**: R9
**Slice**: windows-visual-smoke
Windows WebView chart path 必须通过 visual/manual smoke，并保留 WPF fallback。

#### 场景: WebView renders chart modules without layout overlap
- **当** Windows WebView 主窗口加载包含 chart payload 的 snapshot
- **那么** detail 中的 chart modules 非空、未互相遮挡、controls 可操作

#### 场景: WPF fallback remains available
- **当** WebView chart path 已实现
- **那么** WPF fallback/regression oracle 不被移除
- **并且**既有 WPF chart/control tests 仍通过

## 修改需求

无。

## 移除需求

无。
