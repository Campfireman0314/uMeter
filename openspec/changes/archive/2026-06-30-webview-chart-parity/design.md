# Design: 2026-06-30-webview-chart-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## 上下文

已证实事实：
- `DesktopMeterHostController.BuildSnapshot` 当前 selected detail 使用 `Array.Empty<HostDetailModuleSnapshot>()`，并把 waveform/spectrum/loudness/collision visibility 固定为 false，stereo 固定为 true。
- `HostSourceSnapshot` 和 Web `HostSourceSnapshotDto` 当前已有 `waveformSamples`、`channelWaveformSamples`、`spectrumBuckets`、`channelSpectrumBuckets`、`stereoImagePoints` 和 `plotFrameVersion`。
- Web `AppShell` detail 当前渲染 metric readouts 和三个 placeholder 文本，不包含 Canvas chart components。
- `UserWorkspaceSettings` 已存在 inline/popup detail module visibility、spectrum range、loudness clock range/cycle、collision thresholds/scale/source 等字段。
- WPF 侧已有 waveform/spectrum/stereo/loudness/collision controls 和相关 tests，可作为 behavior oracle。

推断：
- Web UI 可以先用 Canvas 消费现有 source arrays，实现 waveform/spectrum/stereo 的主要 parity。
- Loudness clock 不需要 Host 持久化 history；Web runtime 可以按 selected source、cycle seconds 和 `plotFrameVersion` 管理短期显示 history。
- Frequency collision 需要 Host 投影 compact bands，避免 Web 依赖 WPF ViewModel。

## 目标 / 非目标

**目标：**
- 让 HostBridge v1 成为 WebView 图表 UI 的主要消费边界。
- 让 WebView detail shell 渲染 waveform、spectrum、stereo image、loudness clock 和 frequency collision。
- 保留 WPF fallback，并用既有 WPF tests/controls 作为 regression oracle。

**非目标：**
- 不实现 native DSP/core migration。
- 不改变 JUCE sender 或 Protocol。
- 不退休 WPF。
- 不新增 workspace schema version。
- 不实现 macOS runner。

## 需求追踪

- [R1] -> [U1, U4]
- [R2] -> [U2, U4]
- [R3] -> [U1, U3]
- [R4] -> [U3, U4]
- [R5] -> [U3, U4]
- [R6] -> [U3, U4]
- [R7] -> [U3, U4]
- [R8] -> [U3, U4]
- [R9] -> [U5]

## 实施单元

### [U1] Host chart snapshot projection
- 关联需求: [R1, R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs`
- 设计:
  - `HostDetailSnapshot` 增加或填充 module projection，不使用 WPF control state。
  - Chart settings 从 `UserWorkspaceSettings` 现有字段投影，缺省值与 WPF ViewModel 常量保持一致。
  - Collision projection 复用或提取 host-neutral analyzer helper，输出 DTO bands。
- 验证方式:
  - Host tests 覆盖 selected detail modules、visibility defaults、workspace overrides。
  - DTO mapper tests 覆盖 chart module/settings/collision serialization。
- 知识沉淀:
  - 归档后更新 host-contract/webview-shell codemap，记录 chart DTO boundary。

### [U2] HostBridge chart command mapping
- 关联需求: [R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeRequest.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostCommandTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
- 设计:
  - 新增 command names/payloads：module visibility、waveform/spectrum display mode、spectrum range、loudness clock settings、collision settings。
  - C# payload validation 拒绝非法 enum、缺失 source key 和越界 numeric values。
  - Command result 始终携带 fresh snapshot。
- 验证方式:
  - C# tests 覆盖 success round-trip 和 invalid payload rejection。

### [U3] Web chart renderers and runtime state
- 关联需求: [R3, R4, R5, R6, R7, R8]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/charts/**`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.ts`
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.tsx`
- 设计:
  - 纯函数负责坐标映射、bucket normalization、band mapping 和 empty-state 判断。
  - React components 使用 `canvas` 和 `ResizeObserver` 或稳定容器尺寸绘制。
  - Loudness clock history 留在 Web runtime，以 source key 和 cycle settings 分区。
  - Rendering utilities 不访问 DOM 之外的 host globals，便于 unit tests。
- 验证方式:
  - Vitest 覆盖 pure mapping、history reset、empty state 和 command payload construction。

### [U4] Web detail workflow and controls
- 关联需求: [R1, R2, R4, R5, R6, R7, R8]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.ts`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.test.ts`
- 设计:
  - Detail shell 渲染 module list，而不是固定 placeholder grid。
  - Controls 使用 select/toggle/input 等原生/React UI，走 HostBridge typed commands。
  - 保持紧凑 dashboard 风格，避免说明性大段文本。
  - Stable dimensions 防止 empty/loading/hover 改变布局。
- 验证方式:
  - React tests 覆盖模块渲染、empty state、settings command dispatch、snapshot update。

### [U5] Verification, visual smoke, and fallback guard
- 关联需求: [R9]
- 模块边界:
  - `openspec/changes/2026-06-30-webview-chart-parity/evidence.md`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**`
  - `tests/DesktopLoudnessMeter.Tests/Controls/**`
- 设计:
  - 自动化先覆盖 DTO/commands/rendering invariants。
  - Windows WebView manual smoke 记录截图或用户确认。
  - 若 visual smoke 发现布局或 DPI 问题，只做本 change 内的 Web chart/layout 修正。
- 验证方式:
  - .NET tests、Web tests/build、boundary grep、manual smoke。

## 决策

1. **Collision bands 由 Host 投影。**
   - 原因：collision 需要跨 source 比较和 workspace threshold/scale；如果 Web 自行推断，会复制业务逻辑并绕过 Host contract。
   - 影响：Host DTO 变大，但 Web renderer 保持纯显示。

2. **Loudness clock history 留在 Web runtime。**
   - 原因：clock 是显示历史，不是 core truth；Host snapshot 已有当前 momentary loudness 和 frame token。
   - 影响：source switch、cycle change 和 frame discontinuity 必须清理 Web history。

3. **Canvas 优先，WebGL 暂不引入。**
   - 原因：当前图表复杂度和数据量可由 Canvas 覆盖，WebGL 会增加测试和兼容成本。
   - 影响：若 manual smoke 发现性能不足，记录为 stabilization finding 并回到 design 修订。

4. **WPF controls 不作为 Web API。**
   - 原因：跨平台目标要求 HostBridge UI-neutral。
   - 影响：可复用算法/helper，但不能把 WPF control/viewmodel instance 暴露给 Web。

## 风险 / 权衡

- Scope 偏大：通过 strict-change、少量明确 task 和 manual gate 控制，不把 native/schema/WPF retirement 混入。
- Collision 复用风险：现有 analyzer 与 `AudioSessionMeterViewModel` 绑定较紧，可能需要提取 helper；若提取会改变 WPF 行为，必须用现有 ViewModel/Controls tests 锁定。
- Visual test 局限：Canvas pixel assertions 在 jsdom 中有限，必须结合 pure function tests 和 Windows manual smoke。
- Live source 不稳定：manual smoke 可使用 live WASAPI/JUCE source 或 mock/diagnostic source，但 evidence 必须说明输入来源。

## 稳定化策略

- `[stabilization-first]` 适用单元：[U5]
- doc impact 处理：TASK_EVIDENCE_ONLY；若 implementation 发现 schema/scope 变化，升级为 DESIGN_AMENDMENT 或 SCOPE_CHANGE。
- final hardening：完成 .NET/Web automated tests、Web build、boundary grep、Windows manual smoke；manual gate 归档前必须关闭。

## 回退方案

删除新增 chart DTO/commands/components/tests，恢复 Web detail placeholder。由于不做 schema migration，用户数据不需要回滚。若只需要临时规避 Web chart bug，可以保留 Host DTO 并通过 module visibility/feature guard 隐藏 Web chart components，WPF fallback 继续可用。

## 并行 / Subagent 边界

- 默认串行：Host DTO/commands 与 Web DTO types 高耦合，先串行收敛。
- 可并行 lane：只读调查 WPF controls 或 Web renderer visual references。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后更新：
- `.aiknowledge/codemap/webview-shell.md`：Web detail charts、Canvas boundary、manual smoke。
- `.aiknowledge/codemap/host-contract.md`：chart DTO/command/collision projection boundary。
- `.aiknowledge/codemap/analysis-rendering.md`：Web Canvas parity 与 WPF oracle 的关系。
- `.aiknowledge/pitfalls/`：如发现 Canvas sizing、clock history reset 或 collision projection 坑点，补充 pitfall。
