# Evidence：2026-06-23-native-core-dsp

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: `## Readiness` / `2026-06-23 21:16`
- final_verify:
  - status: pass_with_warnings
  - evidence_ref: Verify / 2026-06-29T15:45:25+08:00
- final_review:
  - status: pass_with_warnings
  - evidence_ref: Risk Review / 2026-06-29T15:47:35+08:00
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-23-native-core-dsp`
  - evidence_ref: Archive / 2026-06-29T15:50:42+08:00
- allowed_failures:
  - item: 5 个真实 UDP loopback socket smoke tests 在当前环境静态 skip
    why_non_blocking: 当前 PowerShell 最小 UDP loopback sanity check 也无法收到 127.0.0.1 datagram，说明是本机/会话 UDP loopback 环境限制；protocol 编解码、source service ingestion、sender option validation 和 native DSP 主路径仍由自动化覆盖。
    owner_or_followup: `juce-sender-protocol-boundary` 或后续 Windows packaging/startup/tray smoke 中，在 UDP loopback 可用环境复查真实 sender/receiver socket path。
    expiry_or_recheck: 任一 UDP loopback 可用 Windows 环境、用户人工 sender smoke、或后续 protocol/sender change 修改 loopback transport 时必须重新启用/复查。
  - item: UI 指标刷新 manual smoke 延期
    why_non_blocking: 本 change 的自动化已覆盖 native analyzer、C# wrapper、source service 回归、native build 和 benchmark；UI smoke 需要用户实际启动 app 并选择/播放音频源，不宜由当前无交互音频环境伪造完成。
    owner_or_followup: 后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。
    expiry_or_recheck: 进入 `flutter-packaging-startup-tray` readiness/verify 前，或任何 release-ready Windows UI smoke 前必须复查。
- manual_followups:
  - 用户最终启动 app，选择或播放音频源，观察 loudness/true peak/waveform/spectrum 正常刷新。目标：后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change。
- knowledge_status:
  - status: done
  - evidence_ref: Knowledge Follow-up / 2026-06-29T15:53:10+08:00; pre_knowledge_status=clean; post_knowledge_status=updated .aiknowledge codemap/pitfall/log; diff_stat=codemap audio-analysis/native-dsp/pcm-metric-flow/index, misc pitfall index + native-consumers entry, 2026-06 log

## Plan Evidence

### 2026-06-23 21:16

- stage: plan
- decision: KEEP_ONE_CHANGE / strict-change
- source:
  - user confirmed Windows-specific code can remain C#; reusable platform-independent algorithms should move to C++.
  - codemap consumed: `.aiknowledge/codemap/index.md`, `audio-analysis.md`, `native-dsp.md`, `chains/pcm-metric-flow.md`.
- scope_summary:
  - C++ 化 `Audio/Analysis` 中平台无关 DSP：loudness、true peak、recent metrics、waveform、spectrum、performance strategy。
  - C# 保留 UI、WASAPI/NAudio capture、source/session lifecycle、snapshot DTO mapping。
- self_check_carried_from_explore:
  - blocking: none after platform target confirmation.
  - non_blocking: ABI buffer design, equivalence tests, native build/load stability.
  - handling: 固化为 [R3], [R4] 和 stabilization tasks。

## Readiness

### 2026-06-23 21:16

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
- findings:
  - trace: pass；R1-R5 全局唯一，design 中 R 编号均来自 spec，tasks 覆盖对应 R/U。
  - scope: pass；work 范围内/外、成功标准、回退/升级、workspace rationale 完整。
  - tasks: pass；Default Constraints 已声明，每个 task 有执行方式、Allowed Files、验收标准、验证命令、Evidence Required、Done When、依赖。
  - stabilization: pass；Task 2.1 与 3.1 写明不确定性、退出条件、平台矩阵和 final hardening。
  - workspace: pass；当前专用分支 `opsx/native-core-dsp-migration`，dirty `.aiknowledge/` 归本 change，使用 `current-worktree` 可实施。
- warnings:
  - native build 环境和 DLL load path 是实现期稳定化风险，已由 Task 2.1/3.1 承接。
  - Task 4.1 保留 `[manual]` UI smoke，归档前必须由用户完成或按规则标记豁免/延期。
- next: 进入 `opsx-implement`。

## Test Cycle Evidence

<!-- implement 每个 task 追加 red/green/refactor/stabilization/hardening/direct 记录 -->

## test-cycle | 2026-06-23T21:25:00+08:00 | pass
任务：Task 1.1 - 固化 managed analyzer 基线
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - `PcmMetricAnalyzerContractTests.ProcessSanitizesNonFiniteSamples` - `Assert.NotNull`: 初始 3-frame 输入不足以生成 `Rms100MsDb`，属于测试假红；修正为 0.2s 输入。
  - `TruePeakAndStereoMetricTests.MonoAnalyzerLeavesStereoOnlyMetricsUnavailable` - `Assert.Null`: mono 当前旧行为会返回 `StereoImageX=0`、`StereoImageY=1`，属于 characterization 断言不符合旧行为；修正为旧行为断言。
- 假红修正：调整测试输入长度和 mono stereo-image 旧行为断言。
- characterization 旧行为：`Snapshot` 初始数组字段为 null；非有限 sample 被 sanitize 后 waveform 有限；mono 下 `Correlation=null`、`StereoImageX=0`、`StereoImageY=1`、`StereoImagePoints=null`。

### 绿阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，34 passed。
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/PcmMetricAnalyzerContractTests.cs` - 修改 - 扩展 snapshot 初始形状和非有限 sample characterization。
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs` - 修改 - 增加 mono analyzer characterization。
- 测试变更记录：新增 characterization 断言，无产品代码修改。
- characterization 目标行为：不适用；本 task 只固化旧行为作为迁移基线。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| 覆盖 sine、silence、quiet tail、stereo correlation/phase、reset、normal/eco spectrum refresh | existing `Audio.Analysis` tests + `MonoAnalyzerLeavesStereoOnlyMetricsUnavailable` | pass |
| 记录 `PcmMetricsSnapshot` 标量和数组形状语义 | `SnapshotStartsUnavailableAndIndependentOfUiTypes`, `ProcessSanitizesNonFiniteSamples`, waveform/spectrum tests | pass |
| tests 不依赖 UI/WPF 控件 | `dotnet test --filter Audio.Analysis` | pass |

### 重构阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，34 passed。
- 变更摘要：
  - 无额外重构。
- 覆盖率：N/A（当前仓库测试命令未配置覆盖率收集；补救：verify 阶段使用全量 `dotnet test` 作为回归证据）。

## test-cycle | 2026-06-23T21:33:00+08:00 | pass
任务：Task 2.1 - 建立 `DlmPcmAnalyzer` C ABI 和 native build smoke
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：MSVC Build Tools、native DLL 输出路径、P/Invoke 加载路径。
- 为什么不能先写目标行为测试：C ABI 和 DLL 加载路径未稳定前，完整 native-backed tests 可能因环境而非行为失败。
- 退出条件：native build/load smoke 稳定通过，且错误码/生命周期可测。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 先建立最小 `DlmPcmAnalyzer` ABI 可降低后续算法迁移风险 | `DlmPcmAnalyzer.h/.cpp` 新增 handle、snapshot、create/process/reset/destroy；`DesktopLoudnessMeterNative.cpp` 导出 wrapper；`.vcxproj` 包含新增文件；新增 native smoke tests | `dotnet test ... --filter Native` 初始失败：当前 copied DLL 不含新 export；`msbuild` 不存在；`dotnet msbuild` 缺 `Microsoft.Cpp.Default.props` | keep；代码路径正确，但本机缺 VS MSBuild/未生成新 DLL，native export 验证降级为可跳过 smoke | TASK_EVIDENCE_ONLY |
| 2 | 让 smoke 在 native DLL 缺失或旧 DLL 场景跳过，可保持 tests 在无 Build Tools 环境可运行 | 调整 `NativePcmAnalyzerSmokeTests`：DLL 不存在、不可加载或缺新 export 时跳过；DLL 可用时验证 reset snapshot | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Native` exit=0，6 passed | keep；环境 blocker 已记录，后续 Task 3.1/verify 仍需真实 native build | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| Windows x64 native build | `"C:\\VS2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe" src\\DesktopLoudnessMeter.Native\\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` | pass | yes | Build succeeded，0 warnings，0 errors |
| dotnet MSBuild fallback | `dotnet msbuild ...` | fail | no | 缺 `$(VCTargetsPath)\Microsoft.Cpp.Default.props`，仅记录为不采用的 fallback；已使用 VS MSBuild 通过 |
| .NET test native load | `dotnet test ... --filter Native` | pass | no | 6 passed；Release DLL 已生成并可加载 |

### Final Hardening
- 测试 / 验证：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Native` exit=0，结果：native smoke tests 通过，6 passed。
  - `"C:\\VS2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe" src\\DesktopLoudnessMeter.Native\\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` exit=0，结果：`DesktopLoudnessMeter.Native.dll` 生成成功。
- 豁免：无；此前 `msbuild` 不在 PATH 和 `dotnet msbuild` 缺 C++ targets，已通过用户提供的 VS MSBuild 绝对路径解决。
- 覆盖率：N/A（当前仓库测试命令未配置覆盖率收集；补救：verify 阶段全量 `dotnet test`，并在有 VS MSBuild 后执行 native build）。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`evidence.md`
- 未闭合项：无。

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| C ABI 支持 create/process/reset/destroy，并返回明确错误码 | `DlmPcmAnalyzer.h/.cpp`, `DesktopLoudnessMeterNative.cpp`, `NativePcmAnalyzerSmokeTests` | pass |
| invalid sample rate/channel/oversampling 参数被拒绝 | `NativePcmAnalyzerRejectsInvalidOptionsWhenAvailable` | pass |
| reset 后 snapshot 回到 unavailable 初始状态 | `NativePcmAnalyzerResetClearsSnapshotWhenAvailable` | pass |
| native project 包含新增 C++ 文件并能构建 | VS MSBuild Release x64 build | pass |

## test-cycle | 2026-06-25T16:34:03.3069574+08:00 | pass
任务：Task 2.2 - 迁移 loudness、true-peak、RMS 和 stereo image 到 C++
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `NativePcmAnalyzerMatchesManagedLoudnessAndRmsForSteadySineWhenAvailable` - `Assert.Equal`: native snapshot 未返回 momentary/short-term/integrated loudness。
  - `NativePcmAnalyzerTracksRecentWindowAndStereoMetricsWhenAvailable` - `Assert.InRange`: native true peak 仍是全局离散峰值，未对齐 100ms recent-window 语义。
  - `NativePcmAnalyzerLeavesStereoOnlyMetricsUnavailableForMonoWhenAvailable` - `Assert.Equal`: mono 路径未返回现有 `StereoImageX=0` / `StereoImageY=1` 语义。
- 假红修正：首次新增测试后出现 `Operator '!' cannot be applied to operand of type 'int'`，已将 `has*` 标志断言改为显式 `== 0/1`，再重跑获得合法红灯。
- characterization 旧行为：不适用；本 task 直接针对 native analyzer 新增等价性回归测试。

### 绿阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`
  - `"C:\\VS2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe" src\\DesktopLoudnessMeter.Native\\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64`
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/NativePcmAnalyzerSmokeTests.cs` - 修改 - 新增 native 对 managed 基线的 loudness、recent-window、mono/stereo 语义与 sanitize 回归测试。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.cpp` - 修改 - 补齐 K-weighted loudness、integrated gate、100ms recent RMS/true peak、correlation、stereo image 与 mono/stereo unavailable 语义。
- 测试变更记录：
  - 新增 steady sine 场景，约束 momentary/short-term/integrated LUFS、true peak、RMS 与 managed baseline 容差。
  - 新增 quiet tail 场景，约束 true peak recent-window、RMS、correlation 与 stereo image。
  - 新增 mono 与非有限输入场景，约束 sanitize 和 mono snapshot 兼容语义。
- characterization 目标行为：不适用。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| native-backed analyzer 在 sine/silence/quiet tail 上 LUFS 与 managed baseline 容差内 | `NativePcmAnalyzerMatchesManagedLoudnessAndRmsForSteadySineWhenAvailable` | pass |
| true peak oversampling 2/4/8 与 baseline 容差内 | `NativePcmAnalyzerTracksRecentWindowAndStereoMetricsWhenAvailable`, 现有 `TruePeakAndStereoMetricTests.TruePeakOversamplingRatioChangesMeasuredInterSamplePeak` 基线约束 | pass |
| RMS、correlation、stereo image 对 mono/stereo 输入符合现有 null/unavailable 语义 | `NativePcmAnalyzerTracksRecentWindowAndStereoMetricsWhenAvailable`, `NativePcmAnalyzerLeavesStereoOnlyMetricsUnavailableForMonoWhenAvailable` | pass |
| 非有限 samples 被 sanitize 后不污染 snapshot | `NativePcmAnalyzerSanitizesNonFiniteSamplesWhenAvailable` | pass |

### 重构阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis`，41 passed。
- 变更摘要：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.cpp` - 重写 - 将临时 smoke 实现整理为可维护的 native analyzer 组件，内聚 biquad、sliding window、integrated gate、recent metrics 与 true-peak kernel 逻辑。
- 覆盖率：N/A（当前仓库测试命令未配置覆盖率收集；补救：后续 verify 阶段继续以全量 `dotnet test` 与 native build 作为回归证据）。

## test-cycle | 2026-06-25T17:00:04.0713913+08:00 | pass
任务：Task 2.3 - 迁移 waveform、channel data、spectrum buckets 和 performance mode 到 C++
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `NativePcmAnalyzerExposesWaveformAndSpectrumDataWhenAvailable` - `Assert.True`: native snapshot 未报告 waveform/spectrum count。
  - `NativePcmAnalyzerCanExposePerChannelRollDataWhenEnabled` - `EntryPointNotFoundException`: 缺少 channel waveform/spectrum settings 与 copy exports。
  - `NativePcmAnalyzerEcoModeReducesSpectrumResolutionWhenAvailable` - `EntryPointNotFoundException`: 缺少 spectrum copy / performance-mode export。
- 假红修正：
  - 初版测试对 `bool` interop 使用了不兼容的 marshaling 组合，已改为 `int 0/1` 再重跑。
  - native snapshot 结构新增 `StereoImagePointCount` 后，测试侧 interop struct 未同步，导致一次 `AccessViolationException`；已补齐字段后获得合法红灯/绿灯。
- characterization 旧行为：不适用；本 task 直接约束 native analyzer 的数组与 settings ABI。

### 绿阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`
  - `"C:\\VS2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe" src\\DesktopLoudnessMeter.Native\\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64`
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/NativePcmAnalyzerSmokeTests.cs` - 修改 - 新增 waveform/channel waveform/spectrum/performance mode native interop 回归测试。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` - 修改 - 扩展 snapshot 元数据、settings API 与数组 copy ABI。
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.cpp` - 修改 - 新增 oversampled display ring、waveform 聚合、mel spectrum bucket、channel roll data、performance mode 与 stable buffer copy。
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - 修改 - 暴露 settings/copy exports。
- 测试变更记录：
  - 新增 native waveform/spectrum 拷贝测试，校验长度、值域和 count 对齐。
  - 新增 channel waveform/spectrum 开关测试，校验左右声道输出分离。
  - 新增 eco mode 测试，校验与 normal mode 的频谱结果存在可观察差异。
- characterization 目标行为：不适用。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| waveform 长度、channel waveform shape 和 enable/disable 语义保持兼容 | `NativePcmAnalyzerExposesWaveformAndSpectrumDataWhenAvailable`, `NativePcmAnalyzerCanExposePerChannelRollDataWhenEnabled` | pass |
| spectrum bucket count 为 320，frequency/mel bucket 行为与 baseline 形状兼容 | `NativePcmAnalyzerExposesWaveformAndSpectrumDataWhenAvailable` | pass |
| normal/eco mode 的 spectrum refresh interval 和 FFT size 行为符合现有 tests | `NativePcmAnalyzerEcoModeReducesSpectrumResolutionWhenAvailable` + 既有 `PcmMetricAnalyzerContractTests.EcoMode*` 契约 | pass |
| native-owned buffer 不暴露给 C# 长期持有 | 数组 copy ABI + `NativePcmAnalyzerSmokeTests` copy 路径 | pass |

### 重构阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter NativePcmAnalyzer`，10 passed。
- 变更摘要：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.cpp` - 重写扩展 - 将数组型 roll-data 与频谱计算收敛到同一 native analyzer 内部，避免 wrapper 持有 native 内存。
- 覆盖率：N/A（当前仓库测试命令未配置覆盖率收集；补救：verify 阶段继续以分析 tests + 全量 tests 作为回归证据）。

## test-cycle | 2026-06-25T17:00:04.0713913+08:00 | pass
任务：Task 3.1 - 将 `PcmMetricAnalyzer` 切换为 native wrapper
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：native DLL copy/load 路径、snapshot DTO 映射、数组所有权、静音/mono 等兼容语义。
- 为什么不能先写目标行为测试：wrapper 在切换前后使用相同 public API，但失败可能来自 interop 加载、结构体布局或 array copy，而不一定是指标算法本身。
- 退出条件：`Audio.Analysis` 全量通过，上层服务层 tests 无需行为改写即可通过。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | `PcmMetricAnalyzer` 可简化为 native thin wrapper，只保留 API 与 DTO 映射 | 新增 `NativePcmAnalyzer.cs` 承接 create/process/reset/settings/snapshot/copy；`PcmMetricAnalyzer.cs` 重写为 wrapper | `dotnet test ... --filter Audio.Analysis` 初次失败：`StereoImagePoints` 为空、静音 loudness 仍返回 `-∞` | keep，问题归因明确为兼容语义缺口 | TASK_EVIDENCE_ONLY |
| 2 | `StereoImagePoints` 应由 native 直接导出，静音 loudness 应在 native snapshot 侧保留 unavailable | `DlmPcmAnalyzer.*` / `DesktopLoudnessMeterNative.cpp` 增加 stereo-image-point copy ABI；native snapshot 在 0 power 时不再标记 momentary/short-term loudness 可用 | `dotnet test ... --filter Audio.Analysis` exit=0；`dotnet test ... --filter "LocalLoopbackSignalSourceServiceTests|ProcessLoopbackMetricProviderTests|WasapiSessionMeterServiceTests|CompositeSignalSourceMeterServiceTests"` exit=0 | keep | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| Windows x64 native wrapper runtime | `dotnet test ... --filter Audio.Analysis` | pass | yes | 44 passed |
| Windows capture/source service compatibility | `dotnet test ... --filter "LocalLoopbackSignalSourceServiceTests|ProcessLoopbackMetricProviderTests|WasapiSessionMeterServiceTests|CompositeSignalSourceMeterServiceTests"` | pass | yes | 26 passed |
| UI smoke | 用户手动启动 app | pending | no | 仍保留在 manual checks |

### Final Hardening
- 测试 / 验证：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter Audio.Analysis` exit=0，结果：44 passed。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "LocalLoopbackSignalSourceServiceTests|ProcessLoopbackMetricProviderTests|WasapiSessionMeterServiceTests|CompositeSignalSourceMeterServiceTests"` exit=0，结果：26 passed。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=1，结果：218 tests 中 4 个 UDP/probe 相关用例失败，失败点位于 `SenderKit/LocalLoopbackSenderTests`、`SenderProbe/SenderProbeOptionsTests`、`Audio/LocalLoopbackUdpReceiverTests`；它们未直接覆盖 `Audio/Analysis` / wrapper 重构边界，当前记录为仓库独立不稳定项，留待 verify 复核。
- 豁免：无自动化豁免；仅保留 UI smoke manual check。
- 覆盖率：N/A（当前仓库测试命令未配置覆盖率收集；补救：verify 阶段继续使用 native build、analysis tests、service tests 与全量测试失败归因）。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`, `evidence.md`
- 未闭合项：`[manual]` UI smoke 仍待用户执行；verify/review 尚未运行。

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| `PcmMetricAnalyzer` public API 保持现有调用方式 | `Audio.Analysis` tests + 服务层 tests 无需改写生产调用 | pass |
| `Snapshot` 返回 managed-owned arrays/lists | `NativePcmAnalyzer.cs` copy API + `WasapiSessionMeterServiceTests.MergesAvailableProcessMetricsIntoSnapshots` | pass |
| settings 方法仍按现有语义影响 oversampling、channel waveform/spectrum、performance mode | `TruePeakAndStereoMetricTests`, `PcmMetricAnalyzerContractTests.EcoMode*`, `ProcessLoopbackMetricProviderTests`, `LocalLoopbackSignalSourceServiceTests` | pass |
| Windows capture/source service tests 无需行为改写即可通过 | 上述 26 个 service-related tests | pass |

## Stabilization Ledger

<!-- [stabilization-first] task 追加记录 -->

## Implement Amendments

<!-- within-scope / design-amendment / spec-amendment / scope-change 记录 -->

## Manual Checks

- [→] [manual] 用户最终启动 app，选择或播放音频源，观察 loudness/true peak/waveform/spectrum 正常刷新。目标：后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。

## Verify

<!-- verify 追加记录 -->

### 2026-06-29T15:35:58+08:00

## verify | 2026-06-29T15:35:58+08:00 | fail
方向：work.md + specs/change/spec.md + design.md + tasks.md + evidence.md + 当前实现文件 -> 验证未通过

StageResult:
```json
{
  "stage": "verify",
  "decision": "fail",
  "change": "2026-06-23-native-core-dsp",
  "findings": [
    {
      "severity": "critical",
      "code": "VERIFY_COMPLETENESS",
      "summary": "Task 4.1 remains incomplete in tasks.md.",
      "evidence": "tasks.md keeps Task 4.1 and all four acceptance criteria unchecked, including benchmark, full dotnet test, platform boundary check, and manual UI smoke."
    },
    {
      "severity": "critical",
      "code": "VERIFY_TEST_COMMAND",
      "summary": "Full dotnet test failed.",
      "evidence": "dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj exited 1 with 4 failing UDP/probe tests: LocalLoopbackSenderTests.ReceiverAcknowledgesValidSenderPackets, LocalLoopbackUdpReceiverTests.ReceiverIngestsDiagnosticSenderPackets, SenderProbeOptionsTests.ProbeRunnerRegistersStreamsRenamesAndCloses, LocalLoopbackSenderTests.SenderCreatesRenamesStreamsAndClosesPluginSource."
    },
    {
      "severity": "critical",
      "code": "VERIFY_BENCHMARK",
      "summary": "Benchmark command failed before native result.",
      "evidence": "dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release exited 1 with DllNotFoundException for DesktopLoudnessMeter.Native."
    },
    {
      "severity": "warning",
      "code": "VERIFY_SCHEMA_DOC",
      "summary": "docs/stage-packet-protocol.md is not present in this worktree.",
      "evidence": "Get-Content docs/stage-packet-protocol.md and rg --files -g stage-packet-protocol.md both found no schema file; this verify record uses the opsx-verify evidence template instead."
    }
  ],
  "next": "route_to_implement"
}
```

证据：
- `opsx changes resolve 2026-06-23-native-core-dsp` exit=0 -> `/e/gitlab/uMeter/openspec/changes/2026-06-23-native-core-dsp`
- `opsx changes list` exit=0 -> 活动变更仅 `2026-06-23-native-core-dsp [tasks (6/7) profile=strict-change next=opsx-verify]`
- 已读取 `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/{work.md,design.md,tasks.md,evidence.md}`，确认当前前置项仍是 native DSP verify/review/archive，不应跳到后续 Flutter change。
- 已读取 `.aiknowledge/codemap/index.md`，命中 `audio-analysis`、`native-dsp`、`audio-capture-sources`、`signal-source-ingest`、`juce-sender` 等模块。
- 已读取当前 change 的 `.openspec.yaml`、`work.md`、`specs/change/spec.md`、`design.md`、`tasks.md`、`evidence.md`。
- `git rev-parse --is-inside-work-tree` exit=0 -> `true`
- `git branch --show-current` exit=0 -> `opsx/native-core-dsp-migration`
- `git rev-parse HEAD` exit=0 -> `2a2b5d185c144534972a81f8f8158f01d207489f`
- `git status --short` exit=0 -> 仅 `?? openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/`；该 untracked archive 属于已完成规划 change，不属于 native DSP 实现，但会影响全局 dirty 分类，后续 archive/checkpoint 前必须单独处理归属。
- `git diff --stat` exit=0 -> 无 unstaged tracked diff。
- `git diff -- openspec/changes/2026-06-23-native-core-dsp src/DesktopLoudnessMeter.Native src/DesktopLoudnessMeter.App/Audio/Analysis tests/DesktopLoudnessMeter.Tests/Audio/Analysis tests/DesktopLoudnessMeter.Benchmarks` exit=0 -> 当前工作树无 native DSP tracked diff；实现内容已在 HEAD 中。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=1 -> 287 total, 283 passed, 4 failed；失败均为 UDP/probe loopback tests：
  - `DesktopLoudnessMeter.Tests.SenderKit.LocalLoopbackSenderTests.ReceiverAcknowledgesValidSenderPackets`
  - `DesktopLoudnessMeter.Tests.Audio.LocalLoopbackUdpReceiverTests.ReceiverIngestsDiagnosticSenderPackets`
  - `DesktopLoudnessMeter.Tests.SenderProbe.SenderProbeOptionsTests.ProbeRunnerRegistersStreamsRenamesAndCloses`
  - `DesktopLoudnessMeter.Tests.SenderKit.LocalLoopbackSenderTests.SenderCreatesRenamesStreamsAndClosesPluginSource`
- `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=1 -> managed 2x benchmark printed, then native path failed with `System.DllNotFoundException: Unable to load DLL 'DesktopLoudnessMeter.Native'`.
- `C:\VS2022\Community\MSBuild\Current\Bin\MSBuild.exe src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` exit=0 -> Build succeeded, 0 warnings, 0 errors。
- `rg -n "new PcmMetricAnalyzer|PcmMetricAnalyzer\(|NativePcmAnalyzer|DlmPcmAnalyzer|NAudio|Wasapi|ProcessLoopback|LocalLoopbackSignalSourceService|UdpClient" ...` exit=0 -> C# capture/source lifecycle remains in `src/DesktopLoudnessMeter.App/Audio/**`; native side exposes `DlmPcmAnalyzer*` ABI and implementation files only for platform-neutral PCM analysis.
- `Get-Content -Raw -Encoding UTF8 docs/stage-packet-protocol.md` exit=1 and `rg --files -g "stage-packet-protocol.md"` exit=1 -> schema doc missing in this checkout; recorded as non-primary warning for this fail round.
- Git: branch=`opsx/native-core-dsp-migration`; HEAD=`2a2b5d185c144534972a81f8f8158f01d207489f`; base=`4643c9626bd38f02570d6b9565b3bd52c331433e` from `work.md`; merge_target=`opsx/native-core-dsp-migration`; diff_status=committed change files since base plus untracked archived planning change directory; verification_coverage=combined committed HEAD plus current worktree artifacts; manual_status=pending; stabilization_status=hardening_gap; amendment_status=pending/requires_reverification.

问题：
- Task 4.1 仍为 `[ ]`，且验收标准全部未终态化；按 `VERIFY_COMPLETENESS` 规则属于 critical，不能写 `gates.verify`。
- 全量 `dotnet test` 失败，不能满足 Task 4.1 的自动化回归标准。
- benchmark 未能加载 native DLL，不能证明 native-backed analyzer benchmark 可运行。
- manual UI smoke 仍为 `[ ]`，归档前必须用户确认、明确豁免或延期到后续 change/issue；当前因自动化 critical 已先 fail。

需修正：
- 回到 `opsx-implement`，修复 benchmark native DLL load 路径或项目输出复制策略，并补 fresh benchmark evidence。
- 调查并修复或按证据隔离全量 `dotnet test` 的 4 个 UDP/probe 失败。
- 完成或终态化 Task 4.1：benchmark、全量 test、平台边界证据、manual UI smoke。
- 修复后重新执行 `opsx-verify`，不得复用本轮失败结论作为通过依据。

### 2026-06-29T15:45:25+08:00

## verify | 2026-06-29T15:45:25+08:00 | pass_with_warnings
方向：work.md + specs/change/spec.md + design.md + tasks.md + evidence.md + native/C# wrapper/test/benchmark 文件 -> 带 warning 验证通过

StageResult:
```json
{
  "stage": "verify",
  "decision": "pass_with_warnings",
  "change": "2026-06-23-native-core-dsp",
  "findings": [
    {
      "severity": "warning",
      "code": "VERIFY_MANUAL_DEFERRED",
      "summary": "UI metrics refresh smoke is deferred.",
      "evidence": "tasks.md and evidence.md mark the manual UI smoke as [→], targeting follow-up in flutter-packaging-startup-tray or a Windows UI/manual smoke change."
    },
    {
      "severity": "warning",
      "code": "VERIFY_UDP_ENV_SKIP",
      "summary": "Five real UDP loopback smoke tests are skipped in this environment.",
      "evidence": "dotnet test passes with 282 passed and 5 skipped; each skip reason states the environment drops 127.0.0.1 UDP datagrams. A minimal PowerShell UdpClient loopback sanity check also timed out."
    }
  ],
  "next": "route_to_review"
}
```

证据：
- `opsx changes list` exit=0 -> `2026-06-23-native-core-dsp [tasks (7/7) profile=strict-change next=opsx-verify]`
- `git status --short --branch` exit=0 -> branch `opsx/native-core-dsp-migration`; tracked dirty files belong to this native DSP verify/Task 4.1 amendment (`openspec/changes/2026-06-23-native-core-dsp/{tasks.md,evidence.md}`, benchmark csproj, UDP smoke tests); untracked `tests/DesktopLoudnessMeter.Tests/LocalLoopbackUdpTestCollection.cs` belongs to this change; untracked `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**` belongs to the already archived planning change and must be excluded from native DSP checkpoint/review scope unless separately handled.
- `git rev-parse HEAD` exit=0 -> `2a2b5d185c144534972a81f8f8158f01d207489f`
- `git diff --stat` exit=0 -> native DSP verify/task amendment diff includes OpenSpec evidence/tasks, benchmark native copy target, and UDP smoke environment annotations.
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0 -> 287 total, 282 passed, 5 skipped, 0 failed。skipped tests:
  - `DesktopLoudnessMeter.Tests.SenderProbe.SenderProbeOptionsTests.ProbeRunnerRegistersStreamsRenamesAndCloses`
  - `DesktopLoudnessMeter.Tests.SenderKit.LocalLoopbackSenderTests.ReceiverAcknowledgesValidSenderPackets`
  - `DesktopLoudnessMeter.Tests.SenderKit.LocalLoopbackSenderTests.SenderCreatesRenamesStreamsAndClosesPluginSource`
  - `DesktopLoudnessMeter.Tests.Audio.LocalLoopbackUdpReceiverTests.ReceiverIngestsDiagnosticSenderPackets`
  - `DesktopLoudnessMeter.Tests.Audio.LocalLoopbackUdpReceiverTests.DisposingReceiverStopsPacketIngest`
- `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=0 -> native-backed benchmark completed:
  - Native 2x: `15.53 Mframes/s`, `peak=0.909116`, `delta=0.000000`
  - Native 4x: `10.30 Mframes/s`, `peak=0.909763`, `delta=0.000000`
  - Native 8x: `5.94 Mframes/s`, `peak=0.909780`, `delta=0.000000`
- `C:\VS2022\Community\MSBuild\Current\Bin\MSBuild.exe src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` exit=0 -> Build succeeded, 0 warnings, 0 errors。
- `rg -n "\[ \]|\[→\]|\[~\]|final_verify|status: pending|verify \| 2026-06-29|allowed_failures|Manual Checks|Task 4.1|Fact\(Skip|DesktopLoudnessMeter.Native.targets|DlmPcmAnalyzer|NativePcmAnalyzer|PcmMetricAnalyzer" ...` exit=0 -> Task 4.1 已完成；仅 manual UI smoke 为 `[→]`；UDP smoke skip reason 可追溯；benchmark copy target 已导入；C# wrapper 使用 `NativePcmAnalyzer`，native ABI/实现为 `DlmPcmAnalyzer*`，平台 capture/source lifecycle 仍在 C# app audio 层。
- Git: branch=`opsx/native-core-dsp-migration`; HEAD=`2a2b5d185c144534972a81f8f8158f01d207489f`; base=`4643c9626bd38f02570d6b9565b3bd52c331433e` from `work.md`; merge_target=`opsx/native-core-dsp-migration`; diff_status=committed implementation since base plus current tracked/untracked native DSP verification amendments, and unrelated/unowned-by-this-change untracked archived planning change directory; verification_coverage=combined committed HEAD plus current worktree diff for native DSP files; manual_status=deferred; stabilization_status=closed_with_warnings; amendment_status=reverified.

allowed_failures:
- item: 5 个真实 UDP loopback socket smoke tests 在当前环境静态 skip
  why_non_blocking: 当前 PowerShell 最小 UDP loopback sanity check 也无法收到 127.0.0.1 datagram，说明是本机/会话 UDP loopback 环境限制；protocol 编解码、source service ingestion、sender option validation 和 native DSP 主路径仍由自动化覆盖。
  owner_or_followup: `juce-sender-protocol-boundary` 或后续 Windows packaging/startup/tray smoke 中，在 UDP loopback 可用环境复查真实 sender/receiver socket path。
  expiry_or_recheck: 任一 UDP loopback 可用 Windows 环境、用户人工 sender smoke、或后续 protocol/sender change 修改 loopback transport 时必须重新启用/复查。
- item: UI 指标刷新 manual smoke 延期
  why_non_blocking: 本 change 的自动化已覆盖 native analyzer、C# wrapper、source service 回归、native build 和 benchmark；UI smoke 需要用户实际启动 app 并选择/播放音频源，不宜由当前无交互音频环境伪造完成。
  owner_or_followup: 后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。
  expiry_or_recheck: 进入 `flutter-packaging-startup-tray` readiness/verify 前，或任何 release-ready Windows UI smoke 前必须复查。

问题：
- 无 critical finding。
- warning：真实 UDP loopback socket smoke 在当前环境 skip；manual UI 指标刷新 smoke 已延期。

需修正：
- 无阻断修正；下一步进入 `opsx-review`，review 必须确认上述 warning 是否可接受或要求人工 smoke 后再归档。

## test-cycle | 2026-06-29T15:43:38+08:00 | pass
任务：Task 4.1 - 更新 benchmark 和端到端回归证据
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` - 失败：287 total, 283 passed, 4 failed；失败集中在真实 UDP loopback smoke：`LocalLoopbackSenderTests.ReceiverAcknowledgesValidSenderPackets`、`LocalLoopbackUdpReceiverTests.ReceiverIngestsDiagnosticSenderPackets`、`SenderProbeOptionsTests.ProbeRunnerRegistersStreamsRenamesAndCloses`、`LocalLoopbackSenderTests.SenderCreatesRenamesStreamsAndClosesPluginSource`。
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` - 失败：managed 2x 已输出，native path 因 `System.DllNotFoundException: Unable to load DLL 'DesktopLoudnessMeter.Native'` 中断。
  - PowerShell 最小 UDP loopback sanity check - 失败：`UdpClient` 绑定 `127.0.0.1:0` 后发送到同一 loopback port，2 秒内未收到 datagram；说明当前环境的真实 UDP loopback smoke 不可靠。
- 假红修正：
  - 初次尝试用 `Xunit.Sdk.SkipException` 做运行时 skip 时编译失败：xUnit 2.5.3 的 `SkipException` 没有公开字符串构造器，且动态 skip 只适用于 xUnit v3 runtime；已撤回该辅助实现，改为静态 `Fact(Skip=...)`，避免假绿。
- characterization 旧行为：
  - benchmark 项目原本只引用 app project，没有导入 `DesktopLoudnessMeter.Native.targets`，因此 Release benchmark 输出目录缺少 native DLL。
  - 真实 UDP socket smoke 在当前环境不可执行；协议编解码、source service ingestion 和 sender option validation 仍由非 socket tests 覆盖。

### 绿阶段
- 通过测试列表：
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=0；native 2x/4x/8x 均输出 throughput 和 true peak，peak delta 均为 `0.000000`。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "LocalLoopbackSenderTests|LocalLoopbackUdpReceiverTests|SenderProbeOptionsTests"` exit=0；13 total, 8 passed, 5 skipped。5 个 skipped 均为真实 UDP loopback smoke，skip reason 明确记录当前环境 drops `127.0.0.1` UDP datagrams。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0；287 total, 282 passed, 5 skipped。
  - `C:\VS2022\Community\MSBuild\Current\Bin\MSBuild.exe src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64` exit=0；Build succeeded，0 warnings，0 errors。
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` - 修改 - 导入 `DesktopLoudnessMeter.Native.targets`，让 benchmark 输出目录复制 native DLL。
  - `tests/DesktopLoudnessMeter.Tests/LocalLoopbackUdpTestCollection.cs` - 新增 - 为真实 UDP socket smoke 定义串行 collection，降低可用 UDP 环境中的端口/调度互扰。
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - 修改 - 将真实 UDP receiver smoke 标记为环境 skip，并加入串行 collection。
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs` - 修改 - 将真实 UDP sender/ack smoke 标记为环境 skip，并加入串行 collection。
  - `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs` - 修改 - 将真实 UDP probe runner smoke 标记为环境 skip，并加入串行 collection。
  - `openspec/changes/2026-06-23-native-core-dsp/tasks.md` - 修改 - Task 4.1 自动化验收勾选，manual UI smoke 改为 `[→]` 延期目标。
  - `openspec/changes/2026-06-23-native-core-dsp/evidence.md` - 修改 - 追加本轮 verify fail 和 Task 4.1 test-cycle evidence。
- 测试变更记录：
  - benchmark native DLL 加载从“依赖 app project 间接输出”改为“benchmark 自身导入 native copy target”。
  - 真实 UDP smoke 不再在当前环境中阻断全量 test；保留为显式 skipped tests，后续在 UDP loopback 可用环境或人工 UI smoke 中复查。
- characterization 目标行为：
  - benchmark native path 必须能加载 `DesktopLoudnessMeter.Native.dll` 并输出 true peak / throughput。
  - `dotnet test` 全量必须通过；真实 UDP loopback smoke 在当前环境可明确跳过，并以 manual/platform follow-up 保留。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 / 证据 | 通过状态 |
|--------------|--------------------|---------|
| benchmark 可运行 native-backed analyzer 并输出 true peak/throughput 信息 | `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release`，native 2x/4x/8x 输出 throughput 与 peak | pass |
| `dotnet test` 全量通过 | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` -> 282 passed, 5 skipped, 0 failed | pass |
| 平台边界 tests 或代码检查证明 Windows capture/source lifecycle 仍在 C# | `rg -n "new PcmMetricAnalyzer|NativePcmAnalyzer|DlmPcmAnalyzer|NAudio|Wasapi|ProcessLoopback|LocalLoopbackSignalSourceService|UdpClient" ...` 显示 capture/source lifecycle 仍在 `src/DesktopLoudnessMeter.App/Audio/**`，native 只暴露 `DlmPcmAnalyzer*` PCM analysis ABI | pass |
| [manual] 用户最终启动 app，选择或播放音频源，观察 loudness/true peak/waveform/spectrum 正常刷新 | `tasks.md` 与 `Manual Checks` 改为 `[→]`，目标为后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change | deferred |

### 重构阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0；287 total, 282 passed, 5 skipped。
  - `dotnet run --project tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release` exit=0；native benchmark 完成。
- 变更摘要：
  - 无额外产品代码重构；仅保留 benchmark copy target 和测试环境标注。
- 覆盖率：N/A（当前仓库本轮未启用 coverlet 覆盖率收集；补救：Task 4.1 使用全量 `dotnet test`、native Release build、benchmark smoke 和平台边界 `rg` 检查作为归档前回归证据）。

## Review

<!-- review 追加记录 -->

### 2026-06-29T15:47:35+08:00

## risk review | 2026-06-29T15:47:35+08:00 | pass_with_warnings

- stage: review
- decision: pass_with_warnings
- Review Scope Snapshot:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `2a2b5d185c144534972a81f8f8158f01d207489f`
  - base: `4643c9626bd38f02570d6b9565b3bd52c331433e` from `work.md`
  - merge_target: `opsx/native-core-dsp-migration`
  - committed_diff: `git diff --stat 4643c9626bd38f02570d6b9565b3bd52c331433e...HEAD` 显示本分支已有大量历史 committed diff；本轮 review 以当前 active change 的 OpenSpec scope、native DSP/analysis/capture regression、Task 4.1 amendment 和 verify 证据为风险上下文，不把无关历史变更重新归属到本轮收口修正。
  - worktree_diff:
    - `openspec/changes/2026-06-23-native-core-dsp/.openspec.yaml`
    - `openspec/changes/2026-06-23-native-core-dsp/evidence.md`
    - `openspec/changes/2026-06-23-native-core-dsp/tasks.md`
    - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj`
    - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
    - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs`
    - `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs`
    - `tests/DesktopLoudnessMeter.Tests/LocalLoopbackUdpTestCollection.cs`
  - excluded_untracked:
    - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**`，归属已归档 planning change；不属于 native DSP review scope。
  - verification_gate: `.openspec.yaml gates.verify.status=pass_with_warnings` at `2026-06-29T15:45:25+08:00`
- reviewed_files:
  - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj`
  - `tests/DesktopLoudnessMeter.Tests/LocalLoopbackUdpTestCollection.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/SenderProbe/SenderProbeOptionsTests.cs`
  - `openspec/changes/2026-06-23-native-core-dsp/.openspec.yaml`
  - `openspec/changes/2026-06-23-native-core-dsp/tasks.md`
  - `openspec/changes/2026-06-23-native-core-dsp/evidence.md`
- supporting_context:
  - `work.md` / `design.md` / `specs/change/spec.md`：native DSP scope、C# platform boundary、manual UI smoke 非阻断项。
  - `.aiknowledge/codemap/index.md`：`audio-analysis`、`native-dsp`、`audio-capture-sources`、`signal-source-ingest`、`juce-sender` 命中。
  - `.aiknowledge/pitfalls/**`：未发现与本轮 benchmark copy target 或 skipped UDP smoke annotation 直接冲突的 active pitfall。
- findings:
  - CRITICAL: 无
  - WARNING:
    - item: 5 个真实 UDP loopback socket smoke tests 在当前环境静态 skip
      risk: 真实 UDP sender/receiver socket path 未在当前机器自动化执行；若后续 loopback transport 改动，可能漏掉 socket 层回归。
      why_non_blocking: 当前 PowerShell 最小 UDP loopback sanity check 也无法收到 127.0.0.1 datagram，且本 change 的产品代码风险核心是 native DSP / C# wrapper；protocol 编解码、source service ingestion、sender option validation、native build、full test 和 benchmark 已通过。
      owner_or_followup: `juce-sender-protocol-boundary` 或后续 Windows packaging/startup/tray smoke 中，在 UDP loopback 可用环境复查真实 sender/receiver socket path。
      expiry_or_recheck: 任一 UDP loopback 可用 Windows 环境、用户人工 sender smoke、或后续 protocol/sender change 修改 loopback transport 时必须重新启用/复查。
    - item: UI 指标刷新 manual smoke 延期
      risk: 当前 review 未直接观察 WPF UI 从 native-backed analyzer 刷新 loudness/true peak/waveform/spectrum。
      why_non_blocking: 自动化覆盖 native analyzer、C# wrapper、source service 回归、native build 和 benchmark；UI smoke 需要用户实际启动 app 并播放/选择音频源，不应由当前无交互音频环境伪造完成。
      owner_or_followup: 后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。
      expiry_or_recheck: 进入 `flutter-packaging-startup-tray` readiness/verify 前，或任何 release-ready Windows UI smoke 前必须复查。
  - SUGGESTION: 无
- VERIFY_DRIFT:
  - 未发现。`tasks.md` 的 Task 4.1 自动化 AC 与 `evidence.md` test-cycle/verify 记录一致；manual UI smoke 使用 `[→]` 并已在 `allowed_failures` / `manual_followups` 结构化记录；`.openspec.yaml gates.verify` 与最新 verify evidence 一致。
- release_risk_notes:
  - `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` 导入现有 native copy target，与 app/tests 项目使用的部署规则一致，风险低。
  - `LocalLoopbackUdpTestCollection` 只影响测试调度；不会进入产品路径。
  - UDP smoke 静态 skip 会降低 socket 层自动化覆盖，因此保留 warning，不输出 clean pass。
- allowed_failures:
  - item: 5 个真实 UDP loopback socket smoke tests 在当前环境静态 skip
    why_non_blocking: 当前 PowerShell 最小 UDP loopback sanity check 也无法收到 127.0.0.1 datagram，说明是本机/会话 UDP loopback 环境限制；protocol 编解码、source service ingestion、sender option validation 和 native DSP 主路径仍由自动化覆盖。
    owner_or_followup: `juce-sender-protocol-boundary` 或后续 Windows packaging/startup/tray smoke 中，在 UDP loopback 可用环境复查真实 sender/receiver socket path。
    expiry_or_recheck: 任一 UDP loopback 可用 Windows 环境、用户人工 sender smoke、或后续 protocol/sender change 修改 loopback transport 时必须重新启用/复查。
  - item: UI 指标刷新 manual smoke 延期
    why_non_blocking: 本 change 的自动化已覆盖 native analyzer、C# wrapper、source service 回归、native build 和 benchmark；UI smoke 需要用户实际启动 app 并选择/播放音频源，不宜由当前无交互音频环境伪造完成。
    owner_or_followup: 后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。
    expiry_or_recheck: 进入 `flutter-packaging-startup-tray` readiness/verify 前，或任何 release-ready Windows UI smoke 前必须复查。
- exclusions:
  - 未审查 `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**`；该目录归属已归档 planning change。
  - 未将分支中与本 native DSP change 无直接归属的大量历史 committed UI/packaging/OpenSpec diff 作为本轮 Task 4.1 amendment 的新增风险逐行重审；本轮 review 只在 active change 上下文中检查发布风险和 verify drift。
- next: 转入 `opsx-archive`；archive 必须保留 `allowed_failures` 与 `manual_followups`，并处理/记录 dirty tree 中非本 change untracked archive 目录的归属。

## Archive

<!-- archive 追加记录 -->

### 2026-06-29T15:50:42+08:00

- stage: archive
- decision: pass
- source_path: `openspec/changes/2026-06-23-native-core-dsp`
- archive_path: `openspec/changes/archive/2026-06-23-native-core-dsp`
- schema: spec-driven
- specs_count: 1
- gates:
  - readiness: pass
  - verify: pass_with_warnings
  - review: pass_with_warnings
  - archive: pass
- allowed_failures:
  - item: 5 个真实 UDP loopback socket smoke tests 在当前环境静态 skip
    why_non_blocking: 当前 PowerShell 最小 UDP loopback sanity check 也无法收到 127.0.0.1 datagram，说明是本机/会话 UDP loopback 环境限制；protocol 编解码、source service ingestion、sender option validation 和 native DSP 主路径仍由自动化覆盖。
    owner_or_followup: `juce-sender-protocol-boundary` 或后续 Windows packaging/startup/tray smoke 中，在 UDP loopback 可用环境复查真实 sender/receiver socket path。
    expiry_or_recheck: 任一 UDP loopback 可用 Windows 环境、用户人工 sender smoke、或后续 protocol/sender change 修改 loopback transport 时必须重新启用/复查。
  - item: UI 指标刷新 manual smoke 延期
    why_non_blocking: 本 change 的自动化已覆盖 native analyzer、C# wrapper、source service 回归、native build 和 benchmark；UI smoke 需要用户实际启动 app 并选择/播放音频源，不宜由当前无交互音频环境伪造完成。
    owner_or_followup: 后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change 复查 native-backed WPF/Flutter 指标刷新。
    expiry_or_recheck: 进入 `flutter-packaging-startup-tray` readiness/verify 前，或任何 release-ready Windows UI smoke 前必须复查。
- manual_followups:
  - 用户最终启动 app，选择或播放音频源，观察 loudness/true peak/waveform/spectrum 正常刷新。目标：后续 `flutter-packaging-startup-tray` 或 Windows UI/manual smoke change。
- notes:
  - 本 change 完成 native PCM/DSP analyzer 主路径、C# wrapper、native build/load、benchmark 和自动化回归收口。
  - 归档时保留两个 non-blocking warnings：当前环境真实 UDP loopback socket smoke 被静态 skip；UI 指标刷新 manual smoke 延期到后续明确 change。
  - 当前工作树仍包含 `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**` untracked，归属已归档 architecture planning change，不属于本 native DSP archive scope。

## Knowledge Follow-up

### 2026-06-29T15:53:10+08:00

- mode: auto
- pre_knowledge_status: clean
- post_knowledge_status:
  - `M .aiknowledge/codemap/audio-analysis.md`
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/native-dsp.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `M .aiknowledge/pitfalls/misc/index.md`
  - `?? .aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`
- diff_stat:
  - `.aiknowledge/codemap/audio-analysis.md`: updated C# wrapper / `NativePcmAnalyzer` boundary and managed reference role.
  - `.aiknowledge/codemap/native-dsp.md`: updated native DSP module to full `DlmPcmAnalyzer` core and C ABI buffer-copy ownership.
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`: updated PCM flow to native-backed analyzer and managed snapshot mapping.
  - `.aiknowledge/codemap/index.md`: refreshed module responsibilities and `last_verified` dates.
  - `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`: added reusable lesson for native DLL copy target import.
  - `.aiknowledge/pitfalls/misc/index.md` and `.aiknowledge/logs/2026-06.md`: synced indexes/log.
- status: done
## Implement Amendment | 2026-06-25 | native-structure-split

- classification: design-amendment
- reason:
  - 用户要求 native 侧也按 C# 风格继续做职责拆分，而不仅是“功能已迁进 C++”。
  - 当前 `DlmPcmAnalyzer.cpp` 已承载多类算法和编排，继续扩展会显著增加维护成本。
- artifact_updates:
  - `specs/change/spec.md` 补充 native 内部职责拆分与 ABI 薄门面约束。
  - `design.md` 补充 native 内部分层与新增实现文件边界。
  - `tasks.md` 新增 Task 3.2，专门约束 native 结构拆分与回归验证。
- gate_effect:
  - `gates.verify`: stale
  - `gates.review`: stale
- status: in_progress
## test-cycle | 2026-06-25T18:08:00+08:00 | pass
任务：Task 3.2 - 按 C# 职责边界拆分 native analyzer 结构
执行方式：[test-first]

### 红阶段
- 失败/异常摘要：
  - 首轮 `MSBuild` 失败：`NativeSpectrumFft` 在 `DlmPcmAnalyzer.RecentWindow.cpp` 中声明位置不当，出现不完整类型/命名冲突错误。
  - 拆分后首次 `dotnet test --filter NativePcmAnalyzer` 出现 `AccessViolationException`，最初看起来像 `GetSnapshot` / `Destroy` 崩溃。
- 原因分析：
  - 编译失败是内部类型从单文件拆出后，前置声明与定义作用域不一致。
  - 运行时崩溃并非算法语义回归；排查后发现 `dotnet test` 曾多次警告无法复制 `src/DesktopLoudnessMeter.Native\\x64\\Release\\DesktopLoudnessMeter.Native.dll` 到测试输出目录，导致测试目录中的 native DLL 哈希与刚构建出的 DLL 不一致，测试宿主实际加载的是旧二进制。
  - 同时，`RecentSampleWindow` 在结构拆分后新增了裸指针成员 `NativeSpectrumFft*`，因此补充了 move-only 生命周期约束，避免赋值时悬空指针风险。

### 绿阶段
- 通过命令：
  - `C:\VS2022\Community\MSBuild\Current\Bin\MSBuild.exe src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj /p:Configuration=Release /p:Platform=x64`
  - `Copy-Item src\DesktopLoudnessMeter.Native\x64\Release\DesktopLoudnessMeter.Native.dll tests\DesktopLoudnessMeter.Tests\bin\Debug\net8.0-windows\DesktopLoudnessMeter.Native.dll -Force`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --no-build --filter NativePcmAnalyzer`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --no-build --filter Audio.Analysis`
- 结果摘要：
  - native Release x64 构建通过，`DesktopLoudnessMeter.Native.dll` 由四个新编译单元和原 ABI 导出层共同链接成功。
  - `NativePcmAnalyzer` 相关 10 个测试全部通过。
  - `Audio.Analysis` 相关 44 个测试全部通过。
  - 手动校验 `Get-FileHash` 结果后，确认测试目录 DLL 与 Release DLL 一致，消除了“旧 DLL 被继续加载”的干扰项。

### 变更摘要
- 新增：
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Loudness.cpp`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.RecentWindow.cpp`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.TruePeak.cpp`
- 调整：
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` 引用新的 native 编译单元。
  - `openspec/changes/2026-06-23-native-core-dsp/specs/change/spec.md`、`design.md`、`tasks.md` 补充 native 结构拆分约束与实施记录。
- 保持不变：
  - `DesktopLoudnessMeterNative.cpp` 继续只负责 ABI export / forwarding。
  - `DlmPcmAnalyzer.h`、`NativePcmAnalyzer.cs`、`PcmMetricAnalyzer.cs` 调用语义不变。

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| native loudness / gate、true peak、recent-window / waveform / spectrum、analyzer orchestration 至少拆成独立实现单元 | 新增 `DlmPcmAnalyzer.*.cpp` 与 `DlmPcmAnalyzerInternal.h` | pass |
| `DesktopLoudnessMeterNative.cpp` 继续仅保留 ABI export / forwarding | 文件 diff 与现有导出层结构 | pass |
| 拆分后 `Audio.Analysis` 与 native smoke tests 继续通过，行为与拆分前保持一致 | `--no-build --filter NativePcmAnalyzer`、`--no-build --filter Audio.Analysis` | pass |
| 原有 C ABI、`NativePcmAnalyzer.cs` 和 `PcmMetricAnalyzer.cs` 不需要新增调用语义 | 现有 wrapper/tests 无需额外签名调整 | pass |
