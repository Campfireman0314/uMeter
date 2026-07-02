# Spec: 2026-06-29-native-source-ingest-core

正文默认使用中文；路径、命令、配置键和代码标识保持原文。

## 新增需求

### 需求: Native source lifecycle core
**Trace**: R1
**Slice**: native-source-ingest/lifecycle
Native core MUST own local/plugin loopback source lifecycle for register, heartbeat, rename, audio, close and timeout.

#### 场景: Register creates a source
- **Given** a valid register event with handle, display name, source kind, sample rate and channels
- **When** the managed service ingests the event
- **Then** native source ingest stores one source keyed by handle
- **And** the managed snapshot exposes source key `loopback:<handle>`

#### 场景: Heartbeat keeps a source alive
- **Given** a registered source and a positive source timeout
- **When** heartbeat is received before the timeout expires
- **Then** the source remains visible in `GetCurrentSources()`

#### 场景: Close removes a source
- **Given** a registered source
- **When** close is received for the same handle
- **Then** the source is removed from current snapshots
- **And** its native analyzer state is disposed or no longer reachable

#### 场景: Timeout removes stale sources
- **Given** a registered source and no fresh register, heartbeat or audio within the timeout window
- **When** current sources are requested
- **Then** the stale source is removed before snapshot projection

### 需求: Source identity and reconfiguration parity
**Trace**: R2
**Slice**: native-source-ingest/identity
Source identity and same-handle reconfiguration MUST remain compatible with current host/UI behavior.

#### 场景: Duplicate display names preserve handle identity
- **Given** two register events with different handles and the same display name
- **When** snapshots are projected
- **Then** each source keeps a distinct `loopback:<handle>` key

#### 场景: Rename updates display only
- **Given** a registered source
- **When** a rename event is received for the same handle
- **Then** the source key remains unchanged
- **And** the display name changes in the next snapshot

#### 场景: Same-handle changed format reconfigures in place
- **Given** a registered source with analyzer settings applied
- **When** a register event arrives with the same handle and different sample rate or channel count
- **Then** the source key remains unchanged
- **And** analyzer state is rebuilt for the new format
- **And** per-source settings are preserved

#### 场景: Same-handle same format touches source
- **Given** a registered source
- **When** a register event arrives with the same handle and same sample rate and channel count
- **Then** the source remains the same logical source
- **And** the last-seen timestamp is refreshed

### 需求: Snapshot projection parity
**Trace**: R3
**Slice**: native-source-ingest/snapshot
Native snapshots MUST project existing audio metrics and graph buffers into `SignalSourceSnapshot` without changing host contract semantics.

#### 场景: Audio updates peak and metrics
- **Given** a registered source
- **When** valid interleaved audio is ingested
- **Then** the next snapshot contains peak, peak dB text, loudness/true-peak/RMS values when available, waveform buffers and spectrum buffers with the same semantic meanings as before

#### 场景: Channel graph switches control projected arrays
- **Given** a registered source
- **When** channel waveform or channel spectrum is enabled for that source
- **Then** the projected snapshot exposes the corresponding channel arrays when native analyzer data is available

#### 场景: Invalid audio is ignored
- **Given** a registered source
- **When** audio samples are null, empty or not divisible by the source channel count
- **Then** native ingest ignores the packet
- **And** source identity and prior settings remain intact

### 需求: Managed service adapter compatibility
**Trace**: R4
**Slice**: native-source-ingest/managed-adapter
Managed adapter MUST preserve existing service interfaces, settings commands, reset commands and performance-mode controls.

#### 场景: Service interfaces remain stable
- **Given** existing callers use `LocalLoopbackSignalSourceService`
- **When** the native source ingest core is introduced
- **Then** the service still implements `ISignalSourceMeterService`, `ISignalSourceResetService`, `ISignalSourceAnalysisSettingsService` and `IDisposable`

#### 场景: Reset source clears metrics
- **Given** a registered source with processed audio
- **When** `ResetSource` is called with its source key
- **Then** subsequent snapshots clear analyzer-derived values until new audio arrives

#### 场景: Reset all sources clears all metrics
- **Given** multiple registered sources with processed audio
- **When** `ResetAllSources` is called
- **Then** analyzer-derived values are cleared for every source

#### 场景: Performance mode updates active sources
- **Given** one or more active sources
- **When** performance mode changes
- **Then** native analyzer performance mode is updated for active sources without changing source identity

### 需求: Build, verification and documentation closure
**Trace**: R5
**Slice**: native-source-ingest/closure
Build/test/docs evidence MUST cover Windows verified native path, CMake file inclusion, focused regression tests and full app/web sanity.

#### 场景: Native file lists include source ingest core
- **Given** new native source ingest files exist
- **When** Windows MSBuild project and CMake project are inspected
- **Then** both build boundaries include the new native files

#### 场景: Focused regressions cover lifecycle invariants
- **Given** the implementation is complete
- **When** focused local loopback service tests run
- **Then** identity, reconfiguration, timeout, close, settings, reset and snapshot projection cases pass

#### 场景: Full sanity remains green
- **Given** the implementation is complete
- **When** full .NET tests, app build, web tests/build and available native build/smoke commands run
- **Then** results are recorded in `evidence.md`

#### 场景: Knowledge reflects new ownership
- **Given** verify passes
- **When** codemap updates are written
- **Then** signal-source-ingest/native-dsp/pcm-metric-flow entries describe native ownership and managed adapter boundaries

## 修改需求

无。

## 移除需求

无。
