---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-06-29
last_verified_by: opsx-archive
verification_basis: change:2026-06-23-native-core-dsp archived
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/Analysis
source_refs:
  - user:/opsx-explore core DSP replace with C++
  - change:2026-06-23-native-core-dsp
  - evidence:openspec/changes/archive/2026-06-23-native-core-dsp/evidence.md
superseded_by:
merged_from:
deprecated_reason:
---

# audio-analysis

## 职责

负责 C# analyzer 边界：`PcmMetricAnalyzer` 保持现有 public API，拥有 `NativePcmAnalyzer` handle wrapper，映射 `PcmMetricsSnapshot`，并把 waveform/spectrum/stereo-image arrays 复制为 managed-owned 数据。平台无关 PCM 指标主实现已迁入 `DesktopLoudnessMeter.Native`。

## 关键文件
| 文件 | 角色 |
|------|------|
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` | public analyzer facade；参数校验、settings forwarding、snapshot DTO mapping |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs` | P/Invoke owner；create/process/reset/settings/get snapshot/copy arrays/destroy |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs` | UI/服务消费的指标 DTO 边界 |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs` | managed reference/helper for BS.1770 K-weighting tests and comparison |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs` | managed reference/helper for loudness window semantics |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs` | managed reference/helper for integrated loudness gate semantics |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs` | managed reference true-peak estimator used by tests/benchmarks |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs` | managed reference for recent-window waveform/spectrum semantics and rendering-adjacent helpers |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/ManagedSpectrumFft.cs` | managed radix-2 FFT fallback |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeRfRapidSpectrumFft.cs` | native FFT adapter with managed fallback |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeRfRapidTruePeakEstimator.cs` | native true-peak adapter |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs` | normal/eco spectrum refresh/resolution policy |

## 隐式约束
- `PcmMetricAnalyzer.Process` 接收 interleaved float samples，长度必须按 channel 对齐。
- 当前支持 mono/stereo；oversampling 只允许 2/4/8。
- `PcmMetricsSnapshot` 不依赖 WPF 控件类型，是迁移后 C# 边界的自然保持点。
- `PcmMetricAnalyzer` 现在偏 fail-fast/native-backed；不要假设完整 managed analyzer fallback 仍是产品主路径。
- `NativePcmAnalyzer` 必须返回 managed-owned arrays/lists；不能让 UI 或 source services 长期持有 native 内存。
- `RecentSampleWindow` 等 managed classes 仍是等价/基线语义的重要参考；后续删除前必须确认测试覆盖不下降。
