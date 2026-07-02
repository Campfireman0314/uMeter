# Design: 2026-06-29-host-snapshot-command-contract

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

已证实事实：
- `MainViewModel` 当前负责 timer refresh、source reconciliation、missing row retention、selected detail、recent events、workspace preferences、theme/settings 和 row command routing。
- `AudioSessionMeterViewModel` 当前负责 row-level display/readout formatting、WPF commands、alias editing、hide/restore/move events 和 metric graph tick projection。
- `SignalSourceSnapshot` 已是接近 UI-neutral 的 source input DTO，包含 source key/kind、process id、display name、peak/readout、state、loudness/true peak/RMS/stereo/waveform/spectrum 和 `PlotFrameVersion`。
- `ISignalSourceMeterService`、`ISignalSourceResetService`、`ISignalSourceAnalysisSettingsService` 已提供 source snapshot、reset 和 analysis settings 转发接口。
- `UserWorkspaceSettings` 已持久化 theme、recent events settings、event folder、per-source alias/hidden/order/detail preferences 等字段；本 change 不做 schema 迁移。

推断：
- Flutter shell 若直接消费 `MainViewModel`，会被 WPF binding shape 绑定；host contract 应在 C# host 层先收敛。
- 初版 host contract 可以留在 `DesktopLoudnessMeter.App` 内部 namespace，后续 Flutter bridge change 再决定是否拆出单独 assembly。

## 目标 / 非目标

**目标：**
- 提供 `DesktopLoudnessMeter.App.Host` host snapshot/command contract。
- 把 source/detail/workspace/recent-event 状态机从 WPF binding adapter 中抽出。
- 保持 WPF UI 行为，降低后续 Flutter shell 接入风险。

**非目标：**
- 不实现 Flutter bridge 或 Flutter UI。
- 不迁移 C++ core、source ingest、capture adapter 或 packaging。
- 不改变 `UserWorkspaceSettings` JSON schema。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U1, U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] Host snapshot model
- 关联需求: [R1, R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostDetailSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostRecentEventSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs`
- 设计:
  - host DTO 使用 `record` / immutable collection，避免 `ObservableCollection` 和 `ICommand`。
  - source row snapshot 保留 stable `SourceKey`、`SignalSourceKind`、`ProcessId`、display/effective title、hidden/order、missing status、metric arrays 和 graph frame tokens。
  - settings snapshot 只 projection 当前 workflow 所需字段，不定义新 schema version。

### [U2] Host state controller
- 关联需求: [R1, R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceState.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostWorkspacePreferenceMapper.cs`
  - `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- 设计:
  - controller 拥有 refresh/reconcile/missing detail/recent events/preference application 的业务状态。
  - controller 消费现有 source service 和 settings；不直接拥有 WPF timer/window/popup。
  - WPF `MainViewModel.RefreshNow()` 作为 adapter 调用 controller refresh，再应用 snapshot。

### [U3] Host command model and execution
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommandResult.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommandError.cs`
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
- 设计:
  - command 使用 typed record hierarchy 或 discriminated-style enum+payload，覆盖 source/global/detail/settings 操作。
  - result 总是带 `Success`、可选 `Error` 和 command 后 snapshot。
  - service exception 被转为 typed error code，UI 不解析底层异常文本；日志仍可由现有 logging 机制记录。

### [U4] WPF adapter parity
- 关联需求: [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/RecentEventViewModel.cs`
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs`
- 设计:
  - WPF ViewModel 保留 public API，避免 XAML 大面积重写。
  - `RelayCommand` wrapper 只做参数转换、CanExecute 映射和 snapshot application。
  - `DetailPopupRequested` 等 window glue 仍留在 WPF adapter，不进入 host contract。

### [U5] Boundary and verification guardrails
- 关联需求: [R5]
- 模块边界:
  - `openspec/changes/2026-06-29-host-snapshot-command-contract/**`
  - `tests/DesktopLoudnessMeter.Tests/Host/**`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**`
- 设计:
  - 用 grep boundary check 确认 `src/DesktopLoudnessMeter.App/Host` 不依赖 WPF-only 类型。
  - review 阶段检查 diff 不触碰 native/JUCE/protocol/setup/Flutter 实现。
  - evidence 中明确 schema compatibility 和 Flutter non-goal。

## 数据模型草案

草案用于实现方向，最终命名可在 implementation 中按现有代码风格微调：

- `HostSnapshot`
  - `IReadOnlyList<HostSourceSnapshot> Sources`
  - `HostDetailSnapshot? SelectedDetail`
  - `HostSettingsSnapshot Settings`
  - `IReadOnlyList<HostRecentEventSnapshot> RecentEvents`
  - `string StatusText`
  - `HostCommandAvailability Commands`
- `HostSourceSnapshot`
  - identity: `SourceKey`, `SourceKind`, `ProcessId`
  - display: `DisplayName`, `Alias`, `EffectiveDisplayName`, `DisplayTitle`, `SourceIdentityText`
  - state: `IsHidden`, `OrderIndex`, `IsInstanceMissing`, `State`, `PeakDbText`
  - metrics: loudness/true peak/RMS/stereo/waveform/spectrum/channel arrays
  - graph tokens: `PlotFrameVersion`, `WaveformPlotTick`, `SpectrumPlotTick`, `LoudnessClockPlotTick` 或等效刷新 token
- `HostCommand`
  - global: `Refresh`, `BackToGlobal`, `ResetAll`, `SetTheme`, `SetPerformanceMode`
  - source: `SelectDetail`, `ResetSource`, `SetAlias`, `HideSource`, `RestoreSource`, `MoveSource`, `ForgetSource`
  - analysis/detail: `SetTruePeakOversampling`, `SetWaveformDisplayMode`, `SetSpectrumDisplayMode`, `SetDetailModuleVisibility`, `SetCollisionSettings`, `SetLoudnessClockSettings`
- `HostCommandResult`
  - `bool Success`
  - `HostCommandError? Error`
  - `HostSnapshot Snapshot`

## 决策

1. **Host contract 先留在 App assembly。**
   - 原因：当前 Flutter bridge 尚未存在；先在 WPF app 内形成 tested boundary，避免 premature package split。
   - 影响：后续 `flutter-windows-app-shell` 可选择直接 bridge App assembly 或再提取 shared assembly。

2. **WPF adapter public API 尽量保持。**
   - 原因：XAML 绑定和现有测试很多；大面积改名会制造非必要风险。
   - 影响：实现可能先出现 adapter mapping 层，但这是为 Flutter 边界换来的明确分层。

3. **不改变 `SignalSourceSnapshot` 语义。**
   - 原因：它是 source service 到 UI/host 的稳定输入，native DSP change 刚归档。
   - 影响：host snapshot 是 projection，不是 ingest source of truth。

4. **Command result 显式返回 snapshot。**
   - 原因：Flutter bridge 更适合 request/result 或 stream snapshot 模型；WPF 也可直接应用 command 后状态。
   - 影响：controller command 执行必须维护一致的 state refresh 规则。

## 风险 / 权衡

- `MainViewModel` 体量大，直接抽取可能诱发回归：通过 characterization-first task 和现有 ViewModel tests 缓冲。
- Host DTO 可能过度贴近 WPF readout 字符串：允许初版保留用户可见 readout 文本，但必须同时保留 machine-readable metric 字段，后续 Flutter 可决定显示格式。
- Settings projection 可能滑向 schema migration：work/tasks 明确禁止 JSON schema 变更，若需要版本化则拆 change。
- Current-worktree 处理 shared interface 有风险：readiness/implement 前后都需要确认 dirty tree，必要时切 isolated worktree。

## 稳定化策略

- `[stabilization-first]` 适用单元：无。
- doc impact 处理：`TASK_EVIDENCE_ONLY`，除本 OpenSpec change 外不更新用户文档。
- final hardening：完整 tests + boundary grep + diff scope review。

## 回退方案

回退到 WPF ViewModel 直接消费现有 service 的路径；删除 host contract 类型和 tests。由于本 change 不改 `UserWorkspaceSettings` schema，不需要用户数据迁移回滚。

## 并行 / Subagent 边界

默认串行实现。若后续拆分 worker：
- Host DTO/controller 和 WPF adapter 不能并行写同一文件。
- 可以只读调查 XAML binding 或 tests，但 controller/main agent 串行合并 `MainViewModel`。
- `tasks.md`、`evidence.md`、`.openspec.yaml` 只由 controller 串行更新。

## 知识沉淀

归档时需要更新 `.aiknowledge/codemap/source-list-workspace.md`，记录 host contract 抽取后 source list/workspace 的新入口；如新增 `Host` 模块，则更新 `.aiknowledge/codemap/index.md`。
