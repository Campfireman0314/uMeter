# Design: 2026-06-29-native-source-ingest-core

正文默认使用中文；路径、命令、配置键和代码标识保持原文。

## 上下文

`LocalLoopbackSignalSourceService` 当前在 C# 中完成 local/plugin loopback source registry、packet lifecycle、per-source analyzer ownership、timeout cleanup 和 `SignalSourceSnapshot` projection。该模块是平台无关 source ingest 状态机，符合 WebView UI + C++ Core 目标中“C++ Core 承担 source ingest 状态机、snapshot generation 和核心状态机”的迁移方向。

旧归档 `2026-06-29-cross-platform-architecture-plan` 仍作为历史切分来源，但其中 Flutter route 已由 `2026-06-29-ui-runtime-architecture-replan` 废弃。本文所有 UI runtime 约束均以 WebView2/WKWebView、HostBridge v1、TypeScript + React + Vite 为准。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] Native source lifecycle core
- 关联需求: [R1]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h`
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h`
  - `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp`
- 验证方式: focused managed tests exercise native lifecycle through `LocalLoopbackSignalSourceService`; native build/smoke evidence confirms exports load.
- 知识沉淀: source lifecycle ownership moves from C# service internals to native core for local/plugin loopback.

### [U2] Identity and reconfiguration parity
- 关联需求: [R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.*`
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
- 验证方式: duplicate-name, rename, same-handle same-format and same-handle changed-format tests.
- 知识沉淀: source key remains `loopback:<handle>`; changed format rebuilds analyzer but preserves settings.

### [U3] Snapshot projection and metric copy
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.*`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs`
  - `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`
- 验证方式: audio-driven peak/loudness/graph arrays and invalid audio tests.
- 知识沉淀: host/UI snapshot semantics remain unchanged; native core supplies data through managed projection.

### [U4] Managed adapter compatibility
- 关联需求: [R4]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs`
  - `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`
- 验证方式: existing callers/tests compile unchanged; reset/settings/performance mode tests pass.
- 知识沉淀: C# service becomes adapter/lock/DTO boundary, not source lifecycle owner.

### [U5] Build and documentation closure
- 关联需求: [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt`
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets`
  - `openspec/changes/2026-06-29-native-source-ingest-core/evidence.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- 验证方式: inspect/build native file lists, run available build/test commands, update evidence and codemap.
- 知识沉淀: CMake remains reserved cross-platform boundary; Windows MSBuild remains verified path.

## 架构决策

### Native core shape

Add an opaque native type:

- `DlmSourceIngestCoreCreate(timeoutMilliseconds, nowMilliseconds, handle*)`
- `DlmSourceIngestCoreDestroy(handle)`
- `Register`, `Heartbeat`, `Rename`, `ProcessF32`, `Close`, `RemoveExpired`
- `ResetSource`, `ResetAll`, `SetTruePeakOversampling`, `SetChannelWaveformEnabled`, `SetChannelSpectrumEnabled`, `SetPerformanceMode`
- `GetSourceCount`, `GetSourceSnapshotAt`, `Copy...` functions for strings and metric arrays

Exact names may vary during implementation, but the ABI must follow existing conventions:

- `extern "C"` export wrappers in `DesktopLoudnessMeterNative.cpp`
- `DLM_NATIVE_EXPORT` and `DLM_NATIVE_CALL`
- integer return codes
- opaque `void*` handles
- caller-owned copy buffers for variable-length arrays/strings

### Time and timeout

C# remains the clock owner because tests and app service already inject `Func<DateTimeOffset>`. The adapter passes monotonic or epoch milliseconds into native calls. Native core compares timestamps and removes expired sources. This keeps deterministic tests and moves source lifecycle policy into native state.

### String and source kind mapping

Native source state stores normalized display names and source kind as small integer enum values. C# remains responsible for mapping native kind to `SignalSourceKind` and building `loopback:<handle>` source keys. This prevents native core from depending on UI/Host string conventions while still letting it own registry identity by handle.

### Analyzer ownership

Each native source state owns one `DlmPcmAnalyzer` instance. Reconfigure with changed sample rate or channel count destroys/recreates that analyzer and resets peak/metrics. Reconfigure with the same format only touches metadata and last-seen timestamp. True-peak oversampling changes also recreate the analyzer and preserve source identity.

### Snapshot projection

Native source snapshot is flat and ABI-safe:

- fixed scalar fields: kind, sample rate, channels, peak, optional metric flags/values, counts, plot frame version
- copied string fields: handle, display name
- copied arrays: waveform, channel waveform per channel, spectrum, channel spectrum per channel, stereo image points

C# maps these fields into existing `SignalSourceSnapshot`. No HostBridge/WebView DTO is changed in this change.

### Error handling

Invalid input returns non-zero codes at native boundary. The managed adapter follows current service behavior:

- invalid register missing sample rate/channels: ignore
- heartbeat/rename/close for unknown handle: ignore
- invalid audio null/empty/channel mismatch: ignore
- disposed service: no-op
- native initialization/export failure: fail fast in tests/app build path like existing `NativePcmAnalyzer`, unless implementation discovers a documented fallback requirement and routes back to plan.

## 风险 / 权衡

- Native ABI growth can become too wide. Mitigation: keep source ingest ABI local/plugin focused and use copy-buffer patterns already present in native analyzer.
- Rewriting source lifecycle risks subtle UI regressions. Mitigation: keep `LocalLoopbackSignalSourceService` public surface and expand tests around known lifecycle pitfalls.
- CMake may not be installed in the environment. Mitigation: update file lists and record build-tool availability; Windows MSBuild/native load tests remain the verified path.
- String copy ABI can be error-prone. Mitigation: use explicit capacity/actualCount semantics and tests for duplicate/renamed names.

## 稳定化策略

- `[stabilization-first]` 适用单元：无
- doc impact: `DESIGN_AMENDMENT` if implementation changes ABI shape materially; otherwise `TASK_EVIDENCE_ONLY`.
- final hardening: focused lifecycle tests, native smoke tests, full .NET tests, app/web build sanity, codemap updates.

## 回退方案

If implementation shows source ingest cannot be separated without protocol/capture changes, stop and return to `opsx-plan`. Do not silently expand scope into WASAPI, JUCE sender, Protocol, HostBridge or Web UI.

## 并行 / Subagent 边界

Default serial implementation in current worktree. Sidecar read-only investigation is allowed if native ABI details become unclear, but final scope and file edits remain controller-owned.
