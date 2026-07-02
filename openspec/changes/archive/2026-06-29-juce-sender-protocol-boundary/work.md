# Work: 2026-06-29-juce-sender-protocol-boundary

## 目标

将 JUCE sender、`.NET SenderKit`、`DesktopLoudnessMeter.Protocol`、Windows host receiver 和 native source ingest 之间的 v1 loopback 边界收敛成可测试、可演进的模块闭环。目标不是替换现有 JSON/UDP v1，而是固化 versioned lifecycle vocabulary，减少 C# 与 JUCE 双实现漂移，并明确 native core 当前接收 typed ingest event 的边界。

## 范围内

- 固化 `desktop-loudness-meter.loopback.v1` 的 packet type、field name、source kind、sample/handle constraints、ack 语义和 little-endian float32 base64 音频 payload。
- 为 C# `DesktopLoudnessMeter.Protocol` 提供集中 vocabulary / DTO boundary，使 host receiver、SenderKit、diagnostic sender 和 tests 不再散落 magic strings。
- 为 JUCE `LoopbackProtocol` 增加与 C# v1 vocabulary 对齐的 contract/characterization tests，覆盖 register/heartbeat/rename/audio/close/ack、invalid ack、handle constraints 和 audio payload encoding。
- 明确 sender lifecycle 边界：JUCE sender 继续负责 DAW 端 handle、register/heartbeat/audio/rename/close、ack polling 和 same-handle restart；Protocol 只负责 vocabulary/codec；native core 只接收 decode 后的 typed source ingest event。
- 调整或补充 host receiver / SenderKit / source ingest tests，证明 receiver 仍过滤非 loopback remote、忽略 ack、不把 UDP JSON parser 下沉到 native，并能把 valid packet 路由到 native-backed `LocalLoopbackSignalSourceService`。
- 更新 `.aiknowledge/codemap` 中与 juce-sender、signal-source-ingest、pcm-metric-flow 或 protocol 边界相关的条目。

## 范围外

- 不实现 binary protocol、compression、remote transport、WebView HostBridge transport 或网络发现。
- 不把 JUCE DAW sender lifecycle 迁移到 native core。
- 不把 Windows UDP socket ownership 或 capture adapter 迁移到 native core。
- 不改变 public UDP port、wire protocol version string 或 v1 JSON shape 的向后兼容语义。
- 不交付 AAX/AU sender、macOS runner、WKWebView 或 packaging。
- 不迁移 WebView charts、settings editor、tray/startup/single-instance 行为。

## 依赖

- 已归档 `2026-06-29-ui-runtime-architecture-replan`：当前主 UI runtime 路线为 WebView，不再沿用旧 Flutter route。
- 已归档 `2026-06-29-native-source-ingest-core`：`LocalLoopbackSignalSourceService` 已是 native-backed source ingest adapter，native core 接收 typed register/heartbeat/rename/audio/close event。
- 当前 codemap：
  - `.aiknowledge/codemap/juce-sender.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- Pitfall：
  - `.aiknowledge/pitfalls/lifecycle/publish-stop-mode-before-worker-exit.md`

## 完成标准

- C# Protocol 和 JUCE protocol tests 都证明 v1 lifecycle vocabulary、field names、sourceKind、sample encoding 和 ack matching 一致。
- SenderKit 和 JUCE sender 仍保留 sender-side responsibilities；same-handle restart 不发 close 的语义继续有 regression coverage。
- Host receiver 仍只接收 loopback remote，忽略 inbound ack，并将 valid non-ack packets route 到 native-backed source ingest adapter。
- Native source ingest 不暴露 UDP/JSON parser ownership；native boundary 在文档和 tests 中被证明是 typed event boundary。
- Windows 可运行主路径测试仍通过：native build、managed tests、JUCE protocol/sender tests（在可用工具链下）、Web tests/build 无回归。
- `.aiknowledge/codemap` 更新到归档后事实，不保留与本 change 冲突的旧描述。

## 验证计划

- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackProtocolTests`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSenderTests`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- JUCE tests via the existing CMake/JUCE test route if toolchain is available; if unavailable, record toolchain blocker and cover JUCE protocol files by source-level contract tests / documented manual follow-up.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
- `npm --prefix src\DesktopLoudnessMeter.Web run build`

## 回退策略

- Protocol vocabulary additions are additive internal refactors and can be reverted by restoring previous encoder/decoder constants if contract tests fail.
- JUCE codec changes must preserve existing packet JSON shape; if parity tests reveal drift, keep the old emitted wire shape and move only tests/docs into the change.
- Receiver/source-ingest routing changes must not alter socket ownership. If regressions appear, restore current `LocalLoopbackUdpReceiver` routing and retain only vocabulary/contract coverage.

## Bugfix Root Cause

本 change 是 feature/boundary migration，不是单点 bugfix。当前风险是跨实现 protocol vocabulary 漂移：C# Protocol、JUCE sender 和 SenderKit 都能发 v1 packet，但 vocabulary/shape 的权威来源分散，后续将 protocol ingest 进一步靠近 C++ Core 时容易把 sender lifecycle、wire codec 和 source ingest state 混在一起。

## Readiness Notes

- 已证实事实：
  - `DesktopLoudnessMeter.Protocol.LocalLoopbackProtocol` owns C# v1 JSON encode/decode and packet validation.
  - `DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.*` owns JUCE v1 JSON packet emission and ack matching.
  - `LocalLoopbackUdpReceiver` decodes UDP bytes in C# and routes non-ack packets to `LocalLoopbackSignalSourceService`.
  - `DlmSourceIngestCore` accepts typed handle/source kind/audio samples, not UDP JSON bytes.
- 推断：
  - 本轮最稳妥的 native protocol ingest boundary 是“typed event seam”，不是把 UDP JSON parser 一次迁入 native。
  - 后续如果需要 binary transport，应在新 change 中作为 v2 或 alternate transport，不应破坏 v1 JSON lifecycle vocabulary。
- 探索漏洞扫描结论：
  - blocking：无新的事实性阻塞。
  - non-blocking：旧架构规划包含 Flutter 历史文本，但 `ui-runtime-architecture-replan` 已修正主路线；UDP loopback 环境测试在当前环境有 skip，需要本 change 以非 socket contract tests 作为主 gate。

## Workspace Strategy

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-29-juce-sender-protocol-boundary`
- Base Branch: `codex/2026-06-29-native-source-ingest-core`
- Merge Target: 待用户后续决定；本 change 链接在 `f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4` 之后。
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树干净；创建后 dirty 仅允许包含 `openspec/changes/2026-06-29-juce-sender-protocol-boundary/**`，后续实现 dirty 必须属于本 change 范围。
  - 链式判断：这是链式 change，base_branch=`codex/2026-06-29-native-source-ingest-core`，base_head=`f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4`，因为本 change 消费上一轮 native source ingest 边界。
  - isolated 触发：无无关 dirty；单 agent 串行；不需要并行验证或隔离依赖安装；虽然触及公共协议边界，但当前分支已专用且工作树干净。
  - 结论：使用 `current-worktree`，path-limited staging，archive 后形成 change-level checkpoint。

## 切分结论

### 总判断

- 结论：`KEEP_ONE_CHANGE`
- 理由：Protocol vocabulary、JUCE/SenderKit parity、host receiver routing 和 native typed ingest seam 共享同一个 lifecycle vocabulary 决策，必须一起验证才能证明 sender/protocol/native-core 边界没有漂移。
- OpenSpec 下限检查：通过。该 change 涉及公共协议、跨语言 sender、host receiver、native ingest seam 和 regression tests，形成可评审、可验证、可归档的模块闭环。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| v1 lifecycle vocabulary | 防止 sender/receiver/native ingest 语义漂移 | Protocol, JUCE, SenderKit | 不能单独延期 | 是其他簇的共同契约 |
| sender parity | 保证 DAW sender 和 .NET sender 继续按同一 v1 语义工作 | JUCE sender, SenderKit | 依赖 vocabulary | 包含 ack 与 same-handle restart |
| receiver-to-native seam | 保证 host glue 只做 decode/routing，native core 只吃 typed event | App receiver, native-backed service | 依赖 vocabulary | 不迁移 socket/parser |

### 推荐切分 / Top-Level Changes

1. `2026-06-29-juce-sender-protocol-boundary`
   - 范围：本文件“范围内”列表。
   - 不包含：binary protocol、native UDP JSON parser、AAX/AU、WebView transport、packaging。
   - 依赖：`ui-runtime-architecture-replan`、`native-source-ingest-core`。
   - 建议 profile：`strict-change`
   - work 关注点：跨语言 contract tests 优先，保持 v1 wire compatibility。

### 执行顺序

- 当前进入：`opsx-readiness 2026-06-29-juce-sender-protocol-boundary`
- readiness 通过后进入 implement；若 readiness 发现范围仍过大或 native/parser seam 不清晰，回到 plan/explore。
