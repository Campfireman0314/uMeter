# Spec: juce-sender-protocol-boundary

## 新增需求

### 需求: v1 lifecycle vocabulary has one authoritative contract
**Trace**: R1
**Slice**: protocol/vocabulary
Protocol v1 必须有集中、可测试的 lifecycle vocabulary，覆盖 protocol version、packet type、field name、source kind、handle constraints、sample constraints、ack semantics 和 audio payload encoding。

#### 场景: C# encoder and decoder share vocabulary constants
- **当** C# Protocol 编码或解码 register、heartbeat、rename、audio、close 或 ack packet
- **那么** 它必须使用同一组 vocabulary constants / DTO fields，而不是在多个方法中散落 magic strings

#### 场景: v1 packet shape remains wire-compatible
- **当** 现有 v1 register、heartbeat、rename、audio、close 或 ack JSON packet 被编码
- **那么** `protocol`、`type`、`handle`、`displayName`、`sourceKind`、`sampleRate`、`channels`、`sequence` 和 `samplesBase64` 的 wire field names 与既有 v1 保持兼容

#### 场景: audio samples remain little-endian float32 base64
- **当** C# 或 JUCE 编码 audio packet
- **那么** `samplesBase64` 必须表示 little-endian float32 bytes 的 base64 文本

### 需求: JUCE sender keeps DAW-side lifecycle ownership
**Trace**: R2
**Slice**: juce-sender/lifecycle
JUCE sender 必须继续作为 DAW 端 sender，负责 handle、register/heartbeat/audio/rename/close packet emission、ack polling、dropped packet accounting 和 same-handle restart packet ordering。

#### 场景: same-handle restart preserves source identity
- **当** JUCE sender 以相同 handle 和不同 sample rate 或 channel count 重新 start
- **那么** 它必须先 suppress close，再发出新的 register，并且不能在 replacement register 前发出 close

#### 场景: explicit stop still sends close
- **当** JUCE sender 显式 stop 或析构 active worker
- **那么** 它必须在 worker shutdown 路径发送 close packet，除非该 stop 是 same-handle restart 的内部步骤

#### 场景: ack matching rejects wrong protocol or handle
- **当** JUCE sender poll 到 ack packet
- **那么** 只有 protocol version、type 和 handle 全部匹配当前 sender 时才允许进入 connected state

### 需求: SenderKit remains a .NET sender companion, not host ingest core
**Trace**: R3
**Slice**: senderkit/boundary
`.NET SenderKit` 必须继续作为 sender companion，复用 Protocol v1 vocabulary，并保持 sender-side queue/backpressure/heartbeat/close 行为，不接管 host receiver 或 native ingest state。

#### 场景: SenderKit uses protocol vocabulary for outbound packets
- **当** SenderKit start、rename、enqueue audio、heartbeat 或 dispose
- **那么** 它必须通过 `DesktopLoudnessMeter.Protocol` 编码 v1 packet

#### 场景: SenderKit backpressure is non-blocking
- **当** audio packet pool 已满或 samples 超出 v1 UDP JSON transport 上限
- **那么** SenderKit 必须 drop packet 并递增 dropped count，而不是阻塞 realtime sender path

### 需求: Host receiver bridges UDP JSON to typed native ingest events
**Trace**: R4
**Slice**: host-receiver/native-seam
Windows host receiver 必须继续拥有 UDP socket 和 JSON decode/routing glue；native source ingest core 只接收 typed lifecycle/audio events，不直接接收 UDP JSON bytes。

#### 场景: receiver ignores inbound ack packets
- **当** host receiver 收到 valid v1 ack packet
- **那么** 它不得调用 source ingest service，也不得发送 ack-of-ack

#### 场景: receiver routes valid non-ack packets
- **当** host receiver 收到 valid v1 register、heartbeat、rename、audio 或 close packet from loopback remote
- **那么** 它必须将 decoded typed packet 交给 `LocalLoopbackSignalSourceService`

#### 场景: receiver rejects non-loopback remote
- **当** host receiver 收到来自非 loopback remote endpoint 的 packet
- **那么** 它必须丢弃 packet，不更新 source ingest state

#### 场景: native source ingest boundary stays typed
- **当** source ingest core 被调用
- **那么** 调用边界必须是 handle、sourceKind、displayName、sampleRate、channels、timestamp 和 interleaved float samples 等 typed values，而不是 UDP JSON payload

### 需求: Verification avoids socket-only gates
**Trace**: R5
**Slice**: verification/contract-tests
本 change 的主验证必须依赖 deterministic contract tests，而不是只依赖当前环境已 skip 的 UDP loopback socket smoke。

#### 场景: contract tests cover protocol parity
- **当** verify 阶段运行测试
- **那么** C# Protocol tests 和 JUCE protocol/sender tests 必须覆盖 v1 packet shape、ack matching、audio payload 和 same-handle restart ordering

#### 场景: skipped UDP smoke does not block deterministic coverage
- **当** 当前环境无法可靠运行 127.0.0.1 UDP smoke
- **那么** verify 必须记录原因，并用 non-socket contract/unit tests 覆盖 receiver routing 与 protocol semantics

## 修改需求

无。

## 移除需求

无。
