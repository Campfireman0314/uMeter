---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive:2026-07-01-native-host-projection-core
applies_to:
  - src/DesktopLoudnessMeter.App/Audio
  - src/DesktopLoudnessMeter.Native
source_refs:
  - user:/opsx-explore core DSP replace with C++
  - change:2026-06-23-native-core-dsp
  - evidence:openspec/changes/archive/2026-06-23-native-core-dsp/evidence.md
  - change:2026-06-29-native-core-build-boundary
  - evidence:openspec/changes/archive/2026-06-29-native-core-build-boundary/evidence.md
  - change:2026-06-29-native-source-ingest-core
  - evidence:openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md
  - change:2026-06-29-juce-sender-protocol-boundary
  - evidence:openspec/changes/archive/2026-06-29-juce-sender-protocol-boundary/evidence.md
  - change:2026-06-30-native-protocol-ingest-core
  - evidence:openspec/changes/archive/2026-06-30-native-protocol-ingest-core/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
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
2. `PcmMetricAnalyzer.Process` 做 C# 边界校验并调用 `NativePcmAnalyzer.Process`。
3. `NativePcmAnalyzer` 通过 `DlmPcmAnalyzer*Export` 把 block 交给 native C++ analyzer。
4. native `DlmPcmAnalyzer` 子模块计算 loudness、true peak、recent RMS/correlation/stereo image、waveform、spectrum 和 performance-mode refresh。
5. `NativePcmAnalyzer.Snapshot` 读取 flat native snapshot，并通过 copy ABI 拉取 waveform/spectrum/stereo-image arrays 到 managed-owned collections。
6. `PcmMetricAnalyzer.Snapshot` 映射为 `PcmMetricsSnapshot`。
7. `WasapiSessionMeterService.MergeMetrics` 或 `LocalLoopbackSignalSourceService.CreateSnapshot` 合并到 UI-facing snapshot。

## 隐式约束

- `PcmMetricsSnapshot` 是 DSP 到 UI/服务的稳定边界；C++ 迁移后仍由 C# wrapper 映射并保持字段语义。
- true-peak / recent-window data 不仅返回 `TruePeakDb`，还驱动 waveform/spectrum/stereo detail shape。
- UI 的 waveform/spectrum/stereo 控件消费已聚合列表，不要求知道 DSP 内部状态。
- native DLL 缺失会导致 native-backed wrapper/benchmark load failure；所有 consuming project 必须导入 `DesktopLoudnessMeter.Native.targets` 或有明确部署策略。当前 Windows verified DLL path 是 `src/DesktopLoudnessMeter.Native/x64/<Configuration>/DesktopLoudnessMeter.Native.dll`，CMake build entry 只是 future cross-platform boundary。
- 真实 UDP sender/receiver socket smoke 可能受本机 loopback 环境影响；协议/source-service 逻辑仍可通过非 socket tests 覆盖，socket path 需在可用 Windows 环境或后续 protocol/packaging smoke 中复查。

## 关键分支

- normal/eco mode 改变 spectrum FFT 刷新节流和 FFT size。
- channel waveform/spectrum 开关只影响 snapshot 是否暴露对应 channel 数据；内部 recent window 仍有缓冲结构。
- oversampling 变化会重建 analyzer，清空历史指标。

## 2026-06-29 native source ingest update

- Local/plugin loopback now enters C++ earlier: `LocalLoopbackSignalSourceService.ProcessAudio` routes packet samples into managed `NativeSourceIngestCore`, which calls `DlmSourceIngestCoreProcessF32Export`; native source state owns the per-source `DlmPcmAnalyzer` and updates peak/metrics.
- C# still maps native flat source snapshots back to `SignalSourceSnapshot` and `PcmMetricsSnapshot`; HostBridge/WebView/WPF consumers do not read native DTOs directly.
- Windows process loopback remains unchanged: `ProcessLoopbackCaptureWorker` / `ProcessLoopbackMetricProvider` still use the existing `PcmMetricAnalyzer` wrapper path and are not part of this source-ingest migration.

## 2026-06-29 protocol boundary update

- Local/plugin sender path now has an explicit Protocol v1 boundary: JUCE sender and SenderKit emit JSON register/heartbeat/audio/rename/close packets using aligned vocabulary, while `LocalLoopbackUdpReceiver` owns host UDP socket/routing glue and delegates protocol bytes to native `DlmLoopbackProtocol`.
- Receiver routing ignores inbound ack, filters non-loopback remotes, sends ack only for valid non-ack packets, and passes decoded `LocalLoopbackPacket` values to `LocalLoopbackSignalSourceService`.
- Native `DlmSourceIngestCore` remains a typed ingest event boundary; native `DlmLoopbackProtocol` owns Protocol v1 JSON parser and ack payloads, and Host glue still owns UDP socket transport.

## 2026-06-30 native protocol ingest update

- Local/plugin receiver path now enters C++ before managed packet routing: Host `LocalLoopbackUdpReceiver` keeps UDP socket receive/send and loopback remote filtering, then calls managed `NativeLoopbackProtocol`, which P/Invokes `DlmLoopbackProtocolDecodeExport`.
- Native `DlmLoopbackProtocol` parses Protocol v1 JSON bytes, validates lifecycle vocabulary, decodes little-endian float32 base64 audio samples, and generates ack JSON payloads.
- Managed `NativeLoopbackProtocol` maps native flat packets back to existing `LocalLoopbackPacket`; `LocalLoopbackSignalSourceService` and `DlmSourceIngestCore` continue to receive typed packets/events.
- `DlmSourceIngestCore` still does not own UDP socket transport; its typed lifecycle/audio ABI remains unchanged.

## 2026-06-30 native host source-list projection update

- After WASAPI and local/plugin services produce `SignalSourceSnapshot` rows, `DesktopMeterHostController` delegates source-list projection metadata to managed `NativeHostSourceProjection`, which P/Invokes native `DlmHostSourceProjection`.
- Native projection owns hidden filtering, explicit/fallback ordering, duplicate display title disambiguation, source identity/title text, workspace row defaults, and selected-source visibility for the host source list.
- PCM metric arrays and loudness/spectrum/waveform values still flow through `SignalSourceSnapshot` and are merged into `HostSourceSnapshot` in managed Host. Recent events, detail/collision snapshots, settings persistence, and command mutation remain Host responsibilities.
