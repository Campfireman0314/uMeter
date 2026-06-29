---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-06-23
last_verified_by: opsx-knowledge
verification_basis: codemap-initialization
applies_to:
  - src/DesktopLoudnessMeter.App/Audio
  - src/DesktopLoudnessMeter.Native
source_refs:
  - user:/opsx-explore core DSP replace with C++
superseded_by:
merged_from:
deprecated_reason:
---

# PCM metric flow

## 触发点

- Windows process loopback: `ProcessLoopbackCaptureWorker.ReadPacket` 捕获 PCM bytes，转换为 `float[]` 后调用 analyzer。
- Local/plugin loopback: `LocalLoopbackSignalSourceService.ProcessAudio` 接收 packet samples 后调用 source analyzer。

## 调用链

1. `ProcessLoopbackCaptureWorker.ReadPacket` 或 `LocalLoopbackSignalSourceService.SourceState.Process` 输入 interleaved `float[]`。
2. `PcmMetricAnalyzer.Process` normalize 非有限值，逐帧计算 K-weighted power、true peak oversampling、recent sample window。
3. `SlidingPowerWindow` 产出 momentary/short-term mean power；`IntegratedLoudnessGate` 产出 integrated loudness。
4. `RecentSampleWindow` 产出 RMS、correlation、stereo image、waveform、spectrum buckets；频谱通过 `SpectrumFftProvider.Create()` 优先 native FFT。
5. `PcmMetricAnalyzer.RefreshSnapshot` 生成 `PcmMetricsSnapshot`。
6. `WasapiSessionMeterService.MergeMetrics` 或 `LocalLoopbackSignalSourceService.CreateSnapshot` 合并到 UI-facing snapshot。

## 隐式约束

- `PcmMetricsSnapshot` 是 DSP 到 UI/服务的稳定边界；迁移 C++ 时优先保持字段语义不变。
- true-peak 不仅返回 `TruePeakDb`，还为 waveform/spectrum 提供 oversampled samples。
- UI 的 waveform/spectrum/stereo 控件消费已聚合列表，不要求知道 DSP 内部状态。
- native DLL 缺失时现有行为仍可用 managed fallback；全量迁移会改变该可恢复路径，需要在 plan 中显式决策。

## 关键分支

- normal/eco mode 改变 spectrum FFT 刷新节流和 FFT size。
- channel waveform/spectrum 开关只影响 snapshot 是否暴露对应 channel 数据；内部 recent window 仍有缓冲结构。
- oversampling 变化会重建 analyzer，清空历史指标。
