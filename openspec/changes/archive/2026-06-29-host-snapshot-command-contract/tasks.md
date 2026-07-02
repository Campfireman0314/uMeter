# Tasks: 2026-06-29-host-snapshot-command-contract

## Default Constraints

**Forbidden Files**（默认）:
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**（默认）:
- `.openspec.yaml` - controller 串行更新
- `tasks.md` - controller 串行更新
- `evidence.md` - controller 串行更新

**Worker Return Format**（默认）: DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED

## 1. Host contract implementation

### Task 1.1 [characterization-first] 固定当前 source/detail/workspace 行为基线

**Status**: [x]
**需求追踪**: [R1, R2, R4] -> [U1, U2, U4]
**执行方式**: [characterization-first]
**Allowed Files**:
- `tests/DesktopLoudnessMeter.Tests/Host/**` - 新增 host/controller characterization tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - 补充现有 source/detail regression
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - 补充 workspace/detail preference regression
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - 补充 row metric/readout regression

**验收标准**:
- [x] source key、kind、display/effective title、hidden/order、selected detail 和 missing detail 语义有 characterization 覆盖。
- [x] alias、forget source、detail module preference、true-peak/waveform/spectrum setting persistence 有 characterization 覆盖或现有测试引用说明。
- [x] recent event、status text 和 graph refresh token 语义有 characterization 覆盖或现有测试引用说明。

**验证命令 / 方法**:
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`，预期：通过。

**Evidence Required**:
- 新增/命中的测试清单、命令退出码、关键输出摘要写入 `evidence.md`。

**Done When**:
- 行为基线可以在重构前后重复运行，并能捕获 host contract 抽取导致的 WPF-visible 回归。

**依赖**: 无

### Task 1.2 [test-first] 定义 UI-neutral host snapshot DTO

**Status**: [x]
**需求追踪**: [R1, R2, R5] -> [U1, U5]
**执行方式**: [test-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/Host/**` - 新增 host snapshot DTO
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs` - 仅允许非破坏性 projection helper
- `tests/DesktopLoudnessMeter.Tests/Host/**` - snapshot DTO/projection tests

**验收标准**:
- [x] host snapshot 类型不依赖 `ICommand`、`ObservableCollection`、`DispatcherTimer` 或 XAML control 类型。
- [x] source snapshot projection 包含 stable identity、display/effective title、hidden/order、missing state、readout metrics 和 graph data。
- [x] settings snapshot projection 读取现有 `UserWorkspaceSettings` 字段，不新增 schema version 或改变 JSON 字段语义。

**验证命令 / 方法**:
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host"`，预期：通过。
- `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`，预期：无产品依赖命中。

**Evidence Required**:
- 测试命令、boundary grep 输出、若有允许命中则记录原因。

**Done When**:
- Host DTO 可由 WPF 或未来 Flutter bridge 消费，而不需要引用 WPF binding primitives。

**依赖**: Task 1.1

### Task 1.3 [test-first] 实现 host command vocabulary 和 typed result

**Status**: [x]
**需求追踪**: [R3, R5] -> [U3, U5]
**执行方式**: [test-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/Host/**` - command/result/error 类型和执行入口
- `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs` - 仅允许兼容 interface 组合/helper
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 仅允许兼容 helper
- `tests/DesktopLoudnessMeter.Tests/Host/**` - command success/error tests

**验收标准**:
- [x] source commands 覆盖 select/reset/alias/hide/restore/move/reorder/forget/analyzer setting 更新。
- [x] global commands 覆盖 refresh/back/reset-all/theme/settings/performance mode 更新。
- [x] 无效 target、无效参数或 service failure 返回 typed error result，不要求 UI 捕获底层异常。
- [x] command result 携带 command 后 snapshot 或明确可复用的 snapshot refresh 语义。

**验证命令 / 方法**:
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host"`，预期：通过。

**Evidence Required**:
- command 覆盖表、测试命令输出、错误码语义摘要写入 `evidence.md`。

**Done When**:
- WPF adapter 和未来 Flutter bridge 可以通过同一 request/result vocabulary 操作 host。

**依赖**: Task 1.2

### Task 1.4 [characterization-first] 抽出 host state controller

**Status**: [x]
**需求追踪**: [R1, R2, R3] -> [U2, U3]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/Host/**` - host controller/state/preference mapper
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - 逐步迁移 source/detail/workspace 状态机
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs` - 仅允许兼容 helper
- `tests/DesktopLoudnessMeter.Tests/Host/**` - controller tests
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - regression
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - regression

**验收标准**:
- [x] refresh/reconcile、missing detail retention、source lifecycle recent events、preferences application 和 command routing 由 host controller 持有。
- [x] WPF `MainViewModel` 仍保留 public binding API，但不再作为唯一业务状态真相。
- [x] service failure 被转换成 snapshot/status 或 typed error，不破坏 refresh loop。
- [x] 不改变 source service、capture、DSP 或 ingest 行为。

**验证命令 / 方法**:
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "Host|MainViewModel"`，预期：通过。

**Evidence Required**:
- 抽取范围摘要、测试输出、任何保留在 WPF adapter 中的业务逻辑说明。

**Done When**:
- Host controller 可独立测试，WPF adapter 可通过它生成 UI state。

**依赖**: Task 1.3

### Task 1.5 [characterization-first] 将 WPF ViewModel 接到 host contract

**Status**: [x]
**需求追踪**: [R4] -> [U4]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - host snapshot application 和 command wrapper
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - host row/detail adapter
- `src/DesktopLoudnessMeter.App/ViewModels/RecentEventViewModel.cs` - 如需 adapter 映射
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs` - 仅限 popup command glue 兼容
- `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - WPF parity tests

**验收标准**:
- [x] 现有 WPF public properties/commands 保持兼容，XAML 不需要大面积改写。
- [x] `RelayCommand` wrapper 调用 host command/result，而不是复制 host controller 业务规则。
- [x] detail popup/window-specific event 仍留在 WPF adapter，不进入 host contract。
- [x] 现有 ViewModel tests 通过。

**验证命令 / 方法**:
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "MainViewModel|AudioSessionMeterViewModel|DetailWindowViewModel"`，预期：通过。

**Evidence Required**:
- WPF parity 测试输出、保留 adapter-only 行为列表。

**Done When**:
- WPF 继续作为用户可见 UI 工作，且 host contract 是其主要状态/命令来源。

**依赖**: Task 1.4

### Task 1.6 [direct] 执行边界和全量回归验证

**Status**: [x]
**需求追踪**: [R1, R2, R3, R4, R5] -> [U5]
**执行方式**: [direct]
**Allowed Files**:
- 无产品实现文件；本 task 只运行验证命令和边界检查。

**Controller Artifacts**:
- `openspec/changes/2026-06-29-host-snapshot-command-contract/evidence.md` - controller 在 task 完成后追加验证证据
- `openspec/changes/2026-06-29-host-snapshot-command-contract/tasks.md` - controller 在 task 完成后更新 task 状态

**验收标准**:
- [x] host contract boundary grep 通过或所有命中均有合理说明。
- [x] 完整 test suite 通过；若既有 UDP socket smoke 仍 skip，evidence 明确为环境限制且本 change 覆盖不依赖该 path。
- [x] diff scope 不包含 native/JUCE/protocol/setup/Flutter 实现修改。

**验证命令 / 方法**:
- `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host`，预期：无产品依赖命中。
- `git diff --stat`，预期：仅本 change 允许范围。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：通过。

**Evidence Required**:
- boundary grep、diff scope、完整测试输出、skip/allowed failure 说明。

**Done When**:
- 所有 task 完成并有 fresh evidence，可以进入 `opsx-verify`。

**依赖**: Task 1.5

## Manual Checks

- [~] [manual] WPF/Flutter 人工 UI smoke | 原因：本 change 的用户可见行为由 WPF ViewModel 自动化 regression 覆盖；Flutter UI 尚未创建，Windows Flutter manual smoke 属于 `flutter-windows-app-shell`。
