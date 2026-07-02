# Work Packet: 2026-06-29-host-snapshot-command-contract

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断
- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付单元是一个 UI-neutral host snapshot/command contract。它需要同时覆盖 source list、detail state、settings/preferences projection 和 command result vocabulary，因为这些语义现在共同驻留在 `MainViewModel` 与 `AudioSessionMeterViewModel` 的交互里；拆开会让 Flutter shell 仍然缺少一个完整消费边界。它不包含 Flutter shell、settings schema migration、native build 或 source ingest core 迁移，避免变成全栈 migration。
- OpenSpec 下限检查：通过。该 change 会形成可评审、可测试、可归档的工程闭环：新增/提取 C# host contract，保留 WPF 作为回归 client，并用现有 ViewModel 行为测试和新增 contract 测试验证语义未漂移。

### 能力簇
| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Host snapshot projection | Flutter 可以消费稳定的 source/detail/settings/readout 状态，而不理解 WPF controls | `src/DesktopLoudnessMeter.App/ViewModels`, `src/DesktopLoudnessMeter.App/Audio`, `src/DesktopLoudnessMeter.App/Settings` | 高 | 首个 Flutter 前置边界 |
| Host command vocabulary | Flutter 可以通过 typed command 请求 reset、select、alias、hide、reorder 和 analysis setting 更新 | `MainViewModel`, source service interfaces, workspace settings | 高 | 不改变 capture/DSP 行为 |
| WPF adapter parity | 现有 Windows WPF UI 继续可用并作为 regression oracle | `tests/DesktopLoudnessMeter.Tests/ViewModels` | 高 | 后续 Flutter shell 可复用 contract |

## 意图

当前仓库的 Windows 主 UI 由 WPF `MainViewModel` 直接承担窗口绑定、source reconciliation、detail selection、workspace persistence、recent events、analysis settings forwarding 和 row commands。后续 Flutter shell 需要一个不依赖 WPF `ICommand`、`ObservableCollection`、控件状态和 XAML 绑定细节的 host 边界；否则 Flutter 会被迫复刻 WPF ViewModel 结构，跨平台架构会在第一步就把 UI 和 host glue 缠在一起。

本 change 的目标是抽出 C# host 层可消费/可测试的状态快照和命令契约，让 WPF 成为 contract 的一个 client，同时为后续 `flutter-windows-app-shell` 提供稳定接入面。

## 范围

### 范围内
- 定义 UI-neutral host snapshot DTO，覆盖 source list、source row state、selected detail state、detail visualization settings、recent events、global settings/status 和 command availability/error feedback。
- 定义 UI-neutral host command/request/result vocabulary，覆盖 refresh/select/back、reset source/all、alias commit/clear、hide/restore、move/reorder、forget source、true-peak oversampling、waveform/spectrum channel mode、performance mode、theme/settings preference updates。
- 将现有 WPF ViewModel 中适合作为 host 状态机的逻辑提取到 C# host controller/service，WPF ViewModel 只保留 WPF binding adapter、commands wrapper、window-specific popup/event hooks。
- 保持 `SignalSourceSnapshot` / `PcmMetricsSnapshot` 语义作为输入依赖，不改变 DSP、capture、local loopback ingest 或 native ABI。
- 用测试覆盖 contract projection、command result、workspace persistence compatibility 和现有 WPF ViewModel regression。

### 范围外
- 不创建 Flutter 项目，不实现 Flutter bridge，不创建 Flutter UI。
- 不修改 `DesktopLoudnessMeter.Native` C++ core、native build、C ABI 或 analyzer 算法。
- 不迁移 source ingest 状态机到 C++ core。
- 不正式重写 settings/workspace schema；只允许为 host snapshot 暴露当前 schema 的兼容 projection，持久化格式迁移留给 `settings-workspace-schema`。
- 不迁移托盘、启动、单实例、installer/package。
- 不改变 JUCE sender、SenderKit 或 protocol lifecycle vocabulary。

## 依赖

- `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/` 已归档，指定本 change 为首个后续实施入口。
- `openspec/changes/archive/2026-06-23-native-core-dsp/` 已归档，`PcmMetricsSnapshot` 和 native-backed analyzer 主路径稳定为 host snapshot 输入。
- `.aiknowledge/codemap/source-list-workspace.md` 标明 source identity、alias/hide/order/detail preferences 的当前约束。
- `.aiknowledge/codemap/audio-analysis.md` 和 `.aiknowledge/codemap/chains/pcm-metric-flow.md` 标明 `PcmMetricsSnapshot` 是 DSP 到 UI/service 的稳定边界。

## 成功标准

- Host contract 类型不依赖 WPF/XAML-only 类型，例如 `ICommand`、`ObservableCollection`、`DispatcherTimer` 或控件实例。
- WPF ViewModel 可以从 host snapshot 更新自身显示状态，并通过 host command result 驱动现有 command 行为。
- Source identity、alias、hidden state、ordering、selected detail retention/missing-state behavior、recent events、true-peak/waveform/spectrum settings 与现有测试语义一致。
- Host command 对无效 source key、不可执行命令和 service failure 提供可消费错误结果，不需要 UI 层解析异常。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 通过；若环境限制导致既有 UDP socket smoke 被 skip，应在 evidence 中延续说明且不得掩盖本 change 的 host contract 验证。

## 需求 / 验收摘要

- [R1] Host snapshot 必须暴露 UI-neutral 的全局状态、source rows、detail state、recent events 和 settings projection。
- [R2] Host projection 必须保留现有 source identity、排序、hide/show、missing detail、alias 和 metric/readout 语义。
- [R3] Host command vocabulary 必须覆盖现有 WPF source/detail/settings 操作，并返回 typed success/error result。
- [R4] WPF ViewModel 必须迁移为 host contract client，同时保持现有用户可见行为和测试覆盖。
- [R5] Change 必须明确边界：不实现 Flutter shell、不迁移 native/source ingest、不改变持久化 schema 格式。

## 方案摘要

新增 `DesktopLoudnessMeter.App.Host` 一组类型和 controller。controller 消费现有 `ISignalSourceMeterService`、`ISignalSourceResetService`、`ISignalSourceAnalysisSettingsService`、`UserWorkspaceSettings` 与时间/进程存活依赖，输出 immutable host snapshot；controller 同时接受 typed command request 并返回 typed result。WPF `MainViewModel` 保留当前 public binding 属性和 `RelayCommand`，但内部通过 host snapshot 和 command result 同步状态。

该方案把“状态真相”和“WPF 绑定形状”分离。Flutter 后续可以通过 bridge 消费 host snapshot/command result，而不直接依赖 WPF ViewModel。为降低风险，本 change 不碰 native DSP、capture 或 protocol，所有音频输入仍来自现有 `SignalSourceSnapshot`。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.App/Host/**` - 新增 UI-neutral host contract、snapshot、command、controller 类型。
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - 迁移为 host contract client，保留 WPF binding facade。
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - 必要时调整为 host row/detail snapshot adapter，保留 WPF commands wrapper。
- `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs` - 如需补齐 host controller 对现有 source service 的 interface 组合，可做兼容扩展。
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs` - 仅允许添加非破坏性 projection helper 或字段注释；不得改变现有语义。
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 仅允许添加兼容 projection/helper；不得改变 JSON schema 或迁移策略。
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - 如需显式包含新目录或 analyzer 设置。
- `tests/DesktopLoudnessMeter.Tests/Host/**` - 新增 host contract/controller tests。
- `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - 更新或补充 WPF adapter regression。
- `tests/DesktopLoudnessMeter.Tests/Audio/**` - 仅限 host contract 需要的 source snapshot characterization。

### 禁止修改

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.SenderKit/**`
- `src/DesktopLoudnessMeter.Setup/**`
- Flutter 项目或 runner 目录（当前不存在；本 change 不创建）
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `opsx/native-core-dsp-migration`
- Base Branch: `opsx/native-core-dsp-migration` at `00093295bde70ac9467886ee36014699703e9883`
- Merge Target: `opsx/native-core-dsp-migration`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-29-host-snapshot-command-contract/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 明确依赖已归档的 native DSP change 和架构规划 change，当前 branch 已承载该架构推进链；base_head=`00093295bde70ac9467886ee36014699703e9883`。
  - isolated 触发：本 change 将触及 shared host contract/interface，按规则存在 isolated 触发信号；但当前 workspace 是单 agent 串行推进、工作树干净、目标分支就是本架构迁移分支，且没有并行 worker/依赖隔离需求。因此采用 `current-worktree`，并在 implementation 前再次检查 dirty tree；若出现并行实现、无关 dirty、或需要跨分支验证，回到 plan 切换 isolated worktree。
  - 结论：`current-worktree`，readiness 需确认该例外理由足够且 Git 状态仍干净或仅含本 change。

## 验证计划

- `opsx changes list`，预期：仅本 change 活动，阶段指向 readiness。
- `opsx changes resolve 2026-06-29-host-snapshot-command-contract`，预期：解析到当前 change root。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|SignalSourceSnapshot|WasapiSessionMeterService|LocalLoopbackSignalSourceService"`，预期：host contract 与关键 WPF/source regression 通过。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：完整测试通过；已知 UDP socket smoke 若仍因本机 loopback 环境 skip，需要 evidence 说明为既有环境限制。
- `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`，预期：无命中或仅命中文档注释中的禁止说明。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：no
- 不确定性来源：Flutter bridge 技术形态仍未落地，但本 change 只产出 C# host contract，不依赖 Flutter runtime；macOS runner/capture 也不在范围内。
- 人工 / 平台覆盖：不需要新增人工 UI smoke；WPF 行为由现有和新增自动化测试覆盖。后续 `flutter-windows-app-shell` 必须执行 Windows Flutter manual/platform smoke。
- 最终硬化策略：用 contract tests 锁定 DTO/command 语义，用 ViewModel regression 锁定现有 WPF parity，用 boundary grep 检查 host contract 不依赖 WPF-only 类型。

## 回退 / 升级

- 回退方式：移除 `src/DesktopLoudnessMeter.App/Host/**`，恢复 `MainViewModel`/`AudioSessionMeterViewModel` 到直接服务消费路径，并删除新增 tests；由于不改持久化 schema，不需要数据迁移回滚。
- 升级条件：若 readiness 或 implementation 发现 settings persistence 需要正式 schema 版本化，立即停止本 change 对 schema 的修改，回到 plan，将该部分移入 `settings-workspace-schema`。
- 升级条件：若 host contract 需要 Flutter bridge/channel 代码才能验证，停止本 change 的 Flutter 相关尝试，保持 bridge 为后续 `flutter-windows-app-shell` 范围。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“直接让 Flutter 消费 `MainViewModel`”方案：这会把 WPF binding shape、`ICommand` 和 `ObservableCollection` 变成跨平台 contract，违背 Flutter 主 UI 与 host glue 分层目标。
- 不采用“一次性迁移 settings schema”方案：当前 change 的最小闭环是 host state/command boundary；schema 版本化和旧数据兼容有独立风险，应由 `settings-workspace-schema` 接管。
- 不采用“先建 Flutter shell 再补 contract”方案：归档架构规划已明确 Flutter 前置条件是稳定 host contract；先建 shell 会迫使 UI 猜测状态边界。
- 不采用“把 source ingest 移进 host contract”方案：source ingest 状态机未来属于 native/source ingest core，当前只消费 `SignalSourceSnapshot`。

## Readiness Notes

- readiness 需检查本 change 是否仍然是一个完整 contract 闭环，而不是混入 Flutter/native/settings schema 迁移。
- readiness 需检查 R1-R5 是否在 spec/design/tasks 中有完整 trace。
- readiness 需检查 `Workspace Rationale` 对 shared interface 触发但继续 current-worktree 的理由是否可接受。
- readiness 需确认 task 中的 tests 是业务逻辑 task 的一部分，而不是独立“补测试”意图清单。
