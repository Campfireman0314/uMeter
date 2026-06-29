---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-06-23
last_verified_by: opsx-knowledge
verification_basis: codemap-initialization
applies_to:
  - src/DesktopLoudnessMeter.App/Audio
source_refs:
  - user:/opsx-explore core DSP replace with C++
superseded_by:
merged_from:
deprecated_reason:
---

# audio-capture-sources

## 职责

负责采集/接收音频、按源管理 analyzer 生命周期，并把 `PcmMetricsSnapshot` 合并到 session/source snapshot，最终供 ViewModel/UI 展示。

## 关键文件
| 文件 | 角色 |
|------|------|
| `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs` | Windows process loopback capture thread；PCM bytes -> float[] -> `PcmMetricAnalyzer` |
| `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackMetricProvider.cs` | 按进程管理 capture worker，转发 oversampling/waveform/spectrum/performance 设置 |
| `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` | WASAPI session 列表与 process metrics 合并 |
| `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` | 本地/plugin loopback packet 到 source analyzer 的生命周期和 metrics snapshot |
| `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs` | WASAPI sample bytes 转 interleaved float samples |
| `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs` | loopback packet 音频样本协议 |

## 隐式约束
- UI 层不直接调用 DSP；服务层消费 `PcmMetricsSnapshot` 和 source/session snapshot。
- `ProcessLoopbackCaptureWorker` 内部加锁访问 analyzer；native engine 需要明确线程/实例所有权。
- `LocalLoopbackSignalSourceService.SourceState` 每个 source 持有独立 analyzer，切换 oversampling 会重建 analyzer 并丢弃历史状态。
- 采集仍依赖 Windows/NAudio；不属于平台无关核心 DSP 迁移范围，但决定 C# wrapper 输入格式。
