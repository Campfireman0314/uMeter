# Tasks: 2026-06-29-native-source-ingest-core

正文默认使用中文；路径、命令、配置键和代码标识保持原文。

## Default Constraints

- Forbidden Files:
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Native source ingest core

- [x] 1.1 [test-first] 增加 lifecycle parity 测试并确认当前行为基线

  **需求追踪**: [R1] -> [U1], [R2] -> [U2]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - lifecycle/reconfiguration regression tests

  **验收标准**:
  - [x] duplicate display name、rename、same-handle same-format、same-handle changed-format、heartbeat、timeout、close、reregister 行为均有 focused assertions。
  - [x] 新增测试在当前 C# implementation 上可表达目标行为，作为 native migration parity oracle。

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`，预期通过。

  **Evidence Required**:
  - focused test command and result。

  **Done When**:
  - tests encode target lifecycle invariants and pass before native adapter rewrite or fail only for newly intended native-only cases documented in evidence。

  **依赖**: 无

- [x] 1.2 [test-first] 实现 native source registry 与 C ABI lifecycle functions

  **需求追踪**: [R1] -> [U1]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h` - native API/types
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp` - source registry/state machine
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - export wrappers
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*` - limited helper access if required
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/*` - native smoke coverage if needed

  **验收标准**:
  - [x] native core can create/destroy a registry and accept register/heartbeat/rename/audio/close/remove-expired calls.
  - [x] close/timeout releases source analyzer state.
  - [x] invalid register/audio inputs return or map to ignored behavior without corrupting existing sources.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~Native`，预期通过或记录 unavailable/tooling blocker。

  **Evidence Required**:
  - native smoke/parity command result or explicit blocker。

  **Done When**:
  - native lifecycle functions exist, compile into the native project file list task, and are exercised by managed/native tests.

  **依赖**: Task 1.1

## 2. Managed adapter and snapshot parity

- [x] 2.1 [test-first] 接入 C# native wrapper and preserve `LocalLoopbackSignalSourceService` public behavior

  **需求追踪**: [R2] -> [U2], [R4] -> [U4]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs` - P/Invoke wrapper and DTO mapping
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - service adapter
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - adapter parity tests

  **验收标准**:
  - [x] service still implements existing interfaces and constructor patterns.
  - [x] source settings commands and reset commands work through native state.
  - [x] same-handle changed format preserves source key/settings and rebuilds analyzer metrics.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`，预期通过。

  **Evidence Required**:
  - focused service parity command and result。

  **Done When**:
  - local loopback service tests pass against native-backed adapter.

  **依赖**: Task 1.2

- [x] 2.2 [test-first] 完成 native snapshot projection and metric buffer copy

  **需求追踪**: [R3] -> [U3]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.*` - snapshot/copy functions
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs` - snapshot projection
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - `SignalSourceSnapshot` mapping
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - metrics/invalid audio assertions

  **验收标准**:
  - [x] audio processing updates peak, dB text, true-peak/RMS/loudness where available, waveform and spectrum arrays.
  - [x] channel waveform/spectrum switches control channel arrays.
  - [x] invalid audio packets are ignored and prior state remains intact.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`，预期通过。

  **Evidence Required**:
  - focused metric/snapshot command and result。

  **Done When**:
  - `SignalSourceSnapshot` semantics remain compatible with current host/UI consumers.

  **依赖**: Task 2.1

## 3. Build boundaries and regression closure

- [x] 3.1 [direct] 更新 native MSBuild/CMake file lists

  **需求追踪**: [R5] -> [U5]
  **执行方式**: [direct]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` - Windows verified project file list
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt` - reserved cross-platform build file list

  **验收标准**:
  - [x] new native source/header files are included in `.vcxproj`.
  - [x] new native source files are included in `CMakeLists.txt`.

  **验证命令 / 方法**:
  - `rg -n "DlmSourceIngestCore" src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj src/DesktopLoudnessMeter.Native/CMakeLists.txt`，预期命中。

  **Evidence Required**:
  - file-list inspection command and result。

  **Done When**:
  - both Windows and CMake native boundaries reference new source ingest files.

  **依赖**: Task 1.2

- [x] 3.2 [characterization-first] 运行 full regression/build sanity 并修复 scope 内问题

  **需求追踪**: [R5] -> [U5]
  **执行方式**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Native/**` - fixes owned by this change
  - `src/DesktopLoudnessMeter.App/Audio/**` - adapter fixes owned by this change
  - `tests/DesktopLoudnessMeter.Tests/Audio/**` - regression fixes owned by this change
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - only if native deployment wiring is required

  **验收标准**:
  - [x] focused local loopback tests pass.
  - [x] native smoke/parity tests pass or unavailable tooling is documented with fallback coverage.
  - [x] full .NET tests pass.
  - [x] app build passes.
  - [x] web tests/build remain green.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~Native`
  - `dotnet test uMeter.sln`
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `npm --prefix src/DesktopLoudnessMeter.Web test -- --run`
  - `npm --prefix src/DesktopLoudnessMeter.Web run build`

  **Evidence Required**:
  - command results and any scoped fixes made after failures。

  **Done When**:
  - regression/build sanity is fresh in `evidence.md` and remaining gaps are explicit.

  **依赖**: Task 2.2, Task 3.1

- [x] 3.3 [direct] 更新 OpenSpec evidence and codemap ownership notes

  **需求追踪**: [R5] -> [U5]
  **执行方式**: [direct]
  **Allowed Files**:
  - `.aiknowledge/codemap/signal-source-ingest.md` - source ingest ownership update
  - `.aiknowledge/codemap/native-dsp.md` - native core boundary update
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md` - source ingest metric flow update

  **验收标准**:
  - [x] evidence records explore, plan, readiness, implementation and verification facts.
  - [x] codemap distinguishes native source ingest ownership from Windows capture adapter and WebView UI boundaries.

  **验证命令 / 方法**:
  - `rg -n "native source|DlmSourceIngestCore|source ingest|LocalLoopbackSignalSourceService" .aiknowledge/codemap openspec/changes/2026-06-29-native-source-ingest-core/evidence.md`，预期命中 updated notes。

  **Evidence Required**:
  - docs/codemap inspection command and result。

  **Done When**:
  - docs reflect the implementation and no out-of-scope module is claimed complete.

  **依赖**: Task 3.2
