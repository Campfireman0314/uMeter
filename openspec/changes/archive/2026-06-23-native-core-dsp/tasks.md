# Tasks：2026-06-23-native-core-dsp

## Default Constraints

**Forbidden Files**（默认）：
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**（默认）：
- `.openspec.yaml` — controller 串行更新
- `tasks.md` — controller 串行更新
- `evidence.md` — controller 串行更新

**Worker Return Format**（默认）：DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED

## 1. Characterization baseline

- [x] 1.1 [characterization-first] 固化 managed analyzer 基线

  **需求追踪**：[R4] → [U5]
  **执行方式**：[characterization-first]
  **Allowed Files**：
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — baseline/contract tests
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/**` — 仅允许为测试访问做 internal 可见性或 helper 调整

  **验收标准**：
  - [x] tests 覆盖 sine、silence、quiet tail、stereo correlation/phase、reset、normal/eco spectrum refresh。
  - [x] tests 记录迁移前 `PcmMetricsSnapshot` 标量和数组形状语义。
  - [x] tests 不依赖 UI/WPF 控件。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，预期：新增/现有 analysis tests 通过。

  **Evidence Required**：
  - red/green 命令、退出码、失败摘要和通过摘要追加到 `evidence.md`。

  **Done When**：
  - managed baseline 可作为 C++ 迁移等价判断依据。

  **依赖**：无

## 2. Native engine core

- [x] 2.1 [stabilization-first] 建立 `DlmPcmAnalyzer` C ABI 和 native build smoke

  **需求追踪**：[R1, R3] → [U1]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — C ABI export
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*` — native analyzer handle/types
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` — C++ project includes
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — native smoke/interop tests

  **验收标准**：
  - [x] C ABI 支持 create/process/reset/destroy，并返回明确错误码。
  - [x] invalid sample rate/channel/oversampling 参数被拒绝。
  - [x] reset 后 snapshot 回到 unavailable 初始状态。
  - [x] native project 包含新增 C++ 文件并能构建。

  **验证命令 / 方法**：
  - `msbuild src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64`，预期：DLL 构建成功。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Native`，预期：native smoke tests 通过。

  **Evidence Required**：
  - native build 命令、dotnet test 命令、退出码、任何环境 blocker。

  **Stabilization Rationale**：
  - 不确定性来源：MSVC Build Tools、native DLL 输出路径、P/Invoke 加载路径。
  - 为什么不能先写完整目标行为测试：C ABI 和 DLL 加载路径未稳定前，完整 native-backed tests 可能因环境而非行为失败。
  - 退出条件：native build/load smoke 稳定通过，且错误码/生命周期可测。

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows x64 native build | `msbuild ... /p:Platform=x64` | yes | `DesktopLoudnessMeter.Native.dll` 生成 |
  | .NET test native load | `dotnet test ... --filter Native` | yes | P/Invoke 可加载 DLL |

  **Final Hardening Plan**：
  - native build/load 稳定后补充 process/reset/destroy regression tests。
  - doc impact：`TASK_EVIDENCE_ONLY`；如 ABI 变化超出 design，升级为 `DESIGN_AMENDMENT`。

  **Done When**：
  - native engine 生命周期和 build/load 基础可用。

  **依赖**：Task 1.1

- [x] 2.2 [test-first] 迁移 loudness、true-peak、RMS 和 stereo image 到 C++

  **需求追踪**：[R1, R4] → [U2, U5]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*` — native algorithms
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — ABI glue
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — equivalence tests

  **验收标准**：
  - [x] native-backed analyzer 在 sine/silence/quiet tail 上 LUFS 与 managed baseline 容差内。
  - [x] true peak oversampling 2/4/8 与 baseline 容差内。
  - [x] RMS、correlation、stereo image 对 mono/stereo 输入符合现有 null/unavailable 语义。
  - [x] 非有限 samples 被 sanitize 后不污染 snapshot。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，预期：analysis tests 通过。

  **Evidence Required**：
  - red/green 测试记录、容差说明、任何偏差解释。

  **Done When**：
  - 标量指标主路径由 C++ engine 产出且测试通过。

  **依赖**：Task 2.1

- [x] 2.3 [test-first] 迁移 waveform、channel data、spectrum buckets 和 performance mode 到 C++

  **需求追踪**：[R1, R4] → [U3, U5]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*` — waveform/spectrum algorithms
  - `src/DesktopLoudnessMeter.Native/RfRapid/**` — 仅允许必要集成调整
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` — ABI glue
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — waveform/spectrum tests

  **验收标准**：
  - [x] waveform 长度、channel waveform shape 和 enable/disable 语义保持兼容。
  - [x] spectrum bucket count 为 320，frequency/mel bucket 行为与 baseline 形状兼容。
  - [x] normal/eco mode 的 spectrum refresh interval 和 FFT size 行为符合现有 tests。
  - [x] native-owned buffer 不暴露给 C# 长期持有。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，预期：analysis tests 通过。

  **Evidence Required**：
  - red/green 测试记录、数组 shape 断言摘要。

  **Done When**：
  - 数组型和频谱指标由 C++ engine 产出且测试通过。

  **依赖**：Task 2.2

## 3. C# integration

- [x] 3.1 [stabilization-first] 将 `PcmMetricAnalyzer` 切换为 native wrapper

  **需求追踪**：[R2, R3, R5] → [U4, U6]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` — wrapper integration
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs` — P/Invoke wrapper 如新增
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs` — 仅允许兼容性必要调整
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` — native DLL copy 如需调整
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — wrapper contract tests
  - `tests/DesktopLoudnessMeter.Tests/Audio/**` — source/service regression tests

  **验收标准**：
  - [x] `PcmMetricAnalyzer` public API 保持现有调用方式。
  - [x] `Snapshot` 返回 managed-owned arrays/lists。
  - [x] settings 方法仍按现有语义影响 oversampling、channel waveform/spectrum、performance mode。
  - [x] Windows capture/source service tests 无需行为改写即可通过。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：全部通过。

  **Evidence Required**：
  - wrapper 集成测试记录、DLL copy/load 观察、失败与修正摘要。

  **Stabilization Rationale**：
  - 不确定性来源：native DLL 加载路径、已有 tests 是否在运行目录包含 DLL、fallback/fail-fast 策略。
  - 为什么不能先写完整目标行为测试：加载路径和 wrapper ownership 稳定前，失败可能来自部署路径而非 analyzer 行为。
  - 退出条件：全量 tests 可通过，且 wrapper ownership/fallback 行为有明确测试。

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows x64 test runtime | `dotnet test` | yes | native DLL 被加载 |
  | UI smoke | 用户手动启动 app | no | 指标刷新，无明显崩溃 |

  **Final Hardening Plan**：
  - 稳定后补 wrapper contract tests：process/reset/settings/snapshot arrays。
  - doc impact：`TASK_EVIDENCE_ONLY`；如 fallback 策略改变，升级为 `DESIGN_AMENDMENT`。

  **Done When**：
  - C# app/test 主路径使用 native C++ DSP engine。

  **依赖**：Task 2.3

## 4. Verification hardening

- [x] 3.2 [test-first] 按 C# 职责边界拆分 native analyzer 结构

  **需求追踪**: [R1, R3] -> [U1, U2, U3]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**`
  - `openspec/changes/2026-06-23-native-core-dsp/design.md`
  - `openspec/changes/2026-06-23-native-core-dsp/specs/change/spec.md`

  **验收标准**:
  - [x] native loudness / gate、true peak、recent-window / waveform / spectrum、analyzer orchestration 至少拆成独立实现单元
  - [x] `DesktopLoudnessMeterNative.cpp` 继续仅保留 ABI export / forwarding，不回灌算法细节
  - [x] 拆分后 `Audio.Analysis` 与 native smoke tests 继续通过，行为与拆分前保持一致
  - [x] 原有 C ABI、`NativePcmAnalyzer.cs` 和 `PcmMetricAnalyzer.cs` 不需要新增调用语义
  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，预期：native smoke tests 通过
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，预期：analysis tests 通过
  - `C:\VS2022\Community\MSBuild\Current\Bin\MSBuild.exe src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64`，预期：native DLL 构建成功
  **Evidence Required**:
  - 拆分前后测试记录、构建命令、失败与修正摘要、以及 design/spec amendment 记录追加到 `evidence.md`
  **Done When**:
  - native C++ 主路径不仅功能迁移完成，内部结构也与 C# 责任边界大体对齐，后续维护不再依赖单个超大 `.cpp`
  **依赖**: Task 3.1

- [x] 4.1 [characterization-first] 更新 benchmark 和端到端回归证据

  **需求追踪**：[R4, R5] → [U5, U6]
  **执行方式**：[characterization-first]
  **Allowed Files**：
  - `tests/DesktopLoudnessMeter.Benchmarks/Program.cs` — benchmark 适配
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — regression tests
  - `tests/DesktopLoudnessMeter.Tests/Audio/**` — capture/source integration tests 如需更新断言

  **验收标准**：
  - [x] benchmark 可运行 native-backed analyzer 并输出 true peak/throughput 信息。
  - [x] `dotnet test` 全量通过。
  - [x] 平台边界 tests 或代码检查证明 Windows capture/source lifecycle 仍在 C#。
  - [→] [manual] 用户最终启动 app，选择或播放音频源，观察 loudness/true peak/waveform/spectrum 正常刷新。目标：后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：全部通过。
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release`，预期：benchmark 完成。

  **Evidence Required**：
  - test/benchmark 命令、退出码、关键输出；manual check 保留为待用户执行。

  **Done When**：
  - 自动化验证完成，只剩用户 UI smoke manual check。

  **依赖**：Task 3.1
