# 2026-06-30-native-protocol-ingest-core

## Intent

把本地 loopback Protocol v1 的接收侧协议接入迁入 C++ Core：native core 负责解析 UDP payload 中的 Protocol v1 JSON、校验 lifecycle vocabulary、解码 little-endian float32 base64 audio samples，并生成 ack payload；Host 只保留 UDP socket、loopback remote 过滤、调用 Host/Source service 的 glue。

这补齐 completion audit 中的高优先缺口：`LocalLoopbackUdpReceiver` 仍在 C# 使用 `LocalLoopbackProtocol.TryDecode` 做 protocol ingest，而 native `DlmSourceIngestCore` 只接收 typed fields。

## Scope

- 新增 native protocol ingest 单元，提供 C ABI：
  - decode Protocol v1 JSON bytes -> flat packet metadata + copy-string/copy-sample buffers。
  - encode ack(handle) -> caller-owned byte buffer。
  - 复用现有 Protocol v1 vocabulary：`register` / `heartbeat` / `rename` / `audio` / `close` / `ack`，`plugin` / `loopback` / `local-loopback`。
- 新增 managed native wrapper，把 native flat DTO 映射为现有 `LocalLoopbackPacket`。
- 修改 `LocalLoopbackUdpReceiver.RouteReceivedPacket` 和 ack 发送路径，使 receiver 使用 native protocol wrapper，而不是直接调用 C# `LocalLoopbackProtocol.TryDecode` / `EncodeAck`。
- 保留 `DesktopLoudnessMeter.Protocol` 的 sender-side encode API 和 contract tests；SenderKit/JUCE sender 不改变职责。
- 更新 native MSBuild/CMake file list、focused tests、codemap/evidence。

## Non-Goals

- 不迁移 UDP socket、端口绑定、loopback IP 过滤、async receive loop 到 C++。
- 不改变 Protocol v1 wire shape 或版本号。
- 不改变 JUCE sender/SenderKit 的 packet emission 行为。
- 不改变 `DlmSourceIngestCore` 的 typed source lifecycle/state-machine ABI。
- 不迁移 HostSnapshot、selected detail、recent events、settings/workspace projection。
- 不声明 macOS runtime parity；CMake 只保持跨平台 native build 边界。

## Dependencies

- 已归档 `2026-06-29-native-source-ingest-core`：native source ingest 已接收 typed events 并拥有 source lifecycle/analyzer state。
- 已归档 `2026-06-29-juce-sender-protocol-boundary`：Protocol v1 vocabulary 和 sender/receiver typed seam 已被 characterization tests 固化。
- 已归档 `2026-06-30-completion-audit`：明确 protocol ingest remains C# host glue 是 high priority gap。
- 当前 UI runtime route 来自 `2026-06-29-ui-runtime-architecture-replan` / codemap：WebView route supersedes old Flutter wording。

## Success Criteria

- C++ Core 有独立 protocol ingest ABI，能解析 register/heartbeat/rename/audio/close/ack，拒绝 invalid protocol/type/handle/sample rate/channel count/base64/audio byte alignment。
- receiver 对有效非 ack loopback packet 的行为不变：packet 被 ingest，ack handle 与 packet handle 一致。
- receiver 对 inbound ack、invalid packet、非 loopback remote 的行为不变：不 ingest、不发送 ack。
- C# receiver route 不再直接调用 `LocalLoopbackProtocol.TryDecode` 或 `LocalLoopbackProtocol.EncodeAck`。
- Windows native Release build、focused protocol/receiver/source-service tests、full managed tests、app build、web tests/build 均有 fresh evidence；JUCE binary runner 若仍不可用，必须记录为工具限制且不宣称通过。

## Verification Plan

- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackProtocol`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackUdpReceiverTests`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
- `rg -n "LocalLoopbackProtocol\.TryDecode|LocalLoopbackProtocol\.EncodeAck" src\DesktopLoudnessMeter.App\Audio\LocalLoopbackUdpReceiver.cs` 应无命中。
- `rg -n "DlmLoopbackProtocol|NativeLoopbackProtocol" src\DesktopLoudnessMeter.Native src\DesktopLoudnessMeter.App tests .aiknowledge openspec/changes/2026-06-30-native-protocol-ingest-core`

## Rollback / Escalation

- Rollback：移除 native protocol wrapper 调用，让 receiver 回到 C# `LocalLoopbackProtocol.TryDecode` / `EncodeAck`，保留 source ingest native path 不变。
- Escalation：若 C++ JSON/base64 parser 实现出现高风险或不可稳定验证，缩小为 native protocol ABI spike，不改 receiver route，并回到 explore/plan 重新切分。
- Native build blocker：如 MSBuild/C++ toolchain 不可用，记录 exact blocker；不得把未构建 DLL 的 DllImport 测试当作通过。

## Bugfix Root Cause

这不是单点 bugfix，而是架构迁移 change。根因是此前 `juce-sender-protocol-boundary` 为降低风险保留了 typed ingest seam：C# receiver 继续做 UDP JSON decode/ack，导致 completion audit 判定 “C++ Core 承担协议接入” 未完成。

## Readiness Notes

- 已证实事实：
  - `opsx changes list` 在创建前无活动变更。
  - `LocalLoopbackUdpReceiver.RouteReceivedPacket` 当前直接调用 `LocalLoopbackProtocol.TryDecode`，`SendAckAsync` 调用 `LocalLoopbackProtocol.EncodeAck`。
  - `DlmSourceIngestCore` 当前只接收 typed handle/sourceKind/displayName/sampleRate/channel/samples。
  - native `.vcxproj` 和 `CMakeLists.txt` 均显式列出 source files，新 native 文件必须同时加入。
- 推断：
  - 用独立 `DlmLoopbackProtocol` native 单元比并入 `DlmSourceIngestCore` 更适合保持 core module 边界。
  - Host 继续保留 UDP socket/IP 过滤能满足 “Host glue” 边界，因为 socket 是平台/transport glue，不是 protocol vocabulary ingest。

## Explore Self-Check / Gap Scan

- 候选推荐：创建 `2026-06-30-native-protocol-ingest-core`。
- 第 1 轮扫描：blocking=none；non-blocking=JSON/base64 parser correctness、native DLL export freshness、real UDP smoke environment limitation、JUCE runner availability。
- 未验证前提：native C++ parser 可以在不引入第三方依赖的情况下覆盖当前 Protocol v1 JSON 子集；Windows native build toolchain 仍可用。
- 最可能错处：把 protocol ingest 过度扩大成 UDP transport 迁移，或让 native packet DTO 过早暴露给 HostBridge/Web UI。
- 失败路径 / 边界：invalid JSON、malformed base64、misaligned float bytes、case-insensitive type handling、ack packet routing、same-handle register path。
- 更小替代：只新增 native decode/ack wrapper，但 receiver route 暂不切换。该替代无法关闭 audit 中 “protocol ingest remains C# host glue” 缺口，因此不采用。
- 证据 vs 推断：代码和 codemap 支撑当前 C# decode/ack 状态；“独立 native protocol 单元” 是设计推断，会在 design/tasks 中固化并验证。
- 处理方式：把 non-blocking 风险写入 spec/design/tasks 验证项；无新的关键事实 blocker，进入 `opsx-plan`。
- 最终扫描结论：无新的关键事实性漏洞；剩余不确定性不改变 change 范围和 profile，转入 plan。

## Scope / Profile / Split Decision

- 结论：`KEEP_ONE_CHANGE`
- Profile：`strict-change`
- 理由：该 change 触及 native ABI、managed P/Invoke wrapper、Host receiver route、Protocol v1 contract tests 和 build files，是跨语言公共边界迁移；但仍是一个完整模块闭环，能力单一：receiver-side Protocol v1 ingest 进入 C++ Core。
- OpenSpec 下限检查：通过。它不是微型单点改动，而是可评审、可验证、可归档的架构缺口闭环。
- 不拆分原因：decode、ack encode、managed wrapper、receiver route 必须一起验证才能证明 C# host glue 缩减；拆开会留下无法关闭 audit gap 的半迁移状态。

## Workspace Strategy

- Workspace Strategy：`current-worktree`
- Workspace Path：`E:\gitlab\uMeter`
- Workspace Branch：`codex/2026-06-30-native-protocol-ingest-core`
- Base Branch：`codex/2026-06-30-web-settings-workflow-parity`
- Base Head：`845416989cf6c4393d86b297a0e44eaec93d6a74`
- Merge Target：deferred to user / current goal chain
- Workspace Rationale：
  - dirty 分类：创建 change 后 dirty 文件均属于本 change：`openspec/changes/2026-06-30-native-protocol-ingest-core/**`。
  - 明确无关 dirty file：无。
  - 归属不明 dirty file：无。
  - 链式判断：当前 change 基于刚归档的 `web-settings-workflow-parity` checkpoint，是 goal chain 的后续架构缺口修复。
  - isolated 触发：无多 worker、无并行 worktree 需求；虽然触及公共 ABI，但当前工作树干净且单 agent 串行推进。
  - 结论：使用 `current-worktree`，专用 branch 隔离提交。
