# Tasks: 2026-07-01-host-snapshot-assembly-boundary

## Default Constraints

- Forbidden Files: native C++ sources/exports, service interface definitions, HostBridge DTO files, Web UI files, macOS runner files, startup/tray/installer files, workspace schema files unless plan is amended.
- Shared Artifacts: `.openspec.yaml`, `tasks.md`, and `evidence.md` are updated only by the main agent/controller.
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Snapshot Assembler Boundary

- [x] 1.1 [test-first] Add focused tests for Host snapshot assembly.

  **需求追踪**: [R2] -> [U1], [U3]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotAssemblerTests.cs` - new focused tests

  **验收标准**:
  - [x] Tests cover aggregate status/settings/command/recent event projection.
  - [x] Tests cover selected-detail module visibility and collision mapping.
  - [x] Tests cover status text override precedence.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostSnapshotAssemblerTests`，预期先失败，完成实现后通过。

  **Evidence Required**:
  - red/failing test evidence or compile failure before implementation
  - passing focused test evidence after implementation

  **Done When**:
  - focused tests exist and fail for missing assembler before implementation.

  **依赖**: none

- [x] 1.2 [test-first] Implement `HostSnapshotAssembler`.

  **需求追踪**: [R1], [R2] -> [U1]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshotAssembler.cs` - new boundary
  - `tests/DesktopLoudnessMeter.Tests/Host/HostSnapshotAssemblerTests.cs` - update only if compile requires exact method names

  **验收标准**:
  - [x] Assembler builds aggregate Host snapshots using native aggregate/chrome/detail planners.
  - [x] Assembler maps selected-detail native plans to Host DTOs.
  - [x] Focused tests pass.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~HostSnapshotAssemblerTests`，预期通过。

  **Evidence Required**:
  - focused passing test evidence

  **Done When**:
  - assembler tests pass and controller integration has not changed behavior.

  **依赖**: Task 1.1

## 2. Controller Integration

- [x] 2.1 [characterization-first] Replace controller-local snapshot assembly with assembler calls.

  **需求追踪**: [R1], [R3] -> [U2], [U3]
  **执行方式**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - integration
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - only if existing characterization needs small assertion updates

  **验收标准**:
  - [x] Controller no longer contains selected-detail DTO assembly helper methods.
  - [x] Existing Host controller command tests pass.
  - [x] `LastSourceSnapshots` rebuild semantics remain unchanged.

  **验证命令 / 方法**:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostSnapshotAssemblerTests|FullyQualifiedName~DesktopMeterHostControllerTests"`，预期通过。
  - `rg -n "BuildDetailSnapshot|ToSelectedDetailSources|ToSelectedDetailLayouts|ToHostFrequencyCollisionSnapshot" src\DesktopLoudnessMeter.App\Host\DesktopMeterHostController.cs src\DesktopLoudnessMeter.App\Host\HostSnapshotAssembler.cs`，预期 helper 只在 assembler 中。

  **Evidence Required**:
  - targeted controller regression evidence
  - search evidence

  **Done When**:
  - controller integration passes targeted Host tests and search evidence confirms helper extraction.

  **依赖**: Task 1.2

## 3. Knowledge And Final Validation

- [x] 3.1 [direct] Update codemap with the new Host snapshot assembly boundary.

  **需求追踪**: [R4] -> [U4]
  **执行方式**: [direct]
  **Allowed Files**:
  - `.aiknowledge/codemap/host-contract.md` - Host boundary status
  - `.aiknowledge/codemap/source-list-workspace.md` - snapshot boundary note if needed

  **验收标准**:
  - [x] Codemap records that snapshot assembly is isolated.
  - [x] Codemap keeps remaining architecture gaps explicit.

  **验证命令 / 方法**:
  - `rg -n "snapshot assembly|HostSnapshotAssembler|does not yet qualify|not complete|completion audit" .aiknowledge\codemap\host-contract.md .aiknowledge\codemap\source-list-workspace.md`，预期命中新边界和 remaining gaps。

  **Evidence Required**:
  - codemap grep evidence

  **Done When**:
  - knowledge reflects actual implementation without overclaiming completion.

  **依赖**: Task 2.1

- [x] 3.2 [direct] Run final managed validation.

  **需求追踪**: [R1], [R2], [R3], [R4] -> [U1], [U2], [U3], [U4]
  **执行方式**: [direct]
  **Allowed Files**:
  - `openspec/changes/2026-07-01-host-snapshot-assembly-boundary/evidence.md` - validation evidence
  - `openspec/changes/2026-07-01-host-snapshot-assembly-boundary/tasks.md` - task status

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
