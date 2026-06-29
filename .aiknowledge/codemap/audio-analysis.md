---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-06-23
last_verified_by: opsx-knowledge
verification_basis: codemap-initialization
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/Analysis
source_refs:
  - user:/opsx-explore core DSP replace with C++
superseded_by:
merged_from:
deprecated_reason:
---

# audio-analysis

## 职责

负责平台无关 PCM 指标处理：响度、真峰值、RMS、声像、波形采样、频谱 bucket 和性能模式。当前主体仍在 C#，但 FFT 和真峰值已有 native fallback/accelerated path。

## 关键文件
| 文件 | 角色 |
|------|------|
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` | 分析入口；处理 interleaved float block，维护 snapshot，调度 K-weighting、true peak、recent window |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricsSnapshot.cs` | UI/服务消费的指标 DTO 边界 |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/KWeightingFilter.cs` | BS.1770 K-weighting 双 biquad |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/SlidingPowerWindow.cs` | 400ms/3s loudness rolling mean power |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/IntegratedLoudnessGate.cs` | integrated loudness absolute/relative gate |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/TruePeakEstimator.cs` | managed sinc/Hann oversampling true-peak estimator |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs` | 100ms recent window；RMS、correlation、stereo image、waveform、spectrum buckets |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/ManagedSpectrumFft.cs` | managed radix-2 FFT fallback |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeRfRapidSpectrumFft.cs` | native FFT adapter with managed fallback |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeRfRapidTruePeakEstimator.cs` | native true-peak adapter |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs` | normal/eco spectrum refresh/resolution policy |

## 隐式约束
- `PcmMetricAnalyzer.Process` 接收 interleaved float samples，长度必须按 channel 对齐。
- 当前支持 mono/stereo；oversampling 只允许 2/4/8。
- `PcmMetricsSnapshot` 不依赖 WPF 控件类型，是迁移后 C# 边界的自然保持点。
- native true-peak 失败后会禁用 native path 并回退 managed path；全量 C++ 化时需重新定义 fallback 策略。
- `RecentSampleWindow` 把 true-peak oversampled mono/interleaved samples 用于波形和频谱显示，不能只返回标量指标。
