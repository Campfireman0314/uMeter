# Work Packet: 2026-07-01-host-runtime-service-effects-boundary

## Metadata

- profile: lean-change
- source_type: refactor
- status: planning
- created: 2026-07-01

## Intent

已证实：`DesktopMeterHostController` 已将 reset/performance/source-target 等决策委托给 native planners，并已通过 `HostWorkspaceCommandEffectApplier` 隔离 workspace-backed persistence effects；但 runtime/service side effects 仍直接散落在 controller command handlers 中，包括 reset all/source、performance mode forwarding、true-peak oversampling forwarding、waveform/spectrum channel enable forwarding。

本 change 的目标是新增一个 Host glue boundary 来应用 runtime service/analyzer setting side effects，使 controller 更接近 command router + planner orchestrator + snapshot coordinator。该 change 不把服务实现迁移到 native，不改变 command DTO 或 HostBridge payload。

## Scope

### In Scope

- 新增 `HostRuntimeServiceEffectApplier`，封装对 `ISignalSourceResetService` 和 `ISignalSourceAnalysisSettingsService` 的副作用调用。
- 更新 `DesktopMeterHostController`，让 reset/performance/analyzer setting forwarding 通过该 boundary 完成。
- 增加 focused unit tests 覆盖 runtime service effect boundary 在支持/不支持服务接口时的行为。
- 保留现有 controller command 行为、native planner API、workspace effect boundary、HostBridge DTO 和 Web UI 行为。
- 更新 `.aiknowledge/codemap/host-contract.md` 和必要的相关 codemap，记录 runtime service effect boundary 与剩余架构缺口。

### Out Of Scope

- 不修改 native C++ exports、C ABI、planner record types 或 source ingest core。
- 不修改 `ISignalSourceMeterService` / reset / analysis settings service interfaces。
- 不迁移 workspace schema/persistence，不改 `HostWorkspaceCommandEffectApplier` 职责。
- 不修改 Web UI、HostBridge DTO、WebView transport、macOS runner、startup/tray/installer。
- 不运行或声明最终 completion audit 通过。

## Success Criteria

- `DesktopMeterHostController` 不再直接调用 `ResetAllSources`、`ResetSource`、`SetPerformanceMode`、`SetTruePeakOversampling`、`SetChannelWaveformEnabled` 或 `SetChannelSpectrumEnabled`。
- runtime/service side effects 由一个命名清晰、可测试的 Host glue class 承担。
- 不支持对应 service interface 时，applier 静默 no-op，保持现有 controller 行为。
- 现有 Host controller tests 继续通过。
- 新增 focused tests 覆盖 reset/performance/analysis forwarding 的 apply 行为。
- Codemap 记录 Host 进一步缩减，但仍未完全 glue-only。

## Requirements Summary

- [R1] Host controller shall delegate runtime service side-effect application to a dedicated Host boundary.
- [R2] The dedicated boundary shall preserve reset-all, reset-source, performance mode, true-peak, waveform, and spectrum forwarding behavior.
- [R3] Existing Host command externally observable behavior shall remain unchanged.
- [R4] Codemap shall record the new boundary and remaining incomplete architecture work.

## Design Summary

新增 `HostRuntimeServiceEffectApplier`，构造时接收 `ISignalSourceMeterService`。它根据服务对象是否实现 `ISignalSourceResetService` / `ISignalSourceAnalysisSettingsService` 来应用 native/runtime/chart plans 中已经计算好的 side effects。Controller 仍负责 native planner invocation、source existence checks、runtime state application、workspace effect application 和 snapshot rebuild。

## Files / Ownership

### Allowed Files

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - 接入 runtime service effect boundary。
- `src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs` - 新增 Host runtime/service effect boundary。
- `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs` - 新增 focused tests。
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - 仅当 characterization 需要补强时修改。
- `.aiknowledge/codemap/host-contract.md` - 更新 Host contract 状态。
- `.aiknowledge/codemap/source-list-workspace.md` - 如需说明 analyzer forwarding 边界则更新。
- `openspec/changes/2026-07-01-host-runtime-service-effects-boundary/**` - 本 change 产物。

### Forbidden Files

- `src/DesktopLoudnessMeter.Native/**`
- Web UI / HostBridge DTO / WebView transport files
- macOS runner files
- app startup/tray/installer files
- service interface definitions, unless implementation proves impossible and scope returns to plan

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-host-runtime-service-effects-boundary`
- Base Branch: `codex/2026-07-01-host-workspace-command-effects-boundary`
- Merge Target: `codex/2026-07-01-host-workspace-command-effects-boundary`
- Workspace Rationale: baseline clean after commit `8046811`; this change is single-agent and scoped to Host runtime/service effect glue, focused tests, codemap, and OpenSpec artifacts. It does not require isolated worktree or dependency isolation. Dirty classification after init belongs to this change.

## Verification Plan

- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests`，预期先红后绿。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostRuntimeServiceEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"`，预期通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`，预期通过。
- `git diff --check`，预期通过。

## Stabilization / Manual Platform Uncertainty

- 是否需要 `[stabilization-first]`: no
- 不确定性来源: none；目标行为为已有 managed service forwarding 的边界迁移，可用单元测试覆盖。
- 人工 / 平台覆盖: 不需要新增人工验收；不触碰 UI runtime、capture transport、installer 或 macOS runner。
- 最终硬化策略: focused unit tests + existing Host controller regression tests + full managed test project。

## Rollback / Escalation

- 回退方式: revert 本 change 的 commit，恢复 controller 内联调用 service side effects。
- 升级条件: 若需要修改 service interfaces、native ABI、HostBridge payload，或改变命令外部行为，则停止实现并回到 plan 拆分。

## Bugfix Root Cause

- none；source_type=refactor。

## Readiness Notes

- 已读入口证据：`opsx changes list` 无活动变更；归档规划；`.aiknowledge/codemap/index.md`；`ui-runtime-architecture`；`host-contract`；`source-list-workspace`；`signal-source-ingest`。
- 已证实缺口：runtime/service side-effect calls 仍内联在 controller command handlers。
- 推断：隔离 runtime/service effect application 会降低 controller 职责混杂，为继续 Host glue reduction 提供更窄边界；该推断不等同于最终架构完成。
