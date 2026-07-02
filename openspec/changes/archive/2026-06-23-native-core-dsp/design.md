# Design：2026-06-23-native-core-dsp

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

当前 `PcmMetricAnalyzer` 位于 WPF app 项目内，但它实际承载平台无关 DSP：K-weighting、LUFS windows/gate、true peak oversampling、recent sample window、waveform、spectrum、stereo image。`DesktopLoudnessMeter.Native` 已有 C++ RfRapid FFT 和 true-peak C ABI，但还不是完整 PCM analyzer。未来多平台适配需要复用算法内核，因此需要把 `Audio/Analysis` 中可复用 DSP 移入 C++，保留 C# 作为 Windows 宿主和 UI glue。

## 目标 / 非目标

**目标：**
- 将 PCM 指标算法主实现迁入 `DesktopLoudnessMeter.Native` C++ engine。
- 保持 `PcmMetricAnalyzer` public API 和 `PcmMetricsSnapshot` 对上层调用方的语义。
- 用自动化测试约束迁移前后行为等价。
- 明确平台边界：C++ core 只处理 interleaved PCM 和分析状态，不依赖 Windows capture/UI。

**非目标：**
- 不迁移 WASAPI/NAudio capture、process/session/source 生命周期和 WPF UI。
- 不实现 macOS/Linux 宿主。
- 不改变 `PcmMetricsSnapshot` 的字段协议。
- 不重做发布/安装包流程。

## 需求追踪

- [R1] -> [U1], [U2], [U3]
- [R2] -> [U4]
- [R3] -> [U1], [U4]
- [R4] -> [U5]
- [R5] -> [U4], [U6]

## 实施单元

### [U1] C++ analyzer 生命周期和 C ABI
- 关联需求: [R1], [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*`
- 验证方式: native build；C# interop create/process/reset/destroy smoke；invalid option tests。
- 知识沉淀: native ABI handle ownership、错误码和 buffer copy 规则。

### [U2] C++ loudness/true-peak/recent metric 算法
- 关联需求: [R1]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs`
- 验证方式: managed baseline characterization tests；native-backed analyzer tests。
- 知识沉淀: BS.1770 计算、recent window 缓存刷新、oversampled samples 供 waveform/spectrum 使用。

### [U3] C++ FFT/spectrum bucket 和 performance 策略
- 关联需求: [R1]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Loudness.cpp`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.TruePeak.cpp`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.RecentWindow.cpp`
  - 可新增 `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp`
  - `src/DesktopLoudnessMeter.Native/RfRapid/**`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/ManagedSpectrumFft.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/SpectrumScale.cs`
  - 参考 `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs`
- 验证方式: spectrum bucket count/refresh tests；normal/eco FFT size characterization。
- 知识沉淀: mel scale bucket mapping 与 RfRapid FFT 集成。

### [U4] C# thin wrapper 和 snapshot 映射
- 关联需求: [R2], [R3], [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs`
  - 可新增 `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs`
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
- 验证方式: 现有 analyzer contract tests、source service tests、process worker tests 编译和通过。
- 知识沉淀: C# wrapper 只拥有 native handle 和 managed array copy，不持有 native 临时指针。

### [U5] 等价和回归测试
- 关联需求: [R4]
- 模块边界:
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**`
  - `tests/DesktopLoudnessMeter.Benchmarks/Program.cs`
  - 如需测试访问，可调整 `src/DesktopLoudnessMeter.App/AssemblyInfo.cs`
- 验证方式: `dotnet test`；benchmark smoke。
- 知识沉淀: native-backed DSP 迁移的 characterization-first 模式。

### [U6] 平台边界守护
- 关联需求: [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs`
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
  - `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs`
  - `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs`
- 验证方式: no-op 或只因 API 编译需要而最小调整；相关 tests 通过。
- 知识沉淀: capture/input adapter 不进入 DSP core。

## 决策

1. **C++ engine 以 opaque handle + C ABI 暴露。**
   - 原因：现有 C# P/Invoke 已采用此模式；未来非 Windows wrapper 也可复用。
   - 影响：C# 不直接持有 native 内部集合；数组结果复制到 managed buffer。

2. **`PcmMetricsSnapshot` 保持 C# DTO。**
   - 原因：它是 UI/service 边界，当前上层大量消费；跨平台 core 不应依赖 WPF，但也不需要把 DTO 搬到 C++。
   - 影响：C++ 提供 flat snapshot + array copy API，C# 映射为现有 record。

3. **首期保持 mono/stereo 和 oversampling 2/4/8 限制。**
   - 原因：现有 contract 如此，扩大 channel 数属于新能力。
   - 影响：native create 参数沿用现有校验语义。

4. **测试先固化 managed baseline，再切 wrapper。**
   - 原因：迁移算法风险高；先 characterization 可避免无意识改变指标语义。
   - 影响：实现期间可保留 managed classes 作为 internal baseline 或 test helper；最终是否删除由覆盖情况决定。

5. **native 缺失策略首期偏 fail-fast，受控 fallback 可保留。**
   - 原因：目标是 C++ 主实现，静默回退会掩盖迁移失败；但开发环境 native DLL 缺失会影响测试。
   - 影响：如果保留 fallback，必须有测试证明 fallback 是显式、可观察、不会改变上层 API。

## 风险 / 权衡

- ABI buffer ownership 容易出错：用 caller-provided buffer 或 copy API，禁止暴露长期 native pointer。
- 算法浮点差异：设置容差，区分标量指标和 spectrum bucket 形状。
- native build 环境不确定：Task 使用 `[stabilization-first]`，若 MSVC 不可用则记录 blocker。
- AVX2/RfRapid 初始化：现有 vcxproj 开启 AVX2，老 CPU 兼容未纳入本次范围；如运行时失败需记录并决定 fallback。
- change 规模大：保持一个 change，因为 ABI/snapshot/测试强耦合；任务拆小控制风险。

## 稳定化策略

- `[stabilization-first]` 适用单元：[U1], [U4]
- doc impact 处理：默认 `TASK_EVIDENCE_ONLY`；ABI 或范围调整时使用 `DESIGN_AMENDMENT`。
- final hardening：native build/load 稳定后补充 create/process/reset/destroy smoke、DLL missing/fallback 或 fail-fast 行为测试。

## 回退方案

- 若 native engine 实现未完成但 wrapper API 已改，可恢复 `PcmMetricAnalyzer` 到 managed implementation，并保留 C++ engine 未接入代码在 change 内待后续修正。
- 若单项算法偏差无法收敛，可临时把该项回退 managed path，但必须在 `evidence.md` 标记为 incomplete 或调整范围，不得声称全量 C++ 化完成。
- 若 native build 环境阻塞，停止实现并请求用户安装/确认 Build Tools，不做无验证的大范围改动。

## 并行 / Subagent 边界

- 默认串行：OpenSpec 产物、C ABI、C# wrapper。
- 可并行 lane：测试基线整理、native algorithm port、benchmark smoke，但当前由单 agent 串行执行。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后刷新 `.aiknowledge/codemap/audio-analysis.md`、`.aiknowledge/codemap/native-dsp.md` 和 `pcm-metric-flow`，记录 C++ analyzer ABI、wrapper 边界和测试策略。
## Design Amendment | 2026-06-25 | native-structure-split

- classification: design-amendment
- summary:
  - 在保持 `R1`-`R5`、C ABI、`PcmMetricAnalyzer` API 与现有测试语义不变的前提下，将 native analyzer 从单文件聚合实现调整为按职责拆分的内部结构。
  - 拆分目标对齐原 C# `Audio/Analysis` 的职责边界，而不是要求与 C# 文件数 1:1 对应。
- added_module_boundaries:
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h`：内部共享常量、状态结构、私有帮助函数声明。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Loudness.cpp`：K-weighting、sliding window、integrated gate、LUFS / RMS 相关逻辑。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.TruePeak.cpp`：true-peak kernel、重建与 oversampled ring 输入逻辑。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.RecentWindow.cpp`：recent window、waveform、spectrum、stereo image、performance mode 刷新策略。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp`：`DlmPcmAnalyzer` 生命周期、process、snapshot、settings 与 copy API 编排。
- invariants:
  - `DesktopLoudnessMeterNative.cpp` 继续仅作为 ABI export / forwarding 层。
  - `DlmPcmAnalyzer.h` 继续只暴露稳定 C ABI，不暴露内部模块类型。
  - C# wrapper、snapshot DTO、tests 调用方式不因本次结构拆分发生语义变化。
- rationale:
  - 当前 `DlmPcmAnalyzer.cpp` 已承载 loudness、true peak、recent window、waveform、spectrum 与 orchestration，继续迭代会明显提高维护、排障和后续扩展成本。
  - 该调整属于实现期结构化增强，不改变 change 范围，但会影响设计中的模块边界与验证关注点，因此记录为 `design-amendment`。
