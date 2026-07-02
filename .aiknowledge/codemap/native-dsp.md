---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - src/DesktopLoudnessMeter.Native
source_refs:
  - user:/opsx-explore core DSP replace with C++
  - change:2026-06-23-native-core-dsp
  - evidence:openspec/changes/archive/2026-06-23-native-core-dsp/evidence.md
  - change:2026-06-29-native-core-build-boundary
  - evidence:openspec/changes/archive/2026-06-29-native-core-build-boundary/evidence.md
  - change:2026-06-29-native-source-ingest-core
  - evidence:openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md
  - change:2026-06-30-native-protocol-ingest-core
  - evidence:openspec/changes/archive/2026-06-30-native-protocol-ingest-core/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
  - change:2026-07-01-native-host-command-and-state-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-command-and-state-core/evidence.md
superseded_by:
merged_from:
deprecated_reason:
---

# native-dsp

## 职责

承载当前 C++ native PCM analyzer core：K-weighting、momentary/short-term/integrated LUFS、true peak、RMS、correlation、stereo image、waveform、channel waveform、spectrum、channel spectrum 和 performance mode。由 C# `NativePcmAnalyzer` 通过 `DllImport("DesktopLoudnessMeter.Native")` 调用，C ABI 使用 opaque handle 和 caller-provided buffer copy。Windows 已验证路径仍是 MSVC x64 DLL；`CMakeLists.txt` / `CMakePresets.json` 记录 future cross-platform C++ Core build boundary，但 macOS configure/build 仍由 `macos-readiness` 验证。

## 关键文件
| 文件 | 角色 |
|------|------|
| `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` | native DLL C ABI export / forwarding；导出 FFT、true-peak legacy entry 和 `DlmPcmAnalyzer*Export` |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.h` | stable C ABI snapshot/settings/copy declarations；定义 `DLM_NATIVE_EXPORT` / `DLM_NATIVE_CALL` 以保留 Windows ABI 并预留非 Windows symbol visibility |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzerInternal.h` | internal analyzer state, constants, helper declarations |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp` | analyzer lifecycle, process orchestration, snapshot mapping, settings, buffer copy |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Loudness.cpp` | K-weighting, sliding windows, integrated loudness gate, LUFS/RMS calculations |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.TruePeak.cpp` | true-peak kernel and oversampled ring input |
| `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.RecentWindow.cpp` | recent window, waveform, spectrum, stereo image, performance refresh policy |
| `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` | MSVC x64 DLL 项目；C++17；AVX2；输出到 `x64/<Configuration>` |
| `src/DesktopLoudnessMeter.Native/CMakeLists.txt` | reserved cross-platform native core build entry；source set mirrors `.vcxproj` analyzer/RfRapid files |
| `src/DesktopLoudnessMeter.Native/CMakePresets.json` | reserved Windows/macOS configure presets；当前 Windows session 未验证 CMake because `cmake` is unavailable |
| `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` | copies the native DLL into managed app/test/benchmark output directories when imported |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfRapid.h` | RfRapid API 声明 |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfRapid.cpp` | RfRapid FFT/vector 实现 |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfCPUID.*` | CPU feature detection |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativePcmAnalyzer.cs` | C# P/Invoke owner for native analyzer handle and managed array copies |

## 隐式约束
- Windows verified build/deployment path remains `DesktopLoudnessMeter.Native.vcxproj` + `DesktopLoudnessMeter.Native.targets`; CMake is a reserved boundary for future `macos-readiness`, not a completed macOS support claim.
- `DesktopLoudnessMeterNative.cpp` 应保持 ABI export / forwarding，算法细节留在 `DlmPcmAnalyzer.*` 子模块。
- `DlmPcmAnalyzer` ABI 不暴露 native-owned 临时集合；C# 必须通过 copy functions 获取 waveform/spectrum/stereo-image arrays。
- `DlmRfRapidTruePeakProcessF64` 需要 caller 提供完整输出缓冲；C# 当前按 `frameCount * oversampling` 预分配。
- native true-peak 算法与 managed `TruePeakEstimator` 同构；可作为迁移参照和等价性基准。
- consuming managed executables/tests/benchmarks must import `DesktopLoudnessMeter.Native.targets`; otherwise runtime `DllNotFoundException` can appear even when the app project copies the DLL.
- Native build boundary verification should include: MSBuild Release x64 build, `dumpbin /exports` or equivalent export check, `NativePcmAnalyzer` smoke tests, full managed tests, and benchmark/load smoke.

## 2026-06-29 native source ingest update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h` | Local/plugin source-ingest core C ABI declarations and flat source snapshot DTO |
| `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp` | Native source registry, lifecycle state machine, per-source `DlmPcmAnalyzer` ownership, timeout cleanup, settings, and copy-buffer snapshot projection |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs` | C# P/Invoke owner for native source-ingest handle and managed source snapshot copies |

## 2026-06-30 native protocol ingest update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.h` | Protocol v1 receiver-side C ABI declarations for decode, copy and ack encoding |
| `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp` | Native JSON field parser, Protocol v1 validation, little-endian float32 base64 decode, and ack JSON generation |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs` | Managed P/Invoke wrapper that maps native protocol packets into existing `LocalLoopbackPacket` DTOs |

Additional constraints:
- `DlmLoopbackProtocol` owns protocol bytes and ack payload shape, not UDP socket transport.
- Host code may still filter loopback remotes before native decode because remote address validation is transport glue.
- Keep `DlmLoopbackProtocol.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale native DLL exports will surface as managed `DllNotFoundException` / `EntryPointNotFoundException` style failures.

Additional constraints:
- `DlmSourceIngestCore` is platform-neutral core state, not a capture adapter: it owns local/plugin loopback source lifecycle after packets have been decoded by native `DlmLoopbackProtocol` and mapped back to managed packet DTOs.
- Managed `NativeSourceIngestCore` must keep using caller-owned buffers and UTF-8 string marshalling.
- HostBridge/WebView DTOs should continue to consume C# `SignalSourceSnapshot`, not native DTOs directly.

## 2026-06-30 native host source-list projection update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSourceProjection.h` | Native C ABI declarations for source-list projection metadata and string copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostSourceProjection.cpp` | Native filtering, ordering, duplicate title disambiguation, source identity text, workspace defaults, and selected-source visibility projection |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceProjection.cs` | Managed P/Invoke wrapper returning projection rows for Host source snapshots |

Additional constraints:
- `DlmHostSourceProjection` is generic over source metadata and workspace row state so it covers WASAPI, local loopback, and plugin rows after service aggregation.
- It projects source-list metadata only; metrics arrays remain pass-through from `SignalSourceSnapshot`, and Host still owns recent events, detail/collision snapshots, settings persistence, and command mutation.
- Keep `DlmHostSourceProjection.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed P/Invoke failures in host projection tests.

## 2026-07-01 native host recent events update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostRecentEvents.h` | Native C ABI declarations for Host/Web source appeared recent-event lifecycle and row copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostRecentEvents.cpp` | Native previous-live/current-live state machine, visible title lookup, newest-first recent history, and 1..200 limit trimming |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRecentEvents.cs` | Managed P/Invoke wrapper that returns `HostRecentEventSnapshot` rows for Host snapshots |

Additional constraints:
- `DlmHostRecentEvents` uses unfiltered live source keys for lifecycle truth and visible projected rows only for display titles. Hidden-live then visible is not a new appearance.
- Host supplies formatted `HH:mm:ss` time text and max recent-event limit; native does not read system time or settings.
- WPF local recent events are not migrated by this core. `MainViewModel` still owns WPF-specific source disappeared, true peak, loudness, collision, and filter workflows.
- Keep `DlmHostRecentEvents.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed P/Invoke failures in native recent-event tests.

## 2026-07-01 native host detail module projection update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostDetailModuleProjection.h` | Native C ABI declarations for selected-detail module projection rows and title copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostDetailModuleProjection.cpp` | Native detail module row projection: default order, stable equal-order handling, duplicate/invalid filtering, titles, visibility defaults, and height normalization |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostDetailModuleProjection.cs` | Managed P/Invoke wrapper returning rows consumed by `HostDetailSnapshot.Modules` |

Additional constraints:
- `DlmHostDetailModuleProjection` projects module rows only. Selected source lookup, detail shell assembly, frequency collision bands, settings projection, command mutation, and workspace persistence remain managed Host responsibilities.
- The native sort must preserve input order for equal `OrderIndex` values to match the previous managed `OrderBy` behavior.
- Keep `DlmHostDetailModuleProjection.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native detail module projection tests.

## 2026-07-01 native host snapshot chrome projection update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSnapshotChromeProjection.h` | Native C ABI declarations for Host settings snapshot, per-source settings rows, command availability, and string copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostSnapshotChromeProjection.cpp` | Native settings/command projection core with explicit nullable presence flags and ordinal source-key row ordering |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSnapshotChromeProjection.cs` | Managed P/Invoke wrapper returning `HostSettingsSnapshot` and `HostCommandAvailability` for `DesktopMeterHostController.BuildSnapshot` |

Additional constraints:
- `DlmHostSnapshotChromeProjection` projects deterministic snapshot chrome only: global settings fields, ordered source settings rows, nullable field preservation, and selected-source command availability.
- Workspace schema normalization/persistence and command mutation remain managed Host/settings responsibilities; native projection must not migrate or persist workspace state.
- Keep `DlmHostSnapshotChromeProjection.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed P/Invoke failures in native chrome projection tests.

## 2026-07-01 native host collision projection update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostCollisionProjection.h` | Native C ABI declarations for focus/comparison spectrum collision projection and band rows |
| `src/DesktopLoudnessMeter.Native/DlmHostCollisionProjection.cpp` | Native selected-detail collision band projection: floor/prominence/relative-threshold analysis, multi-comparison merge, Mel/log scale ratio mapping, and compact bands |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostCollisionProjection.cs` | Managed P/Invoke wrapper returning `FrequencyCollisionBand` rows for Host selected-detail snapshots |

Additional constraints:
- `DlmHostCollisionProjection` owns numeric band projection only. It remains a lower-level core used by `DlmHostSelectedDetailSnapshot`; selected-detail snapshot composition owns selected source lookup, comparison source key/display title resolution, settings defaulting, and comparison spectrum selection. Host still owns command mutation and workspace persistence.
- WPF `MainViewModel` local collision event workflows are not migrated by this core and may still use managed `FrequencyCollisionAnalyzer` as fallback/oracle behavior.
- Keep `DlmHostCollisionProjection.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed P/Invoke failures in native collision projection tests.

## 2026-07-01 native host source order command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSourceOrderCommand.h` | Native C ABI declarations for source-order command planning and row copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostSourceOrderCommand.cpp` | Native move up/down and move-to-index planner with clamp behavior, missing-source status, and contiguous order output |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceOrderCommandPlanner.cs` | Managed P/Invoke wrapper returning source-key/order-index rows consumed by `DesktopMeterHostController` move commands |

Additional constraints:
- `DlmHostSourceOrderCommand` plans source ordering only. Host still applies returned rows, persists `OrderIndex` through `UserWorkspaceSettings`, and rebuilds post-command snapshots.
- Missing source is a native status mapped by Host to `HostCommandErrorCode.SourceNotFound`; it should not be converted into an exception for normal command flow.
- Keep `DlmHostSourceOrderCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native source-order planner tests.

## 2026-07-01 native host source preference command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSourcePreferenceCommand.h` | Native C ABI declarations for source preference command planning and alias copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostSourcePreferenceCommand.cpp` | Native alias/hide/restore/forget planner with missing-source status, UTF-8 whitespace alias normalization, selected-source clearing flag, and forget plan output |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourcePreferenceCommandPlanner.cs` | Managed P/Invoke wrapper returning source preference plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostSourcePreferenceCommand` plans source preference commands only. Host still applies alias/hidden/forget results, persists `UserWorkspaceSettings`, clears selected detail when instructed, and rebuilds post-command snapshots.
- Alias normalization should remain compatible with previous managed `Trim` / `IsNullOrWhiteSpace` behavior for common Unicode whitespace such as `U+3000` and `U+00A0`.
- Alias/hide/restore missing source is a native status mapped by Host to `HostCommandErrorCode.SourceNotFound`; forget does not require a live source so users can clear stale workspace preference rows.
- Keep `DlmHostSourcePreferenceCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native source preference planner tests.

## 2026-07-01 native host global settings command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostGlobalSettingsCommand.h` | Native C ABI declarations for global settings command planning and string copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostGlobalSettingsCommand.cpp` | Native theme/recent-limit/event-folder/show-hidden planner with UTF-8 whitespace normalization and 1..200 recent limit clamp |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostGlobalSettingsCommandPlanner.cs` | Managed P/Invoke wrapper returning global settings plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostGlobalSettingsCommand` plans values only. Host still applies theme, recent limit and event output folder plans to `UserWorkspaceSettings`, reconciles recent events after limit changes, treats show-hidden as runtime Host state, and rebuilds post-command snapshots.
- Native global settings planning does not own WebBridge payload validation, theme whitelist/palette, workspace schema/migration/persistence, platform folder picking, or performance mode service forwarding.
- Theme and event output folder normalization should remain compatible with previous managed trim behavior for common Unicode whitespace such as `U+3000` and `U+00A0`.
- Keep `DlmHostGlobalSettingsCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native global settings planner tests.

## 2026-07-01 native host chart settings command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostChartSettingsCommand.h` | Native C ABI declarations for source/detail/chart settings command planning and collision comparison source-key copy |
| `src/DesktopLoudnessMeter.Native/DlmHostChartSettingsCommand.cpp` | Native chart settings command planner for true peak, waveform/spectrum display mode, detail visibility, spectrum range, loudness clock, and collision settings values |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostChartSettingsCommandPlanner.cs` | Managed P/Invoke wrapper returning command-specific chart settings plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostChartSettingsCommand` plans values only. Source-scoped chart/detail command target existence is guarded by `DlmHostSourceTargetCommand`; Host still applies values to `UserWorkspaceSettings`, saves workspace state, forwards true peak/waveform/spectrum analyzer settings, and rebuilds post-command snapshots.
- Native chart settings planning does not own HostBridge payload validation, numeric domain validation, workspace schema/migration/persistence, selected-detail shell assembly, comparison display-name lookup, or performance mode.
- Collision comparison source-key normalization should remain compatible with managed trim behavior for common Unicode whitespace such as `U+3000` and `U+00A0`.
- Keep `DlmHostChartSettingsCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native chart settings planner tests.

## 2026-07-01 native host runtime service command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostRuntimeServiceCommand.h` | Native C ABI declarations for reset-all, reset-source, and performance-mode runtime service command planning |
| `src/DesktopLoudnessMeter.Native/DlmHostRuntimeServiceCommand.cpp` | Native runtime service command planner: reset target flags, missing-source status, source identity echo, and Host-to-analysis performance mode mapping |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRuntimeServiceCommandPlanner.cs` | Managed P/Invoke wrapper returning runtime service command plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostRuntimeServiceCommand` plans runtime service side effects only. Host still invokes `ISignalSourceResetService` / `ISignalSourceAnalysisSettingsService`, owns typed `HostCommandResult` envelopes, and rebuilds post-command snapshots.
- Reset-source missing source is a native status mapped by Host to `HostCommandErrorCode.SourceNotFound`; it should remain a normal command flow, not an exception.
- Performance-mode planning preserves the requested Host mode value and maps only Host Eco to analysis Eco; all other Host values map to analysis Normal to preserve previous managed behavior.
- Keep `DlmHostRuntimeServiceCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native runtime service planner tests.

## 2026-07-01 native host selected-detail shell update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailShell.h` | Native C ABI declarations for selected-detail shell planning and comparison string/index copy |
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailShell.cpp` | Native selected-detail shell planner: selected source index resolution, module visibility summary, comparison source index selection, missing-key fallback, display metadata, and collision setting defaults |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailShellPlanner.cs` | Managed P/Invoke wrapper returning selected-detail shell plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostSelectedDetailShell` plans shell metadata only. It returns source indexes and normalized settings; actual frequency collision bands still flow through `DlmHostCollisionProjection`.
- Missing comparison source keys preserve the requested key, return no comparison display title, and use all non-selected sources as comparison spectra.
- The native default frequency scale must remain `CollisionFrequencyScale.Logarithmic` (`1`), not the enum zero value.
- Keep `DlmHostSelectedDetailShell.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native selected-detail shell planner tests.

## 2026-07-01 native host selected-source command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedSourceCommand.h` | Native C ABI declarations for selected-source command/state plans |
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedSourceCommand.cpp` | Native selected-source planner: select-detail missing-source status, back-to-global clear flag, refresh reconciliation clear decision, and hidden selected-source cleanup |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedSourceCommandPlanner.cs` | Managed P/Invoke wrapper returning selected-source plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostSelectedSourceCommand` plans selected-source runtime decisions only. After the native runtime state update, Host applies those plan flags through `DlmHostRuntimeState`, wraps typed command results, and rebuilds snapshots.
- Source key matching is exact ordinal identity matching. Display titles and aliases must not participate in selected-source identity.
- Missing select-detail source is a native status mapped by Host to `HostCommandErrorCode.SourceNotFound`; it should remain normal command flow, not an exception.
- Keep `DlmHostSelectedSourceCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native selected-source command planner tests.

## 2026-07-01 native host source target command update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSourceTargetCommand.h` | Native C ABI declarations for source-target command plans |
| `src/DesktopLoudnessMeter.Native/DlmHostSourceTargetCommand.cpp` | Native exact source-key target existence planner with success/source-not-found status and source-key copy |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceTargetCommandPlanner.cs` | Managed P/Invoke wrapper returning source-target plans consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostSourceTargetCommand` plans target existence only. It does not normalize chart values, mutate workspace rows, validate HostBridge payloads, or invoke analyzer services.
- Source key matching is exact ordinal identity matching. Display titles and aliases must not participate in source-target command identity.
- Missing and blank source targets are normal source-not-found plan statuses mapped by Host to `HostCommandErrorCode.SourceNotFound`; they should not become exceptions.
- Keep `DlmHostSourceTargetCommand.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native source-target planner tests.

## 2026-07-01 native host aggregate snapshot update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostAggregateSnapshot.h` | Native C ABI declarations for aggregate Host snapshot planning |
| `src/DesktopLoudnessMeter.Native/DlmHostAggregateSnapshot.cpp` | Native aggregate planner for selected source index, selected detail inclusion, and normal source-count status text |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostAggregateSnapshotPlanner.cs` | Managed P/Invoke wrapper returning aggregate snapshot plans consumed by `DesktopMeterHostController.BuildSnapshot` |

Additional constraints:
- `DlmHostAggregateSnapshot` plans aggregate snapshot metadata only. Host still assembles `HostSnapshot` DTOs, builds selected detail snapshots, applies managed warning/failure/initial-wait status overrides, owns workspace persistence, invokes services, and orchestrates other native projections.
- Normal source-count status text is native-backed and must preserve existing wording: `No active audio sources`, `1 source tracked`, and `<count> sources tracked`.
- Keep `DlmHostAggregateSnapshot.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native aggregate snapshot planner tests.

## 2026-07-01 native host runtime state update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostRuntimeState.h` | Native C ABI declarations for selected source, show-hidden, and Host performance runtime state |
| `src/DesktopLoudnessMeter.Native/DlmHostRuntimeState.cpp` | Native runtime state handle with caller-buffer selected source copy and planner-result application helpers |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRuntimeState.cs` | Managed P/Invoke owner consumed by `DesktopMeterHostController` |

Additional constraints:
- `DlmHostRuntimeState` owns runtime storage only. It does not persist workspace settings, invoke capture/analyzer services, validate HostBridge payloads, or assemble DTOs.
- `DesktopMeterHostController` still owns workspace persistence, service invocation, DTO assembly, and native-wrapper orchestration, so Host is still not glue-only.
- Keep `DlmHostRuntimeState.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native runtime state tests.

## 2026-07-01 native host selected-detail snapshot update

Additional key files:

| File | Role |
|------|------|
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailSnapshot.h` | Native C ABI declarations for selected-detail snapshot planning, module rows, collision bands, and string copy functions |
| `src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailSnapshot.cpp` | Native selected-detail snapshot core that composes detail module projection, selected-detail shell metadata/defaults, comparison spectrum selection, and collision band projection |
| `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailSnapshotPlanner.cs` | Managed P/Invoke wrapper returning selected-detail module rows, visibility flags, comparison metadata/settings, and collision bands |

Additional constraints:
- `DlmHostSelectedDetailSnapshot` owns selected-detail snapshot planning only. Host still supplies source snapshots/workspace settings, maps returned plans to `HostDetailSnapshot`, persists workspace settings, validates HostBridge payloads, and invokes services.
- `DlmHostDetailModuleProjection`, `DlmHostSelectedDetailShell`, and `DlmHostCollisionProjection` remain valid lower-level native cores and regression or composition boundaries; `DesktopMeterHostController` should consume the unified `NativeHostSelectedDetailSnapshotPlanner` for selected-detail snapshot assembly.
- Keep `DlmHostSelectedDetailSnapshot.cpp` listed in both `.vcxproj` and `CMakeLists.txt`; stale exports surface as managed `EntryPointNotFoundException` in native selected-detail snapshot planner tests.
