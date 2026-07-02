# Work Packet: 2026-06-29-native-core-build-boundary

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付单元是把 `DesktopLoudnessMeter.Native` 从 Windows/MSVC DLL 项目收敛为可被 Windows 当前主路径消费、可被后续 macOS runner 验证的 C++ Core build boundary。CMake/构建入口、Windows DLL 输出、managed copy target、ABI 守护和文档证据共享同一组文件和验证命令，拆开会导致每个 slice 都无法独立证明 native core 可被 host 加载。
- OpenSpec 下限检查：通过。该 change 触及 native 构建配置、公共 ABI、managed runtime deployment 和跨平台边界，属于可评审、可验证、可归档的工程闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Native core build boundary | 后续 Windows/WebView host 与 macOS runner 可复用同一 C++ Core source set | `src/DesktopLoudnessMeter.Native/**` | 高 | 当前 focus |
| Windows parity | 保证现有 WPF/WebView fallback 和 tests 继续加载 `DesktopLoudnessMeter.Native.dll` | `.vcxproj`, `.targets`, managed consumers | 高 | 与 build boundary 同一闭环 |
| ABI guard | 防止 C# `NativePcmAnalyzer` 调用面被构建调整破坏 | `DlmPcmAnalyzer.h`, `DesktopLoudnessMeterNative.cpp`, `NativePcmAnalyzer.cs` | 中 | 不独立交付 |
| macOS reserved path | 给 `macos-readiness` 留出可验证入口 | CMake/docs/evidence | 中 | 不宣称 macOS 已通过 |

### 推荐切分 / Top-Level Changes

1. `native-core-build-boundary`
   - 范围：新增或调整 native core build boundary，保留 Windows DLL 输出，预留 macOS CMake/ABI 验证入口。
   - 不包含：source ingest 状态机、protocol ingest、WebView UI、new DSP algorithm、macOS runner。
   - 依赖：`2026-06-23-native-core-dsp` 已归档；`2026-06-29-ui-runtime-architecture-replan` 已归档；`2026-06-29-webview-windows-app-shell` 已归档。
   - 建议 change 名：`2026-06-29-native-core-build-boundary`
   - work 关注点：C ABI 稳定、MSVC/CMake 或等效构建策略、Windows parity、macOS 可验证边界。

### 执行顺序

- 当前 change 完成后，后续推荐进入：`settings-workspace-schema` 或按风险优先继续 `native-source-ingest-core`。
- 本 change 不创建 sibling changes；source ingest 和 protocol boundary 仍按 replan change map 后续推进。

## 意图

当前 native DSP 主路径已经由 `2026-06-23-native-core-dsp` 归档完成，但 `DesktopLoudnessMeter.Native` 仍以 Windows x64 MSVC `.vcxproj` 和 `x64/<Configuration>/DesktopLoudnessMeter.Native.dll` 作为实际构建/部署边界。用户最终目标要求 C++ Core 承担 DSP、协议接入、source ingest 状态机和 snapshot generation；在迁移更多 core 逻辑之前，需要先把 native 项目整理为明确的跨平台 C++ Core build boundary。

本 change 先完成构建边界，不扩大 core 职责。Windows 必须继续可构建、可复制、可由 C# P/Invoke 加载；macOS 只预留 CMake/ABI runner 入口和后续验证方式，不在 Windows 机器上声称已通过。

## 范围

### 范围内

- 为 `DesktopLoudnessMeter.Native` 定义可维护的 native core source set 和 build entry，优先使用 CMake 或等效跨平台 build boundary。
- 保留现有 Windows MSVC x64 DLL 输出，确保 `DesktopLoudnessMeter.Native.dll` 仍可被 app/tests/benchmarks 复制和加载。
- 更新 native copy target 或 build helper，使 managed consumers 不依赖间接 `ProjectReference` 复制 native DLL。
- 增加 ABI/export 守护，证明 `NativePcmAnalyzer.cs` 当前使用的 `DlmPcmAnalyzer*Export` entrypoints 未被破坏。
- 记录 macOS reserved boundary：可从相同 source set 配置 native dynamic library，但实际 macOS runner/capture/package 验证留给 `macos-readiness`。

### 范围外

- 不迁移 `LocalLoopbackSignalSourceService`、WASAPI capture、source registry 或 snapshot projection 到 C++。
- 不改变 `PcmMetricAnalyzer` public API、`PcmMetricsSnapshot` 字段语义或 HostBridge DTO。
- 不新增 DSP 算法，不调整 loudness、true peak、waveform、spectrum 行为。
- 不实现 macOS WKWebView runner、capture adapter、packaging、notarization 或 installer。
- 不退休 WPF，不改变 WebView shell 启动/托盘/packaging 主路径。

## 依赖

- `openspec/changes/archive/2026-06-23-native-core-dsp/`：native PCM analyzer、C ABI、C# wrapper 和 tests 的事实基线。
- `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/`：WebView UI + C++ Core change map。
- `openspec/changes/archive/2026-06-29-webview-windows-app-shell/`：当前 Windows 主路径仍可通过 WPF fallback 打开 WebView shell。
- `.aiknowledge/codemap/native-dsp.md`、`.aiknowledge/codemap/audio-analysis.md`、`.aiknowledge/codemap/chains/pcm-metric-flow.md`。
- `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`。

## 成功标准

- Native core build boundary 能列出并构建当前 `DlmPcmAnalyzer.*`、ABI export layer 和 `RfRapid` source set。
- Windows Release x64 native DLL 输出保持 `DesktopLoudnessMeter.Native.dll`，且 managed app/tests/benchmarks 仍能复制到最终进程输出目录。
- `NativePcmAnalyzer` 相关 tests 通过，证明 current C ABI、snapshot layout 和 copy functions 仍可用。
- 构建/文档清楚区分“Windows 已验证”和“macOS reserved boundary 未验证”。
- `dotnet build` / `dotnet test` / native build smoke 的证据写入 `evidence.md`。

## 需求 / 验收摘要

- [R1] Native core 必须有跨平台 build entry，且 source set 与现有 MSVC 项目一致。
- [R2] Windows native DLL 输出必须保持现有 P/Invoke 名称与 Release/Debug 消费路径。
- [R3] Managed consumers 必须通过显式 native deployment rule 获得 native DLL。
- [R4] 当前 C ABI exports 必须保持与 `NativePcmAnalyzer` 调用面兼容。
- [R5] macOS build boundary 必须被记录为 reserved/待验证，不得伪装成已完成 macOS 支持。

## 方案摘要

采用“新增跨平台 build boundary，保留 Windows `.vcxproj` 输出”的渐进方案。CMake 或等效入口负责表达 native core source set、C++17、导出宏和平台条件；现有 `.vcxproj` 可继续作为 Windows 已验证构建路径。`.targets` 和 helper script 需要明确最终输出目录与 managed consumer copy 规则，避免重现 benchmark `DllNotFoundException`。ABI 守护以现有 P/Invoke entrypoints、native smoke tests 和 export 检查为主。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.Native/CMakeLists.txt` — native core build boundary。
- `src/DesktopLoudnessMeter.Native/CMakePresets.json` — 可选的 configure/build presets。
- `src/DesktopLoudnessMeter.Native/build-native.ps1` — Windows helper，可扩展 MSBuild/CMake 路径探测。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` — Windows source set parity 或 build metadata。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` — managed native DLL deployment rule。
- `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` — 仅允许 ABI export macro / platform guard，不允许语义改签名。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — 仅允许 export portability glue，不允许算法语义变更。
- `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer*.cpp`、`src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h`、`src/DesktopLoudnessMeter.Native/RfRapid/**` — 仅允许 build portability include/compile fixes。
- `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs` — 仅允许必要的 load-name guard 或 test-visible ABI assertions。
- `tests/DesktopLoudnessMeter.Tests/**` — ABI/build/deployment smoke 或 characterization tests。
- `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` — 仅允许 native deployment rule parity 修正。
- `README.md`、`docs/current-status.md`、`docs/**` — native build boundary 文档。
- `.aiknowledge/codemap/**`、`.aiknowledge/logs/**` — 仅归档后知识维护。
- `openspec/changes/2026-06-29-native-core-build-boundary/**` — 当前 change 产物。

### 禁止修改

- `src/DesktopLoudnessMeter.Web/**`
- `src/DesktopLoudnessMeter.App/WebView/**`
- `src/DesktopLoudnessMeter.App/Host/**`
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.Setup/**`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-native-core-build-boundary`
- Base Branch: `codex/2026-06-29-webview-windows-app-shell` at `59d9b6f4bf176cf275e662189f7d629a22b17398`
- Merge Target: `codex/2026-06-29-webview-windows-app-shell`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-29-native-core-build-boundary/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已归档 WebView shell 分支继续推进总体架构，`base_branch=codex/2026-06-29-webview-windows-app-shell`，`base_head=59d9b6f4bf176cf275e662189f7d629a22b17398`，属于链式 change。
  - isolated 触发：任务会触及构建配置和 native deployment 公共面，但当前工作树干净、单 agent 串行、无需并行依赖隔离；不创建额外 worktree。若 implementation 阶段需要并行验证多个 toolchain 或引入大量生成产物，再回到 plan 裁决 isolated worktree。
  - 结论：`current-worktree`。

## 验证计划

- `opsx changes list`，预期：仅本 change 为活动变更。
- `opsx changes resolve 2026-06-29-native-core-build-boundary`，预期：解析到当前 change root。
- `git status --short --branch`，预期：dirty 仅包含本 change 归属文件。
- `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` 或显式 VS MSBuild 命令，预期：Windows x64 native DLL 构建成功。
- CMake configure/build smoke：若 `cmake` 可用则运行 native preset；若不可用则记录环境 blocker，至少验证 CMake files/source set 静态一致性。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，预期：native ABI smoke tests 通过或因 native DLL 不可用按既有 skip 策略可解释。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：全量测试通过或记录非本 change 环境 skip。
- `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release`，预期：native DLL 可由 benchmark 进程加载；如 benchmark 时间过长则至少执行 load/smoke 子路径并记录原因。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes
- 不确定性来源：本机 `cmake` 不在 PATH，`C:\Program Files\CMake\bin\cmake.exe` 不存在；macOS runner/capture/package 无本机验证环境；native DLL copy/load 依赖最终进程输出目录。
- 人工 / 平台覆盖：Windows MSBuild/native DLL load 是阻断项；CMake/macOS 在本 change 中作为 reserved boundary 和环境证据，不作为 macOS support 宣称。
- 最终硬化策略：以 Windows native build、NativePcmAnalyzer tests、managed full tests、benchmark/load smoke 和 source-set parity 检查收口；CMake 不可用时必须在 evidence 中标记 allowed failure 和后续 `macos-readiness` recheck。

## 回退 / 升级

- 回退方式：回退新增 CMake/build helper/targets/doc changes，保留原 `.vcxproj` + `.targets` Windows DLL 路径；不得修改 analyzer 算法作为回退手段。
- 升级条件：如果 build boundary 需要改变 C ABI 签名、`PcmMetricsSnapshot` 语义、source ingest ownership 或 app startup/package 行为，停止 implementation，回到 plan 拆出新的顶层 change。
- 升级条件：如果 Windows native build 无法在当前环境稳定通过，先记录 blocker 并请求工具链上下文，不用未经验证的大范围重写替代。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“把 native-source-ingest-core 一起做”：source registry、lifecycle、snapshot projection 是独立状态机迁移，需要自己的 readiness 和验证矩阵。
- 不采用“直接替换 `.vcxproj` 为 CMake-only”：当前 Windows 主路径和 native copy target 已验证依赖 MSVC 输出路径，直接替换会扩大回归面。
- 不采用“宣称 macOS 已支持”：当前只有 Windows 环境证据，macOS runner/WKWebView/capture/package 都必须留给 `macos-readiness`。
- 不采用“只写文档不改 build boundary”：用户目标要求 C++ Core 构建边界真实落地，纯文档不能支撑后续 native-source-ingest-core。

## Readiness Notes

- readiness 需检查本 change 是否仍聚焦 build boundary，不包含 source ingest/protocol/WebView UI 实施。
- readiness 需检查 R1-R5 在 spec/design/tasks 中有完整 trace。
- readiness 需检查 `[stabilization-first]` 是否明确区分 Windows 阻断验证和 macOS reserved evidence。
- readiness 需检查 Workspace Rationale 四要素完整。
