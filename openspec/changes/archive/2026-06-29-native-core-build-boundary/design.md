# Design: 2026-06-29-native-core-build-boundary

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

已证实事实：
- `2026-06-23-native-core-dsp` 已归档，C++ analyzer 主路径位于 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*`，C# wrapper 通过 `DllImport("DesktopLoudnessMeter.Native")` 调用 `DlmPcmAnalyzer*Export`。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` 只配置 `Debug|x64` 和 `Release|x64`，输出目录为 `$(ProjectDir)$(Platform)\$(Configuration)\`。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` 当前只复制 `x64/<Configuration>/DesktopLoudnessMeter.Native.dll`，缺失 preferred path 时 fallback 到 `x64/Release`。
- app、tests、benchmarks 已显式导入 native copy target；相关 pitfall 说明不能依赖 `ProjectReference` 间接复制 native DLL。
- 仓库已有 JUCE `CMakeLists.txt`，但 `src/DesktopLoudnessMeter.Native` 尚无 CMake entry。
- 当前环境 `cmake` 不在 PATH，`C:\Program Files\CMake\bin\cmake.exe` 不存在。

推断：
- CMake 是后续 macOS/native-core build readiness 的合理入口，但本 change 只能在 Windows 环境验证文件结构、source set parity 和 Windows build/load parity。
- `DesktopLoudnessMeterNative.cpp` 的 `__declspec(dllexport)` / `__cdecl` 需要平台条件化，才能作为跨平台 source set 的一部分被非 MSVC toolchain 配置。
- `.vcxproj` 可以暂时保留为 Windows 已验证主路径，CMake entry 先作为跨平台 boundary 和后续 macOS 验证入口。

## 目标 / 非目标

**目标：**
- 为 native analyzer core 建立平台中立 build entry。
- 保持 Windows MSVC x64 DLL 构建和 managed deployment parity。
- 守护 `NativePcmAnalyzer` 当前使用的 C ABI exports。
- 明确 macOS build 是 reserved boundary，不宣称 runtime parity。

**非目标：**
- 不改变 DSP 算法、snapshot 字段语义或 analyzer public behavior。
- 不迁移 source ingest、Protocol、JUCE sender 或 HostBridge。
- 不实现 macOS runner、capture adapter、installer 或 signing。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U2]
- [R4] -> [U3]
- [R5] -> [U4]

## 实施单元

### [U1] Native core build entry
- 关联需求: [R1], [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt`
  - `src/DesktopLoudnessMeter.Native/CMakePresets.json`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
- 设计:
  - 新增 native core build entry 表达当前 analyzer source set，不把 source ingest 或 protocol source 加入 core target。
  - target name 保持 `DesktopLoudnessMeter.Native`，Windows dynamic library 输出名不变。
  - C++ standard 使用 C++17，与 `.vcxproj` 保持一致。
  - 如需导出宏，优先在 `DlmPcmAnalyzer.h` 或小型 internal header 定义 `DLM_NATIVE_EXPORT` / calling convention macro，让 Windows exports 继续使用 `__declspec(dllexport)` 和 `__cdecl`，非 Windows 使用默认 visibility 或空宏。
- 验证方式:
  - source set parity 检查。
  - CMake configure/build smoke（若 toolchain 可用）。
  - Windows MSBuild Release x64 build。
- 知识沉淀:
  - native source set 以后从 build entry 维护，减少 `.vcxproj` 和 CMake 漂移。

### [U2] Windows output and deployment parity
- 关联需求: [R2], [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/build-native.ps1`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets`
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
  - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj`
- 设计:
  - Windows helper 继续支持 MSBuild path，因为这是当前已验证主路径。
  - 如果新增 CMake helper mode，必须显式输出到 managed copy target 能发现的位置，或同步更新 `.targets` 的候选路径。
  - `.targets` 应继续用 consuming project 的 `$(OutputPath)` 作为最终 copy destination，防止 native DLL 只进入 referenced app 输出目录。
- 验证方式:
  - native Release x64 build。
  - app/test/benchmark output load/copy smoke。
- 知识沉淀:
  - 更新 native copy target pitfall 或 codemap，记录新候选路径。

### [U3] ABI export compatibility guard
- 关联需求: [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/NativePcmAnalyzerSmokeTests.cs`
- 设计:
  - `DlmPcmAnalyzer.h` 保持 C ABI data layout 和 function declarations，不改变 `DlmPcmAnalyzerSnapshot` 字段顺序。
  - `DesktopLoudnessMeterNative.cpp` 继续作为 export/forwarding layer，不搬入算法逻辑。
  - ABI guard 优先通过 tests 和 export inspection 验证，不在本 change 引入新的 product API。
- 验证方式:
  - `NativePcmAnalyzer` tests。
  - Windows export list 检查。
  - full `dotnet test`。
- 知识沉淀:
  - 若新增 ABI guard 测试，归档后记录到 `native-dsp` codemap。

### [U4] Build boundary docs and evidence
- 关联需求: [R5]
- 模块边界:
  - `README.md`
  - `docs/current-status.md`
  - `docs/**`
  - `openspec/changes/2026-06-29-native-core-build-boundary/evidence.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- 设计:
  - 文档只声明 Windows verified path；macOS 只写 reserved boundary 和后续验证入口。
  - evidence 必须记录 CMake 工具链可用性事实，避免把未运行的 configure/build 写成 pass。
  - 归档后刷新 codemap，把 native-dsp 从 Windows-only MSVC DLL 边界更新为 Windows-verified + cross-platform build boundary。
- 验证方式:
  - 文档 grep 检查无 Flutter/旧路线误引。
  - final verify/review/archive evidence。
- 知识沉淀:
  - 记录 CMake availability、copy target deployment 和 ABI guard 策略。

## 决策

1. **保留 `.vcxproj` 作为 Windows verified path。**
   - 原因：当前自动化和 native DLL copy target 已依赖 MSVC x64 输出，直接切换为 CMake-only 会扩大回归面。
   - 影响：本 change 需要维护 source set parity，避免 `.vcxproj` 和 CMake entry 漂移。

2. **新增 CMake 或等效 build entry 作为跨平台 boundary。**
   - 原因：macOS runner/WKWebView 后续需要非 MSBuild 的 native core 构建入口。
   - 影响：当前 Windows 环境缺 CMake 时，可以记录 configure/build blocker，但必须保证文件结构和 source set 清晰。

3. **ABI 不扩展，只守护现有 exports。**
   - 原因：本 change 目标是 build boundary，不是 native core feature migration。
   - 影响：任何新增 protocol/source ingest ABI 都必须进入后续 `native-source-ingest-core` 或 protocol boundary change。

4. **managed consumers 继续显式导入 copy target。**
   - 原因：已有 pitfall 证明 `ProjectReference` 不会把 native DLL 传递复制到最终进程输出目录。
   - 影响：`.targets` 修改必须用 app/tests/benchmarks 三类 consumer 共同验证。

5. **macOS 是 reserved boundary，不是 pass 项。**
   - 原因：当前没有 macOS runner、capture、package 或 toolchain evidence。
   - 影响：verify 可通过 Windows + boundary evidence，但必须把 macOS runtime 验证留给后续 change。

## 风险 / 权衡

- CMake 不可用会限制 configure/build smoke：用 toolchain availability 证据、source set parity 检查和 Windows MSBuild 作为本轮阻断验证；macOS recheck 留给 `macos-readiness`。
- Export macro portability 可能误改 Windows ABI：通过 `NativePcmAnalyzer` smoke 和 export inspection 兜底。
- `.targets` 候选路径过多可能复制旧 DLL：copy target 应输出清晰 message，并优先当前 configuration 输出。
- CMake 和 `.vcxproj` source list 容易漂移：tasks 要求静态 parity 检查或文档说明维护点。

## 稳定化策略

- `[stabilization-first]` 适用单元：[U1], [U2], [U3], [U4]（[U3]/[U4] 通过 final hardening task 收口）
- doc impact 处理：`TASK_EVIDENCE_ONLY`，若实现中发现必须改变 ABI/export signatures，则升级为 `SPEC_AMENDMENT` 或 `SCOPE_CHANGE`。
- final hardening：Windows native Release build、export inspection、`NativePcmAnalyzer` tests、full managed tests、benchmark/load smoke、CMake availability evidence。

## 回退方案

如果 CMake/build boundary 修改破坏 Windows DLL 构建或 P/Invoke load，回退新增 build entry 和 export macro changes，恢复 `.vcxproj` + `.targets` 原始 Windows path。若问题来自 analyzer 算法文件编译兼容性，不允许用修改 DSP 语义作为回退；应先最小化 platform guard 或暂停并回到 plan。

## 并行 / Subagent 边界

- 默认串行：OpenSpec 产物、build files、targets、ABI guard、evidence。
- 可并行 lane：只读检查 CMake/source set parity 或 docs grep；不得让 sidecar 写 `.openspec.yaml`、`tasks.md`、`evidence.md`。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后更新 `.aiknowledge/codemap/native-dsp.md`、`.aiknowledge/codemap/audio-analysis.md` 或 `pcm-metric-flow`，记录 native build boundary、Windows verified path、CMake reserved path 和 deployment rule。若 `.targets` 规则变化，更新 `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`。
