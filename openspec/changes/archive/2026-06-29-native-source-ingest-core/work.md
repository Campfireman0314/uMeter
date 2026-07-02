# Work Packet: 2026-06-29-native-source-ingest-core

正文默认使用中文；路径、命令、配置键、frontmatter key、状态枚举和代码标识保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断
- 结论：KEEP_ONE_CHANGE
- 理由：本 change 聚焦一个工程闭环：把 local/plugin loopback source registry、register/heartbeat/rename/audio/close 状态机、same-handle reconfiguration、per-source analyzer ownership 和 source snapshot projection 的平台无关部分迁入 `DesktopLoudnessMeter.Native`，并由 C# adapter 继续暴露既有 `LocalLoopbackSignalSourceService` 行为。它触及 native ABI、C# wrapper、服务生命周期和回归测试，低于“全 Core 迁移”，高于零碎重构，适合单个 strict-change。
- OpenSpec 下限检查：通过。该 change 可独立验证为“local/plugin loopback source ingest 由 C++ Core 承担且现有 host/UI snapshot 行为保持兼容”。

### 能力簇
| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| Native source ingest core | 为 WebView UI + C++ Core 架构移走一段平台无关 source 状态机，减少 Host 业务职责 | `src/DesktopLoudnessMeter.Native`, `src/DesktopLoudnessMeter.App/Audio` | 高 | local/plugin loopback only |
| Managed adapter parity | 让现有 Host/WPF/WebView contract 继续消费相同 `SignalSourceSnapshot` | `LocalLoopbackSignalSourceService`, tests | 高 | UI-neutral contract 不变 |
| Lifecycle regression evidence | 防止 source identity、timeout、close、settings 和 reconfiguration 退化 | `tests/DesktopLoudnessMeter.Tests/Audio` | 高 | 覆盖已知 pitfalls |

### 本轮不拆分
- 不把 Windows WASAPI/process loopback capture 纳入本 change：它是平台 adapter/glue，后续按 capture adapter 或 macOS readiness 处理。
- 不把 UDP binary protocol、JUCE sender lifecycle 或 protocol vocabulary 纳入本 change：它们属于 `juce-sender-protocol-boundary`。
- 不把 WebView 图表、HostBridge transport、tray/startup/installer 纳入本 change：它们已有后续 change。

## 意图

当前 uMeter 已完成 WebView runtime replan、Windows WebView shell、native core build boundary 和 versioned settings/workspace schema。下一步需要把 C# Host 中平台无关的 source ingest 状态机逐步迁往 C++ Core，避免 Host 长期承担 DSP/source lifecycle 业务逻辑。

已证实事实：
- `.aiknowledge/codemap/ui-runtime-architecture.md` 说明旧 `cross-platform-architecture-plan` 中 Flutter 路线已被 `ui-runtime-architecture-replan` 废弃，后续 UI runtime 是 WebView2/WKWebView + TypeScript/React/Vite。
- `.aiknowledge/codemap/signal-source-ingest.md` 说明 `LocalLoopbackSignalSourceService` 当前拥有 local/plugin loopback source registry、heartbeat/close、timeout、audio ingest、analyzer settings 和 snapshot creation。
- `.aiknowledge/codemap/native-dsp.md` 说明 `DesktopLoudnessMeter.Native` 已有 Windows-verified DLL boundary、CMake reserved boundary、opaque analyzer handle 和 buffer-copy C ABI。
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` 已覆盖 duplicate display name identity、audio metrics/settings、same-handle format reconfiguration、heartbeat/timeout/close/reregister。

推断：
- 把 local/plugin loopback source lifecycle 迁到 native core 后，WebView UI 仍应主要消费既有 UI-neutral host snapshot/command/result contract；本 change 不应改变 HostBridge v1 payload 语义。
- Native source ingest 可以复用现有 `DlmPcmAnalyzer` 作为 per-source analyzer owner，C# adapter 负责 DTO/string mapping 与 .NET collection projection。

## 范围

### 范围内
- 在 `DesktopLoudnessMeter.Native` 中新增 opaque native source-ingest core handle，管理 local/plugin loopback source registry。
- Native core 支持 register、heartbeat、rename、audio、close、timeout cleanup、reset source、reset all sources、true-peak oversampling、channel waveform/spectrum switches、performance mode。
- Native core 使用现有 `DlmPcmAnalyzer` 为每个 source 处理 PCM 并生成 flat snapshot/copy buffers。
- C ABI 暴露稳定 DTO 与 copy 函数，遵循现有 native export/calling convention 和 buffer-copy pattern。
- C# wrapper/adapter 将 native snapshot 映射回既有 `SignalSourceSnapshot`，保持 `LocalLoopbackSignalSourceService` public surface 和 host contract 行为。
- 回归测试覆盖 source identity、duplicate names、same-handle reconfiguration、settings persistence across reconfigure、timeout/close/reregister、invalid packet ignore 和 metrics array copy。
- 更新 native MSBuild/CMake file lists，并补充 codemap/evidence。

### 范围外
- 不迁移 WASAPI/process loopback capture worker 或 Windows capture adapter。
- 不改变 `LocalLoopbackProtocol` wire format、UDP receiver、JUCE sender 或 SenderKit。
- 不改变 HostBridge v1 transport、typed command bus 或 JSON DTO payload。
- 不改变 WebView shell UI、WPF UI、图表渲染、tray/startup/installer。
- 不承诺 macOS runner/capture 可运行；只保持 native boundary 不阻断后续 macOS readiness。

## 成功标准

- `LocalLoopbackSignalSourceService` 对外行为与现有 tests 等价，但平台无关 source registry/lifecycle/snapshot projection 由 native source-ingest core 承担。
- Same-handle register with changed format preserves source key and settings while rebuilding analyzer state.
- Close and timeout remove source state and dispose native analyzers; reregister with same handle returns stable `loopback:<handle>` key.
- Invalid audio packets are ignored without corrupting source state.
- Native MSBuild verified path can build the new source files; CMake boundary includes the same native files for later cross-platform use.
- Host/WebView/WPF snapshot contract does not gain WPF-specific or transport-specific fields.

## 需求 / 验收摘要

- [R1] Native core must own local/plugin loopback source lifecycle for register, heartbeat, rename, audio, close and timeout.
- [R2] Source identity and same-handle reconfiguration must remain compatible with current host/UI behavior.
- [R3] Native snapshots must project existing audio metrics and graph buffers into `SignalSourceSnapshot` without changing host contract semantics.
- [R4] Managed adapter must preserve existing service interfaces, settings commands, reset commands and performance-mode controls.
- [R5] Build/test/docs evidence must cover Windows verified native path, CMake file inclusion, focused regression tests and full app/web sanity.

## 方案摘要

新增 `DlmSourceIngestCore` native type，内部以 handle 字符串为 key 保存 source state。每个 source state 保存 display name、source kind、sample rate、channels、last seen timestamp、peak、settings 和一个 `DlmPcmAnalyzer` handle/object。C ABI 使用 opaque core handle、integer result codes、UTF-16/UTF-8-safe string copy strategy 或 length-bounded char buffer strategy，并采用现有 copy-buffer 风格导出 metric arrays。

C# 新增 internal native wrapper，把 .NET packet DTO 映射为 native calls；`LocalLoopbackSignalSourceService` 继续实现 `ISignalSourceMeterService` / reset / analysis settings interfaces，锁、clock 和 timeout policy 保持在 service boundary 或明确委托给 native core。UDP receiver 和 protocol parser 保持 C#。

## 文件 / Ownership

### 允许修改
- `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.*` - native source registry/state machine implementation。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - C ABI export forwarding。
- `src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.*` - only if needed to share analyzer ownership/copy helpers without changing existing analyzer behavior。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` - include new native files for Windows verified build。
- `src/DesktopLoudnessMeter.Native/CMakeLists.txt` - include new native files for reserved cross-platform build boundary。
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs` - managed adapter to native source ingest while preserving public behavior。
- `src/DesktopLoudnessMeter.App/Audio/Analysis/Native*.cs` or new `NativeSourceIngestCore.cs` - P/Invoke wrapper and DTO mapping。
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - parity/regression coverage。
- `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/*` - native smoke/parity tests if wrapper coverage belongs there。
- `.aiknowledge/codemap/signal-source-ingest.md`, `.aiknowledge/codemap/native-dsp.md`, `.aiknowledge/codemap/chains/pcm-metric-flow.md` - update after implementation/verify if behavior changes land.
- `openspec/changes/2026-06-29-native-source-ingest-core/**` - this change artifacts and evidence.

### 禁止修改
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**` except docs-only references if readiness reveals a required clarification; implementation changes are out of scope.
- `src/DesktopLoudnessMeter.Web/**`
- `src/DesktopLoudnessMeter.App/WebView/**`
- Installer/startup/tray files unless verify reveals a build-only dependency issue; any behavior change is out of scope.

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-native-source-ingest-core`
- Base Branch: `codex/2026-06-29-settings-workspace-schema` at `568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`
- Merge Target: `codex/2026-06-29-settings-workspace-schema`
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树干净；创建后 dirty 仅允许包含 `openspec/changes/2026-06-29-native-source-ingest-core/**`，后续实现 dirty 必须属于本 change 允许修改范围。
  - 链式判断：本 change 基于上一已归档 change `2026-06-29-settings-workspace-schema` 的分支 HEAD，属于链式推进；base_head=`568842a2a8a51be4c4ed8936d1f5f9416f9df2e0`。
  - isolated 触发：本 change 触及 native ABI、managed adapter 和 shared service behavior，理论上具备 isolated trigger；但当前工作树干净、单 agent 串行推进、用户要求持续在当前 goal 中推进，且已创建专用 branch，因此采用 `current-worktree`。若出现无关 dirty、并行 worker 或需要长时间隔离验证，回到 plan 改为 isolated worktree。
  - 结论：`current-worktree` on dedicated branch。

## 验证计划

- `opsx changes list`：确认本 change 为唯一活动 change。
- `opsx changes resolve 2026-06-29-native-source-ingest-core`：确认 resolved root。
- Focused tests: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`，预期通过。
- Native smoke/parity tests: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~Native`，预期通过或记录平台限制。
- Full .NET tests: `dotnet test uMeter.sln`，预期通过。
- Web tests/build sanity: `npm --prefix src/DesktopLoudnessMeter.Web test -- --run` and `npm --prefix src/DesktopLoudnessMeter.Web run build`，预期通过。
- App build: `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`，预期通过。
- Native build: use existing verified Windows native build command from repo docs/scripts; if tooling unavailable, record exact blocker and rely on managed native load tests only after explaining coverage gap.

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：no
- 不确定性来源：macOS native build/capture readiness not verified in this change; CMake remains reserved boundary. Windows native build tools may be unavailable in the current environment.
- 人工 / 平台覆盖：不需要人工 UI 验证；若 native build tooling unavailable，evidence 必须明确记录 unavailable command and fallback coverage。
- 最终硬化策略：focused lifecycle tests + native wrapper tests + full .NET tests + app/web build sanity + codemap updates。

## 回退 / 升级

- 回退方式：revert this change branch diff; no persisted user data migration is introduced.
- 升级条件：如果 readiness 或 implementation 发现 source ingest cannot be separated from protocol parsing or WASAPI capture without breaking behavior，停止 implementation，回到 `opsx-plan` 拆分为 native ABI spike and managed adapter change，或回到 `opsx-explore` 补证据。

## Bugfix Root Cause

none (feature)

## Rejected Attempts

- 不采用“直接迁移所有 source/capture”：WASAPI/process loopback 是平台 adapter，和 local/plugin packet lifecycle 的验证边界不同。
- 不采用“只新增 native shell 不接入 service”：无法形成用户可验证的行为闭环，也不能证明 Host 已卸下 source ingest 状态机。
- 不采用“重写 protocol/JUCE sender 同时迁 native core”：会把 lifecycle vocabulary、wire format、sender restart semantics 和 core state machine 混成过大 change。
- 不采用“让 WebView 直接消费 native source snapshots”：违反 HostBridge v1 typed command bus 和 UI-neutral host contract 约束。

## Readiness Notes

- readiness 必须确认旧 Flutter wording 已被 WebView replan superseded，不能把 Flutter shell 当成本 change 的依赖。
- readiness 必须确认 R1-R5 全部能映射到 U1-U5 和 tasks。
- readiness 必须确认 native ABI/file list/C# wrapper/testing scope 足够实现，但未包含 WASAPI/JUCE/WebView/packaging。
- readiness 必须确认 current-worktree 策略在当前 clean dedicated branch 上可接受。
