# Design: 2026-06-29-juce-sender-protocol-boundary

## 上下文

已证实事实：
- `DesktopLoudnessMeter.Protocol.LocalLoopbackProtocol` 当前在 C# 侧编码/解码 v1 JSON UDP packet，version 为 `desktop-loudness-meter.loopback.v1`。
- `DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.*` 当前在 JUCE 侧独立编码 v1 JSON packet，并提供 `isAckForHandle`。
- `DesktopLoudnessMeter.SenderKit.LocalLoopbackSender` 使用 C# Protocol 发 register/audio/heartbeat/rename/close，并做 queue/backpressure。
- `DesktopLoudnessMeter.App.Audio.LocalLoopbackUdpReceiver` 拥有 Windows host 的 UDP socket，过滤非 loopback remote，C# decode packet，忽略 inbound ack，对 non-ack 发送 ack，并路由到 `LocalLoopbackSignalSourceService`。
- `DlmSourceIngestCore` 当前公开的是 typed C ABI：register/heartbeat/rename/process/close 接收 handle/sourceKind/displayName/sampleRate/channels/timestamp/samples，不接收 JSON bytes。

推断：
- 本轮最小稳定迁移点是固化 v1 vocabulary 与 typed ingest seam，避免后续把 JUCE sender lifecycle、Protocol wire codec 和 native source ingest state 混在一起。
- 把 UDP JSON parser 一次迁到 native 会扩大风险，并触碰 host socket ownership、error handling、UTF-8/JSON parser 依赖和跨平台 runtime 选择；应留给后续 native protocol ingest change。

## 需求追踪

- [R1] -> [U1], [U2]
- [R2] -> [U3]
- [R3] -> [U4]
- [R4] -> [U5]
- [R5] -> [U2], [U5], [U6]

## 实施单元

### [U1] C# Protocol vocabulary boundary
- 关联需求: [R1]
- 模块边界:
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs`
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs`
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackSourceKind.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`
- 设计:
  - 引入集中 vocabulary constants 或等价内部结构，命名 packet type、field name、source kind 和 version string。
  - 保持 public API 兼容：`EncodeRegister`、`EncodeHeartbeat`、`EncodeRename`、`EncodeAudio`、`EncodeClose`、`EncodeAck`、`TryDecode` 不改变调用方签名。
  - Tests 必须先 characterization 当前 wire shape，再做重构，避免大小写、field name 或 sourceKind 字符串漂移。

### [U2] Cross-implementation protocol parity tests
- 关联需求: [R1], [R5]
- 模块边界:
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp`
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.*`
- 设计:
  - C# tests 覆盖 canonical JSON shape，尤其是 register field set、ack packet shape、invalid protocol/type/handle、audio little-endian base64。
  - JUCE tests 覆盖同一 canonical shape 与 ack matching rejection：wrong protocol、wrong type、wrong handle、invalid JSON/null data。
  - 不要求跨语言运行时互相调用；以相同 expected wire facts 做 parity contract。

### [U3] JUCE sender lifecycle guard
- 关联需求: [R2]
- 模块边界:
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h`
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp`
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp`
  - `.aiknowledge/pitfalls/lifecycle/publish-stop-mode-before-worker-exit.md`
- 设计:
  - 保留 `sendCloseOnStop` / same-handle restart 语义。
  - 如需调整 stop/start 或 ack polling，先加 characterization tests，避免 worker exit 读取旧 stop mode。
  - 不把 sender lifecycle 或 queue ownership 移入 Protocol 或 native core。

### [U4] SenderKit protocol companion boundary
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs`
  - `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs`
  - `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs`
- 设计:
  - SenderKit 继续通过 C# Protocol encoder 发 packet。
  - Backpressure/transport-size tests 继续证明 sender path 不阻塞。
  - 若新增 vocabulary constants，对 SenderKit 透明；SenderKit 不直接拼 JSON。

### [U5] Host receiver to native typed ingest seam
- 关联需求: [R4], [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs`
  - `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`
- 设计:
  - Receiver 仍是 host glue：socket ownership、loopback remote filtering、decode、ack response。
  - Native source ingest remains typed event boundary；不新增 `DlmSourceIngestCoreProcessJson` 或 equivalent。
  - 当前环境 UDP smoke 已 skip，因此 receiver routing 的新增 coverage 应优先使用 fake service/handler 或 extracted packet router，而不是强依赖 real UDP。

### [U6] Knowledge and evidence refresh
- 关联需求: [R5]
- 模块边界:
  - `.aiknowledge/codemap/juce-sender.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `openspec/changes/2026-06-29-juce-sender-protocol-boundary/evidence.md`
- 设计:
  - 记录 Protocol v1 vocabulary authority、JUCE/SenderKit sender boundary、host receiver to native typed ingest seam。
  - Verify evidence 明确区分已运行测试、skip 的 UDP smoke、工具链缺失的 JUCE test route。

## 关键决策

1. **v1 JSON shape 保持不变。**
   - 原因：JUCE plugin、SenderKit、diagnostic sender 和 host receiver 已共享 v1 JSON UDP vocabulary。
   - 影响：本 change 的重构必须先 characterization，再改内部常量。

2. **native protocol ingest boundary 本轮定义为 typed event seam。**
   - 原因：上一轮 native source ingest core 已接收 typed lifecycle/audio event；把 JSON parser 下沉会扩大到 parser dependency、socket glue 和 transport error policy。
   - 影响：`DlmSourceIngestCore` 不新增 JSON/UDP API；后续 native protocol ingest 可在新 change 中选择 parser/DTO strategy。

3. **UDP socket smoke 不是主 gate。**
   - 原因：当前 tests 已标明环境会 drop `127.0.0.1` UDP datagrams。
   - 影响：本 change 以 deterministic unit/contract tests 作为主验证，manual/UDP smoke 如仍不可运行则记录为 waived 或 follow-up，不能作为唯一证据。

4. **JUCE sender remains DAW-side.**
   - 原因：目标架构要求 JUCE sender 保持 DAW 发送端职责。
   - 影响：native core 不接管 handle creation、worker lifecycle、audio queue 或 ack polling。

## 风险 / 权衡

- 跨语言测试工具链可能不可用：Windows session 可能没有 CMake/JUCE test runner PATH。处理方式：readiness/verify 先探测工具链；若不可用，记录 blocker/waiver，并用源代码 contract review + C# tests 覆盖可验证部分。
- 抽出 receiver routing 可能改变 production code shape：限制为最小 seam，只为去 socket 化测试服务，不改变 UDP receiver public API。
- Magic string 常量化可能被误做为 wire shape change：tests 必须固定 JSON field names 和 values。
- Ack semantics 容易产生 ack-of-ack：receiver tests 必须覆盖 inbound ack ignored。

## 稳定化策略

- `[characterization-first]` 用于现有 protocol/sender behavior，先锁定 wire shape 与 lifecycle ordering。
- `[test-first]` 用于新增 receiver routing seam，因为目标行为可以用 deterministic tests 表达。
- doc impact: `TASK_EVIDENCE_ONLY`，除 codemap 外不改用户文档。
- final hardening: run focused tests first, then full managed tests/build/web checks; JUCE route 根据工具链探测记录。

## 回退方案

- 如果 C# vocabulary refactor 引入 wire drift，回退内部常量化并保留 characterization tests。
- 如果 receiver routing extraction 风险过高，保留 production receiver 逻辑，仅新增 decode/routing helper 的测试覆盖。
- 如果 JUCE tests 运行环境缺失，保留代码层 contract tests/inspection evidence，但不得宣称 JUCE binary test 已通过。

## 并行 / Subagent 边界

默认串行。该 change 的关键是跨语言 vocabulary 一致性和边界判断，拆给并行 worker 容易产生重复或互相漂移的常量定义。若后续使用 subagent，只允许只读调查 JUCE test runner/toolchain，不允许修改 artifacts 或代码。

## 知识沉淀

Archive 后执行 `opsx-knowledge mode=auto`，刷新 juce-sender / signal-source-ingest / pcm-metric-flow / same-handle-reconfiguration 的边界描述，并判断是否需要新增 protocol drift pitfall。
