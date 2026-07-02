# Evidence: 2026-06-29-native-core-build-boundary

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-29 18:53 +08:00
- final_verify:
  - status: pass_with_warnings
  - evidence_ref: Verify / 2026-06-29T19:05:46+08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-29T19:07:28+08:00
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-06-29-native-core-build-boundary
  - evidence_ref: Archive / 2026-06-29T19:08:41+08:00
- allowed_failures:
  - item: `cmake --version` fails because CMake is not installed or not in PATH in this Windows session.
    why_non_blocking: 本 change 已验证 Windows MSBuild native DLL、managed copy/load、ABI exports、native smoke、full managed tests 和 benchmark native load；CMake/macOS 在 work/spec/tasks 中明确为 reserved boundary，不作为本轮 Windows pass 宣称。
    owner_or_followup: `macos-readiness`
    expiry_or_recheck: 后续开始 `macos-readiness` 或本机安装 CMake 后必须重新执行 native CMake configure/build。
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Knowledge Auto / 2026-06-29T19:09:30+08:00

## Archive

### 2026-06-29T19:08:41+08:00

- stage: archive
- decision: pass
- archive_path: `openspec/changes/archive/2026-06-29-native-core-build-boundary`
- gates:
  - readiness: pass
  - verify: pass_with_warnings
  - review: pass
  - archive: pass
- specs_count: 1
- tasks_status: 5/5 complete
- manual_status:
  - `[~] macOS configure/build 本轮不验`，原因：当前为 Windows session，本 change 只提供 reserved build boundary，实际 macOS 验证属于 `macos-readiness`
- allowed_failures:
  - item: `cmake --version` fails because CMake is not installed or not in PATH in this Windows session.
    why_non_blocking: Windows MSBuild native DLL、managed copy/load、ABI exports、native smoke、full managed tests 和 benchmark native load 已验证；CMake/macOS 是 reserved boundary。
    owner_or_followup: `macos-readiness`
    expiry_or_recheck: 后续开始 `macos-readiness` 或本机安装 CMake 后必须重新执行 native CMake configure/build。
- knowledge_status:
  - status: done
  - evidence_ref: Knowledge Auto / 2026-06-29T19:09:30+08:00

### Knowledge Auto / 2026-06-29T19:09:30+08:00

- mode: auto
- pre_knowledge_status: clean
- post_knowledge_status:
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/native-dsp.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `M .aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`
- diff_stat:
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md | 4 +++-`
  - `.aiknowledge/codemap/index.md | 2 +-`
  - `.aiknowledge/codemap/native-dsp.md | 11 ++++++++---`
  - `.aiknowledge/logs/2026-06.md | 1 +`
  - `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md | 4 ++++`
- summary:
  - `native-dsp` now records Windows-verified MSVC DLL/deployment path plus reserved CMake boundary.
  - `pcm-metric-flow` records the current native DLL output path and CMake boundary distinction.
  - `native-consumers-must-import-copy-targets` refreshed with the build-entry/copy-target compatibility lesson.

## Explore Baseline

### 2026-06-29 Native build boundary exploration

- stage: explore
- commands:
  - `opsx changes list`
  - `Get-Content openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `Get-Content .aiknowledge/codemap/index.md`
  - `Get-Content .aiknowledge/codemap/native-dsp.md`
  - `Get-Content .aiknowledge/codemap/audio-analysis.md`
  - `Get-Content .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `rg --files src/DesktopLoudnessMeter.Native src tests | rg "(Native|Dlm|CMake|vcxproj|targets|props|csproj|DllImport|NativeMethods|PcmMetricAnalyzer|Benchmark)"`
  - `Get-Content src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets`
  - `Get-Content src/DesktopLoudnessMeter.Native/build-native.ps1`
  - `rg -n "<(Project|ItemGroup|PropertyGroup|ClCompile|Configuration|Platform|OutDir|TargetName|AdditionalOptions|LanguageStandard|RuntimeLibrary|PreprocessorDefinitions|AdditionalIncludeDirectories|Optimization|EnableEnhancedInstructionSet)" src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - `rg --files -g "CMakeLists.txt" -g "CMakePresets.json" -g "*.cmake"`
  - `cmake --version`
- facts:
  - `opsx changes list` returned `无活动变更` before this change was created.
  - `DesktopLoudnessMeter.Native.vcxproj` has only `Debug|x64` and `Release|x64`; it builds a dynamic library with C++17 and AVX2.
  - `.vcxproj` source set includes `DesktopLoudnessMeterNative.cpp`, `DlmPcmAnalyzer.Core.cpp`, `DlmPcmAnalyzer.Loudness.cpp`, `DlmPcmAnalyzer.RecentWindow.cpp`, `DlmPcmAnalyzer.TruePeak.cpp`, `RfRapid/RfCPUID.cpp`, and `RfRapid/RfRapid.cpp`.
  - `DesktopLoudnessMeter.Native.targets` copies `x64/<Configuration>/DesktopLoudnessMeter.Native.dll` first and falls back to `x64/Release`.
  - `build-native.ps1` currently finds Visual Studio MSBuild with `vswhere` and builds the `.vcxproj` for `x64`.
  - `NativePcmAnalyzer.cs` imports `DesktopLoudnessMeter.Native` and calls `DlmPcmAnalyzer*Export` functions with `CallingConvention.Cdecl`.
  - No `CMakeLists.txt` exists under `src/DesktopLoudnessMeter.Native`; JUCE native projects do have CMake files.
  - `cmake` is not available in PATH and `C:\Program Files\CMake\bin\cmake.exe` was not found in this environment.
- self_rebuttal:
  - Blocking issue checked: source ingest/protocol migration is not required to make build boundary useful; it remains a later change.
  - Blocking issue checked: macOS cannot be claimed from Windows-only evidence; plan records it as reserved boundary.
  - Non-blocking issue retained: CMake configure/build may be untestable in this environment; tasks require availability evidence and Windows parity proof.
  - Non-blocking issue retained: export macro portability could break Windows ABI; tasks require `NativePcmAnalyzer` smoke and export inspection.
- decision:
  - Scope is sufficient for `KEEP_ONE_CHANGE` with `profile: strict-change`.

## Readiness

### 2026-06-29 18:53 +08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `opsx changes list` -> `2026-06-29-native-core-build-boundary [tasks (0/5) profile=strict-change next=opsx-readiness]`
  - `opsx changes resolve 2026-06-29-native-core-build-boundary` -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-native-core-build-boundary`
  - `rg -n "Trace|需求追踪|Allowed Files|验收标准|验证命令 / 方法|Evidence Required|Done When|Stabilization Rationale|Manual / Platform Matrix|Final Hardening Plan|Workspace Strategy|Workspace Rationale" openspec/changes/2026-06-29-native-core-build-boundary`
  - `git status --short --branch` -> only current change files are untracked
- checks:
  - `work.md` has intent, scope, non-goals, dependencies, success criteria, verification plan, rollback/escalation and readiness notes.
  - `specs/change/spec.md` defines globally unique R1-R5 and observable scenarios.
  - `design.md` maps R1-R5 to U1-U4, and every U has module boundaries, validation method and risk context.
  - `tasks.md` covers R1-R5 through five dispatchable tasks, includes Default Constraints, and every task has Allowed Files, acceptance criteria, verification method, Evidence Required and Done When.
  - `[stabilization-first]` tasks include rationale, platform matrix and final hardening plans; Windows build/load checks are blocking, macOS is explicitly reserved for `macos-readiness`.
  - Workspace Rationale contains dirty classification, chained-change decision, isolated trigger decision and `current-worktree` conclusion.
- findings:
  - none blocking
- next:
  - proceed to `opsx-implement 2026-06-29-native-core-build-boundary`

## Test Cycle Evidence

## test-cycle | 2026-06-29T18:56:44.3768539+08:00 | pass
任务：Task 1.1 - 新增 native core build entry
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：CMake 当前不可用；macOS toolchain 不在本机；export macro portability 需要先用 Windows build 验证。
- 为什么不能先写目标行为测试：构建入口和工具链可用性属于平台/环境行为，失败可能来自缺工具而非产品行为；必须先稳定 build route。
- 退出条件：Windows native build 通过，CMake availability 被记录，source set parity 明确。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 新增 CMake target 并平台化 export 宏，可以保留 Windows ABI，同时给 macOS readiness 留入口 | `src/DesktopLoudnessMeter.Native/CMakeLists.txt` / `CMakePresets.json` 新增；`DlmPcmAnalyzer.h` 增加 `DLM_NATIVE_EXPORT` / `DLM_NATIVE_CALL`；`DesktopLoudnessMeterNative.cpp` 使用宏替代 Windows-only export spelling | source-set `rg` pass；`cmake` unavailable；`build-native.ps1 -Configuration Release` pass | keep，Windows DLL 输出和 export spelling 编译通过；CMake 不可用按计划记录 | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| Windows native Release | `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` | pass | yes | MSBuild exit=0，Build succeeded，0 warnings，0 errors |
| CMake configure/build | `cmake --version` and `C:\Program Files\CMake\bin\cmake.exe` check | waived | no | `cmake` not recognized；known absolute path check returned `False` |
| macOS native build | 后续 `macos-readiness` | waived | no | 本 change 不宣称 macOS build pass |

### Final Hardening
- 测试 / 验证：
  - `rg -n "DlmPcmAnalyzer\\.(Core|Loudness|RecentWindow|TruePeak)\\.cpp|DesktopLoudnessMeterNative\\.cpp|RfRapid" src/DesktopLoudnessMeter.Native/CMakeLists.txt src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` exit=0，结果：CMake 与 `.vcxproj` 均列出 analyzer/RfRapid source set。
  - `cmake --version` exit=1，结果：当前 shell 不存在 `cmake` 命令。
  - `Test-Path -LiteralPath 'C:\Program Files\CMake\bin\cmake.exe'` exit=0，结果：`False`。
  - `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` exit=0，结果：`DesktopLoudnessMeter.Native.dll` 构建成功。
- 豁免：CMake/macOS configure/build 本轮无法自动化；补救：后续 `macos-readiness` 在 macOS/toolchain 环境重跑 native build entry。
- 覆盖率：N/A（本 task 是 build boundary/config stabilization，没有启用代码覆盖率；补救：Task 2.1/3.2 使用 native smoke/full test 覆盖 ABI/load 行为）。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`, `evidence.md`
- 未闭合项：无

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| native build entry lists the same analyzer/RfRapid source files as the Windows project | source-set `rg` exit=0 | pass |
| native build entry sets C++17 for the native core target | `CMakeLists.txt` uses `target_compile_features(... cxx_std_17)` | pass |
| target/output naming keeps Windows `DesktopLoudnessMeter.Native.dll` compatibility | `CMakeLists.txt` sets `OUTPUT_NAME "DesktopLoudnessMeter.Native"` and MSBuild Release produced `DesktopLoudnessMeter.Native.dll` | pass |
| macOS configure/build is not manually verified in this Windows session | Manual/platform matrix waived with `macos-readiness` follow-up | pass |

## Stabilization Ledger

## test-cycle | 2026-06-29T18:58:53.9309595+08:00 | pass
任务：Task 1.2 - 稳定 Windows native deployment rule
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：native DLL load path depends on final process output directory and current configuration.
- 为什么不能先写目标行为测试：copy/load failures appear only after consuming project build/run chooses an output path; static tests alone cannot prove runtime deployment.
- 退出条件：app/tests/benchmark outputs can obtain the native DLL or a bounded blocker is recorded.

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 现有 `.targets` 和三个 consumer import 已满足 deployment rule，不需要改代码 | 无产品文件变更；执行 import scan 和 app/test/benchmark build/load 验证 | import `rg` pass；`dotnet test --filter NativePcmAnalyzer` pass；benchmark run pass；初次 app build 并行 WPF temp project 出现 `CS5001`，随后 `dotnet build ... -m:1` pass | keep，fresh app build pass 且 copy message 出现；初次失败未复现，记录为 stabilization 观察 | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| App native copy | `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj -m:1` | pass | yes | exit=0，copy message copied Release DLL to app Debug output |
| Tests native copy/load | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` | pass | yes | exit=0，10 passed，copy message copied Release DLL to test Debug output |
| Benchmark native load | `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` | pass | no | exit=0，Native 2x/4x/8x benchmark paths loaded DLL and matched managed peak |

### Final Hardening
- 测试 / 验证：
  - `rg -n "DesktopLoudnessMeter.Native.targets" src tests` exit=0，结果：app/tests/benchmarks 均显式导入 native copy target。
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj -m:1` exit=0，结果：app build succeeded，native DLL copied to app output。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` exit=0，结果：10 passed，native DLL copied to test output。
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=0，结果：native benchmark loads and peak delta is 0 for 2x/4x/8x。
  - `Test-Path` checks exit=0，结果：app Debug output、test Debug output、benchmark Release output 均存在 `DesktopLoudnessMeter.Native.dll`。
- 豁免：无。
- 覆盖率：N/A（本 task 验证 deployment/copy/load 行为，未采集覆盖率；补救：Task 3.2 full `dotnet test` 覆盖 managed regression）。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`, `evidence.md`
- 未闭合项：无

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| Release build output is copied from the current configuration when present | native build produced Release DLL; app/test/benchmark outputs contain DLL | pass |
| Debug managed builds can still fall back to Release native DLL when Debug native DLL is absent | Debug app/test builds copied `x64\Release\DesktopLoudnessMeter.Native.dll` | pass |
| app, tests, and benchmark projects retain explicit native copy/deployment rules | import `rg` found all three project imports | pass |

## Implement Amendments

## test-cycle | 2026-06-29T19:00:16.0978925+08:00 | pass
任务：Task 2.1 - 守护现有 native exports
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - 无。本 task 的目标行为是保持现有 ABI/export 不变，不引入新的业务断言；先用旧行为扫描和 native smoke 固化当前行为。
- 假红修正：无。
- characterization 旧行为：
  - `rg -n "DllImport|DlmPcmAnalyzer.*Export" ...` exit=0，确认 `NativePcmAnalyzer.cs` 的 P/Invoke entrypoints 与 `DesktopLoudnessMeterNative.cpp` exports 同名。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` exit=0，10 passed，证明旧 wrapper smoke 行为仍可加载/调用。

### 绿阶段
- 通过测试列表：
  - `NativePcmAnalyzer` filtered test set: 10 passed.
- 变更摘要：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` - 修改 - 新增 platform export/calling convention macro。
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - 修改 - export declarations 使用 macro，函数名和参数不变。
- 测试变更记录：无测试文件修改。
- characterization 目标行为：
  - 旧断言 -> 新断言：`DlmPcmAnalyzer*Export` entrypoints 必须存在 -> 同一 entrypoints 在新 DLL 中继续存在。
  - 行为变更理由：无行为变更；仅平台化 export spelling。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 / 证据 | 通过状态 |
|--------------|--------------------|---------|
| `NativePcmAnalyzer.cs` P/Invoke signatures do not need semantic changes | `rg` entrypoint scan showed existing `DllImport` signatures unchanged | pass |
| Windows DLL exports all current `DlmPcmAnalyzer*Export` entrypoints used by the wrapper | absolute-path `dumpbin /exports` listed all current analyzer exports | pass |
| snapshot layout and copy API behavior remain covered by existing native smoke tests | `dotnet test --filter NativePcmAnalyzer` 10 passed | pass |

### 重构阶段
- 通过测试列表：
  - `NativePcmAnalyzer` filtered test set: 10 passed.
- 变更摘要：
  - 无额外重构。
- 覆盖率：N/A（本轮未启用 coverlet 覆盖率收集；补救：Task 3.2 将运行 full managed regression）。

## Verify

## verify | 2026-06-29T19:05:46+08:00 | pass_with_warnings
方向：tasks.md + specs/**/*.md + design.md + code/docs diff -> 验证通过但保留 CMake/macOS 环境 warning
证据：
- `opsx changes resolve 2026-06-29-native-core-build-boundary` exit=0 -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-native-core-build-boundary`
- Git: branch=`codex/2026-06-29-native-core-build-boundary`; HEAD=`59d9b6f4bf176cf275e662189f7d629a22b17398`; base/merge target=`codex/2026-06-29-webview-windows-app-shell@59d9b6f4bf176cf275e662189f7d629a22b17398`; diff_status=unstaged tracked files belong to this change (`README.md`, `docs/current-status.md`, `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`, `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`), untracked files belong to this change (`openspec/changes/2026-06-29-native-core-build-boundary/**`, `src/DesktopLoudnessMeter.Native/CMakeLists.txt`, `src/DesktopLoudnessMeter.Native/CMakePresets.json`); verification_coverage=worktree diff plus generated native DLL output; manual_status=waived with reason for macOS configure/build; stabilization_status=closed with non-blocking CMake/macOS reserved warning; amendment_status=none
- `rg -n "DlmPcmAnalyzer\\.(Core|Loudness|RecentWindow|TruePeak)\\.cpp|DesktopLoudnessMeterNative\\.cpp|RfRapid" src/DesktopLoudnessMeter.Native/CMakeLists.txt src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` exit=0 -> CMake entry and `.vcxproj` source set are traceable.
- `rg -n "DesktopLoudnessMeter.Native.targets" src tests` exit=0 -> app/tests/benchmarks keep explicit native copy target imports.
- `rg -n "DllImport|DlmPcmAnalyzer.*Export" src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` exit=0 -> wrapper entrypoints and native exports still match by name.
- `rg -n "build-native|CMake|macOS|Flutter|DesktopLoudnessMeter.Native" README.md docs/current-status.md docs` exit=0 -> docs include Windows verified native path and CMake/macOS reserved boundary; no future Flutter route was introduced.
- `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` exit=0 -> Build succeeded, 0 warnings, 0 errors.
- `dumpbin /exports src\DesktopLoudnessMeter.Native\x64\Release\DesktopLoudnessMeter.Native.dll` via VS absolute path exit=0 -> all current `DlmPcmAnalyzer*Export` and `DlmRfRapid*` exports listed.
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` exit=0 -> 10 passed.
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0 -> 306 passed, 5 skipped; skips are existing UDP/socket environment-sensitive tests.
- `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=0 -> native benchmark path loaded DLL and matched managed peak at 2x/4x/8x.
- `cmake --version` exit=1 and `Test-Path 'C:\Program Files\CMake\bin\cmake.exe'` -> `False`.
allowed_failures:
- item: `cmake --version` fails because CMake is not installed or not in PATH in this Windows session.
  why_non_blocking: 本 change 已验证 Windows MSBuild native DLL、managed copy/load、ABI exports、native smoke、full managed tests 和 benchmark native load；CMake/macOS 在 work/spec/tasks 中明确为 reserved boundary，不作为本轮 Windows pass 宣称。
  owner_or_followup: `macos-readiness`
  expiry_or_recheck: 后续开始 `macos-readiness` 或本机安装 CMake 后必须重新执行 native CMake configure/build。
问题：
- 无 blocking finding。
需修正：
- 无。下一步进入 `opsx-review`。

## test-cycle | 2026-06-29T19:01:46.9725917+08:00 | pass
任务：Task 3.1 - 更新 build boundary 文档
执行方式：direct

### 直接验证
- 跳过测试循环理由：纯文档更新；无产品逻辑或可执行行为变更。
- 验证命令 / 方法：
  - `rg -n "build-native|CMake|macOS|Flutter|DesktopLoudnessMeter.Native" README.md docs/current-status.md docs` exit=0，结果：README/current-status 包含 Windows verified native build、CMake reserved boundary 和 macOS follow-up；未新增 future Flutter route。
- 变更摘要：
  - `README.md` - 修改 - 说明 native DLL 是 Windows-verified DSP boundary，CMake 是 future C++ Core reserved path。
  - `docs/current-status.md` - 修改 - 更新 native DSP 状态和重要文件描述，明确 macOS 验证留给 `macos-readiness`。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| docs show Windows verified build command | `README.md` / `docs/current-status.md` grep includes `build-native` and Windows verified wording | pass |
| docs identify CMake/macOS path as reserved or follow-up verification | grep includes CMake reserved boundary and `macos-readiness` | pass |
| docs do not describe Flutter as the future main UI runtime | doc grep did not introduce future Flutter route | pass |

## Risk Review

## review | 2026-06-29T19:07:28+08:00 | pass
scope:
- branch: `codex/2026-06-29-native-core-build-boundary`
- HEAD: `59d9b6f4bf176cf275e662189f7d629a22b17398`
- merge_base: `codex/2026-06-29-webview-windows-app-shell` -> `59d9b6f4bf176cf275e662189f7d629a22b17398`
- diff_status: tracked unstaged files are in scope (`README.md`, `docs/current-status.md`, `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`, `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`); untracked in-scope files are current OpenSpec change files and native CMake files.
- reviewed_diff:
  - tracked diff for native export macro and docs.
  - untracked `src/DesktopLoudnessMeter.Native/CMakeLists.txt`.
  - untracked `src/DesktopLoudnessMeter.Native/CMakePresets.json`.
  - current change OpenSpec files as workflow artifacts.
context:
- codemap: `.aiknowledge/codemap/native-dsp.md`
- pitfall: `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`
- verify gate: `pass_with_warnings` with structured CMake/macOS reserved-boundary allowed failure.
findings:
- 无 CRITICAL。
- 无 WARNING。
- 无 SUGGESTION。
review_notes:
- ABI risk thread: `DlmPcmAnalyzer.h` macro addition preserves Windows `__cdecl`/`__declspec(dllexport)` under `EXPORT`; `DesktopLoudnessMeterNative.cpp` function names and parameters remain unchanged; verify dumpbin/native smoke evidence covers runtime compatibility.
- Deployment pitfall thread: app/tests/benchmarks still import `DesktopLoudnessMeter.Native.targets`; no copy target regression was introduced.
- CMake boundary thread: source set matches `.vcxproj`; CMake unavailability is already scoped as a non-blocking verify warning and not a release blocker for Windows verified path.
- `CMakePresets.json` parsed successfully with `ConvertFrom-Json`.
- TODO/FIXME scan found no new implementation TODO/FIXME; matches only planned OpenSpec escalation text.
allowed_failures:
- item: 无新增 review allowed failure；沿用 verify 的 CMake/macOS reserved-boundary warning。
  why_non_blocking: review 未发现额外发布风险；CMake/macOS 未验证已在 verify 中结构化归属 `macos-readiness`。
  owner_or_followup: `macos-readiness`
  expiry_or_recheck: 后续开始 `macos-readiness` 或本机安装 CMake 后复查。
next:
- proceed to `opsx-archive 2026-06-29-native-core-build-boundary`

## test-cycle | 2026-06-29T19:03:09.7867790+08:00 | pass
任务：Task 3.2 - 完成 native build boundary hardening
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：native build, deployment, export inspection and managed load must converge together.
- 为什么不能先写目标行为测试：final hardening spans toolchain commands and runtime load paths, where failures can be environmental or stale-binary related.
- 退出条件：fresh build/test evidence covers the final diff.

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 已完成的 CMake boundary、export macro、docs 和 deployment evidence 可通过最终 hardening | 无额外产品代码变更；重跑 native build、native smoke、full managed tests、scope diff | native build pass；NativePcmAnalyzer pass；full tests pass；scope status clean for allowed files only | keep，最终实现证据 fresh | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| Windows native build | `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` | pass | yes | exit=0，Build succeeded |
| Managed native smoke | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` | pass | yes | exit=0，10 passed |
| Full managed regression | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` | pass | yes | exit=0，306 passed，5 skipped |
| macOS build | follow-up `macos-readiness` | waived | no | CMake/macOS 未在当前 Windows session 声称通过 |

### Final Hardening
- 测试 / 验证：
  - `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` exit=0，结果：MSBuild succeeded，0 warnings，0 errors。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer` exit=0，结果：10 passed。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0，结果：306 passed，5 skipped；skip 均为既有 UDP/socket 环境相关 tests。
  - `git status --short --branch` exit=0，结果：dirty files limited to `README.md`, `docs/current-status.md`, `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`, `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`, current change OpenSpec files, and new native CMake files.
  - `git diff --stat -- openspec/changes/2026-06-29-native-core-build-boundary src/DesktopLoudnessMeter.Native tests/DesktopLoudnessMeter.Tests tests/DesktopLoudnessMeter.Benchmarks README.md docs` exit=0，结果：tracked diff is within planned native/docs/OpenSpec scope；untracked CMake/OpenSpec files are listed by `git status`.
- 豁免：CMake configure/build 和 macOS build 未执行；当前环境 `cmake` 不可用，macOS 验证属于 `macos-readiness`。
- 覆盖率：N/A（当前验证命令未启用 coverlet 覆盖率收集；补救：native smoke/full regression 已覆盖 build/load ABI 主路径）。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`, `evidence.md`
- 未闭合项：无

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| Windows native Release build passes | `build-native.ps1 -Configuration Release` exit=0 | pass |
| `NativePcmAnalyzer` tests pass | `dotnet test --filter NativePcmAnalyzer` exit=0, 10 passed | pass |
| full managed test suite passes or any environment skip is documented | full `dotnet test` exit=0, 306 passed, 5 skipped UDP/socket tests | pass |
| CMake/macOS limitations are recorded as non-blocking boundary evidence | Task 1.1 CMake availability and this hardening matrix | pass |

## Manual Checks

- [~] [manual] macOS configure/build 本轮不验 | 原因：当前为 Windows session，本 change 只提供 reserved build boundary，实际 macOS 验证属于 `macos-readiness`

## Rejected Attempts

### Attempt 1

- hypothesis: 直接把 source ingest 状态机放进本 change，可以更快接近 C++ Core 目标。
- rejection_reason: `native-source-ingest-core` 涉及 source identity、same-handle reconfiguration、thread ownership 和 snapshot projection，属于独立状态机迁移，不应与 build boundary 混在同一 readiness/verify 中。

### Attempt 2

- hypothesis: 直接用 CMake 替换 `.vcxproj` 作为唯一 Windows native build path。
- rejection_reason: 当前 Windows app/tests/benchmarks 已验证依赖 MSVC `x64/<Configuration>` 输出和 `.targets` copy path；CMake-only 会破坏已知主路径并扩大回归风险。
