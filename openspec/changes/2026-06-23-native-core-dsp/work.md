# Work Packet：2026-06-23-native-core-dsp

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: planned
- created: 2026-06-23

## 切分结论

### 总判断
- 结论：KEEP_ONE_CHANGE
- 理由：本次目标是一个工程闭环：把可复用 PCM/DSP 分析内核迁入 C++，让现有 Windows C# 宿主继续通过 wrapper 使用同一指标语义。loudness、true-peak、recent metrics、waveform、spectrum 虽可拆成任务，但共享同一个 analyzer 生命周期、snapshot ABI、测试基线和回退策略，拆成多个顶层 change 会造成重复 ABI 改动和中间状态不可演示。
- OpenSpec 下限检查：通过。它涉及公共 native ABI、跨语言边界、算法等价性、构建输出和多模块验证。

### 能力簇
| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| C++ DSP engine | 后续多平台宿主复用同一算法内核 | `DesktopLoudnessMeter.Native`, `Audio/Analysis` | 中 | 必须与 wrapper 和测试一起交付才可用 |
| C# wrapper 保持现有 UI 语义 | Windows UI 不感知迁移 | `PcmMetricAnalyzer`, `PcmMetricsSnapshot` | 低 | 依赖 native engine ABI |
| 等价测试和验证 | 防止响度/峰值/频谱漂移 | tests/benchmarks/native | 中 | 是迁移安全网 |

### 推荐切分 / Top-Level Changes
1. `2026-06-23-native-core-dsp`
   - 范围：C++ 化可复用 DSP 核心并接入现有 C# 分析入口。
   - 不包含：非 Windows UI、多平台 UI、非 Windows audio capture、安装包发布流水线。
   - 依赖：现有 `Audio/Analysis` 行为和 `DesktopLoudnessMeter.Native` native DLL。
   - work 关注点：算法等价、ABI 稳定、C# 宿主最小改动、可验证回退。

### 执行顺序
- changes：`2026-06-23-native-core-dsp`
- 现在进入：当前 change 的 readiness，然后 implement。

## 意图

当前仓库的 Windows UI、WASAPI/NAudio 采集和线程/源管理可以继续保留 C#。未来要做多平台适配，响度、真峰值、声像、波形、频谱等可复用 DSP 算法不应继续绑定在 WPF app 的 C# 层。本次把平台无关 PCM 分析核心迁移到 C++ native engine，C# 只保留宿主 glue、DTO 映射和 Windows capture 输入适配。

## 范围

### 范围内

- 新增/扩展 `DesktopLoudnessMeter.Native` 中的 C++ PCM analyzer engine。
- C++ engine 覆盖：K-weighting、momentary/short-term/integrated LUFS、true peak、RMS、correlation、stereo image、waveform、channel waveform、spectrum、channel spectrum、normal/eco performance 策略。
- C ABI 暴露 analyzer create/process/reset/destroy、settings 更新和 snapshot/buffer 读取能力。
- `PcmMetricAnalyzer` 改为 thin wrapper，保持现有 public API 和 `PcmMetricsSnapshot` 字段语义。
- 保留现有 C# managed analyzer 代码作为测试/回退参照，或仅在测试中使用等价基线；实现阶段若删除需证明测试覆盖不下降。
- 更新 C++ project 和测试，确保 native DLL 可被 app/tests 使用。

### 范围外

- 不迁移 WPF UI、Controls、ViewModels。
- 不迁移 `WasapiSessionMeterService`、`ProcessLoopbackCaptureWorker`、`LocalLoopbackSignalSourceService` 的源生命周期和 Windows capture。
- 不实现 macOS/Linux capture 或 UI。
- 不改变 `PcmMetricsSnapshot` 对上层服务/UI 的字段语义。
- 不引入新的 package publish、installer 或远端 CI 发布流程。

## 成功标准

- `PcmMetricAnalyzer` 现有调用方无需改 API 即可获得由 C++ engine 生成的指标。
- 自动化测试覆盖 loudness、true peak、RMS、stereo image、waveform、spectrum、reset、settings 更新和 native 缺失/初始化失败策略。
- C++ engine 与迁移前 C# 行为在测试信号上满足明确容差。
- app project 仍复制 `DesktopLoudnessMeter.Native.dll`，测试项目能运行 native-backed analyzer 测试。
- 构建和测试命令在本机通过，除手动 UI 听感/显示检查外不需要用户介入。

## 需求 / 验收摘要

- [R1] C++ engine 必须成为 PCM 指标的主实现，并覆盖现有可复用算法范围。
- [R2] C# wrapper 必须保持现有 analyzer API 和 snapshot 语义。
- [R3] native ABI 必须支持增量处理、reset、settings 更新和稳定 buffer 生命周期。
- [R4] 迁移必须提供自动化等价/回归测试。
- [R5] Windows 平台 glue 必须保留在 C#，不被误迁到 C++ core。

## 方案摘要

在 `DesktopLoudnessMeter.Native` 中新增 `DlmPcmAnalyzer` handle。C++ 内部复用/整合现有 RfRapid FFT 和 true-peak 实现，补齐 C# `Audio/Analysis` 中的平台无关算法。C# `PcmMetricAnalyzer` 改为 native handle owner，`Process` 只做参数校验、P/Invoke 调用和 `PcmMetricsSnapshot` 映射。对 native 缺失/失败，首期采用启动失败显式异常或受控 fallback；最终策略由实现证据确认并写入 `evidence.md`。

## 文件 / Ownership

### 允许修改

- `src/DesktopLoudnessMeter.Native/**` — C++ engine、C ABI、project 配置。
- `src/DesktopLoudnessMeter.App/Audio/Analysis/**` — C# wrapper、基线类可见性/测试辅助、native interop。
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` — native 输出复制规则如需调整。
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/**` — analyzer 等价和回归测试。
- `tests/DesktopLoudnessMeter.Benchmarks/**` — benchmark 如需适配 native engine。
- `.aiknowledge/codemap/**` — 归档后刷新知识地图。
- `openspec/changes/2026-06-23-native-core-dsp/**` — controller 串行维护计划和证据。

### 禁止修改

- UI XAML/controls/viewmodel，除非编译因 snapshot 类型变化必须同步，且需先更新 design。
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackInterop*` 平台 capture interop。
- installer/publish/CI 配置，除非 native build 必须且需先更新 design。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `e:/gitlab/uMeter`
- Workspace Branch: `opsx/native-core-dsp-migration`
- Base Branch: `opsx/native-core-dsp-migration` at `4643c9626bd38f02570d6b9565b3bd52c331433e`
- Merge Target: `opsx/native-core-dsp-migration`
- Workspace Rationale:
  - dirty 分类：当前 dirty 仅有 `.aiknowledge/` untracked，来自本次 explore/plan 对 codemap 的初始化，归本 change；无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：当前分支即 `opsx/native-core-dsp-migration`，未发现活动 change 依赖；按普通 change 处理，base 为当前 HEAD `4643c9626bd38f02570d6b9565b3bd52c331433e`。
  - isolated 触发：变更触及 native ABI 和跨模块接口，但当前已有专用分支、无无关 dirty、单 agent 串行执行；不创建额外 isolated worktree。
  - 结论：`current-worktree`。若后续出现无关 dirty 或需要并行验证，再升级为 isolated worktree。

## 验证计划

- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：全部通过。
- `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release`，预期：native analyzer 可运行并输出合理性能/峰值差异。
- `msbuild src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` 或等效 VS Build Tools 命令，预期：native DLL 构建成功。
- 编译 app/test solution，预期：C# P/Invoke 签名和 DLL 复制无错误。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes，仅用于 native build/平台集成 task。
- 不确定性来源：本机 MSVC Build Tools 可用性、native DLL 加载路径、AVX2/RfRapid 初始化行为。
- 人工 / 平台覆盖：自动化覆盖 Windows native build 和 tests；最终需要用户人工启动 app 观察 UI 指标刷新。
- 最终硬化策略：行为稳定后补齐 regression/characterization 测试；doc impact 默认 `TASK_EVIDENCE_ONLY`，若 ABI 或范围调整则升级为 `DESIGN_AMENDMENT`。

## 回退 / 升级

- 回退方式：保持 `PcmMetricsSnapshot` 和调用方 API 不变；若 native engine 集成失败，可在 wrapper 中临时恢复 managed analyzer path，但必须记录为 within-scope fallback 或降级范围。
- 升级条件：native ABI 无法稳定表达 snapshot/buffer 生命周期、native build 环境缺失导致无法验证、或算法偏差超出容差且无法解释。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“只迁 true-peak/FFT”：已有 native backend 但不能满足多平台复用完整指标的目标。
- 不采用“迁移 capture 到 C++”：WASAPI/NAudio 属 Windows 宿主适配，不是平台无关 DSP core。
- 不采用“先做非 Windows UI”：本次只建立可复用算法内核。

## Readiness Notes

- explore 保留的不确定性：未来非 Windows UI 技术栈未定；不影响 core engine 边界。
- 关键风险：native ABI buffer ownership、managed/native 等价容差、native DLL 缺失时行为、AVX2 支持。
- readiness 需检查：R 编号一致、task 文件边界可执行、manual check 没有阻塞实现前进。
