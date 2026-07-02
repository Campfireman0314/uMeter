# Tasks: 2026-07-01-host-runtime-service-effects-boundary

## Default Constraints

- Forbidden Files: native C++ sources/exports, service interface definitions, HostBridge DTO files, Web UI files, macOS runner files, startup/tray/installer files unless plan is amended.
- Shared Artifacts: `.openspec.yaml`, `tasks.md`, and `evidence.md` are updated only by the main agent/controller.
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Runtime Service Effect Boundary

- [x] 1.1 [test-first] Add focused tests for runtime service effects.

  **需求追踪**: [R2] -> [U1]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs` - new focused tests

  **验收标准**:
  - [x] Tests cover reset-all and reset-source forwarding.
  - [x] Tests cover performance mode forwarding.
  - [x] Tests cover true-peak, waveform, and spectrum analyzer forwarding.
  - [x] Tests cover no-op behavior when optional service interfaces are not implemented.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests`，预期先失败，完成实现后通过。

  **Evidence Required**:
  - red/failing test evidence or compile failure before implementation
  - passing focused test evidence after implementation

  **Done When**:
  - focused tests exist and fail for missing applier before implementation.

  **依赖**: none

- [x] 1.2 [test-first] Implement `HostRuntimeServiceEffectApplier`.

  **需求追踪**: [R1], [R2] -> [U1]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs` - new boundary
  - `tests/DesktopLoudnessMeter.Tests/Host/HostRuntimeServiceEffectApplierTests.cs` - update only if compile requires exact method names

  **验收标准**:
  - [x] Applier applies reset/performance/analyzer forwarding plans through optional service interfaces.
  - [x] Applier is no-op when optional interfaces are absent.
  - [x] Focused tests pass.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostRuntimeServiceEffectApplierTests`，预期通过。

  **Evidence Required**:
  - focused passing test evidence

  **Done When**:
  - applier tests pass and controller integration has not changed behavior.

  **依赖**: Task 1.1

## 2. Controller Integration

- [x] 2.1 [characterization-first] Replace controller-local service side effects with applier calls.

  **需求追踪**: [R1], [R3] -> [U2], [U3]
  **执行方式**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - integration
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - only if existing characterization needs small assertion updates

  **验收标准**:
  - [x] Controller command handlers do not directly call reset or analysis setting service methods.
  - [x] Existing Host controller command tests pass.
  - [x] Runtime state and workspace effect responsibilities remain separate.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostRuntimeServiceEffectApplierTests|FullyQualifiedName~DesktopMeterHostControllerTests"`，预期通过。

  **Evidence Required**:
  - targeted controller regression evidence

  **Done When**:
  - controller integration passes targeted Host tests.

  **依赖**: Task 1.2

## 3. Knowledge And Final Validation

- [x] 3.1 [direct] Update codemap with the new Host runtime service effect boundary.

  **需求追踪**: [R4] -> [U4]
  **执行方式**: [direct]
  **Allowed Files**:
  - `.aiknowledge/codemap/host-contract.md` - Host boundary status
  - `.aiknowledge/codemap/source-list-workspace.md` - analyzer forwarding boundary note if needed

  **验收标准**:
  - [x] Codemap records that runtime service effect application is isolated.
  - [x] Codemap keeps remaining architecture gaps explicit.

  **验证命令 / 方法**:
  - `rg -n "runtime service effect|HostRuntimeServiceEffectApplier|does not yet qualify|not complete" .aiknowledge\codemap\host-contract.md .aiknowledge\codemap\source-list-workspace.md`，预期命中新边界和 remaining gaps。

  **Evidence Required**:
  - codemap grep evidence

  **Done When**:
  - knowledge reflects actual implementation without overclaiming completion.

  **依赖**: Task 2.1

- [x] 3.2 [direct] Run final managed validation.

  **需求追踪**: [R1], [R2], [R3], [R4] -> [U1], [U2], [U3], [U4]
  **执行方式**: [direct]
  **Allowed Files**:
  - `openspec/changes/2026-07-01-host-runtime-service-effects-boundary/evidence.md` - validation evidence
  - `openspec/changes/2026-07-01-host-runtime-service-effects-boundary/tasks.md` - task status

  **验收标准**:
  - [x] Full managed test project passes.
  - [x] `git diff --check` passes.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`，预期通过。
  - `git diff --check`，预期通过。

  **Evidence Required**:
  - full test evidence
  - diff check evidence

  **Done When**:
  - all tasks complete and evidence is ready for opsx-verify.

  **依赖**: Task 3.1
