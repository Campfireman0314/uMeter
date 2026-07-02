# Design

## Overview

新增 `DlmLoopbackProtocol` native module，位于 `DesktopLoudnessMeter.Native`，作为 Protocol v1 receiver-side ingest boundary。它不持有 source registry，也不接触 UDP socket；它只处理 raw protocol bytes 和 ack bytes。

目标数据流：

```text
UDP socket (Host glue)
  -> loopback remote filter (Host glue)
  -> NativeLoopbackProtocol.TryDecode(raw bytes)
  -> LocalLoopbackPacket (managed DTO)
  -> LocalLoopbackSignalSourceService.Ingest
  -> DlmSourceIngestCore typed lifecycle/audio ingest

Ack:
LocalLoopbackUdpReceiver.SendAckAsync(handle)
  -> NativeLoopbackProtocol.EncodeAck(handle)
  -> UDP socket send (Host glue)
```

## Key Decisions

- 新建 native protocol 单元，而不是扩展 `DlmSourceIngestCore`。`DlmSourceIngestCore` 继续只负责 typed source lifecycle、analyzer ownership 和 source snapshots。
- 不引入第三方 JSON 库。Protocol v1 receiver 子集较小，可实现 bounded parser：读取顶层 object 的 string/number fields，忽略未知字段，严格校验已知字段。
- C# `DesktopLoudnessMeter.Protocol.LocalLoopbackProtocol` 继续作为 sender-side encode API 和 wire-shape oracle。receiver route 不再直接使用它 decode/ack。
- Native ABI 使用 caller-owned buffers 和 explicit counts，延续现有 native copy pattern，避免 native-owned memory 泄露给 managed caller。

## Implementation Units

### [U1] Native protocol ABI and parser

需求覆盖：R1, R2, R3, R5

模块边界：
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.h`
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp`
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp`
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj`
- `src/DesktopLoudnessMeter.Native/CMakeLists.txt`

设计：
- 定义 flat DTO：
  - `packetType`: register/heartbeat/rename/audio/close/ack 的整数枚举。
  - `sourceKind`: loopback/plugin 的整数枚举，未知或缺失默认 loopback。
  - `sampleRate`, `channels`, `sequence`。
  - `handleUtf8ByteCount`, `displayNameUtf8ByteCount`, `sampleCount`。
- API：
  - decode raw UTF-8 JSON bytes 到 native packet handle / parse result。
  - copy handle/displayName/sample arrays 到 caller-owned buffers。
  - destroy parse result。
  - encode ack(handle) 到 caller-owned byte buffer，并返回 actual byte count。
- 错误模型：
  - invalid input 返回 “not decoded” 状态，managed wrapper 映射为 `false`。
  - API misuse / null output 返回 failure code，managed wrapper 抛出异常，防止静默损坏。

### [U2] Managed native wrapper

需求覆盖：R1, R2, R3, R4

模块边界：
- `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs`
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`

设计：
- `NativeLoopbackProtocol.TryDecode(ReadOnlySpan<byte>, out LocalLoopbackPacket)` 负责 P/Invoke、copy strings/samples，并映射到现有 `LocalLoopbackPacket`。
- `NativeLoopbackProtocol.EncodeAck(string handle)` 返回 `byte[]`。
- wrapper 是 Host glue 与 native protocol core 的 managed adapter，不向 WebView/HostBridge 暴露 native DTO。

### [U3] Receiver route migration

需求覆盖：R2, R4

模块边界：
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`

设计：
- `RouteReceivedPacket` 保留 remote loopback filter。
- loopback bytes 交给 `NativeLoopbackProtocol.TryDecode`。
- inbound ack decode 成功后仍不 ingest、不 ack。
- valid non-ack decode 成功后先 ingest，再返回 ack handle，保持现有生产顺序。

### [U4] Contract and regression tests

需求覆盖：R1, R2, R3, R5

模块边界：
- `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`

设计：
- 新增 native protocol focused tests，覆盖 register/audio/simple/rename/ack decode、invalid rejection、ack encoding wire shape。
- 保留 C# `LocalLoopbackProtocolTests` 作为 sender-side encode contract。
- receiver tests 继续覆盖 inbound ack、valid non-ack、non-loopback remote，并增加 invalid payload no-ack case。

### [U5] Knowledge and evidence

需求覆盖：R5

模块边界：
- `.aiknowledge/codemap/signal-source-ingest.md`
- `.aiknowledge/codemap/native-dsp.md`
- `.aiknowledge/codemap/juce-sender.md`
- `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- `.aiknowledge/logs/2026-06.md`
- `openspec/changes/2026-06-30-native-protocol-ingest-core/evidence.md`

设计：
- 记录新的边界：Host UDP socket glue -> native protocol decode/ack -> managed DTO -> native source ingest typed events。
- 不宣称 JUCE binary tests pass，除非本机 runner 实际可用并有命令输出。

## Risks And Handling

- C++ JSON parser drift：用 focused tests 对齐现有 Protocol v1 wire shape；未知字段忽略，已知字段严格校验。
- Base64 / float endianness drift：测试固定 `0.25f, -0.5f` 的现有 base64 `AACAPgAAAL8=`。
- Native DLL stale export：实现后必须先跑 `build-native.ps1 -Configuration Release`，再跑 managed DllImport tests。
- Over-migration：UDP socket、remote filter、source-key mapping、HostSnapshot projection 明确 out of scope。
- Tooling limit：若 CMake/JUCE runner 不可用，只记录限制，不宣称 binary pass。

## Rollback

若 native protocol decode 出现不可接受风险，回滚 receiver 到 C# `LocalLoopbackProtocol.TryDecode` / `EncodeAck`，保留新增 tests 作为 characterization 依据，再拆出 parser spike change。
