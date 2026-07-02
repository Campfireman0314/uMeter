# Work Packet: 2026-07-01-host-snapshot-assembly-boundary

## Metadata

- profile: lean-change
- source_type: refactor
- status: planning
- created: 2026-07-01

## Intent

已证实：`DesktopMeterHostController` 仍直接承担 Host snapshot assembly，包括 aggregate snapshot chrome projection 调用、selected detail DTO assembly、selected-detail native plan 到 `HostDetailSnapshot` / `HostFrequencyCollisionSnapshot` 的映射，以及多处 post-command snapshot rebuild。近期 changes 已把 workspace effects 和 runtime service effects 从 controller 隔离出来，但 snapshot assembly 仍内联在 controller 中。

本 change 的目标是新增一个 `HostSnapshotAssembler` Host glue boundary，集中执行“用当前 source list、workspace settings、native snapshot planners 和 recent-event state 组装 `HostSnapshot`”这一职责。controller 继续负责 refresh、command routing、planner orchestration、state/effect application 和 source reconciliation；assembler 负责 snapshot DTO assembly。该 change 不把 snapshot generation 迁移到 native，也不改变 HostBridge DTO 或 Web UI 行为。

## Scope

### In Scope

- 新增 `HostSnapshotAssembler`，封装 `HostSnapshot` / `HostDetailSnapshot` / `HostFrequencyCollisionSnapshot` 组装。
- 将 `DesktopMeterHostController.BuildSnapshot`、`BuildDetailSnapshot` 和相关 private mapping helpers 移入 assembler。
- controller 通过 assembler rebuild snapshot，保留现有 `Snapshot` 结果、状态文本 override、selected detail、settings、commands 和 recent events 行为。
- 增加 focused unit tests，证明 assembler 输出与 native-backed detail/aggregate projection 行为一致。
- 保留并运行现有 `DesktopMeterHostControllerTests`，证明外部 command/snapshot 行为不漂移。
- 更新 `.aiknowledge/codemap/host-contract.md` 和必要相关 codemap，记录 snapshot assembly boundary 与剩余架构缺口。

### Out Of Scope

- 不修改 native C++ exports、C ABI 或 native planner record types。
- 不新增 native snapshot generation core；该 change 只是 Host glue boundary 隔离。
- 不修改 HostBridge DTO、Web UI、WebView transport、macOS runner、startup/tray/installer。
- 不修改 workspace schema、settings persistence 或 service interfaces。
- 不运行或声明最终 completion audit 通过。

## Success Criteria

- `DesktopMeterHostController` 不再包含 selected-detail snapshot DTO assembly helpers。
- `HostSnapshotAssembler` 作为命名清晰、可测试的 Host glue class 负责 aggregate/detail snapshot DTO assembly。
- 现有 Host controller command/snapshot tests 继续通过。
- focused assembler tests 覆盖 selected detail module/collision mapping、status/settings/commands/recent-event projection。
- Codemap 记录 snapshot assembly 已从 controller 隔离，但 Host 仍未达到 glue-only。

## Requirements Summary

- [R1] Host snapshot assembly shall be delegated from `DesktopMeterHostController` to a dedicated Host boundary.
- [R2] The dedicated boundary shall preserve aggregate snapshot fields and selected detail snapshot mapping.
- [R3] Existing Host command externally observable snapshots shall remain unchanged.
- [R4] Codemap shall record the new snapshot assembly boundary and remaining incomplete architecture work.

## Design Summary

新增 internal sealed `HostSnapshotAssembler`，构造时接收 `UserWorkspaceSettings` 和 `NativeHostRecentEvents`。它暴露 `BuildSnapshot(IReadOnlyList<HostSourceSnapshot> sources, string? selectedSourceKey, bool showHiddenSources, string? statusTextOverride)`，内部调用现有 native aggregate/chrome/selected-detail snapshot planners，并返回 `HostSnapshot`。controller 在初始化、refresh、command handlers 和 `RebuildFromLastSources` 中调用 assembler，而不是自己构造 snapshot DTO。

## Files / Ownership

### Allowed Files

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - 接入 snapshot assembler。
- `src/DesktopLoudnessMeter.App/Host/HostSnapshotAssembler.cs` - 新增 snapshot assembly boundary。
- `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotAssemblerTests.cs` - 新增 focused tests。
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - 仅当 characterization 需要小范围补充时修改。
- `.aiknowledge/codemap/host-contract.md` - 更新 Host contract 状态。
- `.aiknowledge/codemap/source-list-workspace.md` - 如需说明 source workspace snapshot 边界则更新。
- `openspec/changes/2026-07-01-host-snapshot-assembly-boundary/**` - 本 change 产物。

### Forbidden Files

- `src/DesktopLoudnessMeter.Native/**`
- Web UI / HostBridge DTO / WebView transport files
- macOS runner files
- app startup/tray/installer files
- service interface definitions
- workspace schema or persistence model definitions

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-host-snapshot-assembly-boundary`
- Base Branch: `codex/2026-07-01-host-runtime-service-effects-boundary`
- Merge Target: `codex/2026-07-01-host-runtime-service-effects-boundary`
- Workspace Rationale:
  - dirty 分类：创建分支前工作树干净；创建 OpenSpec change 后 dirty 文件仅属于本 change。
  - 链式判断：该 change 基于刚完成的 Host runtime service effect boundary，继续推进同一条 Host glue reduction 线。
  - isolated 触发：本 change 单 agent、边界集中、无需依赖隔离或并行 worker，不触发 isolated worktree。
  - 结论：使用 `current-worktree`。

## Verification Plan

- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostSnapshotAssemblerTests`，预期先红后绿。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostSnapshotAssemblerTests|FullyQualifiedName~DesktopMeterHostControllerTests"`，预期通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`，预期通过。
- `rg -n "BuildDetailSnapshot|ToSelectedDetailSources|ToSelectedDetailLayouts|ToHostFrequencyCollisionSnapshot" src\DesktopLoudnessMeter.App\Host\DesktopMeterHostController.cs src\DesktopLoudnessMeter.App\Host\HostSnapshotAssembler.cs`，预期 detail assembly helpers 只在 assembler 中。
- `git diff --check`，预期通过。

## Stabilization / Manual Platform Uncertainty

- 是否需要 `[stabilization-first]`: no
- 不确定性来源: none；这是 managed Host glue refactor，可用 unit/controller tests 覆盖。
- 人工 / 平台覆盖: 不需要新增人工验收；不触碰 UI runtime、capture transport、installer 或 macOS runner。
- 最终硬化策略: focused unit tests + existing Host controller regression tests + full managed test project.

## Rollback / Escalation

- 回退方式: revert 本 change 的 commit，恢复 controller 内联 snapshot assembly。
- 升级条件: 若需要修改 native ABI、HostBridge payload、workspace schema，或改变 `HostSnapshot` public shape，则停止实现并回到 plan 拆分。

## Bugfix Root Cause

- none；source_type=refactor。

## Readiness Notes

- 已读入口证据：`opsx changes list` 无活动变更；归档架构规划；`.aiknowledge/codemap/index.md`；`ui-runtime-architecture`；`host-contract`；`source-list-workspace`；`native-dsp`；`macos-runner-boundary`。
- 已证实缺口：`DesktopMeterHostController` 仍内联 `BuildSnapshot` / `BuildDetailSnapshot` / selected-detail mapping helpers。
- 推断：隔离 snapshot assembly 会降低 controller 职责混杂，为后续更完整的 native snapshot generation 下沉提供替换点；该推断不等于最终架构完成。
