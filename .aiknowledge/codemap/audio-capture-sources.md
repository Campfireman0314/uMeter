---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive:2026-07-01-native-host-projection-core
applies_to:
  - src/DesktopLoudnessMeter.App/Audio
source_refs:
  - user:/opsx-explore core DSP replace with C++
  - change:2026-06-29-native-source-ingest-core
  - evidence:openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md
  - change:2026-06-30-native-protocol-ingest-core
  - evidence:openspec/changes/archive/2026-06-30-native-protocol-ingest-core/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
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
| `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` | Local/plugin loopback packet adapter over native source-ingest core, source-key mapping, timeout calls, and UI-facing source snapshot mapping |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs` | Managed wrapper for native Protocol v1 receiver-side decode and ack generation |
| `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp` | Native Protocol v1 JSON decode, validation, float32 base64 decode, and ack encoding |
| `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs` | WASAPI sample bytes 转 interleaved float samples |
| `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs` | loopback packet 音频样本协议 |

## 隐式约束
- UI 层不直接调用 DSP；服务层消费 `PcmMetricsSnapshot` 和 source/session snapshot。
- `ProcessLoopbackCaptureWorker` 内部加锁访问 analyzer；native engine 需要明确线程/实例所有权。
- Local/plugin Protocol v1 decode and ack payload generation now live in native `DlmLoopbackProtocol`; Host `LocalLoopbackUdpReceiver` still owns UDP socket transport and loopback remote filtering.
- Local/plugin loopback source lifecycle and per-source analyzer ownership live in native `DlmSourceIngestCore`; `LocalLoopbackSignalSourceService` owns locking, packet routing, key/kind mapping, and managed snapshot projection.
- 采集仍依赖 Windows/NAudio；不属于平台无关核心 DSP 迁移范围，但决定 C# wrapper 输入格式。
