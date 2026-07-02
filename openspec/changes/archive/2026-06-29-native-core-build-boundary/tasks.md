# Tasks: 2026-06-29-native-core-build-boundary

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## Default Constraints

- **Forbidden Files**（默认）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
- **Shared Artifacts**（默认）：
  - `.openspec.yaml` — controller 串行更新
  - `tasks.md` — controller 串行更新
  - `evidence.md` — controller 串行更新
- **Worker Return Format**（默认）：`DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Native Build Boundary

- [x] 1.1 [stabilization-first] 新增 native core build entry

  **需求追踪**：[R1], [R5] → [U1], [U4]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt` — native core build entry
  - `src/DesktopLoudnessMeter.Native/CMakePresets.json` — optional native configure presets
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` — optional export/calling convention macro
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — optional export macro usage
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` — source set parity
  - `README.md` — native build command documentation
  - `docs/current-status.md` — native build boundary status

  **验收标准**：
  - [x] native build entry lists the same analyzer/RfRapid source files as the Windows project.
  - [x] native build entry sets C++17 for the native core target.
  - [x] target/output naming keeps Windows `DesktopLoudnessMeter.Native.dll` compatibility.
  - [~] [manual] macOS configure/build is not manually verified in this Windows session | 原因：本 change 只预留 macOS boundary，实际 macOS toolchain 验证属于 `macos-readiness`

  **验证命令 / 方法**：
  - `rg -n "DlmPcmAnalyzer\\.(Core|Loudness|RecentWindow|TruePeak)\\.cpp|DesktopLoudnessMeterNative\\.cpp|RfRapid" src/DesktopLoudnessMeter.Native/CMakeLists.txt src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`，预期：source set 可追溯。
  - `cmake --version` 或已知绝对路径检查，预期：可用则运行 configure/build；不可用则记录 allowed failure。
  - `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`，预期：Windows Release native DLL 构建成功。

  **Evidence Required**：
  - Stabilization Rationale
  - CMake/toolchain availability result
  - Source-set parity check
  - Windows native Release build result
  - Doc impact classification

  **Stabilization Rationale**：
  - 不确定性来源：CMake 当前不可用；macOS toolchain 不在本机；export macro portability 需要先用 Windows build 验证。
  - 为什么不能先写目标行为测试：构建入口和工具链可用性属于平台/环境行为，失败可能来自缺工具而非产品行为；必须先稳定 build route。
  - 退出条件：Windows native build 通过，CMake availability 被记录，source set parity 明确。

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows native Release | `build-native.ps1 -Configuration Release` | yes | DLL 构建成功 |
  | CMake configure/build | `cmake --version` then preset build if available | no | 可用则 pass；不可用则记录 allowed failure |
  | macOS native build | 后续 `macos-readiness` | no | 本 change 不宣称 pass |

  **Final Hardening Plan**：
  - 用 Windows native build、source set parity 和 ABI smoke 收口。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`，除非 implementation 发现需要改变 ABI 或范围。

  **Done When**：
  - build entry landed, Windows build still passes, unavailable CMake/macOS evidence recorded without claiming support.

  **依赖**：无

- [x] 1.2 [stabilization-first] 稳定 Windows native deployment rule

  **需求追踪**：[R2], [R3] → [U2]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/build-native.ps1` — native build helper
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` — native DLL copy target
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` — consumer import check only if needed
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` — consumer import check only if needed
  - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` — consumer import check only if needed
  - `README.md` — deployment command documentation
  - `docs/current-status.md` — deployment status

  **验收标准**：
  - [x] Release build output is copied from the current configuration when present.
  - [x] Debug managed builds can still fall back to Release native DLL when Debug native DLL is absent.
  - [x] app, tests, and benchmark projects retain explicit native copy/deployment rules.

  **验证命令 / 方法**：
  - `rg -n "DesktopLoudnessMeter.Native.targets" src tests`，预期：app/tests/benchmarks consumer imports remain explicit.
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`，预期：app builds and native copy target runs or is explainably skipped.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，预期：native interop path loads when DLL is available.
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release`，预期：benchmark native path can load DLL; if long-running, record bounded smoke alternative.

  **Evidence Required**：
  - Native copy target import scan
  - App/test/benchmark build or load result
  - Any skipped benchmark rationale

  **Stabilization Rationale**：
  - 不确定性来源：native DLL load path depends on final process output directory and current configuration.
  - 为什么不能先写目标行为测试：copy/load failures appear only after consuming project build/run chooses an output path; static tests alone cannot prove runtime deployment.
  - 退出条件：app/tests/benchmark outputs can obtain the native DLL or a bounded blocker is recorded.

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | App native copy | `dotnet build` | yes | native DLL available to app output |
  | Tests native copy/load | `dotnet test --filter NativePcmAnalyzer` | yes | load/interops pass or existing skip reason applies |
  | Benchmark native load | `dotnet run ... -c Release` | no | native path loads; long benchmark may be replaced with bounded smoke |

  **Final Hardening Plan**：
  - Run full managed tests after deployment rule stabilizes.
  - doc impact 分类：`TASK_EVIDENCE_ONLY` unless copy target behavior changes require docs update.

  **Done When**：
  - Managed native deployment is verified for app/tests/benchmark or non-blocking limits are documented.

  **依赖**：Task 1.1

## 2. ABI Guard

- [x] 2.1 [characterization-first] 守护现有 native exports

  **需求追踪**：[R4] → [U3]
  **执行方式**：[characterization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` — ABI declarations
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — export forwarding layer
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs` — P/Invoke wrapper, only if assertion/load guard is needed
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/NativePcmAnalyzerSmokeTests.cs` — ABI smoke tests

  **验收标准**：
  - [x] `NativePcmAnalyzer.cs` P/Invoke signatures do not need semantic changes.
  - [x] Windows DLL exports all current `DlmPcmAnalyzer*Export` entrypoints used by the wrapper.
  - [x] snapshot layout and copy API behavior remain covered by existing native smoke tests.

  **验证命令 / 方法**：
  - `rg -n "DllImport|DlmPcmAnalyzer.*Export" src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`，预期：entrypoints match.
  - `dumpbin /exports src/DesktopLoudnessMeter.Native/x64/Release/DesktopLoudnessMeter.Native.dll` or available equivalent，预期：exports are present; if tool unavailable, record fallback.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，预期：pass.

  **Evidence Required**：
  - Entry point scan
  - Export inspection or fallback rationale
  - Native smoke test result

  **Done When**：
  - ABI compatibility is characterized by scan/export/test evidence.

  **依赖**：Task 1.1

## 3. Final Verification Surface

- [x] 3.1 [direct] 更新 build boundary 文档

  **需求追踪**：[R5] → [U4]
  **执行方式**：[direct]
  **Allowed Files**：
  - `README.md` — user-facing native build instructions
  - `docs/current-status.md` — current native/core status
  - `docs/**` — additional native build docs if an existing page is more appropriate

  **验收标准**：
  - [x] docs show Windows verified build command.
  - [x] docs identify CMake/macOS path as reserved or follow-up verification.
  - [x] docs do not describe Flutter as the future main UI runtime.

  **验证命令 / 方法**：
  - `rg -n "build-native|CMake|macOS|Flutter|DesktopLoudnessMeter.Native" README.md docs/current-status.md docs`，预期：native build text is current and no future Flutter route is introduced.

  **Evidence Required**：
  - Documentation grep result
  - Summary of doc changes

  **Done When**：
  - Docs accurately describe Windows verified path and reserved macOS boundary.

  **依赖**：Task 1.1

- [x] 3.2 [stabilization-first] 完成 native build boundary hardening

  **需求追踪**：[R1], [R2], [R3], [R4], [R5] → [U1], [U2], [U3], [U4]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/**` — final build fixes within scope
  - `tests/DesktopLoudnessMeter.Tests/**` — final smoke/characterization fixes within scope
  - `tests/DesktopLoudnessMeter.Benchmarks/**` — final benchmark load fixes within scope
  - `README.md` — final doc drift fixes
  - `docs/**` — final doc drift fixes

  **验收标准**：
  - [x] Windows native Release build passes.
  - [x] `NativePcmAnalyzer` tests pass.
  - [x] full managed test suite passes or any environment skip is documented.
  - [x] CMake/macOS limitations are recorded as non-blocking boundary evidence.

  **验证命令 / 方法**：
  - `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`，预期：exit 0.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，预期：exit 0.
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：exit 0.
  - `git diff -- openspec/changes/2026-06-29-native-core-build-boundary src/DesktopLoudnessMeter.Native tests/DesktopLoudnessMeter.Tests tests/DesktopLoudnessMeter.Benchmarks README.md docs`，预期：diff is within scope.

  **Evidence Required**：
  - Final native build result
  - Native smoke and full test result
  - Scope diff summary
  - Final hardening notes

  **Stabilization Rationale**：
  - 不确定性来源：native build, deployment, export inspection and managed load must converge together.
  - 为什么不能先写目标行为测试：final hardening spans toolchain commands and runtime load paths, where failures can be environmental or stale-binary related.
  - 退出条件：fresh build/test evidence covers the final diff.

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows native build | `build-native.ps1` | yes | pass |
  | Managed native smoke | `dotnet test --filter NativePcmAnalyzer` | yes | pass |
  | Full managed regression | `dotnet test` | yes | pass |
  | macOS build | follow-up `macos-readiness` | no | not claimed |

  **Final Hardening Plan**：
  - If a stale DLL or copy lock appears, rebuild native DLL, clear only current output artifacts when safe, then rerun smoke.
  - doc impact 分类：`TASK_EVIDENCE_ONLY` unless implementation changes scope or ABI.

  **Done When**：
  - All final hardening evidence is fresh and ready for opsx-verify.

  **依赖**：Task 1.1, Task 1.2, Task 2.1, Task 3.1
