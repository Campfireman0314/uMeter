# Work Packet: 2026-07-01-host-workspace-command-effects-boundary

## Metadata

- profile: lean-change
- source_type: feature
- status: planning
- created: 2026-07-01

## Intent

已证实：`DesktopMeterHostController` 已经把 source projection、recent events、aggregate snapshot、selected detail snapshot、source order/preference/global/chart/runtime 等决策委托给 native planners，但仍在 command handlers 内直接执行 `UserWorkspaceSettings.GetOrCreateSource`、`ForgetSource` 和 `Save()` 来落 workspace/settings 状态。

本 change 的目标是把 native command plan 到 workspace/settings 持久化副作用的应用逻辑隔离为明确的 Host glue boundary，使 controller 更接近 command router + snapshot coordinator。该 change 不宣称最终架构完成，只消除一个已证实的 Host controller 职责混杂点。

## Scope

### In Scope

- 新增 Host workspace command effect boundary，用于应用 native global/source preference/source order/chart settings plans 到 `UserWorkspaceSettings`。
- 更新 `DesktopMeterHostController`，让 workspace 状态写入、source row mutation、forget source、save 语义通过该 boundary 完成。
- 增加 focused unit tests 覆盖 effect boundary 对 global/source/chart workspace state 的应用行为。
- 保留现有 controller command 行为、Host snapshot DTO、HostBridge DTO、native planner API 和 Web UI 行为。
- 更新 `.aiknowledge/codemap/host-contract.md` 和相关 codemap，记录 Host glue 缩减后的真实边界。

### Out Of Scope

- 不把 workspace schema 或 settings persistence 迁移到 C++ Core。
- 不修改 native planner exports、C++ Core ABI、HostBridge DTO 或 Web UI payload。
- 不实现 macOS WKWebView runner、macOS capture adapter、packaging/signing/notarization。
- 不退休 WPF，不迁移高频图表 parity。
- 不运行最终 completion audit，也不把本模块完成视为总目标完成。

## Success Criteria

- `DesktopMeterHostController` 不再直接调用 `GetOrCreateSource`、`ForgetSource` 或直接为 workspace command effects 调用 `Save()`。
- workspace-backed command side effects 由一个命名清晰、可测试的 Host glue class 承担。
- 现有 Host controller command tests 继续通过。
- 新增 effect boundary tests 覆盖 global settings、source preference/order 和 chart settings 持久化行为。
- codemap 明确记录：Host 仍未完全 glue-only；本 change 仅隔离 workspace effect application。

## Requirements Summary

- [R1] Host controller shall delegate workspace-backed command effect application to a dedicated Host boundary.
- [R2] The dedicated boundary shall preserve existing workspace state mutations for workspace-backed global, source preference, source order, and chart settings commands.
- [R3] Existing Host command externally observable behavior shall remain unchanged.
- [R4] Codemap shall be updated to reflect the new boundary and remaining goal gaps.

## Design Summary

新增 `HostWorkspaceCommandEffectApplier`，以 `UserWorkspaceSettings` 为依赖，提供针对 workspace-backed native plans 的 apply 方法。Controller 仍负责 command routing、native planner invocation、runtime/service forwarding、show-hidden runtime state、snapshot rebuild；workspace mutation/save 则转移到 applier。

该方案是 Host glue 内部边界重排，不改变 transport、DTO 或 native ABI。它为后续继续收缩 Host controller 留出更窄的集成面。

## Files / Ownership

### Allowed Files

- `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - 调整 command handlers 使用 effect boundary。
- `src/DesktopLoudnessMeter.App/Host/HostWorkspaceCommandEffectApplier.cs` - 新增 workspace effect boundary。
- `tests/DesktopLoudnessMeter.Tests/Host/HostWorkspaceCommandEffectApplierTests.cs` - 新增 focused tests。
- `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - 仅当需要保持/补强 characterization 时修改。
- `.aiknowledge/codemap/host-contract.md` - 更新 Host contract 状态。
- `.aiknowledge/codemap/source-list-workspace.md` - 如触及 workspace ownership 说明则更新。
- `openspec/changes/2026-07-01-host-workspace-command-effects-boundary/**` - 本 change 产物。

### Forbidden Files

- Native C++ sources/exports。
- HostBridge DTO contract files。
- Web UI runtime files。
- macOS runner files。
- WPF UI files，除非测试暴露现有编译问题且需回到 plan。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-host-workspace-command-effects-boundary`
- Base Branch: `codex/2026-07-01-web-hostbridge-wkwebview-transport`
- Merge Target: `codex/2026-07-01-web-hostbridge-wkwebview-transport`
- Workspace Rationale: baseline clean；新 change 从上一轮已归档提交 `3ef2a72` 建立专用分支；本轮为单 agent、单仓库、范围集中在 Host workspace effect boundary 和对应 tests/codemap，不需要 isolated worktree。dirty 分类：创建分支前 clean；后续变更均应属于本 change。

## Verification Plan

- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostWorkspaceCommandEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"`，预期通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`，预期通过。
- `git diff --check`，预期无 whitespace/error。

## Stabilization / Manual Platform Uncertainty

- 是否需要 `[stabilization-first]`: no
- 不确定性来源: none；目标行为为已有 managed workspace mutation 行为的边界迁移，可用单元测试覆盖。
- 人工 / 平台覆盖: 不需要新增人工验收；不修改 WebView runtime、托盘、capture、installer 或 macOS runner。
- 最终硬化策略: focused unit tests + existing Host controller regression tests + full managed test project。

## Rollback / Escalation

- 回退方式: revert 本 change 的 commit，恢复 controller 直接应用 workspace effects。
- 升级条件: 若迁移需要修改 native ABI、HostBridge DTO、workspace schema，或需要改变命令外部行为，则停止实现并回到 plan 拆分新 change。

## Bugfix Root Cause

- none；source_type=feature。

## Readiness Notes

- 已读入口证据：`opsx changes list` 无活动变更；归档规划；`.aiknowledge/codemap/index.md`；`.aiknowledge/codemap/ui-runtime-architecture.md`；`.aiknowledge/codemap/host-contract.md`；`.aiknowledge/codemap/source-list-workspace.md`。
- 已证实缺口：Host controller 仍直接写 workspace settings。
- 推断：把 effect application 作为 Host glue boundary 隔离会降低 controller 职责混杂，并为后续 core/state migration 提供更窄接缝；该推断不等同于最终架构完成。
