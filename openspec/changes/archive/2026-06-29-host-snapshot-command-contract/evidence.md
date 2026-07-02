# Evidence: 2026-06-29-host-snapshot-command-contract

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass_with_warnings
  - evidence_ref: Readiness / 2026-06-29T16:04:18+08:00
- final_verify:
  - status: pass_with_warnings
  - evidence_ref: Verify / 2026-06-29T16:30:09+08:00
- final_review:
  - status: pass_with_warnings
  - evidence_ref: Risk Review / 2026-06-29T16:31:23+08:00
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-06-29-host-snapshot-command-contract
  - evidence_ref: Archive / 2026-06-29T16:32:31+08:00
- allowed_failures:
  - item: 5 real UDP socket smoke tests skipped
    why_non_blocking: This change does not alter UDP ingest, SenderKit, JUCE sender, protocol, or packaging paths; full suite otherwise passed 294 tests and host contract coverage does not depend on real UDP sockets.
    owner_or_followup: `juce-sender-protocol-boundary` or Windows packaging/startup/tray smoke in a UDP-capable environment.
    expiry_or_recheck: Recheck before protocol/packaging archive or any change that modifies real UDP sender/receiver behavior.
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Knowledge Follow-up / 2026-06-29T16:34:39+08:00

## Exploration / Plan Evidence

### 2026-06-29 plan baseline

- stage: explore+plan
- commands:
  - `opsx changes list`
    - exit_status: 0
    - result: `无活动变更`
  - `Get-Content -Raw openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/{work,design,tasks,evidence}.md`
    - exit_status: 0
    - result: 归档规划明确首个后续实施入口为 `host-snapshot-command-contract`，范围为 UI-neutral host 状态/命令边界，依赖 native DSP archive。
  - `Get-Content -Raw .aiknowledge/codemap/index.md`
    - exit_status: 0
    - result: 命中 `source-list-workspace`、`signal-source-ingest`、`audio-capture-sources`、`audio-analysis`、`native-dsp`、`app-startup`、`pcm-metric-flow`。
  - `Get-Content -Raw .aiknowledge/codemap/source-list-workspace.md`
    - exit_status: 0
    - result: source identity 必须按 stable source key；alias、hidden、order、detail visualization preferences 由 `MainViewModel`/`UserWorkspaceSettings` 管理。
  - `Get-Content -Raw .aiknowledge/codemap/audio-analysis.md`
    - exit_status: 0
    - result: `PcmMetricsSnapshot` 是 UI/service 消费的稳定 DTO；native-backed analyzer 已是主路径。
  - `Get-Content -Raw .aiknowledge/codemap/chains/pcm-metric-flow.md`
    - exit_status: 0
    - result: `SignalSourceSnapshot` / `PcmMetricsSnapshot` 是 capture/source service 到 UI 的边界；真实 UDP socket smoke 可能受本机 loopback 环境影响。
  - `Get-Content -Raw src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
    - exit_status: 0
    - result: 证实 `MainViewModel` 拥有 refresh/reconcile、detail selection、workspace persistence、recent events、settings forwarding 和 command routing。
  - `Get-Content -Raw src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
    - exit_status: 0
    - result: 证实 row-level state 依赖 WPF command/event shape，但 metric fields 来自 `SignalSourceSnapshot`。
  - `Get-Content -Raw src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs`
    - exit_status: 0
    - result: source snapshot 已是接近 UI-neutral 的 input DTO，包含 source identity、state、metrics 和 plot frame version。
  - `Get-Content -Raw src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
    - exit_status: 0
    - result: 已有 source snapshot、reset、analysis settings service interfaces，可作为 host controller 依赖。
  - `Get-Content -Raw src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
    - exit_status: 0
    - result: 当前 workspace JSON fields 覆盖 alias、hidden、order、detail module settings、clock/spectrum/collision/stereo preferences。
  - `git status --short --branch`
    - exit_status: 0
    - result: 创建本 change 前 branch `opsx/native-core-dsp-migration` ahead 1，工作树干净；创建后 dirty 限于本 OpenSpec change。
  - `git rev-parse HEAD`
    - exit_status: 0
    - result: `00093295bde70ac9467886ee36014699703e9883`
  - `opsx changes init 2026-06-29-host-snapshot-command-contract spec-driven --profile strict-change --source-type feature`
    - exit_status: 0
    - result: 已初始化 `openspec/changes/2026-06-29-host-snapshot-command-contract`
  - `Get-ChildItem -Name package.json,pyproject.toml,CMakeLists.txt,build.gradle,pom.xml,*.sln -ErrorAction SilentlyContinue`
    - exit_status: 1
    - result: 发现 `desktop-loudness-meter.sln`；部分通配未命中导致 PowerShell 返回非零，但不影响判断。
  - `Get-ChildItem -Name src,test,tests,__tests__ -ErrorAction SilentlyContinue`
    - exit_status: 1
    - result: 发现 `src` 与 `tests` 下的多个项目；部分路径未命中导致 PowerShell 返回非零。
  - `Get-Content -Raw tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
    - exit_status: 0
    - result: xUnit/.NET test framework 已存在，并导入 native copy target。
- facts:
  - 当前无活动 change；本 change 是 native DSP archive 后的下一个计划入口。
  - `SignalSourceSnapshot` 与 `PcmMetricsSnapshot` 已是适合 host snapshot 的输入边界。
  - `MainViewModel` 当前混合 host 状态机和 WPF binding adapter。
  - 现有测试基础设施为 xUnit，已有 ViewModel、Audio 和 infrastructure tests。
- inferences:
  - Host contract 初版放在 `DesktopLoudnessMeter.App/Host` 内最小化拆 assembly 风险；后续 Flutter bridge 再决定是否提取。
  - WPF ViewModel public API 应保持，降低 XAML 重写风险。
- non_blocking_uncertainties:
  - Flutter bridge channel 形态仍未定义；本 change 以 C# host contract 为稳定前置。
  - Settings schema 正式版本化不在本 change；如实现中发现必须迁移，回到 plan 拆 change。

## 自我反驳 / 漏洞扫描

- 候选推荐：创建 `2026-06-29-host-snapshot-command-contract`，profile=`strict-change`，范围限于 C# host snapshot/command contract + WPF adapter parity。
- 第 1 轮扫描：
  - blocking：无。已确认归档规划要求该 change 先于 Flutter shell；native DSP 已归档。
  - non-blocking：shared interface change 本应考虑 isolated worktree；当前 branch 是串行架构推进分支且工作树干净，plan 记录 current-worktree 例外并要求 readiness 复核。
  - 未验证前提：Host DTO 初版能否避免过度贴近 WPF readout；处理方式是 spec 要求 machine-readable metrics 与 boundary grep。
  - 最可能错处：把 settings projection 做成隐性 schema migration；处理方式是 R5 + tasks 禁止 schema 变化。
  - 失败路径 / 边界：若实现需要 Flutter bridge 才能验证，说明范围过大，应拆到 `flutter-windows-app-shell`。
  - 更小替代：只新增 DTO 不接 WPF，但无法形成 Flutter 可用 command boundary，也无法验证 parity，低于本模块闭环。
  - 证据 vs 推断：代码/codemap 支撑当前 WPF/source/settings事实；Flutter bridge 消费方式仍是推断。
  - 处理方式：继续 `opsx-plan`，将不确定性写入 scope、tasks、verification。
- 最终扫描结论：无新的关键事实性漏洞；剩余不确定性不会改变本 change 选择，已转成 scope guardrail 和 readiness notes。

## Readiness

### 2026-06-29T16:04:18+08:00

- stage: readiness
- decision: pass_with_warnings
- evidence_refs:
  - `opsx changes list` -> `2026-06-29-host-snapshot-command-contract [tasks profile=strict-change next=opsx-readiness]`
  - `opsx changes resolve 2026-06-29-host-snapshot-command-contract` -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-host-snapshot-command-contract`
  - `git status --short --branch` -> branch `opsx/native-core-dsp-migration...origin/opsx/native-core-dsp-migration [ahead 1]`; dirty 仅为 `?? openspec/changes/2026-06-29-host-snapshot-command-contract/`
  - `rg -n "Trace\\*\\*: R[0-9]|\\[R[0-9]\\]|\\[U[0-9]\\]" openspec/changes/2026-06-29-host-snapshot-command-contract` -> R1-R5、U1-U5 和 task trace 均可追踪
  - `rg -n "TODO|<path>|<!--|undecided|<" work/design/spec/tasks` -> 仅设计草案中的泛型类型文本命中 `<HostSourceSnapshot>` / `<HostRecentEventSnapshot>`，未发现模板占位或 undecided
  - `tasks.md` -> 存在 Default Constraints；task 省略 Forbidden Files / Shared Artifacts / Worker Return Format 时沿用默认；Task 1.6 使用 `Controller Artifacts` 明确 controller 写 evidence/tasks，不把共享产物作为 worker-owned implementation scope
- findings:
  - blocking: 无
  - warnings:
    - 本 change 会触及 shared host contract/interface，按 isolation 规则存在 isolated-worktree 信号；plan 选择 `current-worktree` 的理由是当前 branch 专用于串行架构推进、工作树干净且无并行 worker。implement 前必须再次确认 dirty tree，若出现无关 dirty、并行实现或跨分支验证需求，应回到 plan 切换 isolated worktree。
    - Task 1.6 是直接验证任务，不改产品代码；其 evidence/tasks 更新只能由 controller 在 task 完成后串行写入。
- consistency:
  - work.md: scope/non-goals 明确，包含 verification、rollback/escalation、workspace rationale。
  - specs/change/spec.md: R1-R5 全局唯一，每条需求有可观察场景。
  - design.md: R1-R5 均映射到 U1-U5，每个 U 均列出模块边界。
  - tasks.md: 6 个 task 均包含需求追踪、执行方式、Allowed Files、验收标准、验证方法、Evidence Required 和 Done When；Default Constraints 覆盖共享字段。
- next: 转入 `opsx-implement`，先执行 Task 1.1 characterization baseline。

## Test Cycle Evidence

### 2026-06-29T16:07:00+08:00 Task 1.1 characterization baseline

- task: 1.1
- phase: characterization-first
- command:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`
    - exit_status: 0
    - result: Passed 125, Failed 0, Skipped 0
  - `rg -n "Alias|Hide|Order|WaveformDisplayMode|SpectrumDisplayMode|TruePeakOversampling|Inline.*Visible|Missing|RecentEvent|PlotTick|Forget|ShowHidden|MoveSource|Detail" tests/DesktopLoudnessMeter.Tests/ViewModels -g "*.cs"`
    - exit_status: 0
    - result: 命中 `MainViewModelTests`、`MainViewModelAdvancedControlsTests`、`AudioSessionMeterViewModelTests`，覆盖 selected detail/missing detail、recent events、alias、forget、inline detail preferences、true-peak/waveform/spectrum settings 和 plot tick 行为。
- coverage:
  - R1/R2 source/detail/workspace 基线已由现有 ViewModel tests 固定。
  - R4 WPF adapter parity 有可重复回归命令。
- notes:
  - 本 task 未新增产品代码；作为后续 host contract 抽取的行为基线。

### 2026-06-29T16:12:00+08:00 Task 1.2 host snapshot DTO

- task: 1.2
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotTests.cs`
- command:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host"`
    - exit_status: 0
    - result: Passed 3, Failed 0, Skipped 0
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: 无命中；host DTO 未暴露 WPF-only 类型。
- coverage:
  - R1: `HostSnapshot`/`HostSettingsSnapshot` 提供 UI-neutral aggregate 和 settings projection。
  - R2: `HostSourceSnapshot.FromSignalSource` 保留 stable source identity、display/effective title、hidden/order、missing state、metrics 和 graph data。
  - R5: 未修改 settings schema；测试确认 `UserWorkspaceSettings` 无 schema version 字段，projection 只读取现有字段。
- notes:
  - 初次测试曾因 `DetailModuleKind` namespace 引用错误编译失败；修正为 `DesktopLoudnessMeter.App.Settings` 后通过。

### 2026-06-29T16:16:00+08:00 Task 1.3 host command vocabulary

- task: 1.3
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommandResult.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostCommandTests.cs`
- command:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host"`
    - exit_status: 0
    - result: Passed 6, Failed 0, Skipped 0
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: 无命中。
- coverage:
  - R3: source/global command records cover select/reset/alias/hide/restore/move/reorder/forget/analyzer settings and refresh/back/reset-all/theme/settings/performance operations.
  - R3: `HostCommandResult` carries success/error and post-command snapshot; `HostCommandError` exposes typed error codes.
  - R5: command vocabulary remains in `App.Host` and does not touch Flutter/native/source ingest/protocol areas.
- notes:
  - 初次实现因 C# record inheritance parameter name/type conflict 编译失败；base source command payload renamed to `TargetSourceKind` 后通过。
  - Command execution remains in Task 1.4 host controller; Task 1.3 closes the typed vocabulary/result layer.

### 2026-06-29T16:20:27+08:00 Task 1.4/1.5 host controller and WPF adapter

- task: 1.4, 1.5
- phase: characterization-first
- files:
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommandResult.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostCommandTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotTests.cs`
- command:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`
    - exit_status: 0
    - result: Passed 137, Failed 0, Skipped 0
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: 无命中。
- coverage:
  - R1/R2: `DesktopMeterHostController.Refresh` 生成 host snapshot，保留 `LastSourceSnapshots` 给 WPF adapter 做兼容 reconcile；host snapshot 对 Flutter-facing consumer 提供 filtered source projection。
  - R3: host controller 执行 typed commands，覆盖 refresh、show hidden、reset all/source、alias、hide/restore、move、forget、theme/settings/performance、true-peak/waveform/spectrum settings；command result 返回 snapshot/error。
  - R4: `MainViewModel` 暴露 `HostSnapshot`，refresh 走 host controller；主要 row/global command wrappers 通过 `ExecuteHostCommand` 调用 host result；WPF detail popup/window glue 留在 WPF adapter。
  - R5: 未修改 capture/source ingest/native/JUCE/protocol/setup/Flutter。
- notes:
  - 初次 WPF adapter 接入让 source warning 丢失且 hidden-source bookkeeping 退化；修正为在 `_sourceWarningText` 初始化后创建 host controller，并让 WPF adapter 使用 host controller 的 `LastSourceSnapshots` 维持原有 hidden filtering。
  - 初次 command routing 让 `persist:false` 初始化路径不再转发 analyzer settings，且 forget-source reset 重新创建 workspace row；修正为 user-initiated persistent command 走 host command，初始化/reset路径仍直接转发 analyzer service 但不写 workspace。
  - `MainViewModel` 仍保留部分 adapter-local state（detail modules、frequency collision rendering、popup/window events、graph layout）。这些属于 WPF binding/window glue 或后续 source/detail schema change 范围，不阻塞本 host contract 闭环。

### 2026-06-29T16:21:20+08:00 Task 1.6 final implementation checks

- task: 1.6
- phase: direct
- command:
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: 无命中，host contract 不暴露 WPF-only 类型。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
    - exit_status: 0
    - result: Passed 294, Failed 0, Skipped 5, Total 299
  - `git status --short --branch`
    - exit_status: 0
    - result: dirty 仅包含 `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`、`src/DesktopLoudnessMeter.App/Host/**`、`tests/DesktopLoudnessMeter.Tests/Host/**` 和本 OpenSpec change。
  - `Get-ChildItem -Recurse src/DesktopLoudnessMeter.App/Host,tests/DesktopLoudnessMeter.Tests/Host`
    - exit_status: 0
    - result: 列出 6 个 host product files 和 3 个 host test files。
- coverage:
  - R1-R3: Host DTO、command vocabulary、controller execution 和 typed result 由 Host tests 覆盖。
  - R4: WPF adapter parity 由 Host/MainViewModel/AudioSessionMeterViewModel/DetailWindowViewModel filtered suite 与完整 suite 覆盖。
  - R5: diff scope 未触及 native/JUCE/protocol/setup/Flutter 实现；boundary grep 无 WPF-only host dependency。
- allowed_failures:
  - item: 5 个真实 UDP socket smoke tests skipped
    why_non_blocking: 该 skip 来自已归档 native DSP change 的当前环境限制；本 host contract change 不依赖真实 UDP socket path，完整 suite 其余 294 tests 通过。
    owner_or_followup: 后续 `juce-sender-protocol-boundary` 或 Windows packaging/startup/tray smoke 在 UDP-capable 环境复查。
    expiry_or_recheck: 后续 protocol/packaging/manual smoke。

## Stabilization Ledger

无 `[stabilization-first]` task。

## Implement Amendments

<!-- 如实现阶段需要修订 plan/spec/design/tasks，在此追加。 -->

### 2026-06-29T16:30:09+08:00 review-amendment

- observation: review 风险审查发现 host controller 用 filtered `_sources` 判断 `Source appeared`，切换 `ShowHiddenSources` 时可能把已经存在但此前被隐藏过滤的 live source 记录成新出现。
- classification: within-scope implementation amendment；属于 host state controller recent-event 语义修正，不改变 spec/design/scope。
- change_summary:
  - `DesktopMeterHostController` 新增 `_knownLiveSourceKeys`，基于真实 service snapshot source key 集合判断首次出现，而不是基于可见/隐藏过滤后的 `_sources`。
  - `DesktopMeterHostControllerTests.ShowingPreviouslyHiddenLiveSourceDoesNotCreateAppearanceEvent` 覆盖隐藏 live source 切换显示时不产生伪造 appearance event。
- docs_synced:
  - tasks/spec/design: unchanged；当前需求 R1/R2/R3 的 host state snapshot/command 语义不变。
  - evidence: 本 amendment 和重新 verify 已记录。
- validation:
  - first_attempt:
    - command: parallel `dotnet test ... --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"` and full `dotnet test ...`
    - exit_status: 1
    - result: WPF build output file locked by `VBCSCompiler`; classified as test-run orchestration failure caused by parallel test commands sharing `obj\Debug\net8.0-windows\uMeter.dll`, not product failure.
  - cleanup:
    - command: `dotnet build-server shutdown`
    - exit_status: 0
    - result: MSBuild and VB/C# compiler servers shut down successfully.
  - rerun_1:
    - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`
    - exit_status: 0
    - result: Passed 138, Failed 0, Skipped 0, Total 138.
  - rerun_2:
    - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
    - exit_status: 0
    - result: Passed 295, Failed 0, Skipped 5, Total 300.
  - boundary:
    - command: `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: no WPF-only host contract dependency found.
- pending: none.
- gate_impact: verify refreshed to pass_with_warnings; review must run again after this amendment.

## Verify

### 2026-06-29T16:24:54+08:00

- stage: verify
- decision: pass_with_warnings
- direction: tasks.md + specs/**/*.md + design.md + implementation files -> verified
- evidence:
  - `opsx changes list`
    - exit_status: 0
    - result: `2026-06-29-host-snapshot-command-contract [tasks profile=strict-change next=opsx-verify]`
  - `Get-Content -Raw openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
    - exit_status: 0
    - result: archived architecture plan confirms `host-snapshot-command-contract` is the first implementation change after native DSP archive and must stay limited to UI-neutral host state/command boundary.
  - `Get-Content -Raw .aiknowledge/codemap/index.md` and `.aiknowledge/codemap/source-list-workspace.md`
    - exit_status: 0
    - result: source-list-workspace remains the hit module; stable source key, alias, hidden/order, detail preference, and graph refresh semantics are the relevant facts.
  - `rg -n "Status\\*\\*: \\[ \\]|- \\[ \\]|final_readiness|pass_with_warnings|Trace\\*\\*: R|\\[R[0-9]\\]|\\[U[0-9]\\]" openspec/changes/2026-06-29-host-snapshot-command-contract`
    - exit_status: 0
    - result: no incomplete implementation task checkboxes found; R/U trace and readiness warning are present.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: no WPF-only host contract dependency found.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`
    - exit_status: 0
    - result: Passed 137, Failed 0, Skipped 0.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
    - exit_status: 0
    - result: Passed 294, Failed 0, Skipped 5, Total 299.
  - `git branch --show-current; git rev-parse HEAD; git status --short --branch; git diff --stat; git diff -- src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
    - exit_status: 0
    - result: branch `opsx/native-core-dsp-migration`, HEAD `00093295bde70ac9467886ee36014699703e9883`, dirty only current change files; tracked diff limited to `MainViewModel.cs` with 66 insertions and 47 deletions, plus untracked host contract/OpenSpec/test files.
  - `Get-ChildItem -Recurse openspec/changes/2026-06-29-host-snapshot-command-contract,src/DesktopLoudnessMeter.App/Host,tests/DesktopLoudnessMeter.Tests/Host | Select-Object FullName,Length`
    - exit_status: 0
    - result: listed current change OpenSpec files, 6 host product files, and 3 host test files.
- Git:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `00093295bde70ac9467886ee36014699703e9883`
  - base: `opsx/native-core-dsp-migration` planning baseline after native DSP archive; merge_target=`opsx/native-core-dsp-migration`
  - diff_status: unstaged tracked `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`; untracked `openspec/changes/2026-06-29-host-snapshot-command-contract/**`, `src/DesktopLoudnessMeter.App/Host/**`, `tests/DesktopLoudnessMeter.Tests/Host/**`; all classified as current change.
  - verification_coverage: worktree diff plus current committed branch state.
  - manual_status: waived for this change; WPF behavior is covered by automated ViewModel regression and Flutter manual smoke belongs to `flutter-windows-app-shell`.
  - stabilization_status: none.
  - amendment_status: none.
- findings:
  - blocking: none.
  - warnings:
    - 5 real UDP socket smoke tests are skipped in the current environment; this is inherited environment coverage, not a host-contract behavior failure.
    - WPF/Flutter manual UI smoke is explicitly waived/deferred as non-blocking: Flutter UI does not exist in this change, and WPF-visible behavior is covered by regression tests.
- allowed_failures:
  - item: 5 real UDP socket smoke tests skipped
    why_non_blocking: This change does not alter UDP ingest, SenderKit, JUCE sender, protocol, or packaging paths; full suite otherwise passed 294 tests and host contract coverage does not depend on real UDP sockets.
    owner_or_followup: `juce-sender-protocol-boundary` or Windows packaging/startup/tray smoke in a UDP-capable environment.
    expiry_or_recheck: Recheck before protocol/packaging archive or any change that modifies real UDP sender/receiver behavior.
- StageResult:
  - stage: verify
  - decision: pass_with_warnings
  - findings_count: 0 critical, 2 warning, 0 suggestion
  - next: opsx-review

### 2026-06-29T16:30:09+08:00

- stage: verify
- decision: pass_with_warnings
- direction: implement-amendment + tasks.md + specs/**/*.md + design.md + implementation files -> reverified
- evidence:
  - `dotnet build-server shutdown`
    - exit_status: 0
    - result: MSBuild and VB/C# compiler servers shut down successfully after parallel test run file-lock failure.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`
    - exit_status: 0
    - result: Passed 138, Failed 0, Skipped 0, Total 138.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
    - exit_status: 0
    - result: Passed 295, Failed 0, Skipped 5, Total 300.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`
    - exit_status: 1
    - result: no WPF-only host contract dependency found.
  - `git branch --show-current; git rev-parse HEAD; git status --short --branch; git diff --stat`
    - exit_status: 0/1 combined because final `rg` returned no-match exit 1 in the same shell batch; git snapshot shows branch `opsx/native-core-dsp-migration`, HEAD `00093295bde70ac9467886ee36014699703e9883`, dirty only current change files.
- Git:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `00093295bde70ac9467886ee36014699703e9883`
  - base: `opsx/native-core-dsp-migration` planning baseline after native DSP archive; merge_target=`opsx/native-core-dsp-migration`
  - diff_status: unstaged tracked `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`; untracked `openspec/changes/2026-06-29-host-snapshot-command-contract/**`, `src/DesktopLoudnessMeter.App/Host/**`, `tests/DesktopLoudnessMeter.Tests/Host/**`; all classified as current change.
  - verification_coverage: worktree diff plus current committed branch state.
  - manual_status: waived for this change; WPF behavior is covered by automated ViewModel regression and Flutter manual smoke belongs to `flutter-windows-app-shell`.
  - stabilization_status: none.
  - amendment_status: reverified.
- findings:
  - blocking: none.
  - warnings:
    - 5 real UDP socket smoke tests are skipped in the current environment; inherited environment coverage, not a host-contract behavior failure.
    - WPF/Flutter manual UI smoke is explicitly waived/deferred as non-blocking: Flutter UI does not exist in this change, and WPF-visible behavior is covered by regression tests.
- allowed_failures:
  - item: 5 real UDP socket smoke tests skipped
    why_non_blocking: This change does not alter UDP ingest, SenderKit, JUCE sender, protocol, or packaging paths; full suite otherwise passed 295 tests and host contract coverage does not depend on real UDP sockets.
    owner_or_followup: `juce-sender-protocol-boundary` or Windows packaging/startup/tray smoke in a UDP-capable environment.
    expiry_or_recheck: Recheck before protocol/packaging archive or any change that modifies real UDP sender/receiver behavior.
- StageResult:
  - stage: verify
  - decision: pass_with_warnings
  - findings_count: 0 critical, 2 warning, 0 suggestion
  - next: opsx-review

## Risk Review

### 2026-06-29T16:31:23+08:00

- stage: review
- decision: pass_with_warnings
- Review Scope Snapshot:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `00093295bde70ac9467886ee36014699703e9883`
  - verify_gate: `pass_with_warnings` at `2026-06-29T16:30:09+08:00`
  - diff_status: unstaged tracked `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`; untracked `openspec/changes/2026-06-29-host-snapshot-command-contract/**`, `src/DesktopLoudnessMeter.App/Host/**`, `tests/DesktopLoudnessMeter.Tests/Host/**`; all classified as current change.
  - review_coverage: worktree diff and untracked current-change files; no unrelated dirty files observed.
- reviewed_artifacts:
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/*.cs`
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` host adapter diff
  - current change OpenSpec artifacts and gates
  - `.aiknowledge/codemap/source-list-workspace.md`
  - `.aiknowledge/pitfalls/index.md`, `misc/visible-only-layout-bindings-can-hide-controls.md`, `performance/hidden-wpf-visualizations-need-active-gates.md`, `misc/native-consumers-must-import-copy-targets.md`
- commands:
  - `opsx changes list; git status --short --branch; git diff --stat`
    - exit_status: 0
    - result: active change is `2026-06-29-host-snapshot-command-contract [tasks profile=strict-change next=opsx-review]`; dirty tree limited to current change files.
  - `Get-Content -Raw openspec/changes/2026-06-29-host-snapshot-command-contract/.openspec.yaml`
    - exit_status: 0
    - result: verify gate is `pass_with_warnings` at `2026-06-29T16:30:09+08:00`.
  - `rg -n "_knownLiveSourceKeys|ShowingPreviouslyHiddenLiveSourceDoesNotCreateAppearanceEvent|AddRecentEvent|RebuildFromLastSources|SetShowHiddenSourcesHostCommand" src/DesktopLoudnessMeter.App/Host tests/DesktopLoudnessMeter.Tests/Host`
    - exit_status: 0
    - result: amendment guard and regression test present; command mutators rebuild from last source snapshots.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher|DesktopLoudnessMeter.Native|DesktopLoudnessMeter.Juce|DesktopLoudnessMeter.Protocol|SenderKit|Setup|pubspec|flutter|Flutter" src/DesktopLoudnessMeter.App/Host tests/DesktopLoudnessMeter.Tests/Host src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
    - exit_status: 0
    - result: WPF primitive hits are limited to existing `MainViewModel` adapter and test assertion strings; no Flutter/native/JUCE/protocol/setup product implementation hit in host contract/test scope.
- findings:
  - CRITICAL: none.
  - WARNING:
    - 5 real UDP socket smoke tests remain skipped in verify; non-blocking for this host contract because release diff does not alter UDP/protocol/sender paths.
    - Manual WPF/Flutter UI smoke remains waived/deferred as recorded in tasks/evidence; WPF-visible behavior is covered by automated ViewModel regression and Flutter UI does not exist in this change.
  - SUGGESTION: none.
- rejected_review_findings:
  - Host recent-event false appearance on hidden-source reveal was identified during review, fixed as within-scope amendment, and reverified before this final review record; it is not an open finding.
  - Active pitfalls for WPF render/layout/native copy target do not apply to this release diff because it does not modify XAML/render controls or native copy targets.
- allowed_failures:
  - item: 5 real UDP socket smoke tests skipped
    why_non_blocking: This change does not alter UDP ingest, SenderKit, JUCE sender, protocol, or packaging paths; full suite otherwise passed 295 tests and host contract coverage does not depend on real UDP sockets.
    owner_or_followup: `juce-sender-protocol-boundary` or Windows packaging/startup/tray smoke in a UDP-capable environment.
    expiry_or_recheck: Recheck before protocol/packaging archive or any change that modifies real UDP sender/receiver behavior.
- next: opsx-archive

## Manual Checks

- [~] [manual] WPF/Flutter 人工 UI smoke | 原因：本 change 的用户可见行为由 WPF ViewModel 自动化 regression 覆盖；Flutter UI 尚未创建，Windows Flutter manual smoke 属于 `flutter-windows-app-shell`。

## Rejected Attempts

### Attempt 1

- hypothesis: 直接让 Flutter 后续消费 `MainViewModel`。
- change_summary: 未执行，仅在 plan 阶段评估。
- verification: `MainViewModel` 证实包含 WPF command/timer/binding 与 host 状态机混合逻辑。
- rejection_reason: 会把 WPF binding shape 变成跨平台 contract。

### Attempt 2

- hypothesis: 在本 change 内同时迁移 settings schema。
- change_summary: 未执行，仅在 plan 阶段评估。
- verification: `UserWorkspaceSettings` 已有多个 per-source/detail fields；schema versioning 风险独立。
- rejection_reason: 过大且与后续 `settings-workspace-schema` 重叠。

### Attempt 3

- hypothesis: 先创建 Flutter Windows shell，再回填 host contract。
- change_summary: 未执行，仅在 plan 阶段评估。
- verification: 归档架构规划明确第一个实施入口为 `host-snapshot-command-contract`。
- rejection_reason: 会迫使 Flutter shell 猜测 WPF ViewModel 语义。

## Archive

### 2026-06-29T16:32:31+08:00

- stage: archive
- decision: pass
- archive_path: `openspec/changes/archive/2026-06-29-host-snapshot-command-contract`
- spec_count: 1
- gates:
  - readiness: pass_with_warnings at `2026-06-29T16:04:18+08:00`
  - verify: pass_with_warnings at `2026-06-29T16:30:09+08:00`
  - review: pass_with_warnings at `2026-06-29T16:31:23+08:00`
  - archive: pass at `2026-06-29T16:32:31+08:00`
- pre_archive_checks:
  - target archive path did not exist before move.
  - tasks complete; manual check is `[~]` with reason and non-blocking follow-up.
  - `allowed_failures` includes `why_non_blocking`, `owner_or_followup`, and `expiry_or_recheck`.
  - no unresolved stabilization/doc drift/final hardening blocker found.
- retained_warnings:
  - 5 real UDP socket smoke tests skipped; follow up in `juce-sender-protocol-boundary` or Windows packaging/startup/tray smoke.
  - WPF/Flutter manual UI smoke waived for this change; Flutter manual smoke belongs to `flutter-windows-app-shell`.
- knowledge_followup: completed by `opsx-knowledge mode=auto`; see Knowledge Follow-up / 2026-06-29T16:34:39+08:00.

## Knowledge Follow-up

### 2026-06-29T16:34:39+08:00

- stage: knowledge
- mode: auto
- decision: done
- source_refs:
  - `change:2026-06-29-host-snapshot-command-contract`
  - `evidence:openspec/changes/archive/2026-06-29-host-snapshot-command-contract/evidence.md`
- pre_knowledge_status:
  - command: `git status --short -- .aiknowledge/`
  - result: clean
- changes:
  - added `.aiknowledge/codemap/host-contract.md`
  - updated `.aiknowledge/codemap/index.md`
  - updated `.aiknowledge/codemap/source-list-workspace.md`
  - added `.aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
  - updated `.aiknowledge/pitfalls/misc/index.md`
  - updated `.aiknowledge/logs/2026-06.md`
- post_knowledge_status:
  - command: `git status --short -- .aiknowledge/`
  - result:
    - `M .aiknowledge/codemap/index.md`
    - `M .aiknowledge/codemap/source-list-workspace.md`
    - `M .aiknowledge/logs/2026-06.md`
    - `M .aiknowledge/pitfalls/misc/index.md`
    - `?? .aiknowledge/codemap/host-contract.md`
    - `?? .aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
- diff_stat:
  - unstaged_tracked: `.aiknowledge/codemap/index.md | 1 +`; `.aiknowledge/codemap/source-list-workspace.md | 12 ++++++++----`; `.aiknowledge/logs/2026-06.md | 2 ++`; `.aiknowledge/pitfalls/misc/index.md | 1 +`
  - untracked: `.aiknowledge/codemap/host-contract.md`; `.aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
  - staged: none
- validation:
  - `Test-Path .aiknowledge/codemap/host-contract.md` -> true
  - `Test-Path .aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md` -> true
  - `rg -n "host-contract|Filtered Views Are Not Lifecycle State|filtered-views-are-not-lifecycle-state|2026-06-29-host-snapshot-command-contract" .aiknowledge` -> hits in codemap index, pitfall index, new entries, and monthly log
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"` -> Passed 138, Failed 0, Skipped 0, Total 138.
